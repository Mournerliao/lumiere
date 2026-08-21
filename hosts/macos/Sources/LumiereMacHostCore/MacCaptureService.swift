import AppKit
import CoreGraphics
import CoreImage
import CoreImage.CIFilterBuiltins
import Foundation
import ImageIO
import ScreenCaptureKit
import UniformTypeIdentifiers

public actor MacCaptureService {
  public init() {}

  public func response(for request: PlatformRequest) async -> PlatformResponse {
    switch request.method {
    case .getCapabilities:
      return .success(id: request.id, result: .capabilities(await capabilities()))
    case .capture:
      guard let parameters = request.capture else {
        return .failure(
          id: request.id,
          error: HostFailure(
            code: .invalidRequest,
            message: "Capture parameters are required.",
            retryable: false
          )
        )
      }
      return .success(
        id: request.id,
        result: .capture(await capture(parameters: parameters))
      )
    }
  }

  private func capabilities() async -> PlatformCapabilities {
    let target = await ActiveDisplayTargetResolver.resolve()
    return PlatformCapabilities(
      contractVersion: platformContractVersion,
      platform: "macos",
      hostStatus: "available",
      captureModes: [.display],
      hdrCapture: target.supportsHDR ? "supported" : "unavailable",
      outputProfiles: ["srgb-visual-match"]
    )
  }

  private func capture(parameters: CaptureParameters) async -> CaptureResult {
    guard parameters.mode == .display else {
      return .failed(
        HostFailure(
          code: .captureUnavailable,
          message: "The first macOS native slice supports display capture only.",
          retryable: false
        )
      )
    }

    guard parameters.delivery == .folder else {
      return .failed(
        HostFailure(
          code: .captureUnavailable,
          message: "The first macOS native slice supports folder delivery only.",
          retryable: false
        )
      )
    }

    let permission = ScreenRecordingPermission.resolve(
      preflightGranted: CGPreflightScreenCaptureAccess(),
      requestAccess: CGRequestScreenCaptureAccess
    )
    guard permission == .granted else {
      return .failed(
        HostFailure(
          code: .permissionDenied,
          message: "Screen Recording permission is required for display capture.",
          retryable: false
        )
      )
    }

    do {
      let target = await ActiveDisplayTargetResolver.resolve()
      let content = try await SCShareableContent.excludingDesktopWindows(
        false,
        onScreenWindowsOnly: true
      )
      guard let display = content.displays.first(where: { $0.displayID == target.displayID }) else {
        return .failed(
          HostFailure(
            code: .captureUnavailable,
            message: "The active display is not available for capture.",
            retryable: true
          )
        )
      }

      let capturesHDR = target.supportsHDR
      let configuration =
        capturesHDR
        ? SCStreamConfiguration(preset: .captureHDRScreenshotLocalDisplay)
        : SCStreamConfiguration()
      configuration.width = display.width
      configuration.height = display.height
      configuration.showsCursor = true

      let filter = SCContentFilter(display: display, excludingWindows: [])
      let image = try await SCScreenshotManager.captureImage(
        contentFilter: filter,
        configuration: configuration
      )
      let fileURL = try outputURL()
      try writeVisualMatchPNG(image, to: fileURL, sourceIsHDR: capturesHDR)

      return .success(
        dynamicRange: capturesHDR ? "hdr" : "sdr",
        delivery: parameters.delivery,
        filePath: fileURL.path
      )
    } catch let error where CaptureErrorClassification.isCancellation(error) {
      return .cancelled()
    } catch {
      return .failed(mapCaptureError(error))
    }
  }

  private func outputURL() throws -> URL {
    let pictures = FileManager.default.urls(for: .picturesDirectory, in: .userDomainMask)[0]
    let directory = pictures.appendingPathComponent("Lumiere", isDirectory: true)
    try FileManager.default.createDirectory(
      at: directory,
      withIntermediateDirectories: true,
      attributes: nil
    )
    return directory.appendingPathComponent("Lumiere-\(UUID().uuidString).png")
  }

  private func writeVisualMatchPNG(
    _ source: CGImage,
    to url: URL,
    sourceIsHDR: Bool
  ) throws {
    guard let srgb = CGColorSpace(name: CGColorSpace.sRGB) else {
      throw CapturePipelineError.srgbColorSpaceUnavailable
    }

    let input = CIImage(cgImage: source)
    let output: CIImage
    if sourceIsHDR {
      let toneMap = CIFilter.toneMapHeadroom()
      toneMap.inputImage = input
      toneMap.sourceHeadroom = max(input.contentHeadroom, 1.0)
      toneMap.targetHeadroom = 1.0
      guard let toneMapped = toneMap.outputImage else {
        throw CapturePipelineError.toneMapFailed
      }
      output = toneMapped
    } else {
      output = input
    }

    let context = CIContext(options: [
      .workingColorSpace: CGColorSpace(name: CGColorSpace.extendedLinearSRGB) as Any,
      .outputColorSpace: srgb,
    ])
    guard
      let rgba8 = context.createCGImage(
        output,
        from: output.extent,
        format: .RGBA8,
        colorSpace: srgb
      )
    else {
      throw CapturePipelineError.renderFailed
    }

    guard
      let destination = CGImageDestinationCreateWithURL(
        url as CFURL,
        UTType.png.identifier as CFString,
        1,
        nil
      )
    else {
      throw CapturePipelineError.imageDestinationUnavailable
    }
    CGImageDestinationAddImage(
      destination, rgba8,
      [
        kCGImagePropertyColorModel: kCGImagePropertyColorModelRGB,
        kCGImagePropertyProfileName: "sRGB IEC61966-2.1",
      ] as CFDictionary)
    guard CGImageDestinationFinalize(destination) else {
      throw CapturePipelineError.pngWriteFailed
    }
  }

  private func mapCaptureError(_ error: Error) -> HostFailure {
    let nsError = error as NSError
    if nsError.domain == SCStreamErrorDomain,
      nsError.code == SCStreamError.Code.userDeclined.rawValue
    {
      return HostFailure(
        code: .permissionDenied,
        message: "Screen Recording permission was denied.",
        retryable: false
      )
    }

    return HostFailure(
      code: .unexpectedFailure,
      message: "The macOS capture pipeline failed (\(nsError.domain):\(nsError.code)).",
      retryable: true
    )
  }
}

private struct ActiveDisplayTarget: Sendable {
  let displayID: CGDirectDisplayID
  let maximumPotentialHeadroom: CGFloat?

  var supportsHDR: Bool {
    #if arch(arm64)
      return DisplayDynamicRange.supportsHDR(
        architectureSupportsHDR: true,
        maximumPotentialHeadroom: maximumPotentialHeadroom
      )
    #else
      return false
    #endif
  }
}

@MainActor
private enum ActiveDisplayTargetResolver {
  static func resolve() -> ActiveDisplayTarget {
    let screens = NSScreen.screens
    let pointerLocation = NSEvent.mouseLocation
    let pointerScreen = screens.first { screen in
      NSMouseInRect(pointerLocation, screen.frame, false)
    }
    let selectedScreen = pointerScreen ?? NSScreen.main
    let mainScreenDisplayID = displayID(for: selectedScreen)
    let fallbackDisplayID = CGMainDisplayID()
    let selectedDisplayID = DisplayTargetSelection.selectDisplayID(
      pointerDisplayID: displayID(for: pointerScreen),
      mainScreenDisplayID: mainScreenDisplayID,
      fallbackDisplayID: fallbackDisplayID
    )
    let headroom =
      screens
      .first(where: { displayID(for: $0) == selectedDisplayID })?
      .maximumPotentialExtendedDynamicRangeColorComponentValue
    return ActiveDisplayTarget(
      displayID: selectedDisplayID,
      maximumPotentialHeadroom: headroom
    )
  }

  private static func displayID(for screen: NSScreen?) -> CGDirectDisplayID? {
    screen?.deviceDescription[NSDeviceDescriptionKey("NSScreenNumber")]
      as? CGDirectDisplayID
  }
}

enum DisplayDynamicRange {
  static func supportsHDR(
    architectureSupportsHDR: Bool,
    maximumPotentialHeadroom: CGFloat?
  ) -> Bool {
    architectureSupportsHDR && (maximumPotentialHeadroom ?? 1.0) > 1.0
  }
}

enum DisplayTargetSelection {
  static func selectDisplayID(
    pointerDisplayID: CGDirectDisplayID?,
    mainScreenDisplayID: CGDirectDisplayID?,
    fallbackDisplayID: CGDirectDisplayID
  ) -> CGDirectDisplayID {
    pointerDisplayID ?? mainScreenDisplayID ?? fallbackDisplayID
  }
}

enum ScreenRecordingPermission: Equatable {
  case granted
  case deniedOrRestricted

  static func resolve(
    preflightGranted: Bool,
    requestAccess: () -> Bool
  ) -> ScreenRecordingPermission {
    if preflightGranted {
      return .granted
    }
    return requestAccess() ? .granted : .deniedOrRestricted
  }
}

enum CaptureErrorClassification {
  static func isCancellation(_ error: Error) -> Bool {
    if error is CancellationError {
      return true
    }
    let nsError = error as NSError
    return nsError.domain == SCStreamErrorDomain
      && nsError.code == SCStreamError.Code.userStopped.rawValue
  }
}

private enum CapturePipelineError: Error {
  case srgbColorSpaceUnavailable
  case toneMapFailed
  case renderFailed
  case imageDestinationUnavailable
  case pngWriteFailed
}

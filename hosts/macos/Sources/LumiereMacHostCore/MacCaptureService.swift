import AppKit
import CoreGraphics
import CoreImage
import CoreImage.CIFilterBuiltins
import Foundation
import ImageIO
import ScreenCaptureKit

public actor MacCaptureService {
  private var issuedRegionTarget: IssuedRegionTarget?

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
    guard let target = await ActiveDisplayTargetResolver.resolve() else {
      issuedRegionTarget = nil
      return PlatformCapabilities(
        contractVersion: platformContractVersion,
        platform: "macos",
        hostStatus: "available",
        captureModes: [],
        deliveryTargets: [.clipboard, .folder],
        hdrCapture: "unavailable",
        outputProfiles: ["srgb-visual-match"]
      )
    }
    let regionTarget = IssuedRegionTarget(id: UUID().uuidString, target: target)
    issuedRegionTarget = regionTarget
    return PlatformCapabilities(
      contractVersion: platformContractVersion,
      platform: "macos",
      hostStatus: "available",
      captureModes: [.region, .display],
      deliveryTargets: [.clipboard, .folder],
      hdrCapture: target.supportsHDR ? "supported" : "unavailable",
      outputProfiles: ["srgb-visual-match"],
      activeTarget: CaptureTarget(
        id: regionTarget.id,
        logicalSize: LogicalSize(width: target.logicalWidth, height: target.logicalHeight)
      )
    )
  }

  private func capture(parameters: CaptureParameters) async -> CaptureResult {
    guard let captureTarget = await resolveCaptureTarget(for: parameters) else {
      return .failed(
        HostFailure(
          code: .captureUnavailable,
          message: "The capture target changed or is no longer available.",
          retryable: true
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
          message: "Screen Recording permission is required for capture.",
          retryable: false
        )
      )
    }

    do {
      let content = try await SCShareableContent.excludingDesktopWindows(
        false,
        onScreenWindowsOnly: true
      )
      guard
        let display = content.displays.first(where: {
          $0.displayID == captureTarget.target.displayID
        })
      else {
        return .failed(
          HostFailure(
            code: .captureUnavailable,
            message: "The active display is not available for capture.",
            retryable: true
          )
        )
      }

      let capturesHDR = captureTarget.target.supportsHDR
      let configuration =
        capturesHDR
        ? SCStreamConfiguration(preset: .captureHDRScreenshotLocalDisplay)
        : SCStreamConfiguration()
      if let region = captureTarget.region {
        configuration.sourceRect = region.sourceRect
        configuration.width = region.pixelWidth
        configuration.height = region.pixelHeight
      } else {
        configuration.width = display.width
        configuration.height = display.height
      }
      configuration.showsCursor = true

      let filter = SCContentFilter(display: display, excludingWindows: [])
      let image = try await SCScreenshotManager.captureImage(
        contentFilter: filter,
        configuration: configuration
      )
      let visualMatchPNG = try makeVisualMatchPNG(image, sourceIsHDR: capturesHDR)
      let deliveries = await MacCaptureDelivery.deliver(
        visualMatchPNG,
        to: parameters.delivery
      )

      return .completed(
        dynamicRange: capturesHDR ? "hdr" : "sdr",
        deliveries: deliveries
      )
    } catch let error where CaptureErrorClassification.isCancellation(error) {
      return .cancelled()
    } catch {
      return .failed(mapCaptureError(error))
    }
  }

  private func resolveCaptureTarget(
    for parameters: CaptureParameters
  ) async -> ResolvedCaptureTarget? {
    if parameters.mode == .display {
      guard let target = await ActiveDisplayTargetResolver.resolve() else {
        return nil
      }
      return ResolvedCaptureTarget(target: target, region: nil)
    }

    guard let targetId = parameters.targetId,
      let geometry = parameters.geometry,
      let issued = issuedRegionTarget,
      issued.id == targetId
    else {
      return nil
    }
    issuedRegionTarget = nil

    guard
      let current = await ActiveDisplayTargetResolver.resolve(displayID: issued.target.displayID),
      current.hasSameTopology(as: issued.target),
      let region = RegionCaptureGeometry.resolve(
        geometry: geometry,
        targetLogicalSize: LogicalSize(
          width: issued.target.logicalWidth,
          height: issued.target.logicalHeight
        ),
        displayPixelSize: LogicalSize(
          width: Double(CGDisplayPixelsWide(issued.target.displayID)),
          height: Double(CGDisplayPixelsHigh(issued.target.displayID))
        )
      )
    else {
      return nil
    }

    return ResolvedCaptureTarget(target: current, region: region)
  }

  private func makeVisualMatchPNG(
    _ source: CGImage,
    sourceIsHDR: Bool
  ) throws -> Data {
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

    guard let data = CFDataCreateMutable(nil, 0),
      let destination = CGImageDestinationCreateWithData(
        data,
        "public.png" as CFString,
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
    return data as Data
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
  let logicalWidth: Double
  let logicalHeight: Double

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

  func hasSameTopology(as other: ActiveDisplayTarget) -> Bool {
    displayID == other.displayID
      && abs(logicalWidth - other.logicalWidth) < 0.001
      && abs(logicalHeight - other.logicalHeight) < 0.001
  }
}

private struct IssuedRegionTarget: Sendable {
  let id: String
  let target: ActiveDisplayTarget
}

private struct ResolvedCaptureTarget: Sendable {
  let target: ActiveDisplayTarget
  let region: RegionCaptureGeometry?
}

struct RegionCaptureGeometry: Equatable, Sendable {
  let sourceRect: CGRect
  let pixelWidth: Int
  let pixelHeight: Int

  static func resolve(
    geometry: CaptureGeometry,
    targetLogicalSize: LogicalSize,
    displayPixelSize: LogicalSize
  ) -> RegionCaptureGeometry? {
    guard targetLogicalSize.width > 0, targetLogicalSize.height > 0,
      displayPixelSize.width > 0, displayPixelSize.height > 0,
      geometry.x >= 0, geometry.y >= 0,
      geometry.width > 0, geometry.height > 0,
      geometry.x + geometry.width <= targetLogicalSize.width,
      geometry.y + geometry.height <= targetLogicalSize.height
    else {
      return nil
    }

    let scaleX = displayPixelSize.width / targetLogicalSize.width
    let scaleY = displayPixelSize.height / targetLogicalSize.height
    return RegionCaptureGeometry(
      sourceRect: CGRect(
        x: geometry.x,
        y: geometry.y,
        width: geometry.width,
        height: geometry.height
      ),
      pixelWidth: max(1, min(Int(displayPixelSize.width), Int(ceil(geometry.width * scaleX)))),
      pixelHeight: max(
        1,
        min(Int(displayPixelSize.height), Int(ceil(geometry.height * scaleY)))
      )
    )
  }
}

@MainActor
private enum ActiveDisplayTargetResolver {
  static func resolve() -> ActiveDisplayTarget? {
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
    return makeTarget(displayID: selectedDisplayID, screens: screens, allowNativeFallback: true)
  }

  static func resolve(displayID: CGDirectDisplayID) -> ActiveDisplayTarget? {
    makeTarget(displayID: displayID, screens: NSScreen.screens, allowNativeFallback: false)
  }

  private static func makeTarget(
    displayID: CGDirectDisplayID,
    screens: [NSScreen],
    allowNativeFallback: Bool
  ) -> ActiveDisplayTarget? {
    let resolvedScreen = screens.first(where: { self.displayID(for: $0) == displayID })
    guard allowNativeFallback || resolvedScreen != nil else {
      return nil
    }
    let headroom = resolvedScreen?.maximumPotentialExtendedDynamicRangeColorComponentValue
    let logicalSize = resolvedScreen?.frame.size ?? CGDisplayBounds(displayID).size
    guard displayID != 0, logicalSize.width > 0, logicalSize.height > 0 else {
      return nil
    }
    return ActiveDisplayTarget(
      displayID: displayID,
      maximumPotentialHeadroom: headroom,
      logicalWidth: Double(logicalSize.width),
      logicalHeight: Double(logicalSize.height)
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

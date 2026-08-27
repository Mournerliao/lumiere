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

      let filter = SCContentFilter(display: display, excludingWindows: [])
      guard
        let outputGeometry = CaptureOutputGeometry.resolve(
          targetLogicalSize: LogicalSize(
            width: captureTarget.target.logicalWidth,
            height: captureTarget.target.logicalHeight
          ),
          filterLogicalSize: LogicalSize(
            width: Double(filter.contentRect.width),
            height: Double(filter.contentRect.height)
          ),
          pointPixelScale: Double(filter.pointPixelScale),
          region: captureTarget.region
        )
      else {
        return .failed(
          HostFailure(
            code: .captureUnavailable,
            message: "The capture target changed or is no longer available.",
            retryable: true
          )
        )
      }

      let capturesHDR = captureTarget.target.supportsHDR
      let configuration =
        capturesHDR
        ? SCStreamConfiguration(preset: .captureHDRScreenshotLocalDisplay)
        : SCStreamConfiguration()
      outputGeometry.apply(to: configuration)
      configuration.showsCursor = true

      let image = try await SCScreenshotManager.captureImage(
        contentFilter: filter,
        configuration: configuration
      )
      guard
        outputGeometry.matches(
          imageWidth: image.width,
          imageHeight: image.height
        )
      else {
        throw CapturePipelineError.unexpectedImageDimensions(
          expectedWidth: outputGeometry.pixelWidth,
          expectedHeight: outputGeometry.pixelHeight,
          actualWidth: image.width,
          actualHeight: image.height
        )
      }
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
      current.hasSameTopology(as: issued.target)
    else {
      return nil
    }

    return ResolvedCaptureTarget(target: current, region: geometry)
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
    if let pipelineError = error as? CapturePipelineError,
      case .unexpectedImageDimensions(
        let expectedWidth,
        let expectedHeight,
        let actualWidth,
        let actualHeight
      ) = pipelineError
    {
      return HostFailure(
        code: .unexpectedFailure,
        message:
          "The macOS capture pipeline returned \(actualWidth)x\(actualHeight) pixels for a requested \(expectedWidth)x\(expectedHeight) capture.",
        retryable: true
      )
    }

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
  let region: CaptureGeometry?
}

struct CaptureOutputGeometry: Equatable, Sendable {
  let sourceRect: CGRect?
  let pixelWidth: Int
  let pixelHeight: Int

  static func resolve(
    targetLogicalSize: LogicalSize,
    filterLogicalSize: LogicalSize,
    pointPixelScale: Double,
    region: CaptureGeometry?
  ) -> CaptureOutputGeometry? {
    guard isValid(size: targetLogicalSize), isValid(size: filterLogicalSize),
      pointPixelScale.isFinite, pointPixelScale > 0,
      matchesTopology(targetLogicalSize, filterLogicalSize)
    else {
      return nil
    }

    let sourceRect: CGRect?
    let sourceWidth: Double
    let sourceHeight: Double
    if let region {
      guard region.coordinateSpace == "target-logical",
        region.x.isFinite, region.y.isFinite,
        region.width.isFinite, region.height.isFinite,
        region.x >= 0, region.y >= 0,
        region.width > 0, region.height > 0,
        region.x + region.width <= targetLogicalSize.width,
        region.y + region.height <= targetLogicalSize.height
      else {
        return nil
      }
      sourceRect = CGRect(
        x: region.x,
        y: region.y,
        width: region.width,
        height: region.height
      )
      sourceWidth = region.width
      sourceHeight = region.height
    } else {
      sourceRect = nil
      sourceWidth = filterLogicalSize.width
      sourceHeight = filterLogicalSize.height
    }

    guard let pixelWidth = pixelDimension(sourceWidth, scale: pointPixelScale),
      let pixelHeight = pixelDimension(sourceHeight, scale: pointPixelScale)
    else {
      return nil
    }

    return CaptureOutputGeometry(
      sourceRect: sourceRect,
      pixelWidth: pixelWidth,
      pixelHeight: pixelHeight
    )
  }

  func apply(to configuration: SCStreamConfiguration) {
    if let sourceRect {
      configuration.sourceRect = sourceRect
    }
    configuration.width = pixelWidth
    configuration.height = pixelHeight
  }

  func matches(imageWidth: Int, imageHeight: Int) -> Bool {
    imageWidth == pixelWidth && imageHeight == pixelHeight
  }

  private static func isValid(size: LogicalSize) -> Bool {
    size.width.isFinite && size.height.isFinite && size.width > 0 && size.height > 0
  }

  private static func matchesTopology(_ first: LogicalSize, _ second: LogicalSize) -> Bool {
    abs(first.width - second.width) < 0.001
      && abs(first.height - second.height) < 0.001
  }

  private static func pixelDimension(_ points: Double, scale: Double) -> Int? {
    let pixels = ceil(points * scale)
    guard pixels.isFinite, pixels >= 1, pixels <= Double(Int.max) else {
      return nil
    }
    return Int(pixels)
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
  case unexpectedImageDimensions(
    expectedWidth: Int,
    expectedHeight: Int,
    actualWidth: Int,
    actualHeight: Int
  )
  case srgbColorSpaceUnavailable
  case toneMapFailed
  case renderFailed
  case imageDestinationUnavailable
  case pngWriteFailed
}

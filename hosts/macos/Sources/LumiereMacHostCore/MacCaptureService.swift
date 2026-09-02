import AppKit
import CoreGraphics
import CoreImage
import CoreImage.CIFilterBuiltins
import Foundation
import ImageIO
import ScreenCaptureKit

public actor MacCaptureService {
  private static let regionLeaseMilliseconds = 60_000
  private var frozenRegion: FrozenRegionSession?

  public init() {}

  public func response(for request: PlatformRequest) async -> PlatformResponse {
    switch request.method {
    case .getCapabilities:
      return .success(id: request.id, result: .capabilities(await capabilities()))
    case .captureDisplay:
      guard let parameters = request.displayCapture else {
        return invalidRequest(id: request.id, message: "Display capture parameters are required.")
      }
      return .success(
        id: request.id,
        result: .capture(await captureDisplay(parameters: parameters))
      )
    case .prepareRegion:
      let result = await prepareRegion()
      switch result {
      case .success(let prepared):
        return .success(id: request.id, result: .preparedRegion(prepared))
      case .failure(let failure):
        return .success(id: request.id, result: .capture(.failed(failure)))
      }
    case .commitRegion:
      guard let parameters = request.commitRegion else {
        return invalidRequest(id: request.id, message: "Region commit parameters are required.")
      }
      return .success(
        id: request.id,
        result: .capture(await commitRegion(parameters: parameters))
      )
    case .cancelRegion:
      guard let sessionId = request.sessionId else {
        return invalidRequest(id: request.id, message: "Region session id is required.")
      }
      cancelRegion(sessionId: sessionId)
      return .success(id: request.id, result: .releasedRegion(.released))
    }
  }

  public func shutdown() {
    releaseFrozenRegion()
  }

  private func capabilities() async -> PlatformCapabilities {
    guard let target = await ActiveDisplayTargetResolver.resolve() else {
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
    return PlatformCapabilities(
      contractVersion: platformContractVersion,
      platform: "macos",
      hostStatus: "available",
      captureModes: [.region, .display],
      deliveryTargets: [.clipboard, .folder],
      hdrCapture: target.supportsHDR ? "supported" : "unavailable",
      outputProfiles: ["srgb-visual-match"]
    )
  }

  private func captureDisplay(parameters: DisplayCaptureParameters) async -> CaptureResult {
    guard let target = await ActiveDisplayTargetResolver.resolve() else {
      return .failed(
        HostFailure(
          code: .captureUnavailable, message: "The capture target is unavailable.",
          retryable: true
        )
      )
    }

    do {
      let frame = try await acquireFrozenFrame(target: target)
      let visualMatchPNG = try makeVisualMatchPNG(frame.image, sourceIsHDR: frame.capturesHDR)
      let deliveries = await MacCaptureDelivery.deliver(
        visualMatchPNG,
        to: parameters.delivery,
        saveDirectory: parameters.saveDirectory
      )
      return .completed(
        dynamicRange: frame.capturesHDR ? "hdr" : "sdr",
        deliveries: deliveries
      )
    } catch let error where CaptureErrorClassification.isCancellation(error) {
      return .cancelled()
    } catch {
      return .failed(mapCaptureError(error))
    }
  }

  private func prepareRegion() async -> PrepareOutcome {
    releaseFrozenRegion()
    guard let target = await ActiveDisplayTargetResolver.resolve() else {
      return .failure(
        HostFailure(
          code: .captureUnavailable,
          message: "The capture target is unavailable.",
          retryable: true
        )
      )
    }

    do {
      let frame = try await acquireFrozenFrame(target: target)
      let previewData = try makeVisualMatchPNG(frame.image, sourceIsHDR: frame.capturesHDR)
      let previewURL = try writeRegionPreview(previewData)
      let sessionId = UUID().uuidString
      let expiration = Task { [weak self] in
        try? await Task.sleep(for: .milliseconds(Self.regionLeaseMilliseconds))
        await self?.expireRegion(sessionId: sessionId)
      }
      frozenRegion = FrozenRegionSession(
        id: sessionId,
        frame: frame,
        previewURL: previewURL,
        expiration: expiration
      )
      return .success(
        PreparedRegionResult(
          status: "prepared",
          sessionId: sessionId,
          targetLogicalSize: LogicalSize(
            width: target.logicalWidth,
            height: target.logicalHeight
          ),
          preview: PreparedRegionResult.Preview(
            filePath: previewURL.path,
            mediaType: "image/png",
            pixelSize: PixelSize(width: frame.image.width, height: frame.image.height)
          ),
          leaseMilliseconds: Self.regionLeaseMilliseconds
        )
      )
    } catch {
      return .failure(mapCaptureError(error))
    }
  }

  private func commitRegion(parameters: CommitRegionParameters) async -> CaptureResult {
    guard let session = frozenRegion, session.id == parameters.sessionId else {
      return .failed(
        HostFailure(
          code: .captureUnavailable,
          message: "The frozen Region capture expired. Select the region again.",
          retryable: true
        )
      )
    }
    frozenRegion = nil
    session.expiration.cancel()
    defer { try? FileManager.default.removeItem(at: session.previewURL) }

    guard
      let geometry = CaptureOutputGeometry.resolve(
        targetLogicalSize: session.frame.targetLogicalSize,
        filterLogicalSize: session.frame.filterLogicalSize,
        pointPixelScale: session.frame.pointPixelScale,
        region: parameters.geometry
      ),
      let image = geometry.makeOutputImage(from: session.frame.image)
    else {
      return .failed(
        HostFailure(
          code: .captureUnavailable,
          message: "The Region selection is outside the frozen capture.",
          retryable: true
        )
      )
    }

    do {
      let visualMatchPNG = try makeVisualMatchPNG(
        image,
        sourceIsHDR: session.frame.capturesHDR
      )
      let deliveries = await MacCaptureDelivery.deliver(
        visualMatchPNG,
        to: parameters.delivery,
        saveDirectory: parameters.saveDirectory
      )
      return .completed(
        dynamicRange: session.frame.capturesHDR ? "hdr" : "sdr",
        deliveries: deliveries
      )
    } catch let error where CaptureErrorClassification.isCancellation(error) {
      return .cancelled()
    } catch {
      return .failed(mapCaptureError(error))
    }
  }

  private func cancelRegion(sessionId: String) {
    guard frozenRegion?.id == sessionId else { return }
    releaseFrozenRegion()
  }

  private func expireRegion(sessionId: String) {
    guard frozenRegion?.id == sessionId else { return }
    releaseFrozenRegion()
  }

  private func releaseFrozenRegion() {
    guard let session = frozenRegion else { return }
    frozenRegion = nil
    session.expiration.cancel()
    try? FileManager.default.removeItem(at: session.previewURL)
  }

  private func acquireFrozenFrame(target: ActiveDisplayTarget) async throws -> FrozenFrame {
    let permission = ScreenRecordingPermission.resolve(
      preflightGranted: CGPreflightScreenCaptureAccess(),
      requestAccess: CGRequestScreenCaptureAccess
    )
    guard permission == .granted else {
      throw MacCaptureSessionError.permissionDenied
    }

    let content = try await SCShareableContent.excludingDesktopWindows(
      false,
      onScreenWindowsOnly: true
    )
    guard let display = content.displays.first(where: { $0.displayID == target.displayID }) else {
      throw MacCaptureSessionError.targetUnavailable
    }
    let filter = SCContentFilter(display: display, excludingWindows: [])
    let targetLogicalSize = LogicalSize(width: target.logicalWidth, height: target.logicalHeight)
    let filterLogicalSize = LogicalSize(
      width: Double(filter.contentRect.width),
      height: Double(filter.contentRect.height)
    )
    let pointPixelScale = Double(filter.pointPixelScale)
    guard
      let geometry = CaptureOutputGeometry.resolve(
        targetLogicalSize: targetLogicalSize,
        filterLogicalSize: filterLogicalSize,
        pointPixelScale: pointPixelScale,
        region: nil
      )
    else {
      throw MacCaptureSessionError.targetUnavailable
    }
    let capturesHDR = target.supportsHDR
    let configuration =
      capturesHDR
      ? SCStreamConfiguration(preset: .captureHDRScreenshotLocalDisplay)
      : SCStreamConfiguration()
    geometry.apply(to: configuration)
    configuration.showsCursor = true
    let image = try await SCScreenshotManager.captureImage(
      contentFilter: filter,
      configuration: configuration
    )
    guard geometry.matchesCapture(imageWidth: image.width, imageHeight: image.height) else {
      throw CapturePipelineError.unexpectedImageDimensions(
        expectedWidth: geometry.capturePixelWidth,
        expectedHeight: geometry.capturePixelHeight,
        actualWidth: image.width,
        actualHeight: image.height
      )
    }
    return FrozenFrame(
      image: image,
      capturesHDR: capturesHDR,
      targetLogicalSize: targetLogicalSize,
      filterLogicalSize: filterLogicalSize,
      pointPixelScale: pointPixelScale
    )
  }

  private func writeRegionPreview(_ data: Data) throws -> URL {
    let directory = URL(fileURLWithPath: NSTemporaryDirectory(), isDirectory: true)
      .appendingPathComponent("lumiere-region-preview", isDirectory: true)
    try FileManager.default.createDirectory(
      at: directory,
      withIntermediateDirectories: true,
      attributes: nil
    )
    let fileURL = directory.appendingPathComponent("\(UUID().uuidString).png")
    try data.write(to: fileURL, options: .atomic)
    return fileURL
  }

  private func invalidRequest(id: String, message: String) -> PlatformResponse {
    .failure(
      id: id,
      error: HostFailure(code: .invalidRequest, message: message, retryable: false)
    )
  }

  private enum PrepareOutcome {
    case success(PreparedRegionResult)
    case failure(HostFailure)
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
    if let sessionError = error as? MacCaptureSessionError {
      switch sessionError {
      case .permissionDenied:
        return HostFailure(
          code: .permissionDenied,
          message: "Screen Recording permission is required for capture.",
          retryable: false
        )
      case .targetUnavailable:
        return HostFailure(
          code: .captureUnavailable,
          message: "The active display is not available for capture.",
          retryable: true
        )
      }
    }
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

private enum MacCaptureSessionError: Error {
  case permissionDenied
  case targetUnavailable
}

private struct FrozenFrame {
  let image: CGImage
  let capturesHDR: Bool
  let targetLogicalSize: LogicalSize
  let filterLogicalSize: LogicalSize
  let pointPixelScale: Double
}

private struct FrozenRegionSession {
  let id: String
  let frame: FrozenFrame
  let previewURL: URL
  let expiration: Task<Void, Never>
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

struct CaptureOutputGeometry: Equatable, Sendable {
  let capturePixelWidth: Int
  let capturePixelHeight: Int
  let cropRect: CGRect?
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

    guard
      let capturePixelWidth = pixelDimension(
        filterLogicalSize.width,
        scale: pointPixelScale
      ),
      let capturePixelHeight = pixelDimension(
        filterLogicalSize.height,
        scale: pointPixelScale
      )
    else {
      return nil
    }

    let cropRect: CGRect?
    let outputPixelWidth: Int
    let outputPixelHeight: Int
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

      guard
        let pixelMinX = pixelEdge(region.x, scale: pointPixelScale, rounding: .down),
        let pixelMinY = pixelEdge(region.y, scale: pointPixelScale, rounding: .down),
        let pixelMaxX = pixelEdge(
          region.x + region.width,
          scale: pointPixelScale,
          rounding: .up
        ),
        let pixelMaxY = pixelEdge(
          region.y + region.height,
          scale: pointPixelScale,
          rounding: .up
        ),
        pixelMaxX > pixelMinX,
        pixelMaxY > pixelMinY,
        pixelMaxX <= capturePixelWidth,
        pixelMaxY <= capturePixelHeight
      else {
        return nil
      }

      outputPixelWidth = pixelMaxX - pixelMinX
      outputPixelHeight = pixelMaxY - pixelMinY
      cropRect = CGRect(
        x: pixelMinX,
        y: pixelMinY,
        width: outputPixelWidth,
        height: outputPixelHeight
      )
    } else {
      cropRect = nil
      outputPixelWidth = capturePixelWidth
      outputPixelHeight = capturePixelHeight
    }

    return CaptureOutputGeometry(
      capturePixelWidth: capturePixelWidth,
      capturePixelHeight: capturePixelHeight,
      cropRect: cropRect,
      pixelWidth: outputPixelWidth,
      pixelHeight: outputPixelHeight
    )
  }

  func apply(to configuration: SCStreamConfiguration) {
    configuration.width = capturePixelWidth
    configuration.height = capturePixelHeight
  }

  func matchesCapture(imageWidth: Int, imageHeight: Int) -> Bool {
    imageWidth == capturePixelWidth && imageHeight == capturePixelHeight
  }

  func makeOutputImage(from capturedImage: CGImage) -> CGImage? {
    guard
      matchesCapture(
        imageWidth: capturedImage.width,
        imageHeight: capturedImage.height
      )
    else {
      return nil
    }
    guard let cropRect else {
      return capturedImage
    }
    guard let croppedImage = capturedImage.cropping(to: cropRect),
      croppedImage.width == pixelWidth,
      croppedImage.height == pixelHeight
    else {
      return nil
    }
    return croppedImage
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

  private static func pixelEdge(
    _ points: Double,
    scale: Double,
    rounding: FloatingPointRoundingRule
  ) -> Int? {
    let pixels = (points * scale).rounded(rounding)
    guard pixels.isFinite, pixels >= 0, pixels <= Double(Int.max) else {
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
  case cropFailed
  case toneMapFailed
  case renderFailed
  case imageDestinationUnavailable
  case pngWriteFailed
}

import CoreGraphics
import Foundation
import ScreenCaptureKit
import Testing

@testable import LumiereMacHostCore

@Test
func decodesGetCapabilitiesRequest() throws {
  let request = try PlatformRequestDecoder.decode(
    line: #"{"version":2,"id":"capabilities-1","method":"getCapabilities","params":{}}"#
  )

  #expect(request.version == 2)
  #expect(request.id == "capabilities-1")
  #expect(request.method == .getCapabilities)
  #expect(request.capture == nil)
}

@Test
func decodesDisplayFolderCaptureRequest() throws {
  let request = try PlatformRequestDecoder.decode(
    line:
      #"{"version":2,"id":"capture-1","method":"capture","params":{"mode":"display","delivery":"folder"}}"#
  )

  #expect(request.capture == CaptureParameters(mode: .display, delivery: .folder))
}

@Test
func decodesTargetLocalRegionCaptureRequest() throws {
  let request = try PlatformRequestDecoder.decode(
    line:
      #"{"version":2,"id":"capture-2","method":"capture","params":{"mode":"region","delivery":"both","targetId":"display-17","geometry":{"coordinateSpace":"target-logical","x":10.5,"y":20,"width":640,"height":360}}}"#
  )

  #expect(request.capture?.mode == .region)
  #expect(request.capture?.targetId == "display-17")
  #expect(request.capture?.geometry?.x == 10.5)
  #expect(request.capture?.geometry?.width == 640)
}

@Test
func rejectsUnknownFields() {
  #expect(throws: PlatformProtocolError.self) {
    try PlatformRequestDecoder.decode(
      line: #"{"version":2,"id":"bad-1","method":"getCapabilities","params":{},"extra":true}"#
    )
  }
}

@Test
func rejectsUnknownProtocolVersion() {
  #expect(throws: PlatformProtocolError.self) {
    try PlatformRequestDecoder.decode(
      line: #"{"version":1,"id":"bad-2","method":"getCapabilities","params":{}}"#
    )
  }
}

@Test
func encodesCapabilitiesResponseWithoutOptionalNulls() throws {
  let response = PlatformResponse.success(
    id: "capabilities-1",
    result: .capabilities(
      PlatformCapabilities(
        contractVersion: 2,
        platform: "macos",
        hostStatus: "available",
        captureModes: [.display],
        deliveryTargets: [.folder],
        hdrCapture: "unvalidated",
        outputProfiles: ["srgb-visual-match"]
      )
    )
  )

  let line = try PlatformResponseEncoder.encodeLine(response)
  let object = try #require(
    JSONSerialization.jsonObject(with: Data(line.utf8)) as? [String: Any]
  )

  #expect(object["version"] as? Int == 2)
  #expect(object["id"] as? String == "capabilities-1")
  #expect(object["error"] == nil)
  let result = try #require(object["result"] as? [String: Any])
  #expect(result["platform"] as? String == "macos")
}

@Test(arguments: [
  (true, CGFloat(16.0), true),
  (true, CGFloat(1.0), false),
  (false, CGFloat(16.0), false),
])
func detectsHDRFromTargetDisplayHeadroom(
  architectureSupportsHDR: Bool,
  maximumPotentialHeadroom: CGFloat,
  expected: Bool
) {
  #expect(
    DisplayDynamicRange.supportsHDR(
      architectureSupportsHDR: architectureSupportsHDR,
      maximumPotentialHeadroom: maximumPotentialHeadroom
    ) == expected
  )
}

@Test
func resolvesHiDPIDisplayPixelsFromFilterScale() throws {
  let output = try #require(
    CaptureOutputGeometry.resolve(
      targetLogicalSize: LogicalSize(width: 2560, height: 1440),
      filterLogicalSize: LogicalSize(width: 2560, height: 1440),
      pointPixelScale: 2,
      region: nil
    )
  )

  #expect(output.sourceRect == nil)
  #expect(output.pixelWidth == 5120)
  #expect(output.pixelHeight == 2880)
}

@Test
func resolvesReportedBlurryRegionAtHiDPIScale() throws {
  let output = try #require(
    CaptureOutputGeometry.resolve(
      targetLogicalSize: LogicalSize(width: 2560, height: 1440),
      filterLogicalSize: LogicalSize(width: 2560, height: 1440),
      pointPixelScale: 2,
      region: CaptureGeometry(
        coordinateSpace: "target-logical",
        x: 100,
        y: 50,
        width: 1408,
        height: 821
      )
    )
  )

  #expect(output.sourceRect == CGRect(x: 100, y: 50, width: 1408, height: 821))
  #expect(output.pixelWidth == 2816)
  #expect(output.pixelHeight == 1642)
}

@Test
func keepsOneToOneDisplaysAtLogicalSize() throws {
  let output = try #require(
    CaptureOutputGeometry.resolve(
      targetLogicalSize: LogicalSize(width: 1920, height: 1080),
      filterLogicalSize: LogicalSize(width: 1920, height: 1080),
      pointPixelScale: 1,
      region: nil
    )
  )

  #expect(output.pixelWidth == 1920)
  #expect(output.pixelHeight == 1080)
}

@Test
func roundsFractionalRegionPixelsUp() throws {
  let output = try #require(
    CaptureOutputGeometry.resolve(
      targetLogicalSize: LogicalSize(width: 2560, height: 1440),
      filterLogicalSize: LogicalSize(width: 2560, height: 1440),
      pointPixelScale: 1.5,
      region: CaptureGeometry(
        coordinateSpace: "target-logical",
        x: 10.25,
        y: 20.5,
        width: 100.25,
        height: 50.25
      )
    )
  )

  #expect(output.pixelWidth == 151)
  #expect(output.pixelHeight == 76)
}

@Test
func preservesRotatedDisplayAxes() throws {
  let output = try #require(
    CaptureOutputGeometry.resolve(
      targetLogicalSize: LogicalSize(width: 1080, height: 1920),
      filterLogicalSize: LogicalSize(width: 1080, height: 1920),
      pointPixelScale: 2,
      region: nil
    )
  )

  #expect(output.pixelWidth == 2160)
  #expect(output.pixelHeight == 3840)
}

@Test(arguments: [0.0, -1.0, Double.nan, Double.infinity])
func rejectsInvalidPointPixelScale(pointPixelScale: Double) {
  #expect(
    CaptureOutputGeometry.resolve(
      targetLogicalSize: LogicalSize(width: 2560, height: 1440),
      filterLogicalSize: LogicalSize(width: 2560, height: 1440),
      pointPixelScale: pointPixelScale,
      region: nil
    ) == nil
  )
}

@Test
func rejectsFilterTopologyMismatch() {
  #expect(
    CaptureOutputGeometry.resolve(
      targetLogicalSize: LogicalSize(width: 2560, height: 1440),
      filterLogicalSize: LogicalSize(width: 1920, height: 1080),
      pointPixelScale: 2,
      region: nil
    ) == nil
  )
}

@Test
func rejectsRegionOutsideIssuedTargetBounds() {
  #expect(
    CaptureOutputGeometry.resolve(
      targetLogicalSize: LogicalSize(width: 1512, height: 982),
      filterLogicalSize: LogicalSize(width: 1512, height: 982),
      pointPixelScale: 2,
      region: CaptureGeometry(
        coordinateSpace: "target-logical",
        x: 1400,
        y: 900,
        width: 200,
        height: 100
      )
    ) == nil
  )
}

@Test
func appliesPixelGeometryWithoutChangingHDRPreset() throws {
  let output = try #require(
    CaptureOutputGeometry.resolve(
      targetLogicalSize: LogicalSize(width: 1728, height: 1117),
      filterLogicalSize: LogicalSize(width: 1728, height: 1117),
      pointPixelScale: 2,
      region: nil
    )
  )
  let configuration = SCStreamConfiguration(preset: .captureHDRScreenshotLocalDisplay)
  let dynamicRange = configuration.captureDynamicRange

  output.apply(to: configuration)

  #expect(configuration.width == 3456)
  #expect(configuration.height == 2234)
  #expect(configuration.captureDynamicRange == dynamicRange)
}

@Test
func appliesRegionGeometryToSDRConfiguration() throws {
  let output = try #require(
    CaptureOutputGeometry.resolve(
      targetLogicalSize: LogicalSize(width: 2560, height: 1440),
      filterLogicalSize: LogicalSize(width: 2560, height: 1440),
      pointPixelScale: 2,
      region: CaptureGeometry(
        coordinateSpace: "target-logical",
        x: 20,
        y: 30,
        width: 640,
        height: 360
      )
    )
  )
  let configuration = SCStreamConfiguration()
  let dynamicRange = configuration.captureDynamicRange

  output.apply(to: configuration)

  #expect(configuration.sourceRect == CGRect(x: 20, y: 30, width: 640, height: 360))
  #expect(configuration.width == 1280)
  #expect(configuration.height == 720)
  #expect(configuration.captureDynamicRange == dynamicRange)
}

@Test
func rejectsUnexpectedCapturedImageDimensions() throws {
  let output = try #require(
    CaptureOutputGeometry.resolve(
      targetLogicalSize: LogicalSize(width: 1408, height: 821),
      filterLogicalSize: LogicalSize(width: 1408, height: 821),
      pointPixelScale: 2,
      region: nil
    )
  )

  #expect(output.matches(imageWidth: 2816, imageHeight: 1642))
  #expect(!output.matches(imageWidth: 1408, imageHeight: 821))
}

@Test
func selectsPointerDisplayBeforeMainAndFallbackDisplays() {
  #expect(
    DisplayTargetSelection.selectDisplayID(
      pointerDisplayID: 17,
      mainScreenDisplayID: 23,
      fallbackDisplayID: 29
    ) == 17
  )
  #expect(
    DisplayTargetSelection.selectDisplayID(
      pointerDisplayID: nil,
      mainScreenDisplayID: 23,
      fallbackDisplayID: 29
    ) == 23
  )
  #expect(
    DisplayTargetSelection.selectDisplayID(
      pointerDisplayID: nil,
      mainScreenDisplayID: nil,
      fallbackDisplayID: 29
    ) == 29
  )
}

@Test
func encodesStructuredFailureDiagnosticWithRequestIdentity() throws {
  let line = try HostDiagnosticEncoder.encodeLine(
    HostDiagnostic(
      requestID: "capture-17",
      event: "capture-failed",
      failure: HostFailure(
        code: .unexpectedFailure,
        message: "Capture failed (ScreenCaptureKit:-1).",
        retryable: true
      )
    )
  )
  let object = try #require(
    JSONSerialization.jsonObject(with: Data(line.utf8)) as? [String: Any]
  )

  #expect(object["level"] as? String == "error")
  #expect(object["event"] as? String == "capture-failed")
  #expect(object["requestID"] as? String == "capture-17")
  #expect(object["code"] as? String == "unexpected-failure")
  #expect(object["retryable"] as? Bool == true)
}

@Test
func resolvesExplicitScreenRecordingPermissionPaths() {
  var requestCount = 0
  let alreadyGranted = ScreenRecordingPermission.resolve(preflightGranted: true) {
    requestCount += 1
    return false
  }
  #expect(alreadyGranted == .granted)
  #expect(requestCount == 0)

  let grantedAfterRequest = ScreenRecordingPermission.resolve(preflightGranted: false) {
    requestCount += 1
    return true
  }
  #expect(grantedAfterRequest == .granted)
  #expect(requestCount == 1)

  let deniedOrRestricted = ScreenRecordingPermission.resolve(preflightGranted: false) {
    requestCount += 1
    return false
  }
  #expect(deniedOrRestricted == .deniedOrRestricted)
  #expect(requestCount == 2)
}

@Test
func classifiesTaskAndUserStoppedErrorsAsCancellation() {
  #expect(CaptureErrorClassification.isCancellation(CancellationError()))
  #expect(
    CaptureErrorClassification.isCancellation(
      NSError(
        domain: SCStreamErrorDomain,
        code: SCStreamError.Code.userStopped.rawValue
      )
    )
  )
  #expect(
    !CaptureErrorClassification.isCancellation(
      NSError(domain: SCStreamErrorDomain, code: SCStreamError.Code.internalError.rawValue)
    )
  )
}

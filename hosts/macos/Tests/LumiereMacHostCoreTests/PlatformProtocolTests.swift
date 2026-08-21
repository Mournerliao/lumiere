import Foundation
import Testing

@testable import LumiereMacHostCore

@Test
func decodesGetCapabilitiesRequest() throws {
  let request = try PlatformRequestDecoder.decode(
    line: #"{"version":1,"id":"capabilities-1","method":"getCapabilities","params":{}}"#
  )

  #expect(request.version == 1)
  #expect(request.id == "capabilities-1")
  #expect(request.method == .getCapabilities)
  #expect(request.capture == nil)
}

@Test
func decodesDisplayFolderCaptureRequest() throws {
  let request = try PlatformRequestDecoder.decode(
    line:
      #"{"version":1,"id":"capture-1","method":"capture","params":{"mode":"display","delivery":"folder"}}"#
  )

  #expect(request.capture == CaptureParameters(mode: .display, delivery: .folder))
}

@Test
func rejectsUnknownFields() {
  #expect(throws: PlatformProtocolError.self) {
    try PlatformRequestDecoder.decode(
      line: #"{"version":1,"id":"bad-1","method":"getCapabilities","params":{},"extra":true}"#
    )
  }
}

@Test
func rejectsUnknownProtocolVersion() {
  #expect(throws: PlatformProtocolError.self) {
    try PlatformRequestDecoder.decode(
      line: #"{"version":2,"id":"bad-2","method":"getCapabilities","params":{}}"#
    )
  }
}

@Test
func encodesCapabilitiesResponseWithoutOptionalNulls() throws {
  let response = PlatformResponse.success(
    id: "capabilities-1",
    result: .capabilities(
      PlatformCapabilities(
        contractVersion: 1,
        platform: "macos",
        hostStatus: "available",
        captureModes: [.display],
        hdrCapture: "unvalidated",
        outputProfiles: ["srgb-visual-match"]
      )
    )
  )

  let line = try PlatformResponseEncoder.encodeLine(response)
  let object = try #require(
    JSONSerialization.jsonObject(with: Data(line.utf8)) as? [String: Any]
  )

  #expect(object["version"] as? Int == 1)
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

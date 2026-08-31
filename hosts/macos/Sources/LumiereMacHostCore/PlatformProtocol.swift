import Foundation

public let platformContractVersion = 2

public enum HostFailureCode: String, Codable, Sendable {
  case hostUnavailable = "host-unavailable"
  case permissionDenied = "permission-denied"
  case captureUnavailable = "capture-unavailable"
  case deliveryUnavailable = "delivery-unavailable"
  case deliveryFailed = "delivery-failed"
  case invalidRequest = "invalid-request"
  case unexpectedFailure = "unexpected-failure"
}

public struct HostFailure: Codable, Equatable, Sendable {
  public let code: HostFailureCode
  public let message: String
  public let retryable: Bool

  public init(code: HostFailureCode, message: String, retryable: Bool) {
    self.code = code
    self.message = message
    self.retryable = retryable
  }
}

public enum CaptureMode: String, Codable, Sendable {
  case region
  case display
}

public enum OutputDelivery: String, Codable, Sendable {
  case clipboard
  case folder
  case both
}

public enum DeliveryTarget: String, Codable, Sendable {
  case clipboard
  case folder
}

public struct LogicalSize: Codable, Equatable, Sendable {
  public let width: Double
  public let height: Double
}

public struct CaptureTarget: Codable, Equatable, Sendable {
  public let id: String
  public let logicalSize: LogicalSize
}

public struct CaptureGeometry: Codable, Equatable, Sendable {
  public let coordinateSpace: String
  public let x: Double
  public let y: Double
  public let width: Double
  public let height: Double
}

public struct CaptureParameters: Codable, Equatable, Sendable {
  public let mode: CaptureMode
  public let delivery: OutputDelivery
  public let saveDirectory: String?
  public let targetId: String?
  public let geometry: CaptureGeometry?

  public init(
    mode: CaptureMode,
    delivery: OutputDelivery,
    saveDirectory: String? = nil,
    targetId: String? = nil,
    geometry: CaptureGeometry? = nil
  ) {
    self.mode = mode
    self.delivery = delivery
    self.saveDirectory = saveDirectory
    self.targetId = targetId
    self.geometry = geometry
  }
}

public enum PlatformMethod: String, Codable, Sendable {
  case getCapabilities
  case capture
}

public struct PlatformRequest: Equatable, Sendable {
  public let version: Int
  public let id: String
  public let method: PlatformMethod
  public let capture: CaptureParameters?
}

public struct PlatformCapabilities: Codable, Equatable, Sendable {
  public let contractVersion: Int
  public let platform: String
  public let hostStatus: String
  public let captureModes: [CaptureMode]
  public let deliveryTargets: [DeliveryTarget]
  public let hdrCapture: String
  public let outputProfiles: [String]
  public let activeTarget: CaptureTarget?
  public let unavailableReason: HostFailure?

  public init(
    contractVersion: Int,
    platform: String,
    hostStatus: String,
    captureModes: [CaptureMode],
    deliveryTargets: [DeliveryTarget],
    hdrCapture: String,
    outputProfiles: [String],
    activeTarget: CaptureTarget? = nil,
    unavailableReason: HostFailure? = nil
  ) {
    self.contractVersion = contractVersion
    self.platform = platform
    self.hostStatus = hostStatus
    self.captureModes = captureModes
    self.deliveryTargets = deliveryTargets
    self.hdrCapture = hdrCapture
    self.outputProfiles = outputProfiles
    self.activeTarget = activeTarget
    self.unavailableReason = unavailableReason
  }
}

public struct DeliveryResult: Codable, Equatable, Sendable {
  public let target: DeliveryTarget
  public let status: String
  public let filePath: String?
  public let failure: HostFailure?

  public static func clipboardSuccess() -> DeliveryResult {
    DeliveryResult(target: .clipboard, status: "success", filePath: nil, failure: nil)
  }

  public static func folderSuccess(filePath: String) -> DeliveryResult {
    DeliveryResult(target: .folder, status: "success", filePath: filePath, failure: nil)
  }

  public static func failed(target: DeliveryTarget, failure: HostFailure) -> DeliveryResult {
    DeliveryResult(target: target, status: "failed", filePath: nil, failure: failure)
  }
}

public struct CaptureResult: Codable, Equatable, Sendable {
  public let status: String
  public let sourceDynamicRange: String?
  public let outputProfile: String?
  public let deliveries: [DeliveryResult]?
  public let failure: HostFailure?

  public static func completed(
    dynamicRange: String,
    deliveries: [DeliveryResult]
  ) -> CaptureResult {
    CaptureResult(
      status: "completed",
      sourceDynamicRange: dynamicRange,
      outputProfile: "srgb-visual-match",
      deliveries: deliveries,
      failure: nil
    )
  }

  public static func failed(_ failure: HostFailure) -> CaptureResult {
    CaptureResult(
      status: "failed",
      sourceDynamicRange: nil,
      outputProfile: nil,
      deliveries: nil,
      failure: failure
    )
  }

  public static func cancelled() -> CaptureResult {
    CaptureResult(
      status: "cancelled",
      sourceDynamicRange: nil,
      outputProfile: nil,
      deliveries: nil,
      failure: nil
    )
  }
}

public enum PlatformResult: Equatable, Sendable {
  case capabilities(PlatformCapabilities)
  case capture(CaptureResult)
}

public struct PlatformResponse: Encodable, Sendable {
  public let version: Int
  public let id: String
  public let result: PlatformResult?
  public let error: HostFailure?

  public static func success(id: String, result: PlatformResult) -> PlatformResponse {
    PlatformResponse(version: platformContractVersion, id: id, result: result, error: nil)
  }

  public static func failure(id: String, error: HostFailure) -> PlatformResponse {
    PlatformResponse(version: platformContractVersion, id: id, result: nil, error: error)
  }

  private enum CodingKeys: String, CodingKey {
    case version
    case id
    case result
    case error
  }

  public func encode(to encoder: Encoder) throws {
    var container = encoder.container(keyedBy: CodingKeys.self)
    try container.encode(version, forKey: .version)
    try container.encode(id, forKey: .id)

    if let error {
      try container.encode(error, forKey: .error)
      return
    }

    switch result {
    case .capabilities(let capabilities):
      try container.encode(capabilities, forKey: .result)
    case .capture(let capture):
      try container.encode(capture, forKey: .result)
    case nil:
      throw EncodingError.invalidValue(
        self,
        EncodingError.Context(
          codingPath: encoder.codingPath,
          debugDescription: "A platform response requires a result or error."
        )
      )
    }
  }
}

public enum PlatformProtocolError: Error, Equatable, CustomStringConvertible {
  case invalidJSON
  case invalidEnvelope(String)

  public var description: String {
    switch self {
    case .invalidJSON:
      return "Request must be one complete JSON object."
    case .invalidEnvelope(let message):
      return message
    }
  }
}

public enum PlatformRequestDecoder {
  public static func decode(line: String) throws -> PlatformRequest {
    guard let data = line.data(using: .utf8),
      let object = try? JSONSerialization.jsonObject(with: data),
      let envelope = object as? [String: Any]
    else {
      throw PlatformProtocolError.invalidJSON
    }

    try requireExactKeys(envelope, expected: ["version", "id", "method", "params"])

    guard let version = envelope["version"] as? Int, version == platformContractVersion else {
      throw PlatformProtocolError.invalidEnvelope("Protocol version must be 2.")
    }

    guard let id = envelope["id"] as? String, !id.isEmpty else {
      throw PlatformProtocolError.invalidEnvelope("Request id must be a non-empty string.")
    }

    guard let methodValue = envelope["method"] as? String,
      let method = PlatformMethod(rawValue: methodValue)
    else {
      throw PlatformProtocolError.invalidEnvelope("Unknown platform-host method.")
    }

    guard let parameters = envelope["params"] as? [String: Any] else {
      throw PlatformProtocolError.invalidEnvelope("Request params must be an object.")
    }

    switch method {
    case .getCapabilities:
      try requireExactKeys(parameters, expected: [])
      return PlatformRequest(version: version, id: id, method: method, capture: nil)
    case .capture:
      let capture = try decodeCaptureParameters(parameters)
      return PlatformRequest(version: version, id: id, method: method, capture: capture)
    }
  }

  private static func decodeCaptureParameters(
    _ parameters: [String: Any]
  ) throws -> CaptureParameters {
    guard let modeValue = parameters["mode"] as? String,
      let mode = CaptureMode(rawValue: modeValue)
    else {
      throw PlatformProtocolError.invalidEnvelope("Capture mode must be region or display.")
    }
    guard let deliveryValue = parameters["delivery"] as? String,
      let delivery = OutputDelivery(rawValue: deliveryValue)
    else {
      throw PlatformProtocolError.invalidEnvelope(
        "Capture delivery must be clipboard, folder, or both."
      )
    }
    let saveDirectory = try decodeSaveDirectory(parameters, delivery: delivery)

    if mode == .display {
      var expectedKeys: Set<String> = ["mode", "delivery"]
      if saveDirectory != nil { expectedKeys.insert("saveDirectory") }
      try requireExactKeys(parameters, expected: expectedKeys)
      return CaptureParameters(
        mode: mode,
        delivery: delivery,
        saveDirectory: saveDirectory
      )
    }

    var expectedKeys: Set<String> = ["mode", "delivery", "targetId", "geometry"]
    if saveDirectory != nil { expectedKeys.insert("saveDirectory") }
    try requireExactKeys(parameters, expected: expectedKeys)
    guard let targetId = parameters["targetId"] as? String, !targetId.isEmpty,
      let geometryObject = parameters["geometry"] as? [String: Any]
    else {
      throw PlatformProtocolError.invalidEnvelope(
        "Region capture requires a non-empty target id and geometry."
      )
    }
    let geometry = try decodeGeometry(geometryObject)
    return CaptureParameters(
      mode: mode,
      delivery: delivery,
      saveDirectory: saveDirectory,
      targetId: targetId,
      geometry: geometry
    )
  }

  private static func decodeSaveDirectory(
    _ parameters: [String: Any],
    delivery: OutputDelivery
  ) throws -> String? {
    guard let value = parameters["saveDirectory"] else {
      return nil
    }
    guard delivery != .clipboard else {
      throw PlatformProtocolError.invalidEnvelope(
        "Clipboard-only capture must not include a save directory."
      )
    }
    guard let path = value as? String, !path.isEmpty, path.hasPrefix("/") else {
      throw PlatformProtocolError.invalidEnvelope(
        "Save directory must be an absolute non-empty path."
      )
    }
    return path
  }

  private static func decodeGeometry(_ object: [String: Any]) throws -> CaptureGeometry {
    try requireExactKeys(
      object,
      expected: ["coordinateSpace", "x", "y", "width", "height"]
    )
    guard object["coordinateSpace"] as? String == "target-logical",
      let x = finiteNumber(object["x"]), x >= 0,
      let y = finiteNumber(object["y"]), y >= 0,
      let width = finiteNumber(object["width"]), width > 0,
      let height = finiteNumber(object["height"]), height > 0
    else {
      throw PlatformProtocolError.invalidEnvelope(
        "Region geometry must be a positive target-logical rectangle."
      )
    }
    return CaptureGeometry(
      coordinateSpace: "target-logical",
      x: x,
      y: y,
      width: width,
      height: height
    )
  }

  private static func finiteNumber(_ value: Any?) -> Double? {
    guard let number = value as? NSNumber,
      CFGetTypeID(number) != CFBooleanGetTypeID(),
      number.doubleValue.isFinite
    else {
      return nil
    }
    return number.doubleValue
  }

  private static func requireExactKeys(
    _ object: [String: Any],
    expected: Set<String>
  ) throws {
    let actual = Set(object.keys)
    guard actual == expected else {
      throw PlatformProtocolError.invalidEnvelope("Request contains missing or unknown fields.")
    }
  }
}

public enum PlatformResponseEncoder {
  public static func encodeLine(_ response: PlatformResponse) throws -> String {
    let encoder = JSONEncoder()
    encoder.outputFormatting = [.sortedKeys, .withoutEscapingSlashes]
    let data = try encoder.encode(response)
    guard let line = String(data: data, encoding: .utf8) else {
      throw PlatformProtocolError.invalidJSON
    }
    return line
  }
}

public struct HostDiagnostic: Encodable, Equatable, Sendable {
  public let level: String
  public let event: String
  public let requestID: String
  public let code: HostFailureCode
  public let message: String
  public let retryable: Bool

  public init(requestID: String, event: String, failure: HostFailure) {
    self.level = "error"
    self.event = event
    self.requestID = requestID
    self.code = failure.code
    self.message = failure.message
    self.retryable = failure.retryable
  }
}

public enum HostDiagnosticEncoder {
  public static func encodeLine(_ diagnostic: HostDiagnostic) throws -> String {
    let encoder = JSONEncoder()
    encoder.outputFormatting = [.sortedKeys, .withoutEscapingSlashes]
    let data = try encoder.encode(diagnostic)
    guard let line = String(data: data, encoding: .utf8) else {
      throw PlatformProtocolError.invalidJSON
    }
    return line
  }
}

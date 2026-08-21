import Foundation

public let platformContractVersion = 1

public enum HostFailureCode: String, Codable, Sendable {
  case hostUnavailable = "host-unavailable"
  case permissionDenied = "permission-denied"
  case captureUnavailable = "capture-unavailable"
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

public struct CaptureParameters: Codable, Equatable, Sendable {
  public let mode: CaptureMode
  public let delivery: OutputDelivery
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
  public let hdrCapture: String
  public let outputProfiles: [String]
}

public struct CaptureArtifact: Codable, Equatable, Sendable {
  public let profile: String
  public let delivery: OutputDelivery
  public let filePath: String?
}

public struct CaptureResult: Codable, Equatable, Sendable {
  public let status: String
  public let sourceDynamicRange: String?
  public let artifact: CaptureArtifact?
  public let failure: HostFailure?

  public static func success(
    dynamicRange: String,
    delivery: OutputDelivery,
    filePath: String
  ) -> CaptureResult {
    CaptureResult(
      status: "success",
      sourceDynamicRange: dynamicRange,
      artifact: CaptureArtifact(
        profile: "srgb-visual-match",
        delivery: delivery,
        filePath: filePath
      ),
      failure: nil
    )
  }

  public static func failed(_ failure: HostFailure) -> CaptureResult {
    CaptureResult(
      status: "failed",
      sourceDynamicRange: nil,
      artifact: nil,
      failure: failure
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
      throw PlatformProtocolError.invalidEnvelope("Protocol version must be 1.")
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
      try requireExactKeys(parameters, expected: ["mode", "delivery"])
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
      return PlatformRequest(
        version: version,
        id: id,
        method: method,
        capture: CaptureParameters(mode: mode, delivery: delivery)
      )
    }
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

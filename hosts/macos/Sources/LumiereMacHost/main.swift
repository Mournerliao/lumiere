import Foundation
import LumiereMacHostCore

@main
enum LumiereMacHostMain {
  static func main() async {
    let service = MacCaptureService()

    while let line = readLine(strippingNewline: true) {
      let response: PlatformResponse
      do {
        let request = try PlatformRequestDecoder.decode(line: line)
        response = await service.response(for: request)
      } catch {
        let requestID = extractRequestID(from: line) ?? "invalid-request"
        response = .failure(
          id: requestID,
          error: HostFailure(
            code: .invalidRequest,
            message: String(describing: error),
            retryable: false
          )
        )
        writeDiagnostic(level: "warning", event: "invalid-request", error: error)
      }

      do {
        print(try PlatformResponseEncoder.encodeLine(response))
        fflush(stdout)
        writeFailureDiagnostic(for: response)
      } catch {
        writeDiagnostic(level: "error", event: "response-encoding-failed", error: error)
      }
    }
  }

  private static func writeFailureDiagnostic(for response: PlatformResponse) {
    let failure: HostFailure?
    let event: String
    if let responseFailure = response.error {
      failure = responseFailure
      event = "request-failed"
    } else if case .capture(let captureResult)? = response.result,
      let captureFailure = captureResult.failure
    {
      failure = captureFailure
      event = "capture-failed"
    } else if case .capture(let captureResult)? = response.result,
      let deliveryFailure = captureResult.deliveries?.compactMap(\.failure).first
    {
      failure = deliveryFailure
      event = "delivery-failed"
    } else {
      return
    }

    guard let failure else {
      return
    }
    do {
      let diagnostic = HostDiagnostic(
        requestID: response.id,
        event: event,
        failure: failure
      )
      var line = try HostDiagnosticEncoder.encodeLine(diagnostic)
      line.append("\n")
      FileHandle.standardError.write(Data(line.utf8))
    } catch {
      writeDiagnostic(level: "error", event: "diagnostic-encoding-failed", error: error)
    }
  }

  private static func extractRequestID(from line: String) -> String? {
    guard let data = line.data(using: .utf8),
      let object = try? JSONSerialization.jsonObject(with: data),
      let envelope = object as? [String: Any]
    else {
      return nil
    }
    return envelope["id"] as? String
  }

  private static func writeDiagnostic(level: String, event: String, error: Error) {
    let diagnostic: [String: String] = [
      "level": level,
      "event": event,
      "message": String(describing: error),
    ]
    guard let data = try? JSONSerialization.data(withJSONObject: diagnostic),
      var line = String(data: data, encoding: .utf8)
    else {
      return
    }
    line.append("\n")
    FileHandle.standardError.write(Data(line.utf8))
  }
}

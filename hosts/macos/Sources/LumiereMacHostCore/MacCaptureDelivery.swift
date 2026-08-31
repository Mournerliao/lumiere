import AppKit
import Foundation

struct MacCaptureDeliveryAdapters: Sendable {
  let writeClipboard: @Sendable (Data) async throws -> Void
  let writeFolder: @Sendable (Data, String?) async throws -> String

  static let live = MacCaptureDeliveryAdapters(
    writeClipboard: { pngData in
      try await MainActor.run {
        let pasteboard = NSPasteboard.general
        pasteboard.clearContents()
        guard pasteboard.setData(pngData, forType: .png) else {
          throw MacCaptureDeliveryError.clipboardWriteFailed
        }
      }
    },
    writeFolder: { pngData, saveDirectory in
      let fileURL = try MacCaptureFolder.outputURL(saveDirectory: saveDirectory)
      try pngData.write(to: fileURL, options: .atomic)
      return fileURL.path
    }
  )
}

enum MacCaptureDelivery {
  static func deliver(
    _ visualMatchPNG: Data,
    to delivery: OutputDelivery,
    saveDirectory: String? = nil,
    using adapters: MacCaptureDeliveryAdapters = .live
  ) async -> [DeliveryResult] {
    var results: [DeliveryResult] = []
    for target in targets(for: delivery) {
      do {
        switch target {
        case .clipboard:
          try await adapters.writeClipboard(visualMatchPNG)
          results.append(.clipboardSuccess())
        case .folder:
          let filePath = try await adapters.writeFolder(visualMatchPNG, saveDirectory)
          results.append(.folderSuccess(filePath: filePath))
        }
      } catch {
        results.append(
          .failed(
            target: target,
            failure: HostFailure(
              code: .deliveryFailed,
              message: failureMessage(for: target, error: error),
              retryable: true
            )
          )
        )
      }
    }
    return results
  }

  private static func targets(for delivery: OutputDelivery) -> [DeliveryTarget] {
    switch delivery {
    case .clipboard:
      return [.clipboard]
    case .folder:
      return [.folder]
    case .both:
      return [.clipboard, .folder]
    }
  }

  private static func failureMessage(for target: DeliveryTarget, error: Error) -> String {
    let nsError = error as NSError
    switch target {
    case .clipboard:
      return "The macOS clipboard delivery failed (\(nsError.domain):\(nsError.code))."
    case .folder:
      return "The macOS folder delivery failed (\(nsError.domain):\(nsError.code))."
    }
  }
}

private enum MacCaptureFolder {
  static func outputURL(saveDirectory: String?, now: Date = Date()) throws -> URL {
    let directory: URL
    if let saveDirectory {
      directory = URL(fileURLWithPath: saveDirectory, isDirectory: true)
    } else {
      let pictures = FileManager.default.urls(for: .picturesDirectory, in: .userDomainMask)[0]
      directory = pictures.appendingPathComponent("Lumiere", isDirectory: true)
    }
    try FileManager.default.createDirectory(
      at: directory,
      withIntermediateDirectories: true,
      attributes: nil
    )

    let formatter = DateFormatter()
    formatter.locale = Locale(identifier: "en_US_POSIX")
    formatter.calendar = Calendar(identifier: .gregorian)
    formatter.timeZone = .current
    formatter.dateFormat = "yyyy-MM-dd-HHmmss"
    let baseName = "Lumiere-\(formatter.string(from: now))"
    var candidate = directory.appendingPathComponent("\(baseName).png")
    var suffix = 2
    while FileManager.default.fileExists(atPath: candidate.path) {
      candidate = directory.appendingPathComponent("\(baseName)-\(suffix).png")
      suffix += 1
    }
    return candidate
  }
}

private enum MacCaptureDeliveryError: Error {
  case clipboardWriteFailed
}

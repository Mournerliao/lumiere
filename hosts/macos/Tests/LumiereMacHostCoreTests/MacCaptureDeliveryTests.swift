import Foundation
import Testing

@testable import LumiereMacHostCore

@Test
func deliversBothTargetsFromTheSameVisualMatchPNG() async throws {
  let expectedPNG = Data([0x89, 0x50, 0x4E, 0x47])
  let recorder = DeliveryRecorder()
  let adapters = MacCaptureDeliveryAdapters(
    writeClipboard: { data in
      await recorder.recordClipboard(data)
    },
    writeFolder: { data, saveDirectory in
      await recorder.recordFolder(data)
      await recorder.recordSaveDirectory(saveDirectory)
      return "/tmp/Lumiere-2026-08-25-120000.png"
    }
  )

  let results = await MacCaptureDelivery.deliver(
    expectedPNG,
    to: .both,
    saveDirectory: "/tmp/custom-captures",
    using: adapters
  )

  #expect(
    results == [
      .clipboardSuccess(),
      .folderSuccess(filePath: "/tmp/Lumiere-2026-08-25-120000.png"),
    ]
  )
  #expect(await recorder.clipboardData() == expectedPNG)
  #expect(await recorder.folderData() == expectedPNG)
  #expect(await recorder.saveDirectory() == "/tmp/custom-captures")
}

@Test
func preservesEachTargetFailureWithoutFailingTheConvertedCapture() async {
  let adapters = MacCaptureDeliveryAdapters(
    writeClipboard: { _ in throw DeliveryStubError.clipboard },
    writeFolder: { _, _ in throw DeliveryStubError.folder }
  )

  let results = await MacCaptureDelivery.deliver(Data([1, 2, 3]), to: .both, using: adapters)

  #expect(results.count == 2)
  #expect(results.map(\.target) == [.clipboard, .folder])
  #expect(results.allSatisfy { $0.status == "failed" })
  #expect(results.allSatisfy { $0.failure?.code == .deliveryFailed })
}

@Test
func createsAndWritesToCustomSaveDirectory() async throws {
  let directory = FileManager.default.temporaryDirectory
    .appendingPathComponent("lumiere-custom-folder-\(UUID().uuidString)", isDirectory: true)
  defer { try? FileManager.default.removeItem(at: directory) }
  let expectedData = Data([1, 2, 3, 4])

  let results = await MacCaptureDelivery.deliver(
    expectedData,
    to: .folder,
    saveDirectory: directory.path
  )

  let result = try #require(results.first)
  #expect(result.status == "success")
  let filePath = try #require(result.filePath)
  #expect(filePath.hasPrefix(directory.path))
  #expect(try Data(contentsOf: URL(fileURLWithPath: filePath)) == expectedData)
}

private actor DeliveryRecorder {
  private var clipboard: Data?
  private var folder: Data?
  private var directory: String?

  func recordClipboard(_ data: Data) {
    clipboard = data
  }

  func recordFolder(_ data: Data) {
    folder = data
  }

  func recordSaveDirectory(_ value: String?) {
    directory = value
  }

  func clipboardData() -> Data? {
    clipboard
  }

  func folderData() -> Data? {
    folder
  }

  func saveDirectory() -> String? {
    directory
  }
}

private enum DeliveryStubError: Error {
  case clipboard
  case folder
}

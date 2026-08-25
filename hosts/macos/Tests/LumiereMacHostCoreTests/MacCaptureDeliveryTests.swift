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
    writeFolder: { data in
      await recorder.recordFolder(data)
      return "/tmp/Lumiere-2026-08-25-120000.png"
    }
  )

  let results = await MacCaptureDelivery.deliver(expectedPNG, to: .both, using: adapters)

  #expect(
    results == [
      .clipboardSuccess(),
      .folderSuccess(filePath: "/tmp/Lumiere-2026-08-25-120000.png"),
    ]
  )
  #expect(await recorder.clipboardData() == expectedPNG)
  #expect(await recorder.folderData() == expectedPNG)
}

@Test
func preservesEachTargetFailureWithoutFailingTheConvertedCapture() async {
  let adapters = MacCaptureDeliveryAdapters(
    writeClipboard: { _ in throw DeliveryStubError.clipboard },
    writeFolder: { _ in throw DeliveryStubError.folder }
  )

  let results = await MacCaptureDelivery.deliver(Data([1, 2, 3]), to: .both, using: adapters)

  #expect(results.count == 2)
  #expect(results.map(\.target) == [.clipboard, .folder])
  #expect(results.allSatisfy { $0.status == "failed" })
  #expect(results.allSatisfy { $0.failure?.code == .deliveryFailed })
}

private actor DeliveryRecorder {
  private var clipboard: Data?
  private var folder: Data?

  func recordClipboard(_ data: Data) {
    clipboard = data
  }

  func recordFolder(_ data: Data) {
    folder = data
  }

  func clipboardData() -> Data? {
    clipboard
  }

  func folderData() -> Data? {
    folder
  }
}

private enum DeliveryStubError: Error {
  case clipboard
  case folder
}

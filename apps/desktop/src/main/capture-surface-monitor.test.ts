import { afterEach, describe, expect, it, vi } from 'vitest'
import { CaptureSurfaceMonitor } from './capture-surface-monitor'
import type { CaptureSurfaceSnapshot } from '../shared/capture-command'

const readySnapshot: CaptureSurfaceSnapshot = {
  platform: 'macos',
  hostAvailable: true,
  captureModes: ['region', 'display'],
  hdrStatus: 'ready',
  output: {
    delivery: 'both',
    label: 'Clipboard and folder',
    location: '~/Pictures/Lumiere',
  },
}

const unavailableSnapshot: CaptureSurfaceSnapshot = {
  ...readySnapshot,
  hdrStatus: 'unavailable',
}

afterEach(() => {
  vi.useRealTimers()
})

describe('CaptureSurfaceMonitor', () => {
  it('loads once on start and does not query the Host while the pointer stays on one display', async () => {
    vi.useFakeTimers()
    const readSnapshot = vi.fn(() => Promise.resolve(readySnapshot))
    const publish = vi.fn()
    const monitor = new CaptureSurfaceMonitor({
      readTargetId: () => 1,
      readSnapshot,
      publish,
    })

    monitor.start()
    await monitor.getSnapshot()
    await vi.advanceTimersByTimeAsync(1_000)

    expect(readSnapshot).toHaveBeenCalledTimes(1)
    expect(publish).toHaveBeenCalledOnce()
    monitor.dispose()
  })

  it('refreshes once when the pointer crosses to another display', async () => {
    vi.useFakeTimers()
    let targetId = 1
    const readSnapshot = vi
      .fn<() => Promise<CaptureSurfaceSnapshot>>()
      .mockResolvedValueOnce(readySnapshot)
      .mockResolvedValueOnce(unavailableSnapshot)
    const publish = vi.fn()
    const monitor = new CaptureSurfaceMonitor({
      readTargetId: () => targetId,
      readSnapshot,
      publish,
    })

    monitor.start()
    await monitor.getSnapshot()
    targetId = 2
    await vi.advanceTimersByTimeAsync(250)

    expect(readSnapshot).toHaveBeenCalledTimes(2)
    expect(publish).toHaveBeenLastCalledWith(unavailableSnapshot)
    monitor.dispose()
  })

  it('discards an obsolete response and publishes only the latest stable target', async () => {
    vi.useFakeTimers()
    let targetId = 1
    let resolveFirst: ((snapshot: CaptureSurfaceSnapshot) => void) | undefined
    const firstSnapshot = new Promise<CaptureSurfaceSnapshot>((resolve) => {
      resolveFirst = resolve
    })
    const readSnapshot = vi
      .fn<() => Promise<CaptureSurfaceSnapshot>>()
      .mockReturnValueOnce(firstSnapshot)
      .mockResolvedValueOnce(unavailableSnapshot)
    const publish = vi.fn()
    const monitor = new CaptureSurfaceMonitor({
      readTargetId: () => targetId,
      readSnapshot,
      publish,
    })

    monitor.start()
    targetId = 2
    await vi.advanceTimersByTimeAsync(250)
    resolveFirst?.(readySnapshot)
    await firstSnapshot
    await Promise.resolve()

    expect(readSnapshot).toHaveBeenCalledTimes(2)
    expect(publish).toHaveBeenCalledTimes(1)
    expect(publish).toHaveBeenCalledWith(unavailableSnapshot)
    monitor.dispose()
  })

  it('settles a rapid display round trip on the final target', async () => {
    vi.useFakeTimers()
    let targetId = 1
    let resolveSecond: ((snapshot: CaptureSurfaceSnapshot) => void) | undefined
    const secondSnapshot = new Promise<CaptureSurfaceSnapshot>((resolve) => {
      resolveSecond = resolve
    })
    const readSnapshot = vi
      .fn<() => Promise<CaptureSurfaceSnapshot>>()
      .mockResolvedValueOnce(readySnapshot)
      .mockReturnValueOnce(secondSnapshot)
      .mockResolvedValueOnce(readySnapshot)
    const publish = vi.fn()
    const monitor = new CaptureSurfaceMonitor({
      readTargetId: () => targetId,
      readSnapshot,
      publish,
    })

    monitor.start()
    await monitor.getSnapshot()
    publish.mockClear()
    targetId = 2
    await vi.advanceTimersByTimeAsync(250)
    targetId = 1
    resolveSecond?.(unavailableSnapshot)
    await secondSnapshot
    await Promise.resolve()

    expect(readSnapshot).toHaveBeenCalledTimes(3)
    expect(publish).toHaveBeenCalledTimes(1)
    expect(publish).toHaveBeenCalledWith(readySnapshot)
    monitor.dispose()
  })

  it('can force a same-display refresh for focus and display metric changes', async () => {
    const readSnapshot = vi
      .fn<() => Promise<CaptureSurfaceSnapshot>>()
      .mockResolvedValueOnce(readySnapshot)
      .mockResolvedValueOnce(unavailableSnapshot)
    const publish = vi.fn()
    const monitor = new CaptureSurfaceMonitor({
      readTargetId: () => 1,
      readSnapshot,
      publish,
    })

    monitor.start()
    await monitor.getSnapshot()
    await monitor.refresh()

    expect(readSnapshot).toHaveBeenCalledTimes(2)
    expect(publish).toHaveBeenLastCalledWith(unavailableSnapshot)
    monitor.dispose()
  })

  it('rechecks the same display when an invalidation arrives during an in-flight query', async () => {
    let resolveFirst: ((snapshot: CaptureSurfaceSnapshot) => void) | undefined
    const firstSnapshot = new Promise<CaptureSurfaceSnapshot>((resolve) => {
      resolveFirst = resolve
    })
    const readSnapshot = vi
      .fn<() => Promise<CaptureSurfaceSnapshot>>()
      .mockReturnValueOnce(firstSnapshot)
      .mockResolvedValueOnce(unavailableSnapshot)
    const publish = vi.fn()
    const monitor = new CaptureSurfaceMonitor({
      readTargetId: () => 1,
      readSnapshot,
      publish,
    })

    monitor.start()
    const invalidation = monitor.invalidate()
    resolveFirst?.(readySnapshot)
    await invalidation

    expect(readSnapshot).toHaveBeenCalledTimes(2)
    expect(publish).toHaveBeenCalledTimes(1)
    expect(publish).toHaveBeenLastCalledWith(unavailableSnapshot)
    monitor.dispose()
  })

  it('pauses target observation while stopped and refreshes when restarted', async () => {
    vi.useFakeTimers()
    let targetId = 1
    const readSnapshot = vi.fn(() => Promise.resolve(readySnapshot))
    const monitor = new CaptureSurfaceMonitor({
      readTargetId: () => targetId,
      readSnapshot,
      publish: vi.fn(),
    })

    monitor.start()
    await monitor.getSnapshot()
    monitor.stop()
    targetId = 2
    await vi.advanceTimersByTimeAsync(1_000)
    expect(readSnapshot).toHaveBeenCalledTimes(1)

    monitor.start()
    await monitor.getSnapshot()
    expect(readSnapshot).toHaveBeenCalledTimes(2)
    monitor.dispose()
  })

  it('keeps the last snapshot and retries transient refresh failures with backoff', async () => {
    vi.useFakeTimers()
    const readSnapshot = vi
      .fn<() => Promise<CaptureSurfaceSnapshot>>()
      .mockResolvedValueOnce(readySnapshot)
      .mockRejectedValueOnce(new Error('temporary failure'))
      .mockResolvedValueOnce(unavailableSnapshot)
    const publish = vi.fn()
    const monitor = new CaptureSurfaceMonitor({
      readTargetId: () => 1,
      readSnapshot,
      publish,
    })

    monitor.start()
    await monitor.getSnapshot()
    await monitor.refresh()
    expect(publish).toHaveBeenLastCalledWith(readySnapshot)

    await vi.advanceTimersByTimeAsync(999)
    expect(readSnapshot).toHaveBeenCalledTimes(2)
    await vi.advanceTimersByTimeAsync(1)
    expect(readSnapshot).toHaveBeenCalledTimes(3)
    expect(publish).toHaveBeenLastCalledWith(unavailableSnapshot)
    monitor.dispose()
  })

  it('rejects the initial snapshot when no usable state has loaded', async () => {
    const monitor = new CaptureSurfaceMonitor({
      readTargetId: () => 1,
      readSnapshot: () => Promise.reject(new Error('initial failure')),
      publish: vi.fn(),
    })

    await expect(monitor.getSnapshot()).rejects.toThrow('initial failure')
    monitor.dispose()
  })

  it('does not publish an in-flight result after monitoring stops', async () => {
    let resolveSnapshot: ((snapshot: CaptureSurfaceSnapshot) => void) | undefined
    const pendingSnapshot = new Promise<CaptureSurfaceSnapshot>((resolve) => {
      resolveSnapshot = resolve
    })
    const publish = vi.fn()
    const monitor = new CaptureSurfaceMonitor({
      readTargetId: () => 1,
      readSnapshot: () => pendingSnapshot,
      publish,
    })

    monitor.start()
    monitor.stop()
    resolveSnapshot?.(readySnapshot)
    await pendingSnapshot
    await vi.waitFor(() => {
      expect(publish).not.toHaveBeenCalled()
    })
    monitor.dispose()
  })
})

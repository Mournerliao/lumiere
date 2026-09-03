import type { CaptureSurfaceSnapshot } from '../shared/capture-command'

const DEFAULT_POLL_INTERVAL_MILLISECONDS = 250
const DEFAULT_RETRY_DELAY_MILLISECONDS = 1_000

interface CaptureSurfaceMonitorOptions {
  readTargetId(): number
  readSnapshot(): Promise<CaptureSurfaceSnapshot>
  publish(snapshot: CaptureSurfaceSnapshot): void
  pollIntervalMilliseconds?: number
  retryDelayMilliseconds?: number
}

export class CaptureSurfaceMonitor {
  private readonly pollIntervalMilliseconds: number
  private readonly retryDelayMilliseconds: number
  private active = false
  private disposed = false
  private observedTargetId: number | null = null
  private resolvedTargetId: number | null = null
  private snapshot: CaptureSurfaceSnapshot | null = null
  private refreshRequested = false
  private invalidationVersion = 0
  private refreshLoop: Promise<void> | null = null
  private refreshingTargetId: number | null = null
  private lastError: unknown = null
  private pollTimer: NodeJS.Timeout | null = null
  private retryTimer: NodeJS.Timeout | null = null

  public constructor(private readonly options: CaptureSurfaceMonitorOptions) {
    this.pollIntervalMilliseconds =
      options.pollIntervalMilliseconds ?? DEFAULT_POLL_INTERVAL_MILLISECONDS
    this.retryDelayMilliseconds = options.retryDelayMilliseconds ?? DEFAULT_RETRY_DELAY_MILLISECONDS
  }

  public start(): void {
    if (this.disposed || this.active) return
    this.active = true
    this.observedTargetId = null
    this.observeTarget()
    this.pollTimer = setInterval(() => {
      this.observeTarget()
    }, this.pollIntervalMilliseconds)
  }

  public stop(): void {
    this.active = false
    if (this.pollTimer) {
      clearInterval(this.pollTimer)
      this.pollTimer = null
    }
    this.clearRetry()
  }

  public dispose(): void {
    this.disposed = true
    this.stop()
  }

  public async getSnapshot(): Promise<CaptureSurfaceSnapshot> {
    const targetId = this.options.readTargetId()
    this.observedTargetId = targetId
    if (this.snapshot && this.resolvedTargetId === targetId) {
      return this.snapshot
    }

    await this.requestRefresh(false)
    if (this.snapshot && this.resolvedTargetId === this.options.readTargetId()) {
      return this.snapshot
    }
    if (this.lastError instanceof Error) {
      throw this.lastError
    }
    throw new Error('The capture surface snapshot is unavailable.')
  }

  public refresh(): Promise<void> {
    if (this.disposed) return Promise.resolve()
    this.resolvedTargetId = null
    return this.requestRefresh(false)
  }

  public invalidate(): Promise<void> {
    if (this.disposed) return Promise.resolve()
    this.invalidationVersion += 1
    this.resolvedTargetId = null
    return this.requestRefresh(true)
  }

  private observeTarget(): void {
    if (!this.active || this.disposed) return
    const targetId = this.options.readTargetId()
    if (targetId === this.observedTargetId && this.snapshot) return
    this.observedTargetId = targetId
    this.resolvedTargetId = null
    void this.requestRefresh(false)
  }

  private requestRefresh(force: boolean): Promise<void> {
    if (this.disposed) return Promise.resolve()
    const targetId = this.options.readTargetId()
    if (this.refreshLoop && this.refreshingTargetId === targetId) {
      if (force) {
        this.refreshRequested = true
      }
      return this.refreshLoop
    }
    this.refreshRequested = true
    this.refreshLoop ??= this.runRefreshLoop().finally(() => {
      this.refreshLoop = null
    })
    return this.refreshLoop
  }

  private async runRefreshLoop(): Promise<void> {
    while (this.refreshRequested && !this.disposed) {
      this.refreshRequested = false
      this.clearRetry()
      const requestedTargetId = this.options.readTargetId()
      const requestedInvalidationVersion = this.invalidationVersion
      this.observedTargetId = requestedTargetId
      this.refreshingTargetId = requestedTargetId

      try {
        const nextSnapshot = await this.options.readSnapshot()
        const currentTargetId = this.options.readTargetId()
        if (
          currentTargetId !== requestedTargetId ||
          requestedInvalidationVersion !== this.invalidationVersion
        ) {
          this.observedTargetId = currentTargetId
          this.resolvedTargetId = null
          this.refreshRequested = true
          continue
        }

        this.snapshot = nextSnapshot
        this.resolvedTargetId = requestedTargetId
        this.lastError = null
        if (this.active) {
          this.options.publish(nextSnapshot)
        }
      } catch (error) {
        this.lastError = error
        this.resolvedTargetId = null
        this.scheduleRetry()
      } finally {
        this.refreshingTargetId = null
      }
    }
  }

  private scheduleRetry(): void {
    if (!this.active || this.disposed || this.retryTimer) return
    this.retryTimer = setTimeout(() => {
      this.retryTimer = null
      void this.requestRefresh(false)
    }, this.retryDelayMilliseconds)
  }

  private clearRetry(): void {
    if (!this.retryTimer) return
    clearTimeout(this.retryTimer)
    this.retryTimer = null
  }
}

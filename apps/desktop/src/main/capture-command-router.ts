import type {
  CaptureCommandResult,
  CaptureNotice,
  CaptureSurfaceSnapshot,
  ProductHdrStatus,
} from '../shared/capture-command'
import type {
  CaptureMode,
  LumierePlatform,
  OutputDelivery,
  DeliveryResult,
  CaptureGeometry,
  CaptureTarget,
  PlatformCapabilities,
  PlatformFailure,
  PlatformHost,
} from '../shared/platform-contract'
import { deliveryTargetsFor } from '../shared/platform-contract'

export interface CapturePreferences {
  delivery: OutputDelivery
}

export interface CapturePreferencesReader {
  getCapturePreferences(): CapturePreferences
}

export type RegionCapturePreparation =
  { status: 'ready'; target: CaptureTarget } | { status: 'failed'; result: CaptureCommandResult }

const defaultPreferences: CapturePreferencesReader = {
  getCapturePreferences: () => ({ delivery: 'both' }),
}

export class CaptureCommandRouter {
  private captureInFlight = false

  public constructor(
    private readonly platform: LumierePlatform,
    private readonly host: PlatformHost,
    private readonly preferences: CapturePreferencesReader = defaultPreferences,
  ) {}

  public async getSurfaceSnapshot(): Promise<CaptureSurfaceSnapshot> {
    const { delivery } = this.preferences.getCapturePreferences()
    return projectSurfaceSnapshot(this.platform, delivery, await this.host.getCapabilities())
  }

  public async captureDisplay(): Promise<CaptureCommandResult> {
    if (this.captureInFlight) {
      return failedResult({
        tone: 'caution',
        title: 'A capture is already in progress',
        detail: 'Wait for it to finish, then try again.',
      })
    }

    this.captureInFlight = true
    try {
      const { delivery } = this.preferences.getCapturePreferences()
      const capabilities = await this.host.getCapabilities()
      const unavailable = captureUnavailableNotice(capabilities, 'display', delivery)
      if (unavailable) {
        return failedResult(unavailable)
      }

      const result = await this.host.capture({
        mode: 'display',
        delivery,
      })

      if (result.status === 'cancelled') {
        return { status: 'cancelled', feedback: 'Capture cancelled' }
      }
      if (result.status === 'failed') {
        return failedResult(noticeForFailure(result.failure))
      }

      return projectDeliveryResult(result.deliveries)
    } catch {
      return failedResult({
        tone: 'critical',
        title: 'Capture failed',
        detail: 'Try again. Restart Lumiere if the issue continues.',
      })
    } finally {
      this.captureInFlight = false
    }
  }

  public async beginRegionCapture(): Promise<RegionCapturePreparation> {
    if (this.captureInFlight) {
      return { status: 'failed', result: captureAlreadyInProgress() }
    }

    this.captureInFlight = true
    try {
      const { delivery } = this.preferences.getCapturePreferences()
      const capabilities = await this.host.getCapabilities()
      const unavailable = captureUnavailableNotice(capabilities, 'region', delivery)
      if (unavailable) {
        this.captureInFlight = false
        return { status: 'failed', result: failedResult(unavailable) }
      }
      if (!capabilities.activeTarget) {
        this.captureInFlight = false
        return {
          status: 'failed',
          result: failedResult({
            tone: 'caution',
            title: 'Region capture unavailable',
            detail: 'Move the pointer to a display and try again.',
          }),
        }
      }
      return { status: 'ready', target: capabilities.activeTarget }
    } catch {
      this.captureInFlight = false
      return { status: 'failed', result: captureFailed() }
    }
  }

  public async completeRegionCapture(
    target: CaptureTarget,
    geometry: CaptureGeometry,
  ): Promise<CaptureCommandResult> {
    if (!this.captureInFlight) {
      return captureFailed()
    }

    try {
      const { delivery } = this.preferences.getCapturePreferences()
      const result = await this.host.capture({
        mode: 'region',
        delivery,
        targetId: target.id,
        geometry,
      })
      return projectCaptureResult(result)
    } catch {
      return captureFailed()
    } finally {
      this.captureInFlight = false
    }
  }

  public cancelRegionCapture(): void {
    this.captureInFlight = false
  }
}

function projectCaptureResult(
  result: Awaited<ReturnType<PlatformHost['capture']>>,
): CaptureCommandResult {
  if (result.status === 'cancelled') {
    return { status: 'cancelled', feedback: 'Capture cancelled' }
  }
  if (result.status === 'failed') {
    return failedResult(noticeForFailure(result.failure))
  }
  return projectDeliveryResult(result.deliveries)
}

function captureAlreadyInProgress(): CaptureCommandResult {
  return failedResult({
    tone: 'caution',
    title: 'A capture is already in progress',
    detail: 'Wait for it to finish, then try again.',
  })
}

function captureFailed(): CaptureCommandResult {
  return failedResult({
    tone: 'critical',
    title: 'Capture failed',
    detail: 'Try again. Restart Lumiere if the issue continues.',
  })
}

function projectSurfaceSnapshot(
  platform: LumierePlatform,
  delivery: OutputDelivery,
  capabilities: PlatformCapabilities,
): CaptureSurfaceSnapshot {
  const hostAvailable = capabilities.hostStatus === 'available'
  return {
    platform,
    hostAvailable,
    captureModes: capabilities.captureModes,
    hdrStatus: projectHdrStatus(capabilities.hdrCapture),
    output: outputSummary(platform, delivery),
    ...(!hostAvailable
      ? {
          blockingNotice: {
            tone: 'critical' as const,
            title: 'Native capture host is unavailable',
            detail: 'Retry, or restart Lumiere if it does not come back.',
          },
        }
      : {}),
  }
}

function projectHdrStatus(value: PlatformCapabilities['hdrCapture']): ProductHdrStatus {
  return value === 'supported' ? 'ready' : value
}

function outputSummary(
  platform: LumierePlatform,
  delivery: OutputDelivery,
): CaptureSurfaceSnapshot['output'] {
  if (delivery !== 'folder') {
    return {
      delivery,
      label: delivery === 'clipboard' ? 'Clipboard' : 'Clipboard and folder',
      location: delivery === 'clipboard' ? 'Ready for paste' : captureFolder(platform),
    }
  }

  return {
    delivery,
    label: 'Folder',
    location: captureFolder(platform),
  }
}

function captureFolder(platform: LumierePlatform): string {
  return platform === 'macos' ? '~/Pictures/Lumiere' : '%USERPROFILE%\\Pictures\\Lumiere'
}

function captureUnavailableNotice(
  capabilities: PlatformCapabilities,
  mode: CaptureMode,
  delivery: OutputDelivery,
): CaptureNotice | null {
  if (capabilities.hostStatus !== 'available') {
    return {
      tone: 'critical',
      title: 'Native capture host is unavailable',
      detail: 'Retry, or restart Lumiere if it does not come back.',
    }
  }
  if (!capabilities.captureModes.includes(mode)) {
    return {
      tone: 'caution',
      title:
        mode === 'region' ? 'Region capture is not available yet' : 'Display capture unavailable',
      detail: 'Choose an available capture action and try again.',
    }
  }
  if (
    !deliveryTargetsFor(delivery).every((target) => capabilities.deliveryTargets.includes(target))
  ) {
    return {
      tone: 'caution',
      title: 'Current output is unavailable',
      detail: 'Choose an available output destination and try again.',
    }
  }
  return null
}

function noticeForFailure(failure: PlatformFailure): CaptureNotice {
  switch (failure.code) {
    case 'permission-denied':
      return {
        tone: 'critical',
        title: 'Screen recording permission is required',
        detail: 'Allow screen recording in System Settings, then try again.',
      }
    case 'host-unavailable':
      return {
        tone: 'critical',
        title: 'Native capture host is unavailable',
        detail: 'Retry, or restart Lumiere if it does not come back.',
      }
    case 'capture-unavailable':
      return {
        tone: 'caution',
        title: 'Capture failed',
        detail: 'The display may have changed. Try again.',
      }
    case 'delivery-unavailable':
    case 'delivery-failed':
      return {
        tone: 'caution',
        title: 'Couldn’t deliver capture',
        detail: 'Check the output destination, then try again.',
      }
    case 'invalid-request':
    case 'unexpected-failure':
      return {
        tone: 'critical',
        title: 'Capture failed',
        detail: 'Try again. Restart Lumiere if the issue continues.',
      }
  }
}

function projectDeliveryResult(deliveries: readonly DeliveryResult[]): CaptureCommandResult {
  const clipboard = deliveries.find(({ target }) => target === 'clipboard')
  const folder = deliveries.find(({ target }) => target === 'folder')
  const clipboardSucceeded = clipboard?.status === 'success'
  const folderSucceeded = folder?.status === 'success'
  const filePath =
    folder?.target === 'folder' && folder.status === 'success' ? folder.filePath : undefined

  if (deliveries.every(({ status }) => status === 'success')) {
    const feedback =
      clipboardSucceeded && folderSucceeded
        ? 'Copied and saved to “Lumiere”'
        : clipboardSucceeded
          ? 'Copied to clipboard'
          : 'Saved to “Lumiere”'
    return { status: 'success', feedback, ...(filePath ? { filePath } : {}) }
  }

  if (deliveries.some(({ status }) => status === 'success')) {
    const feedback = clipboardSucceeded
      ? 'Copied to clipboard, but couldn’t save the file'
      : 'Saved the file, but couldn’t copy it'
    return {
      status: 'partial',
      feedback,
      notice: {
        tone: 'caution',
        title: feedback,
        detail: 'Check the failed output destination, then try again.',
      },
      ...(filePath ? { filePath } : {}),
    }
  }

  return failedResult({
    tone: 'caution',
    title: 'Couldn’t deliver capture',
    detail: 'Check the output destination, then try again.',
  })
}

function failedResult(notice: CaptureNotice): CaptureCommandResult {
  return {
    status: 'failed',
    feedback: notice.title,
    notice,
  }
}

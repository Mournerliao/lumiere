import type {
  CaptureGeometry,
  CaptureMode,
  CaptureTarget,
  LumierePlatform,
  OutputDelivery,
} from './platform-contract'
import type { LumiereSettingsApi } from './settings-command'

export const captureCommandChannels = {
  captureDisplay: 'capture:display',
  captureRegion: 'capture:region',
  completed: 'capture:completed',
  getSurfaceSnapshot: 'capture:get-surface-snapshot',
  getRegionOverlaySnapshot: 'region-overlay:get-snapshot',
  cancelRegionOverlay: 'region-overlay:cancel',
  submitRegionSelection: 'region-overlay:submit-selection',
} as const

export interface RegionOverlaySnapshot {
  targetSize: CaptureTarget['logicalSize']
}

export type ProductHdrStatus = 'ready' | 'unavailable' | 'unvalidated'

export interface CaptureNotice {
  tone: 'critical' | 'caution'
  title: string
  detail: string
}

export interface CaptureOutputSummary {
  delivery: OutputDelivery
  label: string
  location: string
}

export interface CaptureSurfaceSnapshot {
  platform: LumierePlatform
  hostAvailable: boolean
  captureModes: readonly CaptureMode[]
  hdrStatus: ProductHdrStatus
  output: CaptureOutputSummary
  blockingNotice?: CaptureNotice
  advisoryNotice?: CaptureNotice
}

export type CaptureCommandResult =
  | {
      status: 'success'
      feedback: string
      filePath?: string
    }
  | {
      status: 'partial'
      feedback: string
      notice: CaptureNotice
      filePath?: string
    }
  | {
      status: 'cancelled'
      feedback: string
    }
  | {
      status: 'failed'
      feedback: string
      notice: CaptureNotice
    }

export interface LumiereRendererApi extends LumiereSettingsApi {
  readonly platform: LumierePlatform
  getCaptureSurfaceSnapshot(): Promise<CaptureSurfaceSnapshot>
  captureDisplay(): Promise<CaptureCommandResult>
  captureRegion(): Promise<CaptureCommandResult>
  onCaptureCompleted(listener: (result: CaptureCommandResult) => void): () => void
  getRegionOverlaySnapshot(): Promise<RegionOverlaySnapshot>
  cancelRegionOverlay(): void
  submitRegionSelection(geometry: CaptureGeometry): void
}

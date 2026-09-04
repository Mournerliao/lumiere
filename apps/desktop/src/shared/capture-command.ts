import type {
  CaptureGeometry,
  CaptureMode,
  LogicalSize,
  LumierePlatform,
  OutputDelivery,
} from './platform-contract'
import type { LumiereSettingsApi } from './settings-command'

export const captureCommandChannels = {
  captureDisplay: 'capture:display',
  captureRegion: 'capture:region',
  completed: 'capture:completed',
  getSurfaceSnapshot: 'capture:get-surface-snapshot',
  surfaceChanged: 'capture:surface-changed',
  regionOverlayHostReady: 'region-overlay:host-ready',
  regionOverlayActivated: 'region-overlay:activated',
  regionOverlayReset: 'region-overlay:reset',
  regionOverlayReady: 'region-overlay:ready',
  cancelRegionOverlay: 'region-overlay:cancel',
  submitRegionSelection: 'region-overlay:submit-selection',
} as const

export interface RegionOverlaySnapshot {
  generation: number
  targetSize: LogicalSize
  previewPixelSize: LogicalSize
  previewUrl: string
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
  onCaptureSurfaceChanged(listener: (snapshot: CaptureSurfaceSnapshot) => void): () => void
  captureDisplay(): Promise<CaptureCommandResult>
  captureRegion(): Promise<CaptureCommandResult>
  onCaptureCompleted(listener: (result: CaptureCommandResult) => void): () => void
  onRegionOverlayActivated(listener: (snapshot: RegionOverlaySnapshot) => void): () => void
  onRegionOverlayReset(listener: () => void): () => void
  regionOverlayHostReady(): void
  regionOverlayReady(generation: number): void
  cancelRegionOverlay(generation: number): void
  submitRegionSelection(generation: number, geometry: CaptureGeometry): void
}

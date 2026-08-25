import type { CaptureMode, LumierePlatform, OutputDelivery } from './platform-contract'

export const captureCommandChannels = {
  captureDisplay: 'capture:display',
  getSurfaceSnapshot: 'capture:get-surface-snapshot',
} as const

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
}

export type CaptureCommandResult =
  | {
      status: 'success'
      feedback: string
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

export interface LumiereRendererApi {
  readonly platform: LumierePlatform
  getCaptureSurfaceSnapshot(): Promise<CaptureSurfaceSnapshot>
  captureDisplay(): Promise<CaptureCommandResult>
}

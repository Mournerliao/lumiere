export const PLATFORM_CONTRACT_VERSION = 1 as const

export const platformChannels = {
  capture: 'platform:capture',
  getCapabilities: 'platform:get-capabilities',
} as const

export type LumierePlatform = 'macos' | 'windows'
export type CaptureMode = 'region' | 'display'
export type OutputDelivery = 'clipboard' | 'folder' | 'both'

export interface PlatformCapabilities {
  contractVersion: typeof PLATFORM_CONTRACT_VERSION
  platform: LumierePlatform
  hostStatus: 'available' | 'unavailable'
  captureModes: readonly CaptureMode[]
  hdrCapture: 'supported' | 'unavailable' | 'unvalidated'
  outputProfiles: readonly ['srgb-visual-match']
  unavailableReason?: PlatformFailure
}

export interface CaptureRequest {
  mode: CaptureMode
  delivery: OutputDelivery
}

export interface CaptureArtifact {
  profile: 'srgb-visual-match'
  delivery: OutputDelivery
  filePath?: string
}

export type CaptureResult =
  | {
      status: 'success'
      sourceDynamicRange: 'sdr' | 'hdr'
      artifact: CaptureArtifact
    }
  | {
      status: 'cancelled'
    }
  | {
      status: 'failed'
      failure: PlatformFailure
    }

export interface PlatformFailure {
  code:
    | 'host-unavailable'
    | 'permission-denied'
    | 'capture-unavailable'
    | 'invalid-request'
    | 'unexpected-failure'
  message: string
  retryable: boolean
}

export interface LumierePlatformApi {
  getCapabilities(): Promise<PlatformCapabilities>
  capture(request: CaptureRequest): Promise<CaptureResult>
}

export interface PlatformHost extends LumierePlatformApi {}

export type PlatformRequestEnvelope =
  | {
      version: typeof PLATFORM_CONTRACT_VERSION
      id: string
      method: 'getCapabilities'
      params: Record<string, never>
    }
  | {
      version: typeof PLATFORM_CONTRACT_VERSION
      id: string
      method: 'capture'
      params: CaptureRequest
    }

export type PlatformResponseEnvelope =
  | {
      version: typeof PLATFORM_CONTRACT_VERSION
      id: string
      result: PlatformCapabilities | CaptureResult
    }
  | {
      version: typeof PLATFORM_CONTRACT_VERSION
      id: string
      error: PlatformFailure
    }

const captureModes: readonly CaptureMode[] = ['region', 'display']
const outputDeliveries: readonly OutputDelivery[] = ['clipboard', 'folder', 'both']

export function parseCaptureRequest(value: unknown): CaptureRequest {
  if (!isRecord(value)) {
    throw new PlatformContractError('Capture request must be an object.')
  }

  if (!captureModes.includes(value.mode as CaptureMode)) {
    throw new PlatformContractError('Capture mode must be region or display.')
  }

  if (!outputDeliveries.includes(value.delivery as OutputDelivery)) {
    throw new PlatformContractError('Output delivery must be clipboard, folder, or both.')
  }

  return {
    mode: value.mode as CaptureMode,
    delivery: value.delivery as OutputDelivery,
  }
}

export class PlatformContractError extends Error {
  public constructor(message: string) {
    super(message)
    this.name = 'PlatformContractError'
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

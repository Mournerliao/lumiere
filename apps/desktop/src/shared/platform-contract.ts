export const PLATFORM_CONTRACT_VERSION = 2 as const

export const platformChannels = {
  capture: 'platform:capture',
  getCapabilities: 'platform:get-capabilities',
} as const

export type LumierePlatform = 'macos' | 'windows'
export type CaptureMode = 'region' | 'display'
export type OutputDelivery = 'clipboard' | 'folder' | 'both'
export type DeliveryTarget = 'clipboard' | 'folder'

export interface CaptureTarget {
  id: string
  logicalSize: {
    width: number
    height: number
  }
}

export interface PlatformCapabilities {
  contractVersion: typeof PLATFORM_CONTRACT_VERSION
  platform: LumierePlatform
  hostStatus: 'available' | 'unavailable'
  captureModes: readonly CaptureMode[]
  deliveryTargets: readonly DeliveryTarget[]
  hdrCapture: 'supported' | 'unavailable' | 'unvalidated'
  outputProfiles: readonly ['srgb-visual-match']
  activeTarget?: CaptureTarget
  unavailableReason?: PlatformFailure
}

export interface CaptureGeometry {
  coordinateSpace: 'target-logical'
  x: number
  y: number
  width: number
  height: number
}

export type CaptureRequest =
  | {
      mode: 'display'
      delivery: OutputDelivery
    }
  | {
      mode: 'region'
      delivery: OutputDelivery
      targetId: string
      geometry: CaptureGeometry
    }

export type DeliveryResult =
  | {
      target: 'clipboard'
      status: 'success'
    }
  | {
      target: 'folder'
      status: 'success'
      filePath: string
    }
  | {
      target: DeliveryTarget
      status: 'failed'
      failure: PlatformFailure
    }

export type CaptureResult =
  | {
      status: 'completed'
      sourceDynamicRange: 'sdr' | 'hdr'
      outputProfile: 'srgb-visual-match'
      deliveries: readonly DeliveryResult[]
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
    | 'delivery-unavailable'
    | 'delivery-failed'
    | 'invalid-request'
    | 'unexpected-failure'
  message: string
  retryable: boolean
}

export interface LumierePlatformApi {
  getCapabilities(): Promise<PlatformCapabilities>
  capture(request: CaptureRequest): Promise<CaptureResult>
}

export type PlatformHost = LumierePlatformApi

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

  if (value.mode === 'display') {
    requireExactKeys(value, ['mode', 'delivery'])
    return {
      mode: 'display',
      delivery: value.delivery as OutputDelivery,
    }
  }

  requireExactKeys(value, ['mode', 'delivery', 'targetId', 'geometry'])
  if (typeof value.targetId !== 'string' || value.targetId.length === 0) {
    throw new PlatformContractError('Region target id must be a non-empty string.')
  }
  const geometry = parseCaptureGeometry(value.geometry)
  return {
    mode: 'region',
    delivery: value.delivery as OutputDelivery,
    targetId: value.targetId,
    geometry,
  }
}

export function deliveryTargetsFor(delivery: OutputDelivery): readonly DeliveryTarget[] {
  if (delivery === 'both') {
    return ['clipboard', 'folder']
  }
  return [delivery]
}

export class PlatformContractError extends Error {
  public constructor(message: string) {
    super(message)
    this.name = 'PlatformContractError'
  }
}

export function parseCaptureGeometry(value: unknown): CaptureGeometry {
  if (!isRecord(value)) {
    throw new PlatformContractError('Region geometry must be an object.')
  }
  requireExactKeys(value, ['coordinateSpace', 'x', 'y', 'width', 'height'])
  if (value.coordinateSpace !== 'target-logical') {
    throw new PlatformContractError('Region geometry must use target-logical coordinates.')
  }
  if (!isNonNegativeFiniteNumber(value.x) || !isNonNegativeFiniteNumber(value.y)) {
    throw new PlatformContractError('Region origin must contain finite non-negative numbers.')
  }
  if (!isPositiveFiniteNumber(value.width) || !isPositiveFiniteNumber(value.height)) {
    throw new PlatformContractError('Region size must contain finite positive numbers.')
  }
  return {
    coordinateSpace: 'target-logical',
    x: value.x,
    y: value.y,
    width: value.width,
    height: value.height,
  }
}

function requireExactKeys(value: Record<string, unknown>, expected: readonly string[]): void {
  const actual = Object.keys(value)
  if (actual.length !== expected.length || !expected.every((key) => key in value)) {
    throw new PlatformContractError('Capture request contains missing or unknown fields.')
  }
}

function isPositiveFiniteNumber(value: unknown): value is number {
  return typeof value === 'number' && Number.isFinite(value) && value > 0
}

function isNonNegativeFiniteNumber(value: unknown): value is number {
  return typeof value === 'number' && Number.isFinite(value) && value >= 0
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

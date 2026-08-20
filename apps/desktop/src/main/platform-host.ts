import type {
  CaptureRequest,
  CaptureResult,
  LumierePlatform,
  PlatformCapabilities,
  PlatformHost,
} from '../shared/platform-contract'
import { PLATFORM_CONTRACT_VERSION } from '../shared/platform-contract'

export class UnavailablePlatformHost implements PlatformHost {
  public constructor(private readonly platform: LumierePlatform) {}

  public async getCapabilities(): Promise<PlatformCapabilities> {
    return {
      contractVersion: PLATFORM_CONTRACT_VERSION,
      platform: this.platform,
      hostStatus: 'unavailable',
      captureModes: [],
      hdrCapture: 'unavailable',
      outputProfiles: ['srgb-visual-match'],
      unavailableReason: {
        code: 'host-unavailable',
        message: `The ${this.platform} native capture host has not been connected yet.`,
        retryable: false,
      },
    }
  }

  public async capture(_request: CaptureRequest): Promise<CaptureResult> {
    return {
      status: 'failed',
      failure: {
        code: 'host-unavailable',
        message: `The ${this.platform} native capture host has not been connected yet.`,
        retryable: false,
      },
    }
  }
}

export function currentLumierePlatform(): LumierePlatform {
  if (process.platform === 'darwin') {
    return 'macos'
  }

  if (process.platform === 'win32') {
    return 'windows'
  }

  throw new Error(`Lumiere does not support ${process.platform}.`)
}

import type {
  CaptureResult,
  LumierePlatform,
  PlatformCapabilities,
  PlatformHost,
} from '../shared/platform-contract'
import { PLATFORM_CONTRACT_VERSION } from '../shared/platform-contract'

export class UnavailablePlatformHost implements PlatformHost {
  public constructor(private readonly platform: LumierePlatform) {}

  public getCapabilities(): Promise<PlatformCapabilities> {
    return Promise.resolve({
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
    })
  }

  public capture(): Promise<CaptureResult> {
    return Promise.resolve({
      status: 'failed',
      failure: {
        code: 'host-unavailable',
        message: `The ${this.platform} native capture host has not been connected yet.`,
        retryable: false,
      },
    })
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

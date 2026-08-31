import type { CaptureResult, PlatformCapabilities, PlatformHost } from '../shared/platform-contract'
import { parseDisplayCaptureRequest, PlatformContractError } from '../shared/platform-contract'

export interface PlatformHandlers {
  getCapabilities(): Promise<PlatformCapabilities>
  captureDisplay(value: unknown): Promise<CaptureResult>
}

export function createPlatformHandlers(host: PlatformHost): PlatformHandlers {
  return {
    getCapabilities: () => host.getCapabilities(),
    captureDisplay: async (value) => {
      try {
        return await host.captureDisplay(parseDisplayCaptureRequest(value))
      } catch (error) {
        if (error instanceof PlatformContractError) {
          return {
            status: 'failed',
            failure: {
              code: 'invalid-request',
              message: error.message,
              retryable: false,
            },
          }
        }

        return {
          status: 'failed',
          failure: {
            code: 'unexpected-failure',
            message: 'The platform capture host failed unexpectedly.',
            retryable: true,
          },
        }
      }
    },
  }
}

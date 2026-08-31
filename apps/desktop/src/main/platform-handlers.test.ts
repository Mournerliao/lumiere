import { describe, expect, it } from 'vitest'
import type {
  CaptureRequest,
  CaptureResult,
  PlatformCapabilities,
  PlatformHost,
} from '../shared/platform-contract'
import { PLATFORM_CONTRACT_VERSION } from '../shared/platform-contract'
import { createPlatformHandlers } from './platform-handlers'
import { UnavailablePlatformHost } from './platform-host'

describe('platform handlers', () => {
  it('rejects an unknown capture mode before crossing the platform seam', async () => {
    const host = new RecordingPlatformHost()
    const handlers = createPlatformHandlers(host)

    const result = await handlers.capture({ mode: 'window', delivery: 'folder' })

    expect(result).toMatchObject({
      status: 'failed',
      failure: { code: 'invalid-request', retryable: false },
    })
    expect(host.requests).toEqual([])
  })

  it('passes a valid request through the narrow host interface', async () => {
    const host = new RecordingPlatformHost()
    const handlers = createPlatformHandlers(host)

    await handlers.capture({
      mode: 'region',
      delivery: 'both',
      saveDirectory: '/tmp/captures',
      targetId: 'display-17',
      geometry: {
        coordinateSpace: 'target-logical',
        x: 10,
        y: 20,
        width: 640,
        height: 360,
      },
    })

    expect(host.requests).toEqual([
      {
        mode: 'region',
        delivery: 'both',
        saveDirectory: '/tmp/captures',
        targetId: 'display-17',
        geometry: {
          coordinateSpace: 'target-logical',
          x: 10,
          y: 20,
          width: 640,
          height: 360,
        },
      },
    ])
  })

  it('reports an unavailable native host without falling back to Electron capture', async () => {
    const handlers = createPlatformHandlers(new UnavailablePlatformHost('macos'))

    const capabilities = await handlers.getCapabilities()
    const result = await handlers.capture({ mode: 'display', delivery: 'clipboard' })

    expect(capabilities).toMatchObject({
      contractVersion: 2,
      platform: 'macos',
      hostStatus: 'unavailable',
      hdrCapture: 'unavailable',
      outputProfiles: ['srgb-visual-match'],
    })
    expect(result).toMatchObject({
      status: 'failed',
      failure: { code: 'host-unavailable' },
    })
  })
})

class RecordingPlatformHost implements PlatformHost {
  public readonly requests: CaptureRequest[] = []

  public getCapabilities(): Promise<PlatformCapabilities> {
    return Promise.resolve({
      contractVersion: PLATFORM_CONTRACT_VERSION,
      platform: 'macos',
      hostStatus: 'available',
      captureModes: ['region', 'display'],
      deliveryTargets: ['clipboard', 'folder'],
      hdrCapture: 'supported',
      outputProfiles: ['srgb-visual-match'],
    })
  }

  public capture(request: CaptureRequest): Promise<CaptureResult> {
    this.requests.push(request)
    return Promise.resolve({
      status: 'completed',
      sourceDynamicRange: 'hdr',
      outputProfile: 'srgb-visual-match',
      deliveries:
        request.delivery === 'both'
          ? [
              { target: 'clipboard', status: 'success' },
              { target: 'folder', status: 'success', filePath: '/tmp/lumiere.png' },
            ]
          : request.delivery === 'clipboard'
            ? [{ target: 'clipboard', status: 'success' }]
            : [{ target: 'folder', status: 'success', filePath: '/tmp/lumiere.png' }],
    })
  }
}

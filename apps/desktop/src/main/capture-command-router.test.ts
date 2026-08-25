import { describe, expect, it } from 'vitest'
import { CaptureCommandRouter } from './capture-command-router'
import type {
  CaptureRequest,
  CaptureResult,
  PlatformCapabilities,
  PlatformHost,
} from '../shared/platform-contract'
import { PLATFORM_CONTRACT_VERSION } from '../shared/platform-contract'

describe('CaptureCommandRouter', () => {
  it('projects the current macOS display and folder state for the renderer', async () => {
    const host = new StubHost({
      contractVersion: PLATFORM_CONTRACT_VERSION,
      platform: 'macos',
      hostStatus: 'available',
      captureModes: ['display'],
      hdrCapture: 'unavailable',
      outputProfiles: ['srgb-visual-match'],
    })

    await expect(new CaptureCommandRouter('macos', host).getSurfaceSnapshot()).resolves.toEqual({
      platform: 'macos',
      hostAvailable: true,
      captureModes: ['display'],
      hdrStatus: 'unavailable',
      output: {
        delivery: 'folder',
        label: 'Folder',
        location: '~/Pictures/Lumiere',
      },
    })
  })

  it('routes display capture to the current folder delivery', async () => {
    const host = new StubHost(availableCapabilities(), {
      status: 'success',
      sourceDynamicRange: 'hdr',
      artifact: {
        profile: 'srgb-visual-match',
        delivery: 'folder',
        filePath: '/tmp/lumiere.png',
      },
    })

    await expect(new CaptureCommandRouter('macos', host).capture('display')).resolves.toEqual({
      status: 'success',
      feedback: 'Saved to “Lumiere”',
      filePath: '/tmp/lumiere.png',
    })
    expect(host.requests).toEqual([{ mode: 'display', delivery: 'folder' }])
  })

  it('maps native failures to product copy instead of exposing host diagnostics', async () => {
    const host = new StubHost(availableCapabilities(), {
      status: 'failed',
      failure: {
        code: 'permission-denied',
        message: 'CGPreflightScreenCaptureAccess returned false for pid 4812.',
        retryable: false,
      },
    })

    const result = await new CaptureCommandRouter('macos', host).capture('display')

    expect(result).toMatchObject({
      status: 'failed',
      feedback: 'Screen recording permission is required',
      notice: {
        detail: 'Allow screen recording in System Settings, then try again.',
      },
    })
    expect(JSON.stringify(result)).not.toContain('CGPreflightScreenCaptureAccess')
  })

  it('does not start a second native capture while one is in progress', async () => {
    let finishCapture: ((result: CaptureResult) => void) | undefined
    const capturePending = new Promise<CaptureResult>((resolve) => {
      finishCapture = resolve
    })
    const host = new StubHost(availableCapabilities(), capturePending)
    const router = new CaptureCommandRouter('macos', host)

    const firstCapture = router.capture('display')
    await Promise.resolve()
    const secondCapture = await router.capture('display')

    expect(secondCapture).toMatchObject({
      status: 'failed',
      feedback: 'A capture is already in progress',
    })
    expect(host.requests).toHaveLength(1)

    finishCapture?.({
      status: 'success',
      sourceDynamicRange: 'sdr',
      artifact: {
        profile: 'srgb-visual-match',
        delivery: 'folder',
        filePath: '/tmp/first.png',
      },
    })
    await firstCapture
  })
})

class StubHost implements PlatformHost {
  public readonly requests: CaptureRequest[] = []

  public constructor(
    private readonly capabilities: PlatformCapabilities,
    private readonly result: CaptureResult | Promise<CaptureResult> = {
      status: 'cancelled',
    },
  ) {}

  public getCapabilities(): Promise<PlatformCapabilities> {
    return Promise.resolve(this.capabilities)
  }

  public capture(request: CaptureRequest): Promise<CaptureResult> {
    this.requests.push(request)
    return Promise.resolve(this.result)
  }
}

function availableCapabilities(): PlatformCapabilities {
  return {
    contractVersion: PLATFORM_CONTRACT_VERSION,
    platform: 'macos',
    hostStatus: 'available',
    captureModes: ['display'],
    hdrCapture: 'supported',
    outputProfiles: ['srgb-visual-match'],
  }
}

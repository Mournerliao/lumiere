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
  it('projects the current macOS display and both-target default for the renderer', async () => {
    const host = new StubHost({
      contractVersion: PLATFORM_CONTRACT_VERSION,
      platform: 'macos',
      hostStatus: 'available',
      captureModes: ['display'],
      deliveryTargets: ['clipboard', 'folder'],
      hdrCapture: 'unavailable',
      outputProfiles: ['srgb-visual-match'],
    })

    await expect(new CaptureCommandRouter('macos', host).getSurfaceSnapshot()).resolves.toEqual({
      platform: 'macos',
      hostAvailable: true,
      captureModes: ['display'],
      hdrStatus: 'unavailable',
      output: {
        delivery: 'both',
        label: 'Clipboard and folder',
        location: '~/Pictures/Lumiere',
      },
    })
  })

  it('routes display capture to the clipboard-and-folder default', async () => {
    const host = new StubHost(availableCapabilities(), {
      status: 'completed',
      sourceDynamicRange: 'hdr',
      outputProfile: 'srgb-visual-match',
      deliveries: [
        { target: 'clipboard', status: 'success' },
        { target: 'folder', status: 'success', filePath: '/tmp/lumiere.png' },
      ],
    })

    await expect(new CaptureCommandRouter('macos', host).captureDisplay()).resolves.toEqual({
      status: 'success',
      feedback: 'Copied and saved to “Lumiere”',
      filePath: '/tmp/lumiere.png',
    })
    expect(host.requests).toEqual([{ mode: 'display', delivery: 'both' }])
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

    const result = await new CaptureCommandRouter('macos', host).captureDisplay()

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

    const firstCapture = router.captureDisplay()
    await Promise.resolve()
    const secondCapture = await router.captureDisplay()

    expect(secondCapture).toMatchObject({
      status: 'failed',
      feedback: 'A capture is already in progress',
    })
    expect(host.requests).toHaveLength(1)

    finishCapture?.({
      status: 'completed',
      sourceDynamicRange: 'sdr',
      outputProfile: 'srgb-visual-match',
      deliveries: [
        { target: 'clipboard', status: 'success' },
        { target: 'folder', status: 'success', filePath: '/tmp/first.png' },
      ],
    })
    await firstCapture
  })

  it('preserves a successful target when the second delivery fails', async () => {
    const host = new StubHost(
      { ...availableCapabilities(), deliveryTargets: ['clipboard', 'folder'] },
      {
        status: 'completed',
        sourceDynamicRange: 'hdr',
        outputProfile: 'srgb-visual-match',
        deliveries: [
          { target: 'clipboard', status: 'success' },
          {
            target: 'folder',
            status: 'failed',
            failure: {
              code: 'delivery-failed',
              message: 'The configured folder is not writable.',
              retryable: true,
            },
          },
        ],
      },
    )

    await expect(
      new CaptureCommandRouter('macos', host, { delivery: 'both' }).captureDisplay(),
    ).resolves.toMatchObject({
      status: 'partial',
      feedback: 'Copied to clipboard, but couldn’t save the file',
    })
  })

  it('projects two target failures as delivery failure without losing either host fact', async () => {
    const failed = (target: 'clipboard' | 'folder') => ({
      target,
      status: 'failed' as const,
      failure: {
        code: 'delivery-failed' as const,
        message: `${target} failed`,
        retryable: true,
      },
    })
    const host = new StubHost(availableCapabilities(), {
      status: 'completed',
      sourceDynamicRange: 'sdr',
      outputProfile: 'srgb-visual-match',
      deliveries: [failed('clipboard'), failed('folder')],
    })

    await expect(new CaptureCommandRouter('macos', host).captureDisplay()).resolves.toMatchObject({
      status: 'failed',
      feedback: 'Couldn’t deliver capture',
    })
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
    deliveryTargets: ['clipboard', 'folder'],
    hdrCapture: 'supported',
    outputProfiles: ['srgb-visual-match'],
  }
}

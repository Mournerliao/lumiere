import { EventEmitter } from 'node:events'
import { execPath } from 'node:process'
import { PassThrough } from 'node:stream'
import type { ChildProcessWithoutNullStreams } from 'node:child_process'
import { describe, expect, it } from 'vitest'
import { WindowsPlatformHost } from './windows-platform-host'

describe('Windows platform host process transport', () => {
  it('accepts Windows capabilities and typed unavailable capture results', async () => {
    const process = new FakeNativeProcess()
    process.stdin.on('data', (chunk: Buffer) => {
      const request = JSON.parse(chunk.toString('utf8')) as Record<string, unknown>
      const result =
        request.method === 'getCapabilities'
          ? {
              contractVersion: 2,
              platform: 'windows',
              hostStatus: 'available',
              captureModes: [],
              deliveryTargets: [],
              hdrCapture: 'unavailable',
              outputProfiles: ['srgb-visual-match'],
            }
          : {
              status: 'failed',
              failure: {
                code: 'capture-unavailable',
                message: 'Windows capture is not connected yet.',
                retryable: false,
              },
            }
      process.respond({ version: 2, id: request.id, result })
    })

    const host = new WindowsPlatformHost([execPath], () => process.asChildProcess())

    await expect(host.getCapabilities()).resolves.toMatchObject({
      platform: 'windows',
      hostStatus: 'available',
      captureModes: [],
    })
    await expect(host.capture({ mode: 'display', delivery: 'folder' })).resolves.toMatchObject({
      status: 'failed',
      failure: { code: 'capture-unavailable', retryable: false },
    })

    host.dispose()
  })
})

class FakeNativeProcess extends EventEmitter {
  public readonly stdin = new PassThrough()
  public readonly stdout = new PassThrough()
  public readonly stderr = new PassThrough()
  public killed = false

  public respond(envelope: unknown): void {
    this.stdout.write(`${JSON.stringify(envelope)}\n`)
  }

  public kill(): boolean {
    this.killed = true
    return true
  }

  public asChildProcess(): ChildProcessWithoutNullStreams {
    return this as unknown as ChildProcessWithoutNullStreams
  }
}

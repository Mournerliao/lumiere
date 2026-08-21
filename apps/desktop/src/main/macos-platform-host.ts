import { randomUUID } from 'node:crypto'
import { access } from 'node:fs/promises'
import { spawn, type ChildProcessWithoutNullStreams } from 'node:child_process'
import type {
  CaptureRequest,
  CaptureResult,
  PlatformCapabilities,
  PlatformFailure,
  PlatformHost,
} from '../shared/platform-contract'
import { PLATFORM_CONTRACT_VERSION } from '../shared/platform-contract'

const requestTimeoutMilliseconds = 15_000

type HostMethod = 'getCapabilities' | 'capture'
type SpawnHost = (executablePath: string) => ChildProcessWithoutNullStreams

interface PendingRequest {
  method: HostMethod
  resolve(value: unknown): void
  reject(error: Error): void
  timeout: NodeJS.Timeout
}

export class MacOSPlatformHost implements PlatformHost {
  private process: ChildProcessWithoutNullStreams | null = null
  private processStart: Promise<ChildProcessWithoutNullStreams> | null = null
  private stdoutBuffer = ''
  private readonly pending = new Map<string, PendingRequest>()
  private disposed = false

  public constructor(
    private readonly executableCandidates: readonly string[],
    private readonly spawnHost: SpawnHost = spawnNativeHost,
  ) {}

  public async getCapabilities(): Promise<PlatformCapabilities> {
    try {
      return parseCapabilities(await this.request('getCapabilities', {}))
    } catch (error) {
      return unavailableCapabilities(error)
    }
  }

  public async capture(request: CaptureRequest): Promise<CaptureResult> {
    try {
      return parseCaptureResult(await this.request('capture', request))
    } catch (error) {
      return {
        status: 'failed',
        failure: failureFromError(error),
      }
    }
  }

  public dispose(): void {
    this.disposed = true
    const child = this.process
    this.process = null
    if (child && !child.killed) {
      child.kill()
    }
    this.rejectPending(new Error('The macOS native capture host was disposed.'))
  }

  private async request(method: HostMethod, params: object): Promise<unknown> {
    const child = await this.ensureProcess()
    const id = randomUUID()
    const line = JSON.stringify({
      version: PLATFORM_CONTRACT_VERSION,
      id,
      method,
      params,
    })

    return new Promise((resolve, reject) => {
      const timeout = setTimeout(() => {
        this.handleTermination(
          child,
          new Error(`The macOS host timed out while handling ${method}.`),
        )
      }, requestTimeoutMilliseconds)
      this.pending.set(id, { method, resolve, reject, timeout })

      child.stdin.write(`${line}\n`, (error) => {
        if (!error) {
          return
        }
        clearTimeout(timeout)
        this.pending.delete(id)
        reject(error)
      })
    })
  }

  private async ensureProcess(): Promise<ChildProcessWithoutNullStreams> {
    if (this.disposed) {
      throw new Error('The macOS native capture host was disposed.')
    }
    if (this.process) {
      return this.process
    }
    if (this.processStart) {
      return this.processStart
    }

    const processStart = this.startProcess()
    this.processStart = processStart
    try {
      return await processStart
    } finally {
      if (this.processStart === processStart) {
        this.processStart = null
      }
    }
  }

  private async startProcess(): Promise<ChildProcessWithoutNullStreams> {
    const executablePath = await firstAccessiblePath(this.executableCandidates)
    if (!executablePath) {
      throw new Error('The macOS native capture host executable is unavailable.')
    }
    if (this.disposed) {
      throw new Error('The macOS native capture host was disposed.')
    }

    const child = this.spawnHost(executablePath)
    this.process = child

    child.stdout.setEncoding('utf8')
    child.stdout.on('data', (chunk: string) => {
      this.acceptStdout(child, chunk)
    })
    child.stderr.setEncoding('utf8')
    child.stderr.on('data', (chunk: string) => {
      for (const line of chunk.split('\n').filter(Boolean)) {
        process.stderr.write(`[macos-host] ${line}\n`)
      }
    })
    child.on('error', (error) => {
      this.handleTermination(child, error)
    })
    child.on('exit', (code, signal) => {
      this.handleTermination(
        child,
        new Error(
          `The macOS native capture host exited (code=${String(code)}, signal=${String(signal)}).`,
        ),
      )
    })

    return child
  }

  private acceptStdout(child: ChildProcessWithoutNullStreams, chunk: string): void {
    if (this.process !== child) {
      return
    }
    this.stdoutBuffer += chunk
    let newlineIndex = this.stdoutBuffer.indexOf('\n')
    while (newlineIndex >= 0) {
      const line = this.stdoutBuffer.slice(0, newlineIndex)
      this.stdoutBuffer = this.stdoutBuffer.slice(newlineIndex + 1)
      if (line.length > 0) {
        this.acceptResponseLine(child, line)
      }
      newlineIndex = this.stdoutBuffer.indexOf('\n')
    }
  }

  private acceptResponseLine(child: ChildProcessWithoutNullStreams, line: string): void {
    let envelope: unknown
    try {
      envelope = JSON.parse(line)
    } catch {
      this.handleTermination(child, new Error('The macOS host emitted invalid JSON.'))
      return
    }

    if (
      !isRecord(envelope) ||
      envelope.version !== PLATFORM_CONTRACT_VERSION ||
      typeof envelope.id !== 'string' ||
      envelope.id.length === 0 ||
      (!hasExactKeys(envelope, ['version', 'id', 'result']) &&
        !hasExactKeys(envelope, ['version', 'id', 'error']))
    ) {
      this.handleTermination(
        child,
        new Error('The macOS host emitted an invalid protocol envelope.'),
      )
      return
    }

    const pending = this.pending.get(envelope.id)
    if (!pending) {
      return
    }

    try {
      const value =
        'error' in envelope
          ? new NativeHostFailure(parseFailure(envelope.error))
          : pending.method === 'getCapabilities'
            ? parseCapabilities(envelope.result)
            : parseCaptureResult(envelope.result)
      clearTimeout(pending.timeout)
      this.pending.delete(envelope.id)
      if (value instanceof NativeHostFailure) {
        pending.reject(value)
      } else {
        pending.resolve(value)
      }
    } catch (error) {
      this.handleTermination(child, error instanceof Error ? error : new Error(String(error)))
    }
  }

  private handleTermination(child: ChildProcessWithoutNullStreams, error: Error): void {
    if (this.process !== child) {
      return
    }
    this.process = null
    this.stdoutBuffer = ''
    if (!child.killed) {
      child.kill()
    }
    this.rejectPending(error)
  }

  private rejectPending(error: Error): void {
    for (const pending of this.pending.values()) {
      clearTimeout(pending.timeout)
      pending.reject(error)
    }
    this.pending.clear()
  }
}

function spawnNativeHost(executablePath: string): ChildProcessWithoutNullStreams {
  return spawn(executablePath, [], {
    stdio: ['pipe', 'pipe', 'pipe'],
    windowsHide: true,
  })
}

class NativeHostFailure extends Error {
  public constructor(public readonly failure: PlatformFailure) {
    super(failure.message)
    this.name = 'NativeHostFailure'
  }
}

async function firstAccessiblePath(candidates: readonly string[]): Promise<string | null> {
  for (const candidate of candidates) {
    try {
      await access(candidate)
      return candidate
    } catch {
      // Continue to the next explicit candidate.
    }
  }
  return null
}

function unavailableCapabilities(error: unknown): PlatformCapabilities {
  return {
    contractVersion: PLATFORM_CONTRACT_VERSION,
    platform: 'macos',
    hostStatus: 'unavailable',
    captureModes: [],
    hdrCapture: 'unavailable',
    outputProfiles: ['srgb-visual-match'],
    unavailableReason: failureFromError(error),
  }
}

function failureFromError(error: unknown): PlatformFailure {
  if (error instanceof NativeHostFailure) {
    return error.failure
  }
  return {
    code: 'host-unavailable',
    message:
      error instanceof Error ? error.message : 'The macOS native capture host is unavailable.',
    retryable: true,
  }
}

function parseCapabilities(value: unknown): PlatformCapabilities {
  if (!isRecord(value)) {
    throw new Error('The macOS host returned invalid capabilities.')
  }
  const captureModes = value.captureModes
  const outputProfiles = value.outputProfiles
  if (
    value.contractVersion !== PLATFORM_CONTRACT_VERSION ||
    value.platform !== 'macos' ||
    (value.hostStatus !== 'available' && value.hostStatus !== 'unavailable') ||
    !Array.isArray(captureModes) ||
    !captureModes.every((mode) => mode === 'region' || mode === 'display') ||
    new Set(captureModes).size !== captureModes.length ||
    (value.hdrCapture !== 'supported' &&
      value.hdrCapture !== 'unavailable' &&
      value.hdrCapture !== 'unvalidated') ||
    !Array.isArray(outputProfiles) ||
    outputProfiles.length !== 1 ||
    outputProfiles[0] !== 'srgb-visual-match' ||
    !hasExactKeys(
      value,
      ['contractVersion', 'platform', 'hostStatus', 'captureModes', 'hdrCapture', 'outputProfiles'],
      ['unavailableReason'],
    ) ||
    (value.hostStatus === 'unavailable' && value.unavailableReason === undefined)
  ) {
    throw new Error('The macOS host returned invalid capabilities.')
  }

  if (value.unavailableReason !== undefined) {
    parseFailure(value.unavailableReason)
  }

  return value as unknown as PlatformCapabilities
}

function parseCaptureResult(value: unknown): CaptureResult {
  if (!isRecord(value) || typeof value.status !== 'string') {
    throw new Error('The macOS host returned an invalid capture result.')
  }
  if (value.status === 'failed') {
    if (!hasExactKeys(value, ['status', 'failure'])) {
      throw new Error('The macOS host returned an invalid capture result.')
    }
    return { status: 'failed', failure: parseFailure(value.failure) }
  }
  if (value.status === 'cancelled') {
    if (!hasExactKeys(value, ['status'])) {
      throw new Error('The macOS host returned an invalid capture result.')
    }
    return { status: 'cancelled' }
  }
  if (
    value.status !== 'success' ||
    (value.sourceDynamicRange !== 'sdr' && value.sourceDynamicRange !== 'hdr') ||
    !isRecord(value.artifact) ||
    !hasExactKeys(value, ['status', 'sourceDynamicRange', 'artifact']) ||
    value.artifact.profile !== 'srgb-visual-match' ||
    (value.artifact.delivery !== 'clipboard' &&
      value.artifact.delivery !== 'folder' &&
      value.artifact.delivery !== 'both') ||
    (value.artifact.filePath !== undefined &&
      (typeof value.artifact.filePath !== 'string' || value.artifact.filePath.length === 0)) ||
    !hasExactKeys(value.artifact, ['profile', 'delivery'], ['filePath'])
  ) {
    throw new Error('The macOS host returned an invalid capture result.')
  }
  return value as unknown as CaptureResult
}

function parseFailure(value: unknown): PlatformFailure {
  if (!isRecord(value)) {
    throw new Error('The macOS host returned an invalid failure.')
  }
  const validCodes = [
    'host-unavailable',
    'permission-denied',
    'capture-unavailable',
    'invalid-request',
    'unexpected-failure',
  ]
  if (
    typeof value.code !== 'string' ||
    !validCodes.includes(value.code) ||
    typeof value.message !== 'string' ||
    value.message.length === 0 ||
    typeof value.retryable !== 'boolean' ||
    !hasExactKeys(value, ['code', 'message', 'retryable'])
  ) {
    throw new Error('The macOS host returned an invalid failure.')
  }
  return value as unknown as PlatformFailure
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function hasExactKeys(
  value: Record<string, unknown>,
  required: readonly string[],
  optional: readonly string[] = [],
): boolean {
  const keys = Object.keys(value)
  const allowed = new Set([...required, ...optional])
  return required.every((key) => key in value) && keys.every((key) => allowed.has(key))
}

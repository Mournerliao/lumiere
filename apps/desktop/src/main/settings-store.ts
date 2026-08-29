import { mkdir, readFile, rename, writeFile } from 'node:fs/promises'
import { dirname } from 'node:path'
import type { CapturePreferences, CapturePreferencesReader } from './capture-command-router'
import {
  parseAfterCaptureBehavior,
  parseHdrStatusReminders,
  parseOutputDelivery,
  type AfterCaptureBehavior,
} from '../shared/settings-command'
import {
  parseShortcutAccelerator,
  type CaptureMode,
  type CaptureShortcuts,
} from '../shared/shortcut-command'
import type { OutputDelivery } from '../shared/platform-contract'

const SETTINGS_VERSION = 4 as const
const DEFAULT_OUTPUT_DELIVERY: OutputDelivery = 'both'
const DEFAULT_CAPTURE_SHORTCUTS: CaptureShortcuts = { region: null, display: null }
const DEFAULT_AFTER_CAPTURE_BEHAVIOR: AfterCaptureBehavior = 'do-nothing'
const DEFAULT_HDR_STATUS_REMINDERS = true

interface PersistedSettingsV4 {
  version: typeof SETTINGS_VERSION
  outputDelivery: OutputDelivery
  captureShortcuts: CaptureShortcuts
  afterCaptureBehavior: AfterCaptureBehavior
  hdrStatusReminders: boolean
}

export class SettingsStore implements CapturePreferencesReader {
  private outputDelivery: OutputDelivery = DEFAULT_OUTPUT_DELIVERY
  private captureShortcuts: CaptureShortcuts = { ...DEFAULT_CAPTURE_SHORTCUTS }
  private afterCaptureBehavior: AfterCaptureBehavior = DEFAULT_AFTER_CAPTURE_BEHAVIOR
  private hdrStatusReminders = DEFAULT_HDR_STATUS_REMINDERS
  private pendingWrite: Promise<void> = Promise.resolve()

  public constructor(private readonly filePath: string) {}

  public async load(): Promise<void> {
    try {
      const parsed: unknown = JSON.parse(await readFile(this.filePath, 'utf8'))
      const settings = parsePersistedSettings(parsed)
      this.outputDelivery = settings.outputDelivery
      this.captureShortcuts = settings.captureShortcuts
      this.afterCaptureBehavior = settings.afterCaptureBehavior
      this.hdrStatusReminders = settings.hdrStatusReminders
    } catch (error) {
      if (isMissingFile(error)) {
        return
      }
      this.outputDelivery = DEFAULT_OUTPUT_DELIVERY
      this.captureShortcuts = { ...DEFAULT_CAPTURE_SHORTCUTS }
      this.afterCaptureBehavior = DEFAULT_AFTER_CAPTURE_BEHAVIOR
      this.hdrStatusReminders = DEFAULT_HDR_STATUS_REMINDERS
    }
  }

  public getCapturePreferences(): CapturePreferences {
    return { delivery: this.outputDelivery, hdrStatusReminders: this.hdrStatusReminders }
  }

  public getOutputDelivery(): OutputDelivery {
    return this.outputDelivery
  }

  public getCaptureShortcuts(): CaptureShortcuts {
    return { ...this.captureShortcuts }
  }

  public getAfterCaptureBehavior(): AfterCaptureBehavior {
    return this.afterCaptureBehavior
  }

  public getHdrStatusReminders(): boolean {
    return this.hdrStatusReminders
  }

  public async setOutputDelivery(outputDelivery: OutputDelivery): Promise<void> {
    const next = parseOutputDelivery(outputDelivery)
    const write = async (): Promise<void> => {
      await this.writeSettings(
        next,
        this.captureShortcuts,
        this.afterCaptureBehavior,
        this.hdrStatusReminders,
      )
      this.outputDelivery = next
    }

    this.pendingWrite = this.pendingWrite.then(write, write)
    await this.pendingWrite
  }

  public async setCaptureShortcut(mode: CaptureMode, accelerator: string | null): Promise<void> {
    const next = accelerator === null ? null : parseShortcutAccelerator(accelerator)
    const write = async (): Promise<void> => {
      const shortcuts = { ...this.captureShortcuts, [mode]: next }
      await this.writeSettings(
        this.outputDelivery,
        shortcuts,
        this.afterCaptureBehavior,
        this.hdrStatusReminders,
      )
      this.captureShortcuts = shortcuts
    }

    this.pendingWrite = this.pendingWrite.then(write, write)
    await this.pendingWrite
  }

  public async setAfterCaptureBehavior(behavior: AfterCaptureBehavior): Promise<void> {
    const next = parseAfterCaptureBehavior(behavior)
    const write = async (): Promise<void> => {
      await this.writeSettings(
        this.outputDelivery,
        this.captureShortcuts,
        next,
        this.hdrStatusReminders,
      )
      this.afterCaptureBehavior = next
    }

    this.pendingWrite = this.pendingWrite.then(write, write)
    await this.pendingWrite
  }

  public async setHdrStatusReminders(enabled: boolean): Promise<void> {
    const next = parseHdrStatusReminders(enabled)
    const write = async (): Promise<void> => {
      await this.writeSettings(
        this.outputDelivery,
        this.captureShortcuts,
        this.afterCaptureBehavior,
        next,
      )
      this.hdrStatusReminders = next
    }

    this.pendingWrite = this.pendingWrite.then(write, write)
    await this.pendingWrite
  }

  private async writeSettings(
    outputDelivery: OutputDelivery,
    captureShortcuts: CaptureShortcuts,
    afterCaptureBehavior: AfterCaptureBehavior,
    hdrStatusReminders: boolean,
  ): Promise<void> {
    const settings: PersistedSettingsV4 = {
      version: SETTINGS_VERSION,
      outputDelivery,
      captureShortcuts,
      afterCaptureBehavior,
      hdrStatusReminders,
    }
    const temporaryPath = `${this.filePath}.tmp`
    await mkdir(dirname(this.filePath), { recursive: true })
    await writeFile(temporaryPath, `${JSON.stringify(settings, null, 2)}\n`, 'utf8')
    await rename(temporaryPath, this.filePath)
  }
}

function parsePersistedSettings(value: unknown): Omit<PersistedSettingsV4, 'version'> {
  if (isPersistedV1(value)) {
    return {
      outputDelivery: parseOutputDelivery(value.outputDelivery),
      captureShortcuts: { ...DEFAULT_CAPTURE_SHORTCUTS },
      afterCaptureBehavior: DEFAULT_AFTER_CAPTURE_BEHAVIOR,
      hdrStatusReminders: DEFAULT_HDR_STATUS_REMINDERS,
    }
  }
  if (isPersistedV2(value)) {
    return {
      outputDelivery: parseOutputDelivery(value.outputDelivery),
      captureShortcuts: parsePersistedShortcuts(value.captureShortcuts),
      afterCaptureBehavior: DEFAULT_AFTER_CAPTURE_BEHAVIOR,
      hdrStatusReminders: DEFAULT_HDR_STATUS_REMINDERS,
    }
  }
  if (isPersistedV3(value)) {
    return {
      outputDelivery: parseOutputDelivery(value.outputDelivery),
      captureShortcuts: parsePersistedShortcuts(value.captureShortcuts),
      afterCaptureBehavior: parseAfterCaptureBehavior(value.afterCaptureBehavior),
      hdrStatusReminders: DEFAULT_HDR_STATUS_REMINDERS,
    }
  }
  if (
    typeof value !== 'object' ||
    value === null ||
    Array.isArray(value) ||
    Object.keys(value).length !== 5 ||
    !('version' in value) ||
    !('outputDelivery' in value) ||
    !('captureShortcuts' in value) ||
    !('afterCaptureBehavior' in value) ||
    !('hdrStatusReminders' in value) ||
    value.version !== SETTINGS_VERSION
  ) {
    throw new Error('The settings file has an unsupported shape or version.')
  }

  return {
    outputDelivery: parseOutputDelivery(value.outputDelivery),
    captureShortcuts: parsePersistedShortcuts(value.captureShortcuts),
    afterCaptureBehavior: parseAfterCaptureBehavior(value.afterCaptureBehavior),
    hdrStatusReminders: parseHdrStatusReminders(value.hdrStatusReminders),
  }
}

function isPersistedV3(value: unknown): value is {
  version: 3
  outputDelivery: unknown
  captureShortcuts: unknown
  afterCaptureBehavior: unknown
} {
  return (
    typeof value === 'object' &&
    value !== null &&
    !Array.isArray(value) &&
    Object.keys(value).length === 4 &&
    'version' in value &&
    value.version === 3 &&
    'outputDelivery' in value &&
    'captureShortcuts' in value &&
    'afterCaptureBehavior' in value
  )
}

function isPersistedV2(
  value: unknown,
): value is { version: 2; outputDelivery: unknown; captureShortcuts: unknown } {
  return (
    typeof value === 'object' &&
    value !== null &&
    !Array.isArray(value) &&
    Object.keys(value).length === 3 &&
    'version' in value &&
    value.version === 2 &&
    'outputDelivery' in value &&
    'captureShortcuts' in value
  )
}

function isPersistedV1(value: unknown): value is { version: 1; outputDelivery: unknown } {
  return (
    typeof value === 'object' &&
    value !== null &&
    !Array.isArray(value) &&
    Object.keys(value).length === 2 &&
    'version' in value &&
    value.version === 1 &&
    'outputDelivery' in value
  )
}

function parsePersistedShortcuts(value: unknown): CaptureShortcuts {
  if (
    typeof value !== 'object' ||
    value === null ||
    Array.isArray(value) ||
    Object.keys(value).length !== 2 ||
    !('region' in value) ||
    !('display' in value)
  ) {
    throw new Error('The capture shortcut settings are invalid.')
  }
  return {
    region: value.region === null ? null : parseShortcutAccelerator(value.region),
    display: value.display === null ? null : parseShortcutAccelerator(value.display),
  }
}

function isMissingFile(error: unknown): boolean {
  return (
    typeof error === 'object' &&
    error !== null &&
    'code' in error &&
    (error as NodeJS.ErrnoException).code === 'ENOENT'
  )
}

import { mkdir, readFile, rename, writeFile } from 'node:fs/promises'
import { dirname } from 'node:path'
import type { CapturePreferences, CapturePreferencesReader } from './capture-command-router'
import { parseOutputDelivery } from '../shared/settings-command'
import type { OutputDelivery } from '../shared/platform-contract'

const SETTINGS_VERSION = 1 as const
const DEFAULT_OUTPUT_DELIVERY: OutputDelivery = 'both'

interface PersistedSettings {
  version: typeof SETTINGS_VERSION
  outputDelivery: OutputDelivery
}

export class SettingsStore implements CapturePreferencesReader {
  private outputDelivery: OutputDelivery = DEFAULT_OUTPUT_DELIVERY
  private pendingWrite: Promise<void> = Promise.resolve()

  public constructor(private readonly filePath: string) {}

  public async load(): Promise<void> {
    try {
      const parsed: unknown = JSON.parse(await readFile(this.filePath, 'utf8'))
      this.outputDelivery = parsePersistedSettings(parsed).outputDelivery
    } catch (error) {
      if (isMissingFile(error)) {
        return
      }
      this.outputDelivery = DEFAULT_OUTPUT_DELIVERY
    }
  }

  public getCapturePreferences(): CapturePreferences {
    return { delivery: this.outputDelivery }
  }

  public getOutputDelivery(): OutputDelivery {
    return this.outputDelivery
  }

  public async setOutputDelivery(outputDelivery: OutputDelivery): Promise<void> {
    const next = parseOutputDelivery(outputDelivery)
    const write = async (): Promise<void> => {
      const settings: PersistedSettings = {
        version: SETTINGS_VERSION,
        outputDelivery: next,
      }
      const temporaryPath = `${this.filePath}.tmp`
      await mkdir(dirname(this.filePath), { recursive: true })
      await writeFile(temporaryPath, `${JSON.stringify(settings, null, 2)}\n`, 'utf8')
      await rename(temporaryPath, this.filePath)
      this.outputDelivery = next
    }

    this.pendingWrite = this.pendingWrite.then(write, write)
    await this.pendingWrite
  }
}

function parsePersistedSettings(value: unknown): PersistedSettings {
  if (
    typeof value !== 'object' ||
    value === null ||
    Array.isArray(value) ||
    Object.keys(value).length !== 2 ||
    !('version' in value) ||
    !('outputDelivery' in value) ||
    value.version !== SETTINGS_VERSION
  ) {
    throw new Error('The settings file has an unsupported shape or version.')
  }

  return {
    version: SETTINGS_VERSION,
    outputDelivery: parseOutputDelivery(value.outputDelivery),
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

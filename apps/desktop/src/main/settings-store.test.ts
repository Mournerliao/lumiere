import { mkdtemp, readFile, rm, writeFile } from 'node:fs/promises'
import { join } from 'node:path'
import { tmpdir } from 'node:os'
import { afterEach, describe, expect, it } from 'vitest'
import { SettingsStore } from './settings-store'

const temporaryDirectories: string[] = []

afterEach(async () => {
  await Promise.all(temporaryDirectories.splice(0).map((path) => rm(path, { recursive: true })))
})

describe('SettingsStore', () => {
  it('defaults to clipboard and folder when no settings file exists', async () => {
    const store = new SettingsStore(await settingsPath())

    await store.load()

    expect(store.getCapturePreferences()).toEqual({ delivery: 'both' })
    expect(store.getCaptureShortcuts()).toEqual({ region: null, display: null })
    expect(store.getAfterCaptureBehavior()).toBe('do-nothing')
  })

  it('persists an output delivery and restores it in a new store', async () => {
    const filePath = await settingsPath()
    const store = new SettingsStore(filePath)
    await store.load()

    await store.setOutputDelivery('folder')
    const restartedStore = new SettingsStore(filePath)
    await restartedStore.load()

    expect(restartedStore.getOutputDelivery()).toBe('folder')
    await expect(readFile(filePath, 'utf8')).resolves.toContain('"outputDelivery": "folder"')
  })

  it('falls back to the default for an invalid or unsupported settings file', async () => {
    const filePath = await settingsPath()
    await writeFile(filePath, '{"version":3,"outputDelivery":"clipboard"}', 'utf8')
    const store = new SettingsStore(filePath)

    await store.load()

    expect(store.getOutputDelivery()).toBe('both')
  })

  it('migrates v1 output settings when a shortcut is first saved', async () => {
    const filePath = await settingsPath()
    await writeFile(filePath, '{"version":1,"outputDelivery":"folder"}', 'utf8')
    const store = new SettingsStore(filePath)
    await store.load()

    await store.setCaptureShortcut('region', 'Command+Shift+L')

    expect(store.getOutputDelivery()).toBe('folder')
    expect(store.getCaptureShortcuts()).toEqual({
      region: 'Command+Shift+L',
      display: null,
    })
    await expect(readFile(filePath, 'utf8')).resolves.toContain('"version": 3')
  })

  it('migrates v2 settings and persists an after-capture behavior', async () => {
    const filePath = await settingsPath()
    await writeFile(
      filePath,
      JSON.stringify({
        version: 2,
        outputDelivery: 'folder',
        captureShortcuts: { region: 'Command+Shift+L', display: null },
      }),
      'utf8',
    )
    const store = new SettingsStore(filePath)
    await store.load()

    expect(store.getAfterCaptureBehavior()).toBe('do-nothing')
    await store.setAfterCaptureBehavior('show-in-folder')

    const restartedStore = new SettingsStore(filePath)
    await restartedStore.load()
    expect(restartedStore.getOutputDelivery()).toBe('folder')
    expect(restartedStore.getCaptureShortcuts()).toEqual({
      region: 'Command+Shift+L',
      display: null,
    })
    expect(restartedStore.getAfterCaptureBehavior()).toBe('show-in-folder')
    await expect(readFile(filePath, 'utf8')).resolves.toContain('"version": 3')
  })
})

async function settingsPath(): Promise<string> {
  const directory = await mkdtemp(join(tmpdir(), 'lumiere-settings-'))
  temporaryDirectories.push(directory)
  return join(directory, 'settings.json')
}

import { describe, expect, it, vi } from 'vitest'
import { ShortcutService, ShortcutRegistrationError } from './shortcut-service'
import type { CaptureMode, CaptureShortcuts } from '../shared/shortcut-command'

class FakeRegistrar {
  public readonly callbacks = new Map<string, () => void>()
  public readonly rejected = new Set<string>()
  public suspended = false

  public register(accelerator: string, callback: () => void): boolean {
    if (this.rejected.has(accelerator) || this.callbacks.has(accelerator)) return false
    this.callbacks.set(accelerator, callback)
    return true
  }

  public unregister(accelerator: string): void {
    this.callbacks.delete(accelerator)
  }

  public setSuspended(suspended: boolean): void {
    this.suspended = suspended
  }
}

class FakeStore {
  public shortcuts: CaptureShortcuts = { region: null, display: null }
  public readonly writes: { mode: CaptureMode; accelerator: string | null }[] = []
  public writeFailure: Error | null = null

  public getCaptureShortcuts(): CaptureShortcuts {
    return { ...this.shortcuts }
  }

  public setCaptureShortcut(mode: CaptureMode, accelerator: string | null): Promise<void> {
    if (this.writeFailure) return Promise.reject(this.writeFailure)
    this.writes.push({ mode, accelerator })
    this.shortcuts = { ...this.shortcuts, [mode]: accelerator }
    return Promise.resolve()
  }
}

describe('ShortcutService', () => {
  it('registers persisted shortcuts and reports registration status', () => {
    const store = new FakeStore()
    store.shortcuts.region = 'Command+Shift+L'
    const registrar = new FakeRegistrar()
    const captureRegion = vi.fn()
    const service = new ShortcutService(store, registrar, {
      region: captureRegion,
      display: vi.fn(),
    })

    service.initialize()
    registrar.callbacks.get('Command+Shift+L')?.()

    expect(captureRegion).toHaveBeenCalledOnce()
    expect(service.getSnapshot()).toEqual({
      region: { accelerator: 'Command+Shift+L', status: 'registered' },
      display: { accelerator: null, status: 'unconfigured' },
    })
  })

  it('keeps an unavailable persisted shortcut visible without claiming it is registered', () => {
    const store = new FakeStore()
    store.shortcuts.region = 'Command+Shift+L'
    const registrar = new FakeRegistrar()
    registrar.rejected.add('Command+Shift+L')
    const service = new ShortcutService(store, registrar, {
      region: vi.fn(),
      display: vi.fn(),
    })

    service.initialize()

    expect(service.getSnapshot().region).toEqual({
      accelerator: 'Command+Shift+L',
      status: 'unavailable',
    })
  })

  it('preserves the old registration and setting when a replacement conflicts', async () => {
    const store = new FakeStore()
    store.shortcuts.region = 'Command+Shift+L'
    const registrar = new FakeRegistrar()
    registrar.rejected.add('Command+Shift+R')
    const service = new ShortcutService(store, registrar, {
      region: vi.fn(),
      display: vi.fn(),
    })
    service.initialize()

    await expect(service.setShortcut('region', 'Command+Shift+R')).rejects.toThrow(
      ShortcutRegistrationError,
    )

    expect(store.writes).toEqual([])
    expect(registrar.callbacks.has('Command+Shift+L')).toBe(true)
    expect(service.getSnapshot().region.accelerator).toBe('Command+Shift+L')
  })

  it('registers before persisting, then releases the old shortcut', async () => {
    const store = new FakeStore()
    store.shortcuts.region = 'Command+Shift+L'
    const registrar = new FakeRegistrar()
    const service = new ShortcutService(store, registrar, {
      region: vi.fn(),
      display: vi.fn(),
    })
    service.initialize()

    await service.setShortcut('region', 'Command+Shift+R')

    expect(store.writes).toEqual([{ mode: 'region', accelerator: 'Command+Shift+R' }])
    expect(registrar.callbacks.has('Command+Shift+L')).toBe(false)
    expect(registrar.callbacks.has('Command+Shift+R')).toBe(true)
  })

  it('rolls back a new registration when persistence fails', async () => {
    const store = new FakeStore()
    store.writeFailure = new Error('disk full')
    const registrar = new FakeRegistrar()
    const service = new ShortcutService(store, registrar, {
      region: vi.fn(),
      display: vi.fn(),
    })

    await expect(service.setShortcut('region', 'Command+Shift+L')).rejects.toThrow('disk full')

    expect(registrar.callbacks.size).toBe(0)
    expect(service.getSnapshot().region.status).toBe('unconfigured')
  })

  it('clears a shortcut and suspends all registrations while recording', async () => {
    const store = new FakeStore()
    store.shortcuts.region = 'Command+Shift+L'
    const registrar = new FakeRegistrar()
    const service = new ShortcutService(store, registrar, {
      region: vi.fn(),
      display: vi.fn(),
    })
    service.initialize()

    service.setRecording(true)
    expect(registrar.suspended).toBe(true)
    service.setRecording(false)
    await service.setShortcut('region', null)

    expect(registrar.suspended).toBe(false)
    expect(registrar.callbacks.size).toBe(0)
    expect(service.getSnapshot().region.status).toBe('unconfigured')
  })
})

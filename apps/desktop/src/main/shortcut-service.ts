import type {
  CaptureMode,
  CaptureShortcuts,
  CaptureShortcutSnapshot,
} from '../shared/shortcut-command'

export interface ShortcutRegistrar {
  register(accelerator: string, callback: () => void): boolean
  unregister(accelerator: string): void
  setSuspended(suspended: boolean): void
}

export interface ShortcutSettingsStore {
  getCaptureShortcuts(): CaptureShortcuts
  setCaptureShortcut(mode: CaptureMode, accelerator: string | null): Promise<void>
}

type ShortcutHandlers = Record<CaptureMode, () => void | Promise<void>>

export class ShortcutService {
  private readonly registered: CaptureShortcuts = { region: null, display: null }

  public constructor(
    private readonly store: ShortcutSettingsStore,
    private readonly registrar: ShortcutRegistrar,
    private readonly handlers: ShortcutHandlers,
  ) {}

  public initialize(): void {
    const shortcuts = this.store.getCaptureShortcuts()
    for (const mode of ['region', 'display'] as const) {
      const accelerator = shortcuts[mode]
      if (accelerator && this.register(mode, accelerator)) {
        this.registered[mode] = accelerator
      }
    }
  }

  public getSnapshot(): CaptureShortcutSnapshot {
    const shortcuts = this.store.getCaptureShortcuts()
    return {
      region: this.snapshotFor('region', shortcuts.region),
      display: this.snapshotFor('display', shortcuts.display),
    }
  }

  public async setShortcut(mode: CaptureMode, accelerator: string | null): Promise<void> {
    const previous = this.store.getCaptureShortcuts()[mode]
    if (accelerator === previous && this.registered[mode] === accelerator) return

    if (accelerator) {
      const otherMode: CaptureMode = mode === 'region' ? 'display' : 'region'
      if (this.store.getCaptureShortcuts()[otherMode] === accelerator) {
        throw new ShortcutRegistrationError('That shortcut is already used by Lumiere.')
      }
      if (!this.register(mode, accelerator)) {
        throw new ShortcutRegistrationError('That shortcut is already used by another app.')
      }
    }

    try {
      await this.store.setCaptureShortcut(mode, accelerator)
    } catch (error) {
      if (accelerator) this.registrar.unregister(accelerator)
      throw error
    }

    const previousRegistration = this.registered[mode]
    if (previousRegistration && previousRegistration !== accelerator) {
      this.registrar.unregister(previousRegistration)
    }
    this.registered[mode] = accelerator
  }

  public setRecording(recording: boolean): void {
    this.registrar.setSuspended(recording)
  }

  public dispose(): void {
    this.registrar.setSuspended(false)
    for (const accelerator of Object.values(this.registered)) {
      if (accelerator) this.registrar.unregister(accelerator)
    }
    this.registered.region = null
    this.registered.display = null
  }

  private register(mode: CaptureMode, accelerator: string): boolean {
    return this.registrar.register(accelerator, () => {
      void this.handlers[mode]()
    })
  }

  private snapshotFor(mode: CaptureMode, accelerator: string | null) {
    return {
      accelerator,
      status: !accelerator
        ? ('unconfigured' as const)
        : this.registered[mode] === accelerator
          ? ('registered' as const)
          : ('unavailable' as const),
    }
  }
}

export class ShortcutRegistrationError extends Error {
  public constructor(message: string) {
    super(message)
    this.name = 'ShortcutRegistrationError'
  }
}

import type { MenuItemConstructorOptions } from 'electron'
import type { CaptureShortcutSnapshot } from '../shared/shortcut-command'

export interface ApplicationTrayState {
  regionAvailable: boolean
  displayAvailable: boolean
  shortcuts: CaptureShortcutSnapshot
}

export interface ApplicationTrayCommands {
  captureRegion(): void
  captureDisplay(): void
  showWindow(): void
  showSettings(): void
  quit(): void
}

export function applicationTrayMenuTemplate(
  state: ApplicationTrayState,
  commands: ApplicationTrayCommands,
): MenuItemConstructorOptions[] {
  return [
    captureItem('Capture region', 'region', state, () => {
      commands.captureRegion()
    }),
    captureItem('Capture display', 'display', state, () => {
      commands.captureDisplay()
    }),
    { type: 'separator' },
    {
      label: 'Open Lumiere',
      click: () => {
        commands.showWindow()
      },
    },
    {
      label: 'Settings…',
      click: () => {
        commands.showSettings()
      },
    },
    { type: 'separator' },
    {
      label: 'Quit Lumiere',
      click: () => {
        commands.quit()
      },
    },
  ]
}

function captureItem(
  label: string,
  mode: 'region' | 'display',
  state: ApplicationTrayState,
  click: () => void,
): MenuItemConstructorOptions {
  const shortcut = state.shortcuts[mode]
  return {
    label,
    enabled: mode === 'region' ? state.regionAvailable : state.displayAvailable,
    accelerator: shortcut.status === 'registered' ? (shortcut.accelerator ?? undefined) : undefined,
    registerAccelerator: false,
    click,
  }
}

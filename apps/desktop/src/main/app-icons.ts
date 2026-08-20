import { app, Menu, nativeImage, Tray } from 'electron'
import type { BrowserWindow } from 'electron'
import { resolveDesktopIconPaths, type DesktopIconPlatform } from './icon-paths'

function currentIconPlatform(): DesktopIconPlatform {
  if (process.platform === 'darwin' || process.platform === 'win32') {
    return process.platform
  }

  throw new Error(`Unsupported desktop icon platform: ${process.platform}`)
}

export function desktopIconPaths() {
  return resolveDesktopIconPaths({
    appPath: app.getAppPath(),
    isPackaged: app.isPackaged,
    platform: currentIconPlatform(),
    resourcesPath: process.resourcesPath,
  })
}

export function applyMacDockIcon(): void {
  if (process.platform !== 'darwin') {
    return
  }

  const icon = nativeImage.createFromPath(desktopIconPaths().appIcon)
  if (icon.isEmpty()) {
    throw new Error('The macOS Dock icon could not be loaded.')
  }
  app.dock?.setIcon(icon)
}

export function createApplicationTray(showWindow: () => BrowserWindow): Tray {
  const icon = nativeImage.createFromPath(desktopIconPaths().trayIcon)
  if (icon.isEmpty()) {
    throw new Error('The application tray icon could not be loaded.')
  }
  if (process.platform === 'darwin') {
    icon.setTemplateImage(true)
  }

  const tray = new Tray(icon)
  const show = (): void => {
    const window = showWindow()
    window.show()
    window.focus()
  }

  tray.setToolTip('Lumiere')
  tray.setContextMenu(
    Menu.buildFromTemplate([
      { label: 'Show Lumiere', click: show },
      { type: 'separator' },
      {
        label: 'Quit Lumiere',
        click: () => {
          app.quit()
        },
      },
    ]),
  )
  tray.on('click', show)
  return tray
}

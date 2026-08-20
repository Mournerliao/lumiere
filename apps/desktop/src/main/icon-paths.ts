import { posix, win32 } from 'node:path'

export type DesktopIconPlatform = 'darwin' | 'win32'

interface IconPathOptions {
  appPath: string
  isPackaged: boolean
  platform: DesktopIconPlatform
  resourcesPath: string
}

export interface DesktopIconPaths {
  appIcon: string
  trayIcon: string
}

export function resolveDesktopIconPaths(options: IconPathOptions): DesktopIconPaths {
  const path = options.platform === 'win32' ? win32 : posix
  const iconsRoot = options.isPackaged
    ? path.join(options.resourcesPath, 'icons')
    : path.join(options.appPath, 'resources', 'icons')

  if (options.platform === 'darwin') {
    return {
      appIcon: path.join(iconsRoot, 'mac', 'app-icon.png'),
      trayIcon: path.join(iconsRoot, 'mac', 'trayTemplate.png'),
    }
  }

  return {
    appIcon: path.join(iconsRoot, 'windows', 'app.ico'),
    trayIcon: path.join(iconsRoot, 'windows', 'tray.ico'),
  }
}

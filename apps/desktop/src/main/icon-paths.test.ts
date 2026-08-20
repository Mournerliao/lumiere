import { describe, expect, it } from 'vitest'
import { resolveDesktopIconPaths } from './icon-paths'

describe('resolveDesktopIconPaths', () => {
  it('uses project resources while developing on macOS', () => {
    expect(
      resolveDesktopIconPaths({
        appPath: '/workspace/apps/desktop',
        isPackaged: false,
        platform: 'darwin',
        resourcesPath: '/bundle/resources',
      }),
    ).toEqual({
      appIcon: '/workspace/apps/desktop/resources/icons/mac/app-icon.png',
      trayIcon: '/workspace/apps/desktop/resources/icons/mac/trayTemplate.png',
    })
  })

  it('uses unpacked resources beside a packaged Windows application', () => {
    expect(
      resolveDesktopIconPaths({
        appPath: 'C:\\Program Files\\Lumiere\\resources\\app.asar',
        isPackaged: true,
        platform: 'win32',
        resourcesPath: 'C:\\Program Files\\Lumiere\\resources',
      }),
    ).toEqual({
      appIcon: 'C:\\Program Files\\Lumiere\\resources\\icons\\windows\\app.ico',
      trayIcon: 'C:\\Program Files\\Lumiere\\resources\\icons\\windows\\tray.ico',
    })
  })
})

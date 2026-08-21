import { app, BrowserWindow, ipcMain, type Tray } from 'electron'
import { join } from 'node:path'
import { applyMacDockIcon, createApplicationTray, desktopIconPaths } from './app-icons'
import { MacOSPlatformHost } from './macos-platform-host'
import { macOSHostCandidates } from './native-host-paths'
import { createPlatformHandlers } from './platform-handlers'
import { currentLumierePlatform, UnavailablePlatformHost } from './platform-host'
import { platformChannels, type PlatformHost } from '../shared/platform-contract'

let mainWindow: BrowserWindow | null = null
let applicationTray: Tray | null = null
let platformHost: PlatformHost | null = null

function createMainWindow(): BrowserWindow {
  const window = new BrowserWindow({
    width: 960,
    height: 680,
    minWidth: 760,
    minHeight: 560,
    show: false,
    title: 'Lumiere',
    backgroundColor: '#1b1a18',
    ...(process.platform === 'darwin' ? { titleBarStyle: 'hiddenInset' as const } : {}),
    ...(process.platform === 'win32'
      ? {
          autoHideMenuBar: true,
          icon: desktopIconPaths().appIcon,
          titleBarStyle: 'hidden' as const,
          titleBarOverlay: {
            color: '#1b1a18',
            symbolColor: '#ece9e2',
            height: 46,
          },
        }
      : {}),
    webPreferences: {
      preload: join(__dirname, '../preload/index.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
    },
  })

  window.once('ready-to-show', () => {
    window.show()
  })
  window.webContents.setWindowOpenHandler(() => ({ action: 'deny' }))
  window.webContents.on('will-navigate', (event) => {
    event.preventDefault()
  })

  if (!app.isPackaged && process.env.ELECTRON_RENDERER_URL) {
    void window.loadURL(process.env.ELECTRON_RENDERER_URL)
  } else {
    void window.loadFile(join(__dirname, '../renderer/index.html'))
  }

  return window
}

function showMainWindow(): BrowserWindow {
  if (!mainWindow || mainWindow.isDestroyed()) {
    mainWindow = createMainWindow()
    registerPlatformIpc(mainWindow)
  }

  mainWindow.show()
  mainWindow.focus()
  return mainWindow
}

function registerPlatformIpc(window: BrowserWindow): void {
  platformHost ??= createPlatformHost()
  const handlers = createPlatformHandlers(platformHost)

  ipcMain.removeHandler(platformChannels.getCapabilities)
  ipcMain.removeHandler(platformChannels.capture)

  const assertTrustedSender = (event: Electron.IpcMainInvokeEvent): void => {
    if (event.sender.id !== window.webContents.id || event.senderFrame !== event.sender.mainFrame) {
      throw new Error('Rejected IPC from an untrusted renderer.')
    }
  }

  ipcMain.handle(platformChannels.getCapabilities, (event) => {
    assertTrustedSender(event)
    return handlers.getCapabilities()
  })

  ipcMain.handle(platformChannels.capture, (event, request: unknown) => {
    assertTrustedSender(event)
    return handlers.capture(request)
  })
}

function createPlatformHost(): PlatformHost {
  const platform = currentLumierePlatform()
  if (platform !== 'macos') {
    return new UnavailablePlatformHost(platform)
  }

  return new MacOSPlatformHost(
    macOSHostCandidates({
      appPath: app.getAppPath(),
      isPackaged: app.isPackaged,
      resourcesPath: process.resourcesPath,
      overridePath: process.env.LUMIERE_MAC_HOST_PATH,
    }),
  )
}

void app.whenReady().then(() => {
  mainWindow = createMainWindow()
  registerPlatformIpc(mainWindow)
  applyMacDockIcon()
  applicationTray = createApplicationTray(showMainWindow)

  app.on('activate', () => {
    showMainWindow()
  })
})

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    applicationTray?.destroy()
    app.quit()
  }
})

app.on('before-quit', () => {
  if (platformHost instanceof MacOSPlatformHost) {
    platformHost.dispose()
  }
})

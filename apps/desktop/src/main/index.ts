import { app, BrowserWindow, ipcMain, type Tray } from 'electron'
import { join } from 'node:path'
import { applyMacDockIcon, createApplicationTray, desktopIconPaths } from './app-icons'
import { CaptureCommandRouter } from './capture-command-router'
import { MacOSPlatformHost } from './macos-platform-host'
import { macOSHostCandidates } from './native-host-paths'
import { currentLumierePlatform, UnavailablePlatformHost } from './platform-host'
import { captureCommandChannels } from '../shared/capture-command'
import type { PlatformHost } from '../shared/platform-contract'

let mainWindow: BrowserWindow | null = null
let applicationTray: Tray | null = null
let platformHost: PlatformHost | null = null
let captureRouter: CaptureCommandRouter | null = null

function createMainWindow(): BrowserWindow {
  const window = new BrowserWindow({
    width: 480,
    height: 370,
    minWidth: 440,
    minHeight: 340,
    show: false,
    title: 'Lumiere',
    backgroundColor: '#1f1d1b',
    ...(process.platform === 'darwin' ? { titleBarStyle: 'hiddenInset' as const } : {}),
    ...(process.platform === 'win32'
      ? {
          autoHideMenuBar: true,
          icon: desktopIconPaths().appIcon,
          titleBarStyle: 'hidden' as const,
          titleBarOverlay: {
            color: '#1f1d1b',
            symbolColor: '#f0ede6',
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
  const router = (captureRouter ??= new CaptureCommandRouter(
    currentLumierePlatform(),
    platformHost,
  ))

  ipcMain.removeHandler(captureCommandChannels.getSurfaceSnapshot)
  ipcMain.removeHandler(captureCommandChannels.captureDisplay)

  const assertTrustedSender = (event: Electron.IpcMainInvokeEvent): void => {
    if (event.sender.id !== window.webContents.id || event.senderFrame !== event.sender.mainFrame) {
      throw new Error('Rejected IPC from an untrusted renderer.')
    }
  }

  ipcMain.handle(captureCommandChannels.getSurfaceSnapshot, (event) => {
    assertTrustedSender(event)
    return router.getSurfaceSnapshot()
  })

  ipcMain.handle(captureCommandChannels.captureDisplay, (event) => {
    assertTrustedSender(event)
    return router.capture('display')
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

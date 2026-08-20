import { app, BrowserWindow, ipcMain, type Tray } from 'electron'
import { join } from 'node:path'
import { applyMacDockIcon, createApplicationTray, desktopIconPaths } from './app-icons'
import { createPlatformHandlers } from './platform-handlers'
import { currentLumierePlatform, UnavailablePlatformHost } from './platform-host'
import { platformChannels } from '../shared/platform-contract'

let mainWindow: BrowserWindow | null = null
let applicationTray: Tray | null = null

function createMainWindow(): BrowserWindow {
  const window = new BrowserWindow({
    width: 960,
    height: 680,
    minWidth: 760,
    minHeight: 560,
    show: false,
    title: 'Lumiere',
    backgroundColor: '#f4f2ed',
    ...(process.platform === 'win32' ? { icon: desktopIconPaths().appIcon } : {}),
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
  const host = new UnavailablePlatformHost(currentLumierePlatform())
  const handlers = createPlatformHandlers(host)

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

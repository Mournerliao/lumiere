import { app, BrowserWindow, ipcMain, type Tray } from 'electron'
import { join } from 'node:path'
import { applyMacDockIcon, createApplicationTray, desktopIconPaths } from './app-icons'
import { CaptureCommandRouter } from './capture-command-router'
import { MacOSPlatformHost } from './macos-platform-host'
import { macOSHostCandidates } from './native-host-paths'
import { currentLumierePlatform, UnavailablePlatformHost } from './platform-host'
import { SettingsStore } from './settings-store'
import { captureCommandChannels } from '../shared/capture-command'
import {
  availableOutputDeliveries,
  parseOutputDelivery,
  settingsCommandChannels,
  type SettingsSnapshot,
} from '../shared/settings-command'
import type { PlatformHost } from '../shared/platform-contract'

let mainWindow: BrowserWindow | null = null
let settingsWindow: BrowserWindow | null = null
let applicationTray: Tray | null = null
let platformHost: PlatformHost | null = null
let captureRouter: CaptureCommandRouter | null = null
let settingsStore: SettingsStore | null = null

function createRendererWindow(kind: 'main' | 'settings'): BrowserWindow {
  const isSettings = kind === 'settings'
  const window = new BrowserWindow({
    width: isSettings ? 640 : 480,
    height: isSettings ? 340 : 370,
    minWidth: isSettings ? 600 : 440,
    minHeight: isSettings ? 300 : 340,
    show: false,
    title: isSettings ? 'Lumiere Settings' : 'Lumiere',
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
    const rendererUrl = new URL(process.env.ELECTRON_RENDERER_URL)
    rendererUrl.hash = isSettings ? 'settings' : ''
    void window.loadURL(rendererUrl.toString())
  } else {
    void window.loadFile(join(__dirname, '../renderer/index.html'), {
      hash: isSettings ? 'settings' : '',
    })
  }

  return window
}

function showMainWindow(): BrowserWindow {
  if (!mainWindow || mainWindow.isDestroyed()) {
    mainWindow = createRendererWindow('main')
  }

  mainWindow.show()
  mainWindow.focus()
  return mainWindow
}

function showSettingsWindow(): BrowserWindow {
  if (!settingsWindow || settingsWindow.isDestroyed()) {
    settingsWindow = createRendererWindow('settings')
    settingsWindow.on('closed', () => {
      settingsWindow = null
    })
  }

  settingsWindow.show()
  settingsWindow.focus()
  return settingsWindow
}

function registerIpc(): void {
  platformHost ??= createPlatformHost()
  settingsStore ??= new SettingsStore(join(app.getPath('userData'), 'settings.json'))
  const router = (captureRouter ??= new CaptureCommandRouter(
    currentLumierePlatform(),
    platformHost,
    settingsStore,
  ))

  ipcMain.removeHandler(captureCommandChannels.getSurfaceSnapshot)
  ipcMain.removeHandler(captureCommandChannels.captureDisplay)
  ipcMain.removeHandler(settingsCommandChannels.openWindow)
  ipcMain.removeHandler(settingsCommandChannels.getSnapshot)
  ipcMain.removeHandler(settingsCommandChannels.setOutputDelivery)

  const assertTrustedSender = (event: Electron.IpcMainInvokeEvent): void => {
    const trustedContents = [mainWindow, settingsWindow]
      .filter((window): window is BrowserWindow => window !== null && !window.isDestroyed())
      .map((window) => window.webContents)
    if (
      !trustedContents.some((contents) => contents.id === event.sender.id) ||
      event.senderFrame !== event.sender.mainFrame
    ) {
      throw new Error('Rejected IPC from an untrusted renderer.')
    }
  }

  const assertNoArguments = (args: readonly unknown[]): void => {
    if (args.length !== 0) {
      throw new Error('Rejected unexpected IPC arguments.')
    }
  }

  ipcMain.handle(captureCommandChannels.getSurfaceSnapshot, (event, ...args) => {
    assertTrustedSender(event)
    assertNoArguments(args)
    return router.getSurfaceSnapshot()
  })

  ipcMain.handle(captureCommandChannels.captureDisplay, (event, ...args) => {
    assertTrustedSender(event)
    assertNoArguments(args)
    return router.captureDisplay()
  })

  ipcMain.handle(settingsCommandChannels.openWindow, (event, ...args) => {
    assertTrustedSender(event)
    assertNoArguments(args)
    showSettingsWindow()
  })

  ipcMain.handle(settingsCommandChannels.getSnapshot, (event, ...args) => {
    assertTrustedSender(event)
    assertNoArguments(args)
    return getSettingsSnapshot()
  })

  ipcMain.handle(settingsCommandChannels.setOutputDelivery, async (event, ...args) => {
    assertTrustedSender(event)
    if (args.length !== 1) {
      throw new Error('Expected one output-delivery argument.')
    }
    const delivery = parseOutputDelivery(args[0])
    const snapshot = await getSettingsSnapshot()
    if (!snapshot.availableOutputDeliveries.includes(delivery)) {
      throw new Error('The selected output destination is unavailable.')
    }

    if (!settingsStore) {
      throw new Error('Settings are not ready.')
    }
    await settingsStore.setOutputDelivery(delivery)
    const nextSnapshot = await getSettingsSnapshot()
    broadcastSettingsChanged(nextSnapshot)
    return nextSnapshot
  })
}

async function getSettingsSnapshot(): Promise<SettingsSnapshot> {
  if (!platformHost || !settingsStore) {
    throw new Error('Settings are not ready.')
  }
  const capabilities = await platformHost.getCapabilities()
  return {
    outputDelivery: settingsStore.getOutputDelivery(),
    availableOutputDeliveries: availableOutputDeliveries(capabilities.deliveryTargets),
  }
}

function broadcastSettingsChanged(snapshot: SettingsSnapshot): void {
  for (const window of [mainWindow, settingsWindow]) {
    if (window && !window.isDestroyed()) {
      window.webContents.send(settingsCommandChannels.changed, snapshot)
    }
  }
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

void app.whenReady().then(async () => {
  platformHost = createPlatformHost()
  settingsStore = new SettingsStore(join(app.getPath('userData'), 'settings.json'))
  await settingsStore.load()
  mainWindow = createRendererWindow('main')
  registerIpc()
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

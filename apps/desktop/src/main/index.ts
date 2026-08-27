import { app, BrowserWindow, globalShortcut, ipcMain, screen } from 'electron'
import { join } from 'node:path'
import {
  applyMacDockIcon,
  createApplicationTray,
  desktopIconPaths,
  type ApplicationTray,
} from './app-icons'
import { CaptureCommandRouter } from './capture-command-router'
import { MacOSPlatformHost } from './macos-platform-host'
import { macOSHostCandidates } from './native-host-paths'
import { currentLumierePlatform, UnavailablePlatformHost } from './platform-host'
import { SettingsStore } from './settings-store'
import { ShortcutRegistrationError, ShortcutService } from './shortcut-service'
import {
  captureCommandChannels,
  type CaptureCommandResult,
  type RegionOverlaySnapshot,
} from '../shared/capture-command'
import {
  availableOutputDeliveries,
  parseOutputDelivery,
  settingsCommandChannels,
  type SettingsSnapshot,
} from '../shared/settings-command'
import { parseShortcutUpdate } from '../shared/shortcut-command'
import {
  parseCaptureGeometry,
  type CaptureGeometry,
  type CaptureTarget,
  type PlatformHost,
} from '../shared/platform-contract'

let mainWindow: BrowserWindow | null = null
let applicationTray: ApplicationTray | null = null
let platformHost: PlatformHost | null = null
let captureRouter: CaptureCommandRouter | null = null
let settingsStore: SettingsStore | null = null
let shortcutService: ShortcutService | null = null
let regionOverlaySession: RegionOverlaySession | null = null

interface RegionOverlaySession {
  window: BrowserWindow
  target: CaptureTarget
  submitted: boolean
  resolve(result: CaptureCommandResult): void
  stopWatchingDisplays(): void
  restoreMainWindow: boolean
}

function createRendererWindow(): BrowserWindow {
  const window = new BrowserWindow({
    width: 480,
    height: 370,
    minWidth: 440,
    minHeight: 340,
    show: false,
    title: 'Lumiere',
    backgroundColor: '#1f1d1b',
    ...(process.platform === 'darwin'
      ? {
          titleBarStyle: 'hiddenInset' as const,
          trafficLightPosition: { x: 12, y: 15 },
        }
      : {}),
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
  window.once('closed', () => {
    shortcutService?.setRecording(false)
    if (mainWindow === window) mainWindow = null
  })

  if (!app.isPackaged && process.env.ELECTRON_RENDERER_URL) {
    void window.loadURL(process.env.ELECTRON_RENDERER_URL)
  } else {
    void window.loadFile(join(__dirname, '../renderer/index.html'))
  }

  return window
}

function createRegionOverlayWindow(bounds: Electron.Rectangle): BrowserWindow {
  const window = new BrowserWindow({
    ...bounds,
    show: false,
    frame: false,
    transparent: true,
    backgroundColor: '#00000000',
    resizable: false,
    movable: false,
    minimizable: false,
    maximizable: false,
    fullscreenable: false,
    skipTaskbar: true,
    alwaysOnTop: true,
    hasShadow: false,
    enableLargerThanScreen: true,
    webPreferences: {
      preload: join(__dirname, '../preload/index.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
    },
  })

  window.setContentProtection(true)
  if (process.platform === 'darwin') {
    window.setVisibleOnAllWorkspaces(true, { visibleOnFullScreen: true })
    window.setAlwaysOnTop(true, 'screen-saver')
  }
  window.webContents.setWindowOpenHandler(() => ({ action: 'deny' }))
  window.webContents.on('will-navigate', (event) => {
    event.preventDefault()
  })

  if (!app.isPackaged && process.env.ELECTRON_RENDERER_URL) {
    const rendererUrl = new URL(process.env.ELECTRON_RENDERER_URL)
    rendererUrl.searchParams.set('surface', 'region-overlay')
    void window.loadURL(rendererUrl.toString())
  } else {
    void window.loadFile(join(__dirname, '../renderer/index.html'), {
      query: { surface: 'region-overlay' },
    })
  }

  return window
}

function showMainWindow(): BrowserWindow {
  if (!mainWindow || mainWindow.isDestroyed()) {
    mainWindow = createRendererWindow()
  }

  mainWindow.show()
  mainWindow.focus()
  return mainWindow
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
  ipcMain.removeHandler(captureCommandChannels.captureRegion)
  ipcMain.removeHandler(captureCommandChannels.getRegionOverlaySnapshot)
  ipcMain.removeAllListeners(captureCommandChannels.cancelRegionOverlay)
  ipcMain.removeAllListeners(captureCommandChannels.submitRegionSelection)
  ipcMain.removeHandler(settingsCommandChannels.getSnapshot)
  ipcMain.removeHandler(settingsCommandChannels.setCaptureShortcut)
  ipcMain.removeHandler(settingsCommandChannels.setOutputDelivery)
  ipcMain.removeHandler(settingsCommandChannels.setShortcutRecording)

  const assertTrustedWindow = (
    event: Electron.IpcMainEvent | Electron.IpcMainInvokeEvent,
    trustedWindow: BrowserWindow | null,
  ): void => {
    if (
      !trustedWindow ||
      trustedWindow.isDestroyed() ||
      trustedWindow.webContents.id !== event.sender.id ||
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
    assertTrustedWindow(event, mainWindow)
    assertNoArguments(args)
    return router.getSurfaceSnapshot()
  })

  ipcMain.handle(captureCommandChannels.captureDisplay, async (event, ...args) => {
    assertTrustedWindow(event, mainWindow)
    assertNoArguments(args)
    const result = await router.captureDisplay()
    await refreshApplicationTray()
    return result
  })

  ipcMain.handle(captureCommandChannels.captureRegion, async (event, ...args) => {
    assertTrustedWindow(event, mainWindow)
    assertNoArguments(args)
    const result = await captureRegion(router)
    await refreshApplicationTray()
    return result
  })

  ipcMain.handle(captureCommandChannels.getRegionOverlaySnapshot, (event, ...args) => {
    assertTrustedWindow(event, regionOverlaySession?.window ?? null)
    assertNoArguments(args)
    const target = regionOverlaySession?.target
    if (!target) {
      throw new Error('Region overlay is not ready.')
    }
    return { targetSize: target.logicalSize } satisfies RegionOverlaySnapshot
  })

  ipcMain.on(captureCommandChannels.cancelRegionOverlay, (event, ...args) => {
    try {
      assertTrustedWindow(event, regionOverlaySession?.window ?? null)
      assertNoArguments(args)
    } catch {
      return
    }
    cancelRegionOverlay(router)
  })

  ipcMain.on(captureCommandChannels.submitRegionSelection, (event, ...args) => {
    try {
      assertTrustedWindow(event, regionOverlaySession?.window ?? null)
      if (args.length !== 1) {
        return
      }
      const geometry = parseCaptureGeometry(args[0])
      submitRegionSelection(router, geometry)
    } catch {
      return
    }
  })

  ipcMain.handle(settingsCommandChannels.getSnapshot, (event, ...args) => {
    assertTrustedWindow(event, mainWindow)
    assertNoArguments(args)
    return getSettingsSnapshot()
  })

  ipcMain.handle(settingsCommandChannels.setOutputDelivery, async (event, ...args) => {
    assertTrustedWindow(event, mainWindow)
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

  ipcMain.handle(settingsCommandChannels.setCaptureShortcut, async (event, ...args) => {
    assertTrustedWindow(event, mainWindow)
    if (args.length !== 1) {
      throw new Error('Expected one shortcut update.')
    }
    const update = parseShortcutUpdate(args[0])
    if (!shortcutService) {
      throw new Error('Shortcut settings are not ready.')
    }
    try {
      await shortcutService.setShortcut(update.mode, update.accelerator)
    } catch (error) {
      return {
        status: 'failed' as const,
        message:
          error instanceof ShortcutRegistrationError
            ? error.message
            : 'The shortcut could not be saved. Try again.',
      }
    }
    const nextSnapshot = await getSettingsSnapshot()
    broadcastSettingsChanged(nextSnapshot)
    await refreshApplicationTray()
    return { status: 'success' as const, snapshot: nextSnapshot }
  })

  ipcMain.handle(settingsCommandChannels.setShortcutRecording, (event, ...args) => {
    assertTrustedWindow(event, mainWindow)
    if (args.length !== 1 || typeof args[0] !== 'boolean') {
      throw new Error('Expected one shortcut-recording state.')
    }
    shortcutService?.setRecording(args[0])
  })
}

async function captureRegion(router: CaptureCommandRouter): Promise<CaptureCommandResult> {
  const restoreMainWindow = mainWindow?.isVisible() === true
  const initialDisplay = screen.getDisplayNearestPoint(screen.getCursorScreenPoint())
  const preparation = await router.beginRegionCapture()
  if (preparation.status === 'failed') {
    return preparation.result
  }

  const display = screen.getDisplayNearestPoint(screen.getCursorScreenPoint())
  if (
    display.id !== initialDisplay.id ||
    !displayMatchesTarget(display.bounds, preparation.target)
  ) {
    router.cancelRegionCapture()
    return displayChangedResult()
  }

  let overlay: BrowserWindow
  try {
    overlay = createRegionOverlayWindow(display.bounds)
  } catch {
    router.cancelRegionCapture()
    return {
      status: 'failed',
      feedback: 'Capture failed',
      notice: {
        tone: 'critical',
        title: 'Capture failed',
        detail: 'Try again. Restart Lumiere if the issue continues.',
      },
    }
  }
  mainWindow?.hide()
  return new Promise<CaptureCommandResult>((resolve) => {
    const handleDisplayChange = (): void => {
      failRegionOverlay(router, displayChangedResult())
    }
    screen.on('display-added', handleDisplayChange)
    screen.on('display-removed', handleDisplayChange)
    screen.on('display-metrics-changed', handleDisplayChange)
    const stopWatchingDisplays = (): void => {
      screen.removeListener('display-added', handleDisplayChange)
      screen.removeListener('display-removed', handleDisplayChange)
      screen.removeListener('display-metrics-changed', handleDisplayChange)
    }

    regionOverlaySession = {
      window: overlay,
      target: preparation.target,
      submitted: false,
      resolve,
      stopWatchingDisplays,
      restoreMainWindow,
    }
    overlay.once('ready-to-show', () => {
      overlay.show()
      overlay.focus()
    })
    overlay.webContents.once('did-fail-load', () => {
      failRegionOverlay(router, {
        status: 'failed',
        feedback: 'Capture failed',
        notice: {
          tone: 'critical',
          title: 'Capture failed',
          detail: 'Try again. Restart Lumiere if the issue continues.',
        },
      })
    })
    overlay.once('closed', () => {
      if (regionOverlaySession?.window === overlay && !regionOverlaySession.submitted) {
        cancelRegionOverlay(router)
      }
    })
  })
}

function submitRegionSelection(router: CaptureCommandRouter, geometry: CaptureGeometry): void {
  const session = regionOverlaySession
  if (!session || session.submitted) {
    return
  }
  if (!geometryFitsTarget(geometry, session.target)) {
    failRegionOverlay(router, displayChangedResult())
    return
  }

  session.submitted = true
  session.window.hide()
  session.window.destroy()
  setTimeout(() => {
    void router.completeRegionCapture(session.target, geometry).then((result) => {
      finishRegionOverlay(session, result)
    })
  }, 50)
}

function cancelRegionOverlay(router: CaptureCommandRouter): void {
  const session = regionOverlaySession
  if (!session || session.submitted) {
    return
  }
  router.cancelRegionCapture()
  finishRegionOverlay(session, { status: 'cancelled', feedback: 'Capture cancelled' })
}

function failRegionOverlay(router: CaptureCommandRouter, result: CaptureCommandResult): void {
  const session = regionOverlaySession
  if (!session || session.submitted) {
    return
  }
  router.cancelRegionCapture()
  finishRegionOverlay(session, result)
}

function finishRegionOverlay(session: RegionOverlaySession, result: CaptureCommandResult): void {
  if (regionOverlaySession !== session) {
    return
  }
  regionOverlaySession = null
  session.stopWatchingDisplays()
  if (!session.window.isDestroyed()) {
    session.window.destroy()
  }
  if (session.restoreMainWindow) showMainWindow()
  session.resolve(result)
}

function displayMatchesTarget(bounds: Electron.Rectangle, target: CaptureTarget): boolean {
  return (
    Math.abs(bounds.width - target.logicalSize.width) <= 1 &&
    Math.abs(bounds.height - target.logicalSize.height) <= 1
  )
}

function geometryFitsTarget(geometry: CaptureGeometry, target: CaptureTarget): boolean {
  return (
    geometry.x + geometry.width <= target.logicalSize.width + Number.EPSILON &&
    geometry.y + geometry.height <= target.logicalSize.height + Number.EPSILON
  )
}

function displayChangedResult(): CaptureCommandResult {
  return {
    status: 'failed',
    feedback: 'Capture failed',
    notice: {
      tone: 'caution',
      title: 'Capture failed',
      detail: 'The display may have changed. Try again.',
    },
  }
}

function disposeRegionOverlay(router: CaptureCommandRouter): void {
  const session = regionOverlaySession
  if (!session) {
    return
  }
  regionOverlaySession = null
  router.cancelRegionCapture()
  session.stopWatchingDisplays()
  if (!session.window.isDestroyed()) {
    session.window.destroy()
  }
}

async function getSettingsSnapshot(): Promise<SettingsSnapshot> {
  if (!platformHost || !settingsStore) {
    throw new Error('Settings are not ready.')
  }
  const capabilities = await platformHost.getCapabilities()
  return {
    outputDelivery: settingsStore.getOutputDelivery(),
    availableOutputDeliveries: availableOutputDeliveries(capabilities.deliveryTargets),
    captureShortcuts: shortcutService?.getSnapshot() ?? {
      region: { accelerator: null, status: 'unconfigured' },
      display: { accelerator: null, status: 'unconfigured' },
    },
  }
}

function broadcastSettingsChanged(snapshot: SettingsSnapshot): void {
  for (const window of [mainWindow]) {
    if (window && !window.isDestroyed()) {
      window.webContents.send(settingsCommandChannels.changed, snapshot)
    }
  }
}

function broadcastCaptureCompleted(result: CaptureCommandResult): void {
  if (mainWindow && !mainWindow.isDestroyed()) {
    mainWindow.webContents.send(captureCommandChannels.completed, result)
  }
}

async function runExternalCapture(mode: 'region' | 'display'): Promise<void> {
  const router = captureRouter
  if (!router) return
  const result = mode === 'region' ? await captureRegion(router) : await router.captureDisplay()
  broadcastCaptureCompleted(result)
  await refreshApplicationTray()
}

function showSettingsWindow(): void {
  const window = showMainWindow()
  const send = (): void => {
    if (!window.isDestroyed()) {
      window.webContents.send(settingsCommandChannels.showRequested)
    }
  }
  if (window.webContents.isLoadingMainFrame()) {
    window.webContents.once('did-finish-load', send)
  } else {
    send()
  }
}

async function getApplicationTrayState() {
  const shortcuts = shortcutService?.getSnapshot() ?? {
    region: { accelerator: null, status: 'unconfigured' as const },
    display: { accelerator: null, status: 'unconfigured' as const },
  }
  try {
    const snapshot = await captureRouter?.getSurfaceSnapshot()
    return {
      regionAvailable: snapshot?.hostAvailable === true && snapshot.captureModes.includes('region'),
      displayAvailable:
        snapshot?.hostAvailable === true && snapshot.captureModes.includes('display'),
      shortcuts,
    }
  } catch {
    return { regionAvailable: false, displayAvailable: false, shortcuts }
  }
}

async function refreshApplicationTray(): Promise<void> {
  applicationTray?.update(await getApplicationTrayState())
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
  mainWindow = createRendererWindow()
  registerIpc()
  if (!captureRouter) {
    throw new Error('Capture commands are not ready.')
  }
  shortcutService = new ShortcutService(settingsStore, globalShortcut, {
    region: () => runExternalCapture('region'),
    display: () => runExternalCapture('display'),
  })
  shortcutService.initialize()
  applyMacDockIcon()
  applicationTray = createApplicationTray(await getApplicationTrayState(), {
    captureRegion: () => {
      void runExternalCapture('region')
    },
    captureDisplay: () => {
      void runExternalCapture('display')
    },
    showWindow: showMainWindow,
    showSettings: showSettingsWindow,
    quit: () => {
      app.quit()
    },
  })

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
  shortcutService?.dispose()
  if (captureRouter) {
    disposeRegionOverlay(captureRouter)
  }
  if (platformHost instanceof MacOSPlatformHost) {
    platformHost.dispose()
  }
})

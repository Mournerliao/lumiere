import {
  app,
  BrowserWindow,
  dialog,
  globalShortcut,
  ipcMain,
  protocol,
  screen,
  shell,
} from 'electron'
import { readFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import {
  applyMacDockIcon,
  createApplicationTray,
  desktopIconPaths,
  type ApplicationTray,
} from './app-icons'
import { CaptureCommandRouter } from './capture-command-router'
import { MacOSPlatformHost } from './macos-platform-host'
import { macOSHostCandidates, windowsHostCandidates } from './native-host-paths'
import { NativeProcessPlatformHost } from './native-process-platform-host'
import { currentLumierePlatform } from './platform-host'
import { WindowsPlatformHost } from './windows-platform-host'
import { SettingsStore } from './settings-store'
import { ShortcutRegistrationError, ShortcutService } from './shortcut-service'
import { applyAfterCaptureBehavior } from './after-capture'
import { RegionPreviewRegistry, regionPreviewScheme } from './region-preview-registry'
import {
  captureCommandChannels,
  type CaptureCommandResult,
  type RegionOverlaySnapshot,
} from '../shared/capture-command'
import {
  availableOutputDeliveries,
  parseAfterCaptureBehavior,
  parseHdrStatusReminders,
  parseOutputDelivery,
  settingsCommandChannels,
  type SettingsSnapshot,
} from '../shared/settings-command'
import { parseShortcutUpdate } from '../shared/shortcut-command'
import { configureWindowsUpdates } from './windows-updater'
import {
  parseCaptureGeometry,
  type CaptureGeometry,
  type LogicalSize,
  type PlatformHost,
} from '../shared/platform-contract'

protocol.registerSchemesAsPrivileged([
  {
    scheme: regionPreviewScheme,
    privileges: { standard: true, secure: true, supportFetchAPI: true },
  },
])

const regionPreviewDirectory = join(tmpdir(), 'lumiere-region-preview')
const regionPreviewRegistry = new RegionPreviewRegistry(regionPreviewDirectory)

if (process.platform === 'win32') {
  app.setAppUserModelId('io.github.sousouliao.lumiere')
}

let mainWindow: BrowserWindow | null = null
let applicationTray: ApplicationTray | null = null
let platformHost: PlatformHost | null = null
let captureRouter: CaptureCommandRouter | null = null
let settingsStore: SettingsStore | null = null
let shortcutService: ShortcutService | null = null
let regionOverlaySession: RegionOverlaySession | null = null

interface RegionOverlaySession {
  window: BrowserWindow
  targetSize: LogicalSize
  previewToken: string
  previewUrl: string
  leaseTimeout: NodeJS.Timeout
  ready: boolean
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
    transparent: false,
    backgroundColor: '#000000',
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

function registerRegionPreviewProtocol(): void {
  protocol.handle(regionPreviewScheme, async (request) => {
    if (request.method !== 'GET') {
      return new Response(null, { status: 405 })
    }
    const filePath = regionPreviewRegistry.resolve(request.url)
    if (!filePath) {
      return new Response(null, { status: 404 })
    }
    try {
      return new Response(await readFile(filePath), {
        status: 200,
        headers: {
          'Content-Type': 'image/png',
          'Cache-Control': 'no-store',
        },
      })
    } catch {
      return new Response(null, { status: 410 })
    }
  })
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
  ipcMain.removeAllListeners(captureCommandChannels.regionOverlayReady)
  ipcMain.removeAllListeners(captureCommandChannels.cancelRegionOverlay)
  ipcMain.removeAllListeners(captureCommandChannels.submitRegionSelection)
  ipcMain.removeHandler(settingsCommandChannels.getSnapshot)
  ipcMain.removeHandler(settingsCommandChannels.chooseSaveDirectory)
  ipcMain.removeHandler(settingsCommandChannels.setAfterCaptureBehavior)
  ipcMain.removeHandler(settingsCommandChannels.setCaptureShortcut)
  ipcMain.removeHandler(settingsCommandChannels.setHdrStatusReminders)
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
    const result = completeCapture(await router.captureDisplay())
    await refreshApplicationTray()
    return result
  })

  ipcMain.handle(captureCommandChannels.captureRegion, async (event, ...args) => {
    assertTrustedWindow(event, mainWindow)
    assertNoArguments(args)
    const result = completeCapture(await captureRegion(router))
    await refreshApplicationTray()
    return result
  })

  ipcMain.handle(captureCommandChannels.getRegionOverlaySnapshot, (event, ...args) => {
    assertTrustedWindow(event, regionOverlaySession?.window ?? null)
    assertNoArguments(args)
    const session = regionOverlaySession
    if (!session) {
      throw new Error('Region overlay is not ready.')
    }
    return {
      targetSize: session.targetSize,
      previewUrl: session.previewUrl,
    } satisfies RegionOverlaySnapshot
  })

  ipcMain.on(captureCommandChannels.regionOverlayReady, (event, ...args) => {
    try {
      assertTrustedWindow(event, regionOverlaySession?.window ?? null)
      assertNoArguments(args)
    } catch {
      return
    }
    const session = regionOverlaySession
    if (!session || session.ready || session.window.isDestroyed()) return
    session.ready = true
    session.window.show()
    session.window.focus()
  })

  ipcMain.on(captureCommandChannels.cancelRegionOverlay, (event, ...args) => {
    try {
      assertTrustedWindow(event, regionOverlaySession?.window ?? null)
      assertNoArguments(args)
    } catch {
      return
    }
    void cancelRegionOverlay(router)
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

  ipcMain.handle(settingsCommandChannels.chooseSaveDirectory, async (event, ...args) => {
    assertTrustedWindow(event, mainWindow)
    assertNoArguments(args)
    if (!settingsStore || !mainWindow) {
      throw new Error('Settings are not ready.')
    }
    const result = await dialog.showOpenDialog(mainWindow, {
      title: 'Choose save folder',
      defaultPath: settingsStore.getSaveDirectory() ?? defaultCaptureDirectory(),
      buttonLabel: 'Choose',
      properties: ['openDirectory', 'createDirectory'],
    })
    if (result.canceled || result.filePaths.length !== 1) {
      return getSettingsSnapshot()
    }
    await settingsStore.setSaveDirectory(result.filePaths[0])
    const nextSnapshot = await getSettingsSnapshot()
    broadcastSettingsChanged(nextSnapshot)
    return nextSnapshot
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

  ipcMain.handle(settingsCommandChannels.setAfterCaptureBehavior, async (event, ...args) => {
    assertTrustedWindow(event, mainWindow)
    if (args.length !== 1) {
      throw new Error('Expected one after-capture behavior argument.')
    }
    const behavior = parseAfterCaptureBehavior(args[0])
    if (!settingsStore) {
      throw new Error('Settings are not ready.')
    }
    await settingsStore.setAfterCaptureBehavior(behavior)
    const nextSnapshot = await getSettingsSnapshot()
    broadcastSettingsChanged(nextSnapshot)
    return nextSnapshot
  })

  ipcMain.handle(settingsCommandChannels.setHdrStatusReminders, async (event, ...args) => {
    assertTrustedWindow(event, mainWindow)
    if (args.length !== 1) {
      throw new Error('Expected one HDR status reminder argument.')
    }
    const enabled = parseHdrStatusReminders(args[0])
    if (!settingsStore) {
      throw new Error('Settings are not ready.')
    }
    await settingsStore.setHdrStatusReminders(enabled)
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
  mainWindow?.hide()
  const preparation = await router.beginRegionCapture()
  if (preparation.status === 'failed') {
    if (restoreMainWindow) showMainWindow()
    return preparation.result
  }

  const display = screen.getDisplayNearestPoint(screen.getCursorScreenPoint())
  if (
    display.id !== initialDisplay.id ||
    !displayMatchesTarget(display.bounds, preparation.targetSize)
  ) {
    await router.cancelRegionCapture()
    if (restoreMainWindow) showMainWindow()
    return displayChangedResult()
  }

  let overlay: BrowserWindow
  let preview: { token: string; url: string }
  try {
    preview = regionPreviewRegistry.grant(preparation.previewPath)
    overlay = createRegionOverlayWindow(display.bounds)
  } catch {
    await router.cancelRegionCapture()
    if (restoreMainWindow) showMainWindow()
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
  return new Promise<CaptureCommandResult>((resolve) => {
    const handleDisplayChange = (): void => {
      void failRegionOverlay(router, displayChangedResult())
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
      targetSize: preparation.targetSize,
      previewToken: preview.token,
      previewUrl: preview.url,
      leaseTimeout: setTimeout(() => {
        void failRegionOverlay(router, {
          status: 'failed',
          feedback: 'Capture timed out',
          notice: {
            tone: 'caution',
            title: 'Capture timed out',
            detail: 'Start a new capture and select a region sooner.',
          },
        })
      }, preparation.leaseMilliseconds),
      ready: false,
      submitted: false,
      resolve,
      stopWatchingDisplays,
      restoreMainWindow,
    }
    overlay.webContents.once('did-fail-load', () => {
      void failRegionOverlay(router, {
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
        void cancelRegionOverlay(router)
      }
    })
  })
}

function submitRegionSelection(router: CaptureCommandRouter, geometry: CaptureGeometry): void {
  const session = regionOverlaySession
  if (!session || session.submitted) {
    return
  }
  if (!geometryFitsTarget(geometry, session.targetSize)) {
    void failRegionOverlay(router, displayChangedResult())
    return
  }

  session.submitted = true
  session.window.hide()
  session.window.destroy()
  void router.completeRegionCapture(geometry).then((result) => {
    finishRegionOverlay(session, result)
  })
}

async function cancelRegionOverlay(router: CaptureCommandRouter): Promise<void> {
  const session = regionOverlaySession
  if (!session || session.submitted) {
    return
  }
  session.submitted = true
  await router.cancelRegionCapture()
  finishRegionOverlay(session, { status: 'cancelled', feedback: 'Capture cancelled' })
}

async function failRegionOverlay(
  router: CaptureCommandRouter,
  result: CaptureCommandResult,
): Promise<void> {
  const session = regionOverlaySession
  if (!session || session.submitted) {
    return
  }
  session.submitted = true
  await router.cancelRegionCapture()
  finishRegionOverlay(session, result)
}

function finishRegionOverlay(session: RegionOverlaySession, result: CaptureCommandResult): void {
  if (regionOverlaySession !== session) {
    return
  }
  regionOverlaySession = null
  clearTimeout(session.leaseTimeout)
  regionPreviewRegistry.revoke(session.previewToken)
  session.stopWatchingDisplays()
  if (!session.window.isDestroyed()) {
    session.window.destroy()
  }
  if (session.restoreMainWindow) showMainWindow()
  session.resolve(result)
}

function displayMatchesTarget(bounds: Electron.Rectangle, targetSize: LogicalSize): boolean {
  return (
    Math.abs(bounds.width - targetSize.width) <= 1 &&
    Math.abs(bounds.height - targetSize.height) <= 1
  )
}

function geometryFitsTarget(geometry: CaptureGeometry, targetSize: LogicalSize): boolean {
  return (
    geometry.x + geometry.width <= targetSize.width + Number.EPSILON &&
    geometry.y + geometry.height <= targetSize.height + Number.EPSILON
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
  clearTimeout(session.leaseTimeout)
  regionPreviewRegistry.revoke(session.previewToken)
  void router.cancelRegionCapture()
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
    saveDirectory: settingsStore.getSaveDirectory() ?? defaultCaptureDirectory(),
    afterCaptureBehavior: settingsStore.getAfterCaptureBehavior(),
    hdrStatusReminders: settingsStore.getHdrStatusReminders(),
    captureShortcuts: shortcutService?.getSnapshot() ?? {
      region: { accelerator: null, status: 'unconfigured' },
      display: { accelerator: null, status: 'unconfigured' },
    },
  }
}

function defaultCaptureDirectory(): string {
  return join(app.getPath('pictures'), 'Lumiere')
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
  const result = completeCapture(
    mode === 'region' ? await captureRegion(router) : await router.captureDisplay(),
  )
  broadcastCaptureCompleted(result)
  await refreshApplicationTray()
}

function completeCapture(result: CaptureCommandResult): CaptureCommandResult {
  return settingsStore
    ? applyAfterCaptureBehavior(result, settingsStore, (filePath) => {
        shell.showItemInFolder(filePath)
      })
    : result
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
  if (platform === 'macos') {
    return new MacOSPlatformHost(
      macOSHostCandidates({
        appPath: app.getAppPath(),
        isPackaged: app.isPackaged,
        resourcesPath: process.resourcesPath,
        overridePath: process.env.LUMIERE_MAC_HOST_PATH,
      }),
    )
  }

  return new WindowsPlatformHost(
    windowsHostCandidates({
      appPath: app.getAppPath(),
      isPackaged: app.isPackaged,
      resourcesPath: process.resourcesPath,
      overridePath: process.env.LUMIERE_WINDOWS_HOST_PATH,
    }),
  )
}

void app.whenReady().then(async () => {
  registerRegionPreviewProtocol()
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
  void configureWindowsUpdates()

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
  if (platformHost instanceof NativeProcessPlatformHost) {
    platformHost.dispose()
  }
  regionPreviewRegistry.clear()
})

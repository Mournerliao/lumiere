import { contextBridge, ipcRenderer } from 'electron'
import { captureCommandChannels, type LumiereRendererApi } from '../shared/capture-command'
import { settingsCommandChannels, type SettingsSnapshot } from '../shared/settings-command'
import type { LumierePlatform } from '../shared/platform-contract'

const platform: LumierePlatform = process.platform === 'darwin' ? 'macos' : 'windows'

const platformApi: LumiereRendererApi = {
  platform,
  getCaptureSurfaceSnapshot: () => ipcRenderer.invoke(captureCommandChannels.getSurfaceSnapshot),
  captureDisplay: () => ipcRenderer.invoke(captureCommandChannels.captureDisplay),
  captureRegion: () => ipcRenderer.invoke(captureCommandChannels.captureRegion),
  onCaptureCompleted: (listener) => {
    const handleCompleted = (
      _event: Electron.IpcRendererEvent,
      result: Parameters<typeof listener>[0],
    ): void => {
      listener(result)
    }
    ipcRenderer.on(captureCommandChannels.completed, handleCompleted)
    return () => {
      ipcRenderer.removeListener(captureCommandChannels.completed, handleCompleted)
    }
  },
  getRegionOverlaySnapshot: () =>
    ipcRenderer.invoke(captureCommandChannels.getRegionOverlaySnapshot),
  cancelRegionOverlay: () => {
    ipcRenderer.send(captureCommandChannels.cancelRegionOverlay)
  },
  submitRegionSelection: (geometry) => {
    ipcRenderer.send(captureCommandChannels.submitRegionSelection, geometry)
  },
  getSettingsSnapshot: () => ipcRenderer.invoke(settingsCommandChannels.getSnapshot),
  setOutputDelivery: (delivery) =>
    ipcRenderer.invoke(settingsCommandChannels.setOutputDelivery, delivery),
  setCaptureShortcut: (update) =>
    ipcRenderer.invoke(settingsCommandChannels.setCaptureShortcut, update),
  setShortcutRecording: (recording) =>
    ipcRenderer.invoke(settingsCommandChannels.setShortcutRecording, recording),
  onSettingsChanged: (listener) => {
    const handleChanged = (_event: Electron.IpcRendererEvent, snapshot: SettingsSnapshot): void => {
      listener(snapshot)
    }
    ipcRenderer.on(settingsCommandChannels.changed, handleChanged)
    return () => {
      ipcRenderer.removeListener(settingsCommandChannels.changed, handleChanged)
    }
  },
  onShowSettingsRequested: (listener) => {
    const handleShowRequested = (): void => {
      listener()
    }
    ipcRenderer.on(settingsCommandChannels.showRequested, handleShowRequested)
    return () => {
      ipcRenderer.removeListener(settingsCommandChannels.showRequested, handleShowRequested)
    }
  },
}

contextBridge.exposeInMainWorld('lumierePlatform', platformApi)

import { contextBridge, ipcRenderer } from 'electron'
import { captureCommandChannels, type LumiereRendererApi } from '../shared/capture-command'
import { settingsCommandChannels, type SettingsSnapshot } from '../shared/settings-command'
import type { LumierePlatform } from '../shared/platform-contract'

const platform: LumierePlatform = process.platform === 'darwin' ? 'macos' : 'windows'

const platformApi: LumiereRendererApi = {
  platform,
  getCaptureSurfaceSnapshot: () => ipcRenderer.invoke(captureCommandChannels.getSurfaceSnapshot),
  captureDisplay: () => ipcRenderer.invoke(captureCommandChannels.captureDisplay),
  openSettings: () => ipcRenderer.invoke(settingsCommandChannels.openWindow),
  getSettingsSnapshot: () => ipcRenderer.invoke(settingsCommandChannels.getSnapshot),
  setOutputDelivery: (delivery) =>
    ipcRenderer.invoke(settingsCommandChannels.setOutputDelivery, delivery),
  onSettingsChanged: (listener) => {
    const handleChanged = (_event: Electron.IpcRendererEvent, snapshot: SettingsSnapshot): void => {
      listener(snapshot)
    }
    ipcRenderer.on(settingsCommandChannels.changed, handleChanged)
    return () => {
      ipcRenderer.removeListener(settingsCommandChannels.changed, handleChanged)
    }
  },
}

contextBridge.exposeInMainWorld('lumierePlatform', platformApi)

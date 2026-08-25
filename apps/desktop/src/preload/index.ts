import { contextBridge, ipcRenderer } from 'electron'
import { captureCommandChannels, type LumiereRendererApi } from '../shared/capture-command'
import type { LumierePlatform } from '../shared/platform-contract'

const platform: LumierePlatform = process.platform === 'darwin' ? 'macos' : 'windows'

const platformApi: LumiereRendererApi = {
  platform,
  getCaptureSurfaceSnapshot: () => ipcRenderer.invoke(captureCommandChannels.getSurfaceSnapshot),
  captureDisplay: () => ipcRenderer.invoke(captureCommandChannels.captureDisplay),
}

contextBridge.exposeInMainWorld('lumierePlatform', platformApi)

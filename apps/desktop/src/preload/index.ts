import { contextBridge, ipcRenderer } from 'electron'
import type { LumierePlatform, LumiereRendererApi } from '../shared/platform-contract'
import { platformChannels } from '../shared/platform-contract'

const platform: LumierePlatform = process.platform === 'darwin' ? 'macos' : 'windows'

const platformApi: LumiereRendererApi = {
  platform,
  getCapabilities: () => ipcRenderer.invoke(platformChannels.getCapabilities),
  capture: (request) => ipcRenderer.invoke(platformChannels.capture, request),
}

contextBridge.exposeInMainWorld('lumierePlatform', platformApi)

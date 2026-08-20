import { contextBridge, ipcRenderer } from 'electron'
import type { LumierePlatformApi } from '../shared/platform-contract'
import { platformChannels } from '../shared/platform-contract'

const platformApi: LumierePlatformApi = {
  getCapabilities: () => ipcRenderer.invoke(platformChannels.getCapabilities),
  capture: (request) => ipcRenderer.invoke(platformChannels.capture, request),
}

contextBridge.exposeInMainWorld('lumierePlatform', platformApi)

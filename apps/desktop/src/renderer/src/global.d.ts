import type { LumiereRendererApi } from '../../shared/platform-contract'

declare global {
  interface Window {
    lumierePlatform: LumiereRendererApi
  }
}

export {}

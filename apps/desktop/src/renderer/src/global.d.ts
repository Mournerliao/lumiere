import type { LumierePlatformApi } from '../../shared/platform-contract'

declare global {
  interface Window {
    lumierePlatform: LumierePlatformApi
  }
}

export {}

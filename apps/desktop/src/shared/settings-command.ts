import { deliveryTargetsFor, type DeliveryTarget, type OutputDelivery } from './platform-contract'

export const settingsCommandChannels = {
  changed: 'settings:changed',
  getSnapshot: 'settings:get-snapshot',
  setOutputDelivery: 'settings:set-output-delivery',
} as const

export const outputDeliveryOptions: readonly OutputDelivery[] = ['clipboard', 'folder', 'both']

export interface SettingsSnapshot {
  outputDelivery: OutputDelivery
  availableOutputDeliveries: readonly OutputDelivery[]
}

export interface LumiereSettingsApi {
  getSettingsSnapshot(): Promise<SettingsSnapshot>
  setOutputDelivery(delivery: OutputDelivery): Promise<SettingsSnapshot>
  onSettingsChanged(listener: (snapshot: SettingsSnapshot) => void): () => void
}

export function parseOutputDelivery(value: unknown): OutputDelivery {
  if (!outputDeliveryOptions.includes(value as OutputDelivery)) {
    throw new SettingsContractError('Output delivery must be clipboard, folder, or both.')
  }
  return value as OutputDelivery
}

export function availableOutputDeliveries(
  deliveryTargets: readonly DeliveryTarget[],
): readonly OutputDelivery[] {
  return outputDeliveryOptions.filter((delivery) =>
    deliveryTargetsFor(delivery).every((target) => deliveryTargets.includes(target)),
  )
}

export class SettingsContractError extends Error {
  public constructor(message: string) {
    super(message)
    this.name = 'SettingsContractError'
  }
}

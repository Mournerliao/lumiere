import { deliveryTargetsFor, type DeliveryTarget, type OutputDelivery } from './platform-contract'
import type { CaptureShortcutSnapshot, ShortcutUpdate } from './shortcut-command'

export const settingsCommandChannels = {
  changed: 'settings:changed',
  getSnapshot: 'settings:get-snapshot',
  setAfterCaptureBehavior: 'settings:set-after-capture-behavior',
  setCaptureShortcut: 'settings:set-capture-shortcut',
  setOutputDelivery: 'settings:set-output-delivery',
  setShortcutRecording: 'settings:set-shortcut-recording',
  showRequested: 'settings:show-requested',
} as const

export const outputDeliveryOptions: readonly OutputDelivery[] = ['clipboard', 'folder', 'both']
export const afterCaptureBehaviorOptions = ['do-nothing', 'show-in-folder'] as const
export type AfterCaptureBehavior = (typeof afterCaptureBehaviorOptions)[number]

export interface SettingsSnapshot {
  outputDelivery: OutputDelivery
  availableOutputDeliveries: readonly OutputDelivery[]
  captureShortcuts: CaptureShortcutSnapshot
  afterCaptureBehavior: AfterCaptureBehavior
}

export type ShortcutUpdateResult =
  { status: 'success'; snapshot: SettingsSnapshot } | { status: 'failed'; message: string }

export interface LumiereSettingsApi {
  getSettingsSnapshot(): Promise<SettingsSnapshot>
  setOutputDelivery(delivery: OutputDelivery): Promise<SettingsSnapshot>
  setAfterCaptureBehavior(behavior: AfterCaptureBehavior): Promise<SettingsSnapshot>
  setCaptureShortcut(update: ShortcutUpdate): Promise<ShortcutUpdateResult>
  setShortcutRecording(recording: boolean): Promise<void>
  onSettingsChanged(listener: (snapshot: SettingsSnapshot) => void): () => void
  onShowSettingsRequested(listener: () => void): () => void
}

export function parseOutputDelivery(value: unknown): OutputDelivery {
  if (!outputDeliveryOptions.includes(value as OutputDelivery)) {
    throw new SettingsContractError('Output delivery must be clipboard, folder, or both.')
  }
  return value as OutputDelivery
}

export function parseAfterCaptureBehavior(value: unknown): AfterCaptureBehavior {
  if (!afterCaptureBehaviorOptions.includes(value as AfterCaptureBehavior)) {
    throw new SettingsContractError('After-capture behavior must be do-nothing or show-in-folder.')
  }
  return value as AfterCaptureBehavior
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

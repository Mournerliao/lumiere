import { describe, expect, it } from 'vitest'
import {
  availableOutputDeliveries,
  parseAfterCaptureBehavior,
  parseOutputDelivery,
  SettingsContractError,
} from './settings-command'

describe('settings command contract', () => {
  it.each(['clipboard', 'folder', 'both'] as const)('accepts %s as an output delivery', (value) => {
    expect(parseOutputDelivery(value)).toBe(value)
  })

  it.each([undefined, null, 'download', { delivery: 'both' }])(
    'rejects an invalid output delivery payload',
    (value) => {
      expect(() => parseOutputDelivery(value)).toThrow(SettingsContractError)
    },
  )

  it('derives selectable combinations from host capabilities', () => {
    expect(availableOutputDeliveries(['clipboard'])).toEqual(['clipboard'])
    expect(availableOutputDeliveries(['folder'])).toEqual(['folder'])
    expect(availableOutputDeliveries(['clipboard', 'folder'])).toEqual([
      'clipboard',
      'folder',
      'both',
    ])
  })

  it.each(['do-nothing', 'show-in-folder'] as const)(
    'accepts %s as an after-capture behavior',
    (value) => {
      expect(parseAfterCaptureBehavior(value)).toBe(value)
    },
  )

  it.each([undefined, null, 'open-preview', { behavior: 'do-nothing' }])(
    'rejects an invalid after-capture behavior',
    (value) => {
      expect(() => parseAfterCaptureBehavior(value)).toThrow(SettingsContractError)
    },
  )
})

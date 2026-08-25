import { describe, expect, it } from 'vitest'
import {
  availableOutputDeliveries,
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
})

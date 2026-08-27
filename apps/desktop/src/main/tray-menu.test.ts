import { describe, expect, it, vi } from 'vitest'
import { applicationTrayMenuTemplate } from './tray-menu'

describe('application tray menu', () => {
  it('exposes capture, window, settings, and quit commands in product order', () => {
    const template = applicationTrayMenuTemplate(
      {
        regionAvailable: true,
        displayAvailable: false,
        shortcuts: {
          region: { accelerator: 'Command+Shift+L', status: 'registered' },
          display: { accelerator: null, status: 'unconfigured' },
        },
      },
      {
        captureRegion: vi.fn(),
        captureDisplay: vi.fn(),
        showWindow: vi.fn(),
        showSettings: vi.fn(),
        quit: vi.fn(),
      },
    )

    expect(template.map((item) => item.type ?? item.label)).toEqual([
      'Capture region',
      'Capture display',
      'separator',
      'Open Lumiere',
      'Settings…',
      'separator',
      'Quit Lumiere',
    ])
    expect(template[0]).toMatchObject({
      accelerator: 'Command+Shift+L',
      registerAccelerator: false,
      enabled: true,
    })
    expect(template[1]).toMatchObject({ enabled: false })
  })
})

import { renderToStaticMarkup } from 'react-dom/server'
import { describe, expect, it } from 'vitest'
import { SettingsView } from './SettingsView'

describe('SettingsView', () => {
  it('renders the beUI select with all choices and disables unsupported combinations', () => {
    const markup = renderToStaticMarkup(
      <SettingsView
        snapshot={{
          outputDelivery: 'clipboard',
          availableOutputDeliveries: ['clipboard'],
        }}
        isSaving={false}
        error={null}
        onOutputDeliveryChange={() => undefined}
      />,
    )

    expect(markup).toContain('Default destination')
    expect(markup).toContain('aria-haspopup="listbox"')
    expect(markup).toContain('Clipboard and folder')
    expect(markup).toContain('role="option" aria-selected="false" disabled=""')
    expect(markup).toContain('role="option" aria-selected="true" tabindex="-1"')
    expect(markup).not.toContain('<select')
  })
})

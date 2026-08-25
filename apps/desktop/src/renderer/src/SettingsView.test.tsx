import { renderToStaticMarkup } from 'react-dom/server'
import { describe, expect, it } from 'vitest'
import { SettingsView } from './SettingsView'

describe('SettingsView', () => {
  it('renders all three output choices and disables unsupported combinations', () => {
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
    expect(markup).toContain('Clipboard and folder')
    expect(markup).toContain('<option value="folder" disabled="">Folder</option>')
    expect(markup).toContain('<option value="both" disabled="">Clipboard and folder</option>')
    expect(markup).toContain('<option value="clipboard" selected="">Clipboard</option>')
  })
})

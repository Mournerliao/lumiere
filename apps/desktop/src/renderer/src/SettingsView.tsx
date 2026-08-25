import type { OutputDelivery } from '../../shared/platform-contract'
import { outputDeliveryOptions, type SettingsSnapshot } from '../../shared/settings-command'

const outputDeliveryLabels: Record<OutputDelivery, string> = {
  clipboard: 'Clipboard',
  folder: 'Folder',
  both: 'Clipboard and folder',
}

interface SettingsViewProps {
  snapshot: SettingsSnapshot | null
  isSaving: boolean
  error: string | null
  onOutputDeliveryChange: (delivery: OutputDelivery) => void
}

export function SettingsView({
  snapshot,
  isSaving,
  error,
  onOutputDeliveryChange,
}: SettingsViewProps): React.JSX.Element {
  const available = snapshot?.availableOutputDeliveries ?? []

  return (
    <main className="settings-shell">
      <aside className="settings-sidebar" aria-label="Settings sections">
        <div className="settings-window-safe-area" aria-hidden="true" />
        <nav className="settings-navigation">
          <span
            className="settings-navigation-item settings-navigation-item--active"
            aria-current="page"
          >
            Output
          </span>
          <span className="settings-navigation-item settings-navigation-item--inactive">
            Capture
          </span>
          <span className="settings-navigation-item settings-navigation-item--inactive">
            System &amp; About
          </span>
        </nav>
      </aside>

      <section className="settings-content" aria-labelledby="settings-title">
        <header className="settings-title-bar">
          <h1 id="settings-title">Output</h1>
        </header>

        <div className="settings-list">
          <label className="settings-row" htmlFor="default-destination">
            <span className="settings-row-label">Default destination</span>
            <select
              id="default-destination"
              value={snapshot?.outputDelivery ?? 'both'}
              disabled={!snapshot || isSaving || available.length === 0}
              onChange={(event) => {
                onOutputDeliveryChange(event.currentTarget.value as OutputDelivery)
              }}
            >
              {outputDeliveryOptions.map((delivery) => (
                <option key={delivery} value={delivery} disabled={!available.includes(delivery)}>
                  {outputDeliveryLabels[delivery]}
                </option>
              ))}
            </select>
          </label>
        </div>

        {error ? (
          <p className="settings-error" role="alert">
            {error}
          </p>
        ) : null}
      </section>
    </main>
  )
}

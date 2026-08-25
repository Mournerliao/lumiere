import type { OutputDelivery } from '../../shared/platform-contract'
import {
  outputDeliveryOptions,
  parseOutputDelivery,
  type SettingsSnapshot,
} from '../../shared/settings-command'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/motion/select'

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
  const selectedDelivery = snapshot?.outputDelivery ?? 'both'

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
          <div className="settings-row">
            <span className="settings-row-label" id="default-destination-label">
              Default destination
            </span>
            <Select
              value={selectedDelivery}
              disabled={!snapshot || isSaving || available.length === 0}
              onValueChange={(value) => {
                onOutputDeliveryChange(parseOutputDelivery(value))
              }}
              className="settings-select"
            >
              <SelectTrigger
                aria-labelledby="default-destination-label"
                className="settings-select-trigger"
              >
                <SelectValue placeholder={outputDeliveryLabels[selectedDelivery]} />
              </SelectTrigger>
              <SelectContent className="settings-select-content">
                {outputDeliveryOptions.map((delivery) => (
                  <SelectItem
                    key={delivery}
                    value={delivery}
                    disabled={!available.includes(delivery)}
                    className="settings-select-item"
                  >
                    {outputDeliveryLabels[delivery]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
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

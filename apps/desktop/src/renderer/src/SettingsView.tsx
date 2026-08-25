import { useState } from 'react'
import type { CaptureSurfaceSnapshot } from '../../shared/capture-command'
import type { LumierePlatform, OutputDelivery } from '../../shared/platform-contract'
import {
  outputDeliveryOptions,
  parseOutputDelivery,
  type SettingsSnapshot,
} from '../../shared/settings-command'
import { Dock, DockItem } from '@/components/motion/dock'
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

type SettingsSection = 'output' | 'capture' | 'system'

interface SettingsViewProps {
  snapshot: SettingsSnapshot | null
  surfaceSnapshot: CaptureSurfaceSnapshot | null
  platform: LumierePlatform
  isSaving: boolean
  error: string | null
  onDone: () => void
  onOutputDeliveryChange: (delivery: OutputDelivery) => void
}

export function SettingsView({
  snapshot,
  surfaceSnapshot,
  platform,
  isSaving,
  error,
  onDone,
  onOutputDeliveryChange,
}: SettingsViewProps): React.JSX.Element {
  const [section, setSection] = useState<SettingsSection>('output')
  const available = snapshot?.availableOutputDeliveries ?? []
  const selectedDelivery = snapshot?.outputDelivery ?? 'both'

  return (
    <main className="settings-shell">
      <header
        className={`settings-title-bar settings-title-bar--${platform}`}
        aria-label="Lumiere settings window"
      >
        <h1>Settings</h1>
        <button className="settings-done" type="button" onClick={onDone}>
          Done
        </button>
      </header>

      <nav className="settings-dock-slot" aria-label="Settings sections">
        <Dock itemWidth={92} itemHeight={36} className="settings-dock">
          <DockItem
            className="settings-dock-item"
            active={section === 'output'}
            aria-label="Output settings"
            onClick={() => {
              setSection('output')
            }}
          >
            <OutputIcon />
          </DockItem>
          <DockItem
            className="settings-dock-item"
            active={section === 'capture'}
            aria-label="Capture settings"
            onClick={() => {
              setSection('capture')
            }}
          >
            <CaptureIcon />
          </DockItem>
          <DockItem
            className="settings-dock-item"
            active={section === 'system'}
            aria-label="System and about"
            onClick={() => {
              setSection('system')
            }}
          >
            <SystemIcon />
          </DockItem>
        </Dock>
      </nav>

      <section className="settings-content" aria-label={`${section} settings`}>
        {section === 'output' ? (
          <OutputSettings
            snapshot={snapshot}
            surfaceSnapshot={surfaceSnapshot}
            isSaving={isSaving}
            available={available}
            selectedDelivery={selectedDelivery}
            onOutputDeliveryChange={onOutputDeliveryChange}
          />
        ) : null}
        {section === 'capture' ? <CaptureSettings snapshot={surfaceSnapshot} /> : null}
        {section === 'system' ? <SystemSettings snapshot={surfaceSnapshot} /> : null}
        {error ? (
          <p className="settings-error" role="alert">
            {error}
          </p>
        ) : null}
      </section>
    </main>
  )
}

interface OutputSettingsProps {
  snapshot: SettingsSnapshot | null
  surfaceSnapshot: CaptureSurfaceSnapshot | null
  isSaving: boolean
  available: readonly OutputDelivery[]
  selectedDelivery: OutputDelivery
  onOutputDeliveryChange: (delivery: OutputDelivery) => void
}

function OutputSettings({
  snapshot,
  surfaceSnapshot,
  isSaving,
  available,
  selectedDelivery,
  onOutputDeliveryChange,
}: OutputSettingsProps): React.JSX.Element {
  return (
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

      <div className="settings-row">
        <span className="settings-row-label">Save folder</span>
        <span className="settings-row-value settings-row-value--control">
          {surfaceSnapshot?.output.location ?? '~/Pictures/Lumiere'}
        </span>
      </div>

      <div className="settings-row">
        <span className="settings-row-copy">
          <span className="settings-row-label">File naming</span>
          <span className="settings-row-hint">Lumiere 2026-08-21 at 11.24.08.png</span>
        </span>
        <span className="settings-row-value settings-row-value--control">Timestamped</span>
      </div>
    </div>
  )
}

function CaptureSettings({
  snapshot,
}: {
  snapshot: CaptureSurfaceSnapshot | null
}): React.JSX.Element {
  const regionAvailable = snapshot?.captureModes.includes('region') === true
  const displayAvailable = snapshot?.captureModes.includes('display') === true

  return (
    <div className="settings-list">
      <SettingsRow
        label="Region shortcut"
        value={regionAvailable ? 'Not configured' : 'Unavailable'}
      />
      <SettingsRow
        label="Display shortcut"
        value={displayAvailable ? 'Not configured' : 'Unavailable'}
      />
      <SettingsRow label="After capture" value="Do nothing" control />
      <SettingsRow
        label="HDR status"
        hint="Never blocks capture"
        value={hdrStatusLabel(snapshot)}
        tone={snapshot?.hdrStatus === 'ready' ? 'ready' : 'muted'}
      />
    </div>
  )
}

function SystemSettings({
  snapshot,
}: {
  snapshot: CaptureSurfaceSnapshot | null
}): React.JSX.Element {
  const hostAvailable = snapshot?.hostAvailable === true
  const displayAvailable = snapshot?.captureModes.includes('display') === true
  const permissionStatus = !snapshot
    ? 'Checking…'
    : !hostAvailable
      ? 'Unknown'
      : displayAvailable
        ? 'Available'
        : 'Needs attention'

  return (
    <div className="settings-list settings-list--system">
      <SettingsRow
        label="Screen recording permission"
        value={permissionStatus}
        tone={displayAvailable ? 'ready' : 'muted'}
      />
      <SettingsRow
        label="Native capture host"
        value={!snapshot ? 'Checking…' : hostAvailable ? 'Connected' : 'Unavailable'}
        tone={hostAvailable ? 'ready' : 'muted'}
      />
      <SettingsRow label="Version" value="0.1.0" />
      <p className="settings-semantics-note">
        Native HDR-aware capture. Everyday output is sRGB Visual Match. Copied and saved mean
        delivered, not certified.
      </p>
    </div>
  )
}

interface SettingsRowProps {
  label: string
  hint?: string
  value: string
  control?: boolean
  tone?: 'ready' | 'muted'
}

function SettingsRow({
  label,
  hint,
  value,
  control = false,
  tone = 'muted',
}: SettingsRowProps): React.JSX.Element {
  return (
    <div className="settings-row">
      <span className="settings-row-copy">
        <span className="settings-row-label">{label}</span>
        {hint ? <span className="settings-row-hint">{hint}</span> : null}
      </span>
      <span className={`settings-row-value settings-row-value--${control ? 'control' : tone}`}>
        {value}
      </span>
    </div>
  )
}

function hdrStatusLabel(snapshot: CaptureSurfaceSnapshot | null): string {
  if (!snapshot) return 'Checking…'
  if (snapshot.hdrStatus === 'ready') return 'Ready'
  if (snapshot.hdrStatus === 'unvalidated') return 'Not verified'
  return 'Unavailable'
}

function OutputIcon(): React.JSX.Element {
  return (
    <svg viewBox="0 0 18 18" aria-hidden="true">
      <path d="M3 10.5v3.75a.75.75 0 0 0 .75.75h10.5a.75.75 0 0 0 .75-.75V10.5M9 2.25v9m0 0L5.75 8M9 11.25 12.25 8" />
    </svg>
  )
}

function CaptureIcon(): React.JSX.Element {
  return (
    <svg viewBox="0 0 18 18" aria-hidden="true">
      <path d="M6 2.5H3.5a1 1 0 0 0-1 1V6m9.5-3.5h2.5a1 1 0 0 1 1 1V6m0 6v2.5a1 1 0 0 1-1 1H12m-6 0H3.5a1 1 0 0 1-1-1V12" />
    </svg>
  )
}

function SystemIcon(): React.JSX.Element {
  return (
    <svg viewBox="0 0 18 18" aria-hidden="true">
      <circle cx="9" cy="9" r="2.5" />
      <path d="m14.2 10.1 1 .75-1.5 2.6-1.15-.5a7 7 0 0 1-1.5.85L10.9 15h-3l-.15-1.2a7 7 0 0 1-1.53-.87l-1.17.52-1.5-2.6 1.03-.77A6 6 0 0 1 4.53 9a6 6 0 0 1 .06-1.08l-1.04-.77 1.5-2.6 1.17.52a7 7 0 0 1 1.53-.87L7.9 3h3l.15 1.2a7 7 0 0 1 1.53.87l1.17-.52 1.5 2.6-1.04.77A6 6 0 0 1 14.27 9a6 6 0 0 1-.07 1.1Z" />
    </svg>
  )
}

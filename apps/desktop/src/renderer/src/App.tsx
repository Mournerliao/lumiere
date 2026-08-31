import { useEffect, useState } from 'react'
import { Button } from '@/components/motion/button/base'
import type {
  CaptureCommandResult,
  CaptureNotice,
  CaptureSurfaceSnapshot,
} from '../../shared/capture-command'
import type { OutputDelivery } from '../../shared/platform-contract'
import type { CaptureMode, ShortcutUpdate } from '../../shared/shortcut-command'
import type { SettingsSnapshot } from '../../shared/settings-command'
import type { AfterCaptureBehavior } from '../../shared/settings-command'
import { SettingsView } from './SettingsView'
import { RegionOverlay } from './RegionOverlay'

const CAPTURE_LOAD_FAILURE: CaptureNotice = {
  tone: 'critical',
  title: 'Capture controls are unavailable',
  detail: 'Restart Lumiere and try again.',
}

export function App(): React.JSX.Element {
  if (new URLSearchParams(window.location.search).get('surface') === 'region-overlay') {
    return <RegionOverlay />
  }

  return <ApplicationSurface />
}

function ApplicationSurface(): React.JSX.Element {
  const [view, setView] = useState<'capture' | 'settings'>('capture')
  const [captureResult, setCaptureResult] = useState<CaptureCommandResult | null>(null)

  useEffect(() => {
    const stopSettingsListening = window.lumierePlatform.onShowSettingsRequested(() => {
      setView('settings')
    })
    const stopCaptureListening = window.lumierePlatform.onCaptureCompleted((result) => {
      setCaptureResult(result)
    })
    return () => {
      stopSettingsListening()
      stopCaptureListening()
    }
  }, [])

  return view === 'settings' ? (
    <SettingsWindow
      onDone={() => {
        setView('capture')
      }}
    />
  ) : (
    <MainWindow
      result={captureResult}
      onResultChange={setCaptureResult}
      onOpenSettings={() => {
        setView('settings')
      }}
    />
  )
}

function MainWindow({
  result,
  onResultChange,
  onOpenSettings,
}: {
  result: CaptureCommandResult | null
  onResultChange: (result: CaptureCommandResult | null) => void
  onOpenSettings: () => void
}): React.JSX.Element {
  const [snapshot, setSnapshot] = useState<CaptureSurfaceSnapshot | null>(null)
  const [capturingMode, setCapturingMode] = useState<'region' | 'display' | null>(null)
  const [interactionHint, setInteractionHint] = useState<string | null>(null)
  const [loadFailed, setLoadFailed] = useState(false)

  useEffect(() => {
    let isCurrent = true
    const refreshSnapshot = (): void => {
      void window.lumierePlatform
        .getCaptureSurfaceSnapshot()
        .then((nextSnapshot) => {
          if (isCurrent) {
            setSnapshot(nextSnapshot)
          }
        })
        .catch(() => {
          if (isCurrent) {
            setLoadFailed(true)
          }
        })
    }
    refreshSnapshot()
    const stopListening = window.lumierePlatform.onSettingsChanged(() => {
      refreshSnapshot()
    })
    return () => {
      stopListening()
      isCurrent = false
    }
  }, [])

  const captureDisplay = async (): Promise<void> => {
    setCapturingMode('display')
    onResultChange(null)
    setInteractionHint(null)
    try {
      onResultChange(await window.lumierePlatform.captureDisplay())
    } catch {
      onResultChange({
        status: 'failed',
        feedback: CAPTURE_LOAD_FAILURE.title,
        notice: CAPTURE_LOAD_FAILURE,
      })
    } finally {
      setCapturingMode(null)
    }
  }

  const captureRegion = async (): Promise<void> => {
    setCapturingMode('region')
    onResultChange(null)
    setInteractionHint(null)
    try {
      onResultChange(await window.lumierePlatform.captureRegion())
    } catch {
      onResultChange({
        status: 'failed',
        feedback: CAPTURE_LOAD_FAILURE.title,
        notice: CAPTURE_LOAD_FAILURE,
      })
    } finally {
      setCapturingMode(null)
    }
  }

  const supportsRegionCapture =
    snapshot?.hostAvailable === true && snapshot.captureModes.includes('region')
  const supportsDisplayCapture =
    snapshot?.hostAvailable === true && snapshot.captureModes.includes('display')
  const activeNotice =
    result?.status === 'failed' || result?.status === 'partial'
      ? result.notice
      : loadFailed
        ? CAPTURE_LOAD_FAILURE
        : (snapshot?.blockingNotice ?? snapshot?.advisoryNotice)
  const captureBlocked = loadFailed || snapshot?.blockingNotice !== undefined

  return (
    <main className="app-shell">
      <header
        className={`title-bar title-bar--${window.lumierePlatform.platform}`}
        aria-label="Lumiere window"
      >
        <span className="window-title">Lumiere</span>
      </header>

      <section
        className={`capture-panel${activeNotice ? ' capture-panel--with-notice' : ''}`}
        aria-label="Capture controls"
      >
        {activeNotice ? <Notice notice={activeNotice} /> : null}

        <div className="capture-actions">
          <Button
            variant="secondary"
            size="lg"
            pressScale={0.99}
            hoverScale={1}
            className="capture-action capture-action--region"
            disabled={!supportsRegionCapture || capturingMode !== null}
            onClick={() => void captureRegion()}
            onFocus={() => {
              setInteractionHint('Drag to select an area')
            }}
            onBlur={() => {
              setInteractionHint(null)
            }}
            onPointerEnter={() => {
              setInteractionHint('Drag to select an area')
            }}
            onPointerLeave={() => {
              setInteractionHint(null)
            }}
          >
            <RegionIcon />
            <span>{capturingMode === 'region' ? 'Capturing region' : 'Capture region'}</span>
            {capturingMode === 'region' ? (
              <span className="capture-pulse" aria-hidden="true" />
            ) : null}
          </Button>

          <Button
            variant="secondary"
            size="lg"
            pressScale={0.99}
            hoverScale={1}
            className="capture-action"
            disabled={!supportsDisplayCapture || capturingMode !== null}
            onClick={() => void captureDisplay()}
            onFocus={() => {
              setInteractionHint('Capture the display under the pointer')
            }}
            onBlur={() => {
              setInteractionHint(null)
            }}
            onPointerEnter={() => {
              setInteractionHint('Capture the display under the pointer')
            }}
            onPointerLeave={() => {
              setInteractionHint(null)
            }}
          >
            <DisplayIcon />
            <span>{capturingMode === 'display' ? 'Capturing display' : 'Capture display'}</span>
            {capturingMode === 'display' ? (
              <span className="capture-pulse" aria-hidden="true" />
            ) : null}
          </Button>
        </div>

        <div className="output-summary" aria-label="Current output">
          <span className="output-label">Output</span>
          <span className="output-value">{snapshot?.output.label ?? 'Clipboard and folder'}</span>
          <span className="output-location">
            {snapshot?.output.location ?? '~/Pictures/Lumiere'}
          </span>
        </div>
      </section>

      <footer className="status-bar" aria-live="polite">
        <span className={`status-dot status-dot--${statusTone(snapshot, activeNotice)}`} />
        <span className="status-message">
          {statusMessage({
            activeNotice,
            captureBlocked,
            interactionHint,
            capturingMode,
            result,
            snapshot,
          })}
        </span>
        <Button
          variant="ghost"
          size="sm"
          hoverScale={1}
          pressScale={0.98}
          className="settings-link"
          onClick={onOpenSettings}
        >
          Settings
        </Button>
      </footer>
    </main>
  )
}

function SettingsWindow({ onDone }: { onDone: () => void }): React.JSX.Element {
  const [snapshot, setSnapshot] = useState<SettingsSnapshot | null>(null)
  const [surfaceSnapshot, setSurfaceSnapshot] = useState<CaptureSurfaceSnapshot | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const [savingShortcut, setSavingShortcut] = useState<CaptureMode | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let isCurrent = true
    void Promise.all([
      window.lumierePlatform.getSettingsSnapshot(),
      window.lumierePlatform.getCaptureSurfaceSnapshot(),
    ])
      .then(([nextSnapshot, nextSurfaceSnapshot]) => {
        if (isCurrent) {
          setSnapshot(nextSnapshot)
          setSurfaceSnapshot(nextSurfaceSnapshot)
        }
      })
      .catch(() => {
        if (isCurrent) {
          setError('Settings could not be loaded. Restart Lumiere and try again.')
        }
      })
    const stopListening = window.lumierePlatform.onSettingsChanged((nextSnapshot) => {
      if (isCurrent) {
        setSnapshot(nextSnapshot)
      }
    })
    return () => {
      stopListening()
      isCurrent = false
    }
  }, [])

  const setOutputDelivery = async (delivery: OutputDelivery): Promise<void> => {
    setIsSaving(true)
    setError(null)
    try {
      setSnapshot(await window.lumierePlatform.setOutputDelivery(delivery))
    } catch {
      setError('The output destination could not be saved. Try again.')
    } finally {
      setIsSaving(false)
    }
  }

  const chooseSaveDirectory = async (): Promise<void> => {
    setIsSaving(true)
    setError(null)
    try {
      setSnapshot(await window.lumierePlatform.chooseSaveDirectory())
      setSurfaceSnapshot(await window.lumierePlatform.getCaptureSurfaceSnapshot())
    } catch {
      setError('The save folder could not be changed. Try again.')
    } finally {
      setIsSaving(false)
    }
  }

  const setAfterCaptureBehavior = async (behavior: AfterCaptureBehavior): Promise<void> => {
    setIsSaving(true)
    setError(null)
    try {
      setSnapshot(await window.lumierePlatform.setAfterCaptureBehavior(behavior))
    } catch {
      setError('The after-capture behavior could not be saved. Try again.')
    } finally {
      setIsSaving(false)
    }
  }

  const setHdrStatusReminders = async (enabled: boolean): Promise<void> => {
    setIsSaving(true)
    setError(null)
    try {
      setSnapshot(await window.lumierePlatform.setHdrStatusReminders(enabled))
    } catch {
      setError('HDR status reminders could not be saved. Try again.')
    } finally {
      setIsSaving(false)
    }
  }

  const setCaptureShortcut = async (update: ShortcutUpdate): Promise<void> => {
    setSavingShortcut(update.mode)
    setError(null)
    try {
      const result = await window.lumierePlatform.setCaptureShortcut(update)
      if (result.status === 'failed') {
        setError(result.message)
      } else {
        setSnapshot(result.snapshot)
      }
    } catch {
      setError('The shortcut could not be saved. Try again.')
    } finally {
      setSavingShortcut(null)
    }
  }

  return (
    <SettingsView
      snapshot={snapshot}
      surfaceSnapshot={surfaceSnapshot}
      platform={window.lumierePlatform.platform}
      isSaving={isSaving}
      savingShortcut={savingShortcut}
      error={error}
      onDone={onDone}
      onOutputDeliveryChange={(delivery) => void setOutputDelivery(delivery)}
      onChooseSaveDirectory={() => void chooseSaveDirectory()}
      onAfterCaptureBehaviorChange={(behavior) => void setAfterCaptureBehavior(behavior)}
      onHdrStatusRemindersChange={(enabled) => void setHdrStatusReminders(enabled)}
      onShortcutChange={setCaptureShortcut}
      onShortcutRecordingChange={(recording) => {
        if (recording) setError(null)
        return window.lumierePlatform.setShortcutRecording(recording)
      }}
    />
  )
}

function Notice({ notice }: { notice: CaptureNotice }): React.JSX.Element {
  return (
    <div className={`notice notice--${notice.tone}`} role="status">
      <div className="notice-title">
        <span className="notice-dot" aria-hidden="true" />
        {notice.title}
      </div>
      <p>{notice.detail}</p>
    </div>
  )
}

interface StatusMessageInput {
  snapshot: CaptureSurfaceSnapshot | null
  result: CaptureCommandResult | null
  activeNotice: CaptureNotice | undefined
  captureBlocked: boolean
  interactionHint: string | null
  capturingMode: 'region' | 'display' | null
}

function statusMessage({
  activeNotice,
  captureBlocked,
  interactionHint,
  capturingMode,
  result,
  snapshot,
}: StatusMessageInput): string {
  if (capturingMode) {
    return capturingMode === 'region' ? 'Capturing region' : 'Capturing display'
  }
  if (result) {
    return result.feedback
  }
  if (captureBlocked) {
    return 'Capture disabled'
  }
  if (activeNotice) {
    return 'Capture available'
  }
  if (interactionHint) {
    return interactionHint
  }
  if (!snapshot) {
    return 'Checking…'
  }
  if (snapshot.hdrStatus === 'ready') {
    return 'HDR-aware capture ready'
  }
  return 'Display capture ready'
}

function statusTone(
  snapshot: CaptureSurfaceSnapshot | null,
  activeNotice: CaptureNotice | undefined,
): 'ready' | 'caution' | 'critical' {
  if (activeNotice?.tone === 'critical') {
    return 'critical'
  }
  if (!snapshot || activeNotice) {
    return 'caution'
  }
  return 'ready'
}

function RegionIcon(): React.JSX.Element {
  return (
    <svg aria-hidden="true" viewBox="0 0 16 16">
      <path d="M2.5 6V3.5a1 1 0 0 1 1-1H6M10 2.5h2.5a1 1 0 0 1 1 1V6M13.5 10v2.5a1 1 0 0 1-1 1H10M6 13.5H3.5a1 1 0 0 1-1-1V10" />
    </svg>
  )
}

function DisplayIcon(): React.JSX.Element {
  return (
    <svg aria-hidden="true" viewBox="0 0 16 16">
      <rect x="2.25" y="2.75" width="11.5" height="8.25" rx="1.25" />
      <path d="M6 13.25h4M8 11v2.25" />
    </svg>
  )
}

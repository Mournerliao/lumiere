import { useEffect, useState } from 'react'
import { Button } from '@/components/motion/button/base'
import type {
  CaptureCommandResult,
  CaptureNotice,
  CaptureSurfaceSnapshot,
} from '../../shared/capture-command'
import type { OutputDelivery } from '../../shared/platform-contract'
import type { SettingsSnapshot } from '../../shared/settings-command'
import { SettingsView } from './SettingsView'

const CAPTURE_LOAD_FAILURE: CaptureNotice = {
  tone: 'critical',
  title: 'Capture controls are unavailable',
  detail: 'Restart Lumiere and try again.',
}

export function App(): React.JSX.Element {
  const [view, setView] = useState<'capture' | 'settings'>('capture')

  return view === 'settings' ? (
    <SettingsWindow
      onDone={() => {
        setView('capture')
      }}
    />
  ) : (
    <MainWindow
      onOpenSettings={() => {
        setView('settings')
      }}
    />
  )
}

function MainWindow({ onOpenSettings }: { onOpenSettings: () => void }): React.JSX.Element {
  const [snapshot, setSnapshot] = useState<CaptureSurfaceSnapshot | null>(null)
  const [result, setResult] = useState<CaptureCommandResult | null>(null)
  const [isCapturing, setIsCapturing] = useState(false)
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
    setIsCapturing(true)
    setResult(null)
    setInteractionHint(null)
    try {
      setResult(await window.lumierePlatform.captureDisplay())
    } catch {
      setResult({
        status: 'failed',
        feedback: CAPTURE_LOAD_FAILURE.title,
        notice: CAPTURE_LOAD_FAILURE,
      })
    } finally {
      setIsCapturing(false)
    }
  }

  const supportsDisplayCapture =
    snapshot?.hostAvailable === true && snapshot.captureModes.includes('display')
  const activeNotice =
    result?.status === 'failed' || result?.status === 'partial'
      ? result.notice
      : loadFailed
        ? CAPTURE_LOAD_FAILURE
        : snapshot?.blockingNotice

  return (
    <main className="app-shell">
      <header
        className={`title-bar title-bar--${window.lumierePlatform.platform}`}
        aria-label="Lumiere window"
      >
        <span className="window-title">Lumiere</span>
      </header>

      <section className="capture-panel" aria-label="Capture controls">
        {activeNotice ? <Notice notice={activeNotice} /> : null}

        <div className="capture-actions">
          <Button
            variant="secondary"
            size="lg"
            pressScale={0.99}
            hoverScale={1}
            className="capture-action capture-action--region"
            disabled
          >
            <RegionIcon />
            <span>Capture region</span>
          </Button>

          <Button
            variant="secondary"
            size="lg"
            pressScale={0.99}
            hoverScale={1}
            className="capture-action"
            disabled={!supportsDisplayCapture || isCapturing}
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
            <span>{isCapturing ? 'Capturing display' : 'Capture display'}</span>
            {isCapturing ? <span className="capture-pulse" aria-hidden="true" /> : null}
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
            interactionHint,
            isCapturing,
            loadFailed,
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

  return (
    <SettingsView
      snapshot={snapshot}
      surfaceSnapshot={surfaceSnapshot}
      platform={window.lumierePlatform.platform}
      isSaving={isSaving}
      error={error}
      onDone={onDone}
      onOutputDeliveryChange={(delivery) => void setOutputDelivery(delivery)}
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
  interactionHint: string | null
  isCapturing: boolean
  loadFailed: boolean
}

function statusMessage({
  activeNotice,
  interactionHint,
  isCapturing,
  loadFailed,
  result,
  snapshot,
}: StatusMessageInput): string {
  if (isCapturing) {
    return 'Capturing display'
  }
  if (result) {
    return result.feedback
  }
  if (activeNotice || loadFailed) {
    return 'Capture disabled'
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
  if (snapshot.hdrStatus === 'unvalidated') {
    return 'Display environment not verified'
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
  if (!snapshot || activeNotice || snapshot.hdrStatus === 'unvalidated') {
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

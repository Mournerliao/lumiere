import { useEffect, useState } from 'react'
import { Button } from '@/components/motion/button/base'
import type {
  CaptureMode,
  CaptureResult,
  PlatformCapabilities,
} from '../../shared/platform-contract'

export function App(): React.JSX.Element {
  const [capabilities, setCapabilities] = useState<PlatformCapabilities | null>(null)
  const [result, setResult] = useState<CaptureResult | null>(null)
  const [isCapturing, setIsCapturing] = useState(false)

  useEffect(() => {
    void window.lumierePlatform.getCapabilities().then(setCapabilities)
  }, [])

  const capture = async (mode: CaptureMode): Promise<void> => {
    setIsCapturing(true)
    setResult(null)
    try {
      setResult(await window.lumierePlatform.capture({ mode, delivery: 'clipboard' }))
    } finally {
      setIsCapturing(false)
    }
  }

  const hostAvailable = capabilities?.hostStatus === 'available'

  return (
    <main className="app-shell">
      <header className="masthead">
        <div className="brand-mark" aria-hidden="true" />
        <div>
          <p className="eyebrow">HDR-aware capture</p>
          <h1>Lumiere</h1>
        </div>
        <span className={`host-state ${hostAvailable ? 'available' : ''}`}>
          {capabilities
            ? `${capabilities.platform} host ${capabilities.hostStatus}`
            : 'Checking host'}
        </span>
      </header>

      <section className="capture-surface" aria-labelledby="capture-title">
        <div className="capture-copy">
          <p className="eyebrow">Capture</p>
          <h2 id="capture-title">A faithful screen, ready to share.</h2>
          <p>
            Lumiere captures HDR-aware source pixels with a native platform host and produces one
            dependable sRGB Visual Match for everyday apps.
          </p>
        </div>

        <div className="capture-actions">
          <Button
            variant="secondary"
            size="lg"
            pressScale={0.98}
            disabled={!hostAvailable || isCapturing}
            onClick={() => void capture('region')}
          >
            Capture region
            <span>Drag to select</span>
          </Button>
          <Button
            variant="secondary"
            size="lg"
            pressScale={0.98}
            disabled={!hostAvailable || isCapturing}
            onClick={() => void capture('display')}
          >
            Capture display
            <span>Use the active screen</span>
          </Button>
        </div>
      </section>

      <footer className="status-line" aria-live="polite">
        <span className="status-dot" />
        {result?.status === 'failed'
          ? result.failure.message
          : (capabilities?.unavailableReason?.message ?? 'Native capture host ready.')}
      </footer>
    </main>
  )
}

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
    let isCurrent = true
    void window.lumierePlatform.getCapabilities().then((nextCapabilities) => {
      if (isCurrent) {
        setCapabilities(nextCapabilities)
      }
    })
    return () => {
      isCurrent = false
    }
  }, [])

  const capture = async (mode: CaptureMode): Promise<void> => {
    setIsCapturing(true)
    setResult(null)
    try {
      setResult(await window.lumierePlatform.capture({ mode, delivery: 'folder' }))
    } finally {
      setIsCapturing(false)
    }
  }

  const hostAvailable = capabilities?.hostStatus === 'available'
  const supportsRegionCapture = hostAvailable && capabilities.captureModes.includes('region')
  const supportsDisplayCapture = hostAvailable && capabilities.captureModes.includes('display')

  return (
    <main className="app-shell">
      <div
        className={`window-drag-region window-drag-region--${window.lumierePlatform.platform}`}
        aria-hidden="true"
      />

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
            disabled={!supportsRegionCapture || isCapturing}
            onClick={() => void capture('region')}
          >
            Capture region
            <span>Drag to select</span>
          </Button>
          <Button
            variant="secondary"
            size="lg"
            pressScale={0.98}
            disabled={!supportsDisplayCapture || isCapturing}
            onClick={() => void capture('display')}
          >
            Capture display
            <span>Use the screen under the pointer</span>
          </Button>
        </div>
      </section>

      <footer className="status-line" aria-live="polite">
        <span className="status-dot" />
        {statusMessage(result, capabilities)}
      </footer>
    </main>
  )
}

function statusMessage(
  result: CaptureResult | null,
  capabilities: PlatformCapabilities | null,
): string {
  if (result?.status === 'failed') {
    return result.failure.message
  }
  if (result?.status === 'success') {
    return result.artifact.filePath
      ? `Saved sRGB Visual Match to ${result.artifact.filePath}`
      : 'sRGB Visual Match capture completed.'
  }
  return capabilities?.unavailableReason?.message ?? 'Native capture host ready.'
}

import { useEffect, useRef, useState } from 'react'
import type { RegionOverlaySnapshot } from '../../shared/capture-command'
import {
  projectOverlaySelection,
  type OverlayPoint,
  type OverlaySelection,
} from '../../shared/region-overlay'

export function RegionOverlay(): React.JSX.Element {
  const [snapshot, setSnapshot] = useState<RegionOverlaySnapshot | null>(null)
  const [selection, setSelection] = useState<OverlaySelection | null>(null)
  const [pointer, setPointer] = useState<OverlayPoint | null>(null)
  const [capturing, setCapturing] = useState(false)
  const startPoint = useRef<OverlayPoint | null>(null)
  const root = useRef<HTMLElement | null>(null)
  const announcedReady = useRef(false)

  useEffect(() => {
    let isCurrent = true
    void window.lumierePlatform
      .getRegionOverlaySnapshot()
      .then((nextSnapshot) => {
        if (isCurrent) {
          setSnapshot(nextSnapshot)
        }
      })
      .catch(() => {
        if (isCurrent) {
          window.lumierePlatform.cancelRegionOverlay()
        }
      })

    const resetSelection = (): void => {
      startPoint.current = null
      setSelection(null)
      setPointer(null)
    }
    const cancelOnEscape = (event: KeyboardEvent): void => {
      if (event.key === 'Escape') {
        window.lumierePlatform.cancelRegionOverlay()
      }
    }
    window.addEventListener('blur', resetSelection)
    window.addEventListener('keydown', cancelOnEscape)
    return () => {
      isCurrent = false
      window.removeEventListener('blur', resetSelection)
      window.removeEventListener('keydown', cancelOnEscape)
    }
  }, [])

  const projectSelection = (current: OverlayPoint): OverlaySelection | null => {
    if (!snapshot || !startPoint.current) {
      return null
    }
    return projectOverlaySelection(
      startPoint.current,
      current,
      { width: window.innerWidth, height: window.innerHeight },
      snapshot.targetSize,
    )
  }

  const finishSelection = (current: OverlayPoint): void => {
    const completed = projectSelection(current)
    if (!completed?.valid) {
      window.lumierePlatform.cancelRegionOverlay()
      return
    }

    setSelection(completed)
    setCapturing(true)
    startPoint.current = null
    window.requestAnimationFrame(() => {
      window.requestAnimationFrame(() => {
        window.lumierePlatform.submitRegionSelection(completed.geometry)
      })
    })
  }

  const hint = capturing
    ? 'Capturing…'
    : selection
      ? selection.valid
        ? 'Release to capture · Esc cancels'
        : 'Keep dragging · Esc cancels'
      : 'Drag to select · Esc cancels'

  return (
    <main
      ref={root}
      className={`region-overlay${capturing ? ' region-overlay--capturing' : ''}`}
      tabIndex={-1}
      aria-label="Select a region to capture"
      onContextMenu={(event) => {
        event.preventDefault()
        window.lumierePlatform.cancelRegionOverlay()
      }}
      onPointerDown={(event) => {
        if (capturing || event.button !== 0) {
          return
        }
        const point = { x: event.clientX, y: event.clientY }
        startPoint.current = point
        setPointer(point)
        setSelection(projectSelection(point))
        event.currentTarget.setPointerCapture(event.pointerId)
      }}
      onPointerMove={(event) => {
        if (capturing) {
          return
        }
        const point = { x: event.clientX, y: event.clientY }
        setPointer(point)
        if (startPoint.current) {
          setSelection(projectSelection(point))
        }
      }}
      onPointerUp={(event) => {
        if (!capturing && event.button === 0) {
          finishSelection({ x: event.clientX, y: event.clientY })
        }
      }}
    >
      {snapshot ? (
        <img
          className="region-overlay-preview"
          src={snapshot.previewUrl}
          draggable={false}
          aria-hidden="true"
          onLoad={(event) => {
            if (announcedReady.current) return
            void event.currentTarget
              .decode()
              .catch(() => undefined)
              .then(() => {
                window.requestAnimationFrame(() => {
                  window.requestAnimationFrame(() => {
                    if (announcedReady.current) return
                    announcedReady.current = true
                    root.current?.focus()
                    window.lumierePlatform.regionOverlayReady()
                  })
                })
              })
          }}
          onError={() => {
            window.lumierePlatform.cancelRegionOverlay()
          }}
        />
      ) : null}
      {!selection ? <div className="region-overlay-scrim" aria-hidden="true" /> : null}
      {pointer && !capturing ? (
        <>
          <span
            className="region-crosshair region-crosshair--horizontal"
            style={{ top: pointer.y }}
          />
          <span
            className="region-crosshair region-crosshair--vertical"
            style={{ left: pointer.x }}
          />
        </>
      ) : null}
      {selection ? <Selection selection={selection} /> : null}
      <div className={`region-overlay-hint${capturing ? ' region-overlay-hint--capturing' : ''}`}>
        <span className="region-overlay-hint-dot" aria-hidden="true" />
        {hint}
      </div>
    </main>
  )
}

function Selection({ selection }: { selection: OverlaySelection }): React.JSX.Element {
  const labelAbove = selection.top + selection.height + 34 > window.innerHeight
  return (
    <>
      <div
        className={`region-selection${selection.valid ? '' : ' region-selection--invalid'}`}
        style={{
          left: selection.left,
          top: selection.top,
          width: selection.width,
          height: selection.height,
        }}
      />
      <div
        className={`region-selection-size${selection.valid ? '' : ' region-selection-size--invalid'}`}
        style={{
          left: Math.min(selection.left, window.innerWidth - 120),
          top: labelAbove ? Math.max(selection.top - 26, 8) : selection.top + selection.height + 8,
        }}
      >
        {Math.round(selection.geometry.width)} × {Math.round(selection.geometry.height)}
        {selection.valid ? '' : ' · too small'}
      </div>
    </>
  )
}

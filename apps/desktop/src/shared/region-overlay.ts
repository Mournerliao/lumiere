import type { CaptureGeometry } from './platform-contract'

export const minimumRegionSize = {
  width: 32,
  height: 24,
} as const

export interface OverlayPoint {
  x: number
  y: number
}

export interface OverlaySize {
  width: number
  height: number
}

export interface OverlaySelection {
  left: number
  top: number
  width: number
  height: number
  geometry: CaptureGeometry
  valid: boolean
}

export function projectOverlaySelection(
  start: OverlayPoint,
  current: OverlayPoint,
  viewport: OverlaySize,
  target: OverlaySize,
): OverlaySelection | null {
  if (!isPositiveSize(viewport) || !isPositiveSize(target)) {
    return null
  }

  const first = clampPoint(start, viewport)
  const second = clampPoint(current, viewport)
  const left = Math.min(first.x, second.x)
  const top = Math.min(first.y, second.y)
  const width = Math.abs(second.x - first.x)
  const height = Math.abs(second.y - first.y)
  const scaleX = target.width / viewport.width
  const scaleY = target.height / viewport.height
  const geometry: CaptureGeometry = {
    coordinateSpace: 'target-logical',
    x: left * scaleX,
    y: top * scaleY,
    width: width * scaleX,
    height: height * scaleY,
  }

  return {
    left,
    top,
    width,
    height,
    geometry,
    valid: geometry.width >= minimumRegionSize.width && geometry.height >= minimumRegionSize.height,
  }
}

function clampPoint(point: OverlayPoint, viewport: OverlaySize): OverlayPoint {
  return {
    x: Math.min(Math.max(point.x, 0), viewport.width),
    y: Math.min(Math.max(point.y, 0), viewport.height),
  }
}

function isPositiveSize(size: OverlaySize): boolean {
  return (
    Number.isFinite(size.width) && Number.isFinite(size.height) && size.width > 0 && size.height > 0
  )
}

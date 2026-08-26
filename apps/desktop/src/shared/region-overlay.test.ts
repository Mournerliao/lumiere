import { describe, expect, it } from 'vitest'
import { projectOverlaySelection } from './region-overlay'

describe('projectOverlaySelection', () => {
  it('normalizes reverse drags into target-local logical geometry', () => {
    expect(
      projectOverlaySelection(
        { x: 600, y: 400 },
        { x: 100, y: 80 },
        { width: 1000, height: 500 },
        { width: 2000, height: 1000 },
      ),
    ).toMatchObject({
      left: 100,
      top: 80,
      width: 500,
      height: 320,
      geometry: {
        coordinateSpace: 'target-logical',
        x: 200,
        y: 160,
        width: 1000,
        height: 640,
      },
      valid: true,
    })
  })

  it('clamps pointer movement to the overlay bounds', () => {
    expect(
      projectOverlaySelection(
        { x: 50, y: 40 },
        { x: 1200, y: -30 },
        { width: 800, height: 600 },
        { width: 800, height: 600 },
      ),
    ).toMatchObject({
      left: 50,
      top: 0,
      width: 750,
      height: 40,
      geometry: { x: 50, y: 0, width: 750, height: 40 },
    })
  })

  it('marks clicks and too-small regions as invalid', () => {
    expect(
      projectOverlaySelection(
        { x: 20, y: 20 },
        { x: 45, y: 34 },
        { width: 500, height: 300 },
        { width: 500, height: 300 },
      )?.valid,
    ).toBe(false)
  })

  it('rejects an unavailable viewport instead of producing invalid geometry', () => {
    expect(
      projectOverlaySelection(
        { x: 0, y: 0 },
        { x: 10, y: 10 },
        { width: 0, height: 100 },
        { width: 100, height: 100 },
      ),
    ).toBeNull()
  })
})

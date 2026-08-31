import { describe, expect, it } from 'vitest'
import { RegionPreviewRegistry } from './region-preview-registry'

describe('RegionPreviewRegistry', () => {
  it('grants and revokes an opaque URL without exposing the file path', () => {
    const registry = new RegionPreviewRegistry('/tmp/lumiere-region-preview')
    const granted = registry.grant('/tmp/lumiere-region-preview/session-1.png')

    expect(granted.url).not.toContain('/tmp')
    expect(registry.resolve(granted.url)).toBe('/tmp/lumiere-region-preview/session-1.png')

    registry.revoke(granted.token)
    expect(registry.resolve(granted.url)).toBeNull()
  })

  it('rejects paths outside the controlled preview directory', () => {
    const registry = new RegionPreviewRegistry('/tmp/lumiere-region-preview')

    expect(() => registry.grant('/tmp/other/secret.png')).toThrow(/controlled temporary/)
  })

  it('fails closed for malformed and cross-shape URLs', () => {
    const registry = new RegionPreviewRegistry('/tmp/lumiere-region-preview')
    const granted = registry.grant('/tmp/lumiere-region-preview/session-1.png')

    expect(registry.resolve(`${granted.url}?path=/tmp/other`)).toBeNull()
    expect(registry.resolve(granted.url.replace('://frame/', '://other/'))).toBeNull()
  })
})

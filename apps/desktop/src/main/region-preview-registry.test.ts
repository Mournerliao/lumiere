import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { describe, expect, it } from 'vitest'
import { RegionPreviewRegistry } from './region-preview-registry'

const previewDirectory = join(tmpdir(), 'lumiere-region-preview')
const previewFile = join(previewDirectory, 'session-1.png')

describe('RegionPreviewRegistry', () => {
  it('grants and revokes an opaque URL without exposing the file path', () => {
    const registry = new RegionPreviewRegistry(previewDirectory)
    const granted = registry.grant(previewFile)

    expect(granted.url).not.toContain(previewDirectory)
    expect(registry.resolve(granted.url)).toBe(previewFile)

    registry.revoke(granted.token)
    expect(registry.resolve(granted.url)).toBeNull()
  })

  it('rejects paths outside the controlled preview directory', () => {
    const registry = new RegionPreviewRegistry(previewDirectory)

    expect(() => registry.grant(join(tmpdir(), 'other', 'secret.png'))).toThrow(
      /controlled temporary/,
    )
  })

  it('fails closed for malformed and cross-shape URLs', () => {
    const registry = new RegionPreviewRegistry(previewDirectory)
    const granted = registry.grant(previewFile)

    expect(registry.resolve(`${granted.url}?path=/tmp/other`)).toBeNull()
    expect(registry.resolve(granted.url.replace('://frame/', '://other/'))).toBeNull()
  })
})

import { readFile } from 'node:fs/promises'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

const desktopRoot = process.cwd()

describe('macOS packaging policy', () => {
  it('keeps the stable identity, universal host layout, and ad-hoc signing boundary', async () => {
    const desktopPackage = JSON.parse(
      await readFile(resolve(desktopRoot, 'package.json'), 'utf8'),
    ) as Record<string, unknown>
    const builderConfig = JSON.parse(
      await readFile(resolve(desktopRoot, 'electron-builder.json'), 'utf8'),
    ) as {
      appId?: unknown
      productName?: unknown
      extraResources?: unknown
      mac?: Record<string, unknown>
    }

    expect(desktopPackage.version).toBe('0.1.0')
    expect(desktopPackage.productName).toBe('Lumiere')
    expect(builderConfig.appId).toBe('io.github.sousouliao.lumiere')
    expect(builderConfig.productName).toBe('Lumiere')
    expect(builderConfig.extraResources).toContainEqual({
      from: '../../hosts/macos/.build/distribution/staged/${arch}/LumiereMacHost',
      to: 'macos-host/LumiereMacHost',
    })
    expect(builderConfig.mac).toMatchObject({
      target: 'dir',
      minimumSystemVersion: '15.0',
      identity: '-',
      hardenedRuntime: true,
      binaries: ['Contents/Resources/macos-host/LumiereMacHost'],
      notarize: false,
    })
  })
})

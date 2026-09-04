import { readFile } from 'node:fs/promises'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

const desktopRoot = process.cwd()

describe('development Electron runtime', () => {
  it('resolves the Electron 43 lazy-installed executable before dev and preview', async () => {
    const desktopPackage = JSON.parse(
      await readFile(resolve(desktopRoot, 'package.json'), 'utf8'),
    ) as { scripts?: Record<string, unknown> }

    expect(desktopPackage.scripts?.predev).toBe(
      'node ../../scripts/prepare-development-electron.mjs && node ../../scripts/prepare-development-host.mjs',
    )
    expect(desktopPackage.scripts?.prestart).toBe(
      'node ../../scripts/prepare-development-electron.mjs',
    )
  })
})

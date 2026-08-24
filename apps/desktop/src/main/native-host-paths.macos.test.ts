import { describe, expect, it } from 'vitest'
import { macOSHostCandidates } from './native-host-paths'

describe('macOS native host paths', () => {
  it('uses the packaged resources directory in production', () => {
    expect(
      macOSHostCandidates({
        appPath: '/Applications/Lumiere.app/Contents/Resources/app.asar',
        isPackaged: true,
        resourcesPath: '/Applications/Lumiere.app/Contents/Resources',
      }),
    ).toEqual(['/Applications/Lumiere.app/Contents/Resources/macos-host/LumiereMacHost'])
  })

  it('prefers a release Swift build and falls back to debug during development', () => {
    expect(
      macOSHostCandidates({
        appPath: '/workspace/lumiere/apps/desktop',
        isPackaged: false,
        resourcesPath: '/unused',
      }),
    ).toEqual([
      '/workspace/lumiere/hosts/macos/.build/release/LumiereMacHost',
      '/workspace/lumiere/hosts/macos/.build/debug/LumiereMacHost',
    ])
  })

  it('honors an explicit development override', () => {
    expect(
      macOSHostCandidates({
        appPath: '/workspace/lumiere/apps/desktop',
        isPackaged: false,
        resourcesPath: '/unused',
        overridePath: '/tmp/LumiereMacHost',
      }),
    ).toEqual(['/tmp/LumiereMacHost'])
  })
})

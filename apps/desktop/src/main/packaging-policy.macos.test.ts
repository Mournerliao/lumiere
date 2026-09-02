import { mkdtemp, readFile, rm, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join, resolve } from 'node:path'
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
      dmg?: Record<string, unknown>
      productName?: unknown
      extraResources?: unknown
      mac?: Record<string, unknown>
    }
    const repositoryPackage = JSON.parse(
      await readFile(resolve(desktopRoot, '..', '..', 'package.json'), 'utf8'),
    ) as { scripts?: Record<string, unknown> }

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
    expect(builderConfig.dmg).toMatchObject({
      artifactName: '${productName}-${version}-macos-${arch}.${ext}',
      title: 'Lumiere ${version}',
      backgroundColor: '#f4f2ee',
      filesystem: 'HFS+',
      format: 'UDZO',
      sign: false,
      writeUpdateInfo: false,
      contents: [
        { x: 140, y: 160, type: 'file' },
        { x: 380, y: 160, type: 'link', path: '/Applications' },
      ],
    })
    expect(repositoryPackage.scripts?.['release:macos']).toBe('node ./scripts/release-macos.mjs')
  })

  it('derives the release name and checksum entry from final artifact bytes', async () => {
    const releaseModule: unknown = await import(
      // @ts-expect-error Repository release scripts intentionally remain plain Node modules.
      '../../../../scripts/release-macos.mjs'
    )
    const { checksumManifestLine, macOSReleaseArtifactName, sha256File } = releaseModule as {
      checksumManifestLine: (digest: string, fileName: string) => string
      macOSReleaseArtifactName: (version: string) => string
      sha256File: (filePath: string) => Promise<string>
    }
    const directory = await mkdtemp(join(tmpdir(), 'lumiere-release-policy-'))
    const artifactPath = join(directory, 'artifact.dmg')

    try {
      await writeFile(artifactPath, 'abc', 'utf8')
      const digest = await sha256File(artifactPath)

      expect(macOSReleaseArtifactName('0.1.0')).toBe('Lumiere-0.1.0-macos-universal.dmg')
      expect(macOSReleaseArtifactName('0.1.0-beta.1+build.7')).toBe(
        'Lumiere-0.1.0-beta.1+build.7-macos-universal.dmg',
      )
      expect(digest).toBe('ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad')
      expect(checksumManifestLine(digest, 'Lumiere-0.1.0-macos-universal.dmg')).toBe(
        `${digest}  Lumiere-0.1.0-macos-universal.dmg\n`,
      )
    } finally {
      await rm(directory, { force: true, recursive: true })
    }
  })
})

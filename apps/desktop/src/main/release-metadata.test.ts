import { mkdtemp, rm, writeFile } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { beforeAll, describe, expect, it } from 'vitest'

interface ReleaseModule {
  compareSemver: (left: string, right: string) => number
  createChecksumManifest: (directory: string) => Promise<string>
  createReleaseNotes: (release: ReleaseEntry) => string
  finalizeChangelog: (source: string, date: string) => string
  parseChangelog: (source: string) => ParsedChangelog
  parseSemver: (value: string) => unknown
  validateReleaseBaseline: (
    parsed: ParsedChangelog,
    packageVersion: string,
    latestStableTag: string,
  ) => ReleaseEntry
  validateInstallationGuidance: (readme: string, platforms: string[]) => void
  validateReleaseState: (input: {
    changelog: string
    packageVersion: string
    publishing?: boolean
  }) => ParsedChangelog
}

let releaseModule: ReleaseModule

beforeAll(async () => {
  const loaded: unknown = await import(
    // @ts-expect-error Repository release scripts intentionally remain plain Node modules.
    '../../../../scripts/release-metadata.mjs'
  )
  releaseModule = loaded as ReleaseModule
})

const compareSemver = (...args: Parameters<ReleaseModule['compareSemver']>) =>
  releaseModule.compareSemver(...args)
const createChecksumManifest = (...args: Parameters<ReleaseModule['createChecksumManifest']>) =>
  releaseModule.createChecksumManifest(...args)
const createReleaseNotes = (...args: Parameters<ReleaseModule['createReleaseNotes']>) =>
  releaseModule.createReleaseNotes(...args)
const finalizeChangelog = (...args: Parameters<ReleaseModule['finalizeChangelog']>) =>
  releaseModule.finalizeChangelog(...args)
const parseChangelog = (...args: Parameters<ReleaseModule['parseChangelog']>) =>
  releaseModule.parseChangelog(...args)
const parseSemver = (...args: Parameters<ReleaseModule['parseSemver']>) =>
  releaseModule.parseSemver(...args)
const validateReleaseBaseline = (...args: Parameters<ReleaseModule['validateReleaseBaseline']>) =>
  releaseModule.validateReleaseBaseline(...args)
const validateInstallationGuidance = (
  ...args: Parameters<ReleaseModule['validateInstallationGuidance']>
) => {
  releaseModule.validateInstallationGuidance(...args)
}
const validateReleaseState = (...args: Parameters<ReleaseModule['validateReleaseState']>) =>
  releaseModule.validateReleaseState(...args)

interface ReleaseEntry {
  version: string
  platforms: string[]
  categories: { name: string; content: string }[]
}

interface ParsedChangelog {
  candidate?: { targetVersion: string; platforms: string[] }
  releases: ReleaseEntry[]
}

const released = `# Changelog

All notable user-visible changes to Lumiere are documented in this file.

## [Unreleased]

## [0.1.0] - 2026-09-03

Release platforms: macOS

### Added

- First release.

[0.1.0]: https://github.com/Mournerliao/lumiere/releases/tag/v0.1.0
`

function withCandidate({ version = '0.1.1', platforms = 'macOS' } = {}) {
  return released.replace(
    '## [Unreleased]\n',
    `## [Unreleased]

Target version: \`${version}\`
Release platforms: ${platforms}

### Fixed

- Improved capture reliability.
`,
  )
}

describe('release metadata', () => {
  it('accepts stable and prerelease semantic versions and orders them correctly', () => {
    expect(parseSemver('0.1.1')).toBeDefined()
    expect(parseSemver('1.0.0-rc.1+build.7')).toBeDefined()
    expect(compareSemver('0.2.0', '0.1.9')).toBe(1)
    expect(compareSemver('1.0.0', '1.0.0-rc.1')).toBe(1)
    expect(() => parseSemver('v0.1')).toThrow('Invalid semantic version')
    expect(() => parseSemver('1.0.0-rc.01')).toThrow('Invalid semantic version')
  })

  it('accepts an empty Unreleased section or one complete prepared candidate', () => {
    expect(validateReleaseState({ changelog: released, packageVersion: '0.1.0' }).candidate).toBe(
      undefined,
    )
    const parsed = validateReleaseState({
      changelog: withCandidate({ version: '0.2.0', platforms: 'macOS, Windows' }),
      packageVersion: '0.2.0',
    })
    expect(parsed.candidate).toEqual(
      expect.objectContaining({ targetVersion: '0.2.0', platforms: ['macOS', 'Windows'] }),
    )
  })

  it('rejects incomplete, unknown, duplicated, and out-of-order release metadata', () => {
    expect(() => parseChangelog(withCandidate().replace('Target version: `0.1.1`\n', ''))).toThrow(
      'missing Target version',
    )
    expect(() => parseChangelog(withCandidate({ platforms: 'Linux' }))).toThrow(
      'invalid release platforms',
    )
    expect(() =>
      parseChangelog(
        withCandidate().replace(
          '### Fixed\n\n- Improved capture reliability.',
          '### Fixed\n\n- Improved capture reliability.\n\n### Added\n\n- Out of order.',
        ),
      ),
    ).toThrow('canonical order')
    expect(() =>
      parseChangelog(
        released.replace('## [0.1.0]', '## [0.1.0]').replace(
          '[0.1.0]:',
          `## [0.1.0] - 2026-09-02

Release platforms: macOS

### Fixed

- Duplicate.

[0.1.0]:`,
        ),
      ),
    ).toThrow('unique')
    const outOfOrder = released.replace(
      '## [0.1.0] - 2026-09-03',
      `## [0.1.0] - 2026-09-03

Release platforms: macOS

### Added

- First release.

## [0.2.0] - 2026-09-04`,
    )
    expect(() => parseChangelog(outOfOrder)).toThrow('descending')
  })

  it('requires package and changelog versions to agree and finalization before publishing', () => {
    expect(() =>
      validateReleaseState({ changelog: withCandidate(), packageVersion: '0.1.0' }),
    ).toThrow('does not match')
    expect(() =>
      validateReleaseState({
        changelog: withCandidate(),
        packageVersion: '0.1.1',
        publishing: true,
      }),
    ).toThrow('must be finalized')
  })

  it('matches the previous stable changelog entry to the GitHub Release baseline', () => {
    const stable = validateReleaseBaseline(
      parseChangelog(finalizeChangelog(withCandidate(), '2026-09-10')),
      '0.1.1',
      'v0.1.0',
    )
    expect(stable.version).toBe('0.1.0')
    expect(() =>
      validateReleaseBaseline(
        parseChangelog(finalizeChangelog(withCandidate(), '2026-09-10')),
        '0.1.1',
        'v0.0.9',
      ),
    ).toThrow('does not match')
  })

  it('requires installation guidance for every selected platform', () => {
    const readme = '## Install on macOS\n\nInstructions.\n'
    expect(() => {
      validateInstallationGuidance(readme, ['macOS'])
    }).not.toThrow()
    expect(() => {
      validateInstallationGuidance(readme, ['macOS', 'Windows'])
    }).toThrow('installation instructions for Windows')
  })

  it('promotes the candidate without changing history and restores empty Unreleased', () => {
    const finalized = finalizeChangelog(
      withCandidate({ version: '0.2.0', platforms: 'macOS, Windows' }),
      '2026-09-10',
    )
    const parsed = validateReleaseState({
      changelog: finalized,
      packageVersion: '0.2.0',
      publishing: true,
    })
    expect(parsed.candidate).toBeUndefined()
    expect(parsed.releases.map((entry) => entry.version)).toEqual(['0.2.0', '0.1.0'])
    expect(finalized).toContain('## [Unreleased]\n\n## [0.2.0] - 2026-09-10')
    expect(finalized).toContain('## [0.1.0] - 2026-09-03')
    expect(finalized).toContain(
      '[0.2.0]: https://github.com/Mournerliao/lumiere/compare/v0.1.0...v0.2.0',
    )
  })

  it('generates notes from only the selected release entry', () => {
    const finalized = finalizeChangelog(withCandidate(), '2026-09-10')
    const release = parseChangelog(finalized).releases[0]
    const notes = createReleaseNotes(release)
    expect(notes).toContain('## Fixed\n\n- Improved capture reliability.')
    expect(notes).toContain('Lumiere-0.1.1-macos-universal.dmg')
    expect(notes).toContain('/blob/v0.1.1/README.md')
    expect(notes).toContain('- Improved capture reliability.\n\n## Downloads')
    expect(notes).not.toContain('First release')
    expect(notes).not.toContain('Target version')
  })

  it('creates one deterministic checksum manifest for selected platform binaries', async () => {
    const directory = await mkdtemp(join(tmpdir(), 'lumiere-release-metadata-'))
    try {
      await writeFile(join(directory, 'Lumiere-0.2.0-macos-universal.dmg'), 'mac', 'utf8')
      await writeFile(join(directory, 'Lumiere-Setup-0.2.0-x64.exe'), 'windows', 'utf8')
      await writeFile(join(directory, 'latest.yml'), 'ignored', 'utf8')
      const manifest = await createChecksumManifest(directory)
      expect(manifest.trim().split('\n')).toHaveLength(2)
      expect(manifest).toContain('Lumiere-0.2.0-macos-universal.dmg')
      expect(manifest).toContain('Lumiere-Setup-0.2.0-x64.exe')
      expect(manifest).not.toContain('latest.yml')
    } finally {
      await rm(directory, { force: true, recursive: true })
    }
  })
})

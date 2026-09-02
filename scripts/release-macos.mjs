import { createHash } from 'node:crypto'
import { createReadStream } from 'node:fs'
import { access, readFile, rename, rm, writeFile } from 'node:fs/promises'
import { basename, dirname, join, resolve } from 'node:path'
import process from 'node:process'
import { fileURLToPath, pathToFileURL } from 'node:url'
import { macOSPackagingPolicy, packageMacOS } from './package-macos.mjs'

const repositoryRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const artifactsDirectory = join(repositoryRoot, 'artifacts', 'macos')
const checksumManifestPath = join(artifactsDirectory, 'SHA256SUMS')

export function macOSReleaseArtifactName(version) {
  if (
    typeof version !== 'string' ||
    !/^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$/.test(version)
  ) {
    throw new Error(`Invalid release version: ${String(version)}`)
  }
  return `${macOSPackagingPolicy.productName}-${version}-macos-universal.dmg`
}

export async function sha256File(filePath) {
  const hash = createHash('sha256')
  for await (const chunk of createReadStream(filePath)) {
    hash.update(chunk)
  }
  return hash.digest('hex')
}

export function checksumManifestLine(digest, fileName) {
  if (!/^[0-9a-f]{64}$/.test(digest)) {
    throw new Error('SHA-256 digest must contain 64 lowercase hexadecimal characters.')
  }
  if (basename(fileName) !== fileName || fileName.includes('\n')) {
    throw new Error('Checksum file name must be one plain path segment.')
  }
  return `${digest}  ${fileName}\n`
}

async function prepareChecksumManifest(dmgPath) {
  const digest = await sha256File(dmgPath)
  const manifest = checksumManifestLine(digest, basename(dmgPath))
  const temporaryPath = `${checksumManifestPath}.tmp`
  await writeFile(temporaryPath, manifest, 'utf8')
  return temporaryPath
}

export async function releaseMacOS() {
  const { version } = await packageMacOS({ targets: ['dir', 'dmg'] })
  const artifactName = macOSReleaseArtifactName(version)
  const builtDmgPath = join(artifactsDirectory, 'build', artifactName)
  const finalDmgPath = join(artifactsDirectory, artifactName)

  await access(builtDmgPath)
  await rm(`${builtDmgPath}.blockmap`, { force: true })
  const temporaryManifestPath = await prepareChecksumManifest(builtDmgPath)
  await rm(finalDmgPath, { force: true })
  await rename(builtDmgPath, finalDmgPath)
  await rename(temporaryManifestPath, checksumManifestPath)

  const writtenManifest = await readFile(checksumManifestPath, 'utf8')
  const verifiedDigest = await sha256File(finalDmgPath)
  if (writtenManifest !== checksumManifestLine(verifiedDigest, artifactName)) {
    throw new Error('The written SHA256SUMS entry does not match the final disk image.')
  }

  console.log(`Release disk image: ${finalDmgPath}`)
  console.log(`SHA-256 manifest: ${checksumManifestPath}`)
  return { checksumManifestPath, dmgPath: finalDmgPath, version }
}

const invokedUrl = process.argv[1] ? pathToFileURL(resolve(process.argv[1])).href : undefined
if (import.meta.url === invokedUrl) {
  releaseMacOS().catch((error) => {
    console.error(error instanceof Error ? error.message : error)
    process.exitCode = 1
  })
}

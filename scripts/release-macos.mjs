import { createHash } from 'node:crypto'
import { createReadStream } from 'node:fs'
import { access, readFile, rename, rm, stat, writeFile } from 'node:fs/promises'
import { basename, dirname, join, resolve } from 'node:path'
import process from 'node:process'
import { fileURLToPath, pathToFileURL } from 'node:url'
import { macOSPackagingPolicy, packageMacOS } from './package-macos.mjs'

const repositoryRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const artifactsDirectory = join(repositoryRoot, 'artifacts', 'macos')
const checksumManifestPath = join(artifactsDirectory, 'SHA256SUMS')
const maximumDmgBytes = 140 * 1024 * 1024

export function macOSReleaseArtifactName(version, architecture) {
  if (
    typeof version !== 'string' ||
    !/^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$/.test(version)
  ) {
    throw new Error(`Invalid release version: ${String(version)}`)
  }
  if (!macOSPackagingPolicy.architectures.includes(architecture)) {
    throw new Error(`Unsupported macOS release architecture: ${String(architecture)}`)
  }
  return `${macOSPackagingPolicy.productName}-${version}-macos-${architecture}.dmg`
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

export async function checksumManifestForArtifacts(dmgPaths) {
  const manifestLines = []
  for (const dmgPath of dmgPaths) {
    manifestLines.push(checksumManifestLine(await sha256File(dmgPath), basename(dmgPath)))
  }
  return manifestLines.join('')
}

async function prepareChecksumManifest(dmgPaths) {
  const temporaryPath = `${checksumManifestPath}.tmp`
  await writeFile(temporaryPath, await checksumManifestForArtifacts(dmgPaths), 'utf8')
  return temporaryPath
}

export async function releaseMacOS() {
  const { architectures, version } = await packageMacOS({ targets: ['dir', 'dmg'] })
  const dmgPaths = {}
  for (const architecture of architectures) {
    const artifactName = macOSReleaseArtifactName(version, architecture)
    const builtDmgPath = join(artifactsDirectory, 'build', artifactName)
    const finalDmgPath = join(artifactsDirectory, artifactName)
    await access(builtDmgPath)
    await rm(`${builtDmgPath}.blockmap`, { force: true })
    await rm(finalDmgPath, { force: true })
    await rename(builtDmgPath, finalDmgPath)
    const dmgBytes = (await stat(finalDmgPath)).size
    if (dmgBytes > maximumDmgBytes) {
      throw new Error(
        `${finalDmgPath} is ${(dmgBytes / 1024 / 1024).toFixed(2)} MiB, exceeding the 140 MiB packaging budget.`,
      )
    }
    dmgPaths[architecture] = finalDmgPath
    console.log(`Release ${architecture} disk image: ${finalDmgPath}`)
  }

  const orderedDmgPaths = architectures.map((architecture) => dmgPaths[architecture])
  const temporaryManifestPath = await prepareChecksumManifest(orderedDmgPaths)
  await rename(temporaryManifestPath, checksumManifestPath)

  const writtenManifest = await readFile(checksumManifestPath, 'utf8')
  if (writtenManifest !== (await checksumManifestForArtifacts(orderedDmgPaths))) {
    throw new Error('The written SHA256SUMS entry does not match the final disk image.')
  }

  console.log(`SHA-256 manifest: ${checksumManifestPath}`)
  return { checksumManifestPath, dmgPaths, version }
}

const invokedUrl = process.argv[1] ? pathToFileURL(resolve(process.argv[1])).href : undefined
if (import.meta.url === invokedUrl) {
  releaseMacOS().catch((error) => {
    console.error(error instanceof Error ? error.message : error)
    process.exitCode = 1
  })
}

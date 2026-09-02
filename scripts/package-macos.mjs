import { spawn } from 'node:child_process'
import { access, chmod, copyFile, mkdir, readFile, readdir, rename, rm } from 'node:fs/promises'
import { dirname, join, resolve } from 'node:path'
import process from 'node:process'
import { fileURLToPath, pathToFileURL } from 'node:url'

const repositoryRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const desktopRoot = join(repositoryRoot, 'apps', 'desktop')
const hostRoot = join(repositoryRoot, 'hosts', 'macos')
const builderConfigPath = join(desktopRoot, 'electron-builder.json')
const finalAppPath = join(repositoryRoot, 'artifacts', 'macos', 'Lumiere.app')
const architectures = ['arm64', 'x64']

export const macOSPackagingPolicy = Object.freeze({
  appId: 'io.github.sousouliao.lumiere',
  productName: 'Lumiere',
  minimumSystemVersion: '15.0',
  architectures,
  finalAppPath,
})

export function swiftTriple(architecture) {
  if (architecture === 'arm64') return 'arm64-apple-macosx15.0'
  if (architecture === 'x64') return 'x86_64-apple-macosx15.0'
  throw new Error(`Unsupported macOS packaging architecture: ${architecture}`)
}

export async function packageMacOS({ targets = ['dir'] } = {}) {
  if (process.platform !== 'darwin') {
    throw new Error('The macOS application bundle must be built on macOS.')
  }
  if (!targets.includes('dir') || targets.some((target) => target !== 'dir' && target !== 'dmg')) {
    throw new Error('macOS packaging targets must include dir and may additionally include dmg.')
  }

  const desktopPackage = await readJson(join(desktopRoot, 'package.json'))
  const builderConfig = await readJson(builderConfigPath)
  validateConfiguration(desktopPackage, builderConfig)

  const developerDirectory = process.env.DEVELOPER_DIR || (await discoverXcodeDeveloperDirectory())
  if (!developerDirectory) {
    throw new Error('A complete Xcode installation is required to package Lumiere for macOS.')
  }
  const buildEnvironment = { ...process.env, DEVELOPER_DIR: developerDirectory }

  await run('pnpm', ['--filter', '@lumiere/desktop', 'build'], {
    cwd: repositoryRoot,
    env: process.env,
  })

  for (const architecture of architectures) {
    await buildAndStageHost(architecture, buildEnvironment)
  }

  await run(
    'pnpm',
    [
      '--filter',
      '@lumiere/desktop',
      'exec',
      'electron-builder',
      '--mac',
      ...targets,
      '--universal',
      '--config',
      'electron-builder.json',
    ],
    { cwd: repositoryRoot, env: process.env },
  )

  const builtAppPath = join(
    repositoryRoot,
    'artifacts',
    'macos',
    'build',
    'mac-universal',
    'Lumiere.app',
  )
  await access(builtAppPath)
  await mkdir(dirname(finalAppPath), { recursive: true })
  await rm(finalAppPath, { force: true, recursive: true })
  await rename(builtAppPath, finalAppPath)

  await verifyBundle(desktopPackage.version)
  console.log(`Packaged application: ${finalAppPath}`)
  return { finalAppPath, version: desktopPackage.version }
}

async function buildAndStageHost(architecture, environment) {
  const triple = swiftTriple(architecture)
  const scratchPath = join(hostRoot, '.build', 'distribution', architecture)
  const swiftArguments = [
    'swift',
    'build',
    '--package-path',
    hostRoot,
    '--configuration',
    'release',
    '--triple',
    triple,
    '--scratch-path',
    scratchPath,
  ]

  await run('/usr/bin/xcrun', swiftArguments, { cwd: repositoryRoot, env: environment })
  const binaryDirectory = await capture('/usr/bin/xcrun', [...swiftArguments, '--show-bin-path'], {
    cwd: repositoryRoot,
    env: environment,
  })
  const sourcePath = join(binaryDirectory.trim(), 'LumiereMacHost')
  const stagedDirectory = join(hostRoot, '.build', 'distribution', 'staged', architecture)
  const stagedPath = join(stagedDirectory, 'LumiereMacHost')
  await mkdir(stagedDirectory, { recursive: true })
  await copyFile(sourcePath, stagedPath)
  await chmod(stagedPath, 0o755)
}

async function verifyBundle(version) {
  const contentsPath = join(finalAppPath, 'Contents')
  const executablePath = join(contentsPath, 'MacOS', 'Lumiere')
  const hostPath = join(contentsPath, 'Resources', 'macos-host', 'LumiereMacHost')
  const infoPlistPath = join(contentsPath, 'Info.plist')

  await run('/usr/bin/codesign', ['--verify', '--deep', '--strict', '--verbose=2', finalAppPath])
  await expectCommandOutput(
    '/usr/bin/plutil',
    ['-extract', 'CFBundleIdentifier', 'raw', infoPlistPath],
    macOSPackagingPolicy.appId,
  )
  await expectCommandOutput(
    '/usr/bin/plutil',
    ['-extract', 'CFBundleShortVersionString', 'raw', infoPlistPath],
    version,
  )
  await expectCommandOutput(
    '/usr/bin/plutil',
    ['-extract', 'LSMinimumSystemVersion', 'raw', infoPlistPath],
    macOSPackagingPolicy.minimumSystemVersion,
  )
  await expectArchitectures(executablePath)
  await expectArchitectures(hostPath)

  console.log('Electron executable build metadata:')
  await run('/usr/bin/vtool', ['-show-build', executablePath])
  console.log('LumiereMacHost build metadata:')
  await run('/usr/bin/vtool', ['-show-build', hostPath])
}

async function expectArchitectures(binaryPath) {
  const output = await capture('/usr/bin/lipo', ['-archs', binaryPath])
  const actual = new Set(output.trim().split(/\s+/))
  const expected = new Set(['arm64', 'x86_64'])
  if (actual.size !== expected.size || [...expected].some((arch) => !actual.has(arch))) {
    throw new Error(`${binaryPath} has unexpected architectures: ${output.trim()}`)
  }
}

async function expectCommandOutput(command, args, expected) {
  const actual = (await capture(command, args)).trim()
  if (actual !== expected) {
    throw new Error(
      `${args.join(' ')} returned ${JSON.stringify(actual)}; expected ${JSON.stringify(expected)}.`,
    )
  }
}

function validateConfiguration(desktopPackage, builderConfig) {
  if (desktopPackage.productName !== macOSPackagingPolicy.productName) {
    throw new Error('The desktop productName does not match the macOS packaging policy.')
  }
  if (builderConfig.appId !== macOSPackagingPolicy.appId) {
    throw new Error('The electron-builder appId does not match the macOS packaging policy.')
  }
  if (builderConfig.productName !== macOSPackagingPolicy.productName) {
    throw new Error('The electron-builder productName does not match the macOS packaging policy.')
  }
  if (builderConfig.mac?.minimumSystemVersion !== macOSPackagingPolicy.minimumSystemVersion) {
    throw new Error('The electron-builder minimum system version does not match the policy.')
  }
  if (builderConfig.mac?.identity !== '-') {
    throw new Error('The macOS bundle must use an explicit ad-hoc signing identity.')
  }
}

async function discoverXcodeDeveloperDirectory() {
  const entries = await readdir('/Applications', { withFileTypes: true })
  const xcodeApplications = entries
    .filter((entry) => entry.isDirectory() && /^Xcode(?:-.+)?\.app$/.test(entry.name))
    .map((entry) => entry.name)
    .sort((left, right) => {
      if (left === 'Xcode.app') return -1
      if (right === 'Xcode.app') return 1
      return left.localeCompare(right)
    })

  for (const application of xcodeApplications) {
    const developerDirectory = join('/Applications', application, 'Contents', 'Developer')
    try {
      await access(developerDirectory)
      return developerDirectory
    } catch {
      // Continue to another installed Xcode application.
    }
  }
  return undefined
}

async function readJson(path) {
  return JSON.parse(await readFile(path, 'utf8'))
}

function run(command, args, options = {}) {
  return new Promise((resolvePromise, rejectPromise) => {
    const child = spawn(command, args, { ...options, stdio: 'inherit' })
    child.once('error', rejectPromise)
    child.once('exit', (code, signal) => {
      if (code === 0) {
        resolvePromise()
        return
      }
      rejectPromise(
        new Error(
          signal
            ? `${command} was terminated by ${signal}.`
            : `${command} exited with code ${String(code)}.`,
        ),
      )
    })
  })
}

function capture(command, args, options = {}) {
  return new Promise((resolvePromise, rejectPromise) => {
    const child = spawn(command, args, { ...options, stdio: ['ignore', 'pipe', 'inherit'] })
    let stdout = ''
    child.stdout.setEncoding('utf8')
    child.stdout.on('data', (chunk) => {
      stdout += chunk
    })
    child.once('error', rejectPromise)
    child.once('exit', (code, signal) => {
      if (code === 0) {
        resolvePromise(stdout)
        return
      }
      rejectPromise(
        new Error(
          signal
            ? `${command} was terminated by ${signal}.`
            : `${command} exited with code ${String(code)}.`,
        ),
      )
    })
  })
}

const invokedUrl = process.argv[1] ? pathToFileURL(resolve(process.argv[1])).href : undefined
if (import.meta.url === invokedUrl) {
  packageMacOS().catch((error) => {
    console.error(error instanceof Error ? error.message : error)
    process.exitCode = 1
  })
}

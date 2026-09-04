import { access } from 'node:fs/promises'
import { createRequire } from 'node:module'
import { dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const repositoryRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..')
const desktopPackagePath = join(repositoryRoot, 'apps', 'desktop', 'package.json')
const requireFromDesktop = createRequire(desktopPackagePath)

console.log('Preparing Electron development runtime...')
const executablePath = requireFromDesktop('electron')
if (typeof executablePath !== 'string' || executablePath.length === 0) {
  throw new Error('The Electron package did not resolve a development executable.')
}
await access(executablePath)

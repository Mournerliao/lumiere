import { spawn } from 'node:child_process'
import { access, readdir } from 'node:fs/promises'
import { dirname, join, resolve } from 'node:path'
import process from 'node:process'
import { fileURLToPath } from 'node:url'

if (process.platform === 'darwin' && !process.env.LUMIERE_MAC_HOST_PATH) {
  const repositoryRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..')
  const developerDirectory = process.env.DEVELOPER_DIR || (await discoverXcodeDeveloperDirectory())
  console.log('Preparing current macOS development Host...')
  await run('/usr/bin/xcrun', ['swift', 'build', '--package-path', 'hosts/macos'], {
    cwd: repositoryRoot,
    env: developerDirectory ? { ...process.env, DEVELOPER_DIR: developerDirectory } : process.env,
  })
} else if (process.platform === 'win32') {
  console.warn(
    'Windows development Host preparation is pending Issue #7; starting the shared shell with explicit unavailable behavior.',
  )
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

function run(command, args, options) {
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

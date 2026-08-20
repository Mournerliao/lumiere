import { access } from 'node:fs/promises'
import process from 'node:process'

const requiredPaths = [
  'apps/desktop',
  'hosts/macos/README.md',
  'hosts/windows/Lumiere.Windows.sln',
  'hosts/windows/src/Lumiere.Windows.Capture',
  'hosts/windows/src/Lumiere.Windows.Graphics',
  'hosts/windows/src/Lumiere.Windows.Interop',
  'hosts/windows/tests/Lumiere.Windows.Capture.Tests',
  'hosts/windows/tests/Lumiere.Windows.Graphics.Tests',
  'hosts/windows/tests/Lumiere.Windows.Interop.Tests',
  'protocol/platform-host/v1.schema.json',
]

const forbiddenPaths = [
  'src',
  'tests',
  'Lumiere.sln',
  'Directory.Build.props',
  'Directory.Packages.props',
  'knowledge/evidence',
  'hosts/windows/src/Lumiere.App',
  'hosts/windows/src/Lumiere.App.Core',
  'hosts/windows/src/Lumiere.Overlay',
  'hosts/windows/src/Lumiere.Settings',
]

const missing = []
const forbidden = []

for (const path of requiredPaths) {
  if (!(await exists(path))) {
    missing.push(path)
  }
}

for (const path of forbiddenPaths) {
  if (await exists(path)) {
    forbidden.push(path)
  }
}

if (missing.length > 0 || forbidden.length > 0) {
  if (missing.length > 0) {
    console.error(`Missing required paths:\n${missing.map(path => `- ${path}`).join('\n')}`)
  }

  if (forbidden.length > 0) {
    console.error(`Forbidden legacy paths:\n${forbidden.map(path => `- ${path}`).join('\n')}`)
  }

  process.exitCode = 1
}

async function exists(path) {
  try {
    await access(path)
    return true
  }
  catch {
    return false
  }
}

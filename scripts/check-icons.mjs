import { readFile } from 'node:fs/promises'
import { join, resolve } from 'node:path'
import sharp from 'sharp'

const repositoryRoot = resolve(import.meta.dirname, '..')
const iconsRoot = join(repositoryRoot, 'apps/desktop/resources/icons')

function assert(condition, message) {
  if (!condition) {
    throw new Error(message)
  }
}

async function assertPng(
  path,
  expectedWidth,
  expectedHeight,
  expectedDensity,
  requiresAlpha = true,
) {
  const metadata = await sharp(path).metadata()
  assert(metadata.format === 'png', `${path} must be a PNG.`)
  assert(
    metadata.width === expectedWidth && metadata.height === expectedHeight,
    `${path} must be ${expectedWidth} × ${expectedHeight}.`,
  )
  if (requiresAlpha) {
    assert(metadata.hasAlpha, `${path} must contain an alpha channel.`)
  }
  if (expectedDensity) {
    assert(
      metadata.density === expectedDensity,
      `${path} must use ${expectedDensity} DPI metadata.`,
    )
  }
}

async function icoSizes(path) {
  const buffer = await readFile(path)
  assert(buffer.readUInt16LE(0) === 0 && buffer.readUInt16LE(2) === 1, `${path} is not an ICO.`)
  const count = buffer.readUInt16LE(4)
  return Array.from({ length: count }, (_, index) => {
    const offset = 6 + index * 16
    const width = buffer.readUInt8(offset)
    const height = buffer.readUInt8(offset + 1)
    assert(width === height, `${path} contains a non-square representation.`)
    return width === 0 ? 256 : width
  })
}

async function icnsTypes(path) {
  const buffer = await readFile(path)
  assert(buffer.toString('ascii', 0, 4) === 'icns', `${path} is not an ICNS.`)
  assert(buffer.readUInt32BE(4) === buffer.length, `${path} has an invalid ICNS length.`)

  const types = []
  let offset = 8
  while (offset < buffer.length) {
    types.push(buffer.toString('ascii', offset, offset + 4))
    const length = buffer.readUInt32BE(offset + 4)
    assert(length >= 8, `${path} contains an invalid ICNS chunk.`)
    offset += length
  }
  assert(offset === buffer.length, `${path} contains a truncated ICNS chunk.`)
  return types
}

await Promise.all([
  assertPng(join(iconsRoot, 'mac/app-icon.png'), 1024, 1024),
  assertPng(join(iconsRoot, 'mac/trayTemplate.png'), 16, 16, 72),
  assertPng(join(iconsRoot, 'mac/trayTemplate@2x.png'), 32, 32, 144),
  assertPng(join(iconsRoot, 'windows/app.png'), 256, 256, undefined, false),
  assertPng(join(iconsRoot, 'windows/tray.png'), 32, 32),
])

const [appIcoSizes, trayIcoSizes, appIcnsTypes] = await Promise.all([
  icoSizes(join(iconsRoot, 'windows/app.ico')),
  icoSizes(join(iconsRoot, 'windows/tray.ico')),
  icnsTypes(join(iconsRoot, 'mac/app.icns')),
])

assert(
  appIcoSizes.join(',') === '16,20,24,30,32,36,40,48,60,64,72,80,96,256',
  'The Windows application ICO does not contain the required DPI representations.',
)
assert(
  trayIcoSizes.join(',') === '16,20,24,32,40,48,64',
  'The Windows tray ICO does not contain the required DPI representations.',
)
assert(
  appIcnsTypes.join(',') === 'icp4,icp5,icp6,ic07,ic08,ic09,ic10',
  'The macOS ICNS does not contain the required pixel representations.',
)

console.log('Desktop icon assets are complete and internally valid.')

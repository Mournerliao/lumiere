import { mkdir, writeFile } from 'node:fs/promises'
import { join, resolve } from 'node:path'
import sharp from 'sharp'

const repositoryRoot = resolve(import.meta.dirname, '..')
const sourcePath = join(repositoryRoot, 'assets/brand/lumiere-logo.png')
const iconsRoot = join(repositoryRoot, 'apps/desktop/resources/icons')
const macRoot = join(iconsRoot, 'mac')
const macIconsetRoot = join(macRoot, 'app.iconset')
const windowsRoot = join(iconsRoot, 'windows')

const macIconset = [
  ['icon_16x16.png', 16],
  ['icon_16x16@2x.png', 32],
  ['icon_32x32.png', 32],
  ['icon_32x32@2x.png', 64],
  ['icon_128x128.png', 128],
  ['icon_128x128@2x.png', 256],
  ['icon_256x256.png', 256],
  ['icon_256x256@2x.png', 512],
  ['icon_512x512.png', 512],
  ['icon_512x512@2x.png', 1024],
]

const windowsAppSizes = [16, 20, 24, 30, 32, 36, 40, 48, 60, 64, 72, 80, 96, 256]
const windowsTraySizes = [16, 20, 24, 32, 40, 48, 64]

function roundedRectMask(size, radius) {
  return Buffer.from(
    `<svg width="${size}" height="${size}" viewBox="0 0 ${size} ${size}" xmlns="http://www.w3.org/2000/svg"><rect width="${size}" height="${size}" rx="${radius}" fill="#fff"/></svg>`,
  )
}

async function createMacMaster(appArtwork) {
  const artworkSize = 824
  const artwork = await sharp(appArtwork)
    .resize(artworkSize, artworkSize, { fit: 'cover', kernel: sharp.kernel.lanczos3 })
    .composite([{ input: roundedRectMask(artworkSize, 185), blend: 'dest-in' }])
    .png()
    .toBuffer()

  return sharp({
    create: { width: 1024, height: 1024, channels: 4, background: '#00000000' },
  })
    .composite([{ input: artwork, left: 100, top: 100 }])
    .png()
    .toBuffer()
}

async function createAppArtwork() {
  const { data, info } = await sharp(sourcePath)
    .removeAlpha()
    .raw()
    .toBuffer({ resolveWithObject: true })
  const cutout = Buffer.alloc(info.width * info.height * 4)

  for (let sourceOffset = 0, outputOffset = 0; sourceOffset < data.length; sourceOffset += 3) {
    const red = data[sourceOffset]
    const green = data[sourceOffset + 1]
    const blue = data[sourceOffset + 2]
    const coralSeparation = Math.min(red - green, red - blue)
    const redScore = Math.max(0, Math.min(1, (red - 135) / 85))
    const coralScore = Math.max(0, Math.min(1, (coralSeparation - 25) / 65))
    const alpha = Math.round(255 * (1 - redScore * coralScore))

    cutout[outputOffset] = red
    cutout[outputOffset + 1] = green
    cutout[outputOffset + 2] = blue
    cutout[outputOffset + 3] = alpha
    outputOffset += 4
  }

  const canvasSize = 1024
  const characterSize = Math.round(canvasSize * 0.9)
  const characterLeft = Math.round((canvasSize - characterSize) / 2)
  const character = await sharp(cutout, {
    raw: { width: info.width, height: info.height, channels: 4 },
  })
    .resize(characterSize, characterSize, { fit: 'fill', kernel: sharp.kernel.lanczos3 })
    .png()
    .toBuffer()

  return sharp({
    create: { width: canvasSize, height: canvasSize, channels: 4, background: '#E8665A' },
  })
    .composite([
      {
        input: character,
        left: characterLeft,
        top: canvasSize - characterSize,
      },
    ])
    .png()
    .toBuffer()
}

async function createTemplateMaster() {
  const { data, info } = await sharp(sourcePath)
    .removeAlpha()
    .raw()
    .toBuffer({ resolveWithObject: true })
  const output = Buffer.alloc(info.width * info.height * 4)

  for (let sourceOffset = 0, outputOffset = 0; sourceOffset < data.length; sourceOffset += 3) {
    const red = data[sourceOffset]
    const green = data[sourceOffset + 1]
    const blue = data[sourceOffset + 2]
    const lowestChannel = Math.min(red, green, blue)
    const channelSpread = Math.max(red, green, blue) - lowestChannel
    const lightnessAlpha = Math.max(0, Math.min(255, (lowestChannel - 118) * 3.2))
    const neutralityAlpha = Math.max(0, Math.min(255, (94 - channelSpread) * 4))
    const alpha = Math.round((lightnessAlpha * neutralityAlpha) / 255)

    output[outputOffset] = 0
    output[outputOffset + 1] = 0
    output[outputOffset + 2] = 0
    output[outputOffset + 3] = alpha
    outputOffset += 4
  }

  return sharp(output, {
    raw: { width: info.width, height: info.height, channels: 4 },
  })
    .blur(0.6)
    .png()
    .toBuffer()
}

async function writeResizedPng(input, size, outputPath, density) {
  await sharp(input)
    .resize(size, size, { fit: 'fill', kernel: sharp.kernel.lanczos3 })
    .png({ compressionLevel: 9, palette: false })
    .withMetadata(density ? { density } : {})
    .toFile(outputPath)
}

function createIco(pngs) {
  const headerSize = 6
  const directoryEntrySize = 16
  let dataOffset = headerSize + pngs.length * directoryEntrySize
  const header = Buffer.alloc(headerSize)
  header.writeUInt16LE(0, 0)
  header.writeUInt16LE(1, 2)
  header.writeUInt16LE(pngs.length, 4)

  const entries = pngs.map(({ size, buffer }) => {
    const entry = Buffer.alloc(directoryEntrySize)
    entry.writeUInt8(size === 256 ? 0 : size, 0)
    entry.writeUInt8(size === 256 ? 0 : size, 1)
    entry.writeUInt8(0, 2)
    entry.writeUInt8(0, 3)
    entry.writeUInt16LE(1, 4)
    entry.writeUInt16LE(32, 6)
    entry.writeUInt32LE(buffer.length, 8)
    entry.writeUInt32LE(dataOffset, 12)
    dataOffset += buffer.length
    return entry
  })

  return Buffer.concat([header, ...entries, ...pngs.map(({ buffer }) => buffer)])
}

function createIcns(pngs) {
  const chunks = pngs.map(({ type, buffer }) => {
    const header = Buffer.alloc(8)
    header.write(type, 0, 4, 'ascii')
    header.writeUInt32BE(buffer.length + 8, 4)
    return Buffer.concat([header, buffer])
  })
  const totalLength = 8 + chunks.reduce((sum, chunk) => sum + chunk.length, 0)
  const header = Buffer.alloc(8)
  header.write('icns', 0, 4, 'ascii')
  header.writeUInt32BE(totalLength, 4)
  return Buffer.concat([header, ...chunks])
}

async function createIcoFromInput(input, sizes, outputPath) {
  const pngs = await Promise.all(
    sizes.map(async (size) => ({
      size,
      buffer: await sharp(input)
        .resize(size, size, { fit: 'fill', kernel: sharp.kernel.lanczos3 })
        .png({ compressionLevel: 9, palette: false })
        .toBuffer(),
    })),
  )
  await writeFile(outputPath, createIco(pngs))
}

async function main() {
  await Promise.all([
    mkdir(macIconsetRoot, { recursive: true }),
    mkdir(windowsRoot, { recursive: true }),
  ])

  const sourceMetadata = await sharp(sourcePath).metadata()
  if (sourceMetadata.width !== sourceMetadata.height || (sourceMetadata.width ?? 0) < 1024) {
    throw new Error('The canonical logo must be a square image at least 1024 pixels wide.')
  }

  const appArtwork = await createAppArtwork()
  const [macMaster, templateMaster] = await Promise.all([
    createMacMaster(appArtwork),
    createTemplateMaster(),
  ])
  await writeFile(join(macRoot, 'app-icon.png'), macMaster)

  await Promise.all(
    macIconset.map(([filename, size]) =>
      writeResizedPng(macMaster, size, join(macIconsetRoot, filename)),
    ),
  )

  await Promise.all([
    writeResizedPng(templateMaster, 16, join(macRoot, 'trayTemplate.png'), 72),
    writeResizedPng(templateMaster, 32, join(macRoot, 'trayTemplate@2x.png'), 144),
    writeResizedPng(appArtwork, 256, join(windowsRoot, 'app.png')),
  ])

  const windowsTrayMaster = await sharp(templateMaster).tint('#E8665A').png().toBuffer()
  await Promise.all([
    writeResizedPng(windowsTrayMaster, 32, join(windowsRoot, 'tray.png')),
    createIcoFromInput(appArtwork, windowsAppSizes, join(windowsRoot, 'app.ico')),
    createIcoFromInput(windowsTrayMaster, windowsTraySizes, join(windowsRoot, 'tray.ico')),
  ])

  const icnsRepresentations = await Promise.all(
    [
      ['icp4', 16],
      ['icp5', 32],
      ['icp6', 64],
      ['ic07', 128],
      ['ic08', 256],
      ['ic09', 512],
      ['ic10', 1024],
    ].map(async ([type, size]) => ({
      type,
      buffer: await sharp(macMaster)
        .resize(size, size, { fit: 'fill', kernel: sharp.kernel.lanczos3 })
        .png({ compressionLevel: 9, palette: false })
        .toBuffer(),
    })),
  )
  await writeFile(join(macRoot, 'app.icns'), createIcns(icnsRepresentations))

  console.log(`Generated desktop icon assets from ${sourcePath}`)
}

await main()

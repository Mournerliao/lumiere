import path from 'node:path'

export interface NativeHostPathOptions {
  appPath: string
  isPackaged: boolean
  resourcesPath: string
  overridePath?: string
}

export function macOSHostCandidates(options: NativeHostPathOptions): readonly string[] {
  if (options.overridePath) {
    return [path.resolve(options.overridePath)]
  }

  if (options.isPackaged) {
    return [path.join(options.resourcesPath, 'macos-host', 'LumiereMacHost')]
  }

  const hostRoot = path.resolve(options.appPath, '../../hosts/macos/.build')
  return [
    path.join(hostRoot, 'debug', 'LumiereMacHost'),
    path.join(hostRoot, 'release', 'LumiereMacHost'),
  ]
}

import { NativeProcessPlatformHost, type SpawnHost } from './native-process-platform-host'

export class WindowsPlatformHost extends NativeProcessPlatformHost {
  public constructor(executableCandidates: readonly string[], spawnHost?: SpawnHost) {
    super('windows', executableCandidates, spawnHost)
  }
}

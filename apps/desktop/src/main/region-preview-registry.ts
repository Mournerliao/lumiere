import { randomUUID } from 'node:crypto'
import { resolve, sep } from 'node:path'

export const regionPreviewScheme = 'lumiere-region-preview'

export class RegionPreviewRegistry {
  private readonly paths = new Map<string, string>()
  private readonly rootPrefix: string

  public constructor(rootDirectory: string) {
    this.rootPrefix = `${resolve(rootDirectory)}${sep}`
  }

  public grant(filePath: string): { token: string; url: string } {
    const normalizedPath = resolve(filePath)
    if (!normalizedPath.startsWith(this.rootPrefix)) {
      throw new Error('Region preview path is outside the controlled temporary directory.')
    }
    const token = randomUUID()
    this.paths.set(token, normalizedPath)
    return { token, url: `${regionPreviewScheme}://frame/${token}` }
  }

  public resolve(url: string): string | null {
    let parsed: URL
    try {
      parsed = new URL(url)
    } catch {
      return null
    }
    if (
      parsed.protocol !== `${regionPreviewScheme}:` ||
      parsed.hostname !== 'frame' ||
      parsed.search.length > 0 ||
      parsed.hash.length > 0
    ) {
      return null
    }
    const token = parsed.pathname.slice(1)
    if (token.length === 0 || token.includes('/')) {
      return null
    }
    return this.paths.get(token) ?? null
  }

  public revoke(token: string): void {
    this.paths.delete(token)
  }

  public clear(): void {
    this.paths.clear()
  }
}

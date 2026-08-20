import { readFile, readdir } from 'node:fs/promises'
import { resolve } from 'node:path'
import Ajv2020 from 'ajv/dist/2020.js'
import { describe, expect, it } from 'vitest'

const protocolDirectory = resolve(process.cwd(), '../../protocol/platform-host')

describe('platform host protocol', () => {
  it('keeps every checked-in fixture conformant with the language-neutral schema', async () => {
    const schema = JSON.parse(
      await readFile(`${protocolDirectory}/v1.schema.json`, 'utf8'),
    ) as object
    const fixtureNames = (await readdir(`${protocolDirectory}/fixtures`))
      .filter(name => name.endsWith('.json'))
      .sort()
    const validate = new Ajv2020({ allErrors: true, strict: true }).compile(schema)

    expect(fixtureNames.length).toBeGreaterThanOrEqual(6)

    for (const fixtureName of fixtureNames) {
      const fixture = JSON.parse(
        await readFile(`${protocolDirectory}/fixtures/${fixtureName}`, 'utf8'),
      ) as unknown

      expect(validate(fixture), `${fixtureName}: ${JSON.stringify(validate.errors)}`).toBe(true)
    }
  })

  it('rejects protocol messages with unknown fields', async () => {
    const schema = JSON.parse(
      await readFile(`${protocolDirectory}/v1.schema.json`, 'utf8'),
    ) as object
    const validate = new Ajv2020({ allErrors: true, strict: true }).compile(schema)

    expect(validate({
      version: 1,
      id: 'invalid-1',
      method: 'capture',
      params: { mode: 'display', delivery: 'folder', rawFrame: true },
    })).toBe(false)
  })
})

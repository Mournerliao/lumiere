# Platform Host Protocol

This directory owns the language-neutral seam between the Electron shell and each
native capture host. TypeScript, C#, and Swift bindings must conform to
[`v1.schema.json`](v1.schema.json); no language binding is the protocol source of truth.

## Transport

Version 1 uses UTF-8 JSON Lines over a child process' standard input and output. Each
line is one complete request or response. Hosts reserve standard output for protocol
messages and write structured diagnostics to standard error.

Every request carries `version`, a caller-generated `id`, `method`, and `params`.
Every response echoes `version` and `id`, and contains exactly one of `result` or
`error`. The shell may have multiple requests in flight, so ordering is determined by
`id`, not line position.

## Version 1

- `getCapabilities` reports host availability, capture modes, HDR acquisition support,
  and the single MVP output profile.
- `capture` accepts `region` or `display` and delivers to `clipboard`, `folder`, or
  `both`.
- Successful capture reports only compact state and artifact paths. Raw frames,
  textures, and native handles never cross this seam.
- The only MVP output profile is `srgb-visual-match`.

Unknown versions, methods, fields, or enum values are protocol errors. Additive
changes require a new schema version when an older host cannot safely reject or ignore
them.

The fixtures are executable examples and are validated in the desktop test suite.

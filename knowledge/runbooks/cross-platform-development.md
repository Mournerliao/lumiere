# Cross-Platform Development Runbook

This runbook owns shared Electron shell checks. Native capture and HDR behavior must
also follow the platform-specific runbook and evidence gate.

## Prerequisites

- Node.js 22 or newer
- Corepack with pnpm 11.7.0
- macOS or Windows

## Install And Verify

From the repository root:

```sh
pnpm install --frozen-lockfile
pnpm check
pnpm test
pnpm build
```

Run the shell during development:

```sh
pnpm dev
```

Until a native host is connected, the shell must report `host-unavailable` and keep
capture actions disabled. This is expected foundation behavior, not passing capture
evidence. Do not substitute Electron desktop capture to make the buttons appear to work.

## Truth Boundary

- Passing `check`, `test`, and `build` is repository evidence for the shared shell.
- A shell launch on macOS or Windows does not verify the native host on the other OS.
- HDR capture and Visual Match require fixed-scene hardware evidence on each claimed platform.

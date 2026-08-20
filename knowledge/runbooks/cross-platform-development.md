# Cross-Platform Development Runbook

This runbook owns shared Electron shell and protocol checks. Native capture and HDR
behavior must also follow the owning platform runbook.

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

- Passing `check`, `test`, and `build` verifies only the shared repository surface.
- A shell launch on macOS or Windows does not verify the native host on the other OS.
- HDR capture and Visual Match require fixed-scene hardware verification on each claimed platform.

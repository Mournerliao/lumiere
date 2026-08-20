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

## Renderer Components

The renderer uses Tailwind CSS 4 and copies beUI source through the configured
`@beui` shadcn registry. There is no beUI runtime package: installed components live
under `apps/desktop/src/renderer/src/components` and are owned by this repository.

Installed beUI components:

- `button-base`

Preview and add a component from the live registry:

```sh
pnpm --filter @lumiere/desktop ui:dry-run @beui/<slug>
pnpm --filter @lumiere/desktop ui:add @beui/<slug>
```

Add its slug to the installed-components list above in the same change. Do not use
`shadcn add --all`; keep the renderer limited to components it actually uses.

To synchronize an installed component with current beUI source, start from a clean
worktree and inspect the upstream diff before overwriting local files:

```sh
pnpm --filter @lumiere/desktop exec shadcn add @beui/button-base --diff
pnpm --filter @lumiere/desktop ui:update @beui/button-base
pnpm check
pnpm test
pnpm build
```

Update one component or one tightly related component family at a time. Review shared
helpers under `src/renderer/src/lib` carefully because an overwrite may affect several
installed components. Preserve Lumiere-specific accessibility, reduced-motion, theme,
and desktop interaction behavior when resolving upstream changes.

Until a native host is connected, the shell must report `host-unavailable` and keep
capture actions disabled. This is expected foundation behavior, not passing capture
evidence. Do not substitute Electron desktop capture to make the buttons appear to work.

## Truth Boundary

- Passing `check`, `test`, and `build` verifies only the shared repository surface.
- A shell launch on macOS or Windows does not verify the native host on the other OS.
- HDR capture and Visual Match require fixed-scene hardware verification on each claimed platform.

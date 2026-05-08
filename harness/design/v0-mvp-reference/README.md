# Lumiere v0 MVP Reference

This folder contains the imported v0.dev MVP design reference for Lumiere.

It is a runnable Next/React prototype used for durable UX reference only. It is not production application code and must not introduce a web UI dependency into Lumiere's WinUI 3 implementation.

## Covered Surfaces

- Main panel
- Settings panel
- Tray context menu
- HDR status simulation

## Usage

From this folder, the prototype can be previewed with:

```bash
pnpm install
pnpm dev
```

Running this prototype is optional and is separate from Lumiere's Windows validation workflow.

## Implementation Boundary

- Translate layout, density, wording intent, and interaction hierarchy into native WinUI/Fluent patterns.
- Do not copy React, Tailwind, Radix, shadcn, or web-specific implementation code into `src/`.
- Do not treat prototype HDR or SDR fallback wording as verified product behavior without checking Lumiere's HDR invariants and Windows validation level.

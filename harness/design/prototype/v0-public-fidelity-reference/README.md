# Lumiere v0 Public-Fidelity Reference

This folder contains the imported v0.dev public-fidelity design reference for Lumiere.

It is a runnable Next/React prototype used for durable UX reference only. It is not production application code and must not introduce a web UI dependency into Lumiere's WinUI 3 implementation.

## Covered Surfaces

- Main panel
- Settings panel
- Tray context menu
- HDR status simulation
- Perfect HDR Fidelity extension surfaces: target-aware HDR states, output profile status, output result feedback, overlay trust preview, validation evidence panel, and scenario switching.

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
- Do not treat the HDR-preserved feedback state as available until target-aware detection, output profile contracts, target-app compatibility evidence, visual-match evidence, and Windows manual validation pass for the selected profile.

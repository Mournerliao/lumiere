# Lumiere Design References

This folder stores durable UX reference material for Lumiere. These files are design references, not implementation source and not generated sprint output.

## Design Guidance

- [Design Principles](design-principles.md) - Lumiere's native Windows, HDR-first, low-interruption UX principles.
- [Perfect HDR Fidelity Design Extension](perfect-hdr-fidelity-extension.md) - The design supplement for target-aware fidelity states, output profiles, validation evidence, and public-release copy boundaries.
- [Design Workflow](design-workflow.md) - How to combine BMAD UX workflows, Impeccable review, and Microsoft WinUI/Fluent references.
- [UI Review Checklist](ui-review-checklist.md) - Practical checks for overlay, settings, onboarding, gallery, HDR language, and anti-patterns.
- [External References](external-references.md) - Curated references and the boundaries for using each one.

## Current Prototype Reference

Path: `prototype/v0-public-fidelity-reference/`

The imported v0.dev prototype is the active visual input for the current public-fidelity direction. Treat it as a runnable UX reference asset, not production implementation. WinUI 3 and the native graphics pipeline remain the source of truth for application behavior.

Perfect HDR Fidelity work must extend this reference rather than replace it. Fidelity states, HDR-preserved output profiles, validation evidence, benchmark comparisons, and release-copy boundaries are defined in [Perfect HDR Fidelity Design Extension](perfect-hdr-fidelity-extension.md) on top of the existing information architecture, density, and native Windows tone.

Covered surfaces:

- Main panel.
- Settings panel.
- Tray context menu.
- HDR status simulation.

Prototype entrypoints:

- `prototype/v0-public-fidelity-reference/README.md` - Prototype purpose, usage, and implementation boundary.
- `prototype/v0-public-fidelity-reference/app/page.tsx` - Next page entry.
- `prototype/v0-public-fidelity-reference/components/lumiere/` - Lumiere-specific screen components.
- `prototype/v0-public-fidelity-reference/app/globals.css` - Prototype tokens and global styling.

Current baseline interaction intent:

- Main window keeps screenshot entry primary and avoids wrapped control labels.
- Capture enters the direct monitor/region workflow without a picker-first interruption.
- Releasing a valid crop completes capture/copy and shows lightweight feedback.
- Onboarding, gallery, and annotation-heavy overlays remain out of the current baseline unless reintroduced by a story. Output workflows that are required for Public perfect-HDR-fidelity belong to Epic 11+ and are public-release prerequisites, not optional polish.

## Implementation Guidance

- Prefer Windows 11 native WinUI patterns over copying web-specific layout code.
- Preserve the existing native architecture boundaries from `harness/README.md`.
- Treat Perfect HDR Fidelity design work as an extension of the current v0 reference, not a new visual system.
- Use the v0 public-fidelity reference for layout, density, copy intent, and interaction hierarchy, but keep implementation scoped to the current baseline unless a task explicitly asks to build future screens.
- Translate React/Tailwind/shadcn patterns into WinUI/Fluent controls. Do not introduce web UI, Electron, Tauri, or React dependencies into production code.
- Direct capture, release-to-copy, and preview behavior still come from Lumiere's native FP16/scRGB pipeline and architecture docs, especially where the v0 reference does not include a complete overlay mockup.
- Use Impeccable for critique, polish, hardening, and anti-pattern detection. Do not let it override Lumiere's native Windows, HDR-first product direction.


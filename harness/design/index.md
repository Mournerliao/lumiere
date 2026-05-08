# Lumiere Design References

This folder stores durable UX reference material for Lumiere. These files are design references, not implementation source and not generated sprint output.

## Design Guidance

- [Design Principles](design-principles.md) - Lumiere's native Windows, HDR-first, low-interruption UX principles.
- [Design Workflow](design-workflow.md) - How to combine BMAD UX workflows, Impeccable review, and Microsoft WinUI/Fluent references.
- [UI Review Checklist](ui-review-checklist.md) - Practical checks for overlay, settings, onboarding, gallery, HDR language, and anti-patterns.
- [External References](external-references.md) - Curated references and the boundaries for using each one.

## Current MVP Reference

Path: `v0-mvp-reference/`

The imported v0.dev prototype is the active visual input for the current MVP route. Treat it as UX material, not production implementation. WinUI 3 and the native graphics pipeline remain the source of truth for application behavior.

Covered surfaces:

- Main panel.
- Settings panel.
- Tray context menu.
- HDR status simulation.

Prototype entrypoints:

- `v0-mvp-reference/README.md` - Prototype purpose, usage, and implementation boundary.
- `v0-mvp-reference/app/page.tsx` - Next page entry.
- `v0-mvp-reference/components/lumiere/` - Lumiere-specific screen components.
- `v0-mvp-reference/app/globals.css` - Prototype tokens and global styling.

MVP interaction intent:

- Main window keeps screenshot entry primary and avoids wrapped control labels.
- Capture enters the direct monitor/region workflow without a picker-first interruption.
- Releasing a valid crop completes capture/copy and shows lightweight feedback.
- Onboarding, gallery, annotation-heavy overlays, and expanded output workflows remain post-MVP unless reintroduced by a story.

## Implementation Guidance

- Prefer Windows 11 native WinUI patterns over copying web-specific layout code.
- Preserve the existing native architecture boundaries from `harness/README.md`.
- Use the v0 MVP reference for layout, density, copy intent, and interaction hierarchy, but keep implementation scoped to available MVP features unless a task explicitly asks to build future screens.
- Translate React/Tailwind/shadcn patterns into WinUI/Fluent controls. Do not introduce web UI, Electron, Tauri, or React dependencies into production code.
- Direct capture, release-to-copy, and preview behavior still come from Lumiere's native FP16/scRGB pipeline and architecture docs, especially where the v0 reference does not include a complete overlay mockup.
- Use Impeccable for critique, polish, hardening, and anti-pattern detection. Do not let it override Lumiere's native Windows, HDR-first product direction.

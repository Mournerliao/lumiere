# Lumiere Design References

This folder stores durable UX reference material for Lumiere. These files are design references, not implementation source and not generated sprint output.

## Design Guidance

- [Design Principles](design-principles.md) - Lumiere's native Windows, HDR-first, low-interruption UX principles.
- [Design Workflow](design-workflow.md) - How to combine BMAD UX workflows, Impeccable review, and Microsoft WinUI/Fluent references.
- [UI Review Checklist](ui-review-checklist.md) - Practical checks for overlay, settings, onboarding, gallery, HDR language, and anti-patterns.
- [External References](external-references.md) - Curated references and the boundaries for using each one.

## Interactive Prototype

Path: `interactive-prototype/`

The prototype is a durable visual and interaction reference. Treat it as UX material, not production implementation. WinUI 3 and the native graphics pipeline remain the source of truth for application behavior.

### Page Map

- `interactive-prototype/index.html` - Prototype entry and page index.
- `interactive-prototype/1-welcome.html` - Welcome / first-run entry.
- `interactive-prototype/2-onboarding-permissions.html` - System readiness and permissions onboarding.
- `interactive-prototype/3-onboarding-configuration.html` - Default configuration onboarding.
- `interactive-prototype/4-settings-general.html` - General settings.
- `interactive-prototype/5-settings-shortcuts.html` - Keyboard shortcuts settings.
- `interactive-prototype/6-settings-hdr.html` - HDR and color settings.
- `interactive-prototype/7-settings-output.html` - Output and export settings.
- `interactive-prototype/8-capture-overlay.html` - Capture region selection overlay state.
- `interactive-prototype/9-capture-overlay-annotated.html` - Capture overlay with annotation toolbar.
- `interactive-prototype/10-gallery.html` - Capture library / gallery.
- `interactive-prototype/11-dashboard.html` - Capture home dashboard.

### Navigation Flow

- Welcome -> Onboarding permissions -> Onboarding configuration -> Dashboard.
- Dashboard -> Capture overlay -> Annotated capture overlay.
- Dashboard -> Settings: General, Shortcuts, HDR, Output.
- Dashboard -> Gallery.

## Implementation Guidance

- Prefer Windows 11 native WinUI patterns over copying web-specific layout code.
- Preserve the existing native architecture boundaries from `harness/README.md`.
- Use dashboard and settings pages as layout references, but keep implementation scoped to available features unless a task explicitly asks to build future screens.
- Use overlay pages as interaction references for crop UI states; actual preview must remain the native FP16/scRGB pipeline.
- Use Impeccable for critique, polish, hardening, and anti-pattern detection. Do not let it override Lumiere's native Windows, HDR-first product direction.

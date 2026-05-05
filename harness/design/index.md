# Lumiere Design References

This folder stores durable UX reference material for Lumiere. These files are design references, not implementation source and not generated sprint output.

## UXPilot Export - 2026-05-05

Source: local UXPilot export provided by the project owner on 2026-05-05.

Path: `uxpilot-export-2026-05-05/`

Use this export as visual and interaction reference when implementing app shell, onboarding, settings, capture overlay, and gallery screens. Do not treat HTML/CSS from the export as production UI code; Lumiere remains a native WinUI 3 application.

### Page Map

- `uxpilot-export-2026-05-05/1-Lumiere Tool - Welcome.html` - Welcome / first-run entry.
- `uxpilot-export-2026-05-05/2-Lumiere Tool - Onboarding - Pe.html` - System readiness and permissions onboarding.
- `uxpilot-export-2026-05-05/3-Lumiere Tool - Onboarding - De.html` - Default configuration onboarding.
- `uxpilot-export-2026-05-05/4-Lumiere Tool - Settings - Gene.html` - General settings.
- `uxpilot-export-2026-05-05/5-Lumiere Tool - Settings - Shor.html` - Keyboard shortcuts settings.
- `uxpilot-export-2026-05-05/6-Lumiere Tool - Settings - HDR.html` - HDR and color settings.
- `uxpilot-export-2026-05-05/7-Lumiere Tool - Settings - Outp.html` - Output and export settings.
- `uxpilot-export-2026-05-05/8-Lumiere Tool - Capture Overlay.html` - Capture region selection overlay state.
- `uxpilot-export-2026-05-05/9-Lumiere Tool - Capture Overlay.html` - Alternate capture region overlay state.
- `uxpilot-export-2026-05-05/10-Lumiere Tool - Gallery.html` - Capture library / gallery.
- `uxpilot-export-2026-05-05/11-Lumiere Tool - Dashboard - Cap.html` - Capture home dashboard.

### Implementation Guidance

- Prefer Windows 11 native WinUI patterns over copying web-specific layout code.
- Preserve the existing native architecture boundaries from `harness/README.md`.
- Use the dashboard and settings pages as layout references, but keep current implementation scoped to features already available unless a task explicitly asks to build future screens.
- Use overlay pages as interaction references for crop UI states; actual preview must remain the native FP16/scRGB pipeline.

# Lumiere Design References

This folder stores durable UX reference material for Lumiere. These files are design references, not implementation source and not generated sprint output.

## Interactive Prototype

Path: `interactive-prototype/`

可连续交互的设计稿原型，包含完整的页面导航流程。在浏览器中打开 `interactive-prototype/index.html` 即可开始浏览。

### Page Map

- `interactive-prototype/index.html` - 目录页面，提供所有页面的链接入口。
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

- Welcome → Onboarding (Permissions → Configuration) → Dashboard
- Dashboard ↔ Capture Overlay (Region Select → Annotated)
- Dashboard ↔ Settings (General / Shortcuts / HDR / Output)
- Dashboard ↔ Gallery

### Implementation Guidance

- Prefer Windows 11 native WinUI patterns over copying web-specific layout code.
- Preserve the existing native architecture boundaries from `harness/README.md`.
- Use the dashboard and settings pages as layout references, but keep current implementation scoped to features already available unless a task explicitly asks to build future screens.
- Use overlay pages as interaction references for crop UI states; actual preview must remain the native FP16/scRGB pipeline.

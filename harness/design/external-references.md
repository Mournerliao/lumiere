# External Design References

These references inform Lumiere's UX work. They are not dependencies unless explicitly installed in this repository.

## Installed Project Skill

### Impeccable

- Source: https://github.com/pbakaus/impeccable
- Installed path: `.agents/skills/impeccable`
- Use for: UI critique, polish, hardening, UX copy, anti-pattern review, cognitive load, layout, color, motion, and accessibility checks.
- Boundary: do not copy Impeccable's site visual identity into Lumiere. Use it as a reviewer and vocabulary layer.

## Microsoft Native Windows References

### Fluent 2 Design Principles

- Source: https://fluent2.microsoft.design/design-principles
- Use for: platform fit, focus, inclusion, native behavior, and restraint.
- Boundary: Fluent principles guide the product experience; they do not replace Lumiere's HDR and capture constraints.

### WinUI Gallery

- Source: https://github.com/microsoft/WinUI-Gallery
- Use for: WinUI 3 controls, styles, adaptive layouts, XAML examples, and Fluent behavior in a working app.
- Boundary: examples are implementation references, not a mandate to use every available control.

### Windows Controls And Patterns

- Source: https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/
- Use for: choosing standard Windows controls and patterns for settings, commands, status, forms, dialogs, and navigation.
- Boundary: custom overlay and HDR preview behavior may need native graphics-specific design beyond standard controls.

## Screenshot Tool References

Use these products for workflow comparison, not visual copying:

- Windows Snipping Tool: baseline Windows expectations for capture modes, editing, copy/save, and settings.
- Snipaste: fast capture, annotation, pinning, color picker, and measurement workflows.
- ShareX: power-user capture and output configuration, with caution around complexity.
- Shottr: lightweight capture, annotation, scrolling capture, and pixel-oriented workflows.
- PixPin: capture, pinning, OCR, recording, and multi-purpose screenshot workflows.

## Imported Design References

### v0.dev Public-Fidelity Prototype

- Source: imported from `/Users/asherliao/Downloads/b_rQnQ7Q13jLu`.
- Installed path: `harness/design/prototype/v0-public-fidelity-reference/`.
- Use for: current public-fidelity visual direction for the main panel, settings panel, tray context menu, and HDR status simulation.
- Boundary: keep it as a React/Next design reference only. Do not introduce web UI dependencies or treat prototype HDR/SDR wording as validated product behavior.

## External Skills Not Installed

### Anthropic Frontend Design

- Source: https://github.com/anthropics/claude-code/blob/main/plugins/frontend-design/skills/frontend-design/SKILL.md
- Reason not installed: useful for web frontend generation, but less aligned with native WinUI desktop design.

### Ilm-Alan Frontend Design

- Source: https://github.com/Ilm-Alan/frontend-design
- Reason not installed: structured and useful for CSS-token web UI work, but redundant with Impeccable for Lumiere's current needs.

### Arc

- Source: https://github.com/howells/arc
- Reason not installed: overlaps with the existing BMAD planning and UX workflows already present in this repository.

# Lumiere Design Principles

Lumiere is a native Windows screenshot tool for people who care about accurate HDR capture and preview. The interface should feel trustworthy, fast, and quiet. Design serves the capture workflow; it is not the product by itself.

## North Star

Lumiere should feel like a precise native Windows instrument:

- Native first: WinUI 3, Windows App SDK, Fluent controls, keyboard and pointer conventions, high-DPI behavior, and accessibility patterns.
- HDR trustworthy: never imply HDR correctness unless the path has the matching validation level: Mac edit, Windows CI, or Windows manual validation.
- Low interruption: capture, confirm, copy, save, and return the user to their work with minimal ceremony.
- Professional restraint: clear hierarchy, compact controls, calm color, purposeful motion, and no marketing-style decoration.
- Power without clutter: advanced HDR, shortcut, and output controls are available, but not pushed into the primary capture path.

## Product Register

Lumiere is a product UI, not a brand site. Design should serve repeated tool use.

- Favor Windows-native controls and predictable settings patterns.
- Use density deliberately. Screenshot tools are used in the middle of another task.
- Keep toolbar labels short, but not cryptic.
- Prefer familiar icons for capture, copy, save, annotate, undo, redo, cancel, and confirm actions.
- Use progressive disclosure for expert HDR and output controls.

## Interaction Priorities

1. Capture overlay and region selection must be fast, legible, and reversible.
2. HDR preview must clearly communicate what is captured, previewed, transformed, or not yet verified.
3. Onboarding must explain permissions and readiness without sounding like marketing.
4. Settings must be scannable, native, and safe for repeated adjustment.
5. Gallery and history views should support retrieval and comparison without becoming a media manager.

## Visual Direction

- Use Fluent materials and elevation only when they clarify layering.
- Avoid web landing-page composition, oversized hero sections, decorative cards, and gradient-driven brand moments.
- Avoid nested cards, purple-blue gradients, generic SaaS dashboards, and ornamental motion.
- Keep accent color rare and functional: active state, focus, selection, warning, or HDR-specific status.
- Prefer clear text contrast over atmospheric styling.

## Validation Language

Any UI copy, design doc, or review note that references HDR, WinUI, WGC, DXGI, D3D11, multi-monitor behavior, or display correctness must label the validation level when claiming completion:

- Mac edit: structure, docs, and platform-neutral design only.
- Windows CI: restore, build, automated tests, and formatting on Windows.
- Windows manual validation: real Windows hardware, HDR display behavior, multi-monitor capture, and visual inspection.

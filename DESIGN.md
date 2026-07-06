# DESIGN.md

## Design System: Lumiere

Lumiere's design system is a native Windows product UI system. Design serves capture accuracy, speed, and trust. Use Microsoft Fluent and WinUI 3 conventions as the primary implementation reference.

## Visual Direction

- Native Windows 11, not web SaaS.
- Quiet, precise, and professional.
- Compact tool surfaces with clear hierarchy.
- Accent color is functional and rare.
- Motion is brief and stateful.
- Materials and elevation clarify layering, especially around overlay, toolbar, and settings surfaces.

## Typography

- Use WinUI default typography unless a specific native design reason requires otherwise.
- Keep labels short and direct.
- Use sentence case for normal UI copy unless Windows control guidance indicates otherwise.
- Avoid marketing headlines inside product surfaces.

## Color And Contrast

- Follow Windows theme behavior and accessibility contrast requirements.
- Use accent color for selected state, active command, focus, and critical status only.
- Do not use decorative purple-blue gradients, glow-heavy surfaces, or low-contrast gray text on colored backgrounds.
- HDR status and warnings should use semantic meaning, not decoration.

## Layout

- Use WinUI layout patterns for settings, forms, dialogs, flyouts, command bars, and navigation.
- Capture overlay layout must remain stable while the pointer moves.
- Toolbars should have stable dimensions and familiar icons.
- Avoid nested cards, oversized page sections, and landing-page composition.

## Components

- Prefer built-in WinUI controls before custom controls.
- Use CommandBar, AppBarButton, ToggleSwitch, NumberBox, ComboBox, InfoBar, TeachingTip, ContentDialog, Flyout, and NavigationView where they match the task.
- Custom UI is appropriate for capture overlay, crop handles, magnifier, annotation canvas, HDR preview, and GPU-backed surfaces.

## UX Writing

- Be concrete about what the user can do next.
- Explain permissions in terms of capture functionality.
- Distinguish capture, preview, conversion, export, and validation.
- Avoid promising HDR correctness unless the validation level is known.
- Treat "copied", "saved", "converted", and "HDR-preserved" as separate claims. Public release UI may use HDR-preservation language only for paths with target-aware detection, output semantics, and Windows manual validation.

## Validation Labels

Use these labels in design and review notes when discussing platform behavior:

- Mac edit: design, knowledge-base, and platform-neutral work only.
- Windows CI: restore/build/test/format on Windows.
- Windows manual validation: real Windows hardware and HDR display behavior checked.
- Public HDR fidelity validation: target-aware HDR state, supported output profile contract, compatibility matrix, and release copy reviewed against recorded evidence.

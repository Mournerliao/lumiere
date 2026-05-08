# Lumiere UI Review Checklist

Use this checklist for the current MVP surfaces: main panel, settings panel, tray context menu, direct screenshot entry, HDR status language, and capture overlay behavior. Onboarding, gallery, annotation-heavy overlays, and expanded export workflows are post-MVP unless a story explicitly brings them back.

## Native Windows Fit

- Uses WinUI/Fluent patterns before custom controls.
- Uses familiar Windows command patterns for primary actions, overflow, context menus, dialogs, flyouts, InfoBars, TeachingTips, and settings.
- Supports keyboard-first operation for capture, cancel, confirm, copy, save, undo, redo, and settings navigation.
- Keeps focus states visible and logical.
- Handles high-DPI and multi-monitor layouts without ambiguous hit targets.
- Avoids web-first interaction assumptions such as hover-only discovery or page-like hero flows.

## Screenshot Workflow

- Capture entry is fast and visible without becoming noisy.
- Region selection shows clear bounds, dimensions, and reversible cancel/confirm actions.
- Window and screen selection states are visually distinct when supported.
- Annotation controls are treated as post-MVP unless explicitly reintroduced.
- Copy, save, and export outcomes are explicit enough to build trust.
- Failure states tell the user what happened and what they can do next.

## HDR Trust

- UI copy distinguishes capture, preview, conversion, export, and validation.
- HDR settings explain consequences without overpromising.
- SDR fallback or unverified paths are labeled honestly.
- Claims about FP16/scRGB, WGC, DXGI, D3D11, HDR display behavior, or multi-monitor behavior include the validation level when relevant.
- Visual treatments do not hide clipping, tone mapping, or preview uncertainty behind decorative styling.

## Settings And Onboarding

- MVP settings are scannable and limited to current behavior: shortcuts, HDR/readiness preferences, output target, save path, and about/status information.
- Permissions onboarding is post-MVP unless required by a story; any permission copy must explain why each permission is needed and how to recover if unavailable.
- Defaults are safe for a first capture.
- Expert settings are grouped and described without overwhelming first-time users.
- Shortcut conflicts are discoverable and recoverable.
- Settings text is short, concrete, and action-oriented.

## v0 MVP Reference Pass

- Main panel keeps screenshot actions primary and does not wrap key control labels.
- Settings panel follows native Windows settings density instead of web page composition.
- Tray context menu is compact, command-oriented, and works as a Windows tray menu reference.
- HDR status wording distinguishes readiness, availability, unsupported states, and validation level.
- Any React/Tailwind interaction is translated into WinUI/Fluent behavior before implementation.

## Impeccable Anti-Pattern Pass

- No nested cards or page sections styled as decorative card stacks.
- No generic SaaS dashboard composition for tool surfaces.
- No purple-blue gradient branding or ornamental glow unless it communicates state.
- No oversized marketing hero on an app screen.
- No gray text on colored backgrounds with weak contrast.
- No motion that delays capture, confirmation, or return to work.
- No icon-only unfamiliar control without a tooltip or accessible name.
- No tiny touch targets or unstable toolbar dimensions.

## Acceptance Notes

For each review, record:

- Surface reviewed.
- Validation level: Mac edit, Windows CI, or Windows manual validation.
- Main user task.
- Blocking UX risks.
- Follow-up decisions, if any.

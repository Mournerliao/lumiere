# Settings Accessibility Validation

Updated: 2026-06-22

This document is the focused Windows manual validation workflow for Epic 13 / Story 13-2. It validates the native settings shell under keyboard, screen reader, high contrast, text scaling, DPI, and export-profile messaging that stays honest without relying on internal release-process jargon.

Use it together with:

- `release-validation-checklist.md`
- `hdr-sdr-validation-scenarios.md`
- `overlay-validation.md` when settings changes affect overlay copy or export-profile expectations

## Scope

This workflow covers:

- `REL-HDR-06`
- `REL-A11Y-01`
- `REL-A11Y-02`
- `REL-A11Y-03`
- `REL-A11Y-04`
- `REL-A11Y-05`
- Relevant settings persistence checks from `REL-SET-01` through `REL-SET-06`

## Required Test Surfaces

Validate at least these settings areas:

1. Shortcuts
2. HDR alerts and target-aware state rows
3. Export profile options and helper copy
4. Output destination
5. Save path
6. Open after capture
7. Timestamp naming
8. Copy as image
9. About section

## Keyboard Validation

1. Open settings from the main panel with keyboard navigation only.
2. Confirm focus order is logical from top to bottom.
3. Confirm every interactive control can be reached with `Tab` and shifted backward with `Shift+Tab`.
4. Confirm `Space` or `Enter` activates:
   - `ToggleSwitch` controls
   - shortcut buttons
   - save-path browse button
   - export profile radio buttons
   - output destination selection
5. Confirm the back button returns to the main panel and does not leave focus lost.
6. Confirm no focus trap or hidden focus target exists inside collapsed sections.

Expected result:

- All settings controls are operable without pointer input.
- Focus visuals remain visible.
- No custom control requires non-standard activation.

## Screen Reader Smoke Check

Use Narrator or another available Windows screen reader.

1. Read through the shortcuts rows and confirm the control names and current values are understandable.
2. Confirm `HDR alerts`, `Open after capture`, `Timestamp naming`, and `Copy as image` announce as switches with on/off state.
3. Confirm output destination announces as a single-choice group with current selection.
4. Confirm export profile options announce as a single-choice group and expose selected / unavailable state honestly.
5. Confirm the target-aware state and evidence rows expose meaningful text rather than unlabeled decorative content.
6. Confirm helper copy does not imply HDR-preserved output where the path is only compatibility or currently unavailable HDR output.

Expected result:

- Primary controls have usable names, roles, and states.
- Selected-disabled export options remain understandable rather than silent or misleading.

## High Contrast And Theme Validation

1. Enable Windows high contrast mode if available.
2. Reopen settings and inspect each section.
3. Confirm text remains readable, especially:
   - helper text
   - export-profile helper copy for currently unavailable HDR paths
   - target-aware evidence detail
   - save path value text
4. Confirm focus rings, selected radio buttons, and toggle states remain visible without relying on accent color alone.
5. Confirm status labels such as `Validate`, `Build`, and `Compat` remain distinguishable in high contrast.

Expected result:

- Meaning is not lost when accent-color assumptions disappear.
- No critical state is conveyed only by a subtle tint.

## DPI And Text Scaling Validation

Run at the tester's normal DPI plus additional scales where available:

- 100%
- 125%
- 150%
- 200%

At each scale:

1. Open the settings shell.
2. Confirm no clipping in section headers, helper copy, or validation rows.
3. Confirm shortcut buttons, radio buttons, and toggle switches remain visible and clickable.
4. Confirm long save paths ellipsize without overlapping nearby controls.
5. Confirm export profile rows remain legible and do not collapse their status labels into each other.
6. Confirm target-aware evidence text wraps instead of overflowing.
7. If the main panel shows an HDR alert `InfoBar`, confirm the compact shell still leaves primary capture actions and status content visible rather than compressing them into unusable space.

Expected result:

- Settings composition adapts structurally.
- Main-panel alert states do not force the compact shell into unusable density.
- No row becomes unreadable or unreachable at common scales.

## Export Profile Specific Checks

These checks matter because export profile semantics are easy to overclaim and easy to describe poorly for assistive technology users.

1. With default `sRGB`, confirm `sRGB` is selectable and presented as the compatibility path.
2. If persisted profile is `HDR10` or `P3`, reopen settings and confirm:
   - the selected option is still visible
   - the selected option still receives focus and announces as the current choice
   - the option is not falsely presented as fully supported
   - the user can move back to `sRGB`
3. Confirm helper text still states that HDR10/P3 require encoder metadata, conversion policy, target-app assumptions, and Windows validation.
4. Confirm selected-but-currently-unavailable radio-button behavior is understandable in keyboard and screen-reader flows.

Expected result:

- Blocked HDR profiles are visible without pretending to be public-release-ready.
- `sRGB` remains the safe fallback path.

## Result Recording Notes

When filing evidence, record failures separately as:

- keyboard navigation defect
- screen reader naming/state defect
- high contrast readability defect
- DPI/text scaling layout defect
- export-profile honesty defect

This keeps Story 13-2 actionable instead of burying accessibility failures inside generic “UI issue” notes.

# Perfect HDR Fidelity Design Extension

Updated: 2026-06-21

This document extends the existing `v0-mvp-reference/` design direction for the fixed Perfect HDR Fidelity Public Release target. It is a design brief and UX specification supplement, not production code and not a replacement visual system.

## Source Of Truth

The v0 MVP reference remains the base for Lumiere's layout density, command hierarchy, tray/menu shape, settings organization, and compact native Windows tone. Perfect HDR Fidelity adds states and controls on top of that base so the public release can prove fidelity rather than merely claim it.

This extension inherits:

- Native Windows 11 / WinUI / Fluent component vocabulary.
- Compact screenshot-tool density.
- Direct capture entry and release-to-output workflow.
- Low-interruption main, tray, hotkey, and overlay behavior.
- Restrained product UI, where design serves capture accuracy and trust.

This extension adds:

- Target-aware HDR state disclosure.
- Output profile status and validation scope.
- Visual-match evidence surfaces.
- At least one HDR-preserved supported output path in the public-release design model.
- Copy rules that distinguish artifact success from HDR preservation.
- QQ-style visual-match benchmarking as a product-quality reference for gray, white, highlight, and mixed HDR/SDR behavior.

## Impeccable Shape Brief

**Feature summary:** Add the UI and UX model required for Perfect HDR Fidelity Public Release without replacing the v0 MVP design. The target user is a Windows HDR screenshot user who needs to capture quickly, then trust whether the result is visually matched, SDR-compatible, HDR-preserved, degraded, or unvalidated.

**Primary user action:** Capture a region or fullscreen target, receive configured output, and immediately understand which fidelity claim is true for that result.

**Design direction:** Restrained product UI. Scene sentence: a focused Windows user is working on an HDR desktop or mixed-monitor setup under normal desk lighting, captures a region in the middle of another task, and needs precise state feedback without a modal or export wizard. Anchor references: Windows Snipping Tool for direct capture, Microsoft PowerToys for native utility settings, and QQ screenshot behavior as a visual-match benchmark.

**Scope:** Production-ready design brief for the main panel, settings, overlay, tray, output feedback, and validation/evidence surfaces. No code is written here. The design extends the existing v0 reference and should be translated into native WinUI/Fluent controls.

**Visual probe decision:** No new visual direction probe is required because this is not a net-new or ambiguous direction. The user explicitly chose to supplement the existing v0 reference to avoid breaking established design rules.

## Fidelity UX Model

The UI must treat fidelity as a set of separate user-facing facts, never a single optimistic success state.

| Concept | User meaning | UI claim allowed when |
|---|---|---|
| Captured | Lumiere received frame data from the target capture path. | Capture completed without implying output fidelity. |
| Previewed | Lumiere displayed the capture/preview through the active preview path. | Preview path and display target are known. |
| Saved | A file artifact was written. | File write succeeded. |
| Copied | A clipboard artifact was placed on the clipboard. | Clipboard write succeeded. |
| Converted | Output was transformed for compatibility, usually SDR-compatible. | Conversion policy is known and surfaced. |
| Visual match | The artifact/viewer pair was manually checked against expected appearance. | Target app/viewer evidence exists. |
| HDR-preserved | Supported output profile preserves HDR according to its contract. | Profile contract and Windows validation pass. |
| Unvalidated | Lumiere cannot prove the claim for this target/profile/viewer. | Any required evidence is missing. |

Perfect HDR Fidelity Public Release requires visual-match output evidence and at least one HDR-preserved supported output path. SDR-compatible output may be useful, but it is fallback or auxiliary and must never become the public release target.

## Surface Additions

### Main Panel

Keep the v0 compact shell: Lumiere identity, settings entry, fullscreen capture, region capture, shortcut labels, HDR status footer, and minimize/background intent.

Add a small fidelity status area without turning the panel into a dashboard:

- Primary status: target-aware HDR readiness for the current or inferred capture target.
- Secondary status: active output profile summary, such as `Output: SDR-compatible`, `Output: HDR profile pending validation`, or `Output: HDR-preserved profile ready`.
- Evidence affordance: a quiet `Details` or info affordance that opens a native disclosure surface, not a large card grid.

Required main-panel states:

- HDR ready for active target.
- HDR available but disabled for active target.
- SDR target or HDR unavailable.
- Target unknown or unvalidated.
- Output profile unvalidated.
- HDR-preserved output profile available.
- Capture active, capture blocked, and duplicate capture rejected.

### Settings

Keep the v0 settings information architecture: Shortcuts, HDR, Output, Clipboard, About. Add fidelity details through progressive disclosure rather than a new top-level "expert" workspace.

HDR section additions:

- Target-aware detection status.
- Mixed-monitor limitation note when the active target cannot be resolved.
- HDR alerts preference remains user-facing, but critical unvalidated or failed states still appear in status.

Output section additions:

- Replace ambiguous `Export` meaning with `Output profile`.
- Each profile must show one of: `Ready`, `Pending implementation`, `Pending validation`, `Compatibility only`, `HDR-preserved`.
- HDR10, P3, sRGB, or any future profile must be disabled or validation-scoped until source format, destination format, transfer function, primaries, conversion/tone-mapping policy, metadata policy, and viewer assumptions are documented.
- For the first HDR-preserved profile, surface the supported profile contract in a native details view.

Clipboard additions:

- Clipboard image usability must stay separate from HDR preservation.
- Copy-as-image can be enabled for compatibility, but cannot imply HDR-preserved clipboard output without target-app validation.

About/status additions:

- Add a `Validation` row or details surface showing build, validation level, and links to release evidence where appropriate.
- Do not expose raw WGC, DXGI, D3D11, metadata, or transfer-function language in primary UI unless the user opens diagnostics/details.

### Overlay

The overlay remains a temporary lens over the target display. Do not add an editor, gallery, or export picker to the capture moment.

Add only the minimum trust signals needed during capture:

- Target display status: `HDR target`, `SDR target`, `Target unvalidated`, or `Mixed-display check needed`.
- Crop validity and geometry.
- Optional concise warning when output will be converted or unvalidated.
- Stable cancel affordance and Escape behavior.

Overlay rules:

- Never hide clipping, tone-mapping uncertainty, or invalid crop state behind decorative dimming.
- Status chrome must remain legible over bright highlights, dark scenes, and gray/white UI content.
- Status placement must be stable across DPI and mixed-monitor setups.

### Output Feedback

Completion feedback must be target-specific and evidence-scoped.

Preferred pattern:

- Title names the artifact result first: `Copied`, `Saved`, `Copied and saved`, `Partial output`, `Output failed`.
- Detail lines name each target: clipboard, file, viewer/open action.
- Fidelity line is separate: `Visual match validated`, `HDR-preserved profile`, `Converted for compatibility`, or `Fidelity unvalidated`.

Examples:

- `Copied to clipboard. Converted for compatibility.`
- `Saved to folder. HDR-preserved profile validated for supported viewers.`
- `Copied and saved. Clipboard copied; file save failed. HDR preservation not claimed.`
- `Saved to folder. Visual match unvalidated for this viewer.`

Banned completion copy:

- `HDR copied`
- `Perfect HDR saved`
- `HDR10 ready`
- `P3 preserved`
- `Exact color saved`
- `Looks identical everywhere`

These are banned unless the active path has the corresponding contract, target-aware detection, compatibility evidence, and Windows manual validation. Even then, copy must be scoped to supported paths and named viewers.

### Tray

Tray remains compact and command-first.

Add only:

- One-line target-aware HDR/fidelity status below Lumiere identity.
- Disabled reasons for capture commands when a session is active or the target state is unsafe.
- Same command labels as the main panel.

Tray must not introduce new fidelity vocabulary. It mirrors the main panel state in a smaller form.

### Validation And Evidence Surface

Perfect HDR Fidelity needs a small evidence surface for users, testers, and future agents. This does not need to be a public-facing dashboard.

Recommended native shape:

- Settings > About > Validation details.
- Rows for capture target, preview path, output profile, compatibility matrix, last validation build/date, and known limitations.
- Each row uses `Validated`, `Validated with limitation`, `Not run`, `Failed`, or `Not applicable`.

Evidence surfaces must link back to `docs/validation/release-validation-checklist.md` in docs and release work. The app UI should summarize evidence; the docs remain the durable release record.

## Component And State Inventory

| Component | Required states | Notes |
|---|---|---|
| Capture action button | Default, hover, focus, pressed, active, disabled, duplicate rejected, unavailable reason | Use native button semantics and accessible names. |
| Trust status badge | HDR ready, enable HDR, HDR unavailable, degraded, unsupported, preview failed, target unvalidated, output unvalidated, converted, visual-match validated, HDR-preserved | Text plus icon/glyph. Color is reinforcement only. |
| Output profile selector | Enabled, disabled, pending implementation, pending validation, compatibility-only, HDR-preserved, failed validation | Prefer native segmented/radio pattern with details. |
| Overlay lens | Ready, dragging, valid crop, invalid crop, target unvalidated, degraded, output in progress, canceled, failed | Keep chrome minimal and stable. |
| Output feedback | Copied, saved, copied and saved, partial success, clipboard failed, file failed, converted, visual-match validated, HDR-preserved, unvalidated | Artifact result and fidelity result are separate lines. |
| Validation detail row | PASS, PASS with limitation, FAIL, NOT RUN, N/A | Match release checklist vocabulary. |
| Tray menu status | Idle, active capture, HDR target ready, target unvalidated, degraded, unavailable | Mirrors main panel vocabulary. |

## Copy Rules

Use short native Windows product copy. The UI should sound precise, not promotional.

Required distinctions:

- `Captured` is not `saved`.
- `Saved` is not `HDR-preserved`.
- `Copied` is not `visual-match validated`.
- `Converted` is not `perfect`.
- `Unvalidated` is not `failed`, but it cannot be claimed as supported.
- `Visual match` is app/viewer evidence, not universal display truth.
- `HDR-preserved` is profile-specific and viewer-scoped.

Use "supported" only with a named scope. Examples:

- `HDR-preserved for supported viewers`
- `Validated on this target`
- `Converted for compatibility`
- `Fidelity not validated for this target`
- `Output profile pending validation`

Do not use universal copy:

- `Perfect everywhere`
- `Full HDR for all apps`
- `Lossless HDR clipboard`
- `Guaranteed match`
- `True color in every viewer`

## Edge Cases

The design must explicitly handle:

- Mixed HDR/SDR monitors.
- Capture target cannot be resolved.
- HDR-capable display with Windows HDR disabled.
- SDR-only display.
- Viewer accepts the file but tone maps differently.
- Clipboard succeeds but file output fails.
- File writes successfully but open/reveal fails.
- Output profile selected but validation is stale for the current build.
- User disables optional HDR alerts.
- High contrast mode changes status color assumptions.
- Long localized labels or translated status text.
- Text scaling and high-DPI monitors.
- Very bright, very dark, and mid-gray content under overlay chrome.

## Accessibility And Native Fit

All fidelity states need text plus icon/glyph. Color alone is not acceptable.

Native implementation expectations:

- Use WinUI controls before custom controls.
- Use InfoBar, TeachingTip, Flyout, ContentDialog, CommandBar, ToggleSwitch, ComboBox, NumberBox, and standard settings rows where they fit.
- Avoid custom button-shaped toggles when native controls can express the behavior.
- Maintain visible focus and logical tab order.
- Keep status updates perceivable without noisy repeated announcements.
- Keep overlay cancel available through Escape and visible affordance.

## Impeccable Hardening Checklist

Before implementing or reviewing this extension, verify:

- No new visual system replaces v0.
- No generic SaaS dashboard or marketing hero appears in app surfaces.
- No nested cards or decorative card grids are introduced.
- No purple-blue gradient branding or glow-heavy decoration is added.
- No unfamiliar icon-only control appears without tooltip and accessible name.
- No output option looks enabled before its semantics and validation exist.
- No completion message merges artifact success with HDR preservation.
- No public release copy claims a path that lacks checklist evidence.
- No UI state relies on color alone.
- No motion delays capture, confirmation, or return to work.

## Implementation References

Use these files together:

- `harness/design/v0-mvp-reference/` for base layout and density.
- `harness/design/design-principles.md` for native Windows and HDR-trust principles.
- `harness/design/ui-review-checklist.md` for review checks.
- `_bmad-output/planning-artifacts/ux-design-specification.md` for broader UX state model.
- `_bmad-output/planning-artifacts/epics.md` for Epic 10-13 public fidelity work.
- `docs/validation/release-validation-checklist.md` for release gates and evidence vocabulary.
- `docs/adr/0001-perfect-hdr-fidelity-public-release-is-fixed-target.md` for the fixed target decision.
- `docs/adr/0002-perfect-hdr-fidelity-design-extends-v0-reference.md` for the extension-over-replacement decision.

## Confirmation Gate

This document is the design direction to carry forward unless superseded by an explicit new ADR. It keeps Perfect HDR Fidelity Public Release fixed as the public target and keeps the existing v0 MVP reference as the visual foundation.

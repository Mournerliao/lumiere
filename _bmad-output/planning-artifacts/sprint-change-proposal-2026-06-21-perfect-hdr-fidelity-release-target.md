---
project: lumiere
date: 2026-06-21
change_type: sprint_change_proposal
scope_classification: major
status: approved-for-planning
trigger: "User wants 'perfect HDR fidelity' to become the public release target rather than a post-release aspiration."
recommended_route: "MVP review plus new HDR fidelity epics before public release claims"
---

# Sprint Change Proposal: Perfect HDR Fidelity as Public Release Target

## 1. Issue Summary

Lumiere's current MVP is coherent as an early preview: it provides a native Windows capture loop with main window, tray, hotkey entry, fullscreen and region capture, configured clipboard/folder output, HDR readiness messaging, and validation tracking. However, the user now wants "perfect HDR fidelity" to become the release target.

This changes the release bar. The current plan supports an early user release with documented limitations, while "perfect HDR fidelity" requires evidence that capture target selection, preview, output conversion, metadata, target-app behavior, and validation all preserve or honestly transform HDR content.

The trigger is strategic, not a failed implementation. The existing implementation remains valuable, but the public release target needs to move from "HDR-first usable preview" to "validated HDR fidelity release."

## 2. Impact Analysis

### Product Impact

The public release claim must no longer be "core capture loop is usable with known limitations." It becomes:

> Lumiere publicly ships only when its HDR capture, preview, and supported output paths have documented fidelity semantics and Windows hardware validation evidence.

This does not invalidate the current MVP. It reclassifies it as an internal alpha or private preview foundation until HDR fidelity gates are complete.

### Epic Impact

Completed Epics 1-9 remain useful foundation work:

- Epic 1-3: native Windows, FP16/scRGB, WGC, overlay, crop, and release-to-capture foundation.
- Epic 4-9: MVP cutover, settings, output usability, tray/hotkeys, HDR trust states, validation records, and settings completion.

New work is required before public release:

- New Epic 10: Target-aware HDR detection and display capability mapping.
- New Epic 11: HDR output semantics, color conversion, metadata, and export profiles.
- New Epic 12: HDR fidelity validation suite and public release evidence.
- Optional Epic 13: UI/UX polish for fidelity confidence, accessibility, and native control semantics.

### Story Impact

Existing story status does not need rollback. New stories should be added rather than reopening broad completed epics, except where a new story modifies an accepted limitation.

Known items that should move from "deferred limitation" to "release-blocking work" for a perfect-fidelity release:

- HDR probe currently reflecting adapter 0 / output 0 rather than the capture target.
- HDR/SDR mixed multi-monitor validation.
- Actual HDR10/P3/export format implementation rather than read-only design-reference controls.
- Defined scRGB-to-SDR and scRGB-to-HDR conversion policy.
- Target-app compatibility for clipboard and file output.
- Long-run resource trend validation for capture/output loops.
- Public release validation matrix with recorded hardware evidence.

### Artifact Conflicts

The PRD currently allows MVP output to be basic usability as long as HDR-preserving claims are avoided. That remains true for an internal/private preview, but conflicts with "perfect HDR fidelity" as a public release goal.

Architecture already protects HDR invariants and warns against unsupported output claims. It needs an additional release-target decision: target-aware display evidence and output fidelity semantics are required before public release.

UX already says trust is the promise and unsupported claims must be avoided. It needs a stricter public release policy: advanced export controls must either be hidden/scoped or backed by real implementation and validation.

Validation docs already separate CI-pass from Windows manual-pass. The new early user release checklist is the right working artifact, but it should be expanded with perfect-fidelity gates before public release.

### Technical Impact

The public-release path now requires work across these technical areas:

- `Lumiere.Capture`: propagate capture target display identity and monitor/output mapping.
- `Lumiere.Graphics.Hdr`: probe HDR capability for the actual capture target, not only factory adapter 0 / output 0.
- `Lumiere.Graphics.Output`: define and implement output formats, color conversion, tone mapping, metadata, and target-app assumptions.
- `Lumiere.Infrastructure.Interop`: expose narrow DXGI/monitor/display identity APIs without leaking native handles into UI.
- `Lumiere.App.Core`: project stricter trust states and public-release wording.
- `docs/validation`: maintain hardware evidence, compatibility matrix, and release gating.

## 3. Recommended Approach

Recommended path: **Hybrid MVP Review + New Fidelity Epics**.

Do not rollback the current MVP. Treat it as the capture/workflow foundation. Add explicit pre-public-release epics for fidelity semantics, target-aware HDR detection, output pipeline, and validation.

### Rationale

The current app is useful, but "perfect HDR fidelity" is not only a UI promise. It depends on system display state, WGC frame semantics, swap-chain behavior, output conversion, file/clipboard formats, metadata, and target application behavior. Those cannot be inferred from the current MVP feature checklist.

The safest path is to preserve momentum while changing the release gate:

- Internal Alpha / Private Preview: current MVP plus documented limitations.
- Public Preview / Public Release: only after target-aware HDR detection and fidelity validation gates pass.
- 1.0: only after supported output paths have documented conversion and compatibility semantics.

### Effort Estimate

High. This is not a polish pass. It likely requires several focused epics and repeated Windows manual validation.

### Risk Assessment

High if handled only as UI copy or validation paperwork. Medium if split into target-aware detection, output semantics, and validation epics.

The largest risk is overclaiming "perfect fidelity" without proving the path end to end. The second-largest risk is implementing output conversions without a written fidelity definition.

## 4. Detailed Change Proposals

### PRD Changes

#### PRD: Success Criteria / Technical Success

Current intent:

```text
The primary preview and capture path preserves the HDR-first invariants: Windows Graphics Capture frame pool uses R16G16B16A16Float, preview presentation uses an FP16 DXGI swap chain, and scRGB/HDR readiness is represented through typed state rather than silent SDR fallback.
```

Proposed addition:

```text
For public release claims of HDR fidelity, capture readiness must be target-aware. The app must map the capture target to the relevant display/output capability instead of relying on a global or first-output HDR probe.

Public release claims of HDR-preserving output require a written fidelity record for every supported output path, including source format, destination format, transfer function, color primaries, conversion or tone-mapping policy, metadata policy, target-app assumptions, and Windows manual validation evidence.
```

Rationale: FP16/scRGB preview is necessary but not sufficient for a public "perfect HDR fidelity" claim.

#### PRD: Product Scope / MVP

Current intent:

```text
The MVP includes ... export/color format presentation only where backed by real implementation semantics ...
```

Proposed replacement for public-release scope:

```text
For an internal or private preview, Lumiere may expose basic output usability while clearly avoiding HDR-preserving claims.

For public release, Lumiere must either hide advanced output profile choices or fully implement and validate them. Any visible HDR10, P3, sRGB, ICC, HEIF, AVIF, JPEG XL, or similar output option must map to real encoder behavior, conversion policy, metadata policy, and a recorded compatibility matrix.
```

Rationale: The public release goal is no longer merely a coherent MVP; it is a validated fidelity product.

#### PRD: Measurable Outcomes

Proposed new outcomes:

```text
- The HDR readiness state shown in main window, tray, overlay, and output feedback matches the actual capture target display in single-monitor, multi-monitor, HDR/SDR mixed, and common DPI scenarios.
- Supported file output formats have documented source/destination color semantics and metadata policy.
- Supported clipboard output behavior is validated against named target applications and does not imply unsupported HDR preservation.
- A public release validation matrix records hardware, Windows version, GPU, display configuration, DPI, HDR mode, test content, target applications, and observed fidelity result.
```

### Epic Changes

#### New Epic 10: Target-Aware HDR Detection and Trust Mapping

Goal: Make HDR readiness and trust states reflect the actual capture target.

Candidate stories:

1. Map capture targets to DXGI output/display identity.
2. Replace first-output HDR probing with target-aware capability probing.
3. Model mixed HDR/SDR display states explicitly.
4. Update main window, tray, overlay, and output feedback projections to use target-specific evidence.
5. Validate target-aware HDR state on single-monitor, multi-monitor, HDR enabled/disabled, and SDR-only setups.

Acceptance direction:

- No UI surface may show "HDR Ready" based only on an unrelated display.
- Mixed-monitor limitations must be explicit if target-aware mapping cannot be proven.

#### New Epic 11: HDR Output Semantics and Format Pipeline

Goal: Define and implement supported output semantics instead of treating output as basic PNG usability.

Candidate stories:

1. Write the HDR fidelity definition: data-preserving, visual-match, SDR-compatible, and HDR-file-output modes.
2. Define scRGB to SDR PNG conversion and tone-mapping policy.
3. Define scRGB to HDR output format policy for HDR10/PQ/Rec.2020 or chosen first HDR file format.
4. Implement the first validated advanced output path.
5. Attach metadata policy and compatibility notes to each enabled output profile.
6. Keep unsupported export options hidden, disabled, or clearly scoped.

Acceptance direction:

- No export option becomes selectable until encoder, conversion, metadata, and validation evidence exist.
- Output result messages distinguish "copied/saved" from "HDR-preserved".

#### New Epic 12: HDR Fidelity Validation Suite and Public Release Evidence

Goal: Create reproducible validation evidence before public release.

Candidate stories:

1. Build a standard HDR/SDR test content set and scenario list.
2. Expand `docs/validation/early-user-release-checklist.md` into a public-release fidelity checklist.
3. Validate capture, preview, clipboard, folder output, and target apps on HDR-enabled, HDR-disabled, SDR, and mixed-monitor setups.
4. Record 50+ or 100+ cycle lifecycle/resource trend evidence.
5. Produce a public release readiness matrix with pass/fail/limitation status.

Acceptance direction:

- Public release cannot be approved with "NOT RUN" scenarios in target-aware HDR detection, supported output semantics, or primary hardware validation.
- Limitations are allowed only when release copy excludes the unsupported claim.

#### Optional New Epic 13: Fidelity Confidence UX and Accessibility Hardening

Goal: Make the UI communicate the stricter fidelity model without becoming noisy.

Candidate stories:

1. Replace custom settings toggles/selectors with native WinUI semantics where practical.
2. Strengthen status copy for "validated", "unvalidated", "degraded", and "output converted".
3. Improve InfoBar/window sizing resilience under long status text and text scaling.
4. Verify keyboard, high contrast, screen reader, and DPI behavior for public release.

Acceptance direction:

- Trust states must remain understandable without color alone.
- "Perfect HDR fidelity" must not appear as generic success copy; it must map to a validated path.

### Architecture Changes

Proposed architecture note:

```text
Public-release HDR fidelity requires target-aware evidence. Capture target selection must produce or preserve enough display identity to allow `Lumiere.Graphics.Hdr` to evaluate the relevant DXGI output/display capability. UI projections may only claim HDR readiness using evidence tied to the active target, or must label the state as unvalidated/degraded.
```

Proposed output architecture note:

```text
Output profiles are product contracts. Each enabled profile must define source pixel format, destination format, transfer function, color primaries, conversion or tone-mapping policy, metadata policy, target-app assumptions, and validation evidence. UI may not enable a profile that lacks this record.
```

### UX Changes

Proposed UX guidance:

```text
The public-release UI must distinguish four fidelity concepts:

1. Captured in HDR-first FP16/scRGB path.
2. Previewed on a validated HDR target.
3. Converted for SDR/basic output.
4. Exported through a validated HDR-preserving output profile.

Completion feedback should say what happened to the artifact, not imply full fidelity unless the path is validated.
```

### Validation Changes

Proposed validation update:

```text
`docs/validation/early-user-release-checklist.md` remains the live release-gate checklist, but public release now requires a separate "Perfect HDR Fidelity Gates" section:

- Target-aware HDR detection passes.
- Supported output profile semantics are documented.
- Supported output profile compatibility matrix passes.
- HDR/SDR/mixed display validation passes.
- DPI and multi-monitor validation passes.
- Repeated lifecycle/resource trend validation passes.
- Release notes list any limitation that remains outside the public claim.
```

## 5. Implementation Handoff

### Scope Classification

Major.

This change affects release strategy, PRD success criteria, epics, architecture notes, UX copy, validation gates, and future implementation sequencing.

### Recommended Handoff

Product Manager / Architect:

- Approve the stricter public release definition.
- Decide whether "perfect HDR fidelity" means data preservation, visual match, HDR file export, SDR-compatible output, or a tiered model.
- Convert the proposed epics into final PRD/epic updates.

Developer Agent:

- Start with Epic 10 target-aware HDR detection.
- Do not begin advanced export implementation until the fidelity definition is approved.
- Keep current MVP behavior available as internal/private preview foundation.

QA / Validation Owner:

- Backfill existing manual validation into `docs/validation/early-user-release-checklist.md`.
- Expand the checklist with public-release fidelity gates.
- Record test hardware, display topology, DPI, Windows version, GPU, app version, target apps, and observed results.

### Success Criteria

This course correction succeeds when:

- Next agents can see that public release is blocked on HDR fidelity evidence, not ordinary MVP feature completion.
- The current MVP remains useful as foundation work and is not rolled back.
- New stories target target-aware HDR detection, output fidelity semantics, and validation evidence first.
- Release copy cannot accidentally claim perfect HDR fidelity before the code and evidence support it.

## 6. Checklist Execution Notes

- 1.1 Triggering story: N/A. This is a strategic release-target change discovered during product review.
- 1.2 Core problem: Strategic pivot / stricter release bar.
- 1.3 Evidence: Existing docs allow early release with limitations; user now wants perfect HDR fidelity as the release target.
- 2.1-2.5 Epic impact: Existing epics remain valid; new epics required before public release.
- 3.1 PRD impact: Public-release success criteria need stricter fidelity semantics.
- 3.2 Architecture impact: Target-aware display evidence and output profile contracts needed.
- 3.3 UX impact: UI must distinguish capture, preview, conversion, export, and validation.
- 3.4 Other artifacts: Validation docs and release checklist need perfect-fidelity gates.
- 4.1 Direct adjustment: Not enough by itself.
- 4.2 Rollback: Not recommended.
- 4.3 MVP review: Required.
- 4.4 Selected approach: Hybrid MVP Review + New Fidelity Epics.
- 5.1-5.5 Proposal, impact, approach, action plan, and handoff documented above.
- 6.1-6.5 Approved for planning and applied to PRD, epics, architecture, UX, validation docs, deferred work, and sprint-status on 2026-06-21.

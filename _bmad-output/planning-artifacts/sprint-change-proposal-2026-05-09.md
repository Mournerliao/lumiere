---
changeTrigger: "Implementation readiness findings from implementation-readiness-report-2026-05-09.md"
mode: "Batch"
scopeClassification: "Moderate"
status: "Approved"
date: "2026-05-09"
project: "lumiere"
approvedBy: "lumiere"
approvedAt: "2026-05-09"
---

# Sprint Change Proposal - Implementation Readiness Corrections

## 1. Issue Summary

The implementation readiness assessment concluded that the planning set is close to implementation-ready from a requirements coverage standpoint, but should not proceed to implementation as-is.

The trigger is the `NEEDS WORK` status in `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-09.md`.

Evidence:

- PRD functional requirements are fully mapped: `FR1-FR49` have 100% explicit epic coverage.
- No standalone UX design artifact was found, despite the MVP being a user-facing Windows desktop app with main window, settings, tray, overlay, output feedback, hotkey, and HDR status surfaces.
- Epic 1-3 are written as historical foundation preservation, which is useful for traceability but not clean active implementation backlog.
- Epic 5 exposes settings for future-dependent behavior before the underlying output and hotkey behavior exists.
- Epic 8 mixes product trust hardening with release validation, including a release matrix story that depends on prior MVP surfaces.

Core problem statement:

The project has strong PRD and architecture coverage, but the active backlog structure is not clean enough for Phase 4 execution. The plan needs a targeted backlog correction: separate historical foundation from active work, prevent future-dependent settings from appearing functional too early, add a compact UX spec/state inventory, and reframe release validation as an explicit gate.

Issue type:

- Misunderstanding or ambiguity in original planning structure.
- Planning artifact completeness gap.
- Backlog dependency risk discovered during readiness validation.

## 2. Checklist Findings

### Section 1: Understand the Trigger and Context

- [x] 1.1 Triggering story identified: N/A. The trigger is a readiness gate finding, not a single implementation story.
- [x] 1.2 Core problem defined: active backlog structure and UX artifact completeness are insufficient for clean implementation handoff.
- [x] 1.3 Supporting evidence gathered: readiness report findings, PRD/Epics/Architecture cross-check, and absence of standalone UX artifact.

### Section 2: Epic Impact Assessment

- [x] 2.1 Current epic impact: Epic 4 can still be completed as originally planned, but the backlog around it needs clearer historical-baseline treatment.
- [x] 2.2 Epic-level changes needed:
  - Move Epic 1-3 out of active implementation flow into a historical foundation appendix.
  - Revise Epic 5 to avoid presenting future-dependent settings as functional too early.
  - Reframe Epic 8 as release validation and trust hardening.
  - Add a compact UX specification artifact before UI-heavy implementation begins.
- [x] 2.3 Remaining epics reviewed:
  - Epic 4 remains valid.
  - Epic 5 needs scope/acceptance criteria correction.
  - Epic 6 remains valid and should own active output behavior.
  - Epic 7 remains valid and should own active hotkey registration/recovery behavior.
  - Epic 8 remains valid if treated as a release gate/hardening epic.
- [x] 2.4 New epic requirement: No new implementation epic is required. A new planning artifact is required: compact UX spec/state inventory.
- [x] 2.5 Epic order: active execution should begin at Epic 4; Epic 1-3 should not appear as backlog work.

### Section 3: Artifact Conflict and Impact Analysis

- [x] 3.1 PRD conflict check:
  - No PRD requirement needs removal.
  - MVP scope remains achievable.
  - PRD may optionally add a note that UX state inventory is required before implementation of UI-heavy epics.
- [x] 3.2 Architecture conflict check:
  - Architecture already supports the correction.
  - It says Epic 1-3 are historical foundation and updated MVP implementation starts from Epic 4.
  - No architecture rewrite is required, but a small handoff note could point to the new UX spec once created.
- [x] 3.3 UI/UX conflict check:
  - No standalone UX spec exists.
  - `UX-DR1` through `UX-DR20` are embedded in epics but should be extracted or mirrored into a compact UX artifact.
- [x] 3.4 Other artifacts:
  - Sprint planning/status should start from Epic 4 and exclude Epic 1-3 as active backlog.
  - Future story files should reference the compact UX spec for UI state/copy expectations.

### Section 4: Path Forward Evaluation

Option 1: Direct Adjustment

- Status: Viable.
- Effort: Medium.
- Risk: Low.
- Rationale: The issue is a planning-structure problem, not a product or architecture failure. Requirements coverage is complete, so targeted artifact edits should be enough.

Option 2: Potential Rollback

- Status: Not viable.
- Effort: High.
- Risk: Medium.
- Rationale: No recent implementation needs to be reverted. Epic 1-3 work is intentionally preserved as historical foundation.

Option 3: PRD MVP Review

- Status: Not required.
- Effort: Medium.
- Risk: Medium.
- Rationale: MVP goals remain valid. No scope reduction is needed. The required change is backlog and UX readiness correction.

Selected path:

Direct Adjustment with a lightweight UX artifact addition.

## 3. Recommended Approach

Use a moderate-scope backlog correction before sprint planning:

1. Keep PRD scope intact.
2. Leave architecture largely intact.
3. Update `epics.md` so Epic 1-3 are clearly treated as historical baseline/traceability, not active implementation backlog.
4. Revise Epic 5 stories so settings UI does not imply behavior that belongs to Epic 6 or Epic 7 unless the control is disabled/scoped.
5. Reframe Epic 8 as release validation and trust hardening.
6. Create a compact UX specification or state inventory before UI-heavy stories are implemented.

Effort estimate:

- Backlog correction: Low to Medium.
- Compact UX spec: Medium.
- Re-readiness validation after edits: Low.

Risk assessment:

- Low product risk: PRD remains stable.
- Low architecture risk: architecture already supports the intended correction.
- Medium execution risk if skipped: sprint planning may produce misleading active backlog, premature settings UI, and under-specified UX state/copy acceptance criteria.

Timeline impact:

- Expected small delay before sprint planning.
- The delay is justified because it prevents avoidable rework during Epic 5-8 implementation.

## 4. Detailed Change Proposals

### Epics and Stories

#### Proposal E1: Separate Historical Foundation From Active Backlog

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `Epic List` and Epic 1-3 sections.

OLD:

```markdown
### Epic 1: Historical HDR Preview Foundation
Preserve the existing native WinUI/.NET foundation, FP16 Windows Graphics Capture path, D3D11/DXGI interop, FP16 scRGB swap-chain preview, and preview readiness vocabulary as historical foundation work. This epic is retained for traceability and should not be recreated as new MVP story work.
**FRs covered:** FR14, FR44, FR49.

### Epic 2: Historical Direct Capture Lifecycle
...

### Epic 3: Historical Region Overlay Release-to-Capture
...

### Epic 4: MVP Rebaseline Transition and Foundation Cutover
...
```

NEW:

```markdown
## Historical Foundation Baseline

The following historical epics are retained for traceability and evidence only. They are not active Phase 4 implementation backlog and should not be selected by sprint planning as new work:

- Historical Epic 1: HDR Preview Foundation
- Historical Epic 2: Direct Capture Lifecycle
- Historical Epic 3: Region Overlay Release-to-Capture

These records document existing implementation and validation evidence from the pre-MVP-rebaseline route. Active MVP implementation begins with Epic 4.

## Active MVP Epic List

### Epic 4: MVP Rebaseline Transition and Foundation Cutover
...
```

Rationale:

This preserves Epic 1-3 traceability while removing ambiguity that they are executable sprint backlog. It aligns `epics.md` with PRD and Architecture, both of which already state that rebaselined MVP implementation begins from Epic 4.

#### Proposal E2: Update Epic 5 Summary to Avoid Future-Dependent Behavior Claims

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `Epic 5: Native v0 Main Window and Settings Experience`

OLD:

```markdown
Users can operate Lumiere through a native WinUI experience that matches the v0 MVP reference intent: compact Lumiere branding, fullscreen and region capture actions, shortcut labels, HDR status summary, settings entry, minimize/background intent, and settings sections for shortcuts, HDR alerts, output preferences, save path, supported output behavior, timestamp naming, clipboard image option, and about/version information.
```

NEW:

```markdown
Users can operate Lumiere through a native WinUI experience that matches the v0 MVP reference intent: compact Lumiere branding, fullscreen and region capture actions, shortcut labels, HDR status summary, settings entry, minimize/background intent, and settings sections for currently supported preferences. Settings for output, shortcuts, and after-capture behavior must be read-only, disabled, validation-scoped, or explicitly marked pending until the corresponding behavior is implemented in Epic 6 or Epic 7.
```

Rationale:

Epic 5 can deliver the native shell and settings structure without pretending output and global hotkey behavior already exists. This reduces forward-dependency risk.

#### Proposal E3: Revise Story 5.3 Shortcut Settings Acceptance Criteria

Artifact: `_bmad-output/planning-artifacts/epics.md`

Story: `5.3 Add Shortcut and HDR Alert Settings UI`

Section: Acceptance Criteria.

OLD:

```markdown
**Given** the user changes a shortcut
**When** the value is captured
**Then** it is stored in shared settings state and prepared for hotkey registration in Epic 7.
```

NEW:

```markdown
**Given** global hotkey registration is not yet implemented
**When** shortcut controls are displayed in settings
**Then** they are read-only, disabled, or explicitly labeled as pending registration support, and the UI does not imply that changed shortcuts are active.

**Given** Epic 7 implements global hotkey registration
**When** shortcut editing is enabled
**Then** shortcut changes are persisted through shared settings state and registration failure or conflict recovery is handled by the Epic 7 hotkey story.
```

Rationale:

This keeps Epic 5 independently completable without requiring Epic 7 to be done first.

#### Proposal E4: Revise Story 5.4 Output Settings Acceptance Criteria

Artifact: `_bmad-output/planning-artifacts/epics.md`

Story: `5.4 Add Output Preference Settings UI`

Section: Acceptance Criteria.

OLD:

```markdown
**Given** settings are open
**When** the output section is displayed
**Then** the user can select clipboard, folder, or both as the output destination.

**Given** folder output is selected
**When** the save path field is displayed
**Then** the user can choose or change the folder through native Windows UI.
```

NEW:

```markdown
**Given** configured output behavior is not yet implemented
**When** the output section is displayed
**Then** output target, save path, timestamp naming, copy-as-image, and after-capture controls are hidden, disabled, read-only, or explicitly scoped as pending Epic 6 behavior.

**Given** Epic 6 implements configured output behavior
**When** output controls are enabled
**Then** each enabled setting is consumed by the output pipeline and reflected in per-target completion or recoverable failure feedback.
```

Rationale:

This prevents settings UI from validating only form presence while users can configure options that do not affect capture behavior.

#### Proposal E5: Move Active Output Preference Ownership to Epic 6

Artifact: `_bmad-output/planning-artifacts/epics.md`

Story: `6.1 Define Output Target Policy and Result Model`

Section: Acceptance Criteria.

OLD:

```markdown
**Given** output target is clipboard, folder, or both
**When** capture confirmation produces a valid image payload
**Then** the output pipeline attempts only the configured targets.
```

NEW:

```markdown
**Given** output target settings are enabled
**When** capture confirmation produces a valid image payload
**Then** the output pipeline reads the shared persisted output target and attempts only the configured targets.

**Given** output target settings are not yet supported by the output pipeline
**When** the settings UI is reviewed
**Then** the corresponding controls remain hidden, disabled, or explicitly scoped until this story enables them.
```

Rationale:

This makes Epic 6 the owner of turning output settings into real behavior.

#### Proposal E6: Reframe Epic 8 as Release Validation and Trust Hardening

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `Epic 8: HDR Trust, Recovery, and Release Validation`

OLD:

```markdown
Users and developers can trust what Lumiere says about capture fidelity and release readiness. This epic completes the evidence-backed HDR state model, actionable HDR alerts, degraded/unsupported/failed/completed language, structured diagnostics, validation-level records, repeated lifecycle evidence, output validation evidence, and Windows manual validation gates for HDR displays, WGC/DXGI behavior, tray/hotkeys, multi-monitor behavior, DPI scaling, and resource trends.
```

NEW:

```markdown
Users and developers can trust what Lumiere says about capture fidelity and release readiness. This epic is a trust-hardening and release-validation gate that depends on the relevant MVP surfaces existing before final validation can complete. It completes the evidence-backed HDR state model, actionable HDR alerts, degraded/unsupported/failed/completed language, structured diagnostics, validation-level records, repeated lifecycle evidence, output validation evidence, and Windows manual validation gates for HDR displays, WGC/DXGI behavior, tray/hotkeys, multi-monitor behavior, DPI scaling, and resource trends.
```

Rationale:

This keeps Epic 8 valid while removing the expectation that the release matrix can stand alone before main window, settings, output, tray, hotkeys, overlay, and HDR state surfaces exist.

#### Proposal E7: Revise Story 8.5 as Explicit Release Gate

Artifact: `_bmad-output/planning-artifacts/epics.md`

Story: `8.5 Run MVP Release Validation Matrix`

Section: Story statement.

OLD:

```markdown
As a Lumiere developer,
I want a release validation matrix for the MVP capture loop,
So that the team can decide whether Lumiere is ready for early users.
```

NEW:

```markdown
As a Lumiere release owner,
I want a final release validation matrix for the implemented MVP capture loop,
So that the team can decide whether Lumiere is ready for early users based on explicit evidence and documented gaps.
```

Rationale:

This clarifies that Story 8.5 is a release gate/checklist story, not a standalone feature story.

### UX Specification

#### Proposal U1: Create a Compact UX Spec / State Inventory

Artifact: New file, recommended path:

`_bmad-output/planning-artifacts/ux-design.md`

Suggested document outline:

```markdown
# Lumiere MVP UX Specification

## Source Inputs

- PRD user journeys and NFR21/NFR22/NFR24.
- `UX-DR1` through `UX-DR20` from `epics.md`.
- Architecture guidance for native WinUI/Fluent implementation.
- `harness/design/v0-mvp-reference` as UX reference only.

## Main Panel

- Required content.
- Capture button states.
- HDR status summary states.
- Disabled/active capture behavior.

## Settings

- Shortcut controls and pending-registration states.
- HDR alert preference.
- Output controls and pending-output states.
- Save path states.
- Timestamp and copy-as-image states.
- About/version content.

## Tray Menu

- Required commands.
- Active/disabled capture states.
- HDR status summary.
- Settings/open/quit behavior.

## Overlay

- Region selection states.
- Invalid crop recovery.
- Escape/cancel.
- Degraded/unsupported/failed status display.
- Release-to-capture behavior.

## Status and Copy Inventory

- HDR ready.
- Enable HDR.
- HDR unavailable.
- Degraded preview.
- Unsupported capture.
- Preview failed.
- Output complete.
- Output failed.
- Partial output success.

## Accessibility and Review Criteria

- State discrimination cannot rely on color alone.
- Text/icon alternatives required for key statuses.
- Capture flow must remain low-interruption.
- Settings must not imply unsupported behavior.
```

Rationale:

This addresses the readiness warning without requiring a full design overhaul. It gives implementation stories a single UX source of truth for state/copy behavior.

### PRD

#### Proposal P1: Optional PRD Note for UX State Inventory

Artifact: `_bmad-output/planning-artifacts/prd.md`

Section: `MVP - Minimum Viable Product` or `Accessibility & Usability`

OLD:

```markdown
The MVP includes shared persisted settings state across main window, tray, hotkeys, and output pipeline. It also includes validation language and records that distinguish Mac edit, Windows CI-pass, and Windows manual-pass.
```

NEW:

```markdown
The MVP includes shared persisted settings state across main window, tray, hotkeys, and output pipeline. It also includes validation language and records that distinguish Mac edit, Windows CI-pass, and Windows manual-pass.

Before UI-heavy implementation begins, the MVP UX state inventory must define main window, settings, tray, overlay, HDR status, and output feedback states sufficiently to validate non-color-only status discrimination and prevent unsupported controls from appearing functional.
```

Rationale:

This is optional because the PRD already implies the need, but adding it makes the UX readiness gate explicit.

### Architecture

#### Proposal A1: Optional Architecture Handoff Note for UX Spec

Artifact: `_bmad-output/planning-artifacts/architecture.md`

Section: `Implementation Handoff`

OLD:

```markdown
**First Implementation Priority:**

Create updated Epic 4+ implementation planning from this architecture, focused on output semantics, settings persistence, tray/hotkeys, and validation hardening around the existing capture/overlay foundation.
```

NEW:

```markdown
**First Implementation Priority:**

Create updated Epic 4+ implementation planning from this architecture, focused on output semantics, settings persistence, tray/hotkeys, and validation hardening around the existing capture/overlay foundation.

Before UI-heavy implementation stories begin, use the MVP UX specification/state inventory as the source of truth for main panel, settings, tray, overlay, HDR status, completion/failure feedback, disabled controls, and non-color-only state discrimination.
```

Rationale:

This ties architecture handoff to the missing UX artifact without changing architectural decisions.

## 5. PRD MVP Impact

MVP scope is not reduced.

No PRD functional requirement needs removal. The product goal remains the same: a native Windows HDR screenshot utility with low-interruption capture, direct monitor region workflow, configured output, tray/hotkeys, trustworthy HDR state language, and Windows validation evidence.

The change is a readiness correction:

- Clarify active backlog structure.
- Prevent premature UI behavior claims.
- Add UX state/copy specificity before UI-heavy stories.

## 6. Technical Impact

No production code change is required by this proposal.

Likely implementation impact after approval:

- Sprint planning starts from Epic 4.
- Story creation for Epic 5 must reference the UX spec and disabled/future-scoped controls.
- Story creation for Epic 6 must explicitly enable output controls only when the output pipeline consumes them.
- Story creation for Epic 7 must own hotkey registration and shortcut editing recovery.
- Story creation for Epic 8 must treat the final release validation matrix as a gate after relevant surfaces exist.

No rollback is recommended.

## 7. Implementation Handoff

Change scope classification:

Moderate.

Reason:

This does not require PRD reset or architecture redesign, but it does require backlog reorganization and a missing UX artifact before sprint planning.

Recommended routing:

- Product Owner / Developer agents:
  - Update `epics.md` according to Proposals E1-E7.
  - Ensure sprint planning excludes historical Epic 1-3 as active backlog.
- UX Designer or Product/Tech Writer:
  - Create `_bmad-output/planning-artifacts/ux-design.md` using Proposal U1.
- Architect:
  - Optional: add Proposal A1 if architecture handoff should explicitly reference the UX spec.
- PM/Product owner:
  - Optional: add Proposal P1 if PRD should carry the UX readiness gate explicitly.

Success criteria:

- `epics.md` has a clear historical baseline section and an active MVP epic list beginning with Epic 4.
- Epic 5 no longer implies active shortcut/output behavior before Epic 6 and Epic 7 implement it.
- Epic 8 is explicitly framed as release validation and trust hardening.
- A compact UX spec exists and covers main panel, settings, tray, overlay, HDR status, output feedback, disabled/future-scoped controls, and non-color-only state discrimination.
- Re-running implementation readiness no longer returns `NEEDS WORK` for these same issues.

## 8. Proposed Next Actions

1. Approve this proposal.
2. Update `epics.md` with Proposals E1-E7.
3. Create `_bmad-output/planning-artifacts/ux-design.md` using Proposal U1.
4. Optionally update PRD and Architecture with P1/A1.
5. Re-run `/bmad-check-implementation-readiness`.
6. If readiness passes, proceed to `/bmad-sprint-planning`.

## 9. Final Recommendation

Approve a Direct Adjustment path.

Do not restart PRD or architecture. Do not roll back historical implementation. The project is close to ready; the right move is a targeted planning cleanup that makes the active backlog executable and gives UI implementation a concrete UX state/copy source of truth.

## 10. Approval and Handoff Record

Approval status:

- Approved by user: yes.
- Approval date: 2026-05-09.
- Approved scope classification: Moderate.

Handoff route:

- Product Owner / Developer agents for backlog reorganization and `epics.md` updates.
- UX Designer or Product/Tech Writer for compact MVP UX specification creation.
- Architect optional review only if the architecture handoff note is added.
- PM/Product owner optional review only if the PRD UX readiness note is added.

Sprint status update:

- Not updated as part of this proposal approval.
- Reason: no epic/story additions, removals, or renumbering have been applied yet. `sprint-status.yaml` should be updated after the approved backlog edits are executed.

Workflow execution log:

- Issue addressed: Implementation readiness findings from `implementation-readiness-report-2026-05-09.md`.
- Change scope: Moderate.
- Artifacts modified by this workflow: `sprint-change-proposal-2026-05-09.md`.
- Artifacts proposed for follow-up modification: `epics.md`, new `ux-design.md`, optional PRD and Architecture notes.
- Routed to: Product Owner / Developer for backlog changes, UX Designer or Tech Writer for UX spec, optional Architect/PM review.

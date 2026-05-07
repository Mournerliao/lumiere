---
project_name: lumiere
date: 2026-04-21
workflow: bmad-correct-course
mode: batch
status: approved
change_scope: moderate
trigger_source: implementation-readiness-report-2026-04-21.md
---

# Sprint Change Proposal - Lumiere

## 1. Issue Summary

### Problem Statement

The implementation readiness assessment found that the MVP planning artifacts are close to ready, but `epics.md` contains inconsistencies that can mislead sprint planning and implementation agents.

The core issue is not a product pivot or architecture change. It is an artifact precision problem:

1. `epics.md` incorrectly says no standalone UX Design document exists.
2. Epic 5 Story 5.2 claims both FR37 and FR38, but only FR38 diagnostics visibility is truly implementation-ready.
3. Epic 6 is a post-MVP holding epic, but its stories are written in a way that could be mistaken for implementation-ready sprint work.

### Discovery Context

This issue was discovered during `bmad-check-implementation-readiness` on 2026-04-21. The generated report concluded:

- Overall readiness status: `NEEDS WORK`
- Critical issues: 0
- Major issues: 3
- Minor concerns: 3

### Evidence

- `epics.md` includes `No standalone UX Design document was found`, but `_bmad-output/planning-artifacts/ux-design-specification.md` exists and is complete.
- PRD FR37 says users can choose cursor capture only "when that option is implemented", while Story 5.2 allows cursor capture to be omitted or marked as future behavior.
- PRD FR39-FR42 are explicitly post-MVP/output/workflow capabilities, and architecture defers export, clipboard, hotkey/tray, annotation, and history decisions.

## 2. Checklist Analysis

### Section 1: Understand the Trigger and Context

- [N/A] 1.1 Triggering story: No implementation story triggered this issue. The trigger is the readiness report.
- [x] 1.2 Core problem: Misunderstanding/inconsistency in planning artifacts, not a technical limitation or strategic pivot.
- [x] 1.3 Evidence: Readiness report findings plus direct references in `epics.md`, PRD, and UX artifact discovery.

### Section 2: Epic Impact Assessment

- [x] 2.1 Current epic impact: Epic 5 needs clarification; Epic 6 needs stronger post-MVP/not-ready marking.
- [x] 2.2 Epic-level changes: Modify existing epic scope language and FR coverage notes. No new epic required.
- [x] 2.3 Remaining planned epics: Epics 1-4 remain valid. Epic 5 and Epic 6 require wording and traceability corrections.
- [x] 2.4 Future epic validity: No planned epic is obsolete. Epic 6 remains valid as roadmap/post-MVP planning only.
- [x] 2.5 Epic order/priority: No resequencing required.

### Section 3: Artifact Conflict and Impact Analysis

- [x] 3.1 PRD impact: No PRD changes required. PRD already distinguishes MVP and post-MVP scope.
- [x] 3.2 Architecture impact: No architecture changes required. Architecture already defers output/hotkey/annotation work.
- [x] 3.3 UX impact: No UX changes required. UX specification is the correct source for overlay and trust-state behavior.
- [x] 3.4 Other artifacts: `epics.md` requires updates. Sprint status does not appear to exist yet and does not need updating at this stage.

### Section 4: Path Forward Evaluation

- [x] 4.1 Direct Adjustment: Viable. Effort: Low. Risk: Low.
- [N/A] 4.2 Potential Rollback: Not viable/applicable. No implementation work has been completed that needs rollback.
- [N/A] 4.3 PRD MVP Review: Not needed. MVP remains achievable.
- [x] 4.4 Recommended path: Direct Adjustment with moderate backlog/artifact coordination.

### Section 5: Sprint Change Proposal Components

- [x] 5.1 Issue summary included.
- [x] 5.2 Epic impact and artifact adjustment needs included.
- [x] 5.3 Recommended path and rationale included.
- [x] 5.4 MVP impact and action plan included.
- [x] 5.5 Agent handoff plan included.

### Section 6: Final Review and Handoff

- [x] 6.1 Checklist completion reviewed.
- [x] 6.2 Proposal accuracy checked.
- [!] 6.3 User approval pending.
- [N/A] 6.4 Sprint status update not applicable until sprint planning exists or proposal is approved.
- [!] 6.5 Final handoff pending approval.

## 3. Impact Analysis

### Epic Impact

#### Epic 1: Trusted HDR Preview Foundation

No scope change.

Epic 1 remains the correct first implementation epic because it proves the core HDR preview pipeline.

#### Epic 2: Capture Target and Session Lifecycle

No scope change.

Epic 2 remains valid and depends only on Epic 1 outputs.

#### Epic 3: Fullscreen Overlay Crop Workflow

No scope change, but implementation agents should use the UX specification as a required input for overlay behavior.

#### Epic 4: Diagnostics and HDR Capability Trust

No scope change.

Epic 4 remains valid and is reinforced by the UX specification's diagnostics disclosure and recovery-state patterns.

#### Epic 5: Local Preferences and Diagnostic Controls

Requires clarification.

Story 5.2 should focus on FR38 advanced diagnostics visibility. FR37 cursor capture preference should be moved to deferred/post-MVP scope unless the team explicitly wants a disabled future placeholder in settings.

#### Epic 6: Post-MVP Capture Output and Workflow Expansion

Requires stronger gating.

Epic 6 should remain in the roadmap, but it should be marked as not ready for implementation until output, clipboard, hotkey/tray, and annotation semantics are specified.

### Story Impact

- Story 5.2 should remove FR37 from its Requirements list or split FR37 into a separate deferred story.
- Story 6.1 may remain as a post-MVP research/specification story.
- Stories 6.2-6.4 should be marked post-MVP/not implementation-ready or moved out of the sprintable story list.

### Artifact Conflicts

Only `epics.md` requires changes.

No PRD, Architecture, or UX specification changes are required because those documents already express the desired scope boundaries.

### Technical Impact

No code, infrastructure, deployment, or architecture impact.

The change reduces implementation risk by preventing agents from accidentally:

- Ignoring the UX specification.
- Treating cursor capture as implemented MVP scope.
- Pulling export, clipboard, hotkey/tray, or annotation into MVP before design decisions exist.

## 4. Recommended Approach

### Chosen Path

Direct Adjustment.

### Scope Classification

Moderate.

This is not a code implementation task, but it affects backlog/story readiness and should be handled before sprint planning.

### Rationale

Direct adjustment is sufficient because the PRD, architecture, and UX documents are already aligned. The problems are isolated to epics/story wording and traceability. No rollback, MVP reduction, or re-architecture is needed.

### Effort Estimate

Low.

Expected work is limited to editing `epics.md`, then optionally rerunning implementation readiness or proceeding to sprint planning.

### Risk Assessment

Low.

The primary risk is leaving the artifacts as-is, which could misroute implementation agents. Applying the correction reduces risk.

### Timeline Impact

Minimal.

This should be completed before sprint planning. It should not delay implementation materially.

## 5. Detailed Change Proposals

### Proposal 1: Correct UX Design Requirements Section

Artifact: `epics.md`

Section: `### UX Design Requirements`

OLD:

```markdown
No standalone UX Design document was found. UX-related implementation work will be derived from PRD and Architecture requirements for fullscreen overlay, crop interaction, status/degraded messaging, keyboard cancellation, and advanced diagnostics visibility.
```

NEW:

```markdown
A standalone UX Design document exists at `_bmad-output/planning-artifacts/ux-design-specification.md` and must be used as an implementation input alongside the PRD and Architecture.

UX-related implementation work must honor the UX specification's requirements for the fullscreen overlay, HDR readiness/trust states, degraded/unsupported/failed recovery messages, crop interaction, keyboard cancellation, diagnostics disclosure, target context, accessibility, and layout stability.
```

Rationale:

The current text is factually incorrect and may cause implementation agents to ignore the UX specification.

### Proposal 2: Clarify FR37 Coverage as Deferred

Artifact: `epics.md`

Section: `### FR Coverage Map`

OLD:

```markdown
FR37: Epic 5 - Users can choose future cursor capture behavior when implemented.
```

NEW:

```markdown
FR37: Deferred/Post-MVP - Users can choose future cursor capture behavior when cursor capture semantics are implemented.
```

Rationale:

FR37 itself contains deferred language. It should not be counted as an implemented MVP preference unless a concrete cursor capture behavior is specified.

### Proposal 3: Narrow Epic 5 FR Coverage

Artifact: `epics.md`

Section: `### Epic 5: Local Preferences and Diagnostic Controls`

OLD:

```markdown
**FRs covered:** FR36, FR37, FR38

**Implementation notes:** This epic is intentionally thin for MVP. Cursor capture may remain a stored preference or disabled placeholder until implementation semantics are defined; advanced diagnostics visibility is the primary MVP preference.
```

NEW:

```markdown
**FRs covered:** FR36, FR38

**Deferred FRs referenced:** FR37

**Implementation notes:** This epic is intentionally thin for MVP. Advanced diagnostics visibility is the primary MVP preference. Cursor capture must not be treated as implemented MVP behavior until cursor capture semantics are defined in a separate story.
```

Rationale:

This prevents Epic 5 from claiming implementation-ready coverage for a deferred behavior.

### Proposal 4: Narrow Story 5.2 Requirements and Acceptance Criteria

Artifact: `epics.md`

Story: `Story 5.2: Control Advanced Diagnostics Visibility`

Section: `Requirements`

OLD:

```markdown
**Requirements:** FR37, FR38, NFR22, NFR23
```

NEW:

```markdown
**Requirements:** FR38, NFR22, NFR23
```

Section: `Acceptance Criteria`

OLD:

```markdown
**Given** cursor capture preference is not fully implemented
**When** settings are displayed
**Then** cursor capture is either omitted or clearly marked as future behavior.
```

NEW:

```markdown
**Given** cursor capture semantics have not been defined
**When** settings are displayed for MVP
**Then** cursor capture is omitted from implemented preferences and not presented as a working option.
```

Rationale:

Story 5.2 should deliver diagnostics visibility. Cursor capture should not be represented as implemented or sprint-ready by this story.

### Proposal 5: Mark Epic 6 as Roadmap/Not Implementation-Ready

Artifact: `epics.md`

Section: `### Epic 6: Post-MVP Capture Output and Workflow Expansion`

OLD:

```markdown
### Epic 6: Post-MVP Capture Output and Workflow Expansion

Users can eventually export or copy capture output, choose HDR-preserving or SDR tone-mapped output, use hotkey/tray workflows, and add lightweight annotations after the HDR preview pipeline has been proven.
```

NEW:

```markdown
### Epic 6: Post-MVP Capture Output and Workflow Expansion

**Status:** Roadmap / Not ready for MVP implementation

Users can eventually export or copy capture output, choose HDR-preserving or SDR tone-mapped output, use hotkey/tray workflows, and add lightweight annotations after the HDR preview pipeline has been proven.

This epic must not be pulled into MVP sprint planning until separate research or design work defines HDR still export semantics, SDR tone mapping behavior, clipboard behavior, hotkey/tray architecture, and annotation rendering rules.
```

Rationale:

The PRD and architecture both defer these decisions. The epic should explicitly protect MVP scope.

### Proposal 6: Mark Epic 6 Stories as Post-MVP Candidates

Artifact: `epics.md`

Stories: `6.1`, `6.2`, `6.3`, `6.4`

OLD:

```markdown
### Story 6.1: Define HDR Export and Clipboard Semantics
...
### Story 6.2: Implement Explicit HDR or SDR Capture Output
...
### Story 6.3: Add Global Hotkey and Tray Workflow
...
### Story 6.4: Add Lightweight Annotation over Confirmed Capture Output
```

NEW:

```markdown
### Story 6.1: Define HDR Export and Clipboard Semantics

**Status:** Post-MVP research/specification candidate; not part of MVP implementation.
...
### Story 6.2: Implement Explicit HDR or SDR Capture Output

**Status:** Post-MVP implementation candidate; blocked until Story 6.1 or equivalent output semantics are approved.
...
### Story 6.3: Add Global Hotkey and Tray Workflow

**Status:** Post-MVP implementation candidate; blocked until hotkey/tray architecture is specified.
...
### Story 6.4: Add Lightweight Annotation over Confirmed Capture Output

**Status:** Post-MVP implementation candidate; blocked until output and annotation rendering semantics are specified.
```

Rationale:

This keeps useful roadmap material while preventing these stories from being treated as current sprint candidates.

### Proposal 7: Add MVP Readiness Note Before Epic List

Artifact: `epics.md`

Section: Before `## Epic List`

OLD:

```markdown
## Epic List
```

NEW:

```markdown
## MVP Implementation Readiness Note

Epics 1-5 define the MVP implementation lane, with FR37 explicitly deferred until cursor capture behavior is specified. Epic 6 is roadmap/post-MVP only and must not be selected for MVP sprint planning.

## Epic List
```

Rationale:

This gives sprint planning a clear boundary and prevents accidental scope creep.

## 6. Implementation Handoff

### Handoff Classification

Moderate.

This requires backlog/planning artifact correction before sprint planning. It does not require PM or architect rework because PRD, architecture, and UX remain aligned.

### Recommended Handoff

Product Owner / Developer agents.

### Responsibilities

- Product Owner or planning owner: approve scope boundary changes and FR coverage clarification.
- Developer/planning agent: apply approved edits to `epics.md`.
- Sprint planning workflow: use the corrected epics file and exclude Epic 6 from MVP sprint planning.

### Success Criteria

- `epics.md` references `ux-design-specification.md` accurately.
- FR37 is no longer presented as implemented MVP coverage.
- Story 5.2 covers FR38 diagnostics visibility only.
- Epic 6 is clearly marked roadmap/post-MVP and not implementation-ready.
- Sprint planning can start with Epics 1-5 only, with Epic 1 Story 1.1 as the first implementation candidate.

## 7. Approval Status

Approved by user on 2026-04-21.

Approved route:

- Scope classification: Moderate
- Routed to: Product Owner / Developer agents
- Deliverables: Approved Sprint Change Proposal plus backlog/planning artifact correction for `epics.md`
- Next action: Apply approved `epics.md` corrections before sprint planning.

## 8. Workflow Execution Log

- Issue addressed: Implementation readiness findings in `implementation-readiness-report-2026-04-21.md`.
- Change trigger: Planning artifact precision issues before sprint planning.
- Mode: Batch.
- User approval: yes.
- Artifacts modified by this workflow: `sprint-change-proposal-2026-04-21.md`.
- Artifacts proposed for follow-up modification: `epics.md`.
- Handoff recipients: Product Owner / Developer agents.
- Handoff status: Approved for implementation.

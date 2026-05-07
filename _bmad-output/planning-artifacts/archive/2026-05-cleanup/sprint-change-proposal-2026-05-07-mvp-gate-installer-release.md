---
workflowType: sprint-change-proposal
project_name: lumiere
user_name: lumiere
date: 2026-05-07
status: superseded
mode: batch
change_scope: moderate
trigger:
  - mvp completion gate needed
  - installer phase requested
  - 1.0 release route requested
inputDocuments:
  - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/prd.md
  - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/epics.md
  - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md
  - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/ux-design-specification.md
  - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-07-mvp-direct-capture.md
  - /Users/asherliao/Projects/lumiere/_bmad-output/implementation-artifacts/sprint-status.yaml
  - /Users/asherliao/Projects/lumiere/harness/design/mvp/lumiere-mvp-design.png
---

# Sprint Change Proposal: MVP Gate, Installer, and 1.0 Release Route

> Superseded on 2026-05-07 by `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-07-canonical-mvp-1-0-rebaseline.md`.
>
> The canonical route no longer adds Epic 7/8. It rebaselines the active plan into six epics: MVP implementation, MVP gate, installer, and 1.0 release.

## 1. Issue Summary

After approving the direct-capture MVP route, the project still lacks a BMad-trackable completion gate that says when the MVP is actually done. The current sprint plan has feature epics and backlog stories, but it does not include a formal "MVP complete" milestone or a follow-on installer/1.0 release lane.

The desired route is:

1. Finish the revised MVP.
2. Pass an explicit MVP completion gate.
3. Move into installer/package work.
4. Validate install/uninstall and release readiness.
5. Ship a 1.0 version.

Without explicit epics/stories for those gates, BMad can track story completion but cannot confidently answer "Is MVP done?" or "Are we ready for 1.0?"

## 2. Change Navigation Checklist Results

### 1. Understand Trigger and Context

- [x] 1.1 Triggering issue identified: the project needs a formal MVP completion definition and a route from MVP to installer to 1.0.
- [x] 1.2 Core problem: BMad currently tracks implementation stories, but not the milestone gates that convert feature completion into MVP/installer/release completion. Category: planning gap discovered during route review.
- [x] 1.3 Evidence gathered: sprint-status currently ends at Epic 6; the approved direct-capture proposal adds MVP implementation stories but no MVP gate or release epics.

### 2. Epic Impact Assessment

- [x] 2.1 Existing epics remain valid as feature implementation lanes.
- [x] 2.2 Add Epic 7 for MVP completion, validation, and go/no-go.
- [x] 2.3 Add Epic 8 for installer packaging and 1.0 release readiness.
- [x] 2.4 Existing Epic 6 remains post-MVP except for Story 6.0.
- [x] 2.5 Priority changes: Epic 7 starts only after the revised MVP stories are done; Epic 8 starts only after Epic 7 marks MVP complete.

### 3. Artifact Conflict and Impact Analysis

- [x] 3.1 PRD needs a release route update: MVP completion is not equal to 1.0 release.
- [x] 3.2 Architecture needs packaging/release considerations added as a future lane, not mixed into HDR preview implementation stories.
- [x] 3.3 UX artifacts need no immediate screen redesign. The MVP design board remains the MVP visual input.
- [x] 3.4 Validation docs need an MVP completion checklist and installer validation checklist.

### 4. Path Forward Evaluation

- [x] 4.1 Direct Adjustment: viable. Add two epics and their stories.
- [x] 4.2 Rollback: not viable. No completed implementation needs rollback.
- [x] 4.3 PRD MVP Review: viable. Clarify milestone definitions: MVP complete, installer complete, 1.0 ready.
- [x] 4.4 Recommended path: Direct Adjustment + lightweight PRD roadmap clarification.

### 5. Proposal Components

- [x] 5.1 Issue summary included.
- [x] 5.2 Epic/artifact impacts included.
- [x] 5.3 Recommended path included.
- [x] 5.4 MVP and release action plan included.
- [x] 5.5 Handoff plan included.

### 6. Final Review and Handoff

- [x] 6.1 Checklist completed.
- [x] 6.2 Proposal drafted.
- [!] 6.3 User approval pending.
- [!] 6.4 `sprint-status.yaml` update pending approval.
- [x] 6.5 Next steps and handoff plan included.

## 3. Impact Analysis

### Existing MVP Feature Lane

The revised MVP feature lane is now:

1. Epic 1: done.
2. Epic 2 through Story 2.5: direct monitor capture without picker.
3. Epic 3 through Story 3.6: release-to-capture/copy overlay.
4. Epic 4 MVP subset: concise user-facing status and manual validation.
5. Epic 6.0: MVP clipboard output semantics and implementation.

Epic 5 and the remaining Epic 6 stories stay post-MVP unless separately promoted.

### New Epic 7: MVP Completion and Validation Gate

Epic 7 exists so BMad has an explicit "MVP complete" signal. Epic 7 should not add broad product features. It should verify, triage, document, and decide.

Epic 7 should be marked `done` only when:

- Required MVP stories are `done`.
- Windows CI validation is complete.
- Windows manual validation is complete for HDR, SDR, full-screen app, and multi-monitor start-monitor scenarios.
- Deferred-work triage confirms which issues are MVP blockers versus post-MVP work.
- The MVP design asset has been used as implementation reference.
- The team has recorded an MVP go/no-go decision.

### New Epic 8: Installer and 1.0 Release

Epic 8 starts after Epic 7 is done. It turns the MVP build into something installable and releaseable.

Epic 8 should be marked `done` only when:

- Packaging strategy is selected and documented.
- Installer/package is produced.
- Install/uninstall behavior is validated on Windows.
- App versioning and release notes are prepared.
- The final release validation gate passes.
- A 1.0 release/tag can be created.

### Technical Impact

Affected areas:

- Planning:
  - Add Epic 7 and Epic 8 to epics/sprint tracking.
  - Define explicit milestone completion semantics.

- Validation:
  - Add MVP completion checklist.
  - Add installer validation checklist.

- Build/Release:
  - Future packaging work must respect WinUI 3, Windows App SDK, .NET 10, `net10.0-windows10.0.19041.0`, and `x64` constraints.
  - Packaging must not change HDR pipeline architecture or introduce cloud/telemetry.

No immediate production code changes are required by this proposal.

## 4. Recommended Approach

Use direct adjustment:

1. Keep current feature epics.
2. Add Epic 7 as the MVP completion gate.
3. Add Epic 8 as the installer and 1.0 release lane.
4. Update sprint tracking after approval.

Rationale:

- This gives BMad an explicit, machine-readable completion point for MVP.
- It separates "feature implemented" from "validated MVP".
- It separates "MVP done" from "1.0 shipped".
- It prevents packaging/release tasks from being mixed into HDR implementation stories.

Scope classification: Moderate.

## 5. Detailed Change Proposals

### PRD Changes

#### PRD: Release Mode

NEW:

```text
The project uses three milestone gates after the revised MVP route:

1. MVP Complete: direct region capture works end-to-end, release-to-copy succeeds, HDR preview invariants hold, user-facing statuses are understandable, and Windows manual validation passes.
2. Installer Complete: the MVP can be installed/uninstalled on target Windows machines using an approved packaging strategy.
3. 1.0 Release: installer validation, release notes, versioning, and final Windows validation are complete.
```

Rationale:

BMad needs milestone stories to know when the project moves from MVP implementation to installer/release work.

### Epics and Stories Changes

#### Add Epic 7: MVP Completion and Validation Gate

```text
### Epic 7: MVP Completion and Validation Gate

Users and developers can trust that the revised MVP is complete, validated, and ready to move into installer packaging.

Status: Backlog until the revised MVP implementation stories are complete.

Stories:

7.1 Define MVP Completion Gate
7.2 Run MVP Windows Manual Validation
7.3 Triage MVP Deferred Work and Blockers
7.4 Complete MVP Retrospective and Go/No-Go
```

##### Story 7.1: Define MVP Completion Gate

```text
As a product owner,
I want a concrete MVP completion checklist,
So that BMad and future agents can tell when MVP is actually done.

Acceptance Criteria:

Given the revised direct-capture route
When the MVP completion gate is documented
Then it lists the exact required stories, validation commands, Windows manual scenarios, design-input checks, and deferred-work triage rules.

Given a story is not required for MVP
When it is listed in the roadmap
Then the gate explicitly marks it post-MVP and does not block MVP completion.
```

##### Story 7.2: Run MVP Windows Manual Validation

```text
As a developer,
I want to run real Windows validation for the revised MVP,
So that MVP completion is not claimed from macOS edits or unit tests alone.

Acceptance Criteria:

Given the MVP stories are implemented
When validation runs on Windows hardware
Then restore, build, tests, format, direct capture, release-to-copy, HDR, SDR, full-screen app, and multi-monitor scenarios are recorded.

Given any scenario fails
When validation results are documented
Then the failure is classified as MVP blocker, degraded-but-acceptable, or post-MVP follow-up.
```

##### Story 7.3: Triage MVP Deferred Work and Blockers

```text
As a product owner,
I want all deferred work reviewed before declaring MVP complete,
So that known risks are not silently carried into release work.

Acceptance Criteria:

Given deferred work exists
When MVP triage runs
Then each item is classified as MVP blocker, release blocker, post-MVP backlog, or closed-by-design.

Given a blocker is found
When the gate is evaluated
Then MVP cannot be marked complete until the blocker is resolved or explicitly downgraded with rationale.
```

##### Story 7.4: Complete MVP Retrospective and Go/No-Go

```text
As a project owner,
I want an MVP retrospective and go/no-go decision,
So that the project intentionally moves into installer work.

Acceptance Criteria:

Given MVP validation and deferred-work triage are complete
When the retrospective is written
Then it records what shipped, what is deferred, remaining risks, validation level, and the go/no-go decision.

Given the decision is go
When sprint status is updated
Then Epic 7 can be marked done and Epic 8 can begin.
```

#### Add Epic 8: Installer and 1.0 Release

```text
### Epic 8: Installer and 1.0 Release

Users can install the MVP on Windows, launch Lumiere reliably, and receive a clearly versioned 1.0 release package.

Status: Backlog until Epic 7 is done.

Stories:

8.1 Decide Packaging Strategy
8.2 Build Installer Package
8.3 Validate Install, Launch, and Uninstall
8.4 Prepare 1.0 Versioning and Release Notes
8.5 Cut 1.0 Release
```

##### Story 8.1: Decide Packaging Strategy

```text
As a developer,
I want a documented packaging strategy,
So that Lumiere can become an installable Windows application without disrupting the native WinUI/HDR architecture.

Acceptance Criteria:

Given the app uses WinUI 3, Windows App SDK, .NET 10, and x64
When packaging options are evaluated
Then the chosen strategy documents runtime dependency handling, architecture, install location expectations, and signing/not-signing status for MVP/1.0.
```

##### Story 8.2: Build Installer Package

```text
As a user,
I want an installable Lumiere package,
So that I can install the app without running it from the development environment.

Acceptance Criteria:

Given the packaging strategy is approved
When the installer/package is built
Then it installs the app with the correct version, app identity, icon/name metadata, and x64 runtime assumptions.
```

##### Story 8.3: Validate Install, Launch, and Uninstall

```text
As a release owner,
I want install/uninstall validation,
So that the 1.0 package does not fail before users reach the capture workflow.

Acceptance Criteria:

Given the package is built
When it is installed on a clean Windows target
Then Lumiere launches, runs the MVP capture workflow, and can be uninstalled cleanly.
```

##### Story 8.4: Prepare 1.0 Versioning and Release Notes

```text
As a project owner,
I want clear versioning and release notes,
So that users know what 1.0 includes and what remains post-1.0.

Acceptance Criteria:

Given MVP and installer validation are complete
When release notes are written
Then they list the MVP capabilities, validation level, known limitations, and deferred post-MVP features.
```

##### Story 8.5: Cut 1.0 Release

```text
As a release owner,
I want to tag and publish the 1.0 release,
So that the project has a stable release milestone.

Acceptance Criteria:

Given release validation passes
When 1.0 is cut
Then the repo has a versioned release/tag, release notes, and the validated installer artifact.
```

### Sprint Status Changes

Pending approval, add:

```yaml
  epic-7: backlog
  7-1-define-mvp-completion-gate: backlog
  7-2-run-mvp-windows-manual-validation: backlog
  7-3-triage-mvp-deferred-work-and-blockers: backlog
  7-4-complete-mvp-retrospective-and-go-no-go: backlog
  epic-7-retrospective: optional
  epic-8: backlog
  8-1-decide-packaging-strategy: backlog
  8-2-build-installer-package: backlog
  8-3-validate-install-launch-and-uninstall: backlog
  8-4-prepare-1-0-versioning-and-release-notes: backlog
  8-5-cut-1-0-release: backlog
  epic-8-retrospective: optional
```

## 6. Implementation Handoff

Scope classification: Moderate.

Handoff:

1. Product Owner / Developer
   - Approve this proposal.
   - Add Epic 7 and Epic 8 to sprint status.
   - Create Story 7.1 after revised MVP implementation stories are complete, or earlier if the team wants the gate documented before implementation finishes.

2. Developer
   - Continue current next story first: Story 2.5, `Create Monitor Capture Targets Without Picker`.
   - Do not start Epic 8 packaging until Epic 7 declares MVP go.

3. Release owner
   - Use Epic 8 as the release checklist before claiming 1.0.

## 7. Success Criteria

BMad can answer project phase status as follows:

- Revised MVP implementation complete when required stories through 2.5, 3.6, 4.1, 4.4, and 6.0 are done.
- MVP complete when Epic 7 is done.
- Installer/release candidate complete when Epic 8 stories 8.1 through 8.4 are done.
- 1.0 complete when Story 8.5 is done.

## 8. Approval State

This proposal is superseded and should not be used for implementation.

No sprint-status changes should be applied from this superseded proposal.

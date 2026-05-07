---
workflowType: sprint-change-proposal
project_name: lumiere
user_name: lumiere
date: 2026-05-07
status: approved
mode: batch
change_scope: moderate
trigger:
  - canonical epic rebaseline requested
  - remove mixed MVP/post-MVP epic scope
  - define route from MVP to installer to 1.0
approved_by_user: 2026-05-07
artifactsModified:
  - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/prd.md
  - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/epics.md
  - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md
  - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/ux-design-specification.md
  - /Users/asherliao/Projects/lumiere/_bmad-output/implementation-artifacts/sprint-status.yaml
---

# Sprint Change Proposal: Canonical MVP-to-1.0 Rebaseline

## 1. Issue Summary

The previous epic structure mixed active MVP scope with post-MVP settings, tray, global hotkey, annotation, advanced export, and broad diagnostics work. That made it hard for BMad to answer which stories complete the MVP, which stories complete installer readiness, and which stories complete the 1.0 release.

The user approved a full planning rebaseline: the active epic list should contain only the revised MVP plus the installer-to-1.0 route. Post-1.0 roadmap items should remain documented but not appear as active MVP epics.

## 2. Impact Analysis

### Epic Impact

The canonical active epic list is now:

1. Epic 1: HDR Preview Foundation
2. Epic 2: Direct Capture Session Lifecycle
3. Epic 3: Release-to-Copy Overlay Workflow
4. Epic 4: MVP Output, Status, and Validation
5. Epic 5: MVP Completion Gate
6. Epic 6: Installer and 1.0 Release

Completion semantics:

- Epic 1-4 done means MVP feature implementation is complete.
- Epic 5 done means MVP is complete and validated.
- Epic 6 done means 1.0 installable release is complete.

### Artifact Impact

- PRD now includes the approved MVP-to-1.0 rebaseline, revised MVP scope, installer/1.0 release scope, and post-1.0 roadmap.
- Epics were rewritten as the canonical 1-6 active route.
- Architecture now records the approved rebaseline and boundary implications.
- UX specification now records the direct capture/release-to-copy MVP interaction.
- Sprint status now tracks only the revised MVP-to-1.0 route.

### Deferred Roadmap

The following are explicitly post-1.0 unless separately promoted:

- Full HDR-preserving still-image export semantics and implementation.
- Advanced SDR tone-mapping controls.
- Configurable clipboard behavior beyond the MVP default output.
- Cursor inclusion/exclusion controls.
- Full HDR/SDR/multi-monitor capability diagnostics beyond MVP validation needs.
- Global hotkey and system tray workflows.
- Lightweight annotations.
- Capture history.
- Installer update flow and broader distribution polish.

## 3. Recommended Approach

Use direct adjustment. The rebaseline is now applied to planning artifacts and sprint tracking.

No production code is changed by this proposal.

## 4. Detailed Change Proposals Applied

### PRD

Applied:

- Added `Approved MVP-to-1.0 Rebaseline (2026-05-07)`.
- Updated MVP from picker/confirm flow to direct monitor capture and release-to-copy.
- Added installer and 1.0 release scope.
- Moved settings, tray, global hotkey, annotation, advanced export, and history to post-1.0 roadmap.

### Epics

Applied:

- Replaced old mixed epic plan with canonical 1-6 route.
- Preserved completed story history for Epics 1-3.
- Moved MVP clipboard output into Epic 4.
- Moved MVP completion semantics into Epic 5.
- Moved installer and 1.0 release into Epic 6.

### Sprint Status

Applied:

- Removed active backlog entries for old non-MVP/post-MVP Epic 5 and Epic 6 content.
- Added active backlog entries for revised Epic 4, Epic 5, and Epic 6.
- Preserved completed story statuses for already implemented stories.

## 5. Implementation Handoff

Scope classification: Moderate.

Next story remains:

- Story 2.5: `Create Monitor Capture Targets Without Picker`

After Story 2.5, continue in sprint order:

1. Story 3.6
2. Story 4.1
3. Story 4.2
4. Story 4.3
5. Epic 5 gate stories
6. Epic 6 installer/release stories

## 6. Workflow Completion

Correct Course workflow complete.

Issue addressed: the project now has a clean active BMad route covering revised MVP through 1.0 release.

Routed to: Product Owner / Developer for story creation and implementation.


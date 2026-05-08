---
workflowType: sprint-change-proposal
project_name: lumiere
user_name: Asherliao
date: 2026-05-08
status: draft
mode: incremental
change_scope: moderate
trigger:
  - v0 MVP design reference imported
  - MVP scope expansion requested
approved_by_user: pending
artifactsModified:
  - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/epics.md
  - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/prd.md
  - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md
  - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/ux-design-specification.md
  - /Users/asherliao/Projects/lumiere/harness/planning/project-plan.md
---

# Sprint Change Proposal: v0 MVP Scope Expansion

## 1. Issue Summary

### Trigger

On 2026-05-08, the user imported a new v0 MVP design reference (`harness/design/v0-mvp-reference/`) that defines a broader feature scope than the current 6-Epic structure approved on 2026-05-07.

### Core Problem

The v0 design reference includes features that were previously classified as "Deferred Post-1.0":

1. **Settings Panel** — shortcuts, HDR settings, output target, save path configuration
2. **Tray Context Menu** — system tray integration with capture, open, settings, quit actions
3. **Full Screen Capture Mode** — direct screen capture without crop interaction
4. **Main Panel UI Refactoring** — compact layout matching v0 design (360px width, dual capture buttons)

The user has decided to expand MVP scope to include these features.

### Evidence

- v0 design reference at `harness/design/v0-mvp-reference/components/lumiere/` contains:
  - `main-panel.tsx` — Full Screen + Region buttons, HDR status indicator
  - `settings-panel.tsx` — Complete settings UI
  - `tray-context-menu.tsx` — System tray menu
  - `prototype-state.ts` — HDR status types (ready/available/unavailable), capture modes, output targets

## 2. Impact Analysis

### Epic Impact

**Current Epic Structure (2026-05-07 rebaseline):**

| Epic | Status | Impact |
|------|--------|--------|
| Epic 1: HDR Preview Foundation | Done | No impact |
| Epic 2: Direct Capture Session Lifecycle | Done | No impact |
| Epic 3: Release-to-Copy Overlay Workflow | Done | No impact |
| Epic 4: MVP Output, Status, and Validation | Backlog | Needs renumbering |
| Epic 5: MVP Completion Gate | Backlog | Needs renumbering |
| Epic 6: Installer and 1.0 Release | Backlog | Needs renumbering |

**New Epic Structure:**

| Epic | Status | Description |
|------|--------|-------------|
| Epic 1: HDR Preview Foundation | Done | Unchanged |
| Epic 2: Direct Capture Session Lifecycle | Done | Unchanged |
| Epic 3: Release-to-Copy Overlay Workflow | Done | Unchanged |
| **Epic 4: Main Panel UI Refactoring** | **Backlog** | **NEW** — Compact layout, dual capture buttons, HDR status indicator |
| **Epic 5: Full Screen Capture Mode** | **Backlog** | **NEW** — Direct screen capture without crop |
| **Epic 6: Settings Panel** | **Backlog** | **NEW** — Shortcuts, HDR, output, path configuration |
| **Epic 7: Tray Context Menu** | **Backlog** | **NEW** — System tray integration |
| Epic 8: MVP Output, Status, and Validation | Backlog | Renumbered from Epic 4 |
| Epic 9: MVP Completion Gate | Backlog | Renumbered from Epic 5 |
| Epic 10: Installer and 1.0 Release | Backlog | Renumbered from Epic 6 |

### Story Impact

**New Stories Required:**

**Epic 4: Main Panel UI Refactoring**
- 4.1 Refactor Main Panel Layout (Dashboard → Compact)
- 4.2 Implement Dual Capture Buttons (Full Screen + Region)
- 4.3 Implement HDR Status Indicator (Ready/Available/Unavailable)
- 4.4 Add Settings Button Entry

**Epic 5: Full Screen Capture Mode**
- 5.1 Implement Full Screen Capture Action
- 5.2 Skip Crop Overlay for Full Screen Mode
- 5.3 Show Lightweight Completion Feedback

**Epic 6: Settings Panel**
- 6.1 Create Settings Panel Shell
- 6.2 Implement Shortcut Configuration
- 6.3 Implement HDR Settings (Warnings, Export Format)
- 6.4 Implement Output Target Configuration (Clipboard/Folder/Both)
- 6.5 Implement Save Path Configuration
- 6.6 Implement About Section

**Epic 7: Tray Context Menu**
- 7.1 Implement System Tray Integration
- 7.2 Implement Tray Context Menu (Capture, Open, Settings, Quit)
- 7.3 Implement Tray HDR Status Display

### Artifact Conflicts

| Artifact | Conflict | Resolution |
|----------|----------|------------|
| PRD | MVP scope too narrow | Expand MVP to include settings, tray, full screen capture |
| Architecture | Missing Settings module | Add Lumiere.Settings module details |
| UX Design | Missing component definitions | Add main panel, settings, tray component specs |
| Epics | 6-Epic structure insufficient | Expand to 10-Epic structure |
| harness/planning/project-plan.md | Epic structure outdated | Update to reflect new 10-Epic structure |

### Technical Impact

- **Module boundaries**: `Lumiere.Settings` module already exists in architecture but needs implementation
- **New dependencies**: None required (WinUI 3 / Fluent sufficient)
- **Code changes**: Main panel XAML refactoring, new settings panel, tray integration

## 3. Recommended Approach

### Selected Path: Direct Adjustment

**Rationale:**

1. **No rollback needed**: Completed Epics 1-3 are unaffected
2. **Architecture supports it**: Module boundaries already accommodate Settings
3. **Clear design reference**: v0 design provides visual and interaction guidance
4. **Manageable scope**: New features are additive, not replacing existing work

**Effort Estimate:** Medium
- Document updates: 1-2 hours
- Implementation: 2-3 sprints (estimated)

**Risk Level:** Low
- No existing functionality affected
- Clear design reference reduces uncertainty

## 4. Detailed Change Proposals

### 4.1 Epics Document

**Current:** 6-Epic structure
**New:** 10-Epic structure

Changes:
- Add Epic 4: Main Panel UI Refactoring
- Add Epic 5: Full Screen Capture Mode
- Add Epic 6: Settings Panel
- Add Epic 7: Tray Context Menu
- Renumber old Epic 4 → Epic 8
- Renumber old Epic 5 → Epic 9
- Renumber old Epic 6 → Epic 10

### 4.2 PRD Document

**Section: MVP Feature Set**

Current text:
> MVP does not include: multi-action toolbar, output choices, annotation tools, tray controls, hotkey, settings controls, or advanced diagnostics.

New text:
> MVP includes: settings panel (shortcuts, HDR, output, path), tray context menu, full screen capture mode, and main panel UI refactoring.
> MVP does not include: multi-action toolbar, annotation tools, advanced diagnostics, or capture history.

**Section: Functional Requirements**

Add new FRs:
- FR43: Users can access settings panel from main panel
- FR44: Users can configure capture shortcuts
- FR45: Users can configure HDR settings (warnings, export format)
- FR46: Users can configure output target (clipboard/folder/both)
- FR47: Users can configure save path
- FR48: Users can perform full screen capture without crop interaction
- FR49: Users can access Lumiere from system tray
- FR50: Users can perform capture actions from tray context menu

### 4.3 Architecture Document

**Section: Project Structure**

Add:
```
src/Lumiere.Settings/
├── Lumiere.Settings.csproj
├── AppSettings.cs
├── SettingsStore.cs
└── SettingsViewModel.cs
```

**Section: Module Boundaries**

Update:
- `Lumiere.Settings`: local preferences, settings UI, settings persistence

### 4.4 UX Design Specification

**Section: Component Strategy**

Add new components:
- `MainPanel` — compact layout with dual capture buttons and HDR status
- `SettingsPanel` — settings configuration UI
- `TrayContextMenu` — system tray menu
- `FullScreenCaptureAction` — direct screen capture without crop

### 4.5 harness/planning/project-plan.md

**Section: 当前状态**

Update Epic list to reflect 10-Epic structure.

## 5. Implementation Handoff

### Scope Classification

**Moderate** — Requires backlog reorganization and PO/DEV coordination.

### Handoff Recipients

| Role | Responsibility |
|------|---------------|
| Product Owner / Developer | Update Epics and Sprint Status |
| Developer | Implement new Epic stories |

### Deliverables

1. Sprint Change Proposal document (this file)
2. Updated Epics document
3. Updated PRD document
4. Updated Architecture document
5. Updated UX Design Specification
6. Updated harness/planning/project-plan.md

### Success Criteria

- All documents reflect new 10-Epic structure
- New Epics have clear stories and acceptance criteria
- Sprint status tracks new Epics correctly
- Implementation can proceed without ambiguity

## 6. Next Steps

1. **Approve this proposal** — User confirmation required
2. **Update Epics** — Add new Epic 4-7, renumber old Epic 4-6
3. **Update PRD** — Expand MVP scope, add new FRs
4. **Update UX Design** — Add component definitions
5. **Update Architecture** — Add Settings module details
6. **Update project-plan.md** — Reflect new Epic structure
7. **Begin implementation** — Start with Epic 4 (Main Panel UI Refactoring)

---

## Appendix: v0 MVP Design Reference Features

### Main Panel
- Full Screen capture button with shortcut
- Region capture button with shortcut
- HDR status indicator (Ready/Available/Unavailable)
- Settings button

### Settings Panel
- Shortcuts configuration (Full Screen, Region)
- HDR settings (warnings toggle, export format: HDR10/P3/sRGB)
- Output target (Clipboard/Folder/Both)
- Save path configuration
- Auto-open after capture toggle
- Timestamp toggle
- Copy as Image toggle
- About section

### Tray Context Menu
- Lumiere logo and HDR status
- Full Screen capture action
- Region capture action
- Open Lumiere action
- Settings action
- Quit action

### HDR Status Types
- **Ready** (green) — HDR capture available
- **Available** (yellow) — Windows HDR is off
- **Unavailable** (red) — No HDR display detected

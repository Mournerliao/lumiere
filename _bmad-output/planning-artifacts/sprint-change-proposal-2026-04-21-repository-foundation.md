---
workflow: bmad-correct-course
project_name: lumiere
date: 2026-04-21
change_trigger: Story 1.1 repository foundation scope
scope_classification: Minor
status: applied
---

# Sprint Change Proposal: Add Repository Foundation Scope to Story 1.1

## 1. Issue Summary

Story 1.1 was ready for development as the native WinUI 3/.NET foundation story, but its explicit acceptance criteria and task list began at solution scaffolding. The project context and research artifacts both state that no Git repository exists yet and that Git should be established before implementation work so project/package decisions are tracked.

The architecture already expects root-level repository files including `README.md`, `.editorconfig`, and `.gitignore`, but Story 1.1 did not make those files, formatting conventions, commit convention, or basic developer workflow part of the executable scope. This creates an avoidable handoff gap before WinUI scaffolding begins.

## 2. Impact Analysis

**Epic Impact:** Epic 1 remains valid. Story 1.1 expands slightly to include repository foundation work before WinUI scaffolding. No new epic is required and no future epic is invalidated.

**Story Impact:** Story 1.1 gains repository foundation acceptance criteria, tasks, and developer workflow notes. Later stories benefit because build, formatting, commit, and validation conventions will exist before code-heavy HDR work begins.

**Artifact Conflicts:** PRD MVP scope is not changed. Architecture already lists the expected root files, so the change aligns Story 1.1 with architecture rather than changing architecture. UX is not affected.

**Technical Impact:** Implementation order changes: initialize Git and root workflow files first; then create the WinUI solution, package files, and source modules. CI/CD is still not required by this change.

## 3. Recommended Approach

Use direct adjustment within the existing Story 1.1. This is the lowest-risk option because the story is still `ready-for-dev`, the repository is not yet scaffolded, and the required files are foundational rather than product behavior.

Effort estimate: Low.
Risk level: Low.
Timeline impact: Small, front-loaded before WinUI scaffolding.

Rollback and MVP review are not recommended. There is no completed implementation to roll back, and MVP product scope is unchanged.

## 4. Detailed Change Proposals

### Story 1.1 Acceptance Criteria

OLD:

```text
1. Given a clean repository workspace, when the solution is created, then it contains `Lumiere.sln`, `Directory.Build.props`, `Directory.Packages.props`, and the source projects defined by the architecture; and the app project targets `net10.0-windows10.0.19041.0` and `x64`.
2. Given the package configuration, when dependencies are restored, then Windows App SDK, `Vortice.Direct3D11`, `Vortice.DXGI`, and any required CsWinRT package versions are pinned as architecture-approved versions.
3. Given the solution is opened by a developer, when they inspect project references, then UI, overlay, capture, graphics, infrastructure, and settings boundaries are represented as separate projects or modules.
```

NEW:

```text
1. Given a clean repository workspace, when repository foundation work begins, then Git is initialized before WinUI scaffolding proceeds; and the repository contains `.gitignore`, `.editorconfig`, formatting configuration, README, and documented developer workflow conventions.
2. Given a clean repository workspace with repository foundation files in place, when the solution is created, then it contains `Lumiere.sln`, `Directory.Build.props`, `Directory.Packages.props`, and the source projects defined by the architecture; and the app project targets `net10.0-windows10.0.19041.0` and `x64`.
3. Given the package configuration, when dependencies are restored, then Windows App SDK, `Vortice.Direct3D11`, `Vortice.DXGI`, and any required CsWinRT package versions are pinned as architecture-approved versions.
4. Given a developer prepares a local change, when they read the repository workflow documentation, then the expected formatting command, build/restore validation commands, and commit message convention are clear enough to follow before code review.
5. Given the solution is opened by a developer, when they inspect project references, then UI, overlay, capture, graphics, infrastructure, and settings boundaries are represented as separate projects or modules.
```

Rationale: Makes repository setup an explicit first-class deliverable and preserves the original WinUI scaffolding intent.

### Story 1.1 Tasks

Add a new first task group:

```text
- [ ] Establish repository foundation before WinUI scaffolding. (AC: 1, 4)
  - [ ] Initialize Git at the repository root before creating the WinUI solution.
  - [ ] Add a Windows/.NET/Visual Studio oriented `.gitignore`.
  - [ ] Add `.editorconfig` for C#/.NET, XML/XAML, Markdown, YAML, and scripts.
  - [ ] Add formatting configuration compatible with selected .NET tooling.
  - [ ] Add `README.md` with project purpose, constraints, prerequisites, commands, and workflow.
  - [ ] Document concise Conventional Commit prefixes.
  - [ ] Record the expected pre-review validation sequence.
```

Rationale: Gives the developer agent concrete implementation steps instead of relying on implicit setup.

### Epic 1 Story 1.1

Update the requirements line to include repository foundation workflow requirements and mirror the new acceptance criteria in `epics.md`.

Rationale: Keeps the planning epic and implementation story synchronized.

## 5. Implementation Handoff

Scope classification: Minor.

Route to: Developer agent for direct implementation of Story 1.1.

Developer responsibilities:

- Initialize Git before scaffold commits.
- Add `.gitignore`, `.editorconfig`, formatting configuration, README, and developer workflow/commit convention.
- Proceed with WinUI 3/.NET 10 scaffolding only after repository foundation files exist.
- Run and document format, restore, and x64 build validation.

Success criteria:

- Story 1.1 acceptance criteria cover repository foundation and WinUI scaffolding.
- Epic 1 and Story 1.1 remain aligned.
- No PRD, UX, or sprint-status story identity changes are required.

## Checklist Completion

- [x] 1.1 Triggering story: Story 1.1, Scaffold the Native Windows App Foundation.
- [x] 1.2 Core problem: Existing story omitted repository foundation deliverables required before implementation.
- [x] 1.3 Evidence: Project context says no Git repo exists; research says establish Git before scaffolding; architecture lists root repository files.
- [x] 2.1 Current epic still viable.
- [x] 2.2 Epic-level change needed: expand Story 1.1 scope only.
- [x] 2.3 Remaining epics reviewed: no direct changes required.
- [x] 2.4 No new epic required.
- [x] 2.5 No epic resequencing required.
- [x] 3.1 PRD conflict check: no MVP scope change.
- [x] 3.2 Architecture conflict check: aligns with existing structure.
- [x] 3.3 UX conflict check: not affected.
- [x] 3.4 Other artifacts: sprint-status unchanged because no stories were added or removed.
- [x] 4.1 Direct adjustment viable, low effort, low risk.
- [N/A] 4.2 Rollback not applicable.
- [N/A] 4.3 PRD MVP review not required.
- [x] 4.4 Recommended path selected: direct adjustment.
- [x] 5.1-5.5 Proposal components completed.
- [x] 6.1-6.2 Proposal reviewed for consistency.
- [!] 6.3 Explicit approval: user requested the scope addition; this proposal records the applied change for review.
- [N/A] 6.4 sprint-status update not required.

---
status: done
---

# Story 6.5: Scope Export and Color Format Options Honestly

Status: done

## Story

As a screenshot user,
I want export and color options to reflect real implementation support,
so that I am not misled about HDR preservation.

## Acceptance Criteria

1. **Given** HDR10, P3, sRGB, or similar color/export options are considered, **when** implementation semantics are incomplete, **then** those controls may remain visible to match the design reference, but must be disabled or explicitly labeled as validation-scoped until real encoder, metadata, conversion policy, and validation evidence exist.

2. **Given** an output path is described in UI or docs, **when** HDR preservation has not been validated, **then** copy avoids language that implies validated HDR-preserving output.

3. **Given** future output formats are enabled, **when** they are accepted, **then** validation records include format choice, conversion or metadata policy, target-app assumptions, and Windows manual validation results.

## Tasks / Subtasks

- [x] **Task 1: Audit output/color copy and state** (AC: 1,2,3)
  - [x] Read settings projection, MainWindow output settings UI, README/product docs if they mention output, and validation docs.
  - [x] Search for unsupported HDR-preserving, HDR10, P3, sRGB, export, and color output claims.
  - [x] Identify controls that must remain read-only/unavailable until encoder and validation semantics exist.

- [x] **Task 2: Strengthen UI projection semantics** (AC: 1,2)
  - [x] Keep export/color options unavailable and read-only.
  - [x] Ensure copy says current output is basic PNG/clipboard/file usability, not validated HDR preservation.
  - [x] Keep output target, folder, and clipboard behavior active without implying color-managed or HDR-preserving export.

- [x] **Task 3: Add validation guidance for future formats** (AC: 3)
  - [x] Add or update validation documentation listing required evidence for any future export/color format.
  - [x] Include format choice, conversion/metadata policy, target-app assumptions, and Windows manual validation fields.
  - [x] Keep the document clear that current Epic 6 output is not validated HDR-preserving output.

- [x] **Task 4: Add focused tests** (AC: 1,2)
  - [x] Test export/color projection is unavailable/read-only.
  - [x] Test output copy avoids HDR-preserving claims while acknowledging basic output usability.
  - [x] Test docs include the future validation fields required by AC3.

- [x] **Task 5: Validate and record limits** (AC: 1,2,3)
  - [x] Run `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`.
  - [x] Run `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Run `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Run `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`.

### Review Findings

- No code defects found in the Story 6.5 review pass.

## Dev Notes

### Validation Level

**Windows CI-pass** — Automated gates pass on Windows. Controls confirmed disabled/validation-scoped; no manual validation required for intentionally pending behavior.

### Story Scope

Story 6.5 intentionally does not implement HDR10, P3, ICC metadata, encoder metadata policy, or target-app compatibility behavior. Visible HDR10/P3/sRGB controls are allowed as design-reference surface only when clearly validation-scoped and not described as validated output behavior.

### Previous Story Intelligence

Stories 6.1-6.4 implemented settings-backed clipboard/folder/both output as basic PNG/clipboard/file usability. They did not validate HDR-preserving output. Keep that boundary explicit.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 6.5`] - Story requirements and acceptance criteria.
- [Source: `_bmad-output/project-context.md`] - HDR/output claim discipline and validation rules.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Cross-Cutting Concerns Identified`] - Output semantics and validation-level language.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-23: Story created and moved to in-progress after Story 6.4 completed.
- 2026-05-23: Updated output/settings copy to distinguish active basic output behavior from unavailable advanced color/export options.
- 2026-05-23: Added `docs/validation/output-validation.md` with future format acceptance fields and manual validation scenarios.
- 2026-05-23: Validation passed: restore up to date; build succeeded with 0 warnings/errors; `Lumiere.Graphics.Tests` passed 249/249; format verification passed.

### Completion Notes List

- Advanced color/export options remain unavailable/read-only until encoder metadata, conversion policy, target-app assumptions, and Windows validation exist.
- Current output copy describes basic PNG/clipboard/file usability without validated HDR-preserving claims.
- Output validation docs now capture future format acceptance requirements.
- Validation level: Windows CI-pass for projection/doc guardrails. Future advanced format support requires Windows manual validation evidence.
- Story marked done after review and validation.

### File List

- `_bmad-output/implementation-artifacts/6-5-scope-export-and-color-format-options-honestly.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/validation/output-validation.md`
- `src/Lumiere.App.Core/SettingsPanelProjection.cs`
- `src/Lumiere.App/MainWindow.xaml`
- `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs`
- `tests/Lumiere.Graphics.Tests/Output/OutputValidationDocumentationTests.cs`

### Change Log

- 2026-05-23: Created story context and started implementation.
- 2026-05-23: Scoped export/color options honestly and marked story done.
- 2026-05-25: Added scope-correction follow-up: restore the design-reference `Export` segmented surface while keeping HDR10/P3 semantics validation-scoped.

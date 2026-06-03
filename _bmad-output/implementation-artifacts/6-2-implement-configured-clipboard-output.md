---
status: done
---

# Story 6.2: Implement Configured Clipboard Output

Status: done

## Story

As a screenshot user,
I want clipboard output to obey my copy-as-image preference,
so that captures copied to the clipboard behave consistently with settings.

## Acceptance Criteria

1. **Given** clipboard output is enabled and copy-as-image is enabled, **when** a valid fullscreen or region capture completes, **then** Lumiere writes a usable image to the Windows clipboard through the approved output path.

2. **Given** clipboard output is disabled or copy-as-image is off, **when** capture output is processed, **then** no clipboard image is written and the output result records the target as skipped.

3. **Given** clipboard write fails, **when** the failure is handled, **then** the app reports recoverable failure feedback and tears down capture resources without claiming success.

## Tasks / Subtasks

- [x] **Task 1: Audit current clipboard output execution path** (AC: 1,2,3)
  - [x] Read `ClipboardOutputService`, `OutputPolicy`, `OutputResult`, `MainWindow.TryCopyCropToClipboardAsync`, overlay completion feedback, and release-to-capture flow.
  - [x] Confirm fullscreen and region flows both construct policy-backed `OutputRequest` objects before output.
  - [x] Identify any path that can still call clipboard write while policy disables clipboard or copy-as-image.

- [x] **Task 2: Make clipboard execution policy-testable** (AC: 1,2)
  - [x] Keep Windows clipboard and D3D conversion in `Lumiere.Graphics.Clipboard`.
  - [x] Add a pure or injectable seam so policy skip and failure mapping can be tested without requiring real Windows clipboard writes or GPU hardware.
  - [x] Preserve the existing FP16-to-BGRA8 PNG path as basic clipboard usability, not HDR-preserving output.

- [x] **Task 3: Enforce configured clipboard behavior** (AC: 1,2)
  - [x] Attempt clipboard output only when policy target includes clipboard and `CopyAsImage` is true.
  - [x] Return an explicit skipped target result when clipboard output is disabled or copy-as-image is false.
  - [x] Keep invalid/null frame or invalid crop handling recoverable and explicit.

- [x] **Task 4: Preserve recoverable failure and teardown semantics** (AC: 3)
  - [x] Map clipboard conversion/write failures to `OutputResult` failure without claiming success.
  - [x] Ensure app-facing completion logic treats skipped/failed clipboard output distinctly from success.
  - [x] Keep overlay/capture teardown behavior deterministic after output failure.

- [x] **Task 5: Add focused hardware-independent tests** (AC: 1,2,3)
  - [x] Test enabled clipboard policy reaches the clipboard execution seam for valid requests.
  - [x] Test disabled clipboard target and copy-as-image false return skipped without invoking the execution seam.
  - [x] Test writer/conversion failure returns failed clipboard outcome and no success claim.
  - [x] Test app/projection copy remains honest about basic usability and unsupported HDR-preserving claims.

- [x] **Task 6: Validate and record limits** (AC: 1,2,3)
  - [x] Run `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`.
  - [x] Run `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Run `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Run `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`.
  - [x] Record that real Windows clipboard compatibility, target-app behavior, and HDR output fidelity still require Windows manual validation.

### Review Findings

- No code defects found in the Story 6.2 review pass.

## Dev Notes

### Validation Level

**Windows CI-pass** — Automated gates pass on Windows. Clipboard routing and settings integration tested. Known gap: real clipboard compatibility with target apps (Paint, Photos, Chromium) not validated.

### Story Scope

Story 6.2 makes clipboard output obey the output policy created in Story 6.1. It does not implement folder output, both-target orchestration, file naming, export/color formats, after-capture actions, tray, or hotkeys.

### Previous Story Intelligence

Story 6.1 introduced `OutputPolicy` on `OutputRequest`, per-target `OutputTargetResult`, aggregate `OutputResult`, app-boundary policy construction from `ISettingsProvider`, and a first clipboard policy skip in `ClipboardOutputService`. Reuse those contracts rather than creating another settings or output status model.

### Current Implementation State

`MainWindow.TryCopyCropToClipboardAsync` builds an `OutputRequest` using the swap-chain back buffer and the confirmed crop. It now fills `Policy` from `settingsProvider.OutputTarget`, `CopyAsImage`, `SavePath`, `TimestampNaming`, and `AfterCaptureBehavior`.

`ClipboardOutputService.ExecuteOutputAsync` still owns the actual D3D crop, FP16-to-BGRA8 conversion, PNG encoding, and Windows clipboard write. The implementation catches routine conversion/write failures and returns `OutputResult.ClipboardFailed`. It currently cannot be tested for enabled/failure paths without exercising native resources, so add the narrowest test seam that keeps platform work inside `Lumiere.Graphics.Clipboard`.

### Architecture Compliance

- `Lumiere.Graphics.Clipboard` owns Windows clipboard output and D3D conversion.
- `Lumiere.Graphics.Output` owns output request/policy/result contracts.
- `Lumiere.App` may orchestrate output and teardown but must not own conversion, PNG encoding, Windows clipboard interop, or D3D resource policy.
- `Lumiere.Settings` remains the only source of persisted output preference values.
- Clipboard output is basic usability and must not be described as HDR-preserving.

### Testing Guidance

Prefer a small injectable execution seam over broad mocking. Tests should prove policy skip does not call the seam, enabled policy does call it, and seam failures map to failed target results. Do not unit-test real Windows clipboard content, real D3D conversion correctness, or HDR fidelity claims in this story.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 6.2`] - Story requirements and acceptance criteria.
- [Source: `_bmad-output/implementation-artifacts/6-1-define-output-target-policy-and-result-model.md`] - Output policy/result contracts and completion notes.
- [Source: `_bmad-output/project-context.md`] - Clipboard output honesty and validation limits.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Integration Points`] - Output consumes crop/frame payloads, settings, and validation-aware conversion semantics.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-23: Story created from Epic 6 backlog after Story 6.1 completed.
- 2026-05-23: Existing Story 6.1 output policy/result contracts and clipboard output path reviewed for 6.2 implementation.
- 2026-05-23: Added an internal clipboard execution seam so configured skip/failure behavior can be tested without real Windows clipboard or GPU resources.
- 2026-05-23: Validation passed: restore previously succeeded with network approval; build succeeded with 0 warnings/errors; `Lumiere.Graphics.Tests` passed 233/233; format verification passed.

### Completion Notes List

- Clipboard output now has a policy-testable seam while keeping Windows clipboard and D3D conversion work inside `Lumiere.Graphics.Clipboard`.
- Configured folder target, clipboard-disabled, and copy-as-image-off policies skip clipboard output without invoking the execution seam.
- Execution failures map to failed clipboard output results without success claims.
- Validation level: Windows CI-pass for hardware-independent configured clipboard policy behavior. Real Windows clipboard compatibility, target-app behavior, and HDR output fidelity still require Windows manual validation.
- Story marked done after review and validation.

### File List

- `_bmad-output/implementation-artifacts/6-2-implement-configured-clipboard-output.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Lumiere.Graphics/Clipboard/ClipboardOutputService.cs`
- `tests/Lumiere.Graphics.Tests/Output/ClipboardOutputServicePolicyTests.cs`

### Change Log

- 2026-05-23: Created story context and started implementation.
- 2026-05-23: Implemented configured clipboard output policy enforcement and marked story done.

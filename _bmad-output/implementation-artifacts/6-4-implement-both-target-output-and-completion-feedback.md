---
status: done
---

# Story 6.4: Implement Both-Target Output and Completion Feedback

Status: done

## Story

As a screenshot user,
I want captures sent to both clipboard and folder when configured,
so that one capture can support quick sharing and durable storage.

## Acceptance Criteria

1. **Given** output target is both, **when** capture output completes, **then** clipboard and folder targets are attempted independently and the final feedback identifies which targets succeeded.

2. **Given** one target succeeds and another fails, **when** feedback is shown, **then** the message reports partial success and the specific recoverable failure without retrying indefinitely.

3. **Given** output processing is slow or failing, **when** bounded timeout or failure handling occurs, **then** overlay, WGC session, and graphics resources do not remain active indefinitely.

## Tasks / Subtasks

- [x] **Task 1: Audit configured output composition** (AC: 1,2,3)
  - [x] Read `ConfiguredOutputService`, clipboard/folder services, `OutputResult`, and `MainWindow.TryCopyCropToClipboardAsync`.
  - [x] Confirm both-target calls attempt clipboard and folder independently.
  - [x] Identify where user-facing feedback is derived from aggregate output results.

- [x] **Task 2: Harden both-target aggregation** (AC: 1,2)
  - [x] Ensure both target executes both services even if one target returns failed.
  - [x] Convert unexpected per-target service exceptions to failed target results rather than crashing the output pipeline.
  - [x] Preserve per-target success, failure, skipped, artifact path, user message, and diagnostic detail.

- [x] **Task 3: Add bounded timeout handling** (AC: 2,3)
  - [x] Add a bounded per-target timeout seam that returns recoverable failed target results.
  - [x] Ensure no indefinite retry loop is introduced.
  - [x] Preserve cancellation behavior when the caller cancels the whole output operation.

- [x] **Task 4: Improve app-facing completion feedback** (AC: 1,2,3)
  - [x] Update logging so output success/failure uses aggregate result details, not clipboard-only wording.
  - [x] Keep overlay teardown behavior deterministic regardless of success, partial success, failure, or skipped output.
  - [x] Avoid claiming HDR-preserving output in feedback.

- [x] **Task 5: Add focused tests** (AC: 1,2,3)
  - [x] Test both-target success includes clipboard and folder successes.
  - [x] Test partial success reports success overall and records the failed target.
  - [x] Test service exception and timeout become failed target outcomes without preventing the other target attempt.
  - [x] Test clipboard-only and folder-only targets call only the configured service.

- [x] **Task 6: Validate and record limits** (AC: 1,2,3)
  - [x] Run `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`.
  - [x] Run `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Run `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Run `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`.
  - [x] Record that real overlay/WGC/resource lifetime behavior under slow OS clipboard/file output still needs Windows manual validation.

### Review Findings

- No code defects found in the Story 6.4 review pass.

## Dev Notes

### Validation Level

**Windows CI-pass** — Automated gates pass on Windows. Both-target orchestration tested. Known gap: slow OS behavior and resource teardown not validated.

### Story Scope

Story 6.4 completes configured multi-target output orchestration and aggregate completion/failure feedback. It does not implement folder picker UI, export/color options, after-capture actions, tray, or hotkeys.

### Previous Story Intelligence

Story 6.1 created per-target output result contracts. Story 6.2 enforced clipboard policy and failure mapping. Story 6.3 added folder output, deterministic file naming, and `ConfiguredOutputService`. Harden that composition instead of adding a separate orchestration layer.

### Architecture Compliance

- Keep output orchestration in `Lumiere.Graphics.Output`.
- Keep app code at the workflow/logging boundary; it should not know native clipboard or file write details.
- Use typed results for expected failure/timeout states.
- Do not log captured pixels or raw frame content.
- Do not introduce retries or background workers without a story-owned lifecycle model.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 6.4`] - Story requirements and acceptance criteria.
- [Source: `_bmad-output/implementation-artifacts/6-1-define-output-target-policy-and-result-model.md`] - Output result model.
- [Source: `_bmad-output/implementation-artifacts/6-2-implement-configured-clipboard-output.md`] - Clipboard target behavior.
- [Source: `_bmad-output/implementation-artifacts/6-3-implement-folder-output-with-save-path-and-timestamp-naming.md`] - Folder target behavior.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-23: Story created and moved to in-progress after Story 6.3 completed.
- 2026-05-23: Hardened `ConfiguredOutputService` with per-target exception mapping, per-target timeout handling, and focused tests for both/partial/single target flows.
- 2026-05-23: Validation passed: restore up to date; build succeeded with 0 warnings/errors; `Lumiere.Graphics.Tests` passed 248/248; format verification passed.

### Completion Notes List

- Both-target output now attempts clipboard and folder independently and aggregates typed target results.
- Partial success remains overall success while preserving failed target details.
- Per-target service exceptions and timeouts become recoverable failed target outcomes without preventing the other configured target from running.
- App logging now reports configured output aggregate outcome instead of clipboard-only wording.
- Validation level: Windows CI-pass for hardware-independent both-target orchestration. Real overlay/WGC/resource lifetime behavior under slow OS clipboard/file output still requires Windows manual validation.
- Story marked done after review and validation.

### File List

- `_bmad-output/implementation-artifacts/6-4-implement-both-target-output-and-completion-feedback.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Lumiere.App/MainWindow.xaml.cs`
- `src/Lumiere.Graphics/Output/ConfiguredOutputService.cs`
- `tests/Lumiere.Graphics.Tests/Output/ConfiguredOutputServiceTests.cs`

### Change Log

- 2026-05-23: Created story context and started implementation.
- 2026-05-23: Implemented both-target aggregation and bounded target failure handling.

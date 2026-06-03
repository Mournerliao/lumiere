# Story 4.1: Classify Existing Foundation for MVP Cutover

Status: done

<!-- Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a Lumiere product owner,
I want the Epic 1-3 implementation classified as retained, reworked, deferred, or removed,
so that the rebaselined MVP starts from a deliberate foundation instead of accidental historical behavior.

## Acceptance Criteria

1. Given the existing app, capture, graphics, overlay, clipboard, settings, and validation artifacts, when the cutover audit is completed, then each major capability is classified as retained, reworked, deferred, or removed for the MVP route.
2. Given a historical feature remains useful, when it is retained, then the record explains which FR, NFR, UX-DR, or architecture rule it supports.
3. Given a historical behavior conflicts with the v0 MVP direction, when it is reworked, deferred, or removed, then the record states the product reason and the follow-up epic or story that owns the replacement.

## Tasks / Subtasks

- [x] Create a cutover classification document at `_bmad-output/implementation-artifacts/4-1-cutover-classification.md`. (AC: 1, 2, 3)
  - [x] Document the classification schema: retained, reworked, deferred, removed — with clear definitions for each category.
  - [x] Map each major capability from Epic 1-3 to its classification.

- [x] Classify HDR preview foundation capabilities from Epic 1. (AC: 1, 2)
  - [x] Native Windows app scaffold (Story 1.1): .NET 10, WinUI 3, Windows App SDK, module boundaries, central package management.
  - [x] HDR constants and readiness vocabulary (Story 1.2): `HdrConstants`, `PreviewReadinessStatus`, readiness mapping tests.
  - [x] D3D11 device and WinRT/DXGI interop bridge (Story 1.3): `D3D11DeviceFactory`, `SwapChainPanelNativeInterop`, COM ownership.
  - [x] FP16 scRGB swap-chain preview (Story 1.4): `SwapChainManager`, `GraphicsEngine`, FP16/scRGB presentation, `SetSwapChain(null)` teardown.
  - [x] Minimal WGC FP16 capture to live preview (Story 1.5): `CaptureService`, frame pool, FP16 frame to D3D11 texture path.

- [x] Classify direct capture lifecycle capabilities from Epic 2. (AC: 1, 2)
  - [x] Typed capture target selection (Story 2.1): `CaptureTargetSelectionResult`, `DirectMonitorCaptureTargetSelectionService`.
  - [x] Explicit capture session state (Story 2.2): `CaptureSessionState`, `CaptureSessionStatus`, generation-scoped callbacks.
  - [x] Stop, restart, and resource recreation (Story 2.3): teardown ordering, frame pool recreation, shared device preservation.
  - [x] Lifecycle validation evidence (Story 2.4): automated lifecycle tests, resource trend documentation.
  - [x] Direct monitor capture without picker (Story 2.5): `IGraphicsCaptureItemInterop::CreateForMonitor`, no-picker default path.

- [x] Classify region overlay capabilities from Epic 3. (AC: 1, 2)
  - [x] Fullscreen overlay above HDR preview (Story 3.1): `OverlayWindow`, `SwapChainPanel` base layer, topmost placement.
  - [x] Crop selection by dragging (Story 3.2): `CropController`, `CropGeometry`, pointer lifecycle, minimum size enforcement.
  - [x] Crop adjustment and recreation (Story 3.3): handle/edge adjustment, `replacementGestureSelection`, invalid geometry rollback.
  - [x] Confirm and cancel overlay paths (Story 3.4): `ConfirmedCaptureSelection`, `CloseRequested`/`CaptureConfirmed` events, `isClosingRequested` guard.
  - [x] Hit testing and keyboard Escape (Story 3.5): `CropCanvas.IsHitTestVisible` routing, Escape via `RootGrid.KeyDown` and `KeyboardAccelerator`.
  - [x] Release-to-capture and basic clipboard output (Story 3.6): `CropCommitResult`, release-to-capture auto-confirm, `ClipboardOutputService`, "Copied to clipboard" feedback.

- [x] Classify settings and infrastructure capabilities. (AC: 1, 2)
  - [x] `Lumiere.Settings` boundary: exists as module but no concrete persistence yet.
  - [x] `Lumiere.Infrastructure` diagnostics: `ILogger` via `LumiereLoggerFactory`, structured logging system.
  - [x] Validation documents: `docs/validation/lifecycle-validation.md`, `docs/validation/overlay-validation.md`.

- [x] Identify capabilities that conflict with v0 MVP direction. (AC: 3)
  - [x] Dashboard-era labels or debug-oriented commands still in default path.
  - [x] Picker-first assumptions that should be demoted to fallback/debug.
  - [x] Hardcoded status messages that should use typed state vocabulary.
  - [x] Any Confirm button behavior that conflicts with release-to-capture MVP flow.

- [x] Document rework, deferred, and removed items with follow-up ownership. (AC: 3)
  - [x] For each reworked item: state what changes are needed and which Epic 4+ story owns it.
  - [x] For each deferred item: state why it's deferred and which future epic owns it.
  - [x] For each removed item: state why it's removed and confirm no MVP dependency exists.

- [x] Map retained capabilities to their supporting FR/NFR/UX-DR/architecture rules. (AC: 2)
  - [x] Each retained item must cite at least one requirement or architecture rule it supports.
  - [x] Use the FR/NFR numbering from the epics file and the architecture decision document.

## Dev Notes

### Validation Level

**Mac edit** — Documentation/audit story only; no runtime code changes, no Windows hardware dependencies.

### Story Scope

Story 4.1 is an **audit and documentation story**, not a code implementation story. The primary output is a classification document that maps every major Epic 1-3 capability to its MVP cutover status. This document becomes the authoritative reference for all subsequent Epic 4+ stories: they must check this classification before deciding to retain, modify, or remove existing code.

This story does NOT:
- Modify any source code
- Add or remove features
- Change build configuration
- Create new modules or files (except the classification document)

This story DOES:
- Audit the entire Epic 1-3 codebase and validation evidence
- Classify each capability as retained/reworked/deferred/removed
- Map retained capabilities to requirements they satisfy
- Identify conflicts with the v0 MVP direction
- Assign ownership for rework/deferred items to specific future stories

### Current Repository Context

The codebase has completed Epic 1-3 with the following structure:

**Source modules:**
- `src/Lumiere.App/` — App.xaml, MainWindow.xaml/.cs, CaptureActionCard
- `src/Lumiere.Capture/` — CaptureService, CaptureTarget*, CaptureSession*, CaptureLifecycle*, DirectMonitorCaptureTargetSelectionService
- `src/Lumiere.Graphics/` — Devices/, Hdr/, Presentation/, Clipboard/
- `src/Lumiere.Infrastructure/` — Diagnostics/, Interop/, Clipboard/
- `src/Lumiere.Overlay/` — Crop/, Input/, Windowing/, OverlayWindow.xaml/.cs
- `src/Lumiere.Settings/` — SettingsBoundary.cs (minimal)

**Test modules:**
- `tests/Lumiere.Graphics.Tests/` — Capture/, Devices/, Hdr/, Presentation/
- `tests/Lumiere.Overlay.Tests/` — Crop*.cs, Overlay*.cs, ReleaseToCaptureTests.cs

**Validation docs:**
- `docs/validation/lifecycle-validation.md`
- `docs/validation/overlay-validation.md`

**Key types established:**
- `CaptureSessionState`, `CaptureSessionStatus` — lifecycle state model
- `PreviewReadinessStatus` — HDR readiness vocabulary
- `HdrConstants` — FP16/scRGB constants
- `CropController`, `CropGeometry`, `CropCommitResult` — crop state machine
- `ConfirmedCaptureSelection` — overlay confirm payload
- `CaptureTargetSelectionResult` — target selection outcome
- `ClipboardOutputService` — basic clipboard output (Story 3.6)

**Known issues from Story 3.6 review:**
- `ClipboardOutputService` in `Lumiere.Infrastructure` directly creates D3D11 textures, bypassing `Lumiere.Graphics` boundary (deferred architecture violation).
- `CropCommitResult.InvalidGeometry` path creates a `CropSelection` object even when geometry is invalid (low risk, deferred).

### Architecture Compliance

This story is purely analytical. No code changes are expected. The classification document must follow the architecture's module boundaries:

- `Lumiere.App`: startup, composition, orchestration
- `Lumiere.Capture`: WGC targets, session lifecycle, capture state
- `Lumiere.Graphics`: D3D11/DXGI resources, HDR constants, swap-chain presentation
- `Lumiere.Infrastructure`: WinRT/COM/Win32 interop, diagnostics primitives
- `Lumiere.Overlay`: fullscreen overlay, crop UI, pointer/keyboard interaction
- `Lumiere.Settings`: local preferences

### Previous Story Intelligence

From Story 3.6 (release-to-capture and clipboard):
- `ClipboardOutputService` exists in `Lumiere.Infrastructure/Clipboard/` — this is an architecture boundary violation that should be noted in the classification as a rework item for Epic 6.
- The "Copied to clipboard" feedback was changed to "Crop confirmed. Closing..." to avoid overclaiming clipboard success.
- Release-to-capture auto-confirm replaced the manual Confirm button as the primary flow, but the button remains as fallback.

From Epic 3 retrospective:
- Overlay validation is documented in `docs/validation/overlay-validation.md`.
- Manual Windows validation is required for overlay behavior across HDR/SDR displays, multi-monitor, and DPI scales.

### Git Intelligence

Recent commits show the project direction:
- `a07bba3 docs: rebaseline BMad MVP planning artifacts` — the rebaseline to Epic 4+ route
- `abdcecf docs: complete Epic 3 retrospective and add Stories 4.6-4.7 to Epic 4` — Epic 3 closed, Epic 4 stories defined
- `3892d0b feat: introduce structured logging system with Microsoft.Extensions.Logging` — infrastructure addition that Epic 4+ stories should reuse

The codebase is stable at the Epic 3 completion point. No code changes have been made since the rebaseline.

### Classification Definitions

**Retained:** The capability exists, works, and directly supports MVP requirements. No code changes needed for the MVP cutover. Future stories may extend but should not rewrite.

**Reworked:** The capability exists but has issues that conflict with the v0 MVP direction. Code changes are needed, and a specific Epic 4+ story owns the rework.

**Deferred:** The capability exists but is not needed for MVP. It may be kept in the codebase but should not be exposed in the default user path. A future epic owns it.

**Removed:** The capability conflicts with MVP direction and has no future value. It should be deleted or disabled.

### Anti-Patterns to Avoid

- Do NOT classify capabilities as "retained" if they have known conflicts with the v0 MVP direction. Use "reworked" instead.
- Do NOT classify capabilities as "removed" if they have future value beyond MVP. Use "deferred" instead.
- Do NOT create vague classifications like "mostly retained" or "partially reworked." Be precise about what is retained and what needs rework.
- Do NOT skip mapping retained capabilities to their supporting requirements. Every retained item must have a traceability link.
- Do NOT forget to assign follow-up ownership for reworked/deferred items. Each must point to a specific story or epic.
- Do NOT modify any source code in this story. The output is a classification document only.

### UX Requirements

The v0 MVP reference at `harness/design/v0-mvp-reference/` defines the target UX direction. Key UX principles to check against:
- Compact capture-first layout (no dashboard)
- Direct monitor capture as default (no picker-first)
- Lightweight status feedback (no annotation toolbar)
- HDR status with non-color-only discrimination
- Settings organized for currently supported preferences only

### Testing Requirements

No automated tests are required for this story since it produces a documentation artifact only. However, the classification document should note which existing tests cover retained capabilities and which tests may need updates for reworked items.

Run from repository root on Windows to verify the codebase builds and existing tests pass (baseline check):

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

### References

- [Source: _bmad-output/planning-artifacts/epics.md] — Epic 4 story definitions and acceptance criteria
- [Source: _bmad-output/planning-artifacts/architecture.md] — Architecture decisions, module boundaries, and pattern rules
- [Source: _bmad-output/planning-artifacts/prd.md] — Functional and non-functional requirements
- [Source: _bmad-output/project-context.md] — Critical implementation rules for AI agents
- [Source: _bmad-output/implementation-artifacts/3-6-release-to-capture-and-copy.md] — Previous story with review findings and known issues
- [Source: docs/validation/lifecycle-validation.md] — Lifecycle validation evidence
- [Source: docs/validation/overlay-validation.md] — Overlay validation evidence
- [Source: harness/design/v0-mvp-reference/] — UX reference for MVP direction

## Dev Agent Record

### Agent Model Used

mimo-v2.5-pro

### Debug Log References

### Completion Notes List

- Audited entire Epic 1-3 codebase: 100+ source files across 6 modules, 26 test files, 2 validation documents, 3 retrospective documents
- Created comprehensive cutover classification document at `_bmad-output/implementation-artifacts/4-1-cutover-classification.md`
- Classification results:
  - **Retained:** All Epic 1 stories (1.1-1.5), all Epic 2 stories (2.1-2.5), Epic 3 stories 3.1, 3.2, 3.5, and core parts of 3.4/3.6
  - **Reworked:** Story 3.3 (adjustment handles dead code), Story 3.4 UX deviations (Cancel button, invalid crop behavior), Story 3.6 feedback, Settings persistence
  - **Deferred:** `ClipboardOutputService` architecture boundary violation, adjustment handle dead code, SDR/multi-monitor testing, COM pointer ownership patterns
  - **Removed:** None — all existing code has future value
- No capabilities conflict with v0 MVP direction: picker is already demoted, no dashboard-era labels found, no hardcoded status messages in low-level types
- Mapped all retained capabilities to their supporting FR/NFR/UX-DR/architecture rules
- Documented rework ownership: Story 4.6 owns overlay UX deviations, Story 5.5 owns settings persistence, Epic 6 owns output semantics
- Documented deferred items with future owner assignments

### File List

- `_bmad-output/implementation-artifacts/4-1-cutover-classification.md` (new)

### Review Findings

- [x] [Review][Decision] 调整手柄：REWORKED vs Deferred 矛盾 + owner 模糊 — Story 3.3 将调整手柄标为 REWORKED（owner: Story 4.6 or future UX），但 Deferred Items 表也列出 "adjustment handles dead code"（owner: Future UX enhancement epic）。同一能力不能同时 reworked 和 deferred。需决定：是 reworked（由 4.6 负责）还是 deferred（未来 UX）？→ 已改为纯 REWORKED，owner: Story 4.6
- [x] [Review][Decision] Picker 降级与 Story 4.3 矛盾 — 冲突能力章节称 picker "No active conflict found"、"Action: None needed"，但 sprint-status.yaml 中 Story 4.3 标题为 "Demote Legacy Picker and Dashboard Behavior from the Default Path"。如果 picker 已正确降级为 fallback，4.3 无需存在；如果 4.3 仍需执行，则 RETAINED 分类有误。→ 已更新冲突能力章节，标记为 "Potential conflict — pending Story 4.3"
- [x] [Review][Patch] 混合分类 Schema 未定义 [4-1-cutover-classification.md:9-18] — Schema 定义了 4 个互斥类别，但文档使用 "RETAINED (foundation) / REWORKED (UX deviations)" 混合分类。需在 Schema 中定义混合分类规则。→ 已添加混合分类定义
- [x] [Review][Patch] ClipboardOutputService 位置描述错误 [4-1-cutover-classification.md:405] — Deferred Items 表称 "in Infrastructure, creates D3D11 textures"，但实际在 `Lumiere.Graphics.Clipboard` 命名空间。应修正为 "in Graphics.Clipboard"。→ 已修正
- [x] [Review][Patch] Confirm 按钮未在汇总表中 [4-1-cutover-classification.md:367-371,392-394] — 冲突能力章节 #4 标记 Confirm 按钮为 "partial conflict — classified as rework"，但汇总表中 Story 3.4 的 Rework Owner 列未提及。应添加到汇总表。→ 已添加
- [x] [Review][Patch] CropCommitResult.Adjusted 死代码未分类 [4-1-cutover-classification.md] — `Adjusted` 枚举值仅从 `IsAdjusting` 分支返回，而该分支已被标为死代码。应添加到 Story 3.3 的 rework 或 Deferred Items。→ 已添加到 Story 3.3 rework
- [x] [Review][Patch] InvalidGeometry deferred 项缺 owner [4-1-cutover-classification.md:406] — "Code cleanup, not blocking" 不是具体 owner。应分配具体 story/epic 或明确标记为无需 owner。→ 已更新为 "Epic 4+ code cleanup"
- [x] [Review][Patch] Usage Guide 缺少修改 deferred 路径 [4-1-cutover-classification.md:422-427] — 步骤 2 说 "Before modifying code: check rework owner"，步骤 3 说 "Before removing code: check not needed"，但没有 "修改 deferred 代码" 的指引。→ 已添加步骤 3
- [x] [Review][Defer] 文档故事 review 退出标准 — sprint-status 定义 review 为 "Ready for code review"，但文档故事没有从 review 到 done 的定义路径。预存问题，非本文档范围。
- [x] [Review][Defer] Deferred 跟踪机制 — 6 个 deferred 项未创建对应的 story/epic 跟踪。预存问题，非本文档范围。

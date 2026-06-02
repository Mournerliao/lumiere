# Story 8.2: Implement Actionable HDR Alerts

Status: done

## Story

As a screenshot user,
I want concise alerts when HDR is unavailable, degraded, unsupported, or failed,
so that I understand what happened without reading diagnostics during capture.

## Requirements Covered

FR12, FR13, FR20, NFR14, NFR22; UX-DR10

## Acceptance Criteria

1. **Given** HDR alerts are enabled, **when** HDR unavailable, degraded, unsupported, or failed state occurs, **then** Lumiere shows concise actionable feedback appropriate to the surface.

2. **Given** HDR alerts are disabled, **when** a non-critical HDR warning occurs, **then** Lumiere suppresses optional alert chrome while preserving status and diagnostics.

3. **Given** capture cannot continue safely, **when** failure is surfaced, **then** the overlay or session returns to idle/failed state without stranded topmost windows or active WGC resources.

## Tasks / Subtasks

- [x] Task 1: Wire `HdrAlertsEnabled` into projection layer (AC: 1, 2)
  - [x] Subtask 1.1: Add `bool hdrAlertsEnabled` parameter to `MainPanelProjection.Project()` and propagate from all call sites
  - [x] Subtask 1.2: Add a computed `AlertMessage` string property to `MainPanelProjection` — when `hdrAlertsEnabled` is true and readiness is `Degraded`, `Unsupported`, or `Failed`, project a concise actionable hint (e.g., "Enable HDR in Windows Display settings for best capture quality"); when `hdrAlertsEnabled` is false, set `AlertMessage` to empty string
  - [x] Subtask 1.3: Add a computed `bool HasAlert` property that is true when `AlertMessage` is non-empty

- [x] Task 2: Wire alert projection into MainWindow UI (AC: 1, 2)
  - [x] Subtask 2.1: In `MainWindow.xaml`, add a WinUI `InfoBar` element below the trust status bar (`TrustStatusBorder` area) with `IsOpen` bound to `HasAlert` and `Message` bound to `AlertMessage`, using the existing severity mapping (Warning for Degraded, Error for Unsupported/Failed)
  - [x] Subtask 2.2: In `MainWindow.xaml.cs` `UpdateMainPanelProjection()`, pass `settingsProvider.HdrAlertsEnabled` to `MainPanelProjection.Project()` and apply the alert to the `InfoBar` element
  - [x] Subtask 2.3: Ensure `InfoBar.IsClosable = true` so the user can dismiss the alert for the current session without disabling the preference
  - [x] Subtask 2.4: Verify InfoBar closes automatically when readiness returns to `Ready` or `Unknown`/`Initializing`

- [x] Task 3: Wire alert into tray menu projection (AC: 1, 2)
  - [x] Subtask 3.1: Add `string TrayAlertMessage` to `TrayMenuProjection` — project a short alert hint (e.g., "Enable HDR for best quality") when `hdrAlertsEnabled` is true and readiness is degraded/unsupported/failed
  - [x] Subtask 3.2: Pass `settingsProvider.HdrAlertsEnabled` through `TrayMenuProjection.Project()` call chain
  - [x] Subtask 3.3: Update tray menu XAML to show/hide alert hint based on `TrayAlertMessage` being non-empty

- [x] Task 4: Wire alert into overlay status (AC: 1, 2)
  - [x] Subtask 4.1: In `CreateOverlayState()` in `MainWindow.xaml.cs`, accept `hdrAlertsEnabled` parameter
  - [x] Subtask 4.2: When alerts disabled and state is `DegradedPreview` or `UnsupportedCapture`, use a muted overlay message (e.g., remove the `Message` hint text) while keeping the `Label` and `Status` unchanged
  - [x] Subtask 4.3: When alerts enabled, include actionable hint in the overlay `Message` field (e.g., "Enable HDR in Windows for best capture quality")

- [x] Task 5: Update settings panel to pass-through alert state (AC: 2)
  - [x] Subtask 5.1: Verify `SettingsPanelProjection.OptionalHdrAlertChromeEnabled` correctly drives the alert chrome on/off in the settings preview or any downstream consumer — removed as redundant with new projection approach
  - [x] Subtask 5.2: Ensure toggling `HdrAlertsEnabled` while a session is active immediately reflects in the main panel alert and tray alert (call `ApplySettingsProjection` which already propagates)

- [x] Task 6: Handle alert state lifecycle during capture transitions (AC: 1, 3)
  - [x] Subtask 6.1: In `ApplySessionState()`, after updating projections, refresh the alert display so transitioning from Degraded→Capturing (HDR recovered) clears the alert
  - [x] Subtask 6.2: Verify transitioning to Idle/Closed state clears any active alert
  - [x] Subtask 6.3: Verify capture failure states (`PreviewFailed`) with `RequiresFailureTeardown = true` still return the session to idle/failed state without stranded resources — this is already implemented; verify no regression

- [x] Task 7: Unit tests (AC: all)
  - [x] Subtask 7.1: Add tests in `MainPanelProjectionTests` verifying `AlertMessage` is non-empty for Degraded/Unsupported/Failed when `hdrAlertsEnabled = true`
  - [x] Subtask 7.2: Add tests in `MainPanelProjectionTests` verifying `AlertMessage` is empty for all states when `hdrAlertsEnabled = false`
  - [x] Subtask 7.3: Add tests in `MainPanelProjectionTests` verifying `AlertMessage` is empty when readiness is Ready regardless of `hdrAlertsEnabled`
  - [x] Subtask 7.4: Add tests in `TrayMenuProjectionTests` verifying `TrayAlertMessage` projection with alerts enabled and disabled
  - [x] Subtask 7.5: Update existing projection tests that call `Project()` with new parameter
  - [x] Subtask 7.6: Run full validation: restore, build, tests, format verification

- [x] Task 8: Validate and record (AC: all)
  - [x] Subtask 8.1: Run automated gates: restore, build, all tests, format verification
  - [x] Subtask 8.2: Record validation level: Mac edit / Windows CI-pass

## Dev Notes

### Architecture Guardrails

- **State model ownership:** `PreviewReadinessState` and `PreviewReadinessStatus` live in `Lumiere.Graphics.Hdr`. `CaptureSessionState` and `CaptureSessionStatus` live in `Lumiere.Capture`. `MainPanelProjection` and `TrayMenuProjection` live in `Lumiere.App`. `OverlayState` lives in `Lumiere.Overlay`. Alert projections are UI-layer concerns in `Lumiere.App`; do NOT add alert states to the core capture or graphics models.
- **Single vocabulary rule:** Do NOT create a parallel status enum for alerts. The alert is a projection-layer concern derived from existing `PreviewReadinessState` + `HdrAlertsEnabled`.
- **No HDR-preserving claims:** Alert messages must not claim HDR preservation. Say "Enable HDR for best capture quality" not "Enable HDR for HDR capture."
- **Non-color-only discrimination (NFR21):** Alert InfoBar uses WinUI's built-in `InfoBarSeverity` (Informational, Warning, Error) which combines icon + text + color. This satisfies NFR21. The overlay already uses `Label` text + `OverlayStatusStyle` for non-color discrimination.
- **Preserve typed result patterns:** Do not add alert-related fields to `CaptureSessionState` or `PreviewReadinessStatus`. Alert display is a projection concern.

### Current State Model — Files to Modify

**`MainPanelProjection`** ([MainPanelProjection.cs](file:///d:/UGit/lumiere/src/Lumiere.App.Core/MainPanelProjection.cs)):
- `Project()` currently takes `CaptureSessionState` + optional `OutputResult?`. Must add `bool hdrAlertsEnabled` parameter.
- `MapTrust()` already handles all readiness states. Alert message should be derived from the same readiness state.
- New properties: `AlertMessage` (string), `HasAlert` (bool computed).

**`TrayMenuProjection`** ([TrayMenuProjection.cs](file:///d:/UGit/lumiere/src/Lumiere.App.Core/TrayMenuProjection.cs)):
- Already receives `MainPanelProjection` and derives trust labels.
- Add `TrayAlertMessage` property derived from readiness + `hdrAlertsEnabled`.

**`SettingsPanelProjection`** ([SettingsPanelProjection.cs](file:///d:/UGit/lumiere/src/Lumiere.App.Core/SettingsPanelProjection.cs)):
- Already has `HdrAlertsEnabled` and `OptionalHdrAlertChromeEnabled`.
- `OptionalHdrAlertChromeEnabled` is set to same value as `HdrAlertsEnabled` but not consumed anywhere. This story should make it meaningful or remove it if redundant with the new projection approach.

**`MainWindow.xaml`** ([MainWindow.xaml](file:///d:/UGit/lumiere/src/Lumiere.App/MainWindow.xaml)):
- Trust status bar is at lines 123-183 (`TrustStatusBorder`).
- InfoBar should be placed after the trust status bar, before the capture action area.
- WinUI `InfoBar` supports `IsOpen`, `Message`, `Severity`, `IsClosable`, and `Title`.

**`MainWindow.xaml.cs`** ([MainWindow.xaml.cs](file:///d:/UGit/lumiere/src/Lumiere.App/MainWindow.xaml.cs)):
- `UpdateMainPanelProjection()` (~line 1270): Must pass `settingsProvider.HdrAlertsEnabled` to `Project()`.
- `ApplySessionState()` (~line 1186): Already calls `UpdateMainPanelProjection()` and updates overlay. Alert will flow through existing state pipeline.
- `CreateOverlayState()` (~line 1894): Accept `hdrAlertsEnabled` and modify overlay message when alerts are disabled.
- `ApplySettingsProjection()` (~line 1348): Already calls `UpdateMainPanelProjection()`. The settings toggle handler already calls this method, so toggling will immediately reflect.

**`OverlayState`** ([OverlayState.cs](file:///d:/UGit/lumiere/src/Lumiere.Overlay/OverlayState.cs)):
- Factory methods accept `message` parameter. When alerts disabled, pass empty or neutral message for DegradedPreview/UnsupportedCapture.
- No structural changes to `OverlayState` record itself.

### Alert Message Semantics

| ReadinessState | Alert Enabled | AlertMessage | Severity |
|---|---|---|---|
| `Ready` | any | "" (no alert) | — |
| `Degraded` | true | "Enable HDR in Windows Display settings for best capture quality." | Warning |
| `Degraded` | false | "" (suppressed) | — |
| `Unsupported` | true | "HDR capture is not supported on this display." | Error |
| `Unsupported` | false | "" (suppressed) | — |
| `Failed` | true | "Preview failed. Capture may not produce HDR-quality output." | Error |
| `Failed` | false | "" (suppressed) | — |
| `Unknown`/`Initializing` | any | "" (no alert yet) | — |
| Output states | any | "" (output feedback already handled by OutputResult) | — |

### Overlay Message Semantics

| OverlayDisplayStatus | Alert Enabled | Overlay Message |
|---|---|---|
| `DegradedPreview` | true | "Enable HDR in Windows for best capture quality" |
| `DegradedPreview` | false | "" (muted — label "Degraded preview" still shows) |
| `UnsupportedCapture` | true | "HDR capture is not supported on this display" |
| `UnsupportedCapture` | false | "" (muted — label "Unsupported capture" still shows) |
| `PreviewFailed` | any | Keep existing failure message (this is a terminal teardown state) |

### Previous Story Intelligence (Story 8.1)

Story 8.1 completed the evidence-based HDR state mapping:

1. **Design decision (Option C):** output-complete and output-failed are projection-only states derived from `OutputResult`, not added to `PreviewReadinessState` or `CaptureSessionStatus`. This preserves the capture lifecycle model integrity.
2. **7 distinguishable trust labels** exist: HDR Ready, Checking HDR, Enable HDR, HDR unavailable, Preview failed, Output complete, Output error.
3. **`MainPanelProjection.Project()`** accepts optional `OutputResult?` parameter. `MapTrust()` private method handles the mapping.
4. **7 distinct icons** exist: CheckmarkCircle, Clock, Desktop, ErrorCircle, ErrorBadge, InfoCircle, WarningCircle.
5. **`OutputResult` passthrough** added to `TrayMenuProjection`, `AppShellProjection`, `SettingsPanelProjection`.

**Key learnings for Story 8.2:**
- `ApplySessionState` uses a reentrancy guard with deferred application. New state transitions must work correctly with this pattern — do not assume synchronous state propagation. Alert display updates should be part of the same `UpdateMainPanelProjection` / `ApplySessionState` pipeline, not a separate side-channel.
- `OutputResult` is already threaded through projections as an optional parameter. `HdrAlertsEnabled` should follow the same pattern — add it as a parameter, not a global state lookup in projection methods.
- `AppShellProjection.OpenSettings`/`CloseSettings` previously discarded output state; verify alert state is similarly preserved across settings navigation.
- Tests: 285/287 passing (2 pre-existing `DefaultSettingsProviderTests` failures unrelated to changes). Build clean.

### Git Intelligence Summary

Recent commits show the pattern for Epic 8 work:
```
68b97fd feat: complete evidence-based HDR state mapping with output result projection
34a26b6 docs: add Epic 9 settings panel completion plan and sprint change proposal
08a858f docs: record Epic 7 Windows manual validation completed
29e3dd8 feat: resolve capture state technical debt (Story 7.6)
```
- Story 8.1 extended projection layer with `OutputResult` parameter — same pattern for `HdrAlertsEnabled`.
- Projection tests are in `tests/Lumiere.Graphics.Tests/App/`.

### Project Structure Notes

- Projection layer tests: `tests/Lumiere.Graphics.Tests/App/MainPanelProjectionTests.cs`, `TrayMenuProjectionTests.cs`
- Overlay tests: `tests/Lumiere.Overlay.Tests/`
- XAML UI: `src/Lumiere.App/MainWindow.xaml`
- Code-behind: `src/Lumiere.App/MainWindow.xaml.cs`
- WinUI `InfoBar` is available in Windows App SDK 1.8 — no additional package needed

### Key Implementation Constraints

1. **Do NOT add alert state to `CaptureSessionState` or `PreviewReadinessStatus`.** Alerts are projection-only.
2. **Do NOT use `BitmapImage`, `SoftwareBitmap`, `GDI`, `WIC`, or SDR fallback for alert presentation.** Use native WinUI `InfoBar` or equivalent WinUI controls.
3. **Alert dismissal (closable InfoBar) is session-only.** Closing the alert does not change the `HdrAlertsEnabled` setting. The setting governs whether alerts appear at all.
4. **Recovery path:** When `ApplySessionState` is called with a state that resolves to `Ready`, the alert should auto-dismiss (`IsOpen = false`).
5. **Reuse existing `MainPanelProjection` severity mapping** for `InfoBar.Severity`: Warning → `InfoBarSeverity.Warning`, Error → `InfoBarSeverity.Error`.
6. **Preserve existing test patterns:** xUnit `[Fact]` and `[Theory]` attributes, `*Tests.cs` naming convention.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md` — Epic 8, Story 8.2] — Acceptance criteria, requirements, and story scope
- [Source: `_bmad-output/planning-artifacts/architecture.md` — Implementation Patterns] — Naming, state/result models, module ownership, diagnostics
- [Source: `_bmad-output/project-context.md` — Critical Implementation Rules] — Framework-specific rules, testing rules, code quality rules
- [Source: `_bmad-output/planning-artifacts/ux-design-specification.md` — HDR Trust and Failure Recovery Flow] — State vocabulary and UX requirements
- [Source: `_bmad-output/implementation-artifacts/8-1-complete-evidence-based-hdr-state-mapping.md` — Previous story intelligence] — Projection patterns and lessons learned
- [Source: `src/Lumiere.App.Core/MainPanelProjection.cs` — Current projection] — Existing trust mapping to extend
- [Source: `src/Lumiere.Overlay/OverlayState.cs` — Overlay state model] — Factory methods to parameterize

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

- Implemented alert message projection in `MainPanelProjection` with `AlertMessage` and `HasAlert` properties, derived from `PreviewReadinessState` + `hdrAlertsEnabled` + `outputResult`
- Added `MapAlertMessage()` and `MapTrayAlertMessage()` private methods following the existing `MapTrust()` pattern
- Added WinUI `InfoBar` to `MainWindow.xaml` between capture area and trust status bar, with `IsClosable=true` and severity mapped from trust severity
- Added `ApplyHdrAlert()` method in `MainWindow.xaml.cs` to drive InfoBar visibility and content
- Added `TrayAlertMessage` to `TrayMenuProjection` and `TrayMenuSnapshot` for tray menu alert hint display
- Updated `TrayMenuWindow.xaml` with `HdrAlertLabel` TextBlock and `ApplySnapshot()` to show/hide alert
- Updated `CreateOverlayState()` to mute overlay messages when alerts disabled, include actionable hints when enabled
- Removed redundant `OptionalHdrAlertChromeEnabled` from `SettingsPanelProjection`
- Updated `OnSettingsHdrAlertsButtonClick` to immediately reflect setting changes in main panel, tray, and settings
- Added 10 new test methods across `MainPanelProjectionTests` and `TrayMenuProjectionTests`
- Removed 2 stale `OptionalHdrAlertChromeEnabled` assertions from `SettingsPanelProjectionTests`
- All alert messages avoid HDR-preserving claims per NFR22
- Validation: restore pass, build pass (0 errors), 305/307 tests pass (2 pre-existing `DefaultSettingsProviderTests` failures), 88/88 overlay tests pass, format verification pass

### File List

- `src/Lumiere.App.Core/MainPanelProjection.cs` — Added `AlertMessage`, `HasAlert` properties; `hdrAlertsEnabled` parameter; `MapAlertMessage()` method
- `src/Lumiere.App.Core/TrayMenuProjection.cs` — Added `TrayAlertMessage` property; `hdrAlertsEnabled` parameter; `MapTrayAlertMessage()` method
- `src/Lumiere.App.Core/AppShellProjection.cs` — Propagated `hdrAlertsEnabled` parameter
- `src/Lumiere.App.Core/SettingsPanelProjection.cs` — Pass `hdrAlertsEnabled` to `MainPanelProjection.Project()`; removed `OptionalHdrAlertChromeEnabled`
- `src/Lumiere.App/MainWindow.xaml` — Added `HdrAlertInfoBar` InfoBar; restructured grid rows
- `src/Lumiere.App/MainWindow.xaml.cs` — `UpdateMainPanelProjection()` passes `hdrAlertsEnabled`; added `ApplyHdrAlert()`; `CreateOverlayState()` mutes/enables overlay messages; `OnSettingsHdrAlertsButtonClick` updates main+tray; `CreateTrayMenuSnapshot` includes `TrayAlertMessage`
- `src/Lumiere.App/TrayMenuWindow.xaml` — Added `HdrAlertLabel` TextBlock
- `src/Lumiere.App/TrayMenuWindow.xaml.cs` — `ApplySnapshot()` shows/hides alert label
- `src/Lumiere.Infrastructure/Interop/TrayMenuSnapshot.cs` — Added `TrayAlertMessage` field
- `tests/Lumiere.Graphics.Tests/App/MainPanelProjectionTests.cs` — Added 7 alert message test methods
- `tests/Lumiere.Graphics.Tests/App/TrayMenuProjectionTests.cs` — Added 3 tray alert test methods
- `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs` — Removed 2 `OptionalHdrAlertChromeEnabled` assertions

### Change Log

- 2026-06-03: Implemented actionable HDR alerts (Story 8.2) — alert projection layer, MainWindow InfoBar, tray menu alert hint, overlay message gating, settings pass-through, lifecycle handling, unit tests

### Review Findings

- [x] [Review][Patch] Overlay messages use Chinese instead of spec-required English — `MainWindow.xaml.cs:1932-1938` overlay Degraded/Unsupported/Failed messages are in Chinese. User confirmed: change to English to match spec and rest of UI.
- [x] [Review][Patch] InfoBar dismiss overridden by every state update — `MainWindow.xaml.cs:1580` `ApplyHdrAlert()` unconditionally sets `IsOpen = true` when `HasAlert` is true. User confirmed: add per-session dismissed flag so dismiss persists until session ends or state resolves to Ready.
- [x] [Review][Patch] PreviewFailed overlay prepends new hardcoded message instead of keeping existing — `MainWindow.xaml.cs:1937-1938` prepends "预览失败，捕获可能无法产生高质量输出。" before `sessionState.UserFacingReason`. Spec says PreviewFailed should "Keep existing failure message (this is a terminal teardown state)". The existing `message` variable should be passed directly without modification.
- [x] [Review][Patch] Missing test coverage for tray alert Unsupported/Failed states — `TrayMenuProjectionTests.cs` tests only cover Degraded+alerts-enabled, Degraded+alerts-disabled, and Ready+alerts-enabled. No tests for `PreviewReadinessState.Unsupported` or `PreviewReadinessState.Failed` with `hdrAlertsEnabled: true`.
- [x] [Review][Defer] Multi-monitor probe hardcodes output index 0 — `HdrDisplayCapability.cs:92` `adapter.EnumOutputs(0, out output)` always queries first output. On multi-monitor setups with different HDR states per display, probe result may not match the capture target display. Deferred: known limitation, future enhancement.
- [x] [Review][Defer] Duplicated alert-message mapping logic — `MainPanelProjection.MapAlertMessage` and `TrayMenuProjection.MapTrayAlertMessage` share identical guard logic and switch structure with different message strings. Deferred: code smell, not urgent.
- [x] [Review][Defer] Tray alert label always uses Warning color — `TrayMenuWindow.xaml:46` hardcodes `Foreground="{StaticResource WarningBrush}"` for all alert severities. Unsupported/Failed show yellow warning in tray but red error in main panel InfoBar. Deferred: minor UI polish.
- [x] [Review][Defer] Fixed window height may clip InfoBar content — `MainPanelHeightDips = 310` with Auto-sized InfoBar row. Long multi-line messages could reduce capture card area. Deferred: minor layout concern.
- [x] [Review][Defer] No test coverage for Probe() COM paths — `HdrDisplayCapabilityTests.cs` tests only constructed records and `SwapChainColorSpaceConfigurator.Configure()`. Actual `Probe(IDXGIFactory2)` and `Probe(IDXGIDevice)` COM interop paths are untested. Deferred: requires hardware, documented as Mac edit/Windows CI-pass.
- [x] [Review][Defer] swapChain3 double-disposed on error path — `SwapChainManager.cs` catch block and finally block both dispose swapChain3. Deferred: pre-existing issue, not caused by this change.
- [x] [Review][Defer] Probe(IDXGIDevice) overload is dead code — `HdrDisplayCapability.cs:23-48` has zero callers and zero test coverage. Deferred: not caused by this change.
- [x] [Review][Defer] SwapChainManager probes HDR capability without caching — `SwapChainManager.cs:55` allocates COM objects on each Configure call. Deferred: performance optimization, not a bug.

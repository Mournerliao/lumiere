---
title: 'Enhance HDR Display Detection'
type: 'bugfix'
created: '2026-06-03'
status: 'done'
context:
  - 'd:\UGit\lumiere\_bmad-output\project-context.md'
  - 'd:\UGit\lumiere\_bmad-output\planning-artifacts\sprint-change-proposal-2026-06-03.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** HDR status detection relies solely on `IDXGISwapChain3::CheckColorSpaceSupport(RgbFullG10NoneP709)`. On many Windows systems, even when HDR is disabled in Display settings, the swap chain still reports scRGB as supported (hardware capability + Windows tone mapping). The app shows "HDR Ready" when HDR is actually off, making the alert system (Story 8.2) unable to trigger for the most common degraded scenario.

**Approach:** Add DXGI output description query via `IDXGIOutput6::GetDesc1()` to detect actual display HDR capability before swap chain color space probing. When the display probe indicates HDR is not active but `CheckColorSpaceSupport` reports present, prefer the display probe result and mark readiness as `Degraded`.

## Boundaries & Constraints

**Always:**
- FP16/scRGB format path must remain unchanged — only the readiness assessment changes
- Alert messages must not claim HDR preservation (NFR22)
- Must fall back gracefully if DXGI output enumeration fails (headless, remote desktop)
- `PreviewReadinessStatus` and `CaptureSessionState` are the only state vocabularies — no new status enums
- Platform APIs stay in `Lumiere.Graphics` module boundary

**Ask First:**
- If `IDXGIOutput6` is not available on the target system (older Windows), how to handle? Currently: fall back to existing `CheckColorSpaceSupport` behavior with `Degraded` as safe default.

**Never:**
- Do not add `BitmapImage`, `SoftwareBitmap`, GDI, WIC, or CPU readback for HDR detection
- Do not create parallel status enums or ad hoc status strings
- Do not modify `CaptureSessionState`, `PreviewReadinessState`, or `PreviewReadinessStatus` record structure

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| HDR enabled in Windows | `GetDesc1().ColorSpace` = HDR10/P2020 | `CheckColorSpaceSupport` result used as-is (Initializing → Ready) | N/A |
| HDR disabled, hardware supports HDR | `GetDesc1().ColorSpace` = sRGB/P709, `CheckColorSpaceSupport` = Present | Override to `Degraded("Enable HDR in Windows Display settings for best capture quality")` | N/A |
| HDR disabled, hardware no HDR | `GetDesc1()` fails or returns sRGB | `Degraded` (same as current behavior) | N/A |
| DXGI output enumeration fails | Exception from `EnumOutputs` or `QueryInterface<IDXGIOutput6>` | Fall back to existing `CheckColorSpaceSupport` behavior, log warning | Graceful degradation |
| Remote desktop / headless | No physical output | Fall back to existing behavior with `Degraded` | Log warning |

</frozen-after-approval>

## Code Map

- `src/Lumiere.Graphics/Hdr/HdrDisplayCapability.cs` -- NEW: probe type that queries DXGI output description for HDR capability
- `src/Lumiere.Graphics/Presentation/SwapChainColorSpaceConfigurator.cs` -- integrate display probe result into color space readiness decision
- `src/Lumiere.Graphics/Presentation/SwapChainManager.cs` -- pass `IDXGIDevice` to configurator for output enumeration
- `src/Lumiere.Graphics/Hdr/PreviewReadinessStatus.cs` -- no structural changes, used as-is
- `src/Lumiere.Graphics/Hdr/PreviewReadinessStage.cs` -- no changes, `Presentation` stage used
- `tests/Lumiere.Graphics.Tests/Hdr/HdrDisplayCapabilityTests.cs` -- NEW: unit tests for capability mapping logic

## Tasks & Acceptance

**Execution:**
- [x] `src/Lumiere.Graphics/Hdr/HdrDisplayCapability.cs` -- Create new sealed record with static `Probe(IDXGIDevice)` method that enumerates adapters/outputs, queries `IDXGIOutput6.GetDesc1()`, and returns a typed result indicating whether the display supports HDR
- [x] `src/Lumiere.Graphics/Presentation/SwapChainColorSpaceConfigurator.cs` -- Add overload `Configure(ISwapChainColorSpaceController, ColorSpaceType, HdrDisplayCapability)` that combines display probe result with `CheckColorSpaceSupport`; when display probe says no HDR but swap chain says supported, return `Degraded`
- [x] `src/Lumiere.Graphics/Presentation/SwapChainManager.cs` -- Pass `deviceResources.DxgiDevice` to `SwapChainColorSpaceConfigurator.Configure` so it can enumerate outputs
- [x] `tests/Lumiere.Graphics.Tests/Hdr/HdrDisplayCapabilityTests.cs` -- Add tests for: HDR enabled → Initializing, HDR disabled + hardware supports → Degraded, probe failure → fallback behavior
- [x] Run full validation: restore, build, tests, format verification

**Acceptance Criteria:**
- Given HDR is disabled in Windows Display settings, when user captures, then trust label shows "Enable HDR" (not "HDR Ready")
- Given HDR is enabled in Windows Display settings, when user captures, then trust label shows "HDR Ready" after first frame
- Given DXGI output enumeration fails, when capture starts, then app falls back to existing `CheckColorSpaceSupport` behavior without crash
- Given HDR disabled state is detected, when alerts are enabled, then InfoBar shows actionable hint

## Spec Change Log

- **Patch 1 (Med):** TrayMenuProjection.MapTrayAlertMessage did not check OutputResult, causing tray alert to show even after successful output. Fixed by adding `outputResult is not null` check, consistent with MainPanelProjection.MapAlertMessage.
- **Patch 2 (Low):** HdrDisplayCapabilityTests missing InlineData for `YcbcrStudioG2084TopLeftP2020`. Added test data entry.
- **Defer (Med):** Multi-monitor: Probe only checks output index 0. In multi-monitor setups, HDR state may differ per display. Requires display-target-aware probing in a future story.
- **Defer (Med):** Presentation evidence is set once at swap chain creation and never refreshed. If user toggles HDR mid-session, the stale evidence persists until next capture. Requires evidence refresh mechanism.
- **Defer (Med):** InfoBar dismiss (IsClosable=true) is session-scoped but state changes re-open it. User expectation vs behavior may need UX discussion.

## Design Notes

**Why `IDXGIOutput6.GetDesc1()` over `Windows.Graphics.Display.AdvancedColorInfo`:**
- `IDXGIOutput6` is available through Vortice.DXGI bindings already in the project
- `AdvancedColorInfo` requires WinRT interop and `DisplayInformation` which has different threading requirements
- `GetDesc1()` returns `DXGI_OUTPUT_DESC1` with `ColorSpace` field that directly indicates HDR capability
- The DXGI approach stays within the existing `Lumiere.Graphics` module boundary

**Fallback strategy:**
- If `IDXGIOutput6` query fails, the `HdrDisplayCapability` result should indicate `Unknown` capability
- `SwapChainColorSpaceConfigurator` treats `Unknown` as "trust the swap chain check" (existing behavior)
- This ensures the change is strictly additive — no existing working scenario breaks

## Suggested Review Order

**HDR Display Probe**

- New probe type that queries DXGI output description for HDR capability
  [`HdrDisplayCapability.cs:23`](../../src/Lumiere.Graphics/Hdr/HdrDisplayCapability.cs#L23)

- HDR color space detection logic — maps DXGI ColorSpaceType to active/inactive
  [`HdrDisplayCapability.cs:84`](../../src/Lumiere.Graphics/Hdr/HdrDisplayCapability.cs#L84)

**Swap Chain Integration**

- Display probe integrated before color space configuration
  [`SwapChainManager.cs:55`](../../src/Lumiere.Graphics/Presentation/SwapChainManager.cs#L55)

- Inactive display overrides swap chain support to return Degraded
  [`SwapChainColorSpaceConfigurator.cs:34`](../../src/Lumiere.Graphics/Presentation/SwapChainColorSpaceConfigurator.cs#L34)

**Alert Projection (Story 8.2)**

- Alert message derived from readiness state + hdrAlertsEnabled
  [`MainPanelProjection.cs:62`](../../src/Lumiere.App.Core/MainPanelProjection.cs#L62)

- Tray alert message with OutputResult suppression
  [`TrayMenuProjection.cs:70`](../../src/Lumiere.App.Core/TrayMenuProjection.cs#L70)

**UI Integration**

- InfoBar added between capture area and trust status bar
  [`MainWindow.xaml:125`](../../src/Lumiere.App/MainWindow.xaml#L125)

- ApplyHdrAlert drives InfoBar visibility and severity
  [`MainWindow.xaml.cs:1567`](../../src/Lumiere.App/MainWindow.xaml.cs#L1567)

- Overlay message gating based on hdrAlertsEnabled
  [`MainWindow.xaml.cs:1920`](../../src/Lumiere.App/MainWindow.xaml.cs#L1920)

- Tray menu alert label show/hide
  [`TrayMenuWindow.xaml.cs:104`](../../src/Lumiere.App/TrayMenuWindow.xaml.cs#L104)

**Tests**

- HDR display capability unit tests
  [`HdrDisplayCapabilityTests.cs:10`](../../tests/Lumiere.Graphics.Tests/Hdr/HdrDisplayCapabilityTests.cs#L10)

- Alert message projection tests
  [`MainPanelProjectionTests.cs:224`](../../tests/Lumiere.Graphics.Tests/App/MainPanelProjectionTests.cs#L224)

- Tray alert message tests
  [`TrayMenuProjectionTests.cs:84`](../../tests/Lumiere.Graphics.Tests/App/TrayMenuProjectionTests.cs#L84)

## Verification

**Commands:**
- `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false` -- expected: success
- `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false` -- expected: 0 errors
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false` -- expected: all new tests pass, no regressions
- `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal` -- expected: pass

**Manual checks:**
- HDR off: capture → trust label shows "Enable HDR", InfoBar appears (if alerts enabled)
- HDR on: capture → trust label shows "HDR Ready" after first frame

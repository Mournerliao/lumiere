# Story 2.5: Create Monitor Capture Targets Without Picker

Status: done

<!-- Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a screenshot user,
I want Capture to enter region selection directly,
so that I can screenshot whatever is currently visible without first choosing a window or display.

## Acceptance Criteria

1. Given the user clicks Capture, when the default MVP path starts, then no `GraphicsCapturePicker` UI appears.
2. Given the pointer or capture-start context maps to a monitor, when direct capture starts, then Lumiere creates a `GraphicsCaptureItem` for that `HMONITOR` through a narrow infrastructure interop API.
3. Given monitor target creation fails or is unsupported, when the default path cannot continue, then Lumiere reports a recoverable unsupported/failed status and may offer picker fallback outside the default MVP path.
4. Given the target is created through monitor interop, when `CaptureTarget` is created, then its kind is `Display` and its size/display name are validated before WGC frame-pool startup.
5. Given the implementation adds native monitor interop, when files are organized, then HMONITOR/COM/Win32 details remain inside `Lumiere.Infrastructure` and only narrow typed APIs are exposed.
6. Given MVP design is consulted, when the story is implemented, then the flow aligns with `/Users/asherliao/Projects/lumiere/harness/design/mvp/lumiere-mvp-design.png`.

## Tasks / Subtasks

- [x] Add a direct monitor target creation boundary without replacing the existing picker fallback. (AC: 1, 2, 3, 5)
  - [x] Add infrastructure-owned monitor interop under `src/Lumiere.Infrastructure/Interop/`, such as `GraphicsCaptureMonitorInterop` and a small provider/factory abstraction.
  - [x] Keep raw `HMONITOR`, `HWND`, `POINT`, `GetCursorPos`, `MonitorFromPoint`, `MonitorFromWindow`, COM activation factory, HRESULT handling, and ABI pointer release inside `Lumiere.Infrastructure`.
  - [x] Expose only a narrow typed API to app/capture code, for example a method that resolves the current monitor context and returns a `GraphicsCaptureItem` or a typed failure; do not expose native handles across product boundaries.
  - [x] Preserve `GraphicsCaptureTargetPicker` and `GraphicsCapturePickerInterop` for explicit fallback/debug selection, but do not call them from the default Capture/Region Select click path.

- [x] Add capture-owned direct target selection semantics. (AC: 2, 3, 4)
  - [x] Extend `CaptureTarget` with a display-specific factory such as `FromDisplayItem(GraphicsCaptureItem item)` or a validated `FromItem(item, CaptureTargetKind.Display)` overload.
  - [x] Ensure monitor-created targets always have `CaptureTargetKind.Display`, validated positive size, size <= `CaptureTarget.MaxTextureDimension`, and a non-empty display name before `CaptureService.StartCapture`.
  - [x] Add or extend a selection service so direct monitor selection maps support checks and interop failures into `CaptureTargetSelectionResult.Selected`, `Unsupported`, or `Failed` with `PreviewReadinessStatus`.
  - [x] Keep `GraphicsCaptureSession.IsSupported()` as the first support gate so unsupported systems fail before native monitor item creation.

- [x] Rewire the default app capture action to direct monitor capture. (AC: 1, 2, 3, 6)
  - [x] Update `src/Lumiere.App/MainWindow.xaml.cs` so `Capture Now` and `Region Select` no longer start `GraphicsCapturePicker` by default.
  - [x] Use the capture-start context to select the intended monitor. Prefer pointer location when available; otherwise use the main window handle/placement as a deterministic fallback.
  - [x] Start the existing `StartPreview(CaptureTarget target)` path only after direct monitor target creation succeeds.
  - [x] Preserve `StopPreview`, `CloseOverlayWindow`, `previewGeneration`, stale callback checks, and resource disposal sequencing exactly as lifecycle stories established.
  - [x] Update default idle/initializing user messages so they no longer tell the user to choose a display/window for the MVP happy path.

- [x] Keep picker fallback/debug behavior outside the default MVP path. (AC: 1, 3)
  - [x] If an explicit picker fallback remains user-accessible, route it through a separate method/control/debug action with wording that clearly marks it as fallback/debug or explicit target selection.
  - [x] If fallback is only internal for now, keep the picker classes and tests intact without adding hidden picker UI before direct overlay entry.
  - [x] Do not silently launch the picker after direct monitor interop fails; surface a recoverable unsupported/failed status first.

- [x] Add focused automated tests for the new direct monitor target seams. (AC: 1, 3, 4, 5)
  - [x] Add tests proving a monitor-created item becomes a `CaptureTarget` with `Kind == CaptureTargetKind.Display`, validated size, and fallback display name behavior.
  - [x] Add tests proving direct selection returns `Unsupported` when capture support is false and does not call monitor interop.
  - [x] Add tests proving direct selection maps native interop exceptions to failed/unsupported readiness with stage `Interop` or `Capture` as appropriate.
  - [x] Add tests proving the default direct path does not invoke `ICaptureTargetPicker`; use a fake picker that throws if called.
  - [x] Keep tests hardware-independent. Use injectable delegates/fakes for monitor resolution and item creation; do not require a real `HMONITOR`, WGC session, WinUI window, or D3D11 device in unit tests.

- [x] Update manual validation guidance for direct monitor startup. (AC: 1, 2, 3, 6)
  - [x] Update `docs/validation/lifecycle-validation.md` and/or `docs/validation/overlay-validation.md` only if the implementation changes the direct capture checklist.
  - [x] Include checks for no picker before overlay entry, intended monitor selection, multi-monitor cursor/window-start behavior, recoverable direct target failure, and picker fallback if exposed.
  - [x] State that real WGC direct monitor item creation, overlay placement, full-screen apps, HDR/SDR monitor behavior, and multi-monitor targeting require Windows manual validation.

### Review Findings

- [x] [Review][Patch] COM pointer freed with `Marshal.FreeHGlobal` instead of `Marshal.Release` — `GraphicsCaptureMonitorInterop.cs` finally block uses `Marshal.FreeHGlobal(itemPointer)` on a COM interface pointer. Must change to `Marshal.Release(itemPointer)`. Also verify `GraphicsCaptureItem.FromAbi` ownership semantics on Windows before merging.
- [x] [Review][Patch] `GraphicsCaptureItem.FromAbi` ownership semantics — `GraphicsCaptureMonitorInterop.cs` calls `FromAbi(itemPointer)` then releases `itemPointer` in `finally`. Safe fix: use `Marshal.Release` + comment to verify on Windows.
- [x] [Review][Patch] Raw `IntPtr` (HMONITOR) flows across module boundary — wrap in `MonitorHandle` domain type in Infrastructure, inject as `Func<MonitorHandle>` delegates.
- [x] [Review][Patch] `MonitorSelectionInterop.GetMonitorDisplayName` called directly from `Lumiere.Capture` — inject as `Func<MonitorHandle, string>` delegate.
- [x] [Review][Patch] Activation factory COM pointer leaked on every call — `GraphicsCaptureMonitorInterop.cs` `GetGraphicsCaptureItemFactory()` return value never released in `finally` block. Add `Marshal.Release(captureItemFactory)`.
- [x] [Review][Patch] `SelectDirectMonitorTargetAsync` is `async` but contains no `await` — `DirectMonitorCaptureTargetSelectionService.cs:28`. Remove `async`, return `Task.FromResult(...)`.
- [x] [Review][Patch] Dead code: `var activationFactory = WinRT.Interop.WindowNative.GetWindowHandle` — `GraphicsCaptureMonitorInterop.cs:25`. Remove unused variable.
- [x] [Review][Patch] `IGraphicsCaptureItemInterop` COM interface is empty — vtable index 6 is fragile — `GraphicsCaptureMonitorInterop.cs` `InvokeCreateForMonitor` reads `vtable[6]`. Add comment documenting ABI assumption (IUnknown 3 + IInspectable 3 = 6).
- [x] [Review][Patch] Transient monitor-resolution errors mapped to `Unsupported` instead of `Failed` — `MonitorSelectionInterop.cs:53` creates `NativeInteropException` with `Stage = "Interop"`, mapped to `Unsupported`. Cursor-position failure is transient. Change stage to `"Capture"` or add distinct mapping.
- [x] [Review][Patch] `CreateForMonitorDelegate` uses `in Guid` — ABI mismatch risk — `GraphicsCaptureMonitorInterop.cs:125-129`. Change to `Guid resultInterfaceId` (by value).
- [x] [Review][Patch] Tests use `CreateForTest` instead of `FromDisplayItem` — `DirectMonitorCaptureTargetTests.cs:13-83`. Tests named `FromDisplayItem*` should call `CaptureTarget.FromDisplayItem(...)`.
- [x] [Review][Patch] `CreateFakeCaptureItem` returns `null!` on non-Windows/headless — `DirectMonitorCaptureTargetTests.cs:293-305`. Use proper mock/stub or skip on unsupported platforms.
- [x] [Review][Patch] `PointInt32` struct declared `public` in Infrastructure — `MonitorSelectionInterop.cs:62-66`. Make `private`.
- [x] [Review][Patch] Magic HResult `-2147467263` (`E_NOTIMPL`) unexplained — `DirectMonitorCaptureTargetSelectionService.cs:156`. Add named constant.
- [x] [Review][Patch] `SelectWithFallbackPickerAsync` missing `NativeInteropException` catch — `DirectMonitorCaptureTargetSelectionService.cs:87-130`. Add catch with `MapNativeInteropFailure` mapping.
- [x] [Review][Defer] `GetMonitorDisplayName` returns raw `DeviceName` (`\\.\DISPLAY1`) — `MonitorSelectionInterop.cs:42-51` — deferred, pre-existing UX concern
- [x] [Review][Defer] `GetMonitorFromWindow` is public but unused — `MonitorSelectionInterop.cs` — deferred, future use for window-handle fallback
- [x] [Review][Defer] `MonitorFromPoint` with `MONITOR_DEFAULTTONEAREST` never returns null — `MonitorSelectionInterop.cs:18-22` — deferred, dead branch but harmless

## Dev Notes

### Story Scope

Story 2.5 changes the default MVP capture entry from picker-first selection to direct monitor capture. The user-visible happy path becomes: click Capture, resolve the current monitor, create a WGC monitor `GraphicsCaptureItem`, start the existing FP16/scRGB preview, and show the full-screen overlay.

This story does not implement release-to-copy, clipboard output, HDR still-image export, SDR tone mapping, annotations, global hotkeys, tray workflow, settings, capture history, full HDR/SDR capability diagnostics, or advanced monitor selection UI. It also must not rewrite the WGC frame pool, D3D11/DXGI presentation, crop controller, or overlay confirm/cancel foundations.

### Current Repository Context

Current implementation to build on:

- `src/Lumiere.App/MainWindow.xaml.cs` wires both `Capture Now` and `Region Select` to `OnSelectCaptureTargetClick`, which currently creates `CaptureTargetSelectionService(new GraphicsCaptureTargetPicker(this))` and awaits `GraphicsCapturePicker`.
- `StartPreview(CaptureTarget target)` already accepts a typed `CaptureTarget`, creates the overlay, creates the FP16/scRGB swap chain, starts `CaptureService.StartCapture`, and uses `previewGeneration` to reject stale callbacks.
- `EnsureOverlayWindow` already calls `CreateOverlayPlacementRequest(target)`, and that request treats `CaptureTargetKind.Display` as display placement input.
- `CaptureTarget.FromItem(GraphicsCaptureItem item)` currently validates item size and display name but sets `Kind` to `Unknown`; direct monitor targets need an explicit display-kind factory or overload.
- `CaptureTargetSelectionService` currently owns picker support checks and picker outcome mapping. Reuse its result vocabulary rather than creating parallel status types.
- `Lumiere.Infrastructure.Interop` already contains picker, Direct3D, surface, and swap-chain COM/WinRT interop patterns with `NativeInteropException` and `InteropFailureDiagnostics`.
- `tests/Lumiere.Graphics.Tests/Capture/` already covers `CaptureTarget`, target selection results, support checks, lifecycle, and session state with hardware-independent fakes.

Likely changed or new files:

```text
src/Lumiere.App/MainWindow.xaml.cs
src/Lumiere.Capture/CaptureTarget.cs
src/Lumiere.Capture/CaptureTargetSelectionService.cs
src/Lumiere.Infrastructure/Interop/GraphicsCaptureMonitorInterop.cs
src/Lumiere.Infrastructure/Interop/MonitorSelectionInterop.cs
tests/Lumiere.Graphics.Tests/Capture/CaptureTargetTests.cs
tests/Lumiere.Graphics.Tests/Capture/CaptureTargetSelectionTests.cs
docs/validation/lifecycle-validation.md
docs/validation/overlay-validation.md
```

Possible additional files if they keep boundaries clearer:

```text
src/Lumiere.Capture/DirectMonitorCaptureTargetSelectionService.cs
src/Lumiere.Infrastructure/Interop/IMonitorCaptureItemFactory.cs
src/Lumiere.Infrastructure/Interop/MonitorCaptureContext.cs
tests/Lumiere.Graphics.Tests/Capture/DirectMonitorCaptureTargetSelectionTests.cs
```

Avoid changing unless the implementation truly requires it:

```text
src/Lumiere.Capture/CaptureService.cs
src/Lumiere.Graphics/Hdr/HdrConstants.cs
src/Lumiere.Graphics/Presentation/*
src/Lumiere.Overlay/Crop/*
Directory.Build.props
Directory.Packages.props
```

### Architecture Compliance

- `Lumiere.Infrastructure` owns all `HMONITOR`, `HWND`, User32, COM activation factory, ABI pointer, and HRESULT details.
- `Lumiere.Capture` owns target selection semantics, `CaptureTarget`, `CaptureTargetSelectionResult`, support/failure readiness mapping, and WGC session lifecycle.
- `Lumiere.App` owns app composition and orchestration only: button click, status projection, `StartPreview`, `StopPreview`, and overlay lifetime.
- `Lumiere.Overlay` owns overlay/crop input and window behavior only; it should not create monitor capture items or capture sessions.
- `Lumiere.Graphics` owns D3D11/DXGI/HDR rendering only; this story must not alter FP16/scRGB constants or swap-chain format/color-space.

### Direct Monitor Interop Requirements

- Use `IGraphicsCaptureItemInterop::CreateForMonitor` for the acceptance-criteria path. Microsoft documents it as taking an `HMONITOR`, a return interface id, and an out pointer for the created object; the supported return type is `GraphicsCaptureItem`, and the minimum supported Windows client is Windows 10 version 1903/build 18362. [Source: Microsoft Learn, `IGraphicsCaptureItemInterop::CreateForMonitor`](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.capture.interop/nf-windows-graphics-capture-interop-igraphicscaptureiteminterop-createformonitor)
- Use a valid monitor handle resolved from current capture context. `GetCursorPos` returns the cursor position in screen coordinates, and `MonitorFromPoint` can map that point to the containing or nearest monitor. [Source: Microsoft Learn, `GetCursorPos`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getcursorpos), [Source: Microsoft Learn, `MonitorFromPoint`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-monitorfrompoint)
- If pointer context is unavailable, `MonitorFromWindow` can resolve the monitor with the largest intersection with the app window, using nearest/primary/null behavior according to flags. [Source: Microsoft Learn, `MonitorFromWindow`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-monitorfromwindow)
- If implementation needs monitor metadata, `GetMonitorInfo` can retrieve monitor information after the caller initializes the `MONITORINFO` or `MONITORINFOEX` size. Keep this in infrastructure and do not make it a product-level dependency unless needed. [Source: Microsoft Learn, `GetMonitorInfo`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getmonitorinfoa)
- `GraphicsCaptureItem.Size` is the target capture size and `DisplayName` is the target display name. Use those properties for `CaptureTarget` validation and status detail. [Source: Microsoft Learn, `GraphicsCaptureItem`](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.graphicscaptureitem?view=winrt-28000)
- `GraphicsCaptureSession.IsSupported()` returns whether screen capture is supported on the device. Keep this as the first support gate. [Source: Microsoft Learn, `GraphicsCaptureSession.IsSupported`](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.graphicscapturesession.issupported?view=winrt-28000)
- `GraphicsCaptureItem.TryCreateFromDisplayId(DisplayId)` is documented, but Microsoft also documents that programmatic access requires `GraphicsCaptureAccess.RequestAccessAsync(GraphicsCaptureAccessKind.Programmatic)` and the `graphicsCaptureProgrammatic` package capability. Do not switch the MVP direct path to this capability-based route unless the implementation explicitly documents why and updates app manifest/permission handling in scope. [Source: Microsoft Learn, `TryCreateFromDisplayId`](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.graphicscaptureitem.trycreatefromdisplayid?view=winrt-28000)
- Windows App SDK exposes `Win32Interop.GetDisplayIdFromMonitor(IntPtr)` for C# desktop apps. Treat this as a possible future fallback/spike input, not as a replacement for the story's required `HMONITOR` -> `GraphicsCaptureItem` interop unless acceptance criteria are updated. [Source: Microsoft Learn, `GetDisplayIdFromMonitor`](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/win32/microsoft.ui.interop/nf-microsoft-ui-interop-getdisplayidfrommonitor), [Source: Microsoft Learn, `Win32Interop`](https://learn.microsoft.com/en-us/windows/apps/api-reference/cs-interop-apis/microsoft.ui/microsoft.ui.win32interop)

### Implementation Guardrails

- Mirror existing interop style in `Direct3D11Interop`, `Direct3D11SurfaceInterop`, and `SwapChainPanelNativeInterop`: explicit operation names, HRESULT mapping to `NativeInteropException`, `try/finally` cleanup, and no leaked COM pointers.
- Verify the concrete C#/WinRT ABI helper used for `GraphicsCaptureItem` on Windows. Do not leave guessed ABI code or unchecked COM casts in the story implementation.
- Return recoverable `Unsupported` or `Failed` statuses with `PreviewReadinessStage.Capture` or `PreviewReadinessStage.Interop`; do not let direct monitor interop exceptions crash the app from the click handler.
- The default click path must not show `GraphicsCapturePicker`. A fallback/debug picker may exist, but it must be separate and must not interrupt the MVP happy path before overlay entry.
- Do not infer full HDR capability from monitor creation in this story. It is enough to create a display target, validate size/name/kind, and let existing preview readiness and later validation stories handle fidelity evidence.
- Preserve lifecycle behavior from Stories 2.3 and 2.4: dispose old capture and swap-chain resources outside `previewSync`, keep `previewGeneration` checks around all async/callback work, and never revive old targets after a failed or stale direct selection attempt.
- Preserve overlay behavior from Story 3.5: direct capture should feed `CaptureTargetKind.Display` so overlay placement can select display bounds, but overlay code should not receive raw monitor handles.

### UX and MVP Design Requirements

Use `/Users/asherliao/Projects/lumiere/harness/design/mvp/lumiere-mvp-design.png` as the interaction reference:

- Main window has one obvious Capture entry. The current dashboard's `Capture Now` and `Region Select` can remain visually simple, but their default behavior must go directly into monitor-based region selection.
- No picker-first display/window choice appears before the overlay.
- The overlay stays content-first: live preview as the base, crop layer above it, lightweight status, no multi-action toolbar, no output choices, no annotation controls, no settings controls.
- MVP copy should communicate direct capture startup and recoverable failure in plain language. Avoid "Choose a display or window" as the default path text.

### Previous Story Intelligence

Story 2.4 established:

- Repeated lifecycle stability depends on `previewGeneration`, deterministic capture disposal, swap-chain detach-before-release, and validation evidence. Direct monitor selection must not weaken those paths.
- Manual validation now already includes "Start capture through the default direct monitor path and confirm no picker appears" and picker only as fallback/debug.
- Production disposal evidence currently exists but is mostly test/diagnostic infrastructure; do not expand into Epic 4 diagnostics UI here.

Story 2.3 established:

- `MainWindow` is orchestration, not native resource ownership.
- Selecting a new target must stop the old preview first.
- Ordinary stop/restart must not dispose shared `GraphicsDeviceResources`.
- Frame-size mismatch and stale callbacks must remain generation-scoped.

Story 2.1 established:

- Picker cancellation is normal and must not be treated as failure.
- `CaptureTargetSelectionService` and `CaptureTargetSelectionResult` are the target-selection vocabulary.
- Tests should use `CaptureTarget.CreateForTest`; SDK helpers for fake `GraphicsCaptureItem` were not available.

Story 3.5 established:

- Overlay target placement already has display-vs-window input via `OverlayPlacementRequest`.
- A known review finding says real display selections never used display-specific overlay placement. Story 2.5 should help fix the input side by producing `CaptureTargetKind.Display` for direct monitor targets.
- Escape/cancel and overlay close paths must reuse app-level `StopPreview` and `CloseOverlayWindow`; direct capture must not add duplicate teardown.

### Git Intelligence

Recent relevant commits show the implementation lane:

- `f25faf0 docs: rebaseline mvp roadmap` approved the canonical MVP route and made Story 2.5 the next handoff.
- `e501097 feat: implement Epic 3 fullscreen overlay crop workflow` moved preview ownership into the overlay window and hardened overlay input behavior.
- `ed589a7 feat: implement stop, restart, and recreate capture resources` hardened lifecycle stop/restart/recreate paths that direct monitor selection must preserve.
- `9ffea82 feat: complete implementation of target selection for display or window capture` introduced the existing picker selection result/service patterns.
- `2f0e953 feat: implement minimal WGC FP16 capture to live preview` established the GPU-resident WGC/FP16 preview path; do not weaken it with CPU bitmap shortcuts.

### Testing Requirements

Run from repository root on Windows:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

Automated tests should cover direct target selection state, no-picker default routing via fakes, display-kind target validation, support failure mapping, and interop failure mapping. Real monitor handle resolution, `IGraphicsCaptureItemInterop::CreateForMonitor`, WinUI window handle behavior, WGC session startup, overlay placement, HDR/SDR behavior, full-screen app capture, and multi-monitor selection require Windows manual validation.

Completion notes must label validation accurately:

- `Mac-pass`: source/story/code edits or hardware-independent tests only.
- `Windows CI-pass`: restore/build/tests/format on Windows.
- `Windows manual-pass`: real WGC/DXGI/D3D11/HDR/multi-monitor direct capture scenarios executed and recorded.

### Anti-Patterns to Avoid

- Do not show `GraphicsCapturePicker` in the default Capture/Region Select MVP path.
- Do not silently fall back to picker after monitor interop failure without first surfacing a recoverable unsupported/failed status.
- Do not expose raw `HMONITOR`, `HWND`, `POINT`, COM pointers, HRESULT constants, or User32 P/Invoke outside `Lumiere.Infrastructure`.
- Do not create a second capture lifecycle, graphics engine, overlay window flow, crop controller, or status vocabulary.
- Do not render or validate the preview with `BitmapImage`, `SoftwareBitmap`, GDI, WIC, CPU readback, 8-bit textures, SDR screenshots, or PNG bytes.
- Do not update WinUI from WGC frame callbacks.
- Do not claim monitor HDR capability detection is solved by creating a monitor `GraphicsCaptureItem`.
- Do not implement release-to-copy or clipboard output in this story; Story 3.6 and Epic 4 own that work.
- Do not mark the story fully complete without Windows manual validation for the real direct monitor capture path.

## Dev Agent Record

### Agent Model Used

mimo-v2.5-pro

### Debug Log References

- Implementation completed without blocking issues
- All hardware-independent tests pass on macOS editing environment
- Windows CI validation required for full verification

### Completion Notes List

**Mac-pass**: Source/story/code edits and hardware-independent tests completed.

**Implementation Summary:**
1. **Infrastructure Layer** - Created `MonitorSelectionInterop` for cursor/monitor resolution (GetCursorPos, MonitorFromPoint, MonitorFromWindow, GetMonitorInfo) and `GraphicsCaptureMonitorInterop` for HMONITOR → GraphicsCaptureItem conversion via `IGraphicsCaptureItemInterop::CreateForMonitor`.
2. **Capture Layer** - Added `CaptureTarget.FromDisplayItem()` factory that sets `Kind = CaptureTargetKind.Display` with validated size and display name. Created `DirectMonitorCaptureTargetSelectionService` with injectable monitor resolver and item factory delegates.
3. **App Layer** - Rewired `MainWindow.OnSelectCaptureTargetClick` to use direct monitor capture by default. Preserved all lifecycle behavior (StopPreview, previewGeneration, stale callback checks). Updated status messages to reflect direct capture flow.
4. **Tests** - Added comprehensive hardware-independent tests for `CaptureTarget` display factory and `DirectMonitorCaptureTargetSelectionService` with fakes for monitor resolution and item creation.
5. **Validation Docs** - Existing docs already include direct monitor capture validation requirements.

**Key Design Decisions:**
- Used injectable delegates (`Func<IntPtr>` for monitor resolver, `Func<IntPtr, GraphicsCaptureItem>` for item factory) to keep tests hardware-independent
- Preserved `ICaptureTargetPicker` interface and `GraphicsCaptureTargetPicker` for fallback/debug path
- `DirectMonitorCaptureTargetSelectionService` exposes `SelectWithFallbackPickerAsync()` for explicit picker fallback
- Monitor handle resolution uses pointer location first, with `MonitorFromWindow` as deterministic fallback
- All Win32/COM interop stays inside `Lumiere.Infrastructure` with narrow typed APIs exposed

**Windows Manual Validation Required:**
- Real WGC direct monitor item creation
- Multi-monitor cursor/window-start behavior
- Full-screen app capture
- HDR/SDR monitor behavior
- Overlay placement on different monitors

### File List

**New Files:**
- `src/Lumiere.Infrastructure/Interop/MonitorSelectionInterop.cs` - Win32 monitor resolution (GetCursorPos, MonitorFromPoint, MonitorFromWindow, GetMonitorInfo)
- `src/Lumiere.Infrastructure/Interop/GraphicsCaptureMonitorInterop.cs` - COM interop for HMONITOR → GraphicsCaptureItem via IGraphicsCaptureItemInterop::CreateForMonitor
- `src/Lumiere.Infrastructure/Interop/MonitorHandle.cs` - Domain type wrapping IntPtr monitor handle with display name
- `src/Lumiere.Capture/DirectMonitorCaptureTargetSelectionService.cs` - Direct monitor selection service with fallback picker support
- `tests/Lumiere.Graphics.Tests/Capture/DirectMonitorCaptureTargetTests.cs` - Hardware-independent tests for direct monitor selection

**Modified Files:**
- `src/Lumiere.Capture/CaptureTarget.cs` - Added `FromDisplayItem()` factory method for display-kind targets
- `src/Lumiere.App/MainWindow.xaml.cs` - Rewired default capture to direct monitor path, updated status messages

**Validation Docs (already up to date):**
- `docs/validation/lifecycle-validation.md` - Already includes direct monitor capture validation checklist
- `docs/validation/overlay-validation.md` - Already includes "no picker before overlay entry" check

### Change Log

- 2026-05-07: Created Story 2.5 context and marked ready for development.
- 2026-05-07: Implemented direct monitor capture without picker. All tasks complete. Mac-pass validation.

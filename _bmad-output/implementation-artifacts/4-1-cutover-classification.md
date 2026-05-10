# MVP Cutover Classification

Date: 2026-05-10
Story: 4.1 — Classify Existing Foundation for MVP Cutover
Purpose: Authoritative reference for all Epic 4+ stories. Before retaining, modifying, or removing existing code, check this classification.

---

## Classification Schema

**Retained:** The capability exists, works, and directly supports MVP requirements. No code changes needed for the MVP cutover. Future stories may extend but should not rewrite.

**Reworked:** The capability exists but has issues that conflict with the v0 MVP direction. Code changes are needed, and a specific Epic 4+ story owns the rework.

**Deferred:** The capability exists but is not needed for MVP. It may be kept in the codebase but should not be exposed in the default user path. A future epic owns it.

**Removed:** The capability conflicts with MVP direction and has no future value. It should be deleted or disabled.

**Hybrid classifications** (e.g., "Retained (X) / Reworked (Y)"): A capability may have parts in different categories. Each part is scoped in parentheses. Future stories must check which part they are modifying — the retained part is safe to reuse, the reworked part has a specific owner.

---

## Epic 1: HDR Preview Foundation

### Story 1.1: Native Windows App Foundation

**Classification: RETAINED**

| Capability | Status | Supporting Requirements |
|------------|--------|------------------------|
| `Lumiere.sln` solution structure | Retained | NFR25, NFR29 |
| .NET 10 / `net10.0-windows10.0.19041.0` targeting | Retained | NFR25 |
| x64 platform enforcement | Retained | NFR25 |
| WinUI 3 / Windows App SDK `1.8.260317003` | Retained | NFR25 |
| Central package management (`Directory.Packages.props`) | Retained | Project Context: Code Quality Rules |
| Module boundaries: App, Capture, Graphics, Infrastructure, Overlay, Settings | Retained | NFR29, Architecture: Component Boundaries |
| `Directory.Build.props` build configuration | Retained | NFR25 |

**Rationale:** The scaffold is the validated foundation. No conflicts with v0 MVP direction.

### Story 1.2: HDR Constants and Readiness Vocabulary

**Classification: RETAINED**

| Capability | Status | Supporting Requirements |
|------------|--------|------------------------|
| `HdrConstants` (FP16/scRGB constants) | Retained | NFR31, NFR6 |
| `PreviewReadinessStatus` / `PreviewReadinessState` | Retained | FR14, NFR10 |
| `PreviewReadinessStage` | Retained | FR14, NFR10 |
| HDR constants tests (`HdrConstantsTests.cs`, `PreviewReadinessStatusTests.cs`) | Retained | NFR31 |

**Rationale:** Single source of truth for HDR capability and trust states. All future capture, preview, output, and UI stories must reuse these models.

### Story 1.3: D3D11 Device and WinRT/DXGI Interop Bridge

**Classification: RETAINED**

| Capability | Status | Supporting Requirements |
|------------|--------|------------------------|
| `GraphicsDeviceProvider` / `GraphicsDeviceResources` | Retained | NFR26, NFR29 |
| `Direct3D11Interop` / `Direct3D11SurfaceInterop` | Retained | NFR26 |
| `SwapChainPanelNativeInterop` | Retained | NFR26 |
| `NativeInteropException` / `InteropFailureDiagnostics` | Retained | NFR30 |
| COM ownership patterns in Infrastructure | Retained | NFR26, Architecture: Infrastructure Boundaries |

**Rationale:** Native interop remains inside Infrastructure boundary. No conflicts with MVP direction.

### Story 1.4: FP16 scRGB Swap-Chain Preview

**Classification: RETAINED**

| Capability | Status | Supporting Requirements |
|------------|--------|------------------------|
| `SwapChainManager` | Retained | NFR6, NFR7, NFR12 |
| `GraphicsEngine` | Retained | NFR6, NFR7 |
| `SwapChainColorSpaceController` / `SwapChainColorSpaceConfigurator` | Retained | NFR6 |
| `SwapChainCreationOptions` / `SwapChainResources` | Retained | NFR6 |
| `SwapChainDisposalEvidence` / `SwapChainDisposalCoordinator` | Retained | NFR11, NFR12 |
| `PreviewFramePresenter` / `PreviewRenderResult` | Retained | NFR6, NFR7 |
| `CapturedFrameTexture` / `IPreviewFrameOutput` | Retained | NFR6 |
| `SwapChainFrameOutput` | Retained | NFR6 |
| `SetSwapChain(null)` teardown pattern | Retained | NFR12 |

**Rationale:** FP16/scRGB preview is the core HDR invariant. Teardown follows detach-before-release pattern.

### Story 1.5: Minimal WGC FP16 Capture to Live Preview

**Classification: RETAINED**

| Capability | Status | Supporting Requirements |
|------------|--------|------------------------|
| `CaptureService` | Retained | NFR6, NFR25 |
| `CaptureSessionResources` / `CaptureSessionOptions` | Retained | NFR6, NFR11 |
| FP16 frame to D3D11 texture path | Retained | NFR6, NFR7 |
| Frame pool lifecycle | Retained | NFR11 |

**Rationale:** Core capture path. No changes needed for MVP cutover.

---

## Epic 2: Direct Capture Lifecycle

### Story 2.1: Typed Capture Target Selection

**Classification: RETAINED**

| Capability | Status | Supporting Requirements |
|------------|--------|------------------------|
| `CaptureTargetSelectionResult` | Retained | FR6, FR8, FR15, NFR14 |
| `CaptureTargetSelectionService` | Retained | FR6, FR15 |
| `ICaptureTargetPicker` / `GraphicsCaptureTargetPicker` | Retained | FR15 (fallback/debug only) |
| `GraphicsCapturePickerInterop` | Retained | FR15 (fallback/debug only) |

**Rationale:** Typed result model supports explicit outcomes. Picker remains available as fallback/debug but is not the default path.

### Story 2.2: Explicit Capture Session State

**Classification: RETAINED**

| Capability | Status | Supporting Requirements |
|------------|--------|------------------------|
| `CaptureSessionState` | Retained | FR7, FR14, NFR10, NFR13 |
| `CaptureSessionStatus` | Retained | FR7, FR14, NFR10 |
| Generation-scoped callbacks | Retained | NFR13 |
| `CaptureStartResult` | Retained | FR6, FR8, NFR14 |

**Rationale:** Single shared capture lifecycle contract. All future UI surfaces must project from this model.

### Story 2.3: Stop, Restart, and Resource Recreation

**Classification: RETAINED**

| Capability | Status | Supporting Requirements |
|------------|--------|------------------------|
| `CaptureSessionDisposalEvidence` / `CaptureSessionDisposalCoordinator` | Retained | NFR11, NFR15 |
| `CaptureFrameSizeChange` / `CapturePreviewRecreationRequest` | Retained | NFR11 |
| Teardown ordering (frame handler → session → frame pool → preview detach → swap chain) | Retained | NFR11, NFR12 |
| Shared device preservation on stop/restart | Retained | NFR15 |

**Rationale:** Deterministic teardown is a core architectural requirement.

### Story 2.4: Lifecycle Validation Evidence

**Classification: RETAINED**

| Capability | Status | Supporting Requirements |
|------------|--------|------------------------|
| `CaptureLifecycleValidationRecord` / `CaptureLifecycleValidationSummary` | Retained | FR44, FR45, NFR5, NFR33 |
| `CaptureLifecycleAttemptKind` | Retained | FR45 |
| `CaptureResourceGrowthEvidence` | Retained | NFR5 |
| `docs/validation/lifecycle-validation.md` | Retained | NFR27, NFR33 |

**Rationale:** Validation evidence is historical foundation. Future stories reference and extend it.

### Story 2.5: Direct Monitor Capture Without Picker

**Classification: RETAINED**

| Capability | Status | Supporting Requirements |
|------------|--------|------------------------|
| `DirectMonitorCaptureTargetSelectionService` (in `CaptureTargetSelectionService.cs`) | Retained | FR15, FR46, NFR23 |
| `MonitorSelectionInterop` | Retained | FR15, NFR26 |
| `GraphicsCaptureMonitorInterop` | Retained | FR15, NFR26 |
| No-picker default path | Retained | UX-DR4, NFR23 |

**Rationale:** Direct monitor capture is the default MVP path. No conflicts.

---

## Epic 3: Region Overlay Release-to-Capture

### Story 3.1: Fullscreen Overlay Above HDR Preview

**Classification: RETAINED**

| Capability | Status | Supporting Requirements |
|------------|--------|------------------------|
| `OverlayWindow` (XAML + code-behind) | Retained | FR16, FR21, NFR3 |
| `SwapChainPanel` base layer | Retained | NFR6, NFR7 |
| `OverlayPresenterApplication` / `OverlayWindowPresenter` | Retained | NFR3, NFR26 |
| `OverlayPlacementRequest` | Retained | NFR3, NFR27 |
| `OverlayBoundary` | Retained | NFR29 |
| `OverlayPreviewLayout` | Retained | NFR3 |
| `WindowCaptureExclusionInterop` | Retained | NFR26 |
| `WindowZOrderInterop` / `WindowNativeMethods` | Retained | NFR26 |
| `OverlayWindowGeometrySnapshot` / `OverlayWindowGeometryDiagnostics` | Retained | NFR30 |

**Rationale:** Overlay is the foundation for region capture. No conflicts with MVP direction.

### Story 3.2: Crop Selection by Dragging

**Classification: RETAINED**

| Capability | Status | Supporting Requirements |
|------------|--------|------------------------|
| `CropController` | Retained | FR16, FR19, FR21, NFR2 |
| `CropGeometry` | Retained | FR16, NFR2 |
| `CropSelection` / `CropSelectionPhase` | Retained | FR16, FR21 |
| `CropPixelRect` | Retained | FR16 |
| `CropHitTestResult` / `CropHitTestKind` | Retained | FR21 |
| `CropAdjustmentHandle` | Retained | FR21 (see rework note below) |
| `CaptureFrameSize` | Retained | NFR2 |
| Pointer lifecycle (Pressed → Moved → Released → Canceled → CaptureLost) | Retained | NFR2, NFR20 |
| Minimum size enforcement | Retained | FR19, NFR2 |

**Rationale:** Crop creation is core MVP interaction. Works correctly.

### Story 3.3: Crop Adjustment and Recreation

**Classification: REWORKED**

| Capability | Status | Supporting Requirements | Rework Owner |
|------------|--------|------------------------|--------------|
| `CropAdjustmentHandle` hit-testing | Reworked | FR21, NFR2 | Story 4.6 |
| `Adjusting` phase in `CropSelectionPhase` | Reworked | FR21 | Story 4.6 |
| `replacementGestureSelection` logic | Reworked | FR16, FR19 | Story 4.6 |
| Edge/corner handle drag behavior | Reworked | NFR2 | Story 4.6 |
| `CropCommitResult.Adjusted` enum value | Reworked (dead code) | FR17, FR19 | Story 4.6 |

**Rework Reason:** Release-to-capture auto-confirms on pointer release, making adjustment handles dead code in the current flow. The entire `Adjusting` state, handle hit-testing, and replacement gesture logic is unreachable. A different entry mode (e.g., click on existing crop to enter adjustment) is needed if crop adjustment is ever desired.

**Product Reason:** The MVP interaction model is "drag + release = done." Adjustment requires a separate interaction mode that doesn't conflict with release-to-capture.

**Follow-up:** Story 4.6 (Fix Overlay UX Deviations) owns the decision to keep, rework, or remove adjustment handles.

### Story 3.4: Confirm and Cancel Overlay Paths

**Classification: RETAINED (foundation) / REWORKED (UX deviations)**

| Capability | Status | Supporting Requirements | Rework Owner |
|------------|--------|------------------------|--------------|
| `ConfirmedCaptureSelection` / `CanConfirm()` / `TryCreate()` | Retained | FR17, FR20, NFR20 | — |
| `CloseRequested` event (separate from `CaptureConfirmed`) | Retained | FR18, NFR20 | — |
| `CaptureConfirmed` event | Retained | FR17 | — |
| `isClosingRequested` guard | Retained | NFR20 | — |
| Cancel button visibility | Reworked | FR18, NFR20, UX-DR3 | Story 4.6 |
| Invalid crop closes overlay | Reworked | FR19, UX-DR3 | Story 4.6 |

**Rework Reason:** Epic 3 retrospective found 3 UX deviations: (1) no visible Cancel button (only Escape), (2) invalid crop closes overlay instead of staying active, (3) no completion feedback. Story 4.6 owns these fixes.

### Story 3.5: Hit Testing and Keyboard Escape

**Classification: RETAINED**

| Capability | Status | Supporting Requirements |
|------------|--------|------------------------|
| `CropCanvas.IsHitTestVisible` routing | Retained | FR18, FR20, FR21 |
| `OverlayKeyboardInputRouter` | Retained | FR18, NFR20 |
| `OverlayCancelRequestGate` | Retained | NFR20 |
| Escape via `RootGrid.KeyDown` + `KeyboardAccelerator` | Retained | FR18, NFR20 |
| `OverlayHitTestMode` / `OverlayHitTestModeDefaults` | Retained | FR21 |
| `ApplyCropSelectionAvailability()` disabling for unsupported states | Retained | FR20 |

**Rationale:** Hit testing and Escape routing work correctly. No conflicts.

### Story 3.6: Release-to-Capture and Basic Clipboard Output

**Classification: RETAINED (core) / REWORKED (feedback)**

| Capability | Status | Supporting Requirements | Rework Owner |
|------------|--------|------------------------|--------------|
| `CropCommitResult` enum | Retained | FR17, FR19 | — |
| Release-to-capture auto-confirm in `OnCropCanvasPointerReleased` | Retained | FR17, UX-DR3 | — |
| `RequestCaptureConfirm()` shared method | Retained | FR17 | — |
| `ClipboardOutputService` | Retained | FR24, FR48, NFR8 | — |
| "Copied to clipboard" feedback message | Reworked | FR24, UX-DR3 | Story 4.6 |

**Rework Reason:** Epic 3 retrospective found no completion feedback is shown. The closing state message was changed to "Crop confirmed. Closing..." but the actual feedback visibility needs to be fixed in Story 4.6.

---

## Settings and Infrastructure

### Lumiere.Settings Module

**Classification: RETAINED (boundary) / REWORKED (persistence)**

| Capability | Status | Supporting Requirements | Rework Owner |
|------------|--------|------------------------|--------------|
| `SettingsBoundary` marker type | Retained | NFR29 | — |
| Module boundary exists | Retained | NFR29, Architecture: Settings Boundary | — |
| Concrete persistence implementation | Reworked | FR38, NFR18 | Story 5.5 |

**Rework Reason:** The module exists as a boundary but has no concrete persistence, defaults, validation, or migration logic. Story 5.5 (Persist Local Settings Across Launches) owns the implementation.

### Lumiere.Infrastructure Diagnostics

**Classification: RETAINED**

| Capability | Status | Supporting Requirements |
|------------|--------|------------------------|
| `LumiereLoggerFactory` | Retained | Project Context: Logging Rules |
| `FileLogger` / `FileLoggerProvider` | Retained | NFR30 |
| `LogCategories` | Retained | NFR30 |
| `ValidationLogger` | Retained | NFR30 |
| `ILogger` integration | Retained | Project Context: Logging Rules |

**Rationale:** Structured logging is the foundation for diagnostic observability. Story 4.7 will extend it.

### Validation Documents

**Classification: RETAINED**

| Capability | Status | Supporting Requirements |
|------------|--------|------------------------|
| `docs/validation/lifecycle-validation.md` | Retained | NFR27, NFR33 |
| `docs/validation/overlay-validation.md` | Retained | NFR27, NFR33 |

**Rationale:** Validation evidence is historical foundation. Future stories reference and extend it.

---

## Overlay UI Types

### OverlayState and Display Types

**Classification: RETAINED**

| Capability | Status | Supporting Requirements |
|------------|--------|------------------------|
| `OverlayState` | Retained | FR20, NFR10 |
| `OverlayDisplayStatus` | Retained | FR20 |
| `OverlayStatusStyle` | Retained | FR20, NFR21 |
| `OverlayFailureAction` | Retained | FR20, NFR14 |

**Rationale:** Typed overlay state vocabulary. No conflicts.

---

## Capture Border Options

**Classification: RETAINED**

| Capability | Status | Supporting Requirements |
|------------|--------|------------------------|
| `CaptureBorderOptions` / `CaptureBorderApplicationResult` | Retained | NFR3 (overlay exclusion from capture) |

**Rationale:** Borderless capture support. No conflicts.

---

## Capabilities Conflicting with v0 MVP Direction

### 1. Picker-First Assumptions

**Status: Potential conflict — pending Story 4.3.**

The `ICaptureTargetPicker` / `GraphicsCaptureTargetPicker` / `GraphicsCapturePickerInterop` exist but are not the default path. `DirectMonitorCaptureTargetSelectionService` is the default. The picker currently appears to be demoted to fallback/debug only, which is consistent with the architecture decision.

However, Story 4.3 ("Demote Legacy Picker and Dashboard Behavior from the Default Path") is planned, suggesting there may be additional picker-related code or dashboard behavior that still needs active demotion. The current classification assumes the picker is already correctly scoped; Story 4.3 will verify and address any remaining issues.

**Action:** Story 4.3 will audit picker-related code and confirm whether further demotion is needed.

### 2. Dashboard-Era Labels or Debug-Oriented Commands

**Status: No dashboard-era labels found.**

`CaptureActionCard` is a clean card component with Glyph, Title, Description, and Shortcut properties. No debug-oriented commands or dashboard labels exist in the current codebase.

**Action:** None needed.

### 3. Hardcoded Status Messages

**Status: Minor concern — monitored.**

The overlay uses `OverlayState` and `OverlayDisplayStatus` for typed state. The "Crop confirmed. Closing..." message in Story 3.6 is a hardcoded string, but it's in the UI layer where user-facing text is appropriate. No low-level types carry hardcoded messages.

**Action:** None needed for cutover. Story 4.6 will address feedback visibility.

### 4. Confirm Button Conflicts with Release-to-Capture

**Status: Partial conflict — classified as rework.**

The Confirm button exists in `OverlayWindow.xaml` but release-to-capture auto-confirms on pointer release, making the button unreachable in the default flow. The button remains as a fallback for edge cases (e.g., click inside existing crop without dragging, then click Confirm).

**Action:** Story 4.6 may decide whether to keep the button as hidden fallback or remove it entirely.

---

## Summary Table

| Epic | Story | Classification | Rework Owner |
|------|-------|----------------|--------------|
| 1 | 1.1 | Retained | — |
| 1 | 1.2 | Retained | — |
| 1 | 1.3 | Retained | — |
| 1 | 1.4 | Retained | — |
| 1 | 1.5 | Retained | — |
| 2 | 2.1 | Retained | — |
| 2 | 2.2 | Retained | — |
| 2 | 2.3 | Retained | — |
| 2 | 2.4 | Retained | — |
| 2 | 2.5 | Retained | — |
| 3 | 3.1 | Retained | — |
| 3 | 3.2 | Retained | — |
| 3 | 3.3 | Reworked | Story 4.6 |
| 3 | 3.4 | Retained (foundation) / Reworked (UX deviations, Confirm button) | Story 4.6 |
| 3 | 3.5 | Retained | — |
| 3 | 3.6 | Retained (core) / Reworked (feedback) | Story 4.6 |
| — | Settings | Retained (boundary) / Reworked (persistence) | Story 5.5 |
| — | Infrastructure Diagnostics | Retained | — |
| — | Validation Docs | Retained | — |

---

## Deferred Items

| Item | Reason | Future Owner |
|------|--------|--------------|
| `ClipboardOutputService` architecture boundary violation (in Graphics.Clipboard, creates D3D11 textures) | Needs deeper refactoring; acceptable for MVP | Epic 6 (output semantics) |
| `CropCommitResult.InvalidGeometry` creates `CropSelection` even when invalid | Low risk, object identity change | Epic 4+ code cleanup (when touching CropController.Commit) |
| SDR display and multi-monitor testing | Requires Windows hardware | Story 4.5 |
| Clipboard lock recovery testing | Requires Windows hardware | Story 4.5 |
| COM pointer ownership patterns | Not started from Epic 2 retro | Epic 4+ infrastructure stories |

---

## Removed Items

No capabilities are classified as removed. All existing code has future value or is needed for MVP.

---

## Usage Guide

For Epic 4+ stories:

1. **Before retaining code:** Check this document confirms the capability is classified as retained.
2. **Before modifying code:** Check this document for the rework owner. If no owner is assigned, the story must claim ownership.
3. **Before modifying deferred code:** Check the Deferred Items table for the future owner. If your story touches deferred code, either coordinate with the owning epic or explicitly claim temporary ownership and note it in your story.
4. **Before removing code:** Check this document confirms the capability is not needed for MVP. If classified as retained, do not remove.
5. **Before adding new code:** Check this document for existing capabilities that should be reused instead of reinvented.

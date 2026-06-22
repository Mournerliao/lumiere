# 10-3 Carry Captured Target Context Into Output Feedback

## Context

Story `10-3` does not stop at the settings validation surface. Its acceptance criteria explicitly call out `main window, tray, overlay, and output feedback` as trust surfaces that must stay aligned to the active capture target across mixed-monitor scenarios.

Before this slice, the output-result fidelity detail still described profile gates, format contracts, and viewer evidence without saying which captured target the completed output belonged to. That was a real trust-surface gap: once capture finished and the session returned to `Idle`, the app no longer had a stable way to keep output feedback tied to the display or window that produced the artifact.

## What Changed

1. Added an output-specific target-context seam in `CaptureTargetScopeProjection`:
   - `PrefixOutputDetail(...)` now formats captured display/window context for output feedback without reusing the exact capture-status wording.
2. Extended `OutputResultProjection.Project(...)` to accept an optional captured-target context.
3. Extended `MainPanelProjection.Project(...)` and `TrayMenuProjection.Project(...)` to accept an optional `outputContextTarget`.
4. `MainWindow` now preserves the most recent capture target used for a completed output:
   - introduced `lastOutputTarget`
   - reset it when a new preview starts
   - assign it when configured output completes
   - reuse it when projecting post-capture main-panel and tray output feedback after the session has already returned to `Idle`
5. Added focused tests covering:
   - output-result fidelity detail with captured display identity and desktop bounds
   - main-panel output feedback retaining captured target context after session idle reset
   - tray projection remaining consistent with the strengthened output-feedback seam

## Why This Matters

- This closes a real `10-3` trust-surface gap instead of adding more generic copy.
- Output feedback can now say what artifact/profile/fidelity state was produced and which captured target it belonged to, even after the live session state has already been torn down.
- The seam stays clean and deep:
  - `MainWindow` owns the lifecycle fact of "which target produced the last output"
  - projection modules own all user-facing wording
  - no extra platform handles or DXGI details leak outward

## Validation

- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --filter "FullyQualifiedName~OutputResultProjectionTests|FullyQualifiedName~MainPanelProjectionTests|FullyQualifiedName~TrayMenuProjectionTests" --verbosity minimal /nr:false -p:UseSharedCompilation=false`
- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false /m:1 -p:UseSharedCompilation=false`

## Status

Story `10-3` remains `in-progress`: output feedback now carries captured-target context, but Public perfect-HDR-fidelity still requires real Windows mixed HDR/SDR and multi-monitor validation evidence before target-aware trust states can be marked complete.

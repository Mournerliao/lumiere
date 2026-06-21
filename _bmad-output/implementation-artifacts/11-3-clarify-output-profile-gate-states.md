---
title: 'Clarify Output Profile Gate States'
type: 'feature'
created: '2026-06-22'
status: 'in-progress'
route: 'vertical-slice'
story: '11-3'
---

# Clarify Output Profile Gate States

## Intent

The previous HDR10 runtime gate work correctly kept unsupported profiles on the `sRGB` fallback path, but the UI still collapsed too many different states into the same generic fallback presentation.

That made it harder for testers and future agents to answer a basic release-gate question:

- is HDR10 blocked because implementation is incomplete, or
- is HDR10 blocked because Windows manual evidence is still incomplete?

This slice clarifies those states without weakening the gate itself.

## Delivered In This Slice

1. Extended `OutputProfileExecutionCapabilities` with typed gate descriptions so a profile can report both executability and the reason it is still blocked.
2. Added three explicit UI-facing gate states for output profiles:
   - `Build`
   - `Validate`
   - `Ready`
3. Updated `PerfectHdrFidelityProjection` so HDR10 no longer appears as one generic fallback state:
   - `Build` when implementation prerequisites are still incomplete
   - `Validate` when implementation is ready but Windows manual viewer evidence is still incomplete
   - `Ready` only when the current session has both implementation readiness and complete manual evidence
4. Updated settings export-option projection so the `HDR10` radio option now follows the same runtime gate as the main panel instead of always rendering from the static design-only projection.
5. Added/updated tests across main-panel, settings, output-validation-source, and fidelity-projection coverage to lock the three-state behavior.
6. Extended tray projections so tray surfaces now carry explicit output-profile gate labels instead of relying only on fidelity-claim wording to imply runtime executability.

## Suggested Review Order

1. [Output gate model](../../src/Lumiere.Graphics/Output/OutputProfileContract.cs)
2. [Fidelity projection state mapping](../../src/Lumiere.App.Core/PerfectHdrFidelityProjection.cs)
3. [Settings export option projection](../../src/Lumiere.App.Core/SettingsPanelProjection.cs)
4. [Projection tests](../../tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs)

## Validation

- `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`

## Remaining Work

Story `11-3` is still `in-progress`, not `done`.

Remaining blockers:

- The repo still does not contain real Windows manual output validation artifacts, so HDR10 will ordinarily remain at `Build` or `Validate`, not `Ready`.
- Validation surfaces still depend on real evidence before any public-release claim can move beyond scoped status copy.
- Story `13-2` still needs real Windows accessibility validation for the export-profile interaction under keyboard, screen reader, high contrast, and text scaling.

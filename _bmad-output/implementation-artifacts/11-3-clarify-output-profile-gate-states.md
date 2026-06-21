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
7. Extended the validation surface so the current output-profile gate is shown directly alongside release evidence, instead of forcing testers to infer it from lower-level validation rows.
8. Extended the selected output-contract projection so once HDR10 has a complete manual format contract, the contract text also follows the same `Build` / `Validate` / `Ready` split instead of staying stuck on generic `pending implementation` wording.
9. Extended output-result projection so completed copy/save feedback now also preserves the selected profile gate for the current session. A successful artifact can still say the requested HDR10 path is at `Build` or `Validate` while runtime output falls back to `sRGB`, instead of silently collapsing to a generic effective-profile summary.
10. Extended overlay fidelity cue projection so the overlay now surfaces the same selected profile gate (`Build` / `Validate` / `Ready` / `Compat`) instead of only a generic fidelity-claim category.
11. Corrected mixed-target output semantics so `Both` mode no longer compresses clipboard and folder output into one synthetic runtime profile. Output result evidence can now report:
   - `Clipboard` as `sRGB` compatibility output, and
   - `Folder` as `HDR10` artifact output
   within the same capture result when that is what actually happened.
12. Updated output-result copy/tests to make per-target fidelity explicit, preventing `Both + HDR10` sessions from reading like the clipboard path also preserved HDR.
13. Tightened the manual evidence gate beneath those UI states so HDR10 JXR runtime readiness now ignores clipboard-only validation artifacts. The selected profile can only move toward `Ready` when the loaded Windows manual evidence actually covers the folder-based HDR10 artifact path.
14. Deepened the output-validation artifact model so a profile record can narrow its own target coverage with `outputTargetsCovered`, preventing `Both` session summaries from over-claiming HDR10 folder evidence when the record only validated clipboard semantics.
15. Threaded the current `OutputTarget` into selected-profile projection so main panel, settings, tray, and overlay no longer over-project folder-side HDR10 evidence onto clipboard sessions. `Clipboard` now stays `Compat` for HDR10/P3 requests, while `Both` can surface folder-gate progress without implying one uniform HDR-preserved path across both targets.
16. Aligned runtime output policy with that same target-aware scope through a shared `OutputProfileTargetScope` seam, so request-time profile evidence and projected gate state now interpret manual artifacts identically.

## Suggested Review Order

1. [Output gate model](../../src/Lumiere.Graphics/Output/OutputProfileContract.cs)
2. [Fidelity projection state mapping](../../src/Lumiere.App.Core/PerfectHdrFidelityProjection.cs)
3. [Settings export option projection](../../src/Lumiere.App.Core/SettingsPanelProjection.cs)
4. [Output result projection](../../src/Lumiere.App.Core/OutputResultProjection.cs)
5. [Overlay fidelity projection](../../src/Lumiere.App.Core/OverlayFidelityProjection.cs)
6. [Fidelity projection tests](../../tests/Lumiere.Graphics.Tests/App/PerfectHdrFidelityProjectionTests.cs)
7. [Output result projection tests](../../tests/Lumiere.Graphics.Tests/App/OutputResultProjectionTests.cs)
8. [Overlay fidelity projection tests](../../tests/Lumiere.Graphics.Tests/App/OverlayFidelityProjectionTests.cs)
9. [Output result model](../../src/Lumiere.Graphics/Output/OutputResult.cs)
10. [Output policy target-aware profile resolution](../../src/Lumiere.Graphics/Output/OutputRequest.cs)

## Validation

- `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --filter "FullyQualifiedName~OutputResultProjectionTests|FullyQualifiedName~MainPanelProjectionTests" --verbosity minimal /nr:false`
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --filter "FullyQualifiedName~OverlayFidelityProjectionTests|FullyQualifiedName~OutputResultProjectionTests" --verbosity minimal /nr:false`

## Remaining Work

Story `11-3` is still `in-progress`, not `done`.

Remaining blockers:

- The repo still does not contain real Windows manual output validation artifacts, so HDR10 will ordinarily remain at `Build` or `Validate`, not `Ready`.
- Validation surfaces still depend on real evidence before any public-release claim can move beyond scoped status copy.
- Real Windows `Both`-target validation sessions are still needed to confirm that clipboard compatibility and folder HDR artifacts are communicated clearly in live app flows, not only in projections/tests.
- Real Windows validation artifacts should adopt record-level target coverage when one session proves different target semantics for different output profiles.
- The updated target-aware projection rules still need real Windows `Clipboard` and `Both` validation artifacts to prove the UI copy matches observed runtime behavior, not only projection tests.
- Story `13-2` still needs real Windows accessibility validation for the export-profile interaction under keyboard, screen reader, high contrast, and text scaling.

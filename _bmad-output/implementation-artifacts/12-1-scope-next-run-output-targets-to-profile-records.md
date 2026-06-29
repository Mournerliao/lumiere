# 12-1 Scope Next-Run Output Targets To Profile Records

Date: 2026-06-29
Stories touched: `12-1`, `11-3`, `13-2`

## Why this slice

The validation run planner already used loaded evidence to suggest the next Windows run, and output validation artifacts can narrow a profile record through `outputTargetsCovered`. Before this slice, the planner still treated broad session-level `outputTargetsTested` as enough output-target coverage for the requested profile.

That was too optimistic for Public perfect-HDR-fidelity. A session can honestly record `Both` at the session level while the HDR10 profile record only proves `Clipboard` or `Folder`. The next-run guidance must not treat the unproven side as covered.

## What changed

1. Updated `OutputValidationRunPlanner` so missing output targets are calculated with profile-aware coverage:
   - uses `OutputValidationSessionArtifact.CoversProfileOutputTarget(profile.Kind, target)`
   - respects record-level `outputTargetsCovered`
   - keeps session-level `outputTargetsTested` as the fallback only when the profile record does not narrow scope
2. Added focused tests for next-run output-target planning:
   - broad session `Both` plus HDR10 record `Clipboard` still reports missing `Folder`
   - broad session `Both` with no record-level narrowing continues to cover the full session scope

## Validation

Written but NOT RUN on macOS:

- `OutputValidationRunPlannerTests.Create_UsesProfileRecordOutputTargetScopeForMissingOutputTargets`
- `OutputValidationRunPlannerTests.Create_TreatsSessionOutputTargetAsProfileScopeWhenRecordDoesNotNarrowIt`

Reason:

- The current workspace is macOS. Project validation for `.NET 10`, WinUI 3, WGC, DXGI, WIC/JPEG XR, HDR behavior, and native Settings evidence surfaces is Windows-only per `AGENTS.md`.

Recommended Windows validation commands:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --filter "OutputValidationRunPlannerTests|OutputValidationDraftFactoryTests|PerfectHdrFidelityProjectionTests|SettingsPanelProjectionTests" --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

Recommended Windows manual evidence path:

- `%LOCALAPPDATA%\Lumiere\validation\output\*.json`
- `%LOCALAPPDATA%\Lumiere\validation\output\evidence\*.md`

Manual review path:

1. Load or create a validation artifact where `outputTargetsTested` is `Both`.
2. Set the HDR10 profile record `outputTargetsCovered` to `Clipboard`.
3. Reload Settings > Validation.
4. Confirm the summary/draft next-run guidance still asks for `Folder` output evidence before treating HDR10 JXR folder validation as covered.

## Release Gate Status

- `Supported output compatibility matrix`: NOT RUN, pending real Windows named-viewer evidence.
- `HDR-preserved output profile contract`: NOT RUN, pending real Windows HDR10 JXR output evidence and viewer-recognized metadata evidence.
- `HDR/SDR visual validation set`: NOT RUN, pending executed Windows scenario notes.
- `Public perfect-HDR-fidelity`: blocked-on-Windows-evidence.

## Remaining Work

- Record real Windows artifacts whose `outputTargetsCovered` matches the actually observed target semantics for each profile record.
- Keep mixed `Both` sessions explicit: session-level coverage may describe the run, but profile-level coverage controls HDR10 release guidance and runtime readiness.

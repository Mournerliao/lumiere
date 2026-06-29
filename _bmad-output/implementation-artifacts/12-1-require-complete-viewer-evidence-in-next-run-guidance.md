# 12-1 Require Complete Viewer Evidence In Next-Run Guidance

Date: 2026-06-29
Stories touched: `12-1`, `11-3`, `13-2`

## Why this slice

The validation run planner already names missing HDR10 viewer targets for the next Windows run. Before this slice, a viewer target counted as covered as soon as a profile record mentioned the viewer name. That was too weak for Public perfect-HDR-fidelity because an artifact can contain a named viewer row whose artifact handling, visual match, HDR preservation, or HDR10 metadata status is still `NotRun`, `Limited`, or `Fail`.

Next-run guidance must keep asking for that named viewer until the required evidence statuses pass. Otherwise Settings > Validation and generated draft placeholders can stop pointing validators at incomplete target-app compatibility work.

## What changed

1. Updated `OutputValidationRunPlanner` so a viewer target is covered only when the required statuses for that profile pass:
   - `sRGB`: artifact handling and visual match
   - `HDR10`: artifact handling, visual match, HDR preservation, and HDR10 metadata recognition
   - `Display P3`: artifact handling, visual match, and preservation evidence
2. Added focused test coverage for an HDR10 artifact where Windows Photos is named but still lacks HDR10 metadata recognition evidence. The run plan now keeps Windows Photos in `MissingViewerTargets` and the next-run recommendation.

## Validation

Written but NOT RUN on macOS:

- `OutputValidationRunPlannerTests.Create_KeepsViewerTargetMissingUntilRequiredHdr10EvidencePasses`

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

1. Load or create a validation artifact that names Microsoft Paint, Windows Photos, and Microsoft Edge for HDR10.
2. Leave one viewer's `hdr10MetadataStatus` as `NotRun` or `Limited`.
3. Reload Settings > Validation.
4. Confirm the summary/draft next-run guidance still names that viewer instead of treating the target-app compatibility matrix as covered.

## Release Gate Status

- `Supported output compatibility matrix`: NOT RUN, pending real Windows named-viewer evidence.
- `HDR-preserved output profile contract`: NOT RUN, pending real Windows HDR10 JXR output evidence and viewer-recognized metadata evidence.
- `HDR/SDR visual validation set`: NOT RUN, pending executed Windows scenario notes.
- `Public perfect-HDR-fidelity`: blocked-on-Windows-evidence.

## Remaining Work

- Record real Windows target-app compatibility artifacts where each named viewer has concrete app version evidence and all required HDR10 statuses pass.
- Keep named but incomplete viewer rows visible as missing work until manual observations replace placeholder or incomplete statuses.

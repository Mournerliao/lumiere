# 12-1 Open Latest Loaded Validation Evidence

Date: 2026-06-22
Stories touched: `12-1`, `11-3`, `13-2`

## Why this slice

The new loaded-evidence summary made the current validation scope legible inside the app, but a tester still had to leave that summary and manually find the file if they wanted to inspect or edit the actual artifact backing it.

For `Public perfect-HDR-fidelity`, that was still one step too indirect. The app needs to help the validator move from summary to durable evidence record without turning the settings panel into a dashboard or another workflow wizard.

## What changed

1. Extended `OutputValidationArtifactSnapshot` with `ArtifactReferences` so the load seam keeps the stable file path next to each parsed validation artifact.
2. Deepened `ValidationEvidenceSummaryProjection` so it can carry:
   - latest loaded artifact path
   - whether the current session can directly open that artifact
3. Kept latest-artifact selection in the projection seam rather than recomputing it in `MainWindow`.
4. Added a native `Open latest evidence` action to Settings > Validation.
5. Wired the button state, tooltip, and accessibility help text to the projection:
   - enabled only when a loaded artifact path exists
   - disabled when the session has no loaded artifact
6. Reused the existing Windows shell action seam instead of inventing a second file-opening path.

## Validation

- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false /m:1`
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false --filter "FullyQualifiedName~PerfectHdrFidelityProjectionTests|FullyQualifiedName~SettingsPanelProjectionTests|FullyQualifiedName~OutputValidationArtifactSourceTests|FullyQualifiedName~WindowsArtifactShellActionTests"`

Result:

- Build passed.
- Targeted tests passed: `97 passed`.

## Notes

- This slice improves evidence traceability inside the app; it does not change any release gate by itself.
- The next highest-value work is still recording real Windows manual validation artifacts for the supported HDR path.

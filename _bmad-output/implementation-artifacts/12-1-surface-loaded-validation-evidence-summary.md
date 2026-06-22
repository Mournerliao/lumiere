# 12-1 Surface Loaded Validation Evidence Summary

Date: 2026-06-22
Stories touched: `12-1`, `11-3`, `13-2`

## Why this slice

`Public perfect-HDR-fidelity` is still blocked by real Windows manual evidence, but the in-app validation surface was still making the tester reconstruct too much context from raw rows and workspace actions.

We already had:

- local validation workspace setup
- open/reload/create-draft actions
- gate rows and viewer matrix

What was still missing was the small "what evidence is actually loaded right now?" surface described by the design extension. Without that, the settings panel still behaved more like a tool launcher than a reviewable evidence surface.

## What changed

1. Added a dedicated `ValidationEvidenceSummaryProjection` seam in `PerfectHdrFidelityProjection` so evidence aggregation stays out of `MainWindow`.
2. The projection now summarizes:
   - latest loaded artifact date, tester, build, and result summary
   - covered output targets
   - covered named viewers
   - covered checklist IDs
   - known limitations
   - follow-up issues or stories
   - ignored-file warnings
   - workspace-not-ready guidance
3. Kept snapshot-aware behavior honest:
   - raw artifact enumeration still gives the clean evidence summary path
   - snapshot-based settings projection can additionally surface ignored files and workspace issues
4. Added a compact native `Loaded evidence` block to Settings > Validation.
5. Kept the UI aligned with the restrained WinUI product register:
   - no dashboard treatment
   - no new expert workspace
   - no broad release-pass claim
   - only concise, reviewable evidence status
6. Updated validation documentation so the app surface and local Windows workflow describe the same evidence-review model.

## Validation

- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false /m:1`
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false --filter "FullyQualifiedName~PerfectHdrFidelityProjectionTests|FullyQualifiedName~SettingsPanelProjectionTests|FullyQualifiedName~OutputValidationArtifactSourceTests"`

Result:

- Build passed.
- Targeted tests passed: `95 passed`.

## Notes

- This slice improves reviewability of loaded Windows evidence; it does not replace the remaining manual validation work.
- `Epic 12` and `Epic 13` remain `in-progress`.
- The next highest-value step is still recording real Windows validation artifacts, not adding more local evidence tooling.

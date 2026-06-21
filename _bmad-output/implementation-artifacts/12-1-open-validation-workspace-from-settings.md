# 12-1 Open Validation Workspace From Settings

Date: 2026-06-22
Stories touched: `12-1`, `11-3`, `13-2`

## Why this slice

`Public perfect-HDR-fidelity` is still blocked by real Windows manual evidence, not by one more round of abstract gate modeling.

The app already knew:

- where the local output-validation workspace should live
- whether a seeded validation template exists
- how to project that state into the validation record

But the user still had to leave the app and rediscover the local paths manually before recording real Windows evidence. That kept the validation model stronger than the actual validation workflow.

## What changed

1. Extended the settings validation section with native WinUI actions:
   - `Open workspace`
   - `Open template`
   - `Reload evidence`
2. Wired those actions through `MainWindow` so they consume the current validation-record projection instead of inventing a second path-resolution flow.
3. Added in-app validation artifact reload so a tester can edit local evidence JSON, then refresh gates and viewer evidence without restarting Lumiere.
4. Hardened `WindowsArtifactShellAction` so `Open` now supports both file and directory paths. This keeps validation-workspace opening on the same narrow shell seam already used for after-capture artifact actions.
5. Updated automation help text and tooltips so the validation buttons stay explicit, native, and useful for keyboard/screen-reader navigation.
6. Updated validation docs so the runtime workflow now matches the shipped app behavior.

## Validation

- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`
- `dotnet build tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-build --verbosity minimal /nr:false --filter "FullyQualifiedName~WindowsArtifactShellActionTests|FullyQualifiedName~OutputValidationArtifactSourceTests|FullyQualifiedName~PerfectHdrFidelityProjectionTests"`

Result:

- Build passed.
- Targeted tests passed: `49 passed`.

## Notes

- This slice does not claim that Windows manual validation is complete.
- It improves the product-side execution path for future real validation sessions, which is directly aligned with the remaining Public perfect-HDR-fidelity blocker set.

# 12-1 Create Prefilled Output Validation Draft

Date: 2026-06-22
Stories touched: `12-1`, `11-3`, `13-2`

## Why this slice

`Public perfect-HDR-fidelity` is still blocked by real Windows manual evidence, not by another round of abstract gate wording.

The local validation workspace and reload/open actions already reduced path-discovery friction, but the Windows validator still had to hand-build every session JSON before any real observation could be recorded. That left too much avoidable manual setup in the critical path for target-app and HDR evidence.

## What changed

1. Deepened the existing local output-validation workspace seam so it can now both:
   - load local validation artifacts
   - create a new prefilled validation draft in the workspace root
2. Added `OutputValidationDraftFactory` to generate a session-local `OutputValidationSessionArtifact` draft from current runtime context:
   - current app version
   - current build commit token when the informational version exposes one
   - selected output target
   - selected output profile
   - profile-specific viewer skeleton
   - per-viewer target-app version placeholders
   - active capture target display name and bounds when available
3. Kept manual evidence honest:
   - viewer statuses remain `NotRun`
   - tester/device/GPU/DPI/Windows-version fields remain placeholders
   - build commit stays a placeholder only when Lumiere cannot prove a comparable current build token
   - target-app versions stay placeholders until the tested Windows machine records the actual viewer/app versions
   - target HDR detail still requires human validation notes
4. Added a native `Create draft` action to the settings validation section.
5. Kept the draft workflow non-deceptive:
   - Lumiere opens the generated draft for editing
   - Lumiere does not auto-reload the draft into the current session
   - untouched draft files therefore do not immediately affect gate projections
6. Updated workspace guidance text so the seeded local workflow now matches the app behavior.

## Validation

- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false /m:1`
- `dotnet build tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false /m:1`
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-build --verbosity minimal /nr:false --filter "FullyQualifiedName~OutputValidationDraftFactoryTests|FullyQualifiedName~OutputValidationArtifactSourceTests|FullyQualifiedName~PerfectHdrFidelityProjectionTests|FullyQualifiedName~WindowsArtifactShellActionTests"`

Result:

- Build passed.
- Targeted tests passed: `49 passed`.

## Notes

- This slice does not claim that any Windows manual validation has completed.
- It improves the product-side execution path for future real evidence capture, which is directly aligned with the remaining `Public perfect-HDR-fidelity` blocker set.
- `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal` still fails on pre-existing repository-wide line-ending issues outside this slice.

# 11-3 Narrow Browser Validation Target To Microsoft Edge

## Context

The Public perfect-HDR-fidelity direction had already tightened Lumiere's output validation around named target apps, target-aware HDR evidence, build alignment, and recorded target-app versions. One remaining ambiguity was that the browser-side validation target still used a generic Chromium bucket.

That wording was too broad for a public-release evidence model:

- a generic browser bucket does not identify the real tested target app
- target-app version evidence becomes vague when the target is not a concrete application
- draft generation cannot honestly prefill a version for a non-specific browser bucket
- UI and validation templates risk implying broader support than Lumiere has actually validated

## What Changed

1. The named browser validation target was narrowed from a generic Chromium bucket to `Microsoft Edge` across the active output profile contracts, validation templates, UI surfaces, and automated tests.
2. `WindowsTargetAppVersionPrefillProvider` now resolves `Microsoft Edge` through an executable-version seam using `msedge.exe`, while packaged apps such as `Microsoft Paint` and `Windows Photos` still use package-family resolution.
3. Output-validation drafts and seeded schema samples now use `REPLACE_WITH_MICROSOFT_EDGE_VERSION` instead of a generic Chromium placeholder.
4. Current validation guidance now names `Microsoft Edge` explicitly wherever the active release-track browser target matters.

## Why This Matters

- Lumiere's public-fidelity evidence model is now scoped to a real, versionable target app instead of a loose browser family.
- Validation artifacts, runtime gates, and UI copy now align on the same named browser target.
- The local draft workflow reduces manual friction for Edge specifically without pretending that every Chromium-based browser is covered.

## Validation

- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --filter "FullyQualifiedName~TargetAppVersionPrefillProviderTests|FullyQualifiedName~OutputValidationDraftFactoryTests|FullyQualifiedName~OutputValidationArtifactSourceTests|FullyQualifiedName~OutputProfileContractTests|FullyQualifiedName~Hdr10JxrViewerValidationEvidenceTests|FullyQualifiedName~OutputValidationDocumentationTests|FullyQualifiedName~PerfectHdrFidelityProjectionTests|FullyQualifiedName~SettingsPanelProjectionTests|FullyQualifiedName~OutputResultProjectionTests|FullyQualifiedName~OutputValidationSessionArtifactTests" --verbosity minimal /nr:false`
- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false /m:1`

## Status

Story `11-3` remains `in-progress`: the browser-side target is now concretely scoped to `Microsoft Edge`, but public release still requires real Windows manual viewer evidence and recorded current-build validation artifacts.

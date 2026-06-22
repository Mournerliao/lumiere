# 12-1 Prefill Known Target-App Versions In Draft

## Context

The validation workflow had already grown support for target-app version evidence, and the runtime/review gates now depend on it. The remaining friction was that `Create draft` still generated version placeholders for every named viewer/app, even when the local Windows machine could already identify some of those app versions directly.

## What Changed

1. Added a target-app version prefill seam for output-validation draft generation.
2. Production draft generation now uses a Windows-backed provider that can prefill known packaged-app versions for supported viewers such as:
   - `Microsoft Paint`
   - `Windows Photos`
3. Unsupported viewers still stay on explicit `REPLACE_WITH_*` placeholders until Lumiere has a real local version-resolution path for them.
4. Artifact-source draft creation now passes that provider through the normal workspace workflow instead of requiring UI-layer logic.

## Why This Matters

- The draft now moves one step closer to real Windows manual evidence instead of stopping at pure placeholders.
- Lumiere still stays honest: it only pre-fills versions it can identify locally and leaves the rest manual.
- This supports Story `12-1` by making validation records more repeatable and less error-prone across builds.

## Validation

- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --filter "FullyQualifiedName~OutputValidationDraftFactoryTests|FullyQualifiedName~OutputValidationArtifactSourceTests|FullyQualifiedName~TargetAppVersionPrefillProviderTests" --verbosity minimal /nr:false`

## Status

Story `12-1` remains `in-progress`: draft generation is now more evidence-aware, but actual Windows validation sessions and recorded results are still required before public release readiness can be claimed.

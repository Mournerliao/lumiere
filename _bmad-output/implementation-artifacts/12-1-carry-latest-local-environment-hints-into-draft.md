# 12-1 Carry Latest Local Environment Hints Into Draft

## Context

`Create draft` had already become useful for current build, output target, output profile, target-aware HDR placeholders, and target-app version structure. But for real Windows manual evidence, validators were still repeatedly retyping stable environment context such as tester name, Windows version, device label, GPU label, DPI scale, and common entry points.

That repetition was not the same as proof, but it was still friction in the critical path to recording real evidence.

## What Changed

1. Deepened `FileOutputValidationArtifactSource` so draft creation can inspect the latest compatible local output-validation artifacts before generating a new draft.
2. Added a small `OutputValidationDraftSeed` seam that carries prior local environment hints without broadening the release-evidence interface.
3. `OutputValidationDraftFactory` now keeps explicit `REPLACE_WITH_*` placeholders for manual evidence, but appends `latest local artifact: ...` hints when the current machine already has recent local validation context for:
   - tester
   - Windows version
   - device
   - GPU
   - display setup
   - DPI scale
   - entry points tested
4. The seed selection stays narrow and honest:
   - drafts only borrow hints from the latest compatible local artifact
   - output-target and requested-profile compatibility are preferred
   - no borrowed value is treated as completed evidence

## Why This Matters

- This moves `12-1` one step closer to real manual evidence execution instead of another round of generic templates.
- The validator gets less repetitive setup work while still being forced to replace explicit placeholders with fresh observed data.
- Lumiere now reuses durable local context without pretending that older artifacts automatically satisfy current-build release gates.

## Validation

- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-build --filter "FullyQualifiedName~OutputValidationDraftFactoryTests|FullyQualifiedName~OutputValidationArtifactSourceTests|FullyQualifiedName~PerfectHdrFidelityProjectionTests|FullyQualifiedName~SettingsPanelProjectionTests|FullyQualifiedName~OutputValidationSessionArtifactTests" --verbosity minimal /nr:false`
- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false /m:1`

## Status

Story `12-1` remains `in-progress`: draft ergonomics now reuse more stable local context, but Public perfect-HDR-fidelity still depends on real Windows manual validation sessions and recorded current-build evidence.

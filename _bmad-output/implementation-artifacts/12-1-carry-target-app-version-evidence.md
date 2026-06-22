# 12-1 Carry Target-App Version Evidence

Date: 2026-06-22
Stories touched: `12-1`, `11-3`

## Why this slice

The public-fidelity planning artifacts already call out target app versions as part of the Windows manual validation record, but the output-validation artifact shape still only carried target app names.

That meant a validator could record "Windows Photos passed" without preserving which Windows Photos build was actually tested, which weakens the public release evidence trail.

## What changed

1. Extended `OutputValidationSessionArtifact` with optional `TargetAppVersions` records.
2. Updated the built-in sample template and harness template so real sessions now have an obvious place to record viewer/app versions.
3. Updated `OutputValidationDraftFactory` so generated drafts prefill per-viewer version placeholders instead of making validators invent the structure by hand.
4. Extended the loaded-evidence summary so it can surface recorded viewer/app versions directly in coverage text.
5. Added a lightweight summary gap reminder when target apps are present but their versions are still missing.

## Validation

- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false --filter "FullyQualifiedName~OutputValidationSessionArtifactTests|FullyQualifiedName~OutputValidationDraftFactoryTests|FullyQualifiedName~PerfectHdrFidelityProjectionTests|FullyQualifiedName~SettingsPanelProjectionTests"`
- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false /m:1`

Result:

- Build passed.
- Targeted tests passed: `112 passed`.

## Notes

- This slice improves the fidelity of the durable evidence record; it does not by itself satisfy the missing Windows manual validation runs.
- Current public-release blockers remain the real Windows evidence itself, especially the first current-build HDR-preserved file-output path.

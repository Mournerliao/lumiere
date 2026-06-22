---
title: 'Seed Public Validation Guides Into Local Workspace'
type: 'feature'
created: '2026-06-23'
status: 'done'
route: 'native-validation-surface'
story: '12-1'
---

# Seed Public Validation Guides Into Local Workspace

## Intent

The app-local validation workspace could already seed a JSON sample, generate validation drafts, and expose long-run trend helpers. But a Windows validator still had to leave that path and manually hunt through repo docs to find the current release checklist, the HDR/SDR scenario guide, and the settings accessibility workflow.

This slice keeps Public perfect-HDR-fidelity moving toward real evidence execution instead of another isolated review surface: the same local workspace now carries the core release guides, and Settings > Validation can open them directly.

## Delivered In This Slice

1. `FileOutputValidationArtifactSource` now seeds a local `guidance\` folder beside the existing templates and evidence folders.
2. The current build now copies these public-fidelity validation guides into that local workspace:
   - `release-validation-checklist.md`
   - `hdr-sdr-validation-scenarios.md`
   - `settings-accessibility-validation.md`
3. The local workspace `README.txt` now points validators to those seeded guides before treating any manual evidence as public-release support.
4. `ValidationRecordProjection` now carries guide paths alongside the existing workspace/template/trend helper paths.
5. Settings > Validation now exposes three native open actions:
   - `Release checklist`
   - `Scenario guide`
   - `A11y guide`
6. Tests now cover both seeded workspace content and validation-record projection for the new guide paths.

## Review Pointers

1. [OutputValidationArtifactSource.cs](../../src/Lumiere.App.Core/OutputValidationArtifactSource.cs)
2. [PerfectHdrFidelityProjection.cs](../../src/Lumiere.App.Core/PerfectHdrFidelityProjection.cs)
3. [MainWindow.xaml](../../src/Lumiere.App/MainWindow.xaml)
4. [MainWindow.xaml.cs](../../src/Lumiere.App/MainWindow.xaml.cs)
5. [output-validation.md](../../harness/validation/output-validation.md)

## Validation

- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --filter "OutputValidationArtifactSourceTests|PerfectHdrFidelityProjectionTests|SettingsPanelProjectionTests" --no-restore --verbosity minimal /nr:false`
- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 -p:UseSharedCompilation=false --no-restore --verbosity minimal /nr:false`

## Remaining Work

Story `12-1` remains `in-progress`.

Remaining release-blocking work:

- Run and record real Windows manual validation sessions using those seeded guides.
- Replace draft placeholders with observed results for target-aware HDR, named viewers, DPI, and accessibility.
- Use the same local workflow to backfill current-build evidence instead of treating seeded docs as proof by themselves.

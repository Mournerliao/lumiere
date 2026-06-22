---
title: 'Surface Resource Trend Workflow In Settings'
type: 'feature'
created: '2026-06-23'
status: 'in-progress'
route: 'native-validation-surface'
story: '12-3'
---

# Surface Resource Trend Workflow In Settings

## Intent

Story `12-3` already had the repo-level sampler script, workflow doc, and session template, but the app itself still stopped short of helping a Windows validator actually start a long-run run. That meant the local validation surface could seed output-validation drafts, yet resource-trend work still depended on manual path hunting and hand-built PowerShell commands.

This slice closes that gap by extending the existing Settings > Validation workflow so the same app-local workspace can directly support long-run resource-trend execution.

## Delivered In This Slice

1. The app-local validation workspace now seeds:
   - `templates/resource-trend-session-template.md`
   - `collect-resource-trend-samples.ps1`
2. Settings > Validation now exposes three native helpers for Story `12-3`:
   - `Trend template`
   - `Trend script`
   - `Copy trend cmd`
3. `Copy trend cmd` now uses [ResourceTrendValidationCommandProjection](../../src/Lumiere.App.Core/ResourceTrendValidationCommandProjection.cs) so the command shape stays centralized and testable instead of being assembled ad hoc in the window layer.
4. Validation-record projection now carries seeded resource-trend paths and command availability flags so the WinUI layer can stay thin and state-driven.
5. Output-validation guidance and resource-trend workflow docs now describe the new in-app launch path explicitly.

## Review Pointers

1. [MainWindow.xaml](../../src/Lumiere.App/MainWindow.xaml)
2. [MainWindow.xaml.cs](../../src/Lumiere.App/MainWindow.xaml.cs)
3. [OutputValidationArtifactSource.cs](../../src/Lumiere.App.Core/OutputValidationArtifactSource.cs)
4. [PerfectHdrFidelityProjection.cs](../../src/Lumiere.App.Core/PerfectHdrFidelityProjection.cs)
5. [ResourceTrendValidationCommandProjection.cs](../../src/Lumiere.App.Core/ResourceTrendValidationCommandProjection.cs)

## Validation

- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --filter "ResourceTrendValidationCommandProjectionTests|PerfectHdrFidelityProjectionTests|OutputValidationArtifactSourceTests" --no-restore --verbosity minimal /nr:false`
- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 --verbosity minimal`

## Remaining Work

Story `12-3` remains `in-progress`.

Remaining release-blocking work:

- Run real Windows `50+` and `100+` capture/output cycles against `Lumiere.App`.
- Save the resulting CSV and summary JSON artifacts.
- Fill the seeded session template with real observations and release judgement.
- Decide whether any observed drift is a blocker, limitation, or acceptable risk.

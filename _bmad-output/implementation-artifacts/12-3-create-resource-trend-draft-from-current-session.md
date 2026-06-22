---
title: 'Create Resource Trend Draft From Current Session'
type: 'feature'
created: '2026-06-23'
status: 'in-progress'
route: 'native-validation-surface'
story: '12-3'
---

# Create Resource Trend Draft From Current Session

## Intent

Story `12-3` already had seeded long-run helpers inside Settings > Validation, but a validator still had to manually create the session markdown record before starting a real run. That left the workflow half-native: the app could point to the template and build the sampler command, yet it still stopped short of turning current-session context into a durable draft artifact.

This slice closes that gap by letting Lumiere write the first session record directly into the app-local validation workspace while keeping all release-evidence fields explicitly manual until the run is actually observed on Windows hardware.

## Delivered In This Slice

1. `IOutputValidationArtifactSource` now exposes `CreateResourceTrendDraft(...)` so workspace-backed draft creation stays behind the same artifact-source seam as seeded templates and output-validation drafts.
2. [ResourceTrendValidationDraftFactory](../../src/Lumiere.App.Core/ResourceTrendValidationDraftFactory.cs) now centralizes the markdown-draft content, including:
   - current build/commit hint
   - current process ID
   - current output target
   - current-session GPU, DPI, display-topology, and HDR-state hints
   - workspace-local sampler command and expected artifact paths
3. Settings > Validation now exposes a native `Create trend draft` action that writes the draft and opens it immediately for the validator.
4. Validation-record capability projection now distinguishes `CanCreateResourceTrendDraft` from the lighter `Copy trend cmd` path, so the button only enables when the workspace, template, and sampler script are all present.
5. Validation docs now describe the generated resource-trend draft as a workflow accelerator, not as release evidence.

## Review Pointers

1. [ResourceTrendValidationDraftFactory.cs](../../src/Lumiere.App.Core/ResourceTrendValidationDraftFactory.cs)
2. [OutputValidationArtifactSource.cs](../../src/Lumiere.App.Core/OutputValidationArtifactSource.cs)
3. [PerfectHdrFidelityProjection.cs](../../src/Lumiere.App.Core/PerfectHdrFidelityProjection.cs)
4. [MainWindow.xaml](../../src/Lumiere.App/MainWindow.xaml)
5. [MainWindow.xaml.cs](../../src/Lumiere.App/MainWindow.xaml.cs)

## Validation

- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --filter "ResourceTrendValidationDraftFactoryTests|ResourceTrendValidationCommandProjectionTests|OutputValidationArtifactSourceTests|PerfectHdrFidelityProjectionTests" --no-restore --verbosity minimal /nr:false`
- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 --verbosity minimal`

## Remaining Work

Story `12-3` remains `in-progress`.

Remaining release-blocking work:

- Run real Windows `50+` and `100+` capture/output cycles against `Lumiere.App`.
- Save the resulting CSV and summary JSON artifacts from the seeded sampler command.
- Replace every generated placeholder with real manual observations and release judgement.
- Decide whether any measured drift is a blocker, a limitation, or acceptable within the scoped public release claim.

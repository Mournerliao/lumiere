# 12-1 Seed Scenario Session Template Into Local Workspace

Date: 2026-06-24

## Story

Story `12-1-establish-standard-hdr-sdr-validation-content-and-scenarios`

## Context

The repo already had `harness/validation/templates/hdr-sdr-validation-session-template.md` for focused Windows manual scenario runs, but the app-local validation workspace only seeded the JSON output-validation sample, public-release guidance docs, accessibility workflow, and resource-trend helpers. That left Story `12-1` scenario execution less machine-local than the other public-fidelity evidence workflows.

## Changes

- Embedded `Validation/Output/hdr-sdr-validation-session-template.md` in `Lumiere.App.Core`.
- Updated `FileOutputValidationArtifactSource` so `%LOCALAPPDATA%\Lumiere\validation\output\templates\hdr-sdr-validation-session-template.md` is seeded with the rest of the validation workspace.
- Updated workspace README guidance to distinguish the Story `12-1` scenario-session template from Story `12-3` resource-trend templates.
- Updated validation docs to keep the template workflow explicit while keeping the output-validation loader JSON-only.
- Kept UI scope constrained: no new Settings button was added; the existing validation actions remain consolidated under the compact native command surface.

## Validation

- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 -p:UseSharedCompilation=false --no-restore --verbosity minimal /nr:false`
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --filter "OutputValidationArtifactSourceTests|PerfectHdrFidelityProjectionTests|SettingsPanelProjectionTests" --no-restore --verbosity minimal /nr:false`
- `git diff --check`

## Remaining Release Work

This does not complete Story `12-1`. Public release still requires executed Windows manual HDR/SDR scenario sessions, real evidence paths, loaded JSON output-validation artifacts, and release-checklist rows that match the observed Windows hardware behavior.

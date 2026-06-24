# 12-1 Link Scenario Session Drafts To Output Evidence

Date: 2026-06-24

## Story

Story `12-1-establish-standard-hdr-sdr-validation-content-and-scenarios`

## Context

The local validation workspace now seeds `templates\hdr-sdr-validation-session-template.md`, but generated JSON output-validation drafts still pointed at a generic evidence note. That kept Story `12-1` scenario execution and runtime-loaded JSON evidence adjacent but not directly linked.

## Changes

- Added `ScenarioValidationDraftFactory` to prefill the seeded markdown scenario-session template from an `OutputValidationSessionArtifact`.
- Updated `Create draft` so it writes both:
  - the JSON output-validation draft in the workspace root
  - a companion markdown scenario-session draft in `evidence\`
- Updated the generated JSON draft's `evidencePaths` so it points at the companion markdown scenario record.
- Updated the markdown draft so it links back to the generated JSON artifact file name.
- Updated validation docs and the JSON sample template to describe workspace-local scenario-session evidence paths.

## Validation

- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --filter "OutputValidationDraftFactoryTests|OutputValidationArtifactSourceTests|OutputValidationDocumentationTests" --no-restore --verbosity minimal /nr:false`
- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 -p:UseSharedCompilation=false --no-restore --verbosity minimal /nr:false`
- `git diff --check`

## Remaining Release Work

This still does not count as Windows manual evidence. A validator must fill the generated JSON and markdown drafts with observed Windows hardware results before any public-fidelity gate can pass.

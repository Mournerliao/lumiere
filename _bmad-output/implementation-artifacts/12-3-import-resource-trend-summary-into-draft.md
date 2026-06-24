# 12-3 Import Resource Trend Summary Into Draft

Date: 2026-06-24

## Story

Story `12-3-record-long-run-capture-and-output-resource-trends`

## Context

The app-local validation workspace already seeded the resource trend template and sampler script, and `Create trend draft` could generate a long-run markdown draft. After a sampler run, however, validators still had to copy the summary JSON path, CSV path, and metric baseline/final/delta/min/max values by hand.

## Changes

- Added `ResourceTrendSummaryArtifact` to parse the sampler `*-summary.json` output into typed metric summaries.
- Updated `FileOutputValidationArtifactSource.CreateResourceTrendDraft` to import the latest readable `resource-trends\*-summary.json` from the local workspace.
- Updated `ResourceTrendValidationDraftFactory` to fill CSV path, summary path, duration, sample interval, GPU-counter availability, sample-count context, and metric summary rows when a sampler summary exists.
- Kept public-release judgement honest: imported telemetry still leaves metric and session classification as `REPLACE_WITH_PASS_FAIL_LIMITATION` for human review.
- Updated validation docs to describe the imported-summary workflow and its evidence boundary.

## Validation

- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --filter "ResourceTrendValidationDraftFactoryTests|OutputValidationArtifactSourceTests" --no-restore --verbosity minimal /nr:false`
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --filter "ResourceTrendValidationDraftFactoryTests|OutputValidationArtifactSourceTests|OutputValidationDocumentationTests" --no-restore --verbosity minimal /nr:false`
- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 -p:UseSharedCompilation=false --no-restore --verbosity minimal /nr:false`
- `git diff --check`

## Remaining Release Work

This does not complete Story `12-3`. Public release still needs executed Windows `50+` / `100+` cycle runs, retained CSV/summary artifacts, logs or screenshots where useful, and an explicit pass/fail/limitation judgement by a validator.

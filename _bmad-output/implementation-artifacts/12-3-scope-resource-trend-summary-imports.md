# 12-3 Scope Resource Trend Summary Imports

Date: 2026-06-24

## Story

Story `12-3-record-long-run-capture-and-output-resource-trends`

## Context

`Create trend draft` can import sampler `*-summary.json` output into the resource-trend markdown draft, but the first implementation imported the latest readable summary without checking whether it belonged to the current Lumiere process. That could make stale or unrelated sampler output look adjacent to the current validation run.

## Changes

- Added `ResourceTrendSummaryArtifact.MatchesProcessId`.
- Updated resource-trend summary selection to prefer the latest readable summary whose PID matches the current Lumiere process.
- Kept a fallback to the latest readable summary when no matching-PID summary exists, but the generated draft now marks the mismatch with a scope warning.
- Added PID-scope detail to the generated markdown so validators can see whether imported telemetry matches the current process.
- Updated validation docs to require validator confirmation before counting a draft that contains a scope warning.

## Validation

- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --filter "ResourceTrendValidationDraftFactoryTests|OutputValidationArtifactSourceTests|OutputValidationDocumentationTests" --no-restore --verbosity minimal /nr:false`
- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 -p:UseSharedCompilation=false --no-restore --verbosity minimal /nr:false`
- `git diff --check`

## Remaining Release Work

This still does not complete Story `12-3`. Public release still needs executed Windows `50+` / `100+` cycle resource trend sessions, retained CSV/summary artifacts, and explicit pass/fail/limitation judgement by a validator.

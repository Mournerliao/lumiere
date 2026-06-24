# 12-1 Require Workspace-Local Scenario Evidence

Date: 2026-06-24

## Story

Story `12-1-establish-standard-hdr-sdr-validation-content-and-scenarios`

## Context

`Create draft` writes a JSON output-validation draft plus a companion markdown scenario-session note under the local validation workspace `evidence\` folder. The JSON points at that note through `evidencePaths`.

Before this slice, a later edit could leave the JSON artifact pointing at a missing local note while the loader still treated the JSON as usable loaded evidence. That weakened the Story `12-1` chain from standard scenario content to actual session notes.

## Changes

- Added workspace-local `evidence\...` path validation in `FileOutputValidationArtifactSource`.
- If a loaded artifact references a missing local evidence file, the loader records an `OutputValidationArtifactLoadIssue` and skips that artifact for the current session.
- Kept repo-relative evidence references review-only so historical or docs-backed references are not rejected just because the app cannot resolve the repo path from `%LOCALAPPDATA%`.
- Added tests for both missing and present workspace-local scenario evidence.
- Updated `harness/validation/output-validation.md` so the documented loader contract matches the stricter behavior.

## Validation

- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --filter "OutputValidationArtifactSourceTests|OutputValidationDocumentationTests" --no-restore --verbosity minimal /nr:false`
- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 -p:UseSharedCompilation=false --no-restore --verbosity minimal /nr:false`
- `git diff --check`

## Remaining Release Work

This does not complete Story `12-1`. Public release still needs actual Windows HDR/SDR/mixed-display scenario sessions with filled markdown notes, JSON artifacts, target app versions, observed viewer behavior, and release-gate judgement.

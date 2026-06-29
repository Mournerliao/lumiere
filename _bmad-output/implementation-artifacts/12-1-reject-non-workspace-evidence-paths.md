# 12-1 Reject Non-Workspace Evidence Paths

Date: 2026-06-29

## Story

Story `12-1-establish-standard-hdr-sdr-validation-content-and-scenarios`

## Context

Story `12-1` already requires output-validation JSON artifacts to link their companion scenario-session notes through workspace-local `evidence\...` paths. The loader rejected missing workspace-local notes and placeholder markdown, but it still allowed non-workspace-local strings such as `docs\validation\evidence\...` to pass without a load issue because unresolved review references were silently ignored.

For the Public perfect-HDR-fidelity release gate, app-loaded runtime validation evidence must not be unlocked by external or repo-relative references. Human reviewers may still mention repo documentation in notes, but `evidencePaths` that participate in the app's current-session validation state must point into the local validation workspace.

## Changes

- Updated `FileOutputValidationArtifactSource` so non-workspace-local `evidencePaths` now produce load issues:
  - relative paths must start with `evidence\...`
  - absolute paths must remain inside the local workspace `evidence` directory
- Added loader tests for repo-relative `docs\...` evidence paths and absolute paths outside the workspace.
- Updated `harness/validation/output-validation.md` to state that app-loaded `evidencePaths` must be workspace-local; repo-relative references are review notes only.
- Updated the live release checklist and public-fidelity alignment note so the release-gate documentation matches the stricter loader contract.

## Validation

Written but NOT RUN in the current macOS environment:

- `OutputValidationArtifactSourceTests.Load_WhenWorkspacePrepared_RejectsNonWorkspaceLocalEvidencePath`

Recommended Windows validation commands:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --filter "OutputValidationArtifactSourceTests|OutputValidationDocumentationTests" --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

Manual Windows evidence remains pending:

- Place a JSON artifact in `%LOCALAPPDATA%\Lumiere\validation\output\` with `evidencePaths` pointing at `docs\validation\evidence\...` and confirm Settings > Validation reports a load issue instead of applying the artifact.
- Place a JSON artifact with `evidencePaths` pointing at `evidence\<session>.md` plus a filled markdown note under `%LOCALAPPDATA%\Lumiere\validation\output\evidence\` and confirm it can load.

## Remaining Release Work

This does not complete Story `12-1`. Public release still needs real executed Windows HDR/SDR/mixed-display scenario sessions, filled workspace-local scenario notes, target-app versions, viewer observations, and release-gate judgement.

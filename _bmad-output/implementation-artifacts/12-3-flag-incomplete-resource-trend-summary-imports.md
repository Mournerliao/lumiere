# 12-3 Flag Incomplete Resource Trend Summary Imports

Date: 2026-06-29

## Story

Story `12-3-record-long-run-capture-and-output-resource-trends`

## Context

`Create trend draft` could import sampler `*-summary.json` files and prefer summaries whose PID matches the current Lumiere process. That closed one stale-summary risk, but an imported summary could still point at a missing CSV path or omit primary process coverage. For the Public perfect-HDR-fidelity release gate, a summary JSON without its CSV sample file, sample count, handles, and private bytes is incomplete long-run evidence and must not look ready for pass/fail classification.

## Changes

- Added typed `ResourceTrendEvidencePathStatus` and completeness helpers to `ResourceTrendSummaryArtifact`.
- Marked imported resource-trend summaries with `Present` or `Missing` CSV path status when `FileOutputValidationArtifactSource` scans the workspace-local `resource-trends` folder.
- Updated `ResourceTrendValidationDraftFactory` so drafts with missing/unreadable CSV evidence or incomplete primary process metrics keep:
  - Public gate `Long-run lifecycle evidence`: `NOT RUN`
  - Session classification: `NOT RUN`
  - Known limitations / warm-up notes: explicit evidence-completeness warning
- Preserved the honest boundary for complete imported summaries: telemetry rows can be prefilled, but metric and session judgement still require manual validator review.
- Updated resource-trend workflow docs and the release checklist notes so future validators know that incomplete imports are setup guidance only.

## Validation

Written but NOT RUN in the current macOS environment:

- `ResourceTrendValidationDraftFactoryTests.Create_KeepsSessionNotRunWhenImportedSamplerCsvIsMissing`
- `ResourceTrendValidationDraftFactoryTests.Create_KeepsSessionNotRunWhenImportedSamplerCsvWasNotVerified`
- `OutputValidationArtifactSourceTests.CreateResourceTrendDraft_KeepsNotRunWhenImportedSummaryCsvIsMissing`

Recommended Windows validation commands:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --filter "ResourceTrendValidationDraftFactoryTests|OutputValidationArtifactSourceTests|OutputValidationDocumentationTests" --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

Manual Windows evidence remains pending:

- Create a resource-trend draft with a real sampler summary and retained CSV file under the same validation workspace.
- Create or simulate a draft after removing the CSV file and confirm the draft stays `NOT RUN` with an evidence-completeness warning.
- Execute a focused `50+` or `100+` capture/output cycle run before counting Story `12-3` or the public release gate.

## Remaining Release Work

This does not complete Story `12-3`. Public release still needs executed Windows long-run sessions, retained CSV/summary/log artifacts, cycle notes, and explicit pass/fail/limitation judgement from a validator.

---
title: 'Surface Public Gate Evidence Gaps In Validation Summary'
type: 'feature'
created: '2026-06-23'
status: 'done'
route: 'native-validation-surface'
story: '12-1'
---

# Surface Public Gate Evidence Gaps In Validation Summary

## Intent

The loaded-evidence summary could already tell a reviewer what artifacts were loaded, which viewers were named, and whether target-app versions or build alignment were missing. But it still stopped short of translating that evidence into the next public-release work a validator actually has to run.

This slice makes the summary more execution-oriented: it now shows broader coverage context and calls out which public release checklist groups are still missing from the loaded evidence.

## Delivered In This Slice

1. Validation evidence coverage now also summarizes:
   - entry points tested
   - DPI scales
   - display setup coverage
   - HDR state coverage
2. Validation evidence gaps now also call out missing public-fidelity checklist groups for:
   - target-aware HDR
   - viewer/output matrix
   - export-profile accessibility and DPI
   - long-run lifecycle
3. The new gap detail stays evidence-first: it does not invent a pass/fail dashboard, but it does reduce the amount of manual cross-checking between loaded JSON and the release checklist.
4. Tests now cover the richer coverage detail and the new public-gate gap messaging.

## Review Pointers

1. [PerfectHdrFidelityProjection.cs](../../src/Lumiere.App.Core/PerfectHdrFidelityProjection.cs)
2. [PerfectHdrFidelityProjectionTests.cs](../../tests/Lumiere.Graphics.Tests/App/PerfectHdrFidelityProjectionTests.cs)
3. [SettingsPanelProjectionTests.cs](../../tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs)
4. [output-validation.md](../../harness/validation/output-validation.md)

## Validation

- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --filter "PerfectHdrFidelityProjectionTests|SettingsPanelProjectionTests" --no-restore --verbosity minimal /nr:false`
- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 -p:UseSharedCompilation=false --no-restore --verbosity minimal /nr:false`

## Remaining Work

Story `12-1` remains `in-progress`.

Remaining release-blocking work:

- Record real Windows sessions that actually close those checklist gaps.
- Backfill missing topology, viewer/output, accessibility, and long-run evidence instead of only surfacing that it is absent.

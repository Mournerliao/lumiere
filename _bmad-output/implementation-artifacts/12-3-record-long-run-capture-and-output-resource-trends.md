---
title: 'Record Long Run Capture And Output Resource Trends'
type: 'feature'
created: '2026-06-22'
status: 'in-progress'
route: 'validation-asset'
story: '12-3'
---

# Record Long-Run Capture And Output Resource Trends

## Intent

`Public perfect-HDR-fidelity` still lacked a repeatable long-run validation workflow for resource stability. The release checklist already required private bytes, handles, and GPU resource trends, but there was no committed sampler, no standard run shape, and no reusable session artifact for another tester to pick up.

This slice turns Story `12-3` from a vague future validation need into concrete repo assets that can drive real Windows manual evidence runs.

## Delivered In This Slice

1. Added [resource trend sampler script](../../harness/validation/scripts/collect-resource-trend-samples.ps1) for repeated-process and GPU memory sampling.
2. Added [Resource Trend Validation](../../harness/validation/resource-trend-validation.md) as the standard workflow for `50+` and `100+` capture/output runs.
3. Added [Resource Trend Session Template](../../harness/validation/templates/resource-trend-session-template.md) so future testers can record cycle plans, sampler commands, metric summaries, and release classification consistently.
4. Updated [Lifecycle Validation](../../harness/validation/lifecycle-validation.md), [Release Validation Checklist](../../harness/validation/release-validation-checklist.md), and [Validation Index](../../harness/validation/index.md) so Story `12-3` is wired into the live validation surface instead of living only in planning text.
5. Extended the sampler output summary to report thread deltas alongside the existing process and GPU metrics.

## Suggested Review Order

1. [Sampler script](../../harness/validation/scripts/collect-resource-trend-samples.ps1)
2. [Resource Trend Validation](../../harness/validation/resource-trend-validation.md)
3. [Resource Trend Session Template](../../harness/validation/templates/resource-trend-session-template.md)
4. [Release Validation Checklist](../../harness/validation/release-validation-checklist.md)

## Validation

- Smoke-tested the sampler against a live PowerShell process using a short local run:

```powershell
& .\harness\validation\scripts\collect-resource-trend-samples.ps1 `
  -ProcessId $PID `
  -DurationSeconds 10 `
  -SampleIntervalSeconds 1 `
  -OutputDirectory "$env:TEMP\lumiere-resource-trend-smoke"
```

## Remaining Work

Story `12-3` is still `in-progress`, not `done`.

Remaining follow-up:

- Run at least one real Windows `50+` cycle regression sweep against `Lumiere.App`.
- Run at least one real Windows `100+` cycle release-candidate session for the intended public release configuration.
- Record actual pass/fail/limitation evidence rather than templates only.
- Decide whether any observed resource drift is a blocker, a documented limitation, or an accepted risk.

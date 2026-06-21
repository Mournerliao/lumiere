---
title: 'Establish Standard HDR SDR Validation Content And Scenarios'
type: 'feature'
created: '2026-06-22'
status: 'in-progress'
route: 'validation-asset'
story: '12-1'
---

# Establish Standard HDR / SDR Validation Content And Scenarios

## Intent

`Public perfect-HDR-fidelity` still lacked a standard, repeatable Windows validation scenario set. The release checklist already named the gate categories, but another agent still had to invent the actual content families, topology buckets, and session-record structure every time.

This slice turns Story 12-1 into concrete repo assets so future Windows manual validation can run from the same scenario vocabulary.

## Delivered In This Slice

1. Added [HDR / SDR Validation Scenarios](../../harness/validation/hdr-sdr-validation-scenarios.md) as the standard manual-validation scenario set for Public perfect-HDR-fidelity.
2. Added [Settings Accessibility Validation](../../harness/validation/settings-accessibility-validation.md) so Story 13-2 has a focused Windows validation workflow instead of relying on only the top-level checklist rows.
3. Added [HDR / SDR Validation Session Template](../../harness/validation/templates/hdr-sdr-validation-session-template.md) so hardware, Windows version, GPU, displays, DPI, target apps, and observed results can be recorded consistently.
4. Updated the live [Release Validation Checklist](../../harness/validation/release-validation-checklist.md) and [Validation Index](../../harness/validation/index.md) to point to the new detailed workflows.

## Suggested Review Order

1. [HDR / SDR Validation Scenarios](../../harness/validation/hdr-sdr-validation-scenarios.md)
2. [Settings Accessibility Validation](../../harness/validation/settings-accessibility-validation.md)
3. [HDR / SDR Validation Session Template](../../harness/validation/templates/hdr-sdr-validation-session-template.md)
4. [Release Validation Checklist](../../harness/validation/release-validation-checklist.md)

## Why This Matters

Story 12-1 requires repeatable validation, not just a list of goals. These assets make it possible for another agent or tester to:

- choose comparable bright, dark, mixed, browser/media/game, and output-target scenarios
- record the same session metadata every time
- tie settings accessibility checks back to the public-fidelity release gate

## Remaining Work

Story 12-1 is still `in-progress`, not `done`.

Remaining follow-up:

- Run real Windows manual sessions using the new scenario set and template.
- Commit actual evidence files for at least one baseline HDR, SDR, and mixed-display run.
- Backfill named target-app versions and observed results into real session artifacts rather than templates only.

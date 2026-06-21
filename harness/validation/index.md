# Validation Index

Updated: 2026-06-22

This folder contains Lumiere's current validation records, release-gate checklists, supporting workflows, and historical validation snapshots. The live release decision artifact is `release-validation-checklist.md`.

## Live Gate

- [Release Validation Checklist](release-validation-checklist.md) - Active checklist for Public perfect-HDR-fidelity.

## Active Supporting Workflows

- [Lifecycle Validation](lifecycle-validation.md) - Capture cycles, teardown, and resource trend validation.
- [Overlay Validation](overlay-validation.md) - Overlay placement, crop interaction, cancel, DPI, and display validation.
- [Output Validation](output-validation.md) - Clipboard/file output checks and future output profile acceptance records.

## Historical Snapshots

- [Foundation Validation Snapshot (2026-06-03)](history/foundation-validation-snapshot-2026-06-03.md) - Story 8.5 point-in-time validation snapshot.
- [Foundation Validation Registry (2026-06-03)](history/foundation-validation-registry-2026-06-03.md) - Capability validation levels across Epic 4 through Epic 8.

## Status Rules

- Use `PASS`, `PASS with limitation`, `FAIL`, `NOT RUN`, or `N/A`.
- `NOT RUN` never counts as evidence.
- Public HDR fidelity claims require the gates in [Release Validation Checklist](release-validation-checklist.md).
- Historical story artifacts may use older wording or stage labels. Current release decisions should use this index and the live checklist.

# Validation Index

Updated: 2026-06-21

This folder contains Lumiere's validation records and release-gate checklists. The live release decision artifact is `release-validation-checklist.md`; older MVP documents are point-in-time snapshots unless explicitly updated.

## Live Gate

- [Release Validation Checklist](./release-validation-checklist.md) - Active checklist for Private Preview / Early Validation and Public Perfect HDR Fidelity Release.

## Point-In-Time Records

- [MVP Release Validation Matrix](./mvp-release-validation-matrix.md) - Story 8.5 snapshot from 2026-06-03.
- [MVP Validation Registry](./mvp-validation-registry.md) - Capability validation levels across Epic 4 through Epic 8.

## Focused Validation Workflows

- [Lifecycle Validation](./lifecycle-validation.md) - Capture cycles, teardown, and resource trend validation.
- [Overlay Validation](./overlay-validation.md) - Overlay placement, crop interaction, cancel, DPI, and display validation.
- [Output Validation](./output-validation.md) - Clipboard/file output checks and future output profile acceptance records.

## Status Rules

- Use `PASS`, `PASS with limitation`, `FAIL`, `NOT RUN`, or `N/A`.
- `NOT RUN` never counts as evidence.
- Public HDR fidelity claims require the public gates in [Release Validation Checklist](./release-validation-checklist.md), not only MVP foundation validation.
- Historical story artifacts may use older "early user" wording. Current release decisions should use this index and the live checklist.

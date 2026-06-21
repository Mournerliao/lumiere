# Project Documentation Index

Updated: 2026-06-21

This is the primary project-knowledge entry point for BMAD and agent-assisted development. Planning authority lives in `_bmad-output/planning-artifacts/`; this folder stores durable project validation and operational documentation.

## Quick Reference

- **Project:** Lumiere
- **Type:** Native Windows desktop HDR screenshot tool
- **Runtime:** .NET 10 / WinUI 3 / Windows App SDK
- **Graphics stack:** Windows Graphics Capture, Direct3D 11, DXGI, Vortice
- **Release model:** staged release
- **Current public target:** Perfect HDR Fidelity Public Release

## Release Gates

- **MVP Foundation / Private Preview:** Epic 1-9 provide the capture and workflow foundation. Private preview still requires recorded Windows validation and documented limitations.
- **Perfect HDR Fidelity Public Release:** Epic 10+ must provide target-aware HDR detection, output fidelity contracts, target-app compatibility evidence, HDR/SDR validation content, multi-monitor/DPI coverage, long-run lifecycle evidence, visual-match output evidence, at least one HDR-preserved supported output path, and release copy review.

## Validation Documentation

- [Validation Index](./validation/index.md) - Validation documents and release-gate map.
- [Release Validation Checklist](./validation/release-validation-checklist.md) - Live release-gate checklist for private preview and public HDR fidelity.
- [MVP Release Validation Matrix](./validation/mvp-release-validation-matrix.md) - 2026-06-03 MVP foundation validation snapshot.
- [MVP Validation Registry](./validation/mvp-validation-registry.md) - Capability validation levels and gap inventory.
- [Lifecycle Validation](./validation/lifecycle-validation.md) - Repeated capture, teardown, and resource checks.
- [Overlay Validation](./validation/overlay-validation.md) - Overlay, crop, cancel, DPI, and display checks.
- [Output Validation](./validation/output-validation.md) - Clipboard/file output scope and output profile acceptance record.

## Related Planning Artifacts

- [`_bmad-output/planning-artifacts/prd.md`](../_bmad-output/planning-artifacts/prd.md) - Product requirements and release scope.
- [`_bmad-output/planning-artifacts/epics.md`](../_bmad-output/planning-artifacts/epics.md) - MVP foundation epics and public fidelity epics.
- [`_bmad-output/planning-artifacts/architecture.md`](../_bmad-output/planning-artifacts/architecture.md) - Architecture boundaries and implementation priorities.
- [`_bmad-output/planning-artifacts/ux-design-specification.md`](../_bmad-output/planning-artifacts/ux-design-specification.md) - UX state, copy, and validation rules.
- [`_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-21-perfect-hdr-fidelity-release-target.md`](../_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-21-perfect-hdr-fidelity-release-target.md) - Prior course correction that established fidelity evidence gates.

## Harness References

- [`harness/planning/project-plan.md`](../harness/planning/project-plan.md) - Chinese product and implementation direction.
- [`harness/planning/mvp-feature-list.md`](../harness/planning/mvp-feature-list.md) - MVP feature scope.
- [`harness/design/index.md`](../harness/design/index.md) - UX references and prototype boundary.
- [`harness/workflows/cross-platform-development.md`](../harness/workflows/cross-platform-development.md) - Mac-edit / Windows-validate workflow.

## Agent Rules

1. Do not treat MVP completion as public release readiness.
2. Do not claim HDR preservation unless target-aware detection, output semantics, compatibility evidence, and Windows manual validation exist for that path.
3. Keep "copied", "saved", "converted", and "HDR-preserved" as separate claims.
4. Use [Release Validation Checklist](./validation/release-validation-checklist.md) before drafting release copy or deciding release readiness.

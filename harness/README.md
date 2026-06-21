# Lumiere Harness

Updated: 2026-06-22

This directory is the durable documentation root for Lumiere: long-lived context, reusable guidance, release validation records, and architecture decisions that agents and humans should keep following across implementation sessions.

Use `_bmad-output/` for generated planning artifacts, sprint output, story drafts, and stage-specific reports. Promote only stable, reusable guidance into this `harness/` directory.

## Quick Reference

- **Project:** Lumiere
- **Type:** Native Windows desktop HDR screenshot tool
- **Runtime:** .NET 10 / WinUI 3 / Windows App SDK
- **Graphics stack:** Windows Graphics Capture, Direct3D 11, DXGI, Vortice
- **Release model:** staged release
- **Current public target:** Public perfect-HDR-fidelity

## Current Release Direction

- **Current target:** Public perfect-HDR-fidelity
- **Current baseline:** Epic 1-9 provide the implemented capture and workflow baseline that the public-fidelity release continues to build on.
- **Public release evidence:** Epic 10+ must provide target-aware HDR detection, output fidelity contracts, target-app compatibility evidence, HDR/SDR validation content, multi-monitor/DPI coverage, long-run lifecycle evidence, visual-match output evidence, at least one HDR-preserved supported output path, and release copy review.

## Current Plan

- `planning/project-plan.md` - long-lived product intent, architecture direction, and implementation phases.
- `planning/current-feature-baseline.md` - current feature baseline distilled from the imported v0 reference.

## Current Design

- `design/index.md` - durable UX reference index and design entrypoint.
- `design/perfect-hdr-fidelity-extension.md` - design supplement for target-aware fidelity states, output profiles, validation evidence, and public-release copy boundaries.

## Current Validation

- `validation/index.md` - validation documents, release-gate checklists, and evidence workflow entrypoint.
- `validation/release-validation-checklist.md` - live release-gate checklist for Public perfect-HDR-fidelity.
- `validation/output-validation.md` - output-scope rules, validation artifact loading, and output profile acceptance record.

## History And Prototype Assets

- `design/prototype/v0-public-fidelity-reference/` - runnable React design prototype for the current public-fidelity direction.
- `validation/history/` - historical validation registries and point-in-time snapshots.
- `architecture/adr/0001-perfect-hdr-fidelity-public-release-is-fixed-target.md` - fixed public release target decision.
- `architecture/adr/0002-perfect-hdr-fidelity-design-extends-v0-reference.md` - design-extension-over-replacement decision.

## Supporting Guidance

- `skills/` - project-specific skills for AI-assisted development.
  - `winui-gallery-reference/` - WinUI 3 component reference skill for fetching official code examples.
- `workflows/cross-platform-development.md` - supported macOS editing, Windows CI, and Windows hardware validation workflow.
- `workflows/nuget-restore-recovery.md` - local `dotnet run` restore guidance and `NETSDK1064` cache recovery.
- `matt-pocock-skills-guide.md` - usage guide for third-party skills from mattpocock/skills.

## Related Planning Artifacts

- [`_bmad-output/planning-artifacts/prd.md`](../_bmad-output/planning-artifacts/prd.md) - product requirements and release scope.
- [`_bmad-output/planning-artifacts/epics.md`](../_bmad-output/planning-artifacts/epics.md) - implementation epics and public fidelity epics.
- [`_bmad-output/planning-artifacts/architecture.md`](../_bmad-output/planning-artifacts/architecture.md) - architecture boundaries and implementation priorities.
- [`_bmad-output/planning-artifacts/ux-design-specification.md`](../_bmad-output/planning-artifacts/ux-design-specification.md) - UX state, copy, and validation rules.

## Conventions

- Keep harness documents focused on durable guidance, not transient task notes.
- Prefer lowercase kebab-case file names for new harness documents.
- Add new top-level harness folders only when there is real content to place in them.
- Update this index whenever a durable harness document is added, moved, or removed.
- Use `validation/` for current live validation docs and templates.
- Use `validation/history/` for historical validation registries and snapshots.
- Use `architecture/adr/` for durable architecture and product-decision records.


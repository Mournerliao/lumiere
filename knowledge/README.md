# Lumiere Knowledge Index

`knowledge/` is Lumiere's long-lived project knowledge base. Use it to preserve product intent, engineering boundaries, validation evidence, and architectural decisions as the project evolves.

This index is the default entry point for agents and maintainers. It should route readers to the right source of truth without duplicating the full content of each document.

## Start Here

Read in this order when starting unfamiliar work:

1. [MVP product scope](product/mvp.md) for the current product goal, public claim boundary, and success criteria.
2. [Architecture](engineering/architecture.md) for module ownership, platform boundaries, and HDR invariants.
3. [Engineering workflows](engineering/workflows.md) for local development, Windows validation, and NuGet recovery.
4. [MVP validation checklist](validation/mvp-checklist.md) for release evidence and required manual checks.

Then read [Product roadmap](product/roadmap.md), [MVP development plan](engineering/mvp-development-plan.md), and the relevant decision records when the task touches future scope, delivery sequencing, or previously settled tradeoffs.

## Task Routing

| Task or question | Start with |
|---|---|
| Product scope, MVP boundaries, public claims, success criteria | [product/mvp.md](product/mvp.md) |
| Roadmap, future HDR-preserved export, non-goals | [product/roadmap.md](product/roadmap.md) |
| Module boundaries, platform ownership, HDR invariants | [engineering/architecture.md](engineering/architecture.md) |
| Local development flow, Windows validation, NuGet recovery | [engineering/workflows.md](engineering/workflows.md) |
| MVP implementation sequencing and phase status | [engineering/mvp-development-plan.md](engineering/mvp-development-plan.md) |
| HDR terminology, output semantics, JPEG XR boundary | [validation/hdr-notes.md](validation/hdr-notes.md) |
| Release readiness, manual validation, target app evidence | [validation/mvp-checklist.md](validation/mvp-checklist.md) |
| Accepted architectural or product tradeoffs | [decisions/](decisions/) |

## Decision Records

- [0001: MVP-First HDR-Aware Release](decisions/0001-mvp-first-hdr-aware-release.md)
- [0002: sRGB Visual Match As MVP Output](decisions/0002-srgb-visual-match-as-mvp-output.md)
- [0003: Shared sRGB Visual Match Conversion](decisions/0003-shared-srgb-visual-match-conversion.md)
- [0004: Traditional Windows Setup Installer For MVP](decisions/0004-traditional-windows-setup-installer.md)

## Maintenance

- Update this index whenever adding, renaming, moving, or retiring a knowledge document.
- Keep this file as a routing layer. Link to source documents instead of copying their detailed content here.
- If a task changes product claims, module boundaries, validation expectations, or settled tradeoffs, update the relevant source document and decision record together.

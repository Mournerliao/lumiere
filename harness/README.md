# Lumiere Harness

This directory is the durable project harness for Lumiere: long-lived context, reusable guidance, and norms that agents and humans should keep following across implementation sessions.

Use `_bmad-output/` for generated planning artifacts, sprint output, story drafts, and stage-specific reports. Promote only stable, reusable guidance into this `harness/` directory.

## Directory Map

- `planning/project-plan.md` - long-lived product intent, architecture direction, and implementation phases.
- `planning/mvp-feature-list.md` - MVP feature checklist distilled from the imported v0 reference.
- `design/index.md` - durable UX reference index, including the imported v0 MVP reference.
- `skills/` - project-specific skills for AI-assisted development.
  - `winui-gallery-reference/` - WinUI 3 component reference skill for fetching official code examples.
- `workflows/cross-platform-development.md` - supported macOS editing, Windows CI, and Windows hardware validation workflow.

## Conventions

- Keep harness documents focused on durable guidance, not transient task notes.
- Prefer lowercase kebab-case file names for new harness documents.
- Add new top-level harness folders only when there is real content to place in them.
- Update this index whenever a durable harness document is added, moved, or removed.

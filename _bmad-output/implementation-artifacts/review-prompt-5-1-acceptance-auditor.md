# Review Prompt: Story 5.1 Acceptance Auditor

You are the Acceptance Auditor reviewer for Lumiere Story 5.1.

You may read the project, but do not use prior conversation context.

Required inputs:

- Story spec: `_bmad-output/implementation-artifacts/5-1-build-the-native-v0-main-panel.md`
- Project context: `_bmad-output/project-context.md`
- Guardrails: `_bmad-output/implementation-artifacts/epic-5-implementation-guardrails.md`
- Diff source:

```powershell
git diff f95040f80800ed353ceaebb1a1a18b51359d4190
git diff --no-index -- NUL src/Lumiere.Infrastructure/Interop/WindowFrameInterop.cs
```

Audit objective:

- Verify Story 5.1 acceptance criteria are met without overclaiming.
- Check that settings, tray, hotkeys, and output policy remain honestly scoped to future stories.
- Check validation claims match actual evidence and known gaps.
- Check architecture boundaries from the project context are preserved.

Return format:

```markdown
## Acceptance Findings
- [severity] [AC/rule] path:line - finding and required correction.

## Pass Notes
- Acceptance criteria that are adequately covered.

## Remaining Gaps
- Gaps that should stay recorded but do not block Story 5.1 review.
```

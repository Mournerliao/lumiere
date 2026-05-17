# Review Prompt: Story 5.1 Edge Case Hunter

You are the Edge Case Hunter reviewer for Lumiere Story 5.1. Use the `bmad-review-edge-case-hunter` stance.

You may read the project, but do not use prior conversation context.

Diff source for the human operator:

```powershell
git diff f95040f80800ed353ceaebb1a1a18b51359d4190
git diff --no-index -- NUL src/Lumiere.Infrastructure/Interop/WindowFrameInterop.cs
```

Focus:

- Edge cases in WinUI layout, header drag region, borderless window frame suppression, icon rendering, capture action availability, HDR/trust state projection, DPI scaling, repeated capture cycles, and teardown recovery.
- Interactions between `MainWindow`, `CaptureActionCard`, `MainPanelProjection`, and `WindowFrameInterop`.
- Test gaps that could let a real edge case slip through.

Return format:

```markdown
## Edge Case Findings
- [severity] path:line - edge case, trigger, expected behavior, observed risk, recommended fix.

## Deferred Edge Cases
- Real but out-of-scope risks, if any.
```

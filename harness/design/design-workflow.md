# Lumiere Design Workflow

Use this workflow when designing or reviewing Lumiere UI/UX. The goal is to combine the existing BMAD process, Impeccable's design critique, and Microsoft native Windows guidance without turning Lumiere into a web app.

## Tool Order

1. Start with project context.
   - Read `README.md`, `harness/README.md`, and `harness/design/index.md`.
   - Use `harness/planning/project-plan.md` for durable product and architecture intent when it is readable in the current environment.

2. Shape UX with BMAD.
   - Use `$bmad-create-ux-design` for a new UX specification.
   - Use `$bmad-agent-ux-designer` when the task needs an ongoing UX specialist perspective.
   - Use `$bmad-checkpoint-preview` to guide human review of a UI or prototype change.
   - Use `$bmad-advanced-elicitation` for red-team critique, first-principles review, or pre-mortem analysis.

3. Ground implementation choices in Microsoft references.
   - Use Fluent and WinUI guidance for controls, patterns, layout, materials, accessibility, and motion.
   - Use WinUI Gallery examples as implementation references for XAML controls and adaptive UI behavior.

4. Use Impeccable as a review layer.
   - Use `$impeccable critique`, `$impeccable polish`, `$impeccable harden`, `$impeccable clarify`, or `$impeccable audit` for UI quality checks.
   - Treat Impeccable as a design language and anti-pattern reviewer, not as the visual source of truth.
   - Before using Impeccable, run:

     ```powershell
     node .agents/skills/impeccable/scripts/load-context.mjs
     ```

   - The loader should resolve `PRODUCT.md` and `DESIGN.md` from the repository root.

## Impeccable Boundaries

Use Impeccable for:

- Visual hierarchy review.
- Cognitive load and information architecture critique.
- UX writing for permissions, settings, empty states, and failure states.
- Anti-pattern detection: generic SaaS layouts, nested cards, decorative gradients, weak contrast, unclear focus states.
- Hardening around text overflow, keyboard usage, accessibility, responsive prototype behavior, and edge cases.

Do not use Impeccable to:

- Replace WinUI 3, Windows App SDK, Fluent, or native Windows control patterns.
- Introduce web UI, Electron, Tauri, WPF bitmap-first, WinForms, GDI, cloud upload, telemetry, or SDR screenshot-library foundations.
- Convert Lumiere into a marketing site, dashboard SaaS, or decorative web app.
- Claim HDR correctness without Windows validation.

## Design Artifact Rules

- Store durable UX guidance in `harness/design/`.
- Keep generated, stage-specific planning output in `_bmad-output/`.
- Treat `interactive-prototype/` as design reference only; do not copy web-specific CSS or layout code into WinUI.
- Update `harness/design/index.md` whenever a durable design document is added, moved, or removed.

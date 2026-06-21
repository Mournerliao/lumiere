# PRODUCT.md

## Product

Lumiere is a native Windows desktop screenshot tool focused on HDR-correct capture and preview. It is built on WinUI 3, Windows App SDK, Windows Graphics Capture, Direct3D 11, DXGI, and Vortice.

## Register

product

## Users

- Windows users who need fast, reliable screenshots during normal desktop work.
- Designers, engineers, technical writers, and reviewers who care about pixel accuracy, HDR behavior, and repeatable capture workflows.
- Power users with HDR displays and multi-monitor setups who need a tool that does not flatten or misrepresent HDR content.

## Product Purpose

Lumiere should make screenshot capture feel native, precise, and trustworthy on Windows. The main product promise is not decoration or cloud sharing; it is accurate capture, accurate preview, clear export behavior, and a low-interruption workflow.

The current public release target is Perfect HDR Fidelity Public Release. The MVP capture loop may be used as an internal/private preview foundation, but public release claims require target-aware HDR detection, documented output fidelity semantics, target-app compatibility evidence, Windows manual validation, visual-match output, and at least one HDR-preserved supported output path.

## Strategic Principles

- Native Windows first: preserve WinUI 3, Windows App SDK, WGC, D3D11, DXGI, and Vortice boundaries.
- HDR trust first: avoid claims of HDR correctness unless the pipeline has the matching validation level.
- Public fidelity requires evidence: "perfect HDR fidelity" means supported capture/preview/output paths have explicit fidelity contracts and recorded Windows validation, not a universal guarantee for every device, app, or format.
- Fast capture first: the overlay and capture flow must not slow the user's original task.
- Local tool first: no cloud upload, telemetry, Electron, Tauri, WPF bitmap-first, WinForms, GDI, web UI, or SDR screenshot-library foundations.
- Expert power, calm surface: advanced HDR and output controls should exist without overwhelming first-time capture.

## Tone

Calm, precise, professional, native, and concise. The UI should sound like a reliable Windows tool, not a marketing site.

## Anti-References

- Generic SaaS dashboards.
- Purple-blue gradient AI-tool aesthetics.
- Web landing pages with oversized heroes.
- Nested cards and decorative card grids.
- Unverified HDR claims.
- Cloud-sharing-first screenshot tools.
- Decorative motion that delays capture or confirmation.

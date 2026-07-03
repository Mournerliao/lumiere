# PRODUCT.md

## Product

Lumiere is a native Windows desktop screenshot tool focused on HDR-aware capture and preview. It is built on WinUI 3, Windows App SDK, Windows Graphics Capture, Direct3D 11, DXGI, and Vortice.

## Register

product

## Users

- Windows users who need fast, reliable screenshots during normal desktop work.
- Designers, engineers, technical writers, and reviewers who care about screenshots that do not obviously misrepresent HDR content.
- Power users with HDR displays and multi-monitor setups who need honest capability boundaries.

## Product Purpose

Lumiere should make screenshot capture feel native, precise, and trustworthy on Windows. The first release target is an HDR-aware MVP: fast capture, clear preview/status behavior, compatible output, and no unsupported HDR-preserved claims.

HDR-preserved export remains a future milestone. It must be scoped to a named output path with documented format/conversion/metadata semantics, target-app assumptions, and Windows manual validation before it becomes public product language.

## Strategic Principles

- Native Windows first: preserve WinUI 3, Windows App SDK, WGC, D3D11, DXGI, and Vortice boundaries.
- MVP first: ship the core screenshot workflow before broad HDR-preservation certification.
- HDR honesty first: avoid claims of HDR preservation unless the selected output path has matching validation evidence.
- Fast capture first: the overlay and capture flow must not slow the user's original task.
- Local tool first: no cloud upload, telemetry, Electron, Tauri, WPF bitmap-first, WinForms, GDI, web UI, or SDR screenshot-library foundations.
- Calm surface: advanced HDR risks should be handled in copy and docs without overwhelming first-time capture.

## Tone

Calm, precise, professional, native, and concise. The UI should sound like a reliable Windows tool, not a marketing site.

## Anti-References

- Generic SaaS dashboards.
- Purple-blue gradient AI-tool aesthetics.
- Web landing pages with oversized heroes.
- Nested cards and decorative card grids.
- Unverified HDR-preserved claims.
- Cloud-sharing-first screenshot tools.
- Decorative motion that delays capture or confirmation.

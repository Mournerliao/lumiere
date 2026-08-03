# Product Contract

## Goal

Ship a fast, native, and honest Windows screenshot tool. The MVP supports region
and fullscreen capture, native HDR-aware preview, and practical sRGB output for
clipboard and folder workflows without claiming unsupported HDR preservation.

## Users

- Windows users who need fast, reliable screenshots during normal desktop work.
- Designers, engineers, writers, and reviewers who care about credible HDR rendering.
- Power users with HDR and mixed-monitor setups who need honest capability boundaries.

## MVP Scope

- Main-window, tray, and configured-shortcut capture entry points.
- Region capture with drag-to-select and release-to-capture behavior.
- Fullscreen capture for the active target.
- FP16/scRGB-oriented native capture and preview direction.
- Clipboard, folder, and both-target output.
- Local output, path, timestamp, shortcut, after-capture, and HDR-alert settings.
- Target-aware HDR readiness states: ready, unavailable, degraded, or unvalidated.
- One official MVP output path: **sRGB Visual Match**.
- A traditional Windows setup executable with custom install-path support.

## Product Language

- **HDR-aware MVP** — native capture and preview with honest HDR state; not certification.
- **Compatible output** — useful output for common Windows consumers; possibly SDR-compatible.
- **sRGB Visual Match** — FP16/scRGB converted to an SDR-compatible sRGB artifact,
  tuned to avoid obvious washed-out, gray, or blown-out HDR output.
- **Default Visual Match Conversion** — fixed, non-user-adjustable MVP conversion.
- **Artifact success** — the artifact was copied or saved; no fidelity implication.
- **Target-app limitation** — processing introduced by a receiving app after Lumiere
  produced a valid artifact; never an excuse for a Lumiere output defect.
- **HDR-preserved export** — a future named path with documented format, color,
  metadata, viewer assumptions, and real Windows evidence.

## Out Of Scope For MVP

- Broad or universal HDR-preserved export guarantees.
- P3 or HDR10 as normal supported user-selectable modes.
- General image editing, annotations, gallery/history, cloud upload, or sharing.
- Onboarding, telemetry, or non-native production UI stacks.
- Universal display-topology, viewer, DPI, accessibility, or long-run certification.

## Definition Of Product Success

- The app launches, captures repeatedly, cancels, and exits cleanly on Windows.
- Region and fullscreen capture do not leave stale overlay or capture state.
- Clipboard and folder outputs are usable in named common Windows consumers.
- Supported HDR scenes avoid blocking visual failures defined by the claims contract.
- HDR status does not claim more than Lumiere can prove for the active target.
- The MVP installs, launches, upgrades, uninstalls, and reinstalls on a clean machine.

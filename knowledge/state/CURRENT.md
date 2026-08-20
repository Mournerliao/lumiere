# Current Project State

- Updated: 2026-08-20
- Current milestone: 0 — Cross-platform foundation
- Release target: Windows + macOS HDR-aware MVP with sRGB Visual Match
- Active work control: [GitHub Issue #1](https://github.com/Mournerliao/lumiere/issues/1)
- Operating model: Contract → Frontier → Verification

## Current Position

The repository has converged on the final `apps/`, `protocol/`, `hosts/`, and
`knowledge/` ownership layout. Lumiere has an Electron/React shared shell, a
language-neutral platform-host v1 schema with executable fixtures, explicit unavailable
behavior, dual-platform CI, and isolated native host trees.

The shell does not capture yet. Both capture actions intentionally remain disabled
until a native host is connected. Windows development is paused with only the WGC,
D3D11/DXGI, HDR-aware acquisition, sRGB Visual Match, delivery, interop, and resource
lifecycle engine retained. WinUI and the old validation/HDR10-JXR paths are removed.
The Swift ScreenCaptureKit host has not been implemented.

## Progress At A Glance

| Roadmap slice | Current state | What is true now | What remains before exit |
|---|---|---|---|
| 0. Foundation | Implemented locally; Windows CI pending | Final layout, secure shell, language-neutral protocol, paused Windows engine; macOS checks pass | Pass Windows CI, then close Issue #1 |
| 1A. macOS native capture | Not started | ScreenCaptureKit direction and interface ownership are decided | Permission flow, native host, HDR capability, display capture, sRGB file verification |
| 1B. Windows host adapter | Paused | Three clean engine modules remain; no Windows executable exists | Resume with host process/interface adapter and Windows runtime verification |
| 1C. Shared product surface | Shell placeholder only | Main capture surface renders and unavailable state is honest | Region overlay, display flow, tray/menu bar, shortcut, settings, clipboard/folder/both |
| 1D. Distribution and release | Not started | Windows installer direction remains recorded | Windows installer, signed/notarized macOS artifact, clean-machine and hardware verification |
| 2. HDR-preserved export | Planned; not started | Claim gate and milestone are defined | Choose format/viewers, specify semantics, implement and verify both platforms |
| 3. Cross-platform HDR fidelity | Planned; not started | Fidelity is separated from artifact success and HDR preservation | Define support matrix/tolerances and run fixed-scene verification |

This table is a project posture snapshot, not a task ledger. GitHub Issues own
acceptance criteria and implementation status for each vertical slice.

## Verification Truth

- **Repository:** frozen install, layout and TypeScript checks, five shell/protocol tests,
  and the production build pass locally on macOS.
- **macOS shell:** reports `macos host unavailable`; native capture and HDR behavior are unverified.
- **GitHub CI:** not run for the convergence changes because the branch has not been pushed.
- **Windows verified:** no passing Electron-host integration or release-candidate record exists.
- **Hardware verified:** no passing sRGB Visual Match observation exists for either platform.

Do not describe the foundation as working capture, or the cross-platform MVP as
release-ready until both owning platforms are explicitly verified.

## Frontier

Finish Milestone 0 by publishing the branch and passing both CI workflows. The next
implementation frontier is Milestone 1A: one macOS vertical slice from ScreenCaptureKit
permission and display capture through the platform-host interface to an sRGB Visual
Match file. Open that acceptance-ready Issue only after Issue #1 is complete.

# Current Project State

- Updated: 2026-08-21
- Current milestone: 1A — macOS native capture
- Release target: Windows + macOS HDR-aware MVP with sRGB Visual Match
- Completed foundation record: [GitHub Issue #1](https://github.com/Mournerliao/lumiere/issues/1)
- Operating model: Contract → Frontier → Verification

## Current Position

The repository has converged on the final `apps/`, `protocol/`, `hosts/`, and
`knowledge/` ownership layout. Lumiere has an Electron/React shared shell, a
language-neutral platform-host v1 schema with executable fixtures, explicit unavailable
behavior, dual-platform CI, and isolated native host trees.

The macOS development shell now drives a Swift ScreenCaptureKit host through the
platform-host v1 JSON Lines process interface. Display capture with folder delivery
produces an RGBA8/sRGB PNG; region capture remains disabled. Windows development is
paused with only the WGC, D3D11/DXGI, HDR-aware acquisition, sRGB Visual Match,
delivery, interop, and resource lifecycle engine retained. WinUI and the old
validation/HDR10-JXR paths are removed.

## Progress At A Glance

| Roadmap slice | Current state | What is true now | What remains before exit |
|---|---|---|---|
| 0. Foundation | Complete | Final layout, secure shell, language-neutral protocol, paused Windows engine; macOS and Windows CI pass | None |
| 1A. macOS native capture | In progress ([#4](https://github.com/Mournerliao/lumiere/issues/4)) | Swift host, permission request, SDR/HDR display capture, sRGB PNG file delivery, Electron process integration, fixed bright/dark scene verification on XDR hardware | Review remaining acceptance criteria and prepare the slice for integration |
| 1B. Windows host adapter | Paused | Three clean engine modules remain; no Windows executable exists | Resume with host process/interface adapter and Windows runtime verification |
| 1C. Shared product surface | Shell placeholder only | Main capture surface renders and unavailable state is honest | Region overlay, display flow, tray/menu bar, shortcut, settings, clipboard/folder/both |
| 1D. Distribution and release | Not started | Windows installer direction remains recorded | Windows installer, signed/notarized macOS artifact, clean-machine and hardware verification |
| 2. HDR-preserved export | Planned; not started | Claim gate and milestone are defined | Choose format/viewers, specify semantics, implement and verify both platforms |
| 3. Cross-platform HDR fidelity | Planned; not started | Fidelity is separated from artifact success and HDR preservation | Define support matrix/tolerances and run fixed-scene verification |

This table is a project posture snapshot, not a task ledger. GitHub Issues own
acceptance criteria and implementation status for each vertical slice.

## Verification Truth

- **Repository:** frozen install, layout and TypeScript checks, sixteen shell/protocol/process
  tests, eight Swift protocol/capability/diagnostic tests, and the production build pass
  locally on macOS.
- **macOS development runtime:** the Electron display action drove ScreenCaptureKit to
  RGBA8 PNG files with alpha and an embedded sRGB IEC61966-2.1 profile on an Apple
  Silicon Mac. SDR capture was observed at 2560×1440 on an external display. Native
  HDR acquisition and tone mapping were then observed at 1728×1117 on the built-in
  Retina XDR display through both the host and Electron process boundary. After the
  lifecycle review fixes, the Electron path again produced a 2560×1440 sRGB PNG from
  the display under the pointer; the standalone rebuilt CLI remains a distinct TCC
  identity and returned the expected typed permission failure with correlated logging.
- **GitHub CI:** macOS shell and Windows engine workflows pass on PR #2 at the
  Milestone 0 completion frontier.
- **Windows engine:** restore and build pass with zero warnings and errors; Capture 74,
  Graphics 43, and Interop 31 tests pass; `dotnet format --verify-no-changes` passes.
- **Windows integration:** no passing Electron-host integration or release-candidate record exists.
- **Hardware verified:** the macOS Retina XDR path passed fixed bright and dark scene
  observations. The bright ramp preserved ten distinct grayscale steps from 25 through
  255; the dark ramp preserved ten distinct steps from 0 through 32 and alternating
  19/7 fine detail. No equivalent Windows observation exists.

Do not describe the foundation as working capture, or the cross-platform MVP as
release-ready until both owning platforms are explicitly verified.

## Frontier

Milestone 0 is complete. The active frontier is GitHub Issue #4. The next concrete
action is to review its remaining acceptance criteria, close any implementation or
verification gaps, and prepare the macOS vertical slice for integration without
projecting its result to Windows or release readiness.

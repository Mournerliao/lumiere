# Current Project State

- Updated: 2026-08-25
- Current milestone: 1B — Windows native host adapter
- Release target: Windows + macOS HDR-aware MVP with sRGB Visual Match
- Completed foundation record: [GitHub Issue #1](https://github.com/Mournerliao/lumiere/issues/1)
- Operating model: Contract → Frontier → Verification

## Current Position

The repository has converged on the final `apps/`, `protocol/`, `hosts/`, and
`knowledge/` ownership layout. Lumiere has an Electron/React shared shell, a
language-neutral platform-host v1 schema with executable fixtures, explicit unavailable
behavior, dual-platform CI, and isolated native host trees.

The macOS development shell now drives an integrated Swift ScreenCaptureKit host
through the platform-host v1 JSON Lines process interface. Display capture with folder
delivery produces an RGBA8/sRGB PNG; region capture remains disabled. The Windows
engine now exposes one deep display-capture operation that owns target resolution,
target-aware HDR probing, first-frame acquisition, one sRGB Visual Match conversion,
delivery, cancellation, and teardown. The active frontier remains adapting that engine
to the platform-host process interface. WinUI and the old validation/HDR10-JXR paths
remain removed.

## Progress At A Glance

| Roadmap slice | Current state | What is true now | What remains before exit |
|---|---|---|---|
| 0. Foundation | Complete | Final layout, secure shell, language-neutral protocol, paused Windows engine; macOS and Windows CI pass | None |
| 1A. macOS native capture | Complete ([#4](https://github.com/Mournerliao/lumiere/issues/4), [PR #6](https://github.com/Mournerliao/lumiere/pull/6)) | Swift host, explicit permission/cancellation states, SDR/HDR display capture, sRGB PNG file delivery, Electron process integration, fixed bright/dark scene verification on XDR hardware | None |
| 1B. Windows host adapter | In progress ([#7](https://github.com/Mournerliao/lumiere/issues/7)) | The engine has one narrow capture interface, single-conversion delivery, target-aware multi-adapter HDR probing, and deterministic operation teardown | Implement the JSON Lines Host executable, Electron integration, and Windows runtime verification |
| 1C. Shared product surface | Shell placeholder only | Main capture surface renders and unavailable state is honest | Region overlay, display flow, tray/menu bar, shortcut, settings, clipboard/folder/both |
| 1D. Distribution and release | Not started | Windows installer direction remains recorded | Windows installer, signed/notarized macOS artifact, clean-machine and hardware verification |
| 2. HDR-preserved export | Planned; not started | Claim gate and milestone are defined | Choose format/viewers, specify semantics, implement and verify both platforms |
| 3. Cross-platform HDR fidelity | Planned; not started | Fidelity is separated from artifact success and HDR preservation | Define support matrix/tolerances and run fixed-scene verification |

This table is a project posture snapshot, not a task ledger. GitHub Issues own
acceptance criteria and implementation status for each vertical slice.

## Verification Truth

- **Repository:** frozen install, layout and TypeScript checks, sixteen shell/protocol/process
  tests, ten Swift protocol/capability/permission/diagnostic tests, and the production build pass
  locally on macOS.
- **macOS development runtime:** the Electron display action drove ScreenCaptureKit to
  RGBA8 PNG files with alpha and an embedded sRGB IEC61966-2.1 profile on an Apple
  Silicon Mac. SDR capture was observed at 2560×1440 on an external display. Native
  HDR acquisition and tone mapping were then observed at 1728×1117 on the built-in
  Retina XDR display through both the host and Electron process boundary. After the
  lifecycle review fixes, the Electron path again produced a 2560×1440 sRGB PNG from
  the display under the pointer; the standalone rebuilt CLI remains a distinct TCC
  identity and returned the expected typed permission failure with correlated logging.
- **GitHub CI:** macOS shell and Windows engine workflows pass on PR #6 at the
  Milestone 1A integration frontier.
- **Windows engine:** restore and Release build pass with warnings treated as errors;
  Capture 69, Graphics 42, and Interop 31 tests pass; `dotnet format --verify-no-changes`
  passes.
- **Windows integration:** no passing Electron-host integration or release-candidate record exists.
- **Hardware verified:** the macOS Retina XDR path passed fixed bright and dark scene
  observations. The bright ramp preserved ten distinct grayscale steps from 25 through
  255; the dark ramp preserved ten distinct steps from 0 through 32 and alternating
  19/7 fine detail. No equivalent Windows observation exists.

Do not describe the foundation as working capture, or the cross-platform MVP as
release-ready until both owning platforms are explicitly verified.

## Frontier

Milestone 1A is complete. The active frontier is GitHub Issue #7. The next concrete
action is to add the .NET platform-host v1 executable around the retained Windows
engine, integrate it with Electron, and verify repeat, cancellation, clean exit, and
SDR/HDR-state behavior on the named Windows machine without projecting the result to
distribution or release readiness.

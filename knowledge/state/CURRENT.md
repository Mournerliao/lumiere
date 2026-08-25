# Current Project State

- Updated: 2026-08-25
- Current milestone: 1 — Cross-platform HDR-aware MVP
- Release target: Windows + macOS HDR-aware MVP with sRGB Visual Match
- Completed foundation record: [GitHub Issue #1](https://github.com/Mournerliao/lumiere/issues/1)
- Operating model: Contract → Environment-eligible Frontier → Verification

## Current Position

The repository has converged on the final `apps/`, `protocol/`, `hosts/`, and
`knowledge/` ownership layout. Lumiere has an Electron/React shared shell,
language-neutral platform-host schemas with versioned executable fixtures, explicit
unavailable behavior, dual-platform CI, and isolated native host trees. Protocol v1 is
frozen; v2 now owns target-local region geometry, delivery availability, per-target
delivery outcomes, and the cancellation responsibility boundary.

The compact shared main window now uses the approved generated tokens and drives an
integrated Swift ScreenCaptureKit host through one main-process capture command router
and the platform-host v2 JSON Lines process interface. The router owns capability and
delivery gating, persisted clipboard/folder/both preferences with a clipboard-and-folder
default, in-flight state, per-target result projection, and product failure mapping. The
approved Settings output surface now reads and writes that main-owned preference through
validated task-shaped preload/IPC, updates the main-window summary and next capture
immediately, and restores the value after restart. macOS display capture converts once to a
timestamped RGBA8/sRGB PNG representation and delivers those same bytes through native
clipboard and folder adapters; region capture remains honestly disabled. The Windows
engine now exposes one deep display-capture operation that owns target resolution,
target-aware HDR probing, first-frame acquisition, one sRGB Visual Match conversion,
delivery, cancellation, and teardown. Milestone 1 now
advances through environment-aware execution lanes: Issue #7 owns the Windows Host
adapter, while Issue #9 owns the shared product surface and macOS product journeys.
WinUI and the old validation/HDR10-JXR paths remain removed.

## Progress At A Glance

| Roadmap slice | Current state | What is true now | What remains before exit |
|---|---|---|---|
| 0. Foundation | Complete | Final layout, secure shell, language-neutral protocol, paused Windows engine; macOS and Windows CI pass | None |
| 1A. macOS native capture | Complete ([#4](https://github.com/Mournerliao/lumiere/issues/4), [PR #6](https://github.com/Mournerliao/lumiere/pull/6)) | Swift host, explicit permission/cancellation states, SDR/HDR display capture, sRGB PNG file delivery, Electron process integration, fixed bright/dark scene verification on XDR hardware | None |
| 1B. Windows host adapter | In progress ([#7](https://github.com/Mournerliao/lumiere/issues/7)) | The engine has one narrow capture interface, single-conversion delivery, target-aware multi-adapter HDR probing, and deterministic operation teardown | Implement the JSON Lines Host executable, Electron integration, and Windows runtime verification |
| 1C. Shared product surface | In progress ([#9](https://github.com/Mournerliao/lumiere/issues/9)) | Approved compact main window and Settings output surface, generated tokens, persisted capability-gated clipboard/folder/both preference, one main-process capture router, v2 target/delivery contract, honest capability state, and verified macOS display-to-clipboard/folder/both journeys | Add the remaining independent settings slices, overlay, tray/menu bar, shortcut, macOS region, and independent Windows journeys after #7 |
| 1D. Distribution and release | Not started | Windows installer direction remains recorded | Windows installer, signed/notarized macOS artifact, clean-machine and hardware verification |
| 2. HDR-preserved export | Planned; not started | Claim gate and milestone are defined | Choose format/viewers, specify semantics, implement and verify both platforms |
| 3. Cross-platform HDR fidelity | Planned; not started | Fidelity is separated from artifact success and HDR preservation | Define support matrix/tolerances and run fixed-scene verification |

This table is a project posture snapshot, not a task ledger. GitHub Issues own
acceptance criteria and implementation status for each vertical slice.

## Verification Truth

- **Repository:** frozen install, layout and TypeScript checks, thirty-three cross-platform
  shell/protocol/process/settings/UI tests, three macOS path tests, eleven Swift
  protocol/capability/permission/diagnostic tests plus two native-delivery tests, and the
  production build pass locally on macOS. Shared, macOS, and Windows test gates are
  explicit and platform-scoped.
- **macOS development runtime:** the Electron display action drove ScreenCaptureKit to
  RGBA8 PNG files with alpha and an embedded sRGB IEC61966-2.1 profile on an Apple
  Silicon Mac. SDR capture was observed at 2560×1440 on an external display. Native
  HDR acquisition and tone mapping were then observed at 1728×1117 on the built-in
  Retina XDR display through both the host and Electron process boundary. After the
  lifecycle review fixes, the Electron path again produced a 2560×1440 sRGB PNG from
  the display under the pointer; the standalone rebuilt CLI remains a distinct TCC
  identity and returned the expected typed permission failure with correlated logging.
  The compact command-routed main window then repeated the development display-to-folder
  journey and produced a 2560×1440 PNG with alpha and an embedded sRGB IEC61966-2.1
  profile; keyboard focus and two zoom increments remained usable in the rendered window.
  The rebuilt v2 Host then completed independent display-to-clipboard, display-to-folder,
  and display-to-both journeys through the development Electron runtime. Preview created
  a new image from the clipboard-only result; folder-only produced a 2560×1440 PNG with an
  embedded sRGB IEC61966-2.1 profile; both reported copy and save success and produced the
  same class of PNG artifact. These observations establish artifact delivery only, not
  HDR preservation or Visual Match certification. The Settings output slice then changed
  the live main-window summary and subsequent display command across all three targets.
  Preview opened the clipboard-only result; folder-only and both each created a 2560×1440
  PNG with an embedded sRGB IEC61966-2.1 profile, and both reported copy and save success.
  A non-default Folder preference remained visible in both the main window and Settings
  after a full development-app restart; the final development preference was restored to
  Clipboard and folder. The first both-target attempt in this observation exposed the
  typed Host-unavailable recovery state; the immediate retry restarted the Host and
  completed both deliveries.
- **GitHub CI:** macOS shell and Windows engine workflows pass on current `main` at
  `fccf812`; this establishes their configured repository gates, not Windows Host
  runtime or hardware behavior.
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

## Execution Frontiers

Milestone 1A is complete. Per
[ADR 0008](../decisions/0008-environment-aware-execution-lanes.md), the active milestone
may expose one frontier per environment-eligible lane while each writer or worktree
advances only one current working Issue at a time.

- **Shared/macOS lane — [Issue #9](https://github.com/Mournerliao/lumiere/issues/9):**
  implement the shared Region Overlay and macOS region-capture journey on the existing
  platform-host v2 target-local geometry contract. Keep custom save-directory behavior
  in a later independent Settings slice because it is not part of the completed output-
  target preference and may require a separately reviewed protocol change.
- **Windows lane — [Issue #7](https://github.com/Mournerliao/lumiere/issues/7):** add
  the .NET platform-host executable around the retained engine using the current v2
  contract, integrate it with Electron, and verify repeat, cancellation, clean exit,
  and SDR/HDR-state behavior on the named Windows machine.
- **Milestone gate:** 1D distribution and release work remains blocked until both
  native lanes and the shared cross-platform journeys pass their independent criteria.

Neither lane projects repository, runtime, hardware, or completion truth to the other.

# Current Project State

- Updated: 2026-08-31
- Current milestone: 1 — Cross-platform HDR-aware MVP
- Release target: Windows + macOS HDR-aware MVP with sRGB Visual Match
- Completed foundation record: [GitHub Issue #1](https://github.com/Mournerliao/lumiere/issues/1)
- Operating model: Contract → Environment-eligible Frontier → Verification

## Current Position

The repository has converged on the final `apps/`, `protocol/`, `hosts/`, and
`knowledge/` ownership layout. Lumiere has an Electron/React shared shell,
language-neutral platform-host schemas with versioned executable fixtures, explicit
unavailable behavior, dual-platform CI, and isolated native host trees. Protocol v1 and
v2 are frozen; v3 owns capture-before-overlay Region sessions, a query-only capability
handshake, per-target delivery outcomes, and the cancellation responsibility boundary.

The compact shared main window now uses the approved generated tokens and drives an
integrated Swift ScreenCaptureKit host through one main-process capture command router
and the platform-host v3 JSON Lines process interface. The router owns capability and
delivery gating, persisted clipboard/folder/both preferences with a clipboard-and-folder
default, in-flight state, per-target result projection, and product failure mapping. The
approved Settings output surface now reads and writes that main-owned preference through
validated task-shaped preload/IPC, updates the main-window summary and next capture
immediately, and restores the value after restart. Region capture now freezes one native
frame before the Overlay appears: the Host retains that frame, Electron main grants a
revocable preview URL, and commit crops the same frozen frame. The Overlay owns only
target-local selection geometry and local cancel; it never sees the preview file path.
macOS display and region capture convert once to a timestamped
RGBA8/sRGB PNG representation and deliver those same bytes through native clipboard and
folder adapters. The macOS Host now derives Display and Region output dimensions from the
selected ScreenCaptureKit filter's point-to-pixel scale, so scaled 4K and Retina targets
produce backing-pixel-density artifacts instead of one pixel per logical point. Region and
Display now have independent opt-in global shortcut settings;
main owns atomic registration, conflict handling, versioned settings migration, and recording
suspension, while the tray/menu-bar projects capability-gated Region/Display commands,
registered accelerators, Open, Settings, and Quit through the same capture router. The macOS
development launch now incrementally builds the current Swift Debug Host and resolves it
ahead of any stale Release artifact; explicit Host overrides remain authoritative. The shared
Settings surface now also persists `Do nothing` or `Show in folder` after-capture behavior
through validated main-owned IPC. One main-process completion path applies that preference
to main-window, shortcut, and tray/menu-bar captures without changing artifact success when
Finder or Explorer cannot reveal an already-saved file. The shared Settings surface now also
persists whether non-blocking HDR status reminders are shown. The default remains on; validated
main-owned IPC writes the setting, the capture router
projects an advisory only when the Host still declares capture available, and disabling the
preference hides neither the underlying target status nor any blocking Host, permission, or
  capture failure. Settings v5 now also persists an optional custom save directory while migrating
  v1 through v4 values. Main owns the native directory picker and exposes only a no-argument,
  task-shaped preload command; Folder and Both capture requests may carry the selected absolute
  path through protocol v3 while each Host retains directory creation, fixed naming, file writing,
  and target-local delivery failure. The platform default remains Pictures/Lumiere.
The Windows engine now exposes Display as one complete capture operation and Region as
a frozen-frame prepare/commit session. Both paths own target resolution, target-aware
HDR probing, first-frame acquisition, one sRGB Visual Match conversion, delivery,
cancellation, and teardown. Region commit crops the retained owned texture rather than
acquiring a second WGC frame. On HDR-active targets, that conversion now resolves
the target's Windows SDR white level and normalizes captured scRGB input before tone mapping;
an unavailable white level fails capture rather than producing an unverified Visual Match
artifact. A .NET platform-host v3 executable now owns the
Windows JSON Lines process boundary, strict request validation, correlated structured
diagnostics, and typed unavailable behavior. The Host connects that engine for Display and
frozen-frame Region capture, owns its process-scoped lifetime, creates the default
Pictures/Lumiere destination when needed, and maps Clipboard, Folder, and Both outcomes
independently from one encoded artifact into protocol v3. Its capability provider
now resolves the display under the pointer independently of capture, probes that target's HDR
state, and converts physical pixels through effective monitor DPI. Region is advertised
only when a reconstructable native target snapshot exists; prepare captures one complete
frame, copies it to an application-owned D3D11 texture, and commit crops that frozen
frame without a second WGC acquisition.
The development entry
point builds its current Debug artifact, and Electron selects and supervises it through the
shared native-process transport. Unpackaged Windows development builds intentionally retain
the WGC system capture border. Borderless capture is deferred to Milestone 1D, where the
traditional installer will register a signed external-location sparse package, request
`graphicsCaptureWithoutBorder` consent, and preserve the system border as the denied or
unsupported fallback. The Windows Host adapter is complete through Issue #7;
Issue #9 owns the remaining shared product surface and cross-platform product journeys.
WinUI and the old validation/HDR10-JXR paths remain removed.

## Progress At A Glance

| Roadmap slice | Current state | What is true now | What remains before exit |
|---|---|---|---|
| 0. Foundation | Complete | Final layout, secure shell, language-neutral protocol, paused Windows engine; macOS and Windows CI pass | None |
| 1A. macOS native capture | Complete ([#4](https://github.com/Mournerliao/lumiere/issues/4), [PR #6](https://github.com/Mournerliao/lumiere/pull/6)) | Swift host, explicit permission/cancellation states, SDR/HDR display capture, sRGB PNG file delivery, Electron process integration, fixed bright/dark scene verification on XDR hardware | None |
| 1B. Windows host adapter | Complete ([#7](https://github.com/Mournerliao/lumiere/issues/7), [#10](https://github.com/Mournerliao/lumiere/issues/10)) | The v2 Host has strict JSON Lines handling, structured diagnostics, Electron supervision, target-aware HDR/logical-geometry capabilities, SDR-white-aware HDR Visual Match conversion, Display plus target-bound Region routing, Clipboard/Folder/Both delivery, sanitized protocol failures, deterministic teardown, and verified interactive HDR/SDR runtime journeys | None |
| 1C. Shared product surface | In progress ([#9](https://github.com/Mournerliao/lumiere/issues/9)) | Approved compact main window and Settings output/shortcut/after-capture/HDR-reminder/save-directory surfaces, generated tokens, persisted capability-gated preferences, one main-process capture router, capability-gated tray/menu-bar, v3 frozen-region/delivery/custom-directory contract, shared Region Overlay over a Host-frozen preview, HiDPI-aware macOS geometry, and verified macOS display/region/after-capture/custom-directory journeys before this freeze | Run the updated Windows repository/runtime gates and remaining frozen-region product verification |
| 1D. Distribution and release | Not started | Windows retains its traditional signed installer path; macOS will publish a normal direct GitHub release as an ad-hoc-signed app in a disk image, with no App Store, Homebrew, Developer ID, or notarization requirement | Implement each platform-owned package, document the macOS Gatekeeper exception, then complete clean-machine and hardware verification independently |
| 2. HDR-preserved export | Planned; not started | Claim gate and milestone are defined | Choose format/viewers, specify semantics, implement and verify both platforms |
| 3. Cross-platform HDR fidelity | Planned; not started | Fidelity is separated from artifact success and HDR preservation | Define support matrix/tolerances and run fixed-scene verification |

This table is a project posture snapshot, not a task ledger. GitHub Issues own
acceptance criteria and implementation status for each vertical slice.

## Verification Truth

- **Repository:** frozen install, layout and TypeScript checks, eighty-nine cross-platform
  shell/protocol/process/settings/UI tests, three macOS path tests, thirty-two Swift
  protocol/capability/permission/diagnostic/geometry/native-delivery tests, and the current
  production TypeScript check pass on this Mac. Windows Host/engine tests for protocol v3
  frozen Region sessions are in source but were not run here because `dotnet` is not
  installed. Shared, macOS, and Windows test gates remain explicit and platform-scoped.
  The frozen-region slice passed `pnpm check`, all eighty-nine shared tests, three macOS
  path tests, and thirty-two Swift tests on this Mac. That verifies repository contract
  shape, not the interactive freeze → select → commit journey, Windows Host compilation,
  or hardware Visual Match.
  The HDR-reminder slice passed all eighty-two shared tests, repository checks, and the production
  Electron build on the named Windows machine. A 480×370 local renderer observation covered the
  enabled advisory, Capture Settings switch, disabled reminder state, and stable no-scroll layout;
  it used typed local preload-state fixtures and therefore establishes renderer behavior only, not
  Windows or macOS runtime persistence.
  The custom-directory slice passed the current macOS repository checks, all eighty-four shared
  tests, three macOS path tests, thirty Swift tests, Swift formatting, and the production Electron
  build. Its updated Windows Host source and tests were not run on this Mac because `dotnet` is not
  installed; the prior Windows counts do not verify this slice.
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
  completed both deliveries. Region capture then passed clipboard-only, folder-only,
  both-target, repeat, Esc cancel, invalid-click cancel, and clean-exit journeys. A repeated
  region result was 863×694 with alpha and an embedded sRGB IEC61966-2.1 profile; the
  Overlay was absent from the artifact. The final output preference was restored to both.
  The shortcut slice then displayed the capability-gated Capture settings and full
  Region/Display/Open/Settings/Quit menu-bar structure, rejected a conflicting accelerator,
  registered and persisted `Control+Alt+F12`, and cleared it back to the unconfigured default.
  This establishes the macOS development shortcut lifecycle and menu structure, not Windows
  behavior or an additional capture/HDR claim. A root-level `pnpm dev` launch then rebuilt the
  current Debug Host with the installed Xcode toolchain and exposed both Region and Display;
  this closes the stale-Release development-selection failure without changing packaged Host
  discovery. The after-capture slice then selected Folder plus `Show in folder`; display capture
  saved `Lumiere-2026-08-27-164643.png`, reported `Saved to “Lumiere”`, and opened Finder with
  that file selected. With Clipboard plus the same after-capture preference, display capture
  reported `Copied to clipboard` and left Finder on the prior saved file. A full development-app
  restart restored both the Clipboard output and `Show in folder` preferences. The final
  development settings were restored to Clipboard and folder plus `Do nothing`. These
  observations establish after-capture routing and persistence on macOS only; they add no
  Visual Match, HDR-preservation, or Windows claim. A later external-4K clarity report exposed
  that the Host had configured ScreenCaptureKit's pixel output with logical point dimensions:
  a near-full region was only 1408×821 and visibly soft. The rebuilt Host then used the selected
  filter's 2× pixel scale and produced a 5120×2880 Display artifact where the previous result was
  2560×1440. A 1320×769 logical Region produced 2640×1538 pixels. Both retained RGBA, alpha, and
  the embedded sRGB IEC61966-2.1 profile. A ten-capture 4K repeat loop produced 5120×2880 each
  time without Host restart or retained-memory growth. A later same-display A/B isolated a
  remaining Region-only softness: the 5120×2880 full ScreenCaptureKit frame matched the macOS
  system screenshot's edge distribution, while a pixel-aligned `sourceRect` Region remained
  visibly softer. Region now captures the complete backing frame and applies an integer
  `CGImage` crop in the Host. A 3005×1691 hardware result restored sharp text at 100% inspection,
  retained the embedded sRGB profile, and completed clipboard-plus-folder delivery. These
  observations establish corrected backing-pixel geometry and Region sharpness on the named
  scaled external display; the built-in Retina XDR path was not re-observed after this geometry
  correction. The custom-directory Settings surface was then exercised through the real 480×370
  development Electron window and native directory picker. A Display-to-Both capture wrote
  `Lumiere-2026-08-31-115523.png` (20,824,119 bytes) to a non-default temporary directory and
  reported copy and save success. A full development-app restart restored that custom path in the
  main-window summary. With `Show in folder` enabled, a second Display-to-Both capture wrote
  `Lumiere-2026-08-31-115928.png` (20,857,480 bytes); Finder opened the custom directory with that
  artifact selected. The final effective settings were restored to Pictures/Lumiere, Clipboard and
  folder, and `Do nothing`. These observations establish custom-directory routing, persistence,
  artifact delivery, and after-capture reveal behavior on macOS only; they add no Visual Match,
  HDR-preservation, or Windows claim.
- **GitHub CI:** the last recorded macOS shell and Windows engine workflow observation passed at
  `fccf812`; this establishes those configured repository gates at that commit, not current-HEAD CI, Windows Host
  runtime or hardware behavior.
- **Windows engine:** restore and Release build pass with warnings treated as errors;
  Host 28, Capture 85, Graphics 47, and Interop 35 tests pass;
  `dotnet format --verify-no-changes` passes.
- **Windows integration:** the named Windows development machine built the Debug Host through the
  platform-neutral preparation entry point. The Release Host then completed a real JSON Lines
  Display-to-Folder request through WGC on a 3840×2160 HDR-active display, emitted structured
  capture and four-stage teardown diagnostics, returned a correlated `completed` result with
  HDR source state and sRGB Visual Match output profile, wrote an 8,428,590-byte PNG under
  `Pictures/Lumiere`, and exited cleanly on stdin EOF. Electron transport/path tests passed
  against the Windows contract. The rebuilt Release Host then completed a standalone
  `getCapabilities` request on the named scaled HDR-active display, reported target-aware HDR
  support, and returned an opaque target token with a 2560×1440 logical target size. A subsequent
  Display-to-Folder regression smoke under the new per-monitor-v2 DPI context acquired the same
  display at 3840×2160, wrote an 11,421,216-byte PNG, completed all four teardown stages, and exited
  cleanly on stdin EOF. A root-level `pnpm dev` journey then rebuilt and selected the current Debug
  Host, exposed Display plus Folder in the shared UI, and completed two consecutive Electron-to-WGC
  captures on the same 3840×2160 HDR-active display. The requests wrote distinct 6,615,871-byte and
  6,803,406-byte PNG files with HDR source state and sRGB Visual Match output profile; each completed
  all four teardown stages. Closing the Electron window exited the development process with code 0
  and left no Windows Host process. This establishes capability projection, native acquisition,
  repeated Electron-boundary artifact delivery, and clean exit, not Visual Match certification or HDR
  preservation; no release-candidate record exists. The current Release Host was also exercised from
  the non-interactive command session after Region and Clipboard/Both routing landed. That session could
  not access cursor or WGC services (`GetCursorPos` access denied and
  `GraphicsCaptureSession.IsSupported` returned `0x80070424`), so it produced no new artifact evidence.
  The Host kept the technical exception in structured stderr and returned only the typed, sanitized
  `capture-unavailable` / `The capture target is unavailable. Try again.` protocol result. The
  interactive Electron runtime then completed Region-to-Folder and Region-to-Both on the named
  3840×2160 HDR-active display. The Both journey copied the result and wrote a 1200×932 RGBA PNG;
  Paint consumed the Display clipboard result. A clipboard apartment failure found during that
  journey was corrected by performing the WinRT clipboard publication on an STA thread, after
  which structured diagnostics reported `ClipboardOutput` complete. Closing Electron immediately
  after issuing a native Display request exited with code 0 after the request and all four teardown
  stages completed, with no remaining Lumiere window or Host process. A separate Region Overlay
  journey accepted Esc, returned the main window to `Capture cancelled`, produced no artifact, and
  emitted no native Region reservation or capture request, confirming Shell-owned pre-dispatch
  cancellation. Windows HDR was then disabled independently: capability polling observed
  `RgbFullG22NoneP709` with `isHdrActive=False`, and an Electron Display-to-Both request copied the
  artifact, wrote a 3840×2160 RGBA PNG, and completed all four teardown stages. HDR was restored to
  its original enabled state after the observation. These observations establish Windows artifact
  delivery and lifecycle behavior on the named machine, not Visual Match certification, HDR
  preservation, or release readiness. A subsequent fixed-image HDR observation queried the active
  3840×2160 target's SDR white as 240 nits, applied the corresponding 80/240 scRGB normalization,
  and captured a 1401×987 sRGB reference shown at 100%. Alignment against the source measured mean
  absolute RGB errors of 0.867/0.858/0.886 out of 255, a per-channel 95th-percentile error of 1,
  and a median luminance ratio of 0.985. This verifies Windows sRGB Visual Match for that named
  target, SDR-white setting, fixture, and viewer path; it does not establish HDR-preserved export.
- **Hardware verified:** the macOS Retina XDR path passed fixed bright and dark scene
  observations. The bright ramp preserved ten distinct grayscale steps from 25 through
  255; the dark ramp preserved ten distinct steps from 0 through 32 and alternating
  19/7 fine detail. The named scaled external 4K display passed Display and Region
  backing-pixel-dimension observations plus a ten-capture repeat loop after the HiDPI fix.
  Windows has one fixed-image sRGB Visual Match observation on the named HDR-active 4K target;
  broader cross-platform fidelity certification and the Retina XDR geometry correction remain
  independently unobserved on hardware.

Do not describe the foundation as working capture, or the cross-platform MVP as
release-ready until both owning platforms are explicitly verified.

## Execution Frontiers

Milestone 1A is complete. Per
[ADR 0008](../decisions/0008-environment-aware-execution-lanes.md), the active milestone
may expose one frontier per environment-eligible lane while each writer or worktree
advances only one current working Issue at a time.

- **Shared/macOS lane — [Issue #9](https://github.com/Mournerliao/lumiere/issues/9):**
  frozen Region capture is implemented under
  [ADR 0013](../decisions/0013-frozen-region-capture-session.md) and protocol v3:
  capture-before-overlay, Host-owned frozen frames, Electron-main preview capability,
  and commit-from-the-same-frame. macOS Host, shared shell, and protocol fixtures are
  in this worktree. Windows Host/engine source matches the same contract but has not
  been run here because `dotnet` is not installed. Next: verify the macOS shortcut →
  freeze → select → commit journey on this Mac, then hand Windows Host tests/runtime
  to the Windows lane.
- **macOS distribution lane — [Issue #11](https://github.com/Mournerliao/lumiere/issues/11):**
  the next slice is a reproducible packaged-app foundation under
  [ADR 0012](../decisions/0012-direct-ad-hoc-signed-macos-distribution.md).
  Define an acceptance-ready owning Issue, record the stable bundle identity, deployment target,
  and architecture policy, then add the smallest packaging path that builds the current Electron
  shell, generated icons, and Release Swift Host into a coherently ad-hoc-signed, locally runnable
  `.app`. Verify packaged Host discovery, Region and Display capture,
  Clipboard/Folder/Both delivery, permission identity, repeat capture, and clean exit on the
  named Mac. A following slice owns the direct GitHub Release disk image, SHA-256 checksum,
  manual Gatekeeper instructions, replacement upgrade, and clean-machine verification. Developer
  ID signing, notarization, Mac App Store distribution, and Homebrew distribution are out of scope.
- **Windows custom-directory and frozen-region verification lane — [Issue #9](https://github.com/Mournerliao/lumiere/issues/9):**
  run the updated Host tests/build/format gates on Windows, then verify directory selection,
  persistence, Folder/Both delivery, target-local folder failure, Show in folder, and the
  frozen Region journey (shortcut freezes the frame; commit is that frame, not release time)
  independently.
- **Windows lane — [Issues #7](https://github.com/Mournerliao/lumiere/issues/7) and
  [#10](https://github.com/Mournerliao/lumiere/issues/10):** complete;
  the named Windows machine passed the required Display/Region, Clipboard/Folder/Both,
  HDR/SDR-state, SDR-white-aware Visual Match, repeated capture, cancellation, teardown,
  and clean-exit criteria.
- **Milestone gate:** platform-owned 1D implementation may advance independently, but
  cross-platform release verification and the Milestone 1 exit gate remain blocked until
  Issue #9 and both distribution lanes pass their independent criteria.

Neither lane projects repository, runtime, hardware, or completion truth to the other.

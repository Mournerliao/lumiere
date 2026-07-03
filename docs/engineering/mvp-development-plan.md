# MVP Development Plan

This is the traceable development checklist for the HDR-aware MVP. Update it after each implementation session so the plan stays aligned with code, docs, and validation evidence.

Checkbox meaning: checked means the repo or current working tree contains the implementation or documentation for that item. It does not imply Windows build, runtime, or manual HDR validation unless the item explicitly says so.

## Maintenance Rule

- Update this checklist in the same change set as the code or documentation it tracks.
- Keep each item small enough to review, but large enough to deliver a coherent capability.
- When an item changes product or architecture language, update `CONTEXT.md` or `docs/decisions/` in the same session.
- When an item changes release confidence, update `docs/validation/mvp-checklist.md` in the same session.
- Keep P3 and HDR10 as planned profiles until an explicit later decision promotes one of them to a supported output path.

## Phase 0: Direction And Cleanup

- [x] Record MVP-first direction: HDR-aware screenshot tool, not broad HDR-preserved release.
- [x] Maintain domain language for HDR-aware MVP, compatible output, visual match, planned profiles, and target-app limitations.
- [x] Remove legacy broad HDR fidelity naming from current product docs and app projections.
- [x] Remove resource-trend validation workflow from the normal MVP UI and app surface.
- [x] Record sRGB Visual Match as the single official MVP output path.
- [x] Record P3 and HDR10 as planned profiles, not normal user-selectable MVP modes.
- [x] Record shared sRGB Visual Match conversion as an architecture decision.
- [ ] Run restore/build/tests/format on Windows after the cleanup diff.
- [ ] Run a basic Windows runtime smoke after the cleanup diff: launch, open main window, start/cancel capture, and exit.

## Phase 1: Native Capture Baseline

- [x] Establish WinUI 3 / Windows App SDK native app shell.
- [x] Keep module boundaries for App, Capture, Graphics, Infrastructure, Overlay, and Settings.
- [x] Create D3D11 / DXGI graphics device and FP16/scRGB-oriented preview contracts.
- [x] Implement capture target selection for display/window-oriented Windows Graphics Capture paths.
- [x] Implement region capture overlay with drag-to-select and release-to-capture behavior.
- [x] Implement fullscreen capture path for the active target.
- [x] Implement cancel behavior and deterministic teardown for capture/overlay resources.
- [x] Surface target-aware HDR readiness instead of a global HDR guess.
- [x] Provide main window, tray, and shortcut-oriented entry points.
- [ ] Validate launch, region capture, fullscreen capture, cancel, and exit on Windows hardware.

## Phase 2: Official sRGB Visual Match Output

- [x] Support clipboard, folder, and both-target output policies.
- [x] Support save path, timestamp naming, copy-as-image, and after-capture behavior settings.
- [x] Keep output artifact success separate from visual match and HDR preservation claims.
- [x] Keep output profile contracts for sRGB, P3, and HDR10 while releasing only sRGB Visual Match in the MVP UI.
- [x] Hide P3 and HDR10 from normal user-selectable MVP output UI.
- [ ] Extract the current FP16/scRGB to RGBA8/sRGB conversion into a shared output component without changing behavior.
- [ ] Route clipboard output through the shared sRGB Visual Match component.
- [ ] Route folder output through the same shared sRGB Visual Match component.
- [ ] Ensure both-target output performs one shared visual-match conversion per capture result where practical.
- [ ] Update output result copy to identify successful output as sRGB Visual Match without warning on every capture.

## Phase 3: Default Visual Match Conversion

- [x] Define Default Visual Match Conversion as non-user-adjustable MVP behavior.
- [x] Define the first tone mapper as simple, fixed-parameter, explainable, and regression-testable.
- [x] Require ordinary SDR-range content to remain visually stable where practical.
- [x] Require HDR highlights to be smoothly compressed instead of hard-clamped.
- [ ] Add platform-neutral tests for SDR-range stability.
- [ ] Add platform-neutral tests for smooth highlight compression above the SDR range.
- [ ] Add platform-neutral tests for alpha/channel ordering and crop consistency.
- [ ] Replace hard clamp / simple gamma conversion with the first Default Visual Match tone mapper.
- [ ] Verify clipboard and folder output use identical converted pixels before target delivery.

## Phase 4: MVP UI And Product Copy

- [x] Keep normal output UI focused on the single official sRGB Visual Match path.
- [x] Keep P3/HDR10 out of normal user-selectable MVP modes.
- [x] Use low-interruption honesty: normal output feedback may mention sRGB Visual Match, while HDR-preserved limitations live in settings, validation, or help surfaces.
- [x] Keep validation/help copy clear that HDR-preserved export is not yet supported.
- [ ] Review main window, tray, overlay, and output-result copy after shared conversion lands.
- [ ] Remove or reword any remaining normal-UI copy that suggests three supported color modes.

## Phase 5: Lightweight Visual Validation

- [x] Define visual-match validation priorities: avoid overexposure/washed-out/gray output first, preserve usable shadows and contrast second, iterate fine saturation/highlight detail later.
- [x] Define fixed visual-match scene categories: bright HDR scene, dark scene, everyday desktop scene.
- [x] Define target app boundary: Lumiere owns valid artifact generation; receiving app compression/recoloring is recorded as target-app limitation.
- [x] Define MVP blocking visual failures that cannot be shipped as mere limitations.
- [x] Defer full visual-match validation until the shared output conversion and first Default Visual Match tone mapper are implemented.
- [ ] Capture and record bright HDR scene results for clipboard, folder, and both-target output.
- [ ] Capture and record dark scene results for clipboard, folder, and both-target output.
- [ ] Capture and record everyday desktop scene results for clipboard, folder, and both-target output.
- [ ] Compare clipboard and folder output for visible drift caused by Lumiere.
- [ ] Record target-app limitations separately from Lumiere artifact issues.

## Phase 6: Windows Release Gate

- [ ] Run `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false` on Windows.
- [ ] Run `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false` on Windows.
- [ ] Run `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false` on Windows.
- [ ] Run `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`.
- [ ] Complete the MVP validation checklist on Windows.
- [ ] Record known limitations without using them to bypass MVP blocking visual failures.

## Phase 7: Windows Packaging And Installer

- [x] Choose the MVP deployment model: traditional Windows setup installer with a setup `.exe`.
- [x] Decide that custom install path support is a requirement for the MVP installer flow.
- [ ] Choose installer implementation technology when packaging work starts.
- [ ] Define installer ownership at packaging time: app files, shortcuts, uninstall entry, runtime prerequisites, upgrade behavior, signing, and install path selection.
- [ ] Build a signed or clearly unsigned internal installer artifact for Windows testing.
- [ ] Sign the public MVP installer before distribution.
- [ ] Validate clean install, launch after install, upgrade install, uninstall, and reinstall.
- [ ] Validate install behavior on a standard non-development Windows machine.
- [ ] Prepare release notes that say HDR-preserved export is not yet supported.

## Phase 8: After MVP

- [ ] Decide whether the next supported output path is HDR10 or P3.
- [ ] Pick one named format and viewer target before making new public claims.
- [ ] Define source format, destination format, transfer function, primaries, conversion policy, metadata policy, and viewer assumptions for that path.
- [ ] Implement and validate the single chosen HDR-preserved or wide-gamut path.
- [ ] Expand compatibility only after the single path has evidence.

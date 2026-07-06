# Lumiere MVP

Lumiere's first release target is a usable HDR-aware Windows screenshot tool. The release should feel fast, native, and honest: users can capture the screen, get a useful output, and understand when the app is working with HDR conditions without being promised unsupported preservation.

## Product Goal

Ship an MVP that does the core screenshot job well on Windows:

- Start capture from the main window, tray, or configured shortcut.
- Support region and fullscreen capture.
- Keep the preview pipeline oriented around FP16/scRGB HDR data.
- Produce practical clipboard and folder output for common Windows use.
- Report HDR availability and limitations honestly.

The MVP is not a certification of universal HDR preservation. It is the first credible product release that keeps the HDR foundation intact while avoiding unsupported claims.

## In Scope

- Native Windows app using WinUI 3, Windows App SDK, Windows Graphics Capture, Direct3D 11, DXGI, and Vortice.
- x64-only `.NET 10` target.
- Region capture with drag-to-select and release-to-capture behavior.
- Fullscreen capture for the active target.
- Clipboard output, folder output, and both-target output.
- Local settings for output target, save path, timestamp naming, shortcut, after-capture behavior, and HDR alert preference.
- HDR-aware status copy that distinguishes HDR-ready, unavailable, degraded, and unvalidated conditions.
- sRGB Visual Match as the official MVP output path for clipboard, folder, and both-target output: HDR screenshots should avoid obvious washed-out, gray, or blown-out output and should look close to the user's perceived screen appearance in common cases.
- Traditional Windows setup installer flow sufficient for a non-development machine to choose an install path, install, launch, upgrade, and uninstall the MVP.
- Lightweight Windows manual validation for the supported MVP paths.

## Out Of Scope For MVP

- Public claims of broad HDR-preserved fidelity.
- Showing P3 or HDR10 as normal user-selectable MVP output modes.
- Releasing P3 or HDR10 as supported MVP output modes.
- Broad HDR-preserved export guarantees.
- Multi-viewer HDR10/JPEG XR compatibility certification.
- Full display-topology coverage as a release blocker.
- Long-run 50+ or 100+ cycle stability evidence as a release blocker.
- Annotation-heavy editing, gallery/history, cloud upload, sharing workflows, onboarding, telemetry, or web/Electron/Tauri production UI.

## Public Claim Boundary

Use language like:

- "HDR-aware Windows screenshots."
- "Native capture and preview with an HDR-first graphics pipeline."
- "sRGB output tuned for visual match in everyday clipboard and file use."
- "HDR-preserved export is not yet a supported public path."

Use low-interruption honesty in product UI: normal output feedback can identify the result as sRGB Visual Match, while HDR-preserved limitations should live in settings, validation, or help surfaces instead of appearing as a warning after every successful capture.

Do not use language like:

- "Universal HDR fidelity."
- "Three supported color modes."
- "HDR preserved in every output."
- "HDR10/JXR supported" unless a specific export path has implementation and viewer validation evidence.

## Success Criteria

- The app can be launched, used for repeated capture, and exited cleanly on a Windows machine.
- Region and fullscreen capture complete without stuck overlay or stale capture state.
- Clipboard and folder outputs are usable in common Windows consumers.
- HDR screenshots do not show obvious washed-out, gray, or blown-out output in supported validation scenarios.
- HDR status never claims more than the app can currently prove for the active target.
- Known limitations are recorded in release notes or the MVP validation checklist.

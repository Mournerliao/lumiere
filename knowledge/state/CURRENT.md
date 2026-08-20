# Current Project State

- Updated: 2026-08-20
- Release target: Windows + macOS HDR-aware MVP with sRGB Visual Match
- Operating model: Contract → Frontier → Evidence

## Current Position

The product has adopted an Electron/React shared shell with native capture hosts:
existing C# WGC/D3D11/DXGI code is the Windows host foundation, and a Swift
ScreenCaptureKit host will own macOS capture. The three-stage sequence is HDR-aware
sRGB Visual Match MVP, one HDR-preserved export path, then measured cross-platform
HDR fidelity.

GitHub Issue #1 owns the first foundation slice: contracts and ADR, secure Electron
shell, narrow platform-host interface, and macOS-runnable repository checks. Native
host integration and packaging have not started.

## Verification Truth

- **Repository done:** the secure Electron foundation, typed platform-host interface,
  protocol tests, and production build pass on macOS; native hosts remain unconnected.
- **macOS verified:** the production shell launches and truthfully reports the expected
  `host-unavailable` state; macOS capture and HDR behavior remain unverified.
- **Windows verified:** no passing cross-platform release-candidate record exists.
- **Hardware evidenced:** no passing HDR Visual Match record exists for either platform.

Do not describe the cross-platform MVP as release-ready until the applicable evidence
is committed for both platforms.

## Frontier

Complete Issue #1 and leave its repository checks passing. Then open dependent vertical
Issues in this order:

1. Implement a minimal Swift ScreenCaptureKit host that reports permissions and writes
   one sRGB Visual Match file on macOS, proving the new platform-host seam end to end.
2. Connect the existing Windows engine through the same interface and re-run Windows
   runtime/HDR validation.
3. Add native clipboard/folder policies and the shared region/display interaction.
4. Produce signed internal Windows and macOS artifacts, then gather release evidence.

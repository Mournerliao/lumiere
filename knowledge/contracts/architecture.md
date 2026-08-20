# Architecture Contract

## Platform Baseline

- Shared shell: Electron, React, TypeScript, and Chromium; Windows and macOS only.
- Windows host: `.NET 10`, WGC, D3D11, DXGI, Vortice, `x64` / `win-x64`.
- macOS host: Swift, ScreenCaptureKit, and native Apple color/GPU frameworks;
  supported architecture and deployment target must be recorded before distribution.
- MVP output: one shared semantic profile, RGBA8/sRGB Visual Match, delivered through
  platform-native clipboard and file adapters.

Do not introduce Electron desktop capture, renderer Canvas, `NativeImage`, GDI,
cloud upload, or telemetry as the official capture/conversion foundation.

## Module Boundaries

| Module | Responsibility |
|---|---|
| `apps/desktop` | Electron lifecycle, shared React UI, secure preload, platform-host orchestration |
| `apps/desktop/src/shared` | Versioned command/result interface at the platform-host seam |
| `Lumiere.App` | Transitional WinUI composition while the Windows host is extracted |
| `Lumiere.App.Core` | Platform-neutral app projections and orchestration seams |
| `Lumiere.Capture` | Windows WGC target selection, frame-pool lifecycle, capture state |
| `Lumiere.Graphics` | Windows D3D11 device, DXGI presentation, output conversion |
| `Lumiere.Infrastructure` | Windows COM/WinRT interop, diagnostics, typed platform adapters |
| `Lumiere.Overlay` | Transitional Windows overlay and crop interaction |
| `Lumiere.Settings` | Transitional Windows local preferences and settings projections |

The macOS native host will live in its own platform-owned source tree when its first
vertical slice is introduced. It must not be hidden inside renderer or preload code.

Platform APIs stay in their owning module. The shell consumes the platform-host
interface but must not own WGC, DXGI, D3D11, ScreenCaptureKit, Metal, ColorSync,
COM/WinRT, or native-resource lifetime details. IPC may carry commands, typed state,
diagnostics identifiers, and artifact paths; it must not carry raw HDR frames.

## Electron Security Invariants

- Load packaged local content only.
- Keep renderer sandboxing and context isolation enabled and Node integration disabled.
- Expose one named preload method per command; never expose `ipcRenderer` directly.
- Validate IPC sender and payload before crossing the platform-host seam.
- Deny unexpected navigation and window creation.

## HDR Invariants

- Preserve each platform's native high-dynamic-range acquisition semantics until the
  shared sRGB Visual Match conversion is complete.
- Assess HDR against the active target, not a global or first-monitor assumption.
- Keep sRGB Visual Match conversion behind each native host but governed by one shared
  semantic contract and fixed regression fixtures.
- Separate artifact delivery success, visual-match evidence, and HDR preservation.
- Keep platform capability and evidence independent; one adapter cannot certify another.
- Require the claims contract before changing public HDR language.

## Resource And Diagnostics Invariants

- Dispose COM, DXGI, D3D11, WGC, ScreenCaptureKit, Metal, and related native resources deterministically.
- Use structured platform logging; do not use ad-hoc console output for native failures.
- Prefer typed results and explicit state transitions for expected platform failures.
- Automated tests may cover configuration, state, projection, protocol, and lifecycle
  seams; real capture/HDR presentation remains a platform hardware concern.

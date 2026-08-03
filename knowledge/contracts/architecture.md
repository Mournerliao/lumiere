# Architecture Contract

## Platform Baseline

- Runtime: `.NET 10`
- Target: `net10.0-windows10.0.19041.0`
- Architecture: `x64` / `win-x64` only
- UI: WinUI 3 and Windows App SDK
- Capture: Windows Graphics Capture
- Graphics: Direct3D 11, DXGI, and Vortice

Do not introduce Electron, Tauri, WPF bitmap-first foundations, WinForms, GDI
screenshot foundations, cloud upload, telemetry, or web UI as production architecture.

## Module Boundaries

| Module | Responsibility |
|---|---|
| `Lumiere.App` | WinUI startup, dependency composition, windows, app projections |
| `Lumiere.App.Core` | Platform-neutral app projections and orchestration seams |
| `Lumiere.Capture` | WGC target selection, frame-pool lifecycle, capture state |
| `Lumiere.Graphics` | D3D11 device, DXGI swap chain, presentation, output conversion |
| `Lumiere.Infrastructure` | COM/WinRT interop, diagnostics, typed platform adapters |
| `Lumiere.Overlay` | Fullscreen overlay, crop interaction, concise capture cues |
| `Lumiere.Settings` | Local preferences and settings projections |

Platform APIs stay in their owning module. UI may consume projections and narrow
services but must not own DXGI, D3D11, WGC, or COM lifetime details.

## HDR Invariants

- Preserve the FP16/scRGB preview direction.
- Assess HDR against the active target, not a global or first-monitor assumption.
- Keep sRGB Visual Match conversion in one shared output component.
- Separate artifact delivery success, visual-match evidence, and HDR preservation.
- Require the claims contract before changing public HDR language.

## Resource And Diagnostics Invariants

- Dispose COM, DXGI, D3D11, WGC, frame-pool, swap-chain, and capture resources deterministically.
- Use `ILogger` through `LumiereLoggerFactory`; do not use `Console.WriteLine`.
- Prefer typed results and explicit state transitions for expected platform failures.
- Automated tests may cover configuration, state, projection, and lifecycle seams;
  real WGC/DXGI/HDR presentation remains a Windows hardware concern.

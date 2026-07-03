# Architecture

Lumiere is a native Windows desktop application. The architecture should keep platform APIs inside their owning modules and expose narrow interfaces upward.

## Platform And Runtime

- Runtime: `.NET 10`
- Target framework: `net10.0-windows10.0.19041.0`
- Architecture: `x64` only
- UI: WinUI 3 and Windows App SDK
- Capture: Windows Graphics Capture
- Graphics: Direct3D 11, DXGI, Vortice

Do not introduce Electron, Tauri, WPF bitmap-first foundations, WinForms, GDI screenshot foundations, cloud upload, telemetry, or web UI as production architecture.

## Module Boundaries

| Module | Responsibility |
|---|---|
| `Lumiere.App` | WinUI startup, dependency composition, windows, app-level projections |
| `Lumiere.Capture` | Windows Graphics Capture target selection, frame pool lifecycle, capture state |
| `Lumiere.Graphics` | D3D11 device handling, DXGI swap chain, HDR constants and graphics contracts |
| `Lumiere.Infrastructure` | COM/WinRT interop, diagnostics, typed results, UI-thread helpers |
| `Lumiere.Overlay` | Fullscreen overlay, crop interaction, overlay status/fidelity cues |
| `Lumiere.Settings` | Local preferences and settings projections |

Platform APIs should stay in their boundary module. UI code can consume projections and narrow services, but should not own DXGI, D3D11, WGC, or COM lifetime details.

## HDR Invariants

- Preserve the FP16/scRGB preview direction.
- Do not route the main preview through an SDR screenshot-library foundation.
- Treat HDR display capability as target-aware, not as a single global assumption.
- Output artifact success is not the same as HDR preservation.
- sRGB Visual Match conversion must live in a shared output component used by clipboard, folder, and both-target output, so the MVP does not drift into target-specific tone mapping behavior.
- Public HDR-preserved claims require a named output path, documented conversion/metadata policy, target app assumptions, and Windows manual validation.

## Resource And Diagnostics Rules

- Use deterministic disposal for COM, DXGI, D3D11, frame pool, swap chain, and capture resources.
- Use structured logging through `ILogger` and `LumiereLoggerFactory`; do not use `Console.WriteLine`.
- Prefer typed results and explicit state transitions for expected platform failures.
- Automated tests can cover configuration, state, projections, and lifecycle seams. Real HDR presentation and WGC/DXGI behavior still require Windows hardware validation.

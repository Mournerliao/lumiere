# AGENTS.md

## Project Overview

Lumiere is a native Windows desktop screenshot tool focused on HDR-correct capture and preview. The application foundation is WinUI 3 on Windows App SDK with Windows Graphics Capture, Direct3D 11, DXGI, and Vortice for the future GPU-resident HDR pipeline.

## Agent Entrypoints

- Start with `README.md` for the repository overview, platform constraints, validation commands, and commit convention.
- Read `harness/README.md` for durable project context and reusable guidance.
- Use `harness/planning/project-plan.md` for long-lived product and architecture intent.
- Use `harness/workflows/cross-platform-development.md` for the supported macOS editing, Windows CI, and Windows hardware validation workflow.
- Treat `_bmad-output/` as generated or stage-specific planning output, not as the durable source of truth unless a task explicitly points there.

## Platform Constraints

- Target `.NET 10` with `net10.0-windows10.0.19041.0`.
- Keep the primary architecture as `x64` / `win-x64`; do not use `Any CPU`.
- Preserve the native Windows foundation: WinUI 3, Windows App SDK, Windows Graphics Capture, Direct3D 11, DXGI, and Vortice.
- The main preview path must preserve FP16/scRGB HDR data.

## Coding Constraints

- Keep module boundaries narrow:
  - `Lumiere.App` wires WinUI startup and composition.
  - `Lumiere.Overlay` owns overlay and crop UI behavior.
  - `Lumiere.Capture` owns Windows Graphics Capture lifecycle.
  - `Lumiere.Graphics` owns D3D11/DXGI rendering and presentation.
  - `Lumiere.Infrastructure` owns interop, diagnostics, result types, and UI-thread helpers.
  - `Lumiere.Settings` owns local preferences only.
- Do not introduce Electron, Tauri, WPF bitmap-first, WinForms, GDI, web UI, cloud upload, telemetry, or SDR screenshot-library foundations.
- Put platform APIs behind the existing boundary projects before exposing small interfaces to other modules.
- Manage WGC, Vortice, DXGI, and COM resources explicitly with correct disposal semantics.

## Validation Commands

Full validation requires Windows. From the repository root, run:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

macOS is suitable for editing, documentation, refactoring, API design, and platform-neutral test design. WinUI, WGC, DXGI, D3D11, HDR display behavior, and multi-monitor behavior require Windows validation.

## Collaboration Rules

- Follow the user's requested language for responses; this repository currently expects Chinese replies unless the user asks otherwise.
- Read the relevant project, tests, and harness documents before changing code.
- Keep edits scoped to the requested behavior and existing architecture.
- Do not claim full completion for HDR, WinUI, WGC, DXGI, or D3D11 behavior unless the result is clearly labeled with the validation level: Mac edit, Windows CI, or Windows manual validation.

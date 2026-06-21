# Lumiere

Lumiere is a native Windows desktop screenshot tool focused on HDR-correct capture and preview. The application foundation is WinUI 3 on Windows App SDK with Windows Graphics Capture, Direct3D 11, DXGI, and Vortice for the future GPU-resident HDR pipeline.

The current public release target is validated perfect HDR fidelity for supported paths. The existing capture and workflow baseline is treated as the implementation foundation; public release requires target-aware HDR detection, documented output semantics, compatibility evidence, and Windows manual validation.

## Platform Constraints

- Target runtime: `.NET 10` with `net10.0-windows10.0.19041.0`.
- Primary architecture: `x64` / `win-x64`; do not use `Any CPU`.
- Main preview path must preserve FP16/scRGB HDR data.
- Public HDR-preserving claims require target-aware display evidence plus output format, conversion, metadata, target-app compatibility, and Windows manual validation records.
- Do not introduce Electron, Tauri, WPF bitmap-first, WinForms, GDI, web UI, cloud upload, telemetry, or SDR screenshot-library foundations.

## Prerequisites

Development and full validation require Windows:

- Visual Studio 2022 with WinUI / Windows App SDK desktop development workloads.
- .NET 10 SDK.
- Windows SDK `10.0.26100.x` or a documented compatible Windows SDK.

Non-Windows machines can inspect and edit the repository, but WinUI restore/build validation is expected to run on Windows. See [Mac + Windows development workflow](harness/workflows/cross-platform-development.md) for the supported split between macOS editing, Windows CI, and Windows hardware validation.

## Repository Layout

```text
src/
  Lumiere.App/             WinUI app startup and composition
  Lumiere.Overlay/         Future overlay and crop UI behavior
  Lumiere.Capture/         Future Windows.Graphics.Capture lifecycle
  Lumiere.Graphics/        Future D3D11/DXGI rendering and presentation
  Lumiere.Infrastructure/  Interop, diagnostics, result types, UI-thread helpers
  Lumiere.Settings/        Local preferences only
tests/                     Future test projects mirroring source boundaries
```

## Developer Workflow

Lumiere supports a Mac-edit/Windows-validate workflow:

- macOS is suitable for code editing, documentation, refactoring, API design, and platform-neutral test design.
- Windows CI or a Windows development machine must run restore/build/test/format before review.
- A real Windows machine is required for WinUI, WGC, DXGI, D3D11, HDR display, and multi-monitor validation.

Before review, run the validation sequence from the repository root:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

Do not fake HDR graphics tests before the production graphics lifecycle exists. Automated tests can cover configuration, state, and lifecycle behavior; real HDR presentation still requires Windows hardware validation.

## Commit Convention

Use concise Conventional Commit prefixes:

- `feat:` for user-visible capability or product behavior.
- `fix:` for defects.
- `docs:` for documentation-only changes.
- `chore:` for scaffold, build, repository, or maintenance work.
- `test:` for test-only changes.

Examples:

```text
chore: scaffold native windows solution
docs: document hdr validation workflow
```

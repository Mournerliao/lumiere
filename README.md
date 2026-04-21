# Lumiere

Lumiere is a native Windows desktop screenshot tool focused on HDR-correct capture and preview. The application foundation is WinUI 3 on Windows App SDK with Windows Graphics Capture, Direct3D 11, DXGI, and Vortice for the future GPU-resident HDR pipeline.

## Platform Constraints

- Target runtime: `.NET 10` with `net10.0-windows10.0.19041.0`.
- Primary architecture: `x64` / `win-x64`; do not use `Any CPU`.
- Main preview path must preserve FP16/scRGB HDR data.
- Do not introduce Electron, Tauri, WPF bitmap-first, WinForms, GDI, web UI, cloud upload, telemetry, or SDR screenshot-library foundations.

## Prerequisites

Development and full validation require Windows:

- Visual Studio 2022 with WinUI / Windows App SDK desktop development workloads.
- .NET 10 SDK.
- Windows SDK `10.0.26100.x` or a documented compatible Windows SDK.

Non-Windows machines can inspect and edit the repository, but WinUI restore/build validation is expected to run on Windows.

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

Before review, run the validation sequence from the repository root:

```bash
dotnet format Lumiere.sln
dotnet restore Lumiere.sln
dotnet build Lumiere.sln -p:Platform=x64
```

When tests are added, include the relevant `dotnet test` command in the same pre-review sequence. Do not fake HDR graphics tests before the production graphics lifecycle exists.

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


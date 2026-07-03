# Lumiere

Lumiere is a native Windows desktop screenshot tool focused on HDR-aware capture and preview. The first release target is a usable MVP: fast screenshots, honest HDR status, an FP16/scRGB-oriented preview path, and compatible clipboard/file output.

Lumiere does not currently claim broad HDR-preserved export support. HDR-preserved file export remains a future milestone that requires a narrow supported path, documented conversion and metadata policy, named viewer assumptions, and Windows manual validation.

## Current Docs

- [MVP product scope](docs/product/mvp.md)
- [Product roadmap](docs/product/roadmap.md)
- [Architecture](docs/engineering/architecture.md)
- [Engineering workflows](docs/engineering/workflows.md)
- [MVP validation checklist](docs/validation/mvp-checklist.md)
- [HDR notes](docs/validation/hdr-notes.md)

## Platform Constraints

- Target runtime: `.NET 10` with `net10.0-windows10.0.19041.0`.
- Primary architecture: `x64` / `win-x64`; do not use `Any CPU`.
- Production UI must remain native WinUI 3 / Windows App SDK.
- Capture and preview must remain based on Windows Graphics Capture, Direct3D 11, DXGI, and Vortice.
- Main preview work should preserve the FP16/scRGB HDR direction.
- Public HDR-preserved claims require target-aware display evidence, documented output semantics, target-app compatibility, and Windows manual validation.
- Do not introduce Electron, Tauri, WPF bitmap-first, WinForms, GDI screenshot-library foundations, web UI, cloud upload, or telemetry.

## Repository Layout

```text
src/
  Lumiere.App/             WinUI startup and window composition
  Lumiere.Overlay/         Fullscreen overlay and crop UI behavior
  Lumiere.Capture/         Windows Graphics Capture lifecycle
  Lumiere.Graphics/        D3D11/DXGI rendering and presentation
  Lumiere.Infrastructure/  Interop, diagnostics, result types, UI-thread helpers
  Lumiere.Settings/        Local preferences
tests/                     Test projects mirroring source boundaries
docs/                      Current product, engineering, validation, and decision docs
```

## Developer Workflow

Development and full validation require Windows:

- Visual Studio 2022 with WinUI / Windows App SDK desktop development workloads.
- .NET 10 SDK.
- Windows SDK `10.0.26100.x` or a documented compatible Windows SDK.

macOS is suitable for editing, documentation, refactoring, and platform-neutral test design. Windows is required for WinUI restore/build validation and real WGC, DXGI, D3D11, HDR display, tray, shortcut, clipboard, and multi-monitor behavior.

Before review on Windows, run:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

For local app launch, prefer:

```bash
dotnet run --project src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64
```

Use `--no-restore` only after a successful restore in the same workspace state.

## Commit Convention

Use concise Conventional Commit prefixes:

- `feat:` for user-visible capability or product behavior.
- `fix:` for defects.
- `docs:` for documentation-only changes.
- `chore:` for scaffold, build, repository, or maintenance work.
- `test:` for test-only changes.

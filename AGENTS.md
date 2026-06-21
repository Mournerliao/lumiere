# AGENTS.md

## Project Overview

Lumiere is a native Windows HDR screenshot tool built with WinUI 3, Windows App SDK, Direct3D 11, DXGI, and Vortice. The core pipeline captures FP16 frames via Windows Graphics Capture and presents them through a scRGB swap chain.

## Platform Constraints

- Target: `.NET 10` / `net10.0-windows10.0.19041.0` / `x64` only
- Preserve HDR: FP16/scRGB format, never introduce SDR fallbacks
- Public HDR-preserving claims require target-aware display evidence, output format/conversion/metadata policy, target-app compatibility, and Windows manual validation.
- Windows-only: WGC, DXGI, D3D11, WinUI 3

## Architecture

| Module | Responsibility |
|---|---|
| `Lumiere.App` | WinUI startup, window composition |
| `Lumiere.Graphics` | D3D11 device, swap chain, HDR constants |
| `Lumiere.Capture` | WGC frame pool, capture lifecycle |
| `Lumiere.Infrastructure` | COM/WinRT interop, diagnostics |
| `Lumiere.Overlay` | Full-screen overlay, crop UI |
| `Lumiere.Settings` | Local preferences |

**Rule:** Platform APIs must stay in their boundary module. Expose narrow interfaces.

## Coding Constraints

- Use structured logging (`ILogger` via `LumiereLoggerFactory`) — never `Console.WriteLine`
- Manage COM/DXGI resources with deterministic disposal
- Follow existing patterns; read harness docs before changes

## Validation Commands

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

## NuGet Restore/Run Guidance

For ad-hoc local app launch, prefer `dotnet run --project src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64` without `--no-restore`. Use `--no-restore` only after a successful restore in the same workspace state.

If `dotnet build`, `dotnet test`, or `dotnet run` fails with `NETSDK1064` and says a package was not found after restore, treat it as a stale/partial NuGet restore or cache issue before debugging source code. Stop using `--no-restore`, run:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false --force
```

Then retry the original command. If it still fails, follow `harness/workflows/nuget-restore-recovery.md`.

## Commit Convention

```
feat:  user-visible capability
fix:   defect fix
docs:  documentation only
chore: scaffold, build, repo maintenance
test:  test-only changes
```

## Skills & Workflows

This project uses BMad skills for requirements, architecture, and sprint planning. Project-specific skills are in `harness/skills/`.

## Key Files

- `harness/README.md` — project context and reusable guidance
- `harness/planning/project-plan.md` — product intent
- `harness/workflows/cross-platform-development.md` — Mac-edit/Windows-validate workflow

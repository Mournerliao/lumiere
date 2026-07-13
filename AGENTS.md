# AGENTS.md

## Project Overview

Lumiere is a native Windows HDR-aware screenshot tool built with WinUI 3, Windows App SDK, Direct3D 11, DXGI, Windows Graphics Capture, and Vortice. The MVP goal is fast native screenshot capture with honest HDR state and compatible output, not a broad HDR-preserved claim.

## Start Here

- Knowledge index: `knowledge/README.md`
- Product scope: `knowledge/product/mvp.md`
- Roadmap: `knowledge/product/roadmap.md`
- Architecture boundaries: `knowledge/engineering/architecture.md`
- Workflows: `knowledge/engineering/workflows.md`
- MVP validation: `knowledge/validation/mvp-checklist.md`
- HDR notes: `knowledge/validation/hdr-notes.md`

## Platform Constraints

- Target: `.NET 10` / `net10.0-windows10.0.19041.0` / `x64` only.
- Windows-only production stack: WGC, DXGI, D3D11, WinUI 3.
- Preserve the FP16/scRGB preview direction.
- Do not introduce SDR-first screenshot-library foundations.
- Public HDR-preserved claims require target-aware display evidence, documented output semantics, target-app compatibility, and Windows manual validation.

## Architecture

| Module | Responsibility |
|---|---|
| `Lumiere.App` | WinUI startup, dependency composition, windows, app-level projections |
| `Lumiere.Graphics` | D3D11 device, DXGI swap chain, HDR constants and graphics contracts |
| `Lumiere.Capture` | WGC target selection, frame pool lifecycle, capture state |
| `Lumiere.Infrastructure` | COM/WinRT interop, diagnostics, typed results, UI-thread helpers |
| `Lumiere.Overlay` | Fullscreen overlay, crop UI, overlay cues |
| `Lumiere.Settings` | Local preferences and settings projections |

Platform APIs must stay in their boundary module. Expose narrow interfaces upward.

## Coding Constraints

- Use structured logging through `ILogger` via `LumiereLoggerFactory`; never use `Console.WriteLine`.
- Manage COM/DXGI/D3D11/WGC resources with deterministic disposal.
- Follow existing patterns before introducing new abstractions.
- Keep output artifact success separate from HDR-preserved claims.

## Validation Commands

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

## NuGet Restore/Run Guidance

For ad-hoc local app launch, prefer:

```bash
dotnet run --project src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64
```

Use `--no-restore` only after a successful restore in the same workspace state.

If `dotnet build`, `dotnet test`, or `dotnet run` fails with `NETSDK1064` and says a package was not found after restore, treat it as a stale or partial NuGet restore/cache issue before debugging source code. Stop using `--no-restore`, run:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false --force
```

Then retry the original command.

## Commit Convention

```text
feat:  user-visible capability
fix:   defect fix
docs:  documentation only
chore: scaffold, build, repo maintenance
test:  test-only changes
```

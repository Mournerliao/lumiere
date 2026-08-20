# Windows Native Development Runbook

macOS is suitable for the shared shell, documentation, refactoring, and platform-neutral
test design. Windows is required for restore/build confidence and all WGC, DXGI,
D3D11, HDR, Windows tray/shortcut/clipboard, and multi-monitor behavior.

## Prerequisites

- Visual Studio 2022 with WinUI / Windows App SDK desktop workloads
- .NET 10 SDK
- Windows SDK `10.0.26100.x` or a documented compatible version

## Release-Candidate Gates

First run the shared-shell gates from `cross-platform-development.md`, then run the
versioned Windows-native verification entry point:

```powershell
pwsh ./scripts/verify-windows.ps1
```

It restores once, builds x64, runs both test projects, and verifies formatting.

Run the app with restore enabled unless the same workspace state restored successfully:

```powershell
dotnet run --project src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64
```

## NuGet Recovery

If build, test, or run fails with `NETSDK1064` and a package is missing after restore,
treat the cache as stale/partial before changing source:

```powershell
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false --force
```

Then retry without `--no-restore`.

## Embedded Resource Recovery

If compilation fails with `CS1566` and names an old embedded-resource path, inspect
evaluated items first:

```powershell
dotnet msbuild src/Lumiere.App.Core/Lumiere.App.Core.csproj -p:Platform=x64 -getItem:EmbeddedResource
```

When evaluated items are current but the compiler remains stale, shut down build
servers, remove ignored `bin`/`obj`, restore, and retry. If it persists, capture an
MSBuild binary log before changing source paths.

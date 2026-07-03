# Engineering Workflows

## Mac Edit, Windows Validate

macOS can be used for code editing, documentation, refactoring, and platform-neutral test design. Windows is required for restore/build validation and all WinUI, WGC, DXGI, D3D11, HDR display, tray, shortcut, clipboard, and multi-monitor checks.

Before review on Windows, run:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

For ad-hoc local app launch, prefer:

```bash
dotnet run --project src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64
```

Use `--no-restore` only after a successful restore in the same workspace state.

## NuGet Recovery

If `dotnet build`, `dotnet test`, or `dotnet run` fails with `NETSDK1064` after restore and says a package was not found, treat it as a stale or partial NuGet restore/cache issue before debugging source code.

Run:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false --force
```

Then retry the original command without changing source code. If the failure remains, inspect the package cache, SDK version, and Windows workload installation.

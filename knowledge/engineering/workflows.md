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

## Embedded Resource Recovery

If `dotnet build` or `dotnet run` fails with `CS1566` and reports an old embedded-resource path after the project file has already moved to a new path, first inspect the evaluated resource items:

```powershell
dotnet msbuild src/Lumiere.App.Core/Lumiere.App.Core.csproj -p:Platform=x64 -getItem:EmbeddedResource
```

Only when the evaluated items show the current paths but the compiler still reports an old path, stop the persistent build servers and remove ignored build outputs before restoring again:

```powershell
dotnet build-server shutdown
Get-ChildItem src,tests -Recurse -Directory |
  Where-Object { $_.Name -in @("bin", "obj") } |
  Remove-Item -Recurse -Force

dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
```

Then retry the normal build and run commands. If the compiler still receives the old resource path, capture an MSBuild binary log and inspect the evaluated `EmbeddedResource` items and the C# compiler `Resources` input before changing source paths again.

## NuGet Recovery

If `dotnet build`, `dotnet test`, or `dotnet run` fails with `NETSDK1064` after restore and says a package was not found, treat it as a stale or partial NuGet restore/cache issue before debugging source code.

Run:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false --force
```

Then retry the original command without changing source code. If the failure remains, inspect the package cache, SDK version, and Windows workload installation.

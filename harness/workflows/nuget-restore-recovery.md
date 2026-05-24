# NuGet Restore And Run Guidance

This workflow covers local Windows launch/build habits and recurring restore failures such as:

```text
error NETSDK1064: Package Microsoft.Extensions.Logging.Abstractions, version 9.0.4 was not found.
It might have been deleted since NuGet restore.
```

## Why It Happens

Lumiere uses centralized NuGet package versions in `Directory.Packages.props`. Commands such as `dotnet build --no-restore`, `dotnet test --no-restore`, and `dotnet run --no-restore` trust the existing `obj/project.assets.json` files.

`NETSDK1064` usually means the assets file still points at a package/version, but the package is missing or incomplete in the NuGet global package cache. Common causes:

- a NuGet restore was interrupted or only partially completed;
- the global NuGet package cache was cleaned by a tool;
- switching branches changed package versions while stale `obj` assets remained;
- a previous restore failed because of network, antivirus, path length, or file lock issues.

This is normally an environment/cache problem, not a source-code compile error.

## Daily App Launch

For ad-hoc local app launch, prefer letting `dotnet run` restore when needed:

```powershell
dotnet run --project src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64
```

Use `--no-restore` only when a restore has already succeeded in the same workspace state, for example after running the validation sequence from `AGENTS.md`.

## Standard Recovery

Run these from the repository root on Windows.

1. Stop using `--no-restore` until restore succeeds.

2. Force a full solution restore:

```powershell
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false --force
```

3. Retry the command that failed:

```powershell
dotnet run --project src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 --no-restore
```

or the usual validation commands:

```powershell
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
```

## If It Still Fails

Clear NuGet caches, restore without the HTTP cache, then retry:

```powershell
dotnet nuget locals all --clear
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false --no-cache --force
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
```

If only one package is named and you want a narrower cleanup, delete that package folder from the global package cache, then restore:

```powershell
dotnet nuget locals global-packages --list
```

Then remove the named package/version folder shown by the cache path. For the common current error, remove:

```text
<global-packages-root>\microsoft.extensions.logging.abstractions\9.0.4
```

After removal:

```powershell
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false --force
```

## Related File Locks

If build or run fails while copying `Lumiere.App.exe`, check for a running Lumiere process. The app can lock the output executable while it is open. Close Lumiere, then rerun the build.

Do not use `git clean`, delete `bin/obj` broadly, or reset the worktree as the first response. Prefer restore/cache recovery first.

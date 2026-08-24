# Windows Engine Development Runbook

Windows host adaptation is active. The repository contains three native libraries and
no Windows executable yet. Windows is required for .NET restore, Release build, tests,
formatting, WGC/D3D11/DXGI runtime behavior, clipboard behavior, and HDR hardware checks.

## Prerequisites

- .NET 10 SDK
- Windows SDK `10.0.26100.x` or a documented compatible version
- x64 Windows

## Repository Gate

Run the shared gates first, then the Windows-owned entry point from repository root:

```sh
pnpm install --frozen-lockfile
pnpm check
pnpm test
pnpm build
```

```powershell
pwsh ./hosts/windows/scripts/verify.ps1
```

The PowerShell script restores and Release-builds `hosts/windows/Lumiere.Windows.sln`, runs
the Capture, Graphics, and Interop test projects, and verifies formatting.

## Truth Boundary

A passing library build does not prove a runnable Windows host, native capture, HDR
Visual Match, or hardware support. Those checks resume only after a Windows host
executable implements `protocol/platform-host`.

If restore reports a partial NuGet cache, rerun restore with `--force` against the
Windows solution before changing source.

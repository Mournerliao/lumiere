# Windows Engine Development Runbook

Windows host adaptation is active. The repository contains a platform-host v2 executable
with a capability handshake plus the three retained native libraries. Windows is required
for .NET restore, Release build, tests, formatting, WGC/D3D11/DXGI runtime behavior,
clipboard behavior, and HDR hardware checks.

## Prerequisites

- .NET 10 SDK
- Windows SDK `10.0.26100.x` or a documented compatible version
- x64 Windows

## Repository Gate

Run the shared gates first, then the Windows-owned entry point from repository root:

```sh
pnpm install --frozen-lockfile
pnpm check
pnpm test:shared
pnpm build
```

```powershell
pwsh ./hosts/windows/scripts/verify.ps1
```

The PowerShell script restores and Release-builds `hosts/windows/Lumiere.Windows.sln`, runs
the Capture, Graphics, and Interop test projects, and verifies formatting. This
Windows-owned suite runs only in Windows CI; it is not part of the shared `pnpm test`
alias and does not execute macOS tests.

The repository-root `pnpm dev` command builds the current Windows Debug Host unless
`LUMIERE_WINDOWS_HOST_PATH` is set, then Electron selects that artifact ahead of a Release
fallback. The current Host reports itself available but advertises no capture modes until
the retained engine is connected; capture actions therefore remain honestly disabled.

## Truth Boundary

A passing Host handshake does not prove native capture, HDR Visual Match, or hardware
support. Those checks resume after the Host connects the retained engine.

If restore reports a partial NuGet cache, rerun restore with `--force` against the
Windows solution before changing source.

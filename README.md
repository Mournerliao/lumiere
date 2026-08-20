# Lumiere

Lumiere is a Windows and macOS HDR-aware screenshot tool. A shared Electron/React
shell coordinates native capture hosts: WGC/D3D11/DXGI on Windows and
ScreenCaptureKit on macOS. The first release focuses on fast region/display capture,
honest target-aware HDR state, and compatible sRGB Visual Match output.

Lumiere does not currently claim HDR-preserved export support.

## Start Here

- [Current project state](knowledge/state/CURRENT.md)
- [Product roadmap](knowledge/roadmap.md)
- [Knowledge map](knowledge/README.md)
- [Product contract](knowledge/contracts/product.md)
- [Cross-platform development runbook](knowledge/runbooks/cross-platform-development.md)
- [Windows development runbook](knowledge/runbooks/windows-development.md)

The repository uses a lightweight Contract → Frontier → Verification workflow.
GitHub Issues own non-trivial tasks and observed checks, contracts own stable
boundaries, `CURRENT.md` owns the frontier, and Git owns history.

## Platform

`Electron` · `React` · `TypeScript` · Windows native host (`.NET 10`, WGC,
D3D11, DXGI, Vortice) · macOS native host (Swift, ScreenCaptureKit)

macOS can build and verify the shared shell. Each native host and all HDR claims still
require runtime and hardware verification on its owning platform.

## Repository Layout

```text
apps/       shared Electron desktop shell
protocol/   language-neutral platform-host schemas and fixtures
hosts/      native macOS and Windows ownership trees
knowledge/  contracts, current state, ADRs, roadmap, and runbooks
scripts/    cross-repository structural checks
```

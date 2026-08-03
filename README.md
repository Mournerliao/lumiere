# Lumiere

Lumiere is a native Windows HDR-aware screenshot tool built with WinUI 3,
Windows App SDK, Windows Graphics Capture, Direct3D 11, DXGI, and Vortice.
The first release focuses on fast region/fullscreen capture, target-aware HDR state,
an FP16/scRGB-oriented preview path, and compatible sRGB Visual Match output.

Lumiere does not currently claim broad HDR-preserved export support.

## Start Here

- [Current project state](knowledge/state/CURRENT.md)
- [Knowledge map](knowledge/README.md)
- [Product contract](knowledge/contracts/product.md)
- [Windows development runbook](knowledge/runbooks/windows-development.md)
- [Validation evidence](knowledge/evidence/README.md)

The repository uses a lightweight Contract → Frontier → Evidence workflow.
GitHub Issues own non-trivial tasks; contracts own stable boundaries; evidence owns
observed validation; Git owns history.

## Platform

`.NET 10` · `net10.0-windows10.0.19041.0` · `x64` / `win-x64` · WinUI 3 ·
Windows App SDK · WGC · D3D11 · DXGI · Vortice

Windows is required for full build/runtime validation and real HDR evidence. Follow
the Windows runbook rather than copying validation commands into additional docs.

## Repository Layout

```text
src/        application, capture, graphics, interop, overlay, and settings modules
tests/      automated tests mirroring source boundaries
knowledge/  contracts, current state, ADRs, runbooks, evidence, and research
scripts/    deterministic engineering entry points
```

# Windows Host

Windows development is paused after repository convergence. This directory contains
no executable and no WinUI product shell.

The retained modules are:

- `Lumiere.Windows.Capture` for WGC target resolution, session state, and frame lifetime.
- `Lumiere.Windows.Graphics` for D3D11/DXGI, HDR-aware input, sRGB Visual Match, PNG,
  clipboard, and folder delivery.
- `Lumiere.Windows.Interop` for the COM/WinRT and diagnostic implementation those
  modules require.

Run `./scripts/verify.ps1` on Windows to restore, build, test, and format-check the
paused engine. Future work adds a process adapter conforming to
`../../protocol/platform-host/v1.schema.json`; it must not restore a second product UI.

# Windows Engine

Windows host adaptation is the active Milestone 1B frontier. This directory still
contains no executable and no WinUI product shell; the retained engine now exposes one
deep `WindowsDisplayCaptureEngine.CaptureDisplayAsync` interface for the future Host.

The retained modules are:

- `Lumiere.Windows.Capture` for WGC target resolution, session state, and frame lifetime.
- `Lumiere.Windows.Graphics` for D3D11/DXGI, HDR-aware input, sRGB Visual Match, PNG,
  clipboard, and folder delivery.
- `Lumiere.Windows.Interop` for the COM/WinRT and diagnostic implementation those
  modules require.

The capture interface owns target resolution, target-aware HDR probing, first-frame
acquisition, one sRGB Visual Match conversion, requested delivery, cancellation, and
native teardown. A caller supplies a correlation ID but never owns a raw frame,
texture, capture session, or output cache. Call
`WindowsDisplayCaptureEngine.ConfigureLogging` before creating the engine when the
process needs a structured stderr logger.

Run `./scripts/verify.ps1` on Windows to restore, Release-build, test, and format-check
the engine. Future work adds a process adapter conforming to
`../../protocol/platform-host/v1.schema.json`; it must not restore a second product UI.

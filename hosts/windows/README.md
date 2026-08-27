# Windows Host And Engine

Windows host adaptation is the active Milestone 1B frontier. The
`Lumiere.Windows.Host` executable owns the platform-host v2 JSON Lines process boundary;
it currently provides the capability handshake and typed capture-unavailable behavior
while the retained engine is connected in the next vertical slice. It does not restore a
WinUI product shell or claim working Windows capture.

The retained modules are:

- `Lumiere.Windows.Capture` for WGC target resolution, session state, and frame lifetime.
- `Lumiere.Windows.Graphics` for D3D11/DXGI, HDR-aware input, sRGB Visual Match, PNG,
  clipboard, and folder delivery.
- `Lumiere.Windows.Interop` for the COM/WinRT and diagnostic implementation those
  modules require.
- `Lumiere.Windows.Host` for stdin/stdout protocol handling and structured stderr
  diagnostics.

The capture interface owns target resolution, target-aware HDR probing, first-frame
acquisition, one sRGB Visual Match conversion, requested delivery, cancellation, and
native teardown. A caller supplies a correlation ID but never owns a raw frame,
texture, capture session, or output cache. Call
`WindowsDisplayCaptureEngine.ConfigureLogging` before creating the engine when the
process needs a structured stderr logger.

The executable conforms to `../../protocol/platform-host/v2.schema.json`. A Debug build
lives at
`src/Lumiere.Windows.Host/bin/x64/Debug/net10.0-windows10.0.19041.0/win-x64/Lumiere.Windows.Host.exe`;
the Electron development launcher builds and selects that artifact before a Release
fallback. `LUMIERE_WINDOWS_HOST_PATH` remains the authoritative development override.

Run `./scripts/verify.ps1` on Windows to restore, Release-build, test, and format-check
the Host and retained engine.

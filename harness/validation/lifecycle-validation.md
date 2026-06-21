# Lifecycle Validation Checklist

Use this checklist before claiming `Windows manual-pass` for repeated capture lifecycle stability. Automated tests can verify state transitions and disposal sequencing, but real WGC frame pools, DXGI swap chains, D3D11 resources, HDR display behavior, and GPU memory trends require Windows hardware validation.

This workflow is the smoke-to-mid-depth lifecycle pass. For the public-release `50+` / `100+` resource trend runs, pair it with `resource-trend-validation.md`.

## Required scenarios

Run each scenario at least once, then run the repeated sequence loop.

1. Start capture through the default direct monitor path and confirm no picker appears.
2. Stop an active capture session.
3. Press Escape from the overlay before completing a crop.
4. Restart direct capture on the same monitor.
5. Restart direct capture on a different monitor where available.
6. Trigger or simulate frame-size recreation for the active target.
7. Exercise failed initialization or unsupported capture recovery.
8. Close the app window while capture is active.
9. Repeat start, stop, cancel, restart, and close-adjacent cleanup paths in a loop.
10. Exercise `GraphicsCapturePicker` only as fallback/debug behavior, if that path remains exposed.

## Inspect after each scenario

- Final state returns to `Idle`, `Disposed`, `Unsupported`, `Degraded`, or `Failed`; it must not remain stuck in `SelectingTarget`, `Initializing`, or stale `Capturing`.
- The preview window and overlay state do not remain stuck after stop, cancel, failed initialization, or close.
- No frame callback updates the UI after stop, restart, recreate, or close.
- Capture teardown evidence shows frame handler unsubscribe, session stop/dispose, frame pool dispose, and WinRT Direct3D device dispose for session-owned resources.
- Presentation teardown evidence shows `SetSwapChain(null)` completes before DXGI swap-chain resources are released.
- Ordinary stop or restart does not dispose shared `GraphicsDeviceResources`.
- Recreated previews keep the FP16/scRGB path: WGC `R16G16B16A16Float`, DXGI `R16G16B16A16_FLOAT`, and scRGB color-space evidence.
- GPU memory and handle counts do not show unbounded growth across repeated sessions.
- When the run is intended to count toward Story `12-3`, sampler artifacts are recorded through `resource-trend-validation.md`.

## Repeated sequence loop

Run this sequence several times without restarting the app:

1. Start direct capture on monitor/target A and wait for `HDR-ready` or a recoverable degraded/failure state.
2. Stop capture and confirm teardown evidence is complete.
3. Start direct capture on target A again and confirm stale frame events from the previous generation do not update status.
4. Stop capture.
5. Start direct capture on target B and confirm old target evidence does not leak into the new status.
6. Press Escape before completing a crop and confirm capture/preview resources are torn down.
7. Trigger resize/recreate if available, then confirm the mismatched frame is skipped and replacement resources are created.
8. Close the window from an active or initializing session and confirm no post-close UI updates occur.

For Public perfect-HDR-fidelity release evidence, extend this repeated loop into the longer cycle plans in `resource-trend-validation.md` so the session also records CSV/JSON trend artifacts.

## Validation level notes

- `Windows CI-pass` may cover restore, build, tests, and formatting.
- `Windows manual-pass` additionally requires running this checklist on Windows hardware with real WGC, DXGI, D3D11, and HDR-capable preview conditions where applicable.
- Unit tests must not be used to claim GPU memory stability or real frame pool/swap-chain behavior by themselves.

# Lifecycle Validation Checklist

Use this checklist before claiming `Windows manual-pass` for repeated capture lifecycle stability. Automated tests can verify state transitions and disposal sequencing, but real WGC frame pools, DXGI swap chains, D3D11 resources, HDR display behavior, and GPU memory trends require Windows hardware validation.

## Required scenarios

Run each scenario at least once, then run the repeated sequence loop.

1. Start capture and select a valid display or window target.
2. Stop an active capture session.
3. Start target selection and cancel the picker.
4. Restart capture with the same target.
5. Restart capture with a different target.
6. Trigger or simulate frame-size recreation for the active target.
7. Exercise failed initialization or unsupported capture recovery.
8. Close the app window while capture is active.
9. Repeat start, stop, cancel, restart, and close-adjacent cleanup paths in a loop.

## Inspect after each scenario

- Final state returns to `Idle`, `Disposed`, `Unsupported`, `Degraded`, or `Failed`; it must not remain stuck in `SelectingTarget`, `Initializing`, or stale `Capturing`.
- The preview window and overlay state do not remain stuck after stop, cancel, failed initialization, or close.
- No frame callback updates the UI after stop, restart, recreate, or close.
- Capture teardown evidence shows frame handler unsubscribe, session stop/dispose, frame pool dispose, and WinRT Direct3D device dispose for session-owned resources.
- Presentation teardown evidence shows `SetSwapChain(null)` completes before DXGI swap-chain resources are released.
- Ordinary stop or restart does not dispose shared `GraphicsDeviceResources`.
- Recreated previews keep the FP16/scRGB path: WGC `R16G16B16A16Float`, DXGI `R16G16B16A16_FLOAT`, and scRGB color-space evidence.
- GPU memory and handle counts do not show unbounded growth across repeated sessions.

## Repeated sequence loop

Run this sequence several times without restarting the app:

1. Select target A and wait for `HDR-ready` or a recoverable degraded/failure state.
2. Stop capture and confirm teardown evidence is complete.
3. Select target A again and confirm stale frame events from the previous generation do not update status.
4. Stop capture.
5. Select target B and confirm old target evidence does not leak into the new status.
6. Cancel target selection and confirm no WGC session starts.
7. Trigger resize/recreate if available, then confirm the mismatched frame is skipped and replacement resources are created.
8. Close the window from an active or initializing session and confirm no post-close UI updates occur.

## Validation level notes

- `Windows CI-pass` may cover restore, build, tests, and formatting.
- `Windows manual-pass` additionally requires running this checklist on Windows hardware with real WGC, DXGI, D3D11, and HDR-capable preview conditions where applicable.
- Unit tests must not be used to claim GPU memory stability or real frame pool/swap-chain behavior by themselves.

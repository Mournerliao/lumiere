# Overlay Validation

Story 3.1 adds the first fullscreen overlay host for the HDR preview path. Automated tests cover the hardware-independent state and layout seams; the following checks require a Windows desktop with WinUI, WGC, DXGI, D3D11, and HDR-capable display validation.

MVP rebaseline note: the default user path is direct monitor capture with no picker-first interruption, then drag a region and release the pointer to capture/copy. Explicit confirm controls are historical/internal foundation behavior unless a debug or fallback path exposes them.

## Automated Checks

Run from the repository root on Windows:

```powershell
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

## Manual Windows Checks

1. Start Lumiere on a Windows x64 machine with the Windows App SDK runtime installed.
2. Click the default Capture action and confirm no picker appears before overlay entry.
3. Confirm the direct monitor target is resolved and the overlay opens as a borderless topmost window on the intended monitor.
4. Confirm the hardware preview is attached through the overlay `SwapChainPanel` and fills the overlay surface.
5. Confirm the status/control layer appears above the preview and does not resize or shift the preview when status text changes.
6. Press Escape and verify the overlay closes and WGC frame pool/session plus swap-chain resources are torn down.
7. Reopen capture repeatedly and verify stale callbacks do not update a closed or replaced overlay.
8. Force or simulate a preview startup failure where practical and verify no unusable topmost overlay remains.
9. Repeat on HDR and SDR displays, with Windows scaling enabled, and record whether behavior is `Windows manual-pass`, degraded, or failed.

## Story 3.2 Crop Drag Checks

Story 3.2 adds crop creation over the live preview. In addition to the Story 3.1 checks:

1. Drag from top-left to bottom-right over the preview and verify the crop rectangle follows the pointer immediately.
2. Drag from bottom-right to top-left and verify the final rectangle normalizes to the expected origin and size.
3. Drag beyond each preview edge and verify the crop clamps to the visible preview area.
4. Create a tiny click or near-zero drag and verify no active crop remains.
5. Confirm the dimmed non-selected mask and black/white crop boundary remain visible over bright, dark, and high-contrast HDR content.
6. Press Escape or Cancel while creating a crop and verify overlay teardown still works without leaving a stuck creating state.
7. Repeat on high-DPI scaling, HDR/SDR displays, and multi-monitor placements; record whether behavior is `Windows manual-pass`, degraded, or failed.

Story 3.2 does not validate resize handles, release-to-copy output, annotations, or HDR export fidelity. Release-to-copy is covered by Story 3.6 and MVP clipboard output is covered by Epic 4.

## Story 3.3 Crop Adjustment Checks

Story 3.3 adds adjustment handles, edge resizing, crop recreation, and the pure DIP-to-capture-pixel coordinate mapping seam. In addition to the Story 3.1 and 3.2 checks:

1. Create an active crop, then drag each corner handle and verify both adjacent edges resize while the `SwapChainPanel`, status controls, cancel control, and crop canvas do not shift.
2. Drag each edge handle and verify only that edge moves, with the opposite edge staying anchored.
3. Drag a handle or edge past the opposite side and verify the crop normalizes without negative width, negative height, or visual jumps.
4. Drag handles outside the preview surface and verify the crop clamps to the visible preview bounds.
5. Try shrinking below the minimum crop size and verify the last valid active crop remains recoverable.
6. Press or click inside the active crop away from handles/edges and verify the crop is not accidentally destroyed.
7. Start a new drag outside the active crop and verify the previous crop is replaced only after the new crop commits as a valid selection.
8. Start a new outside drag, cancel it or force pointer capture loss, and verify the previous active crop is restored.
9. Confirm the white filled handles with dark strokes, dual crop boundary, and dimmed mask remain visible over bright, dark, and high-contrast HDR preview content.
10. Repeat handle and recreation gestures on common Windows scaling values, HDR and SDR displays, and multi-monitor placements; record whether behavior is `Windows manual-pass`, degraded, or failed.

Story 3.3 does not implement release-to-copy output, annotations, global hotkeys, tray workflow, or HDR export fidelity. Coordinate mapping is covered by automated tests and is ready for release-to-copy and MVP clipboard output stories to consume.

## Story 3.4 Confirm and Cancel Foundation Checks

Story 3.4 adds the typed confirmation/cancel foundation used internally by the later release-to-copy MVP path. These checks validate the foundation where it remains exposed through tests, debug UI, or fallback flows; explicit confirm controls are not the default MVP overlay interaction. In addition to the Story 3.1 through 3.3 checks:

1. If explicit controls are exposed, open the overlay without a crop and verify `Confirm crop` is disabled while `Cancel` remains available.
2. If explicit controls are exposed, create a valid crop and verify `Confirm crop` enables without resizing or shifting the `SwapChainPanel`, crop canvas, status panel, mask, or crop coordinate mapping.
3. Confirm the crop through the available foundation path and verify the app status reports a confirmed crop, including DIP crop, mapped pixel crop, and source frame size details.
4. Confirm that post-confirm teardown follows the existing resource order: WGC capture session/frame pool, preview surface detach through `SetSwapChain(null)`, then DXGI swap-chain resources.
5. Reopen capture after confirming and verify stale callbacks from the previous preview generation do not update the new overlay.
6. Cancel from a ready preview, a degraded preview, and while a crop gesture is in progress; verify the overlay closes and desktop state is restored.
7. Repeatedly click confirm or cancel and verify the operation is idempotent, with no double-dispose crash, stuck overlay, or resurrected status update.
8. Force or simulate degraded preview and verify confirm remains available only with a valid crop and that the confirmed status clearly says the preview was degraded.
9. Force or simulate unsupported capture, preview failure, closing, and disposed states; verify confirm is disabled or rejected, while cancel still closes wherever safe.
10. Repeat confirm and cancel checks on common Windows scaling values, HDR and SDR displays, and multi-monitor placements; record whether behavior is `Windows manual-pass`, degraded, or failed.

Story 3.4 still does not implement the default release-to-copy interaction, file export, annotation, capture history, tone mapping, HDR still image encoding, global hotkeys, tray workflow, or final HDR/SDR output semantics. The MVP completion path is validated in Story 3.6 and Epic 4.

## Story 3.5 Hit Testing and Escape Checks

Story 3.5 hardens overlay input routing while preserving the borderless topmost preview window. In addition to the Story 3.1 through 3.4 checks:

1. Open the overlay and verify the technical detail mentions interactive hit testing for crop input.
2. Drag on empty preview space over the `SwapChainPanel` and verify the transparent crop canvas receives pointer input instead of passing all input through the window.
3. Click visible status and any exposed fallback controls and verify only their visible bounds consume pointer input; nearby empty overlay space should still start or adjust crop gestures.
4. Press Escape while focus is on the root overlay surface, visible controls, status text/details, and after tab navigation; verify all routes raise the same cancel flow.
5. Press Escape repeatedly during ready, degraded, in-progress crop creation, and adjustment states; verify teardown remains idempotent with no double-dispose crash, stuck overlay, or resurrected status update.
6. Verify cancel remains keyboard reachable whenever the overlay can safely close; if explicit confirm controls are exposed, verify `Confirm crop` remains enabled only for valid confirmable crops in HDR-ready or degraded preview states.
7. Repeat on common Windows scaling values, HDR and SDR displays, and multi-monitor placements; record whether behavior is `Windows manual-pass`, degraded, or failed.

Story 3.5 does not introduce whole-window pass-through mode for crop-capable overlay states, release-to-copy output, annotation, capture history, global hotkeys, tray workflow, HDR still-image encoding, or SDR tone mapping.

## Story 3.6 Release-to-Capture Checks

Story 3.6 changes the MVP overlay completion path from explicit confirm controls to release-to-capture/copy. In addition to the Story 3.1 through 3.5 checks:

1. Click the default Capture action and confirm no picker appears before overlay entry.
2. Drag a valid crop and release the pointer; verify the crop is confirmed without clicking a Confirm button.
3. Verify the overlay does not expose a multi-action toolbar, output choices, annotation tools, tray controls, or settings controls.
4. Verify lightweight "Copied to clipboard" feedback appears in the closing state message without resizing or shifting the preview/crop coordinate mapping.
5. Press Escape before release and verify the overlay cancels with deterministic teardown.
6. Try a tiny or invalid crop and verify no output is produced and the overlay remains active.
7. Reopen capture repeatedly and verify no stale callbacks from the previous preview generation update the new overlay or copied-feedback state.
8. Repeat release-to-capture over bright, dark, and high-contrast HDR content, SDR displays, fullscreen apps, and multi-monitor placements; record whether behavior is `Windows manual-pass`, degraded, or failed.

### Clipboard Output Validation

9. After release-to-capture, verify the Windows clipboard contains a bitmap by pasting into Paint, Snipping Tool, or another image editor.
10. Verify the pasted image dimensions match the crop region pixel dimensions (accounting for display scaling).
11. Verify the clipboard write does not leave capture resources active; the overlay should close cleanly after clipboard output.
12. Force or simulate a clipboard write failure (e.g., clipboard locked by another app) and verify the overlay still closes and capture resources are torn down.

### Release-to-Capture Auto-Confirm Logic

13. Drag a valid crop and release; verify `CaptureConfirmed` fires automatically.
14. Adjust an existing crop via handle/edge drag and release; verify `CaptureConfirmed` fires automatically.
15. Click inside an existing crop without dragging and release; verify `CaptureConfirmed` does NOT fire (no-gesture commit).
16. Start a drag that is too small and release; verify `CaptureConfirmed` does NOT fire and the overlay remains active.
17. Press Escape during a drag (before pointer release) and verify the overlay cancels; release the pointer and verify `CaptureConfirmed` does NOT fire.
18. Verify the `isClosingRequested` guard prevents double-confirm between release-to-capture and Escape cancel.

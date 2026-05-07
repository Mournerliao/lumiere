# Deferred Work

Updated 2026-05-07.

This file tracks work that is intentionally deferred after implementation or review. Keep only items that still need future attention here; resolved review history belongs in the story or review artifacts.

## MVP Blockers or Active Defects

None currently known.

## Active Technical Debt

These items do not currently block MVP validation, but should remain visible for cleanup, hardening, or future story planning.

### Overlay and UI Dispatch Hardening

- `overlayWindow` is accessed without explicit synchronization across callback/UI paths. `TryEnqueueUi` reads it while `EnsureOverlayWindow` and `CloseOverlayWindow` write it on the UI thread. Current behavior follows the existing app pattern, but future hardening should make ownership/threading explicit.
- Capture disposal evidence is currently asserted in tests but has no production consumer. It remains useful groundwork for future diagnostics, but is not yet surfaced.

### Capture Target UX and Future Window Path

- `GetMonitorDisplayName` returns raw `DeviceName` values such as `\\.\DISPLAY1` instead of a user-friendly monitor name. This is acceptable for MVP direct monitor capture, but should be improved as UX polish.
- `GetMonitorFromWindow` is public but unused in the current changeset. Keep it for future window-handle fallback work unless that path is removed.

### Release-to-Copy Cleanup

- `ReleaseToCaptureTests.cs` and `CropControllerTests.cs` include overlapping coverage. This is test-maintenance debt, not a behavior defect.
- `CropCommitResult.InvalidGeometry` can create a replacement `CropSelection` with the same region when adjustment geometry is invalid. This changes object identity but not the region; low risk unless downstream reference equality checks are introduced.
- Clipboard output currently provides a basic usable SDR bitmap without claiming HDR-preserving semantics. Full HDR-to-SDR tone mapping remains future output semantics work, expected under Story 4.2 rather than this MVP release-to-copy path.

## Recently Closed

- 2026-05-07: Added debug diagnostics when `TryEnqueueUi` falls back from the overlay dispatcher or drops UI work, and when swap chain disposal evidence is recorded after normal or failed UI detach cleanup.
- 2026-05-07: Cleared stale or resolved review items from stories 2.1, 2.3, 2.5, 3.5, and 3.6. Notable fixes include concurrency-idempotent `CaptureSessionResources.Dispose()`, removal of the `SwapChainResources` test-only `null!` handoff, removal of unreachable `MonitorFromPoint(... MONITOR_DEFAULTTONEAREST)` null handling, typed display capture targets, and moving D3D11 clipboard/crop work from `Lumiere.Infrastructure` into `Lumiere.Graphics`.

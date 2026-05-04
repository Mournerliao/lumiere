## Deferred from: code review of 2-1-start-capture-and-select-a-display-or-window-target (2026-05-04)

### Handled on 2026-05-04

- D1-HIGH: Removed the `null!` handoff from `CaptureTarget.CreateForTest`. Test targets now explicitly report `HasCaptureItem == false`, and production capture startup rejects them with a clear readiness failure instead of reaching WGC with a hidden null item.
- D2-HIGH: Guarded capture selection/preview startup after window close and cleared capture/graphics service references when device resources are disposed.
- D3-MEDIUM: Made capture support probing injectable for tests and kept `NotSupportedException` mapped to an unsupported readiness result.
- D5-MEDIUM: Changed the idle target-selection UI label from "Initializing preview" to "Ready to capture" while preserving the existing readiness state model.
- D7-MEDIUM: Added an upper-bound validation for capture target dimensions using the D3D11 2D texture limit of 16,384 pixels per dimension.
- D8-LOW: Read `previewGeneration` through `Volatile.Read` in async/UI dispatcher callbacks and use `Interlocked.Increment` for generation bumps.

### Closed by design on 2026-05-04

- D4-MEDIUM: Closed as by design. `CaptureTarget` remains a typed target descriptor and does not implement `IDisposable` because `GraphicsCaptureItem` has no documented disposal contract in the API surface this project targets. WGC teardown stays with session/resource owners such as `CaptureSessionResources`.

### Future story candidate

- D6-MEDIUM: Add typed capture target creation for display/window paths. Picker-created production targets should continue to use `CaptureTargetKind.Unknown` because `GraphicsCapturePicker` returns only a `GraphicsCaptureItem`, not whether the user chose a display or a window. A future story should introduce explicit creation paths such as `TryCreateFromDisplayId(...) => Display` and `TryCreateFromWindowId(...) => Window`, likely behind a narrow infrastructure factory.

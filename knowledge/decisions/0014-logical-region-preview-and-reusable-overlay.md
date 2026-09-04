# 0014: Logical-Size Region Preview And Reusable Overlay

Date: 2026-09-04

Tracking: [GitHub Issue #15](https://github.com/sousouliao/lumiere/issues/15)

## Decision

Region capture keeps the native frozen full-resolution frame as artifact truth, but its
interactive preview is a separate short-lived product:

1. each Host derives a lossless sRGB PNG whose pixel dimensions are the target display's
   rounded logical dimensions;
2. macOS performs Visual Match render and Lanczos downscale through one reused `CIContext`;
3. Windows samples the R16G16B16A16_FLOAT readback into the logical output dimensions in
   linear light, then performs SDR-white normalization, tone mapping, and sRGB/BGRA8
   conversion in the same output loop;
4. Region commit still crops and converts the retained full-resolution native frame;
5. Electron prewarms one sandboxed hidden Overlay renderer after the main window's first
   presentation, then reuses it across capture sessions;
6. Overlay activation and every renderer response carry a monotonically increasing
   generation. Main ignores responses from any older generation.

Protocol v3 is unchanged. `preview.pixelSize` already describes the preview and now reports
its actual logical-size pixel dimensions. The temporary-file capability URL remains the
only preview bridge exposed to the renderer.

## Context

The frozen-session decision in ADR 0013 fixed temporal correctness, but its first
implementation treated the Overlay preview like a delivery artifact. On a 5120×2880 frame,
the Host converted and PNG-encoded the complete backing image, Electron created and loaded a
new `BrowserWindow`, and Chromium decoded the full PNG before showing the Overlay. A measured
warm capture reached the Overlay at about 699 ms; roughly 306 ms was full-frame preview
conversion and encoding, while preview file I/O was about 12 ms.

The source investigation in
[`../research/region-preview-pipeline.md`](../research/region-preview-pipeline.md) found that
Flameshot, ShareX, and ksnip freeze a full in-memory frame but paint that frame directly for
selection. Their behavior supports freezing before selection; it does not support making a
full-resolution PNG round trip a prerequisite for interaction.

## Consequences

- A 2× display reduces preview conversion, encoding, transfer, decode, and texture-upload
  pixel count by about 75%, while final Display and Region artifacts retain backing-pixel
  resolution.
- Preview and artifact remain derived from one frozen moment, but they intentionally have
  different resolution and output responsibilities.
- The Overlay's window/document startup cost moves out of the capture hot path. Reset hides
  the window and clears its image and interaction state without destroying the renderer.
- Renderer crash, load failure, display topology change, lease expiry, cancellation, and app
  teardown still converge on one active-session cleanup path. A later renderer is rebuilt on
  demand.
- Windows closing the main window explicitly destroys the hidden Overlay so the existing quit
  behavior is preserved. macOS retains the blank Overlay for menu-bar and shortcut captures.
- Local structured timings use matching capture, render, encode, write, decode-ready, and show
  stages with integer cumulative and adjacent milliseconds. No telemetry is introduced.

## Rejected Alternatives

- Full-resolution preview PNG: measured conversion/encoding cost scales with backing pixels
  and provides detail the logical-size Overlay cannot display.
- JPEG or WebP: lossy text and edge artifacts are unnecessary when logical-size PNG meets the
  latency budget.
- Renderer Canvas or Electron `NativeImage`: these create a second color/capture pipeline in
  the shared shell and weaken native ownership.
- IOSurface, DXGI shared texture, or a new binary side channel: they add handle security,
  synchronization, device-loss, framing, and lifecycle contracts. Existing file read/write
  cost is too small to justify those boundaries.
- Native Overlay: it removes the bridge but duplicates the shared selection UI and interaction
  behavior on both platforms.

# 0013: Native Frozen-Frame Sessions For Region Capture

Date: 2026-08-31

## Decision

Interactive Region capture will use a short-lived native frozen-frame session:

1. the Shell hides Lumiere-owned capture surfaces;
2. the platform Host resolves the target and captures one complete native frame;
3. the Host retains that immutable frame and derives an encoded sRGB preview;
4. only after preparation succeeds does the Shell show the shared Region Overlay;
5. selection commit crops the retained frame and runs the existing platform-owned
   sRGB Visual Match and delivery path; it never captures a second frame;
6. commit, cancellation, timeout, Host teardown, and application teardown converge on
   one idempotent release path.

The platform-host seam will move to protocol v3. `getCapabilities` becomes a pure
query. Region capture uses explicit `prepareRegion`, `commitRegion`, and
`cancelRegion` operations; Display capture remains a one-request operation. The Host
session id and preview file path remain private to Electron main. A sandboxed renderer
receives only target-local logical size and an opaque, revocable preview capability URL
served by a restricted custom protocol.

The authoritative session resource remains in each native Host: a frozen `CGImage` on
macOS and an application-owned D3D11 texture copied from the first complete WGC frame
on Windows. The preview is not artifact truth. Commit crops the native frozen frame
before the existing Visual Match conversion so this change does not silently alter the
current crop-before-convert output semantics.

## Context

Protocol v2 resolved a target during capability polling, displayed a transparent
Overlay over the live desktop, and dispatched native capture only after pointer release.
Animations, video, and clocks therefore continued moving during selection, and the
artifact represented release time rather than command time.

Displaying an Electron-captured background while asking the Host to capture again
would only imitate freezing: preview and artifact could still represent different
moments. Sending raw HDR pixels or native handles through Electron would also violate
the platform ownership and HDR invariants.

Platform guidance supports a real frozen session. `SCScreenshotManager` supplies a
one-shot complete image on macOS. Windows.Graphics.Capture supplies pooled frames, so
the first complete frame must be copied into an application-owned texture before the
pool frame is released. The detailed source record is
[`../research/frozen-region-capture.md`](../research/frozen-region-capture.md).

## Consequences

- Capture-before-overlay and same-temporal-frame output become protocol invariants,
  not renderer timing conventions.
- The shared Overlay remains responsible only for target-local selection interaction.
- Each Host owns one active frozen Region session and deterministic native-resource
  disposal.
- Electron main owns the preview capability registry and validates the preview path;
  renderer IPC carries neither file paths nor image bytes.
- Overlay startup includes native acquisition plus preview encode/decode latency. That
  latency must be measured independently on macOS and Windows.
- A short lease prevents abandoned Overlays from retaining a full-resolution native
  frame indefinitely.
- Protocol v1 and v2 remain compatibility history. The bundled Shell and both bundled
  Hosts switch to v3 together; dual Region implementations are not retained.
- ADR 0009 remains authoritative for target-local geometry and per-delivery results,
  but its “capture immediately after release” Region timing is superseded by this
  decision.

## Rejected Alternatives

- Keep the transparent Overlay and capture after release: this preserves the reported
  defect.
- Show a frozen preview but recapture on commit: preview and artifact are not the same
  temporal frame.
- Use Electron `desktopCapturer`, Canvas, or `NativeImage`: this creates a second
  capture/color pipeline outside the native Hosts.
- Send base64 preview pixels through JSON Lines or Electron IPC: this turns a strict
  control protocol into an inefficient binary transport.
- Convert and retain a full-resolution canonical sRGB master during prepare: this can
  provide pixel-identical preview/output, but it changes the existing conversion order
  and full-frame memory cost without evidence that temporal same-frame semantics are
  insufficient.

# macOS Host

This directory owns the Swift ScreenCaptureKit adapter at the platform-host seam. It
acquires HDR-aware frames, converts the official output to sRGB Visual Match, and
performs native file delivery for the first macOS vertical slice.

The first implementation targets macOS 15 or newer. Swift Package Manager builds for
the active architecture; distribution will record and verify the final universal
architecture policy separately. Apple Silicon uses ScreenCaptureKit's local-display
HDR screenshot preset when the target display exposes potential EDR headroom above
1.0. The target is the display under the pointer when the request reaches the host;
main and system-primary displays are recovery fallbacks only. Intel capture remains
SDR and does not claim HDR acquisition.

Build and test the host:

```sh
swift build --package-path hosts/macos
swift test --package-path hosts/macos
```

The executable reads platform-host v1 JSON Lines requests from standard input, writes
protocol responses to standard output, and reserves standard error for structured
diagnostics correlated by request ID. The current slice supports display capture with
folder delivery; region selection and clipboard delivery remain later product-surface
work.

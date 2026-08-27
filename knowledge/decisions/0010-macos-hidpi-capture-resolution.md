# 0010: macOS HiDPI Capture Uses ScreenCaptureKit Pixel Scale

Date: 2026-08-27

## Decision

Lumiere's macOS Host will capture display and region screenshots at the target
ScreenCaptureKit filter's backing-pixel density. The Host derives output dimensions
from `SCContentFilter.contentRect` and `pointPixelScale`, while region source rectangles
remain in target-local logical points.

The Host will not infer screenshot density from physical panel resolution,
`NSScreen.main`, or `CGDisplayPixelsWide` and `CGDisplayPixelsHigh`. It will not cap a
scaled 4K display to the panel's physical 3840×2160 dimensions. If ScreenCaptureKit
returns an image whose dimensions differ from the requested backing-pixel dimensions,
the capture fails instead of silently delivering a lower-resolution artifact.

## Context

`SCDisplay.width` and `height` are measured in points, while
`SCStreamConfiguration.width` and `height` are measured in output pixels. The original
display path assigned the former directly to the latter. The region path independently
divided Core Graphics display dimensions by AppKit logical dimensions. On a scaled 4K
display these paths produced one output pixel per logical point, including a reported
1408×821 region whose text and fine lines were visibly soft.

ScreenCaptureKit owns the relevant capture-specific conversion through
`SCContentFilter.pointPixelScale`. This scale may describe a backing resolution larger
than the panel's physical resolution under a macOS scaled mode, so panel pixels divided
by logical points is not an equivalent substitute. The HDR screenshot preset configures
dynamic range, pixel format, and color space, but does not select the target's HiDPI
output dimensions.

## Consequences

- Display and region capture share one target-specific point-to-pixel conversion.
- Mixed-scale and rotated displays use the selected filter's current logical content
  size and scalar pixel scale without main-display assumptions or manual axis swaps.
- The platform-host protocol, overlay geometry, sRGB Visual Match conversion, and
  clipboard/folder delivery semantics do not change.
- A 2× capture contains four times as many pixels as a 1× capture. Higher memory and
  conversion cost are accepted as the cost of a sharp native-density screenshot; the
  Host does not silently trade correctness for a smaller artifact.
- This decision defines the macOS implementation policy. It does not claim universal
  DPI or display-topology certification across every supported platform and device.

# 0010: macOS HiDPI Capture Uses ScreenCaptureKit Pixel Scale

Date: 2026-08-27

## Decision

Lumiere's macOS Host will capture display and region screenshots at the target
ScreenCaptureKit filter's backing-pixel density. The Host derives output dimensions
from `SCContentFilter.contentRect` and `pointPixelScale`. For Region capture, the Host
converts all four target-local logical edges into pixel space, rounds the minimum edges
down and maximum edges up, captures the complete display at backing-pixel resolution,
and crops that full frame with the resulting integer pixel rectangle. Region capture
does not use `SCStreamConfiguration.sourceRect`.

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
[`SCContentFilter.pointPixelScale`](https://developer.apple.com/documentation/screencapturekit/sccontentfilter/pointpixelscale).
This scale may describe a backing resolution larger
than the panel's physical resolution under a macOS scaled mode, so panel pixels divided
by logical points is not an equivalent substitute. The HDR screenshot preset configures
dynamic range, pixel format, and color space, but does not select the target's HiDPI
output dimensions.

Hardware A/B testing on the reported scaled 4K display found a second, independent
failure. A full-frame `SCScreenshotManager` result matched the dimensions and edge
acuity of a macOS system screenshot, while a pixel-aligned `sourceRect` request was
visibly softer across the same display. Cropping the full backing frame locally avoided
the observed `sourceRect` softness and restored comparable text and edge acuity on that
display. ScreenCaptureKit does not document its reconstruction kernel, so the decision
relies on this named hardware observation rather than a claim about the framework's
internal algorithm.

## Consequences

- Display and region capture share one target-specific point-to-pixel conversion.
- Fractional Region geometry is aligned inside the native Host. The aligned capture may
  expand the selected area by less than one backing pixel per affected edge so it never
  drops selected content or introduces an avoidable fractional sampling phase.
- Region capture pays the peak-memory cost of a full backing frame before producing the
  smaller cropped artifact. This is accepted because direct `sourceRect` capture failed
  the product's visible-sharpness requirement on the verified scaled 4K display.
- Mixed-scale and rotated displays use the selected filter's current logical content
  size and scalar pixel scale without main-display assumptions or manual axis swaps.
- The platform-host protocol, overlay geometry, sRGB Visual Match conversion, and
  clipboard/folder delivery semantics do not change.
- A 2× capture contains four times as many pixels as a 1× capture. Higher memory and
  conversion cost are accepted as the cost of backing-pixel-density output; Region now
  incurs that cost for the complete display during acquisition. Correct dimensions
  remain separate from visual-match evidence and do not alone prove sharpness.
- `destinationRect`, `scalesToFit`, `captureResolution`, and `shouldBeOpaque` are not
  used to compensate for inconsistent Display or Region geometry.
- This decision defines the macOS implementation policy. It does not claim universal
  DPI or display-topology certification across every supported platform and device.

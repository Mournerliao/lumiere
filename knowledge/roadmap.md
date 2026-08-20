# Product Roadmap

Status belongs in `state/CURRENT.md`; this roadmap defines sequence and scope only.

## Cross-Platform HDR-Aware MVP

Deliver Windows and macOS native region/display capture behind a shared Electron
shell, main/tray-or-menu-bar/shortcut entry points, target-aware HDR readiness, and
one official compatible output path: sRGB Visual Match for clipboard and folder delivery.

Release requires independent Windows and macOS runtime evidence, fixed-scene
visual-match evidence, honest copy, and installable artifacts validated on
non-development machines.

## One HDR-Preserved Export Path

Choose one format and named viewers before implementation or public claims. Define
source/destination color semantics, conversion, metadata, and viewer assumptions,
then gather target-aware hardware evidence on every claimed platform. A platform may
ship a narrower named path before one format is proven portable.

## Cross-Platform HDR Fidelity

Measure fixed scenes across a named Windows/macOS display and viewer matrix. Expand
viewers, mixed HDR/SDR topologies, DPI/Retina/accessibility hardening, longer stability
runs, and fidelity tolerances only after the preserved path has evidence.

## Non-Goals

Lumiere is not a general image editor, HDR mastering tool, cloud sharing product,
or promise of display-independent identity across all monitors and viewers.

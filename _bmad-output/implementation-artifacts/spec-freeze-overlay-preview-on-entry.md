---
title: 'Freeze Overlay Preview On Entry'
type: 'feature'
created: '2026-06-22'
status: 'done'
route: 'one-shot'
---

# Freeze Overlay Preview On Entry

## Intent

**Problem:** Region capture currently keeps the overlay preview live, which makes precise screenshot timing harder because users are dragging against a moving target.

**Approach:** Present the first GPU frame to the overlay, immediately stop further WGC frame delivery for region capture, keep the rendered frame and output snapshot alive for cropping/output, and reject late readiness/diagnostic callbacks after the freeze point.

## Suggested Review Order

1. [Freeze controller seam](../../src/Lumiere.App.Core/OverlayPreviewFreezeController.cs) -- small interface that decides when region capture freezes and when later callbacks must be ignored.
2. [Freeze controller tests](../../tests/Lumiere.Graphics.Tests/App/OverlayPreviewFreezeControllerTests.cs) -- region vs fullscreen behavior and late-callback rejection contract.
3. [Main window orchestration](../../src/Lumiere.App/MainWindow.xaml.cs) -- first-frame freeze, late-callback gating, and paused-frame overlay copy.
4. [Overlay validation doc](../../harness/validation/overlay-validation.md) -- Windows manual checks for paused-frame behavior.
5. [Design alignment](../../harness/design/perfect-hdr-fidelity-extension.md) -- product-level rationale for frozen-frame selection.

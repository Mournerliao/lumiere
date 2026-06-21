---
title: 'HDR10 JXR Viewer Validation Evidence'
type: 'feature'
created: '2026-06-22'
status: 'done'
route: 'one-shot'
---

# HDR10 JXR Viewer Validation Evidence

## Intent

**Problem:** Lumiere records JXR audit metadata and loads manual validation artifacts, but there was no pure contract that answers whether those artifacts satisfy the viewer-recognized HDR10 metadata and manual viewer validation side of the JXR release gate.

**Approach:** Add a hardware-independent evaluator for HDR10 JXR viewer validation evidence. It requires target-aware HDR evidence, a complete HDR10 format contract, all named viewers to pass artifact handling, visual match, HDR preservation, and HDR10 metadata recognition, and keeps runtime capability enabling separate.

## Suggested Review Order

1. [Viewer evidence evaluator](../../src/Lumiere.Graphics/Output/Hdr10JxrViewerValidationEvidence.cs) -- gate inputs, blockers, and no runtime capability mutation.
2. [Evaluator tests](../../tests/Lumiere.Graphics.Tests/Output/Hdr10JxrViewerValidationEvidenceTests.cs) -- empty, automated-only, incomplete viewer metadata, and complete manual evidence paths.
3. [Validation docs](../../docs/validation/output-validation.md) -- explains how evaluator relates to artifacts and `Hdr10JxrCodecReadiness`.

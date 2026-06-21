---
title: 'Surface Output Validation Load Issues'
type: 'feature'
created: '2026-06-22'
status: 'done'
route: 'one-shot'
---

# Surface Output Validation Load Issues

## Intent

**Problem:** Output validation artifacts could fail to load, but testers only had structured logs to discover ignored JSON/schema files, making Windows manual validation evidence hard to repair.

**Approach:** Project the full output validation artifact snapshot into the settings validation record, keep runtime output policy limited to valid artifacts, and show load issue counts plus the first ignored file in the validation panel.

## Suggested Review Order

1. [Validation record projection](../../src/Lumiere.App.Core/PerfectHdrFidelityProjection.cs) -- snapshot-aware manual evidence detail and load issue handling.
2. [Settings projection wiring](../../src/Lumiere.App.Core/SettingsPanelProjection.cs) -- snapshot overload keeps valid artifact flow while exposing load issues.
3. [Main window wiring](../../src/Lumiere.App/MainWindow.xaml.cs) -- settings panel receives the snapshot; output/tray/main paths still consume valid artifacts only.
4. [Projection tests](../../tests/Lumiere.Graphics.Tests/App/OutputValidationArtifactSourceTests.cs) -- load issue and clean artifact snapshots remain limited evidence, not release pass.
5. [Validation docs](../../docs/validation/output-validation.md) -- tester-facing behavior for ignored artifact files.

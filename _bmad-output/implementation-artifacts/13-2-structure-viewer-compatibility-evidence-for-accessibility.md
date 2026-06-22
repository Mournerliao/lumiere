---
title: 'Structure Viewer Compatibility Evidence For Accessibility'
type: 'feature'
created: '2026-06-23'
status: 'in-progress'
route: 'native-validation-surface'
story: '13-2'
---

# Structure Viewer Compatibility Evidence For Accessibility

## Intent

The Public perfect-HDR-fidelity settings surface already carried viewer compatibility evidence for Microsoft Paint, Windows Photos, and Microsoft Edge, but each viewer row still collapsed four distinct evidence categories into one dense paragraph plus one aggregate status. That was technically honest, yet it made target-app review harder under keyboard, screen-reader, high-contrast, and long-text pressure.

This slice keeps the same evidence model but presents it in a more reviewable native structure: each viewer row now exposes an explicit category-by-category breakdown alongside the existing guidance detail, so target-app compatibility work is easier to audit without depending on color or one compressed sentence.

## Delivered In This Slice

1. `ValidationViewerMatrixRowProjection` now separates:
   - `StatusBreakdown`
   - guidance `Detail`
   - combined `AutomationSummary`
2. `ProjectViewerEvidence(...)` now centralizes that split so the evidence model stays in the projection seam instead of leaking formatting logic into the WinUI layer.
3. Settings > Validation viewer rows now render:
   - viewer name
   - explicit status breakdown text for artifact handling / visual match / HDR preservation / HDR10 metadata
   - follow-up guidance detail
   - aggregate row status
4. Viewer-row automation help text now reads the structured breakdown and guidance together instead of only the trailing narrative detail.
5. The design extension now explicitly states that viewer evidence should remain category-readable rather than collapsing into color-only or paragraph-only presentation.

## Review Pointers

1. [PerfectHdrFidelityProjection.cs](../../src/Lumiere.App.Core/PerfectHdrFidelityProjection.cs)
2. [MainWindow.xaml](../../src/Lumiere.App/MainWindow.xaml)
3. [MainWindow.xaml.cs](../../src/Lumiere.App/MainWindow.xaml.cs)
4. [perfect-hdr-fidelity-extension.md](../../harness/design/perfect-hdr-fidelity-extension.md)

## Validation

- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --filter "PerfectHdrFidelityProjectionTests|SettingsPanelProjectionTests" --no-restore --verbosity minimal /nr:false`
- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 --verbosity minimal`

## Remaining Work

Story `13-2` remains `in-progress`.

Remaining release-blocking work:

- Run Windows manual accessibility checks for keyboard, Narrator/screen reader, high contrast, text scaling, and DPI.
- Run Windows manual checks that the validation surface remains readable with real long evidence text and mixed pass/limited/fail states.
- Record explicit manual accessibility evidence rather than relying only on code structure and automated projection tests.

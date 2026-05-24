---
title: 'Restore Export Format Segmented Control'
type: 'feature'
created: '2026-05-25'
status: 'done'
baseline_commit: '1d7a828956400444053b3358b51356b1fc6b5194'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md'
  - '{project-root}/harness/design/v0-mvp-reference/components/lumiere/settings-panel.tsx'
---

<frozen-after-approval reason="human-owned intent - do not modify unless human renegotiates">

## Intent

**Problem:** The user intended the settings panel to match the v0 design reference by showing an `Export` segmented control with `HDR10`, `P3`, and `sRGB`, but the current implementation replaced that area with a single `Color output: Not available` pill. This hides an important product signal and makes the implementation diverge from the chosen design direction.

**Approach:** Restore the native WinUI settings row as `Export` with the three visible design-reference options while keeping the fidelity semantics honest. The UI may show the options as configured/pending state, but it must not claim HDR10, P3, or validated HDR-preserving output until real encoder metadata, conversion policy, target-app compatibility, and Windows validation exist.

## Boundaries & Constraints

**Always:** Keep the feature under Epic 6 / Story 6.5 as a scope correction, not a new epic. Preserve the HDR-first capture/preview invariant and do not change the capture pipeline. Keep settings state projected through `SettingsPanelProjection`; do not make XAML visual state a parallel source of truth. Keep visible copy clear that advanced export profiles are pending validation.

**Ask First:** Halt before implementing real HDR10 encoding, P3 ICC/tagging behavior, persistent user selection, or changing the current PNG/clipboard conversion policy beyond UI projection. Halt before removing the existing validation discipline that prevents unverified HDR-preserving claims.

**Never:** Do not claim `HDR-preserving`, `HDR10 output`, `P3 export`, or `validated output` as implemented. Do not introduce SDR fallback in the authoritative preview path. Do not add Electron/web dependencies or reuse React design code directly.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Design restoration | Settings panel opens with default output settings | HDR section shows an `Export` row with visible `HDR10`, `P3`, and `sRGB` segments; the row visually resembles the design reference within native WinUI styling | N/A |
| Honest semantics | Export profiles are visible before encoder/metadata validation exists | UI automation/help text explains advanced export profiles need encoder metadata, conversion policy, target-app assumptions, and Windows validation; copy does not imply HDR-preserving output | N/A |
| Current basic output | Current clipboard/folder output remains PNG/basic usability | Existing output target and copy-as-image behavior remain active; export profile display does not change output execution | N/A |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/epics.md` -- Story 6.5 acceptance criteria currently allow hidden/disabled/unavailable export options; needs a scope-correction note that visible segmented controls are acceptable when explicitly scoped.
- `_bmad-output/implementation-artifacts/6-5-scope-export-and-color-format-options-honestly.md` -- completed story record currently says no HDR10/P3/sRGB selection; needs an amendment/change log entry for the corrected design intent.
- `harness/design/v0-mvp-reference/components/lumiere/settings-panel.tsx` -- authoritative design reference for `Export` row and `HDR10`/`P3`/`sRGB` labels.
- `src/Lumiere.App.Core/SettingsPanelProjection.cs` -- pure settings projection; should expose export segment labels, selected/current display state, read-only/pending semantics, and help text.
- `src/Lumiere.App/MainWindow.xaml` -- native settings UI; should replace `Color output: Not available` pill with an `Export` segmented row.
- `src/Lumiere.App/MainWindow.xaml.cs` -- applies projection to settings UI and accessibility metadata.
- `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs` -- hardware-independent projection tests for visible labels and honest copy.
- `tests/Lumiere.Graphics.Tests/Output/OutputValidationDocumentationTests.cs` -- guardrail tests for future format validation documentation.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/planning-artifacts/epics.md` -- amend Story 6.5/UX-DR11 language to allow visible design-reference export segments when disabled or validation-scoped -- keeps planning source aligned with user intent.
- [x] `_bmad-output/implementation-artifacts/6-5-scope-export-and-color-format-options-honestly.md` -- append a scope-correction note rather than rewriting history -- preserves prior validation while documenting the product change.
- [x] `src/Lumiere.App.Core/SettingsPanelProjection.cs` -- add export segment projection data for `HDR10`, `P3`, and `sRGB`, with selected/current state and validation-scoped help text -- avoids hard-coded UI logic.
- [x] `src/Lumiere.App/MainWindow.xaml` and `src/Lumiere.App/MainWindow.xaml.cs` -- restore the `Export` segmented control using native WinUI elements and projection-driven state -- matches the design intent without importing web code.
- [x] `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs` -- update/add tests proving all three labels are projected, selected state is deterministic, and help text avoids HDR-preserving claims -- covers the changed UI contract.
- [x] `docs/validation/output-validation.md` and related tests if needed -- ensure validation docs still describe evidence required before any advanced profile becomes real output behavior -- preserves fidelity discipline.

**Acceptance Criteria:**
- Given the settings projection is created, when export profile data is read, then `HDR10`, `P3`, and `sRGB` are present in design order and advanced profiles are marked read-only or pending validation.
- Given the settings panel is shown, when the HDR section is inspected, then the row label is `Export` and the visible controls are `HDR10`, `P3`, and `sRGB`, not a single `Not available` pill.
- Given a user reads export/profile help text, when HDR10/P3 semantics are still unimplemented, then the text states the validation/metadata limits and avoids validated HDR-preserving claims.
- Given clipboard/folder output runs after this change, when output policy is evaluated, then existing output target and copy-as-image behavior are unchanged.

### Review Findings

- [x] [Review][Decision] NFR9/UX-DR11 wording weakened — Accepted: new wording accurately reflects product intent. [epics.md:94,150]
- [x] [Review][Decision] sRGB hardcoded as IsSelected: true — Accepted: matches Design Notes intent ("prefer sRGB"). [SettingsPanelProjection.cs:157]
- [x] [Review][Patch] sRGB help text contains forbidden "HDR-preserving" phrase — Fixed: changed to "advanced fidelity validation is pending". [SettingsPanelProjection.cs:161]
- [x] [Review][Patch] Test doesn't guard ExportColorOptions[2].HelpText for forbidden phrase — Fixed: added DoesNotContain assertions for ExportColorOptions[2].HelpText. [SettingsPanelProjectionTests.cs]
- [x] [Review][Patch] Hardcoded index mapping & no Click handlers — Fixed: added IsEnabled=!option.IsReadOnly and else branch for Count < 3. [MainWindow.xaml.cs:1093-1107]
- [x] [Review][Patch] Missing null guards on ExportColorOptions and projection fields — Fixed: added null checks in ExportColorOptionProjection constructor and null-conditional in ApplyExportColorProjection. [SettingsPanelProjection.cs:194-198, MainWindow.xaml.cs:1091]
- [x] [Review][Patch] Old XAML elements not removed — Already removed in diff. [MainWindow.xaml]
- [x] [Review][Patch] XAML hardcodes helper text — Fixed: changed default Text to empty string. [MainWindow.xaml:391-392]
- [x] [Review][Patch] ApplyExportColorProjection silently skips when Count < 3 — Fixed: added else branch to disable all segments. [MainWindow.xaml.cs:1093]
- [x] [Review][Defer] CreateExportColorOptions allocates new list on every call — Could be static readonly field. [SettingsPanelProjection.cs:141-166] — deferred, pre-existing
- [x] [Review][Defer] "validation-scoped" jargon in accessibility text — Screen reader users won't understand. [MainWindow.xaml:402] — deferred, pre-existing
- [x] [Review][Defer] ExportColorDisplayValue hardcoded to "sRGB" — Panel-level automation name always says "Export profile: sRGB". [SettingsPanelProjection.cs:139] — deferred, pre-existing
- [x] [Review][Defer] No test validates sRGB is default/active segment — Design Notes say "prefer sRGB" but no test explicitly asserts Single(o => o.IsSelected).Label == "sRGB". [SettingsPanelProjectionTests.cs] — deferred, pre-existing

## Spec Change Log

## Design Notes

This is a product-scope correction to Story 6.5, not a reversal of HDR claim discipline. The right compromise is visible UI matching the reference, with interaction and wording that communicate "profile choices are on the product surface, but advanced fidelity is not validated yet." If a single active/default segment is needed before persistence exists, prefer `sRGB` because it best describes the current basic PNG output path; `HDR10` and `P3` should remain unavailable/pending until future stories implement them.

## Verification

**Commands:**
- `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false` -- expected: restore succeeds.
- `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false` -- expected: build succeeds with no new warnings.
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false` -- expected: projection and validation tests pass.
- `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal` -- expected: no formatting changes required.

**Manual checks (if UI runtime is available):**
- Open settings and confirm the HDR section shows `Export` with `HDR10`, `P3`, and `sRGB` segments; no visible copy claims validated HDR-preserving output.

# Deferred Work

Updated: 2026-06-14 (cleanup pass)

This file tracks work intentionally deferred after implementation, review, or retrospective. It is not a graveyard: every unresolved item should either have a target epic/story hint, an accepted-tech-debt label, or a clear reason it remains parked.

Resolved review history belongs in story files or review artifacts. Historical mojibake-heavy review text should not be copied forward unless the meaning is recoverable.

## MVP Blockers or Active Defects

None currently known.

## Active Technical Debt

None currently unresolved.

## Future Story Candidates

### BMad workflow: non-code story review exit criteria

- Source: Epic 4 retrospective.
- Current shape: non-code/documentation stories can reach review without a crisp checklist that proves intended artifacts, links, and follow-through were completed.
- Risk: workflow-only work may appear done while losing review findings, status updates, or traceability.
- Target: before the next documentation/planning-heavy story.

### InvalidCrop integration tests

- Source: Story 4.6 review and Epic 4 retrospective.
- Missing coverage:
  - InvalidCrop save/apply/timer/restore round trip in `OverlayWindow`.
  - Escape or close during active InvalidCrop feedback.
  - Rapid successive invalid crop gestures.
  - Confirm click while InvalidCrop feedback is active.
- Target: next overlay-related story or focused test-hardening task.
- Note: requires UI thread / HWND infrastructure; feasibility for automated testing is TBD.

### Accessibility enhancements

- Source: Epic 8-9 code reviews.
- Missing: AutomationProperties for overlay info panel, hardcoded Chinese text, animation effects, hover states.
- Target: dedicated accessibility story when product reaches beta stage.

### Fixed window height may clip InfoBar content

- Source: Story 8-2 review.
- `MainPanelHeightDips = 310` with Auto-sized InfoBar row.
- Target: UI polish story.

### Shortcut editing: full key binding editor

- Source: Epic 9 implementation.
- Current: basic ContentDialog for shortcut capture.
- Deferred: full key binding editor with conflict detection, validation, and recovery.
- Target: future settings enhancement story.

### Export color format: actual format conversion

- Source: Epic 9 implementation.
- Current: UI-only selection (sRGB only functional; HDR10/P3 are read-only placeholders).
- Deferred: actual format conversion, encoder metadata, HDR metadata policy, target-app compatibility, Windows validation.
- Target: future export pipeline story.

## Known Limitations — Requires Hardware Validation

### Hardware validation gaps from Epic 4

- Source: Story 4.5 and Epic 4 retrospective.
- Gaps:
  - Escape cancel with and without active crop was not fully validated in Story 4.5.
  - Multi-monitor behavior was not validated beyond a single-monitor environment.
  - DPI scales 100%, 125%, and 200% were not validated; 150% was tested.
  - SDR display behavior was not separately validated.
  - Clipboard lock recovery/failure injection was not tested.
- Status: documented as known limitations. Cannot be resolved without physical hardware.

### HDR probe hardcodes output index 0

- Source: Story 8-2 review.
- `HdrDisplayCapability.Probe(IDXGIFactory2)` always queries adapter 0, output 0.
- On multi-monitor setups with different HDR states per display, probe result may not match capture target.
- Status: documented limitation. XML doc comment added to `Probe` method.
- Correct fix requires passing the capture target's display adapter/output — future feature.

## Accepted Decisions / No Current Action

### Sprint-status timestamp formats remain mixed for now

- Source: Epic 4 retrospective follow-through.
- Decision: no current action; normalize only if a future tooling story needs machine-validated timestamps.

### `MainWindow` retains a direct `CaptureService` field for now

- Source: Story 4.4 review.
- Decision: accepted as-is. `ICaptureCommandCoordinator` wraps command reservation, while `MainWindow` still needs `CaptureService` for current session projection.
- Revisit trigger: if a future app-state coordinator removes the remaining projection dependency cleanly.

### Constructor-injected graphics resources fail through caller path

- Source: Story 4.5 review.
- Decision: accepted as-is. `GraphicsEngine` construction failure propagates through app startup; not a current active defect.

### `CaptureCommand` permits a null target

- Source: Story 4.2 review.
- Decision: accepted for current command shape because fullscreen/region commands may reserve target resolution for a later step.

### `CaptureCommandResult` is a class rather than a record

- Source: Story 4.2 review.
- Decision: accepted technical style debt. No current behavioral defect is known.

### Default switch rejects future `CaptureSessionStatus` values

- Source: Story 4.2 review.
- Decision: accepted defensive behavior for now.
- Revisit trigger: when adding a new session status, review command acceptance explicitly.

### ClassifyRejection completeness for new enum values

- Source: Story 7.6 review.
- Decision: accepted extensibility concern; current behavior is correct.

### Deferred loop blocks UI thread with many pending states

- Source: Story 7.6 review.
- Decision: accepted. Low risk in practice; at most 1-2 states accumulate.

### RequiresFailureTeardown behavior in deferred loop

- Source: Story 7.6 review.
- Decision: accepted. Correct but confusing; StopPreview calls during applyingSessionState will queue to pendingSessionState which the loop picks up.

### `TryReportFrameFailure` bare catch swallows callback exceptions

- Source: Story 8-3 review.
- Decision: accepted. Diagnostic logging executes before the callback so diagnostic is not lost.

### `DiagnosticRecord.Create` does not validate empty/whitespace strings

- Source: Story 8-4 review.
- Decision: accepted. Defensive encoding, current no actual risk.

### `SessionDiagnosticScope` 8-char hex ID collision risk

- Source: Story 8-4 review.
- Decision: accepted. Probability extremely low.

### `DiagnosticRecord.Exception` is mutable reference type

- Source: Story 8-4 review.
- Decision: accepted. `init`-only, low risk.

### `CaptureService` log format vs `MapFailureToReadiness` format duplication

- Source: Story 8-4 review.
- Decision: accepted. DRY violation but both methods serve different purposes.

### XamlCompiler fragility with `dotnet format`

- Source: Epic 9 retrospective.
- Decision: known WinUI issue. `dotnet format` removes "unused" `using` statements that the XamlCompiler-generated partial classes depend on. Workaround: verify build after running `dotnet format`.

### FolderPicker is Windows manual validation only

- Source: Epic 9 implementation.
- Decision: accepted. Cannot be validated in CI.

### `DiagnosticContext` factory methods boilerplate

- Source: Story 8-4 review.
- Decision: accepted style preference. 10 methods with identical structure; low-value refactor.

## Recently Closed

### Closed: 2026-06-14 (deferred-work cleanup pass)

- **Fixed: 2 failing `DefaultSettingsProviderTests`** — tests now use isolated `SettingsFileFixture` instead of real `%LOCALAPPDATA%` settings file. Root cause: `hdrAlertsEnabled: false` on disk vs test expectation of `true`.
- **Removed: dead conditional in `CaptureSessionState.FromStartResult`** — both branches called identical code; simplified to single return.
- **Fixed: `SwapChainManager` double-dispose of `swapChain3`** — removed redundant `swapChain3?.Dispose()` from catch block; finally block handles it.
- **Fixed: `CreateExportColorOptions` list allocation** — converted from method to `private static readonly` field `DefaultExportColorOptions`.
- **Removed: dead `Probe(IDXGIDevice)` overload** — zero callers, zero tests. Removed from `HdrDisplayCapability`.
- **Refactored: MainWindow constructor** — reduced from 13 parameters to 7. Created `ISettingsWriterAggregator` interface; `DefaultSettingsProvider` implements it. Removed unused `exportColorSettingsWriter` field.
- **Fixed: `SessionDiagnosticScope.Dispose()` thread safety** — changed `disposed` from `bool` to `int` with `Interlocked.Exchange` pattern.
- **Fixed: `InteropFailureDiagnostics.LogAndFormat` unbounded output** — truncated `exception.ToString()` to 2048 chars.
- **Extracted: shared `AlertMapping` class** — `MainPanelProjection.MapAlertMessage` and `TrayMenuProjection.MapTrayAlertMessage` now use shared `AlertMapping.Classify()` for guard logic and switch structure.
- **Fixed: tray alert color always Warning** — added `TrayAlertSeverity` to `TrayMenuProjection` and `TrayMenuSnapshot`; `TrayMenuWindow` now uses `ErrorBrush` for Failed severity.
- **Cached: HDR capability in `SwapChainManager`** — `HdrDisplayCapability.Probe(factory)` result cached in static field; COM objects no longer allocated on each `Configure` call.
- **Documented: HDR probe multi-monitor limitation** — XML doc comment added to `HdrDisplayCapability.Probe()`.
- **Renamed: `SwapChainManager.MapFailureToReadiness`** → `FormatFailureAsReadiness` to clarify it no longer logs.
- **Wired: `ExportColorDisplayValue` to actual selection** — now reads from `DefaultExportColorOptions` instead of hardcoded `"sRGB"`.
- **Added: 10 unit tests for writer interfaces** — `SettingsWriterTests.cs` covers all 7 writer methods + aggregator interface assertion.
- **Created: `ISettingsWriterAggregator` interface** — aggregates `ISettingsProvider` + 7 writer interfaces.

### Closed: Story 7.6 (2026-05-26)

- Resolved 4 technical debt items: capture command rejection unification, ApplySessionState reentrancy, capture action re-enable diagnostics, Disposed-to-idle consolidation.
- Epic 7 UI-thread protection resolved: all tray/hotkey commands dispatch through `DispatcherQueue`.

### Closed: Epic 5 guardrail

- Guardrail document created in `_bmad-output/implementation-artifacts/epic-5-implementation-guardrails.md`.

### Closed: Epic 7 UI-thread protection

- All tray and hotkey commands dispatch through `DispatcherQueue` before mutating app state.

### Closed: Settings write and persistence

- `ISettingsProvider` and `DefaultSettingsProvider` fully implemented with write support.
- `ISettingsWriterAggregator` created; all 7 writer interfaces consumed through single dependency.

### Closed: Output policy type ownership

- `CropPixelRect`, `OutputTarget`, and output request types live in `Lumiere.Graphics.Output`.
- `Lumiere.Settings` references `OutputTarget` — no circular dependencies.

### Closed: Export format items from spec review

- `CreateExportColorOptions` converted to static readonly field.
- `ExportColorDisplayValue` wired to actual selection from options.
- Writer interface tests added (10 new tests).

### Closed: Overlay info panel test failures

- `DefaultSettingsProviderTests.HdrAlertsEnabled_ReturnsTrue` and `AllProperties_ReturnConsistentValues` fixed.

### Closed: Story 8-2 review items

- Alert mapping DRY: shared `AlertMapping` class extracted.
- Tray alert color: severity-based color mapping added.
- HDR capability caching: static cache in `SwapChainManager`.
- Dead `Probe(IDXGIDevice)` overload: removed.
- Double-dispose: fixed.

### Closed: Story 8-3 review items

- `swapChain3` double-dispose: fixed.
- `InteropFailureDiagnostics` unbounded output: truncated to 2048 chars.

### Closed: Story 8-4 review items

- `SessionDiagnosticScope.Dispose()` thread safety: `Interlocked.Exchange` pattern.
- `MapFailureToReadiness` renamed to `FormatFailureAsReadiness` in `SwapChainManager`.

### Closed: Story 8-5 review items

- Pre-existing `DefaultSettingsProviderTests` failures: fixed.

### Closed: Epic 9 implementation items

- MainWindow 13 parameters: reduced to 7 with `ISettingsWriterAggregator`.
- No writer tests: 10 new tests added.

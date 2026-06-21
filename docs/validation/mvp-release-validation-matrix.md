# MVP Release Validation Matrix

Generated: 2026-06-03
Story: 8.5 Run MVP Release Validation Matrix

This document captures the MVP release validation snapshot created for Story 8.5. For ongoing private-preview or public-release decisions, use `docs/validation/release-validation-checklist.md` as the live release-gate checklist, then update this matrix when a new validation snapshot is recorded.

## Section 1: Automated Gates

| Gate | Command | Result | Date | Notes |
|------|---------|--------|------|-------|
| Restore | `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false` | PASS | 2026-06-03 | Completed in 1.2s |
| Build | `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false` | PASS | 2026-06-03 | All 9 projects built successfully in 18.7s |
| Graphics Tests | `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false` | PARTIAL | 2026-06-03 | 356/358 pass, 2 pre-existing failures (see note below) |
| Overlay Tests | `dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false` | PASS | 2026-06-03 | 88/88 pass |
| Format | `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal` | PASS | 2026-06-03 | No formatting violations |

**Graphics Tests Pre-existing Failures:**
The 2 failures are `DefaultSettingsProviderTests.HdrAlertsEnabled_ReturnsTrue` and `DefaultSettingsProviderTests.AllProperties_ReturnConsistentValues`. These are documented in `deferred-work.md` as pre-existing issues unrelated to any Epic 8 work. They relate to the `HdrAlertsEnabled` default value being `false` while the test expects `true`.

## Section 2: Windows Manual Validation

All manual validation scenarios require interactive desktop access with real Windows hardware, display configuration, and target applications. Scenarios are recorded as `not-run` pending manual validation by a human tester.

| # | Scenario | FR/NFR | Result | Notes | Device/Display |
|---|----------|--------|--------|-------|----------------|
| 1 | Direct monitor capture (no picker default path) | FR46 | not-run | Requires interactive desktop with WGC support | — |
| 2 | Trigger-to-active responsiveness — shortcut entry point (p50/p95) | NFR1 | not-run | Requires timing instrumentation on real hardware | — |
| 3 | Trigger-to-active responsiveness — tray entry point (p50/p95) | NFR1 | not-run | Requires tray interaction on real hardware | — |
| 4 | Trigger-to-active responsiveness — main window entry point (p50/p95) | NFR1 | not-run | Requires main window interaction on real hardware | — |
| 5 | Repeated lifecycle stability: start, cancel, restart, release-to-output, quit (10+ cycles) | NFR5, FR45 | not-run | Requires repeated interactive capture sessions and resource monitoring | — |
| 6 | Overlay crop interaction: drag to create, adjust handles, release-to-capture | FR47, NFR2, NFR3 | not-run | Requires interactive overlay with pointer input | — |
| 7 | Overlay placement: primary monitor, secondary monitor | FR47, NFR27 | not-run | Requires multi-monitor setup | — |
| 8 | Overlay preview stability on HDR display | NFR27 | not-run | Requires HDR-capable display | — |
| 9 | Overlay preview stability on SDR display | NFR27 | not-run | Requires SDR display | — |
| 10 | DPI scaling at 100% | NFR27 | not-run | Requires display configuration change | — |
| 11 | DPI scaling at 125% | NFR27 | not-run | Requires display configuration change | — |
| 12 | DPI scaling at 150% | NFR27 | not-run | Previously validated at 150% in Story 4.5/5.1 (partial) | — |
| 13 | DPI scaling at 200% | NFR27 | not-run | Requires display configuration change | — |
| 14 | Clipboard output: paste to Paint | FR48 | partial | Prior evidence: Story 4.5 confirmed basic clipboard write paste to Paint on HDR 4K at 150% DPI | — |
| 15 | Clipboard output: paste to Photos | FR48 | not-run | Requires Photos app and interactive clipboard operation | — |
| 16 | Clipboard output: paste to Chromium-based browser | FR48 | not-run | Requires Chromium-based browser | — |
| 17 | Clipboard lock recovery/failure injection | NFR4 | not-run | Requires clipboard lock simulation | — |
| 18 | File output: normal user-writable directory | FR48 | not-run | Requires interactive file save and verification | — |
| 19 | File output: missing save path | FR48 | not-run | Requires path configuration and error handling verification | — |
| 20 | File output: permission denied path | FR48 | not-run | Requires restricted directory access | — |
| 21 | File output: long path (>MAX_PATH) | FR48 | not-run | Requires long path configuration | — |
| 22 | Both-target output: clipboard success + file failure | FR48 | not-run | Requires controlled failure injection | — |
| 23 | Both-target output: file success + clipboard failure | FR48 | not-run | Requires controlled failure injection | — |
| 24 | HDR-ready state display on real HDR display | NFR27 | not-run | Requires HDR display hardware | — |
| 25 | HDR state display on SDR display | NFR27 | not-run | Requires SDR display hardware | — |
| 26 | HDR unavailable state behavior | NFR27 | not-run | Requires HDR-capable hardware with HDR disabled | — |
| 27 | Degraded preview state behavior | NFR27 | not-run | Requires hardware/OS conditions for degraded state | — |
| 28 | Unsupported capture state behavior | NFR27 | not-run | Requires hardware/OS conditions for unsupported state | — |
| 29 | Alert display behavior on HDR display | NFR27 | not-run | Requires HDR display and alert triggering | — |
| 30 | Alert display behavior on SDR display | NFR27 | not-run | Requires SDR display and alert triggering | — |
| 31 | Resource trends: private bytes across 10+ capture cycles | NFR5 | not-run | Requires resource monitoring tooling and repeated capture | — |
| 32 | Resource trends: handle count across 10+ capture cycles | NFR5 | not-run | Requires resource monitoring tooling and repeated capture | — |
| 33 | Resource trends: GPU allocator across 10+ capture cycles | NFR5 | not-run | Requires GPU monitoring tooling and repeated capture | — |
| 34 | Tray-only capture flow: start from tray, capture, output | NFR23 | not-run | Requires tray interaction on real hardware | — |
| 35 | Shortcut-only capture flow: global hotkey, capture, output | NFR23 | not-run | Requires global hotkey registration on real hardware | — |
| 36 | Quit resource cleanup: capture active during quit | NFR11 | not-run | Requires active capture session and quit command | — |
| 37 | Quit resource cleanup: deterministic teardown verification | NFR11 | not-run | Requires teardown evidence inspection | — |
| 38 | Settings persistence: change settings, relaunch app | FR38 | not-run | Requires app relaunch and settings comparison | — |
| 39 | After-capture behavior: open file after folder output | FR36 | not-run | Requires folder output and file open verification | — |
| 40 | After-capture behavior: reveal in Explorer after folder output | FR36 | not-run | Requires folder output and Explorer reveal verification | — |
| 41 | Escape cancel without active crop | FR47 | not-run | Requires interactive overlay | — |
| 42 | Escape cancel with active crop | FR47 | not-run | Requires interactive overlay with crop | — |
| 43 | Multi-monitor overlay behavior (primary + secondary) | NFR27 | not-run | Requires multi-monitor hardware setup | — |

**Note on partial prior validation:**
- Epic 7 (stories 7.1–7.5) received Windows manual-pass validation by Dana on 2026-05-26. Tray menu, open main window/settings from tray, global hotkeys, background/minimize-to-tray, and quit-from-tray scenarios have manual evidence from that session.
- Story 4.5/5.1 received partial manual validation on a single HDR 4K display at 150% DPI.

**Prior evidence mapping (10 scenarios with partial evidence):**

| Scenario | Source | Evidence |
|----------|--------|----------|
| #1 Direct monitor capture | Story 4.5 | Confirmed working on HDR 4K at 150% DPI |
| #5 Repeated lifecycle (partial) | Epic 7 | Background/minimize-to-tray flow validated |
| #6 Overlay crop interaction | Story 4.5 | Crop creation, adjustment, release-to-capture confirmed at 150% DPI |
| #12 DPI scaling at 150% | Story 4.5 | Validated at 150% on HDR 4K |
| #14 Clipboard output: paste to Paint | Story 4.5 | Basic clipboard write confirmed |
| #34 Tray-only capture flow | Epic 7 | Tray menu interaction validated |
| #35 Shortcut-only capture flow | Epic 7 | Global hotkey registration validated |
| #36 Quit resource cleanup (capture active) | Epic 7 | Quit-from-tray validated |
| #37 Quit resource cleanup (teardown) | Epic 7 | Quit-from-tray validated |
| #38 Settings persistence (partial) | Epic 7 | Settings navigation from tray validated |

## Section 3: Validation Gap Inventory

Gaps sourced from `docs/validation/mvp-validation-registry.md` and story acceptance criteria, classified per AC2.

### Hardware/Platform Behavior (Epic 4) — 5 gaps

| # | Gap | Classification | Rationale |
|---|-----|---------------|-----------|
| 1 | Escape cancel with and without active crop | Limitation | Core overlay function; code path tested but not manually validated on hardware. Low risk due to comprehensive automated overlay tests. |
| 2 | Multi-monitor behavior beyond single-monitor | Deferred risk | NFR27 requirement; cannot be validated without multi-monitor hardware. Early users on single-monitor setups are unaffected. |
| 3 | DPI scales 100%, 125%, 200% (only 150% tested) | Deferred risk | NFR27 requirement; 150% was partially validated. Other scales may have minor visual issues but core functionality is code-tested. |
| 4 | SDR display behavior not separately validated | Deferred risk | HDR/SDR mixed setup claims unsupported. Code handles SDR paths but no dedicated SDR-only testing performed. |
| 5 | Clipboard lock recovery/failure injection | Limitation | NFR4 requirement; failure handling code exists and is tested, but real clipboard lock scenarios not manually exercised. |

### Settings/Accessibility (Epic 5) — 5 gaps

| # | Gap | Classification | Rationale |
|---|-----|---------------|-----------|
| 6 | Text scaling and high contrast accessibility | Limitation | Accessibility requirements not validated. WinUI 3 provides baseline accessibility; custom UI may have gaps at non-standard text scales. |
| 7 | Mixed-DPI multi-monitor settings rendering | Deferred risk | NFR27 requirement; requires multi-monitor with different DPI scales. Single-DPI rendering validated at 150%. |
| 8 | Keyboard navigation in settings | Limitation | Tab order and keyboard-only interaction not manually verified. WinUI 3 provides baseline keyboard nav. |
| 9 | Screen reader exposure for settings | Limitation | AutomationProperties and screen reader compatibility not validated. Relies on WinUI 3 defaults. |
| 10 | App relaunch persistence (packaged app) | Deferred risk | FR38; JSON persistence code tested but packaged app relaunch cycle not manually verified. |

### Output Behavior (Epic 6) — 4 gaps

| # | Gap | Classification | Rationale |
|---|-----|---------------|-----------|
| 11 | Real clipboard compatibility (Paint, Photos, Chromium) | Limitation | NFR19; clipboard write code tested but paste-to-target-app not verified. Basic clipboard write was confirmed in Story 4.5 (paste to Paint). |
| 12 | Folder output to protected/inaccessible paths | Limitation | NFR18; error handling code exists and is tested, but real filesystem permission denial not manually exercised. |
| 13 | Explorer reveal/open behavior | Limitation | FR36; code path exists but not manually verified on real filesystem. |
| 14 | Both-target partial failure with slow OS behavior | Deferred risk | FR48, NFR4; timeout and partial-failure code tested, but real slow-OS scenarios not manually exercised. |

### HDR Display (Epic 8) — 3 gaps

| # | Gap | Classification | Rationale |
|---|-----|---------------|-----------|
| 15 | HDR state display on real HDR display | Deferred risk | HDR trust model code tested; real HDR display rendering not validated. UX-DR5 not fully validated. |
| 16 | HDR state display on SDR display | Deferred risk | HDR/SDR discrimination code tested; SDR display rendering not separately validated. |
| 17a | Alert display behavior on HDR display | Deferred risk | NFR27; alert projection code tested; real HDR display alert rendering not validated (Scenario #29). |
| 17b | Alert display behavior on SDR display | Deferred risk | NFR27; alert projection code tested; real SDR display alert rendering not validated (Scenario #30). |

### Performance/Stability (NFR1, NFR5) — 3 gaps

| # | Gap | Classification | Rationale |
|---|-----|---------------|-----------|
| 18 | Trigger-to-active responsiveness not validated | Limitation | NFR1; requires timing instrumentation on real hardware for shortcut, tray, and main window entry points (Scenarios #2-4). |
| 19 | Repeated lifecycle stability not validated | Limitation | NFR5, FR45; requires repeated interactive capture sessions and resource monitoring (Scenario #5). |
| 20 | Resource trends not validated | Limitation | NFR5; requires resource monitoring tooling across 10+ capture cycles for private bytes, handles, and GPU allocator (Scenarios #31-33). |

### Gap Summary

| Category | Gap Count | Blockers | Limitations | Deferred Risks |
|----------|-----------|----------|-------------|----------------|
| Hardware/platform (Epic 4) | 5 | 0 | 2 | 3 |
| Settings/accessibility (Epic 5) | 5 | 0 | 3 | 2 |
| Output behavior (Epic 6) | 4 | 0 | 3 | 1 |
| HDR display (Epic 8) | 4 | 0 | 0 | 4 |
| Performance/stability (NFR1, NFR5) | 3 | 0 | 3 | 0 |
| **Total** | **21** | **0** | **11** | **10** |

## Section 4: Release Readiness Summary

### Automated Gates Status

All automated gates pass. The only exception is 2 pre-existing test failures in `DefaultSettingsProviderTests` that are documented in `deferred-work.md` and unrelated to the MVP capture loop. No source code was modified during this validation process.

### Windows Manual Validation Status

No manual validation scenarios were executed in this validation run. 10 scenarios received partial Windows manual-pass evidence from prior work:
- Epic 7 (5 capabilities): tray, window/settings-from-tray, hotkeys, background/minimize, quit — validated by Dana on 2026-05-26
- Story 4.5/5.1: direct capture, overlay, clipboard write at 150% DPI on HDR 4K — partial manual validation

The remaining 33 scenarios have no manual validation evidence.

### Capabilities Validated for Private Preview / Early Validation

The following capabilities had sufficient evidence for private preview or early validation under the 2026-06-03 MVP foundation bar. This section does not approve Perfect HDR Fidelity Public Release.

1. **Capture lifecycle management** (Epic 4): command routing, session state machine, direct monitor capture path — Windows CI-pass + partial manual
2. **Overlay and crop interaction** (Epic 4): crop creation, adjustment, release-to-capture — Windows CI-pass + partial manual at 150% DPI
3. **Main window and settings** (Epic 5): capture actions, HDR status, settings navigation, persistence — Windows CI-pass + partial manual at 150% DPI
4. **Output pipeline** (Epic 6): clipboard, folder, both-target output — Windows CI-pass + partial clipboard manual
5. **Tray, hotkeys, and background** (Epic 7): all tray/hotkey/quit capabilities — Windows manual-pass
6. **HDR trust model** (Epic 8): state mapping, alerts, diagnostics — Windows CI-pass

### Known Limitations for Private Preview / Early Validation Users

1. **Multi-monitor**: overlay placement and capture targeting on multi-monitor setups not validated. May work but is unconfirmed.
2. **DPI scaling**: only 150% DPI was tested. Users at 100%, 125%, or 200% may encounter visual issues.
3. **SDR-only displays**: SDR display behavior not separately validated. HDR trust model handles SDR states in code but no dedicated SDR testing.
4. **Accessibility**: text scaling, high contrast, keyboard navigation, and screen reader support not validated. WinUI 3 provides baseline accessibility.
5. **Clipboard compatibility**: clipboard write works for basic paste (confirmed with Paint in Story 4.5), but Photos and Chromium paste not verified.
6. **HDR display rendering**: HDR state indicators and alerts have not been validated on a real HDR display.
7. **Resource trends**: no long-running resource stability data. Code manages disposal deterministically but 10+ cycle resource monitoring not performed.
8. **Settings persistence in packaged context**: settings JSON persistence works in development; packaged app relaunch not verified.

### MVP Foundation Blockers

**No MVP foundation blockers identified in this 2026-06-03 snapshot.** All 21 validation gaps were classified as limitations (11) or deferred risks (10) for the MVP foundation/private-preview bar. This does not mean Perfect HDR Fidelity Public Release is unblocked; that release target is governed by `docs/validation/release-validation-checklist.md`.

### Recommendation

Lumiere was considered usable for private preview / early validation with documented limitations under this snapshot. Perfect HDR Fidelity Public Release requires the stricter gates in `docs/validation/release-validation-checklist.md`. Priority follow-up validation should target:
1. Multi-monitor behavior (deferred risk — NFR27)
2. DPI scaling at 100%/125%/200% (deferred risk — NFR27)
3. HDR display rendering (deferred risk — HDR trust model)
4. Real clipboard compatibility with target apps (limitation — NFR19)

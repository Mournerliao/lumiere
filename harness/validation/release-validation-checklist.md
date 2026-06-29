# Release Validation Checklist

Updated: 2026-06-21

This checklist is the live release-gate view for Lumiere. Its single current target is **Public perfect-HDR-fidelity**.

Foundation readiness checks still matter because they provide the baseline evidence the public-fidelity release builds on, but they are not a separate current release brand or stage.

Use this document to record manual Windows validation that may already have happened, to decide which release gate a build satisfies, and to add new validation items when product behavior changes.

## Release Gate Definitions

| Status | Meaning | Release effect |
|---|---|---|
| PASS | The scenario was validated on real Windows hardware and evidence is recorded. | Counts toward the applicable release gate. |
| PASS with limitation | The core behavior works, but a known constraint remains and is documented. | Counts only if the limitation is included in release notes or release copy. |
| FAIL | The scenario did not meet expected behavior. | Blocks the applicable release gate unless explicitly removed from scope. |
| NOT RUN | No recorded validation evidence exists yet. | Does not count as validated. |
| N/A | The scenario is not applicable to this release scope. | Requires a reason. |

Validation evidence should include date, tester, build or commit, device/display setup, Windows version, display mode, DPI scale, output target apps where relevant, observed result, and any log or screenshot path.

## Foundation Readiness Checks

These are the minimum baseline checks that should be recorded before treating the current implementation as a credible foundation for Public perfect-HDR-fidelity. They do not by themselves approve public release.

| Gate | Required evidence | Current status | Evidence / notes |
|---|---|---|---|
| Automated restore/build/tests/format | `dotnet restore`, `dotnet build`, Graphics tests, Overlay tests, `dotnet format` pass on Windows. | NOT RUN | Last recorded matrix is 2026-06-03; rerun before release and record the new build/commit. |
| Direct capture entry | Main window, tray, and hotkey can enter capture without picker-first interruption. | NOT RUN | User reports some validation exists; backfill date, build, device, display setup, and observed result before counting it. |
| Region capture loop | Drag valid crop, release to capture/output, overlay closes, app returns to ready state. | NOT RUN | Link detailed checks from `overlay-validation.md`. |
| Fullscreen capture loop | Fullscreen command captures current target and produces configured output. | NOT RUN | Must include no stuck overlay/session state. |
| Output usability | Clipboard and folder output work for the current baseline scope. | NOT RUN | Include target app paste/open/reveal observations. |
| HDR status honesty | HDR state shown in UI matches observed display configuration and does not overclaim preservation. | NOT RUN | Must cover at least one HDR-active and one non-HDR/degraded condition if available. |
| Tray/background workflow | Minimize/close-to-tray, tray capture commands, open settings/main window, and quit work. | NOT RUN | Epic 7 recorded manual evidence on 2026-05-26; confirm still valid for current build before counting it. |
| Repeated lifecycle stability | 10+ capture/cancel/output cycles show no stuck state or obvious resource growth. | NOT RUN | Use `lifecycle-validation.md` for detailed checks and `resource-trend-validation.md` when recording sampler artifacts. |
| DPI/layout sanity | Main panel, settings, overlay, and InfoBar remain usable at tested DPI scales. | NOT RUN | Minimum: current tester DPI. Preferred: 100%, 125%, 150%, 200%. |
| Known limitations reviewed | Remaining NOT RUN or PASS-with-limitation items are listed in release notes. | NOT RUN | Must happen after this checklist is filled. |

## Public perfect-HDR-fidelity Gates

These gates apply to the fixed public release target. They are stricter than the foundation readiness checks above and must prove both visual-match output and at least one HDR-preserved supported output path.

| Gate | Required evidence | Current status | Evidence / notes |
|---|---|---|---|
| Fidelity definition approved | Written definition distinguishes data-preserving capture, HDR preview match, SDR-compatible conversion, and HDR-preserved file export. | PASS with limitation | Output profile contracts and UI projections now distinguish SDR-compatible, visual-match, HDR-preserved, and unvalidated paths. Final public copy review still required before release. |
| Target-aware HDR detection | HDR readiness is tied to the active capture target display/output, including mixed HDR/SDR multi-monitor setups. | PASS with limitation | Code now carries display identity and probes by target evidence where available; run `target-aware-hdr-validation.md` for the focused Windows workflow. Mixed HDR/SDR multi-monitor Windows manual validation is still required before this can count as an unrestricted public claim. |
| HDR-preserved output profile contract | At least one enabled output profile has source format, destination format, transfer function, primaries, conversion/tone-mapping policy, metadata policy, and named viewer assumptions for HDR preservation. | NOT RUN | HDR10 JXR codec/readback/audit metadata seams exist, but runtime HDR10 export remains gated until viewer-recognized HDR10 metadata and Windows manual viewer validation pass. SDR-compatible output can supplement this but cannot replace it. |
| Supported output compatibility matrix | Visual-match output and HDR-preserved output are validated against named target apps and viewers. | NOT RUN | Viewer evidence is modeled and can be loaded from validation artifacts, but real named target-app/viewer evidence has not been recorded. App-loaded `evidencePaths` must be workspace-local under `evidence\`; repo-relative review links cannot unlock runtime validation state. HDR10 JXR runtime readiness also requires every participating folder-output viewer artifact to align with the current build, so one current artifact cannot mask stale evidence for another named viewer. Profile-specific `outputTargetsCovered` controls which output target a record proves; broad session-level `Both` coverage cannot hide missing folder-side HDR10 evidence. Must separate "artifact written", "visual match", and "HDR preserved". |
| HDR/SDR visual validation set | Standard test content covers bright highlights, SDR/HDR mixed content, browser/media/game scenarios, and display mode changes. | NOT RUN | Standard scenario guidance now lives in `hdr-sdr-validation-scenarios.md` with a reusable session template, but executed validation sessions are still missing. |
| Multi-monitor and DPI validation | HDR/SDR mixed displays and common DPI scales are recorded with pass/fail/limitation status. | NOT RUN | Target-aware code support exists, but public claims cannot imply untested display topologies or DPI configurations. |
| Long-run lifecycle evidence | Repeated capture/output cycles record private bytes, handles, and GPU resource trends. | NOT RUN | Use `resource-trend-validation.md` plus `templates/resource-trend-session-template.md`. `Create trend draft` now keeps imported sampler summaries at `NOT RUN` when CSV evidence is missing/unreadable or primary process metrics are incomplete. No focused 50+ or 100+ cycle evidence has been committed yet. |
| Public release copy review | Release notes and UI copy only claim fidelity modes that passed validation. | NOT RUN | Planned as final release gate. Limitations and unsupported modes must be explicit. |

## Functional Validation Matrix

### 1. App Shell

| ID | Scenario | Expected result | Status | Evidence / notes | Retest trigger |
|---|---|---|---|---|---|
| REL-SHELL-01 | Launch app from local build. | Main window opens, no startup crash, logging initializes. | NOT RUN |  | App startup, DI, package, logging, or WinAppSDK change. |
| REL-SHELL-02 | Open settings from main window, then return. | Capture state is preserved; layout remains usable. | NOT RUN |  | Settings UI, shell navigation, window sizing change. |
| REL-SHELL-03 | Minimize or close to tray. | Window hides; tray remains available; app is still running. | NOT RUN |  | Tray, window presenter, lifecycle, hotkey change. |
| REL-SHELL-04 | Open main window and settings from tray. | Commands open the expected surface. | NOT RUN |  | Tray command or shell projection change. |
| REL-SHELL-05 | Quit from tray while idle and while capture is active. | App exits cleanly; resources are torn down. | NOT RUN |  | Capture lifecycle, tray, app shutdown change. |

### 2. Capture And Overlay

| ID | Scenario | Expected result | Status | Evidence / notes | Retest trigger |
|---|---|---|---|---|---|
| REL-CAP-01 | Start region capture from main window. | Direct monitor capture starts without picker-first interruption. | NOT RUN |  | Capture target selection or command routing change. |
| REL-CAP-02 | Start region capture from tray. | Same behavior as main window entry. | NOT RUN |  | Tray capture command change. |
| REL-CAP-03 | Start region capture from global hotkey. | Same behavior as main window entry. | NOT RUN |  | Hotkey registration or dispatch change. |
| REL-CAP-04 | Drag a valid crop and release. | Crop commits immediately, output starts, overlay closes after feedback. | NOT RUN |  | Overlay crop, output, or release-to-capture change. |
| REL-CAP-05 | Try tiny or invalid crop. | No output is produced; overlay remains usable or gives recoverable feedback. | NOT RUN |  | Crop validity threshold or feedback change. |
| REL-CAP-06 | Press Escape before and during crop. | Capture cancels; overlay closes; app returns to ready state. | NOT RUN |  | Keyboard routing, overlay close, lifecycle change. |
| REL-CAP-07 | Reopen capture repeatedly. | No stale frame/status updates from prior sessions. | NOT RUN |  | Preview generation, frame callback, teardown change. |
| REL-CAP-08 | Start fullscreen capture from main window/tray/hotkey. | Full target capture completes and produces configured output. | NOT RUN |  | Fullscreen command or auto-confirm change. |

### 3. Output

| ID | Scenario | Expected result | Status | Evidence / notes | Retest trigger |
|---|---|---|---|---|---|
| REL-OUT-01 | Clipboard output, paste into Paint. | Pasted image appears; dimensions roughly match expected crop/fullscreen output. | NOT RUN |  | Clipboard encoder/output pipeline change. |
| REL-OUT-02 | Clipboard output, paste into Photos or another image consumer. | Target app accepts the output or limitation is recorded. | NOT RUN |  | Clipboard format change. |
| REL-OUT-03 | Clipboard output, paste into Microsoft Edge. | Target app accepts the output or limitation is recorded. | NOT RUN |  | Clipboard format change. |
| REL-OUT-04 | Folder output to normal user-writable directory. | PNG is written with expected name; file opens. | NOT RUN |  | Folder output, naming, save path change. |
| REL-OUT-05 | Both-target output. | Clipboard and file results are both reported; partial failures are clear. | NOT RUN |  | Output orchestration or result model change. |
| REL-OUT-06 | Open after capture. | Saved file opens only when a file artifact exists. | NOT RUN |  | After-capture behavior change. |
| REL-OUT-07 | Reveal after capture. | Explorer opens at the saved file only when a file artifact exists. | NOT RUN |  | Shell action or after-capture behavior change. |
| REL-OUT-08 | Missing, inaccessible, or read-only save path. | Failure is graceful and user-facing message is accurate. | NOT RUN |  | Path policy or filesystem error handling change. |

### 4. HDR Trust And Display Conditions

| ID | Scenario | Expected result | Status | Evidence / notes | Retest trigger |
|---|---|---|---|---|---|
| REL-HDR-01 | HDR display with Windows HDR enabled. | UI state and overlay messaging match observed HDR-ready condition. | NOT RUN | Run `target-aware-hdr-validation.md` and record the active target display. | HDR probe, swap-chain, status copy, display mapping change. |
| REL-HDR-02 | HDR-capable display with Windows HDR disabled. | UI asks user to enable HDR without claiming HDR-ready output. | NOT RUN | Run `target-aware-hdr-validation.md` and record the active target display. | HDR probe or status copy change. |
| REL-HDR-03 | SDR-only display or SDR target. | UI reports unavailable/degraded honestly and capture behavior remains recoverable. | NOT RUN | Run `target-aware-hdr-validation.md` and record the active target display. | HDR probe, fallback messaging, capture state change. |
| REL-HDR-04 | Multi-monitor mixed HDR/SDR setup. | Status reflects the capture target, or limitation is explicitly recorded. | NOT RUN | Run `target-aware-hdr-validation.md`; record both the chosen target and any unresolved/mixed-monitor limitation separately. | Multi-monitor target selection or HDR probe change. |
| REL-HDR-05 | Bright, dark, and high-contrast content under overlay. | Crop border, mask, and status panel remain legible. | NOT RUN |  | Overlay styling or status panel change. |
| REL-HDR-06 | Export profile settings. | UI does not imply HDR10/P3 are functional until encoder and validation exist. | NOT RUN | Run `settings-accessibility-validation.md` export-profile checks and record selected-disabled behavior separately from artifact/output fidelity. | Export format UI/copy change. |

### 5. Settings And Preferences

| ID | Scenario | Expected result | Status | Evidence / notes | Retest trigger |
|---|---|---|---|---|---|
| REL-SET-01 | Change output target and capture. | Capture uses the selected output target. | NOT RUN |  | Settings persistence or output policy change. |
| REL-SET-02 | Change save path and relaunch. | Save path persists and is used for folder output. | NOT RUN |  | Settings store, picker, packaged app change. |
| REL-SET-03 | Toggle timestamp naming. | Folder filenames follow the selected naming behavior. | NOT RUN |  | Naming policy change. |
| REL-SET-04 | Toggle HDR alerts. | Alerts appear or stay hidden according to preference. | NOT RUN |  | Alert mapping or settings store change. |
| REL-SET-05 | Edit fullscreen and region shortcuts. | New shortcuts register or failure is clearly communicated. | NOT RUN |  | Shortcut editor, parser, registrar change. |
| REL-SET-06 | Relaunch app after settings changes. | Persisted settings match previous choices. | NOT RUN |  | Settings schema or packaging change. |

### 6. Accessibility, DPI, And Layout

| ID | Scenario | Expected result | Status | Evidence / notes | Retest trigger |
|---|---|---|---|---|---|
| REL-A11Y-01 | DPI at tester's normal scale. | Main, settings, tray, and overlay are usable with no clipping. | NOT RUN | Run the DPI workflow in `settings-accessibility-validation.md` and record the tested scales. | Any UI layout change. |
| REL-A11Y-02 | DPI 100%, 125%, 150%, 200% where available. | Controls remain visible and clickable; overlay geometry remains correct. | NOT RUN | Run the DPI workflow in `settings-accessibility-validation.md` and record unavailable scales explicitly. | Window sizing, overlay, crop mapping change. |
| REL-A11Y-03 | Keyboard-only settings navigation. | Focus order is logical; controls are operable. | NOT RUN | Run the keyboard workflow in `settings-accessibility-validation.md`. | Settings controls or styles change. |
| REL-A11Y-04 | High contrast or theme variation. | Text and critical controls remain readable. | NOT RUN | Run the high contrast workflow in `settings-accessibility-validation.md`. | Brush/style/theme change. |
| REL-A11Y-05 | Screen reader smoke check. | Primary controls have usable names and states. | NOT RUN | Run the screen-reader workflow in `settings-accessibility-validation.md`. | Control template or AutomationProperties change. |

### 7. Stability And Performance

| ID | Scenario | Expected result | Status | Evidence / notes | Retest trigger |
|---|---|---|---|---|---|
| REL-STAB-01 | 10+ start/cancel/capture/output cycles. | No stuck state, crash, or stale overlay. | NOT RUN |  | Capture, overlay, output, or teardown change. |
| REL-STAB-02 | Observe private bytes and handle count during repeated cycles. | No obvious unbounded growth. | NOT RUN | Run `resource-trend-validation.md` and attach the sampler CSV/summary artifacts. Drafts with missing/unreadable CSV paths or incomplete primary metrics remain `NOT RUN`. | D3D, WGC, swap-chain, output snapshot change. |
| REL-STAB-03 | Trigger capture while another capture is active. | Duplicate command is rejected or queued according to UI state. | NOT RUN |  | Command coordinator/session state change. |
| REL-STAB-04 | Slow or failing clipboard/file target. | App recovers and reports failure without leaving capture resources active. | NOT RUN |  | Output timeout/failure handling change. |

## Validation Record Template

Copy this section for each validation session.

```text
Date:
Tester:
Build / commit:
Windows version:
Device:
GPU:
Display setup:
HDR state:
DPI scale(s):
Entry points tested:
Output targets tested:
Target apps tested:
Checklist IDs covered:
Result summary:
Evidence paths:
Known limitations:
Follow-up issues/stories:
```

## Adding New Validation Items

When a feature changes or a new feature is added:

1. Add at least one checklist row for the user-visible behavior.
2. Add retest triggers so future changes know when the row becomes stale.
3. If the feature touches WGC, DXGI, D3D11, HDR, multi-monitor behavior, clipboard, filesystem, tray, hotkeys, or packaging, require Windows manual validation.
4. Link detailed workflows to focused documents such as `target-aware-hdr-validation.md`, `overlay-validation.md`, `output-validation.md`, `lifecycle-validation.md`, `hdr-sdr-validation-scenarios.md`, and `settings-accessibility-validation.md` instead of duplicating every step here.
5. Update `history/foundation-validation-snapshot-2026-06-03.md` only when a new point-in-time snapshot is intentionally recorded.

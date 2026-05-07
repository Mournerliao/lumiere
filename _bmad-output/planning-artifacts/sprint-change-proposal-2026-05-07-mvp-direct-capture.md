---
workflowType: sprint-change-proposal
project_name: lumiere
user_name: lumiere
date: 2026-05-07
status: approved
mode: batch
change_scope: moderate
trigger:
  - mvp route review
  - real-device feedback
  - direct region screenshot expectation
inputDocuments:
  - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/prd.md
  - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/epics.md
  - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md
  - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/ux-design-specification.md
  - /Users/asherliao/Projects/lumiere/_bmad-output/implementation-artifacts/sprint-status.yaml
  - /Users/asherliao/Projects/lumiere/harness/design/mvp/lumiere-mvp-design.png
---

# Sprint Change Proposal: MVP Direct Capture Flow

## 1. Issue Summary

During real-device MVP testing, the current `Capture` action first opens the Windows `GraphicsCapturePicker`, requiring the user to choose a window or display before entering the crop overlay. This behavior is technically valid for the existing WGC target-selection story, but it does not match the expected screenshot-tool workflow.

Expected MVP behavior is:

1. User clicks `Capture`.
2. Lumiere immediately enters a full-screen capture overlay.
3. User drags a region over the current desktop, regardless of which window is under the pointer.
4. Releasing the mouse completes the capture and copies it to the clipboard.
5. Lumiere shows a lightweight `Copied to clipboard` confirmation.

The current behavior feels like a capture/debug utility instead of a mainstream screenshot tool. The MVP design asset now also represents release-to-capture and auto-copy behavior:

- `/Users/asherliao/Projects/lumiere/harness/design/mvp/lumiere-mvp-design.png`

The `harness/design/mvp` folder is now an implementation-planning input, not a loose visual reference. Stories created from this proposal must reference the MVP design asset for interaction intent, visual hierarchy, and scope boundaries.

### Evidence

- Current app code wires both primary capture buttons to `OnSelectCaptureTargetClick`, which instantiates `CaptureTargetSelectionService(new GraphicsCaptureTargetPicker(this))`.
- `GraphicsCaptureTargetPicker` delegates to `GraphicsCapturePickerInterop.PickSingleItemAsync(owner)`, so picker UI is currently mandatory on the default path.
- Current overlay code still exposes explicit `Confirm crop` and `Cancel` buttons; pointer release only commits the crop selection, not the final screenshot result.
- Existing planning artifacts intentionally defer clipboard and output behavior to Epic 6, but the revised MVP user experience needs a narrow clipboard output path now.
- Windows capture interop supports direct monitor capture through `IGraphicsCaptureItemInterop.CreateForMonitor`, which allows the default MVP path to avoid picker-first target selection while still using WGC.
- The MVP design folder contains the current MVP design board and must be used when creating implementation stories:
  - `/Users/asherliao/Projects/lumiere/harness/design/mvp/lumiere-mvp-design.png`

Primary Microsoft reference:

- `IGraphicsCaptureItemInterop.CreateForMonitor`: https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.capture.interop/nf-windows-graphics-capture-interop-igraphicscaptureiteminterop-createformonitor

## 2. Change Navigation Checklist Results

### 1. Understand Trigger and Context

- [x] 1.1 Triggering story identified: Story 2.1, `Start Capture and Select a Display or Window Target`, established the picker-first default path.
- [x] 1.2 Core problem: the MVP default capture flow is misaligned with screenshot-tool user expectations. Category: misunderstanding/underspecification of original MVP interaction requirements, discovered through real-device UX testing.
- [x] 1.3 Evidence gathered: current code, implementation artifacts, MVP design image, and user real-device feedback.

### 2. Epic Impact Assessment

- [x] 2.1 Epic 2 can remain valid as capture lifecycle infrastructure, but its default MVP target acquisition story needs an additional direct-monitor path.
- [x] 2.2 Add new story under Epic 2 for direct monitor target creation without `GraphicsCapturePicker` on the default path.
- [x] 2.3 Epic 3 needs a new release-to-capture overlay story and should stop treating explicit confirm as the MVP primary interaction.
- [x] 2.4 Epic 6 remains mostly post-MVP, but a narrow clipboard semantics story must be promoted into MVP.
- [x] 2.5 Priority changes: implement direct monitor capture and release-to-copy before settings, tray, global hotkeys, advanced diagnostics, annotations, or HDR export.

### 3. Artifact Conflict and Impact Analysis

- [x] 3.1 PRD conflict: MVP scope currently says crop interaction includes confirm/cancel and clipboard is post-MVP. This must change to direct region capture with a narrow MVP clipboard output.
- [x] 3.2 Architecture impact: add a monitor-target provider under Infrastructure/Capture boundaries; picker becomes fallback/debug rather than default.
- [x] 3.3 UX impact: default flow changes from picker-first + confirm button to direct full-screen overlay + release-to-copy. Escape remains the cancellation path.
- [x] 3.4 Secondary artifacts: sprint status and future story backlog need updates after approval. Windows validation docs should add direct monitor, full-screen app, and multi-monitor start-display scenarios.

### 4. Path Forward Evaluation

- [x] 4.1 Direct Adjustment: viable. Effort: Medium. Risk: Medium.
- [x] 4.2 Rollback: not viable. Existing picker target selection, lifecycle, and overlay work remain useful infrastructure and should not be reverted.
- [x] 4.3 PRD MVP Review: viable. MVP must be redefined from "confirmed in-app crop state" to "direct region capture copied to clipboard with explicit MVP output semantics."
- [x] 4.4 Recommended path: Hybrid of Direct Adjustment + MVP Review.

### 5. Proposal Components

- [x] 5.1 Issue summary included.
- [x] 5.2 Epic and artifact impacts included.
- [x] 5.3 Recommended path included.
- [x] 5.4 MVP impact and action plan included.
- [x] 5.5 Handoff plan included.

### 6. Final Review and Handoff

- [x] 6.1 Checklist completed for all applicable items.
- [x] 6.2 Proposal drafted as actionable document.
- [x] 6.3 User approved this proposal on 2026-05-07.
- [x] 6.4 `sprint-status.yaml` updated with Story 2.5, Story 3.6, and Story 6.0 backlog entries on 2026-05-07.
- [x] 6.5 Next steps and handoff plan included.

## 3. Impact Analysis

### Epic Impact

#### Epic 1: Trusted HDR Preview Foundation

Status remains `done`.

No route change is required. The existing FP16/scRGB WGC-to-swap-chain foundation remains the basis for the new direct capture flow.

#### Epic 2: Capture Target and Session Lifecycle

Epic 2 should remain complete for picker-based lifecycle infrastructure, but it needs one additional MVP story:

- Story 2.5: `Create Monitor Capture Targets Without Picker`

This story promotes the deferred typed target creation work into MVP, using HMONITOR-based WGC interop to create a display target directly. `GraphicsCapturePicker` remains available as fallback/debug or future explicit window capture, but it is no longer the default capture button behavior.

#### Epic 3: Fullscreen Overlay Crop Workflow

Epic 3 needs one additional MVP story:

- Story 3.6: `Release to Capture and Copy`

This story changes MVP overlay behavior so pointer release over a valid crop confirms the selection and starts output. Explicit confirm controls are no longer the default MVP path. Escape remains cancellation.

#### Epic 4: Diagnostics and HDR Capability Trust

Reduce MVP scope to user-facing readiness and validation docs:

- Keep Story 4.1 for concise status.
- Keep Story 4.4 for manual HDR/direct-capture validation.
- Defer Story 4.2 advanced technical diagnostics unless needed for debugging direct monitor interop.
- Defer Story 4.3 full HDR/SDR/multi-monitor capability detection beyond basic selected-monitor status.

#### Epic 5: Local Preferences and Diagnostic Controls

Defer from MVP. MVP can use hard-coded defaults:

- Auto-copy enabled.
- No settings persistence required.
- No settings window required.
- No tray minimize behavior required.

#### Epic 6: Post-MVP Capture Output and Workflow Expansion

Keep Epic 6 as post-MVP, but split out one narrow MVP story:

- Story 6.0: `Define and Implement MVP Clipboard Output`

This is not full HDR export. It is a constrained MVP output path with explicit semantics. If implementation produces SDR/tone-mapped clipboard content, the app must not claim HDR-preserving clipboard output.

Global hotkey, tray menu, annotation, HDR still export, SDR tone-mapping presets, and capture history remain post-MVP.

### Technical Impact

Affected areas:

- `Lumiere.Infrastructure`
  - Add narrow native interop for monitor enumeration/current cursor monitor and `IGraphicsCaptureItemInterop.CreateForMonitor`.
  - Keep Win32/HMONITOR/COM details inside infrastructure.

- `Lumiere.Capture`
  - Add target creation path for monitor capture targets.
  - Preserve `CaptureTargetKind.Display` for direct monitor targets.
  - Keep picker result types for fallback/debug.

- `Lumiere.App`
  - Change `Capture` button default action from picker-first to direct overlay/direct monitor capture.
  - Determine the start monitor, create monitor target, then start preview/overlay.

- `Lumiere.Overlay`
  - Change valid pointer release from "commit selection only" to "confirm capture selection".
  - Keep Escape/cancel semantics.
  - Remove or hide explicit confirm button from MVP UI.

- `Lumiere.Graphics`
  - Existing preview copy path remains valid.
  - Clipboard output may require a separate one-shot output path. It must not weaken the live preview path.

- Tests
  - Add tests for monitor target provider result mapping.
  - Add tests for release-to-confirm overlay behavior.
  - Add tests for MVP clipboard output semantic labeling.

### Design Input Impact

The following design asset becomes a required planning input for MVP implementation stories:

- `/Users/asherliao/Projects/lumiere/harness/design/mvp/lumiere-mvp-design.png`

Implementation stories should use it as the source of truth for:

1. Default capture flow: no picker-first step, direct full-screen overlay, drag region, release to capture.
2. Overlay simplicity: no multi-action toolbar in MVP; preserve only crop selection, size feedback where useful, and lightweight copied-to-clipboard feedback.
3. Main window intent: a compact native Windows capture entry with clear readiness state.
4. Settings/tray surfaces: visible in the design board as product direction, but not MVP implementation scope unless separate stories explicitly pull them in.
5. Visual quality bar: dark WinUI-like styling, crisp typography, consistent blue accent, no text overflow, no awkward wrapping such as the previous long tray label.

The design asset is illustrative for visual alignment, not a pixel-perfect contract. Native WinUI controls, accessibility, platform behavior, and HDR preview constraints remain higher-priority than exact image reproduction.

### Validation Impact

Windows manual validation must add:

1. Click `Capture` and confirm no picker appears on default path.
2. Start capture while another app is active.
3. Start capture while target content is full-screen.
4. Start capture on HDR and SDR monitors.
5. Start capture with multiple monitors and verify MVP behavior is scoped to the pointer/start monitor.
6. Drag and release a valid crop, then verify clipboard output and toast.
7. Press Escape before release and verify cancellation with deterministic teardown.
8. Repeat direct capture sessions to check stale frames and resource cleanup.

## 4. Recommended Approach

Use a hybrid approach:

1. Directly adjust the existing plan by adding three stories.
2. Review and narrow the MVP definition.
3. Do not rollback existing work.

Rationale:

- Existing picker, capture lifecycle, HDR preview, and overlay work are technically valuable and should remain.
- The user-facing default path must change before MVP can be considered credible as a screenshot tool.
- Bringing all of Epic 6 into MVP would create too much scope risk.
- A narrow clipboard story gives MVP a real completion moment while preserving the post-MVP boundary for HDR export, global hotkeys, tray, and annotations.

Scope classification: Moderate.

This requires backlog reorganization and new story creation, then developer implementation. It does not require a full architecture restart.

## 5. Detailed Change Proposals

### PRD Changes

#### PRD: MVP Scope

OLD:

```text
- Basic crop interaction: mouse down, drag, resize/adjust selection, confirm, cancel.
```

NEW:

```text
- Direct region screenshot flow: click Capture, enter a full-screen overlay without a picker-first step, drag a region, release to capture, copy the MVP output to the clipboard, and show lightweight completion feedback.
- Escape cancels the overlay. Explicit confirm controls are not part of the default MVP path.
```

Rationale:

The MVP must match mainstream screenshot-tool expectations and the new MVP design image.

#### PRD: Growth Features

OLD:

```text
- Copy-to-clipboard behavior with explicit HDR/SDR handling.
- Global hotkey and tray integration.
```

NEW:

```text
- Full HDR-aware export formats, advanced SDR tone-mapping controls, configurable clipboard behavior, global hotkey, tray integration, annotations, and history remain post-MVP.
- MVP includes only a narrow default clipboard output path with explicit semantics; it must not be described as HDR-preserving unless that is technically proven.
```

Rationale:

Clipboard as a completion action is now MVP-critical, but full output semantics remain larger than MVP.

### Epics and Stories Changes

#### Epic 2: Add Story 2.5

NEW:

```text
### Story 2.5: Create Monitor Capture Targets Without Picker

As a screenshot user,
I want Capture to enter region selection directly,
So that I can screenshot whatever is currently visible without first choosing a window or display.

Acceptance Criteria:

Given the user clicks Capture
When the default MVP capture path starts
Then no GraphicsCapturePicker UI appears.

Given the pointer or active capture start context maps to a monitor
When direct capture starts
Then Lumiere creates a GraphicsCaptureItem for that HMONITOR through a narrow infrastructure interop API.

Given monitor target creation fails or is unsupported
When the default capture path cannot continue
Then Lumiere reports a recoverable unsupported or failed status and may offer picker fallback outside the default MVP path.

Given the target is created through monitor interop
When CaptureTarget is created
Then its kind is Display and its size/display name are validated before WGC frame-pool startup.
```

Rationale:

This updates target acquisition to support the expected direct screenshot flow.

Design input:

- Use `/Users/asherliao/Projects/lumiere/harness/design/mvp/lumiere-mvp-design.png` to confirm that the default user experience starts from the main capture action and proceeds directly into screenshot selection, not a target picker.

#### Epic 3: Add Story 3.6

NEW:

```text
### Story 3.6: Release to Capture and Copy

As a screenshot user,
I want releasing the mouse after drawing a valid region to finish capture,
So that the screenshot flow is fast and familiar.

Acceptance Criteria:

Given the overlay is active
When the user drags a valid crop and releases the pointer
Then the overlay confirms the crop selection without requiring a Confirm button.

Given the overlay is active
When the user presses Escape before completion
Then capture is canceled and resources are torn down safely.

Given a valid crop completes
When output processing begins
Then the overlay shows lightweight progress/completion feedback without exposing a toolbar of extra actions.

Given the release-to-capture path is enabled
When the crop is too small or invalid
Then the overlay remains active or cancels according to a clearly defined MVP rule without producing output.
```

Rationale:

The overlay should behave like a screenshot tool, not a multi-step crop editor.

Design input:

- Use `/Users/asherliao/Projects/lumiere/harness/design/mvp/lumiere-mvp-design.png` as the visual and interaction reference for the simplified capture overlay: selection rectangle, optional size pill, and lightweight copied-to-clipboard feedback only.

#### Epic 6: Add MVP Story 6.0

NEW:

```text
### Story 6.0: Define and Implement MVP Clipboard Output

Status: MVP exception carved out from post-MVP output epic.

As a screenshot user,
I want my selected region copied to the clipboard after release,
So that the MVP produces a usable screenshot result.

Acceptance Criteria:

Given a confirmed crop selection
When clipboard output is produced
Then the app copies a usable bitmap representation to the Windows clipboard.

Given the output is SDR or tone-mapped
When completion feedback is shown
Then Lumiere does not claim the clipboard data is HDR-preserving.

Given the clipboard operation fails
When the user releases a valid crop
Then Lumiere reports a concise recoverable failure and does not leave capture resources active.

Given the live preview path is FP16/scRGB
When clipboard output code is added
Then it is isolated from the main live preview path and does not introduce SDR fallback into routine preview presentation.
```

Rationale:

This gives MVP a real user-visible completion while protecting the HDR preview architecture.

Design input:

- Use `/Users/asherliao/Projects/lumiere/harness/design/mvp/lumiere-mvp-design.png` to align completion feedback with the intended lightweight `Copied to clipboard` toast. Do not add extra toolbar actions or output-choice UI for MVP.

### Architecture Changes

#### Architecture: Capture Target Acquisition

OLD:

```text
CaptureService owns Windows.Graphics.Capture target selection, frame pool/session lifecycle, frame arrival, and prompt frame disposal.
```

NEW:

```text
CaptureService owns Windows.Graphics.Capture frame pool/session lifecycle, frame arrival, prompt frame disposal, and typed target contracts. Target acquisition is split into:

- Direct monitor target acquisition for the default MVP screenshot path.
- GraphicsCapturePicker-based target acquisition as fallback/debug or future explicit window/display selection.

Native HMONITOR, HWND, and IGraphicsCaptureItemInterop calls remain behind Lumiere.Infrastructure interop APIs.
```

Rationale:

This preserves module boundaries while supporting pickerless default capture.

#### Architecture: Deferred Decisions

OLD:

```text
- Clipboard format semantics.
- Global hotkey/tray architecture.
```

NEW:

```text
- Full clipboard configuration, HDR-preserving clipboard semantics, export formats, global hotkey, and tray architecture remain deferred.
- A narrow MVP clipboard output path is approved only as the release-to-capture completion action, with explicit labeling if output is SDR/tone-mapped.
```

Rationale:

The MVP now needs minimal clipboard usability, but not full output productization.

### UX Specification Changes

#### UX: MVP Design Input

NEW:

```text
The MVP implementation must reference the design board at harness/design/mvp/lumiere-mvp-design.png for the intended MVP interaction model and visual quality bar. The design board clarifies the default direct-capture flow, release-to-copy behavior, simplified overlay, and lightweight completion feedback. Settings, tray, and broader output surfaces shown in the design board remain product direction unless separate approved MVP stories include them.
```

Rationale:

The implementation plan needs a durable design input so later stories do not drift back toward picker-first capture or toolbar-heavy overlay behavior.

#### UX: Default Capture Flow

OLD:

```text
Confirm should not imply export or clipboard output in MVP.
```

NEW:

```text
The default MVP capture flow is release-to-capture. Releasing a valid region completes the capture and shows lightweight copied-to-clipboard feedback. Explicit confirmation controls are removed from the primary path. Escape remains the cancellation path.
```

Rationale:

The new MVP design and user feedback favor speed and familiarity.

#### UX: Controls

OLD:

```text
Users should always know what they can do next: select, drag, adjust, confirm, cancel, retry, or inspect diagnostics.
```

NEW:

```text
Users should always know what they can do next: drag to select, release to capture, Escape to cancel, retry on recoverable failure, or inspect diagnostics when needed. Confirm controls are not shown by default in MVP.
```

Rationale:

This reduces UI weight during the capture moment.

## 6. Implementation Handoff

### Scope Classification

Moderate.

This is not a fundamental re-architecture, but it changes MVP scope and story order. It should be handled as backlog reorganization followed by focused developer stories.

### Recommended Handoff

1. Product Owner / Developer
   - Approve this proposal.
   - Update `sprint-status.yaml`.
   - Create story files for 2.5, 3.6, and 6.0.
   - Include `/Users/asherliao/Projects/lumiere/harness/design/mvp/lumiere-mvp-design.png` in the implementation context for all three story files.
   - Mark Epic 2 and Epic 3 as `done` only after deciding whether the new stories are appended to those epics or tracked as MVP adjustment stories.

2. Developer
   - Implement Story 2.5 first.
   - Implement Story 3.6 second.
   - Implement Story 6.0 third.
   - Use the MVP design board as the visual and interaction reference while preserving native WinUI, WGC, DXGI, and HDR invariants.
   - Add tests and Windows validation docs for each story.

3. Architect / PM if needed
   - Review clipboard output semantics before Story 6.0 if HDR/SDR labeling becomes ambiguous.
   - Review direct monitor capture permission/border behavior if implementation reveals OS constraints.

### Proposed Sprint Order

1. Story 2.5: Create Monitor Capture Targets Without Picker.
2. Story 3.6: Release to Capture and Copy.
3. Story 6.0: Define and Implement MVP Clipboard Output.
4. Story 4.1: Show concise user-facing capture and preview status.
5. Story 4.4: Document direct-capture/HDR manual validation scenarios.

Post-MVP:

- Settings screens and persistence.
- Global hotkey.
- System tray menu.
- Advanced diagnostics.
- HDR export and advanced clipboard configuration.
- Annotation.
- Capture history.

## 7. Success Criteria

The route correction is successful when:

1. Clicking `Capture` does not open picker UI on the default MVP path.
2. Overlay appears directly over the current desktop/monitor.
3. User can drag a region and release to complete capture.
4. Clipboard receives a usable image result.
5. Completion feedback is lightweight and does not show extra toolbar actions.
6. Escape cancels reliably.
7. FP16/scRGB live preview invariants remain intact.
8. Clipboard output does not masquerade as HDR-preserving unless proven.
9. Windows manual validation covers HDR, SDR, full-screen app, and multi-monitor start-monitor cases.

## 8. Approval State

This proposal was approved by the user on 2026-05-07.

`sprint-status.yaml` has been updated to add the approved MVP adjustment stories as backlog entries. Canonical PRD, Architecture, UX, and Epics edits remain proposed in this document and should be applied through the next backlog/story-planning step if the team wants the source planning artifacts rewritten.

## 9. Workflow Completion

Issue addressed: the MVP capture flow needed to move from picker-first target selection to direct full-screen region capture with release-to-copy behavior.

Change scope: Moderate.

Artifacts modified:

- `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-07-mvp-direct-capture.md`
- `/Users/asherliao/Projects/lumiere/_bmad-output/implementation-artifacts/sprint-status.yaml`

Routed to: Product Owner / Developer for backlog reorganization and story creation.

Next recommended story: Story 2.5, `Create Monitor Capture Targets Without Picker`.

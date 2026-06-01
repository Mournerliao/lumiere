# Sprint Change Proposal - Settings Panel Completion

**Date:** 2026-06-01
**Author:** lumiere
**Scope Classification:** Moderate
**Status:** Approved

---

## Section 1: Issue Summary

### Problem Statement

Epic 5 and Epic 6 implemented the settings panel UI skeleton with multiple controls intentionally disabled or set to read-only, pending the implementation of underlying behavior in later epics. However, no subsequent epic created explicit stories to enable these controls, leaving approximately half of the settings panel non-functional.

### Discovery Context

The issue was discovered during a user review of the settings panel after Epic 7 (Tray, Hotkeys, and Background Capture) was completed. The review revealed that while the UI skeleton matches the v0 design spec structure, many controls remain disabled or read-only.

### Evidence

**Affected Controls:**

| Control | Current State | PRD Requirement | Writer Interface |
|---------|---------------|-----------------|------------------|
| HDR Alerts Toggle | `IsEnabled="False"` | FR13 | Exists (`IHdrAlertSettingsWriter`) |
| Timestamp Naming Toggle | `IsEnabled="False"` | FR26 | Missing |
| Save Path Display | Read-only | FR35 | Missing |
| After-Capture Toggle | `IsEnabled="False"` | FR36 | Missing |
| Shortcut Editing | Read-only | FR32, FR33 | Missing |
| Export Color Segments | All read-only | FR29 | Missing |

**PRD Coverage Gap:**

The PRD Functional Requirements Coverage Map (FR Coverage Map) assigns these requirements to Epic 5, 6, and 7, but the stories in those epics only implemented the underlying behavior without enabling the UI controls:

- FR26 (Timestamp naming): Assigned to Epic 6, Story 6.3 implemented behavior, UI toggle remains disabled
- FR32 (Shortcut configuration): Assigned to Epic 5, Story 5.3 created read-only display
- FR33 (Shortcut conflict recovery): Assigned to Epic 7, Story 7.3 implemented registration, editing UI not created
- FR35 (Save path): Assigned to Epic 5, Story 5.4 created read-only display
- FR36 (After-capture behavior): Assigned to Epic 6, Story 6.6 implemented behavior, UI toggle remains disabled
- FR29 (Export/color format): Assigned to Epic 6, Story 6.5 created read-only segments

**UX Design Spec Reference:**

The UX design specification (`ux-design.md`) defines that all settings controls should be interactive:

> Settings should be organized by user jobs: Shortcuts, HDR alerts/status behavior, Output, Clipboard, Background/tray behavior, and About. Rows should be scannable, with label, helper text when needed, and one control per row.

---

## Section 2: Impact Analysis

### Epic Impact

**Epic 8 (HDR Trust, Recovery, and Release Validation):**
- Story 8.2 already covers FR13 (HDR alerts preference), so HDR alerts toggle activation should be coordinated with Story 8.2
- Story 8.5 (Release Validation Matrix) cannot fully validate settings-dependent user journeys until settings controls are functional
- Impact: Low - Epic 8 can proceed, but validation coverage will be incomplete without Epic 9

**Epic 5, 6, 7 (Completed):**
- These epics are marked as done
- Their stories intentionally deferred UI control activation
- No rollback needed; the gap is a planning oversight, not an implementation defect

### Story Impact

**New Stories Required:** 6 stories in Epic 9

| Story | Function | Effort | Dependencies |
|-------|----------|--------|--------------|
| 9-1 | Enable HDR Alerts Toggle | Low | IHdrAlertSettingsWriter (exists) |
| 9-2 | Enable Timestamp Naming Toggle | Low | IOutputSettingsWriter extension |
| 9-3 | Enable Save Path Selection | Medium | New Writer + FolderPicker |
| 9-4 | Enable After-Capture Behavior Toggle | Medium | New Writer + dynamic UI |
| 9-5 | Enable Shortcut Editing | High | New Writer + keyboard capture UI |
| 9-6 | Enable Export Color Format Options | Medium | New Writer + validation labels |

### Artifact Conflicts

**PRD:** No conflicts. All requirements already exist in the PRD.

**Architecture:** Minor additions needed in `Lumiere.Settings` module for new Writer interfaces.

**UX Design:** No conflicts. Implementation needs to match existing design spec.

**Sprint Status:** Needs update to add Epic 9 entries.

### Technical Impact

**Module: Lumiere.Settings**
- Add new Writer interfaces: `ITimestampSettingsWriter`, `ISavePathSettingsWriter`, `IAfterCaptureSettingsWriter`, `IShortcutSettingsWriter`, `IExportColorSettingsWriter`
- Extend `DefaultSettingsProvider` to implement new interfaces
- Extend `LocalSettingsSnapshot` if new settings fields are needed

**Module: Lumiere.App**
- Update `MainWindow.xaml.cs` to enable disabled controls
- Add keyboard capture logic for shortcut editing
- Integrate `FolderPicker` for save path selection
- Update settings projection to reflect new control states

**Module: Lumiere.Infrastructure**
- May need FolderPicker wrapper for save path selection

---

## Section 3: Recommended Approach

### Selected Approach: Option 1 - Direct Adjustment

**Rationale:**

1. **PRD requirements already exist** - No scope changes needed to the product definition
2. **Infrastructure is ready** - `LocalSettingsStore`, `DefaultSettingsProvider`, and existing Writer interfaces provide the foundation
3. **Low risk** - Settings changes do not affect HDR capture pipeline, overlay, or core lifecycle
4. **MVP completeness** - Settings panel functionality is required for PRD Journey 3 (Priya configures output)
5. **Validation dependency** - Epic 8 Story 8.5 (Release Validation Matrix) needs functional settings to validate output behavior

**Effort Estimate:** Medium (6 stories, 1 high complexity, 3 medium, 2 low)

**Risk Level:** Low (settings changes are isolated from core capture path)

**Timeline Impact:** Epic 9 can be implemented in parallel with Epic 8, as they have minimal dependencies.

---

## Section 4: Detailed Change Proposals

### 4.1 Epic 9 Definition

**Add to epics.md:**

```markdown
### Epic 9: Settings Panel Completion

Users can fully configure all settings through the native settings panel, matching the v0 design spec intent. This epic completes the remaining settings controls: HDR alerts toggle, timestamp naming, save path selection, after-capture behavior, shortcut editing, and export color format options. Each control connects to the shared settings persistence and is consumed by the output pipeline, tray, hotkeys, and capture workflow.

**FRs covered:** FR13, FR26, FR29, FR32, FR33, FR35, FR36
```

### 4.2 Story 9-1: Enable HDR Alerts Toggle

**Requirements:** FR13, FR38; UX-DR10, UX-DR18

**Acceptance Criteria:**

**Given** settings are open
**When** the HDR alerts section is displayed
**Then** the HDR alerts toggle is enabled and reflects the current persisted preference.

**Given** the user toggles HDR alerts
**When** the preference is saved
**Then** the change is persisted through `IHdrAlertSettingsWriter.SetHdrAlertsEnabled()` and reflected in subsequent HDR alert behavior.

**Given** HDR alerts are disabled
**When** a non-critical HDR warning occurs
**Then** Lumiere suppresses optional alert chrome while preserving status and diagnostics.

**Given** the toggle state is projected
**When** the settings panel refreshes
**Then** the toggle visual state matches the persisted `HdrAlertsEnabled` value.

**Technical Notes:**
- Writer interface `IHdrAlertSettingsWriter` already exists
- `DefaultSettingsProvider.SetHdrAlertsEnabled()` already implemented
- Change `SettingsHdrAlertsButton.IsEnabled` from `False` to `True`
- Update toggle visual state in settings projection

**Coordination with Epic 8 Story 8.2:**

Implementation Order: Story 8.2 MUST complete before Story 9-1 begins.

Rationale:
- Story 8.2 implements the HDR alert behavior (reading `HdrAlertsEnabled` preference, showing/suppressing alerts)
- Story 9-1 activates the UI toggle (changing `IsEnabled` from `False` to `True`)
- Without 8.2's behavior implemented, the toggle has no observable effect

Handoff Contract:
- Story 8.2 delivers: Working alert show/suppress logic based on `HdrAlertsEnabled` setting
- Story 9-1 delivers: Active UI toggle that persists preference and triggers 8.2's behavior

Validation Dependency:
- Story 9-1 acceptance criteria "Given HDR alerts are disabled, When a non-critical HDR warning occurs, Then Lumiere suppresses optional alert chrome" depends on Story 8.2's suppression logic being in place

---

### 4.3 Story 9-2: Enable Timestamp Naming Toggle

**Requirements:** FR26, FR38; UX-DR15, UX-DR18

**Acceptance Criteria:**

**Given** settings are open
**When** the timestamp naming section is displayed
**Then** the timestamp toggle is enabled and reflects the current persisted preference.

**Given** the user toggles timestamp naming
**When** the preference is saved
**Then** the change is persisted through `ITimestampSettingsWriter.SetTimestampNaming()`.

**Given** timestamp naming is enabled
**When** folder output creates a file
**Then** the filename uses deterministic invariant formatting and avoids overwriting existing files.

**Given** timestamp naming is disabled
**When** folder output creates a file
**Then** the filename uses the configured or default naming policy without timestamp.

**Technical Notes:**
- Create `ITimestampSettingsWriter` interface or extend `IOutputSettingsWriter`
- Implement in `DefaultSettingsProvider`
- Change `SettingsTimestampButton.IsEnabled` from `False` to `True`
- Update `SettingsTimestampHelperText` to show current state
- Update toggle visual state in settings projection

---

### 4.4 Story 9-3: Enable Save Path Selection

**Requirements:** FR35, FR38; UX-DR13, UX-DR18

**Acceptance Criteria:**

**Given** settings are open and folder output is selected
**When** the save path section is displayed
**Then** the save path shows the current configured path with an editable selection control.

**Given** the user activates the save path control
**When** a native Windows folder picker is shown
**Then** the user can select a new folder and the path is persisted through `ISavePathSettingsWriter.SetSavePath()`.

**Given** the selected path is invalid, inaccessible, or permission denied
**When** the path is validated
**Then** the settings UI shows inline recovery guidance near the path control.

**Given** the save path is changed
**When** subsequent captures use folder output
**Then** files are saved to the new configured path.

**Technical Notes:**
- Create `ISavePathSettingsWriter` interface or extend `IOutputSettingsWriter`
- Implement in `DefaultSettingsProvider`
- Change `SettingsSavePathValuePill` from read-only to clickable
- Integrate Windows `FolderPicker` API
- Add path validation logic
- Update save path display in settings projection

---

### 4.5 Story 9-4: Enable After-Capture Behavior Toggle

**Requirements:** FR36, FR38; UX-DR14, UX-DR18

**Acceptance Criteria:**

**Given** settings are open and folder output is selected
**When** the after-capture section is displayed
**Then** the after-capture toggle is enabled and reflects the current persisted preference.

**Given** the user toggles after-capture behavior
**When** the preference is saved
**Then** the change is persisted through `IAfterCaptureSettingsWriter.SetAfterCaptureBehavior()`.

**Given** after-capture is set to "Open"
**When** folder output creates a file
**Then** Lumiere opens the saved file through Windows default application.

**Given** after-capture is set to "Reveal"
**When** folder output creates a file
**Then** Lumiere reveals the saved file in Explorer.

**Given** after-capture is set to "None"
**When** folder output completes
**Then** no additional action is taken and completion feedback is shown normally.

**Given** output target is clipboard-only
**When** after-capture behavior is evaluated
**Then** the control is disabled with helper text explaining clipboard-only has no file artifact.

**Technical Notes:**
- Create `IAfterCaptureSettingsWriter` interface or extend `IOutputSettingsWriter`
- Implement in `DefaultSettingsProvider`
- Change `SettingsOpenAfterCaptureButton.IsEnabled` from `False` to `True`
- Dynamically enable/disable based on OutputTarget
- Update toggle visual state in settings projection

---

### 4.6 Story 9-5: Enable Shortcut Editing

**Requirements:** FR32, FR33, FR38; UX-DR8, UX-DR9, UX-DR18

**Acceptance Criteria:**

**Given** settings are open
**When** the shortcuts section is displayed
**Then** fullscreen and region shortcut controls are editable with current configured values.

**Given** the user activates a shortcut control
**When** the control enters edit mode
**Then** the user can press a key combination to set the new shortcut.

**Given** the user presses an invalid key combination
**When** validation occurs
**Then** the control shows inline feedback and preserves the previous valid shortcut.

**Given** the shortcut conflicts with another registered hotkey
**When** conflict detection runs
**Then** the UI shows conflict feedback and offers recovery options.

**Given** the shortcut cannot be registered with Windows
**When** registration fails
**Then** the UI shows registration failure feedback and preserves or restores a safe shortcut state.

**Given** the shortcut is changed
**When** the preference is saved
**Then** the change is persisted through `IShortcutSettingsWriter` and the hotkey is re-registered.

**Technical Notes:**
- Create `IShortcutSettingsWriter` interface
- Implement in `DefaultSettingsProvider`
- Change `SettingsFullscreenShortcutValuePill` and `SettingsRegionShortcutValuePill` from read-only to editable
- Implement keyboard capture UI (listen for key combinations)
- Integrate with `GlobalHotkeyRegistrationPlan` for registration validation
- Add conflict detection logic
- Remove "Global registration arrives in Epic 7" helper text
- This is the most complex story in Epic 9

---

### 4.7 Story 9-6: Enable Export Color Format Options

**Requirements:** FR29, NFR9; UX-DR11, UX-DR18

**Acceptance Criteria:**

**Given** settings are open
**When** the export section is displayed
**Then** HDR10, P3, and sRGB segments are visible with clear validation-scoped labels.

**Given** the user views export options
**When** implementation semantics are incomplete for HDR10 or P3
**Then** those options are disabled or explicitly labeled as pending encoder metadata, conversion policy, and Windows validation.

**Given** sRGB is selected as the default
**When** the user views the export section
**Then** sRGB shows as active with a note that it reflects the current basic PNG output surface.

**Given** the user selects an enabled export option
**When** the preference is saved
**Then** the change is persisted through `IExportColorSettingsWriter`.

**Given** an export format becomes available in the future
**When** implementation semantics and validation are complete
**Then** the corresponding segment becomes enabled and selectable.

**Technical Notes:**
- Create `IExportColorSettingsWriter` interface
- Implement in `DefaultSettingsProvider`
- Enable `SettingsExportSrgbSegment` for selection
- Keep `SettingsExportHdr10Segment` and `SettingsExportP3Segment` disabled with validation-scoped labels
- Update segment visual states in settings projection
- Update `SettingsExportHelperText` to show current selection

---

## Section 5: Implementation Handoff

### Change Scope Classification: Moderate

This change requires:
- New Epic and Stories to be added to the backlog
- Sprint status to be updated
- Implementation to be coordinated with Epic 8

### Handoff Recipients

**Product Owner / Developer Agents:**
- Add Epic 9 to `epics.md`
- Update `sprint-status.yaml` with Epic 9 and story entries
- Implement Stories 9-1 through 9-6

### Deliverables

1. This Sprint Change Proposal document
2. Epic 9 definition for `epics.md`
3. Story definitions for implementation
4. Updated `sprint-status.yaml` entries

### Success Criteria

- All 6 settings controls are functional and interactive
- Settings changes are persisted through `LocalSettingsStore`
- Settings UI matches the v0 design spec intent
- No regression in existing capture, overlay, or output functionality
- All validation commands pass:
  ```
  dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
  dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
  dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
  dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
  ```

### Next Steps

1. Review and approve this Sprint Change Proposal
2. Update `epics.md` with Epic 9 definition
3. Update `sprint-status.yaml` with Epic 9 entries
4. Implement Epic 8 Story 8.2 first (HDR alert behavior)
5. Then begin Story 9-1 (activate HDR alerts toggle, depends on 8.2)
6. Proceed with remaining Epic 9 stories (9-2 through 9-6)

---

## Appendix: Sprint Status Updates

### Epic 9 Entries for sprint-status.yaml

```yaml
  epic-9: backlog
  9-1-enable-hdr-alerts-toggle: backlog
  9-2-enable-timestamp-naming-toggle: backlog
  9-3-enable-save-path-selection: backlog
  9-4-enable-after-capture-behavior-toggle: backlog
  9-5-enable-shortcut-editing: backlog
  9-6-enable-export-color-format-options: backlog
  epic-9-retrospective: optional
```

---

**Document Status:** Approved
**Next Action:** Update epics.md and sprint-status.yaml, then begin implementation.

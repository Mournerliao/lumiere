# Public Fidelity Progress Alignment - 2026-06-22

This note aligns `sprint-status.yaml` with the code and validation artifacts that landed after the 2026-06-21 Perfect HDR Fidelity course correction. The BMad status file had remained on the planning view where Epics 10-13 were backlog, while implementation commits already advanced several stories.

## Alignment Rules

- `done` means the code/documentation support for the story acceptance criteria exists and is covered by automated tests or committed release-gate documentation.
- `in-progress` means implementation support exists, but public-release completion still depends on Windows manual validation, target-app evidence, or long-run evidence.
- `backlog` means there is no focused implementation or validation evidence yet beyond earlier MVP/private-preview groundwork.

## Epic 10: Target-Aware HDR Detection and Trust Mapping

### 10-1 Map Capture Targets to Display Output Identity - done

Evidence:

- `DisplayOutputIdentity` exists in `Lumiere.Capture`.
- Direct monitor target selection carries device name and display bounds through typed contracts.
- Tests cover capture target display identity and direct monitor target evidence.

Remaining release work is tracked under 10-3, not 10-1.

### 10-2 Probe HDR Capability for the Active Capture Target - done

Evidence:

- `HdrDisplayCapability` supports target-aware selection by display name, desktop bounds, or unambiguous size.
- `SwapChainManager` and readiness mapping surface target match evidence.
- Ambiguous or unresolved target evidence is degraded/unvalidated instead of becoming `HDR Ready`.
- Tests cover target-aware HDR probe selection, ambiguous matches, readiness, and projection behavior.

Remaining release work is hardware validation, tracked under 10-3.

### 10-3 Validate Mixed HDR/SDR and Multi-Monitor Trust States - in-progress

Evidence:

- Code can represent target-aware match evidence and unresolved target states.
- Settings, main panel, tray, overlay, and validation projections expose target-aware evidence.

Remaining blockers:

- Real Windows mixed HDR/SDR and multi-monitor validation must be recorded before this can be `done`.
- `docs/validation/release-validation-checklist.md` still has the public target-aware HDR gate pending evidence.

## Epic 11: HDR Output Semantics and Format Pipeline

### 11-1 Define the HDR Fidelity Contract - done

Evidence:

- `OutputProfileContract` distinguishes SDR-compatible, visual-match, HDR-preserved, and unvalidated fidelity modes.
- Output result, main panel, overlay, tray, and settings projections avoid collapsing artifact success into HDR preservation.
- Public release docs define that copied/saved/converted/HDR-preserved are separate claims.

### 11-2 Define and Implement the First Supported Output Profile - in-progress

Evidence:

- Runtime output profile capabilities gate unsupported profile claims.
- Folder output routes through artifact encoders.
- HDR10 JXR codec seams, WIC JPEG XR adapter, FP16 source readback, audit metadata write/read, and codec readiness blockers exist.
- `Hdr10JxrCodecReadiness` prevents enabling HDR10 output unless metadata policy and codec gates are satisfied.

Remaining blockers:

- Runtime HDR10 export remains disabled until viewer-recognized HDR10 metadata and Windows manual viewer validation pass.
- A supported HDR-preserved output profile is not yet a public-release path.

### 11-3 Validate Target-App Compatibility for Supported Output - in-progress

Evidence:

- Viewer compatibility evidence is modeled separately for artifact handling, visual match, HDR preservation, and HDR10 metadata recognition.
- Output validation artifacts can apply named viewer evidence to output contracts.
- `Hdr10JxrViewerValidationEvidence` evaluates whether loaded manual artifacts satisfy JXR viewer-facing gates.

Remaining blockers:

- Real target-app/viewer validation artifacts must be recorded with Windows manual evidence before this can be `done`.

## Epic 12: HDR Fidelity Validation Suite and Public Release Evidence

### 12-1 Establish Standard HDR/SDR Validation Content and Scenarios - in-progress

Evidence:

- Public release checklist defines the required HDR/SDR, mixed-display, target-app, DPI, and output evidence categories.
- Output validation artifact schema/template defines required session evidence fields.

Remaining blockers:

- Actual standard test content and executed validation sessions are still missing.

### 12-2 Expand the Release Checklist into a Public Fidelity Gate - done

Evidence:

- `docs/validation/release-validation-checklist.md` separates Private Preview / Early Validation from Perfect HDR Fidelity Public Release gates.
- Output validation docs and schema template explain how manual evidence is recorded and why invalid artifacts are surfaced.

### 12-3 Record Long-Run Capture and Output Resource Trends - backlog

Evidence:

- No focused 50+ or 100+ cycle resource trend evidence has been recorded yet.

## Epic 13: Fidelity Confidence UX and Accessibility Hardening

### 13-1 Clarify Fidelity State Copy Across Main, Tray, Overlay, and Output - done

Evidence:

- Main panel, tray, overlay, settings, and output result projections distinguish artifact completion from fidelity claims.
- Tests assert that unvalidated paths do not claim HDR-preserved behavior.
- Validation panel wording keeps public release claims behind evidence gates.

### 13-2 Harden Native Settings and Accessibility Semantics - backlog

Evidence:

- Accessibility gaps remain tracked as future work in validation and deferred-work artifacts.
- No focused public-release accessibility validation story has been completed.

## Resulting Sprint Status

After this alignment:

- Epic 10: `in-progress`
- Epic 11: `in-progress`
- Epic 12: `in-progress`
- Epic 13: `in-progress`

The project has moved beyond backlog for the new public-fidelity direction, but public release remains blocked by Windows manual validation evidence, supported HDR-preserved output validation, target-app compatibility, long-run resource trends, and accessibility hardening.

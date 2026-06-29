# 12-1 Surface Markdown Evidence Repair Guidance

Date: 2026-06-29
Stories: 11-3, 12-1, 13-2
Status: implemented; NOT RUN on macOS; pending Windows validation

## Summary

This slice makes workspace-local scenario markdown load issues actionable without weakening the runtime evidence gate. When an output-validation JSON artifact points at a markdown evidence file that is still a draft, the loader now reports the concrete repair steps required before that artifact can count as loaded evidence.

The immediate release-risk case is a Windows validator creating a draft, reloading evidence, and only seeing a generic `Workspace-local markdown evidence is incomplete` message. The artifact was correctly blocked, but the repair path was under-specified. The ignored-file warning now says whether the validator needs to replace `REPLACE_WITH_*` placeholders, remove the generated draft sentinel, replace template-only language, or choose one observed status per scenario row.

## Code Changes

- `FileOutputValidationArtifactSource` now builds markdown evidence repair guidance from the actual incomplete markers in the workspace-local markdown file.
- One markdown file can surface multiple repair steps in a single load issue detail.
- The existing `OutputValidationArtifactLoadIssue` model remains the delivery mechanism; no parallel UI state or new status string model was added.
- Settings > Validation continues to surface ignored-file details through the existing validation record projection, and the record now refers to ignored artifact or evidence files instead of only JSON/schema files.
- No capture, preview, output encoder, HDR10 execution, or SDR fallback behavior changed.

## Tests Written

- `OutputValidationArtifactSourceTests.Load_WhenWorkspacePrepared_RejectsIncompleteWorkspaceLocalMarkdownEvidence`
  - now verifies each incomplete marker produces the expected repair guidance.
- `OutputValidationArtifactSourceTests.Load_WhenWorkspacePrepared_ReportsEveryIncompleteMarkdownEvidenceFix`
  - verifies multiple incomplete markers are reported together.
- `OutputValidationArtifactSourceTests.Load_WhenWorkspacePrepared_RejectsGeneratedScenarioDraftUntilObservedResultsAreRecorded`
  - now verifies generated draft notes surface the draft-sentinel and unresolved-result repair steps.
- `OutputValidationArtifactSourceTests.LoadedSnapshotSurfacesSpecificMarkdownEvidenceFixInSettingsValidationRecord`
  - verifies the existing Settings validation record exposes the specific markdown repair guidance.
- `OutputValidationDocumentationTests.OutputValidationDocs_RecordFutureFormatAcceptanceFields`
  - verifies the durable output-validation docs mention specific repair guidance.

## Validation Status

NOT RUN in this macOS environment:

- .NET restore/build/test/format
- WinUI Settings > Validation rendering
- Windows local validation workspace creation
- Windows manual HDR/SDR scenario execution
- HDR10 JXR file output
- Target-app compatibility validation
- Keyboard, screen reader, high contrast, and DPI validation

## Release-Gate Impact

This does not complete Story `12-1`, Story `11-3`, Story `13-2`, or any public release gate. It improves the evidence repair loop while keeping Public perfect-HDR-fidelity blocked until real Windows manual validation artifacts exist.

Public perfect-HDR-fidelity remains blocked on target-aware HDR display evidence, observed target color space, filled workspace-local scenario notes, output profile contract proof, target-app versions, viewer-recognized HDR10 metadata, mixed HDR/SDR topology, DPI/accessibility validation, and long-run resource trend evidence.

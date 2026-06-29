# 12-1 Distinguish Rejected Evidence In Validation Summary

Date: 2026-06-29
Stories: 11-3, 12-1, 13-2
Status: implemented; NOT RUN on macOS; pending Windows validation

## Summary

This slice makes Settings > Validation distinguish a truly empty validation workspace from a workspace where validation files exist but were rejected by the loader.

Before this change, a load-issue-only snapshot could look too close to the default "no artifact loaded" state. That was technically safe because runtime gates stayed blocked, but it was not precise enough for a Windows validator trying to repair a generated draft. The loaded-evidence summary now says that validation artifact or evidence files were found but none loaded, that ignored files do not count as Windows manual evidence, and that runtime gates stay blocked until the rejected files are fixed and reloaded.

## Code Changes

- `PerfectHdrFidelityProjection.ProjectValidationEvidenceSummary(...)` now has an explicit rejected-evidence summary path for `Artifacts.Count == 0 && LoadIssues.Count > 0`.
- Rejected-evidence summaries keep status `NotRun`, do not expose an open-latest-artifact path, and keep target-app/build evidence empty.
- The summary and gap detail include the first ignored issue so validator repair guidance from the loader stays visible in Settings.
- No runtime output execution path changed. HDR10 remains blocked until complete Windows manual evidence exists.

## Tests Written

- `PerfectHdrFidelityProjectionTests.ProjectValidationEvidenceSummary_WithOnlyLoadIssuesDistinguishesRejectedEvidenceFromEmptyWorkspace`
  - verifies rejected evidence is distinct from an empty workspace and stays `NotRun`.
- `SettingsPanelProjectionTests.Project_SnapshotValidationSummaryDistinguishesRejectedDraftEvidenceFromMissingEvidence`
  - verifies Settings surfaces the rejected-evidence state, preserves loader repair guidance, keeps `CanOpenLatestArtifact` false, and leaves HDR10 at `Build` / converted fallback.
- `OutputValidationDocumentationTests.OutputValidationDocs_RecordFutureFormatAcceptanceFields`
  - now verifies the durable output-validation docs mention the distinct rejected-evidence state.

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

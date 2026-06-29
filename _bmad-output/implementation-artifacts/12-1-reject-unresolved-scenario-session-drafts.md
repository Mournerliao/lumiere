# 12-1 Reject Unresolved Scenario Session Drafts

Date: 2026-06-29
Stories: 11-3, 12-1, 13-2
Status: implemented; NOT RUN on macOS; pending Windows validation

## Summary

This slice closes the remaining gap where a workspace-local markdown scenario template could be linked from an output-validation JSON artifact without containing real observed scenario results.

Before this change, the output-validation loader rejected empty markdown, `REPLACE_WITH_*`, and `Template only` text. The raw Story `12-1` scenario template, however, could still carry unresolved result-choice rows such as `PASS / PASS with limitation / FAIL / NOT RUN` without those exact markers. That weakened the evidence workflow: a generated or copied scenario note could look present while still being a draft.

## Code Changes

- `FileOutputValidationArtifactSource` now rejects workspace-local markdown evidence that still contains the generated `Draft status: NOT RUN until...` sentinel.
- The loader also rejects unresolved scenario result-choice rows that still contain `PASS / PASS with limitation / FAIL / NOT RUN`.
- The embedded and durable HDR/SDR validation session templates now include the explicit draft sentinel near the top of the markdown file.
- `Create draft` continues to write the companion scenario-session markdown under `evidence\`, but the untouched companion file remains load-blocking until a Windows validator replaces the draft sentinel and result-choice placeholders with observed results.
- No capture, preview, output encoder, HDR10 execution, or SDR fallback behavior changed.

## Tests Written

- `OutputValidationArtifactSourceTests.Load_WhenWorkspacePrepared_RejectsIncompleteWorkspaceLocalMarkdownEvidence`
  - extended with the draft sentinel and unresolved result-choice cases.
- `OutputValidationArtifactSourceTests.Load_WhenWorkspacePrepared_RejectsGeneratedScenarioDraftUntilObservedResultsAreRecorded`
  - proves an untouched `Create draft` JSON plus companion markdown remains rejected by the app loader.
- `OutputValidationArtifactSourceTests.CreateDraft_WritesPrefilledDraftIntoWorkspaceRoot`
  - now verifies generated scenario notes carry the draft sentinel.
- `OutputValidationDocumentationTests.OutputValidationDocs_RecordFutureFormatAcceptanceFields`
  - now verifies the loader contract documents the draft sentinel.
- `OutputValidationDocumentationTests.HdrSdrValidationSessionTemplate_CarriesDraftSentinel`
  - verifies the durable harness template carries the draft sentinel and unresolved result-choice text.

## Validation Status

NOT RUN in this macOS environment:

- .NET restore/build/test/format
- WinUI Settings > Validation rendering
- Windows local validation workspace creation
- Windows manual HDR/SDR scenario execution
- HDR10 JXR file output
- Target-app compatibility validation

## Release-Gate Impact

This does not complete Story `12-1` or any public release gate. It prevents unresolved scenario-session drafts from being counted as loaded evidence and keeps Public perfect-HDR-fidelity blocked until real Windows manual validation artifacts exist.

Public perfect-HDR-fidelity remains blocked on target-aware HDR display evidence, observed target color space, filled workspace-local scenario notes, output profile contract proof, target-app versions, viewer-recognized HDR10 metadata, mixed HDR/SDR topology, DPI/accessibility validation, and long-run resource trend evidence.

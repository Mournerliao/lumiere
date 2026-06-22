---
title: 'Carry Output Target Scope Into Viewer Rows'
type: 'feature'
created: '2026-06-23'
status: 'done'
route: 'native-validation-surface'
story: '11-3'
---

# Carry Output Target Scope Into Viewer Rows

## Intent

The validation surface already modeled output-target coverage at the session and record level, and runtime gating already respected that narrower scope. But the per-viewer compatibility rows still left reviewers to infer whether a named viewer result proved `Folder`, `Clipboard`, or `Both`.

This slice keeps the validation model honest and easier to review: each viewer row now states the output target scope it actually covers, including when the row falls back to session-level scope because record-level target coverage has not been recorded yet.

## Delivered In This Slice

1. `PerfectHdrFidelityProjection` now carries output-validation artifacts into viewer-row projection so each row can describe the target scope that backs the named viewer evidence.
2. Viewer-row detail now prefixes output target scope before target-app version context and narrative guidance.
3. Record-level `outputTargetsCovered` now wins when present, preventing mixed sessions from over-claiming that a viewer result proves more than the specific validated target.
4. Session-level `outputTargetsTested` now remains the fallback when older or broader artifacts do not yet record per-profile scope, and that fallback is explicitly labeled as session-level.
5. Scope text normalizes legacy `File` wording to `Folder` so reviewer-facing copy matches the current product vocabulary.
6. Tests now cover both:
   - record-level scope appearing inline as `Output target scope: Folder.`
   - session-level fallback appearing inline as `Output target scope: Folder (session-level).`

## Review Pointers

1. [PerfectHdrFidelityProjection.cs](../../src/Lumiere.App.Core/PerfectHdrFidelityProjection.cs)
2. [PerfectHdrFidelityProjectionTests.cs](../../tests/Lumiere.Graphics.Tests/App/PerfectHdrFidelityProjectionTests.cs)
3. [perfect-hdr-fidelity-extension.md](../../harness/design/perfect-hdr-fidelity-extension.md)

## Validation

- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --filter "PerfectHdrFidelityProjectionTests|SettingsPanelProjectionTests" --no-restore --verbosity minimal /nr:false`
- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 --verbosity minimal`

## Remaining Work

Story `11-3` remains `in-progress`.

Remaining release-blocking work:

- Record real Windows manual target-app validation artifacts for the supported output path.
- Fill the actual viewer compatibility matrix with named app versions and viewer-specific validation outcomes.
- Prove supported output targets with real evidence instead of only improving the projection and review seams.

---
title: 'Carry Target App Version Evidence Into Viewer Rows'
type: 'feature'
created: '2026-06-23'
status: 'in-progress'
route: 'native-validation-surface'
story: '11-3'
---

# Carry Target App Version Evidence Into Viewer Rows

## Intent

The validation surface already had a dedicated `Target app versions` evidence row, and runtime HDR10 gating already treated missing app-version records as incomplete release evidence. But the per-viewer compatibility rows still stopped short of carrying that same version context inline. That meant a reviewer could see a viewer row and still need to cross-reference a different summary row to understand whether the named app version itself was part of the blocker.

This slice keeps the current evidence model intact while making the viewer rows more release-accurate: each named viewer now carries its own target-app version evidence state and detail, so target-app compatibility review is clearer without relying on cross-row inference.

## Delivered In This Slice

1. `PerfectHdrFidelityProjection` now builds viewer rows with target-app version evidence derived from the loaded validation artifacts instead of treating version evidence as summary-only context.
2. Each viewer row now carries:
   - category evidence status for artifact handling, visual match, HDR preservation, and HDR10 metadata
   - target-app version evidence status for the same viewer
   - recorded version detail when available, or a precise missing-version explanation when blocked
3. Viewer-row aggregate status now stays aligned with inline target-app version evidence for artifact-backed validation sessions.
4. Contract-only viewer projections remain readable without being falsely downgraded when no loaded validation artifact is backing the row yet.
5. Tests now cover:
   - target-app version status in viewer rows
   - recorded-version detail when artifact evidence exists
   - missing-version detail when a named viewer still lacks a concrete app version

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

- Record real Windows manual viewer/app validation artifacts for the supported output path.
- Record concrete app versions for every named viewer in those sessions.
- Finish the actual target-app compatibility matrix instead of only improving review surfaces around it.

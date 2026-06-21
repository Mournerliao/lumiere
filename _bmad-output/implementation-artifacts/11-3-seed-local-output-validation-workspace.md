# 11-3 Seed Local Output Validation Workspace

Date: 2026-06-22
Story focus: `11-3-validate-target-app-compatibility-for-supported-output`

## Context

The HDR10 JXR runtime gate now depends on real Windows manual validation artifacts, but local validation still had too much setup friction:

- testers had to discover `%LOCALAPPDATA%\Lumiere\validation\output\` manually
- the schema sample only existed in repo docs, not in the local runtime workspace
- the app could report loaded or ignored artifacts, but it did not tell the tester where the local workspace and seed template were on the current machine

That made the release gate model cleaner than the actual Windows validation workflow.

## What changed

1. Deepened `FileOutputValidationArtifactSource` so the existing load seam now also prepares the local validation workspace when the real app uses the default source.
2. Seeded the local workspace with:
   - `README.txt`
   - `templates\\output-validation-session.schema-v4.sample.json`
   - `evidence\\`
3. Embedded the schema-v4 sample into `Lumiere.App.Core` so the running app can seed the local template without depending on source-tree file layout.
4. Extended `OutputValidationArtifactSnapshot` with typed workspace state so validation projections can surface workspace readiness separately from artifact evidence.
5. Updated `PerfectHdrFidelityProjection.ProjectValidationRecord(...)` so the settings validation panel now reports:
   - the local validation workspace path
   - the seeded sample-template path when available
   - workspace-setup failures without implying that Windows manual validation passed
6. Updated `harness/validation/output-validation.md` so the documented workflow matches the new runtime behavior.

## Validation

- `dotnet build src/Lumiere.App.Core/Lumiere.App.Core.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false --filter "FullyQualifiedName~OutputValidationArtifactSourceTests|FullyQualifiedName~PerfectHdrFidelityProjectionTests|FullyQualifiedName~SettingsPanelProjectionTests|FullyQualifiedName~OutputValidationDocumentationTests"`

Result:

- Build passed.
- Targeted tests passed: `91 passed`.

## Follow-up

- This does not count as Windows manual evidence by itself.
- The next Windows validation session should use the seeded sample to record real `Folder`, `Clipboard`, and `Both` observations with viewer-specific HDR10 metadata recognition.

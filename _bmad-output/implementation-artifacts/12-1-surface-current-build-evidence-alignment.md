# 12-1 Surface Current-Build Evidence Alignment

Date: 2026-06-22
Stories touched: `12-1`, `11-3`, `13-2`

## Why this slice

The settings validation surface could already show what evidence was loaded, but it still left one release-critical question implicit:

`Does this evidence actually belong to the current build?`

For `Public perfect-HDR-fidelity`, reusing old evidence without making that mismatch visible is too risky. A validator needs to see whether the loaded artifact:

- matches the current build
- is stale for the current build
- cannot yet be aligned to a comparable build token

without leaving the native validation surface or manually diffing JSON fields.

## What changed

1. Deepened `ValidationEvidenceSummaryProjection` with a typed build-alignment payload instead of scattering build-staleness checks into `MainWindow`.
2. Added a dedicated `Current build evidence` validation row so build alignment is visible alongside target-aware HDR, visual match, HDR-preserved profile, and target-app matrix evidence.
3. Kept the comparison strict:
   - `Pass` only when the current app build exposes a comparable commit token and the latest loaded artifact records the same token
   - `Limited` when the latest artifact records a different token
   - `Limited` with explicit unknown wording when the build or artifact cannot provide a comparable token
4. Updated the validation record wording so stale evidence is called out directly in the manual-validation summary rather than being buried in a later release review.
5. Preserved display hygiene:
   - About/version UI still shows the clean user-facing version label
   - validation alignment uses the raw provider build version so `2.3.4+abcdef` style informational versions can still participate in evidence matching
6. Added a small projection helper so snapshot-backed validation rows and summary text stay aligned instead of drifting through separate `with` overrides.

## Validation

- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false /m:1`
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false --filter "FullyQualifiedName~PerfectHdrFidelityProjectionTests|FullyQualifiedName~SettingsPanelProjectionTests"`

Result:

- Build passed.
- Targeted tests passed: `91 passed`.

## Notes

- This slice does not fabricate freshness. If Lumiere cannot prove build alignment, it keeps the state limited.
- This is still evidence-review plumbing, not release-gate completion. Real Windows manual evidence for the supported HDR path remains the blocker.

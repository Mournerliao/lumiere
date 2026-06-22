# 12-1 Surface Target-App Version Evidence Row

## Context

After HDR10 JXR runtime gating began requiring target-app version evidence, the Validation panel still only mentioned missing versions indirectly through the loaded-evidence summary text. That left a review gap: runtime and artifact-completeness rules had tightened, but the formal evidence rows did not yet show that requirement explicitly.

## What Changed

1. `PerfectHdrFidelityProjection` now projects `Target app versions` as a dedicated validation evidence row.
2. The loaded-evidence summary and the formal validation row now share the same missing-version calculation instead of drifting apart.
3. Settings validation tests and projection tests now cover:
   - `NotRun` when no evidence is loaded
   - `Pass` when all named viewers/apps have concrete recorded versions
   - `Limited` when any named viewer/app is missing a recorded version

## Why This Matters

- The validator can now see target-app version completeness in the same evidence ladder as target-aware HDR, visual match, HDR-preserved profile, target-app matrix, and current-build alignment.
- Review UI, artifact completeness, and runtime release gating now describe the same standard.
- This reduces the chance that a partially recorded manual session looks “almost complete” in the UI while still being blocked by the actual runtime gate.

## Validation

- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --filter "FullyQualifiedName~PerfectHdrFidelityProjectionTests|FullyQualifiedName~SettingsPanelProjectionTests" --verbosity minimal /nr:false`

## Status

Story `12-1` remains `in-progress`: the review surface is clearer, but real Windows manual fidelity sessions still need to be executed and recorded.

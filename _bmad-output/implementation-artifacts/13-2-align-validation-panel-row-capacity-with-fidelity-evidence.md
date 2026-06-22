# 13-2 Align Validation Panel Row Capacity With Fidelity Evidence

## Context

The Public perfect-HDR-fidelity validation surface had already evolved to project six evidence rows:

1. Target-aware HDR
2. Visual-match output
3. HDR-preserved profile
4. Target app matrix
5. Target app versions
6. Current build evidence

But the WinUI validation panel still rendered only five row slots. That meant one projected evidence row was silently missing from the actual desktop UI even though tests and projection logic already assumed the fuller evidence model.

For a fidelity-gated release surface, that mismatch is not cosmetic. It undermines the promise that validation state shown in-app matches the real release-evidence model.

## What Changed

1. Added a sixth validation evidence row to the settings validation panel XAML.
2. Updated `MainWindow.ApplyValidationProjection(...)` to bind the sixth projected row instead of truncating the projection at five rows.
3. Strengthened `SettingsPanelProjectionTests` so the validation projection must keep all six rows, including both:
   - `Target app versions`
   - `Current build evidence`

## Why This Matters

- The public-fidelity validation surface is now structurally aligned with the projection and documentation model it already claimed to expose.
- Validators no longer depend on hidden projection state to see one of the key release-gate rows.
- This is directly aligned with Story `13-2`: native settings semantics and validation review surfaces must stay trustworthy, legible, and complete.

## Validation

- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --filter "FullyQualifiedName~SettingsPanelProjectionTests|FullyQualifiedName~PerfectHdrFidelityProjectionTests" --verbosity minimal /nr:false -p:UseSharedCompilation=false`
- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false /m:1 -p:UseSharedCompilation=false`

## Status

Story `13-2` remains `in-progress`: the validation panel now exposes its full projected evidence row set, but public release still depends on real Windows manual accessibility, mixed-monitor, target-app, and long-run validation evidence.

# 10-3 Surface Active Target Context In Validation Row

## Context

`Settings > Validation` already exposed a `Target-aware HDR` row, but the row still forced the validator to infer which active runtime target the row was talking about. In mixed HDR/SDR and multi-monitor work, that missing context adds avoidable ambiguity at exactly the point where Public perfect-HDR-fidelity needs the UI to be most explicit.

This was not a request to treat runtime target context as proof. The requirement was narrower: the validation row should name the current active target/display context directly while keeping manual evidence and runtime hints clearly separated.

## What Changed

1. Extended `PerfectHdrFidelityProjection.ProjectValidation(...)` with an optional `CaptureTarget` context seam.
2. `SettingsPanelProjection` now passes the current `CaptureSessionState.Target` into the validation projection.
3. `Target-aware HDR` row copy now appends explicit runtime context for:
   - display targets with display identity and desktop bounds
   - display targets that still lack recorded display identity
   - window targets that still depend on display mapping
   - unresolved targets
4. Existing artifact/manual evidence behavior is preserved:
   - artifact-based target HDR evidence still stays `Limited`, not `Pass`
   - runtime target text is presented as `Current runtime target: ...`
   - no new wording implies that runtime context alone satisfies Windows manual validation
5. Added focused projection tests covering:
   - unresolved target-display mapping with named runtime target context
   - matched runtime display context with explicit display name
   - artifact-backed target HDR evidence plus current runtime context
   - display identity with desktop bounds
   - window-target runtime context that still depends on display mapping

## Why This Matters

- Story `10-3` is about making mixed-monitor trust states reviewable and honest on real hardware. The validation surface now says which target the current runtime state refers to instead of leaving that implicit.
- This also reinforces Story `13-2`: native validation UI should be complete and legible, not technically correct only in underlying projection objects.
- The seam stays deep and narrow: the UI passes one typed `CaptureTarget`, and the projection layer remains the single place that formats validation-surface wording.

## Validation

- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --filter "FullyQualifiedName~SettingsPanelProjectionTests|FullyQualifiedName~PerfectHdrFidelityProjectionTests" --verbosity minimal /nr:false -p:UseSharedCompilation=false`
- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false /m:1 -p:UseSharedCompilation=false`

## Status

Story `10-3` remains `in-progress`: the validation surface now identifies the active runtime target more clearly, but Public perfect-HDR-fidelity still needs recorded Windows manual evidence across mixed HDR/SDR and multi-monitor scenarios before the story can move to `done`.

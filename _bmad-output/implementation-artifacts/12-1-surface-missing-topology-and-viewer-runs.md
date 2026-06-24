# 12-1 Surface Missing Topology And Viewer Runs

Date: 2026-06-24
Status: done

## Summary

Settings > Validation loaded-evidence summary now turns loaded output validation artifacts into a more actionable next-run plan for Public perfect-HDR-fidelity validation.

## Implementation Evidence

- `PerfectHdrFidelityProjection` now summarizes covered display topology buckets from loaded artifacts.
- The same summary calls out missing display topology buckets from the standard HDR/SDR scenario guide.
- The same summary calls out missing HDR10 named viewer targets from the profile-specific output validation records.
- The next-run text now combines the next uncovered entry point, topology bucket, output target, and HDR10 viewer target set into one concrete Windows manual run suggestion.
- Placeholder/template values are filtered out before they count as coverage.

## Documentation Evidence

- `harness/validation/hdr-sdr-validation-scenarios.md` now tells validators to record the Display Topology Matrix bucket in `displaySetup`.
- `harness/validation/output-validation.md` documents the new loaded-evidence summary fields.
- The repo and embedded output-validation sample JSON templates now keep `displaySetup` as a manual replacement field and explain how topology labels feed the summary.

## Validation

- Covered by focused `PerfectHdrFidelityProjectionTests` assertions for topology coverage, missing topology gaps, missing HDR10 viewer targets, and next Windows run guidance.

## Remaining Work

- This does not replace real Windows manual validation. Epic 12 remains blocked until current-build evidence files record the actual topology, viewer, DPI, output target, and app-version observations.

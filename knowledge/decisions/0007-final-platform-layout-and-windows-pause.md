# 0007: Final Platform Layout And Windows Pause

Date: 2026-08-20

## Decision

Lumiere adopts `apps/`, `protocol/`, `hosts/`, and `knowledge/` as its final top-level
ownership layout. The shared Electron application is the only product shell. Native
hosts communicate through the language-neutral, versioned JSON Lines protocol in
`protocol/platform-host`.

The WinUI application, App.Core projections, overlay/settings UI, embedded validation
workflow, and experimental HDR10/JXR output are removed. Windows development pauses
with only three buildable libraries: Capture, Graphics, and Interop. No Windows
executable remains until a platform-host adapter is implemented.

Release-validation templates are removed rather than archived. Current checks live in
CI, GitHub Issues, runbooks, and the current-state handoff. Formal hardware-validation
artifacts will be redesigned when the release-validation phase begins.

## Consequences

- macOS can own the normal shell feedback loop without implying native capture exists.
- Windows code has one future role: satisfy the platform-host seam behind a small
  interface; it no longer maintains a parallel UI or settings model.
- The official output implementation is sRGB Visual Match PNG. HDR-preserved export
  and cross-platform HDR fidelity remain later milestones, not dormant runtime modes.
- Git history is the recovery path for deleted WinUI and validation-era material.
- ADR 0005 is superseded by the Contract → Frontier → Verification workflow recorded here.

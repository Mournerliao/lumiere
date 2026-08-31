# 0008: Environment-Aware Execution Lanes

Date: 2026-08-25

## Decision

Lumiere keeps milestone gates ordered while allowing the shared, macOS, and Windows
execution lanes inside the active milestone to advance independently when their Issue
dependencies and verification scope are explicit. The current machine determines
which lane is eligible: shared work may run on either supported platform, macOS-native
work requires macOS, and Windows-native work requires Windows.

Multiple lane frontiers may exist, but one writer or worktree advances only one current
working Issue at a time. Switching lanes or machines requires a clean, structured
handoff. Repository, platform, and hardware truth remain separate, and progress in one
lane never closes another lane's acceptance criteria or the shared milestone gate.
Platform-owned implementation in a later slice of the active milestone may therefore
advance when its own dependencies and verification scope are explicit, even while an
earlier slice still awaits independent verification in another lane. Only the shared
completion or release gate waits for every required lane.

## Context

Development regularly alternates between a macOS work environment and a Windows home
environment. Strictly serializing the Windows Host adapter before all shared product
surface work leaves usable development time idle even though Electron/React, protocol,
and macOS-owned slices can be implemented and verified independently. Unrestricted
parallel work would create the opposite problem by obscuring dependencies, overlapping
writers, and projecting one platform's evidence to another.

## Consequences

- GitHub Issues remain the executable control plane and declare cross-lane dependencies.
- `CURRENT.md` records one concrete next action for each active execution lane rather
  than one global implementation frontier.
- Shared product work may overlap the Windows Host adapter, but its Windows journeys
  cannot pass until the Windows lane supplies and verifies the required Host behavior.
- Milestone 1D macOS and Windows implementation lanes may advance independently when
  their owning Issues are acceptance-ready; incomplete verification in one platform
  does not idle eligible work on the other.
- Cross-platform release verification and the Milestone 1 exit gate remain blocked until
  both native lanes and the shared product journeys pass their independent criteria.

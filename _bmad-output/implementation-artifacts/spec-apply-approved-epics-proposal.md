---
title: 'Apply Approved Epics Proposal'
type: 'chore'
created: '2026-04-21'
status: 'done'
route: 'one-shot'
context:
  - '_bmad-output/planning-artifacts/archive/2026-05-cleanup/sprint-change-proposal-2026-04-21.md'
supersededBy:
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-07-canonical-mvp-1-0-rebaseline.md'
  - '_bmad-output/planning-artifacts/epics.md'
---

# Apply Approved Epics Proposal

> Superseded note (2026-05-07): this file records the completed 2026-04-21 one-shot application of an older epics proposal. It is historical execution evidence, not the active implementation route. Current sprint planning should use the canonical MVP-to-1.0 rebaseline in `_bmad-output/planning-artifacts/epics.md`, with Epic 1-4 as MVP feature implementation, Epic 5 as the MVP completion gate, and Epic 6 as installer/1.0 release.

## Intent

**Problem:** `epics.md` contained approved readiness corrections that had not yet been applied, leaving sprint planning at risk of using stale UX guidance and ambiguous MVP/post-MVP boundaries.

**Approach:** Apply the approved Sprint Change Proposal directly to `epics.md`, preserving PRD, architecture, and UX documents unchanged.

## Suggested Review Order

Historical review order for the 2026-04-21 change only:

- [epics.md UX requirements](../planning-artifacts/epics.md:193) -- verify the standalone UX specification is now referenced as an implementation input.
- [epics.md FR37 mapping](../planning-artifacts/epics.md:273) -- confirm FR37 is marked Deferred/Post-MVP instead of Epic 5 implementation coverage.
- [epics.md MVP readiness note](../planning-artifacts/epics.md:285) -- confirm sprint planning is directed to Epics 1-5 and excludes Epic 6 from MVP.
- [epics.md Epic 5](../planning-artifacts/epics.md:323) -- confirm Epic 5 covers FR36 and FR38, with FR37 only referenced as deferred.
- [epics.md Story 5.2](../planning-artifacts/epics.md:789) -- confirm diagnostics visibility is the only implemented requirement and cursor capture is omitted from MVP preferences.
- [epics.md Epic 6 stories](../planning-artifacts/epics.md:807) -- confirm each 6.x story is explicitly marked post-MVP or blocked by future semantics.

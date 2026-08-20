# Engineering Contract

## Work Control

GitHub Issues are the control plane for non-trivial work. An executable Issue states:

- one user- or system-visible goal;
- relevant contract and ADR links rather than copied content;
- acceptance criteria that can be observed;
- required repository, Windows, and hardware gates;
- native Issue dependencies for blockers.

The frontier is one open, unassigned, acceptance-ready Issue with no open blocker.
Small L0 maintenance changes may be performed directly when creating an Issue would
cost more than the change.

## Risk-Adaptive Execution

Classify work before implementation:

- **L0** — small, local, reversible: implement directly and run targeted checks.
- **L1** — normal feature or fix: use one Issue with explicit acceptance criteria.
- **L2** — cross-module, lifetime, output-semantic, or public-claim change: research
  or specify first, add an ADR for a durable choice, and use fresh-context review.
- **L3** — multi-session work: split dependent vertical Issues and advance one
  unblocked frontier at a time with a structured handoff.
- **L4** — bounded automation/Ralph loop: require machine-verifiable acceptance,
  an iteration cap, and no unapproved deployment or destructive action.

Increase ceremony only in response to risk, duration, ambiguity, or an observed
failure mode. Default to one writer. Use sub-agents only for bounded, independent
research or verification where isolated context adds value.

Long work must advance in reviewable vertical slices. A handoff contains only:

- completed behavior and commit/worktree state;
- exact evidence and commands run;
- remaining acceptance criteria or blocker;
- the next concrete action.

Do not preserve transcripts, exploratory reasoning, or session narration as project state.

## Completion

Completion is determined by Issue acceptance criteria and applicable truth level,
not by an agent's declaration. Tests should exercise the production behavior being
claimed. Templates, cached results, and incomplete artifacts never count as evidence.

Truth levels are distinct:

1. **Repository done** — implementation, relevant tests, format, and static checks pass.
2. **Platform verified** — the named Windows or macOS build/test and runtime smoke
   pass. Windows verified and macOS verified are independent claims.
3. **Hardware evidenced** — native capture, HDR behavior, sRGB Visual Match, and named
   receiving apps are observed on the named platform/display and committed as evidence.

MVP release requires the applicable level on both platforms. Public HDR-preserved
claims require level 3 for every named platform. One platform's evidence must never
be projected to another, and level 1 or 2 must never be projected upward.

## Code Rules

- Follow existing patterns before creating abstractions.
- Keep public interfaces narrow and platform ownership explicit.
- Use nullable annotations and typed expected-failure results.
- Use structured logging and deterministic native-resource disposal.
- Keep user-facing output success separate from fidelity/HDR claims.
- Do not bypass relevant formatting or test gates.

## Knowledge Hygiene

Each fact has one owner as listed in `knowledge/README.md`. Update only that owner.
At a milestone boundary, remove stale links and superseded documents instead of
appending historical sections to current-state files. A repeated workflow may become
a Skill, lint, test, or automation only after its repeated value is observed.

# AGENTS.md

Lumiere is a native Windows HDR-aware screenshot tool built with WinUI 3,
Windows App SDK, Windows Graphics Capture, Direct3D 11, DXGI, and Vortice.
This file is a map, not a project manual.

## Start

Read only:

1. `knowledge/README.md` — knowledge ownership map.
2. `knowledge/state/CURRENT.md` — one-screen project state and frontier.
3. The referenced GitHub Issue, when one exists.

Then follow that task's links and load only relevant contracts, ADRs, runbooks,
or evidence. Do not preload the knowledge base.

## Operating Model

Lumiere uses **Contract → Frontier → Evidence**:

- Contracts define what is correct.
- GitHub Issues own non-trivial work, acceptance criteria, dependencies, and status.
- `CURRENT.md` owns only current project posture; Git owns history.
- Evidence records what actually passed.
- ADRs record durable decisions and trade-offs.

Work through `Orient → Classify → Execute → Verify → Handoff`. Use the risk and
truth levels in `knowledge/contracts/engineering.md`. Default to one writer;
planner, evaluator, sub-agent, and Ralph mechanisms are conditional escalation tools.

## Required Boundaries

- Follow `knowledge/contracts/architecture.md` for platform and module ownership.
- Follow `knowledge/contracts/claims.md` for output semantics and HDR language.
- Follow `knowledge/contracts/ui.md` for native UI work.
- Follow existing code patterns before introducing abstractions.
- Use deterministic native-resource disposal and structured `ILogger` logging.
- Keep artifact success, visual match, and HDR preservation separate.

## Verification And Handoff

- Use `knowledge/runbooks/windows-development.md` for build, test, launch, and recovery.
- Use `knowledge/evidence/templates/mvp-release-evidence-template.md` for release evidence.
- Never count a template, cached result, or agent declaration as passing evidence.
- Leave each slice clean and reviewable. Record only completed behavior, exact checks,
  remaining acceptance criteria/blockers, and the next concrete action.

## Knowledge Hygiene

Update only the artifact that owns the changed fact. Do not create duplicate
backlogs, checkbox task ledgers, session/loop logs, generated context dumps, or
transcript memory. Update `CURRENT.md` only when frontier or verification posture changes.

Use Conventional Commit prefixes: `feat:`, `fix:`, `docs:`, `chore:`, and `test:`.

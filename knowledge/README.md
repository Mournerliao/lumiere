# Lumiere Knowledge Map

`knowledge/` is Lumiere's repository-visible, tool-agnostic source of project
truth. Start with this map and disclose detail only when the current task needs it.

## Resume Work

1. Read [`state/CURRENT.md`](state/CURRENT.md).
2. Read the current GitHub Issue, if one exists.
3. Follow only the contract, ADR, runbook, or evidence links relevant to that Issue.

## Ownership Map

| Question | Source of truth |
|---|---|
| What product are we shipping? | [`contracts/product.md`](contracts/product.md) |
| What can the product honestly claim? | [`contracts/claims.md`](contracts/claims.md) |
| Where do platform APIs and dependencies belong? | [`contracts/architecture.md`](contracts/architecture.md) |
| How should engineering work be performed? | [`contracts/engineering.md`](contracts/engineering.md) |
| How should the native UI look and behave? | [`contracts/ui.md`](contracts/ui.md) |
| What phase are we in and what is the frontier? | [`state/CURRENT.md`](state/CURRENT.md) |
| What comes after the current release? | [`roadmap.md`](roadmap.md) |
| Why was a durable choice made? | [`decisions/`](decisions/) |
| How is Windows development performed? | [`runbooks/windows-development.md`](runbooks/windows-development.md) |
| What was actually verified? | [`evidence/`](evidence/) |
| What external research informed a decision? | [`research/`](research/) |

## Maintenance Rules

- GitHub Issues own executable tasks, acceptance criteria, dependencies, and task status.
- `CURRENT.md` owns only the one-screen project snapshot and contains no history.
- Contracts own stable invariants, not progress updates.
- ADRs own important decision rationale, one decision per file.
- Evidence owns observed validation results; templates are never passing evidence.
- Git owns change history. Do not duplicate it with session or loop logs.
- Update only the source that owns the changed fact, and delete superseded documents.

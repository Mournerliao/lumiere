# Matt Pocock Skills Guide

This document provides a usage guide for the skills installed from [mattpocock/skills](https://github.com/mattpocock/skills).

## Engineering Skills

### diagnose -- Bug Diagnosis

**When to use:** Hard-to-reproduce bugs, performance regressions, crashes, or unexpected behavior.

**How to use:** Say `diagnose this` or `debug this`, then describe the problem. It follows a disciplined loop: reproduce, minimize, hypothesize, instrument, fix, regression-test.

**Example:** `diagnose: app crashes when switching HDR displays after startup`

---

### tdd -- Test-Driven Development

**When to use:** Building features or fixing bugs using the red-green-refactor cycle. Also suitable when integration tests are needed.

**How to use:** Describe the feature you want to implement. It will write failing tests first (red), write minimal passing code (green), then refactor.

**Example:** `tdd: implement a crop coordinate calculation module with mouse drag selection`

---

### prototype -- Rapid Prototyping

**When to use:** When you are unsure about a design and want to build a throwaway prototype to validate ideas. Good for exploring UI layouts, data models, or state machines. Can produce a terminal app for logic questions or multiple UI variations.

**How to use:** Say `prototype this` or `let me play with it`.

**Example:** `prototype: design a crop selection UI in three different approaches`

---

### improve-codebase-architecture -- Architecture Improvement

**When to use:** When the codebase feels tightly coupled, needs refactoring, or you want to improve testability and AI-navigability. References `CONTEXT.md` and `harness/architecture/adr/` for informed suggestions.

**How to use:** Describe what you want to improve, such as finding the most coupled modules or making the codebase more testable.

**Example:** `improve-codebase-architecture: find coupling points between Lumiere.Graphics and Lumiere.Capture`

---

### grill-with-docs -- Plan Validation Against Documentation

**When to use:** When you have a plan and want to challenge it against the existing domain model and architecture decision records (ADRs). Sharpens terminology and updates documentation as decisions crystallize.

**How to use:** Describe your plan, then invoke this skill. It will question the plan against existing docs and update `CONTEXT.md` or ADRs inline when decisions are made.

**Example:** `grill-with-docs: I plan to move HDR tone mapping logic from Graphics module to Overlay module`

---

### to-issues -- Break Plan Into Issues

**When to use:** After completing a plan, spec, or PRD and needing to break it into independently executable development tasks or issues. Uses tracer-bullet vertical slices for decomposition.

**How to use:** Provide your plan or PRD. It generates issues ready to submit to an issue tracker.

**Example:** `to-issues: break this crop feature PRD into independent issues`

---

### to-prd -- Convert Conversation to PRD

**When to use:** After a discussion has clarified a feature requirement and you want to formalize it into a Product Requirements Document.

**How to use:** After discussing the feature, say `to-prd` or `create a PRD from this conversation`. It extracts requirements from context and produces a PRD.

**Example:** After discussing annotation features, say `to-prd` to formalize the discussion.

---

### triage -- Issue Triage

**When to use:** When you have a batch of incoming bug reports or feature requests that need classification, priority sorting, or preparation for another agent to handle. Uses a state-machine-driven triage process.

**How to use:** Provide the issue list or descriptions. It classifies each by state (e.g., needs confirmation, ready for dev, needs more info).

**Example:** `triage: classify these GitHub issues by severity and actionability`

---

## Productivity Skills

### caveman -- Compressed Communication Mode

**When to use:** When conversation is getting long and token consumption is high. Saves approximately 75% of tokens by dropping filler, articles, and pleasantries while maintaining full technical accuracy.

**How to use:** Say `caveman mode`, `be brief`, or `less tokens` to activate. All subsequent responses use compressed format.

**Example:** `caveman mode` then continue asking questions normally.

---

### grill-me -- Relentless Questioning

**When to use:** When you have a plan or design and want the AI to pressure-test it from every angle until you reach shared understanding on every branch of the decision tree. Goes deeper than normal conversation.

**How to use:** Describe your plan or design, then say `grill me` or `stress test this plan`.

**Example:** `grill-me: I plan to use Direct2D for the crop overlay rendering`

---

### handoff -- Conversation Handoff

**When to use:** When the conversation context is nearing its limit, you need to switch to a new conversation window, or you want to hand off the task to another agent. Compresses the current conversation into a handoff document.

**How to use:** Say `handoff`. It generates a summary document containing all key decisions and progress. Paste it into the new conversation to continue.

**Example:** When the conversation is too long, say `handoff`, then paste the document into a new chat.

---

### write-a-skill -- Create New Skill

**When to use:** When you find a repeatable workflow worth encapsulating into a reusable skill with proper structure, progressive disclosure, and bundled resources.

**How to use:** Describe the skill functionality and trigger conditions. It generates a structured skill definition file.

**Example:** `write-a-skill: create a skill for automatic DXGI debug layer log analysis`

---

## Quick Reference

| Intent | Skill |
|---|---|
| Diagnose a hard bug | `diagnose` |
| Write tests first | `tdd` |
| Validate a design with prototype | `prototype` |
| Refactor codebase | `improve-codebase-architecture` |
| Challenge plan against docs | `grill-with-docs` |
| Break plan into tasks | `to-issues` |
| Turn discussion into PRD | `to-prd` |
| Classify issues/bugs | `triage` |
| Save tokens | `caveman` |
| Stress-test an idea | `grill-me` |
| Switch conversation window | `handoff` |
| Encapsulate a new workflow | `write-a-skill` |

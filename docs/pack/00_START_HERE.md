# UNQUALIFIED — Game Design & Claude Build Pack

Version 0.1 · 30 October 2026 · Prepared for Malek

Working title only: **UNQUALIFIED**. This name is not cleared for commercial use. Original concept: “Fake It Till You Make It — Unqualified Workers.”

## The game in one breath

You and up to three friends lied your way into professional jobs. Complete a shift, cover up your mistakes, and survive the promises on your own résumés before the client realizes who they hired.

**The central design decision:** the lie must change how you play. Physics chaos supplies the accident; the client witnessing it supplies the pressure; the cover-up supplies the punchline.

This pack is a proposed design, not a report of implemented features. Research findings are labeled separately. All balancing numbers, schedules, prices, and success thresholds are hypotheses for testing.

## Read order

| File | Purpose |
|---|---|
| [01_VISION_AND_BRAINSTORM.md](01_VISION_AND_BRAINSTORM.md) | Identity, creative alternatives, differentiators, scope decisions |
| [02_GAME_SYSTEMS.md](02_GAME_SYSTEMS.md) | Complete rules: lies, tasks, witnesses, suspicion, money, solo scaling |
| [03_JOBS_AND_SCENARIOS.md](03_JOBS_AND_SCENARIOS.md) | Detailed wedding prototype, two later jobs, event and claim examples |
| [04_PRESENTATION_AND_PLAYER_EXPERIENCE.md](04_PRESENTATION_AND_PLAYER_EXPERIENCE.md) | Art, controls, UI, sound, onboarding, accessibility, tone |
| [05_TECHNICAL_DESIGN.md](05_TECHNICAL_DESIGN.md) | Proposed Godot architecture, multiplayer authority, data, saves, failure handling |
| [06_BUILD_PLAN_AND_TESTS.md](06_BUILD_PLAN_AND_TESTS.md) | Implementation tickets, exit gates, playtest plan, risks, estimates |
| [07_RESEARCH_AND_COMMERCIAL_PLAN.md](07_RESEARCH_AND_COMMERCIAL_PLAN.md) | Verified competitor corrections, source links, market experiments, AI tradeoffs |
| [08_CLAUDE_HANDOFF.md](08_CLAUDE_HANDOFF.md) | Ready-to-paste starting prompt and milestone prompts |
| [09_CONTENT_BACKLOG_AND_DECISIONS.md](09_CONTENT_BACKLOG_AND_DECISIONS.md) | Expansion ideas, naming, unresolved decisions, content templates |

`UNQUALIFIED_COMPLETE_HANDOFF.md`, delivered alongside the pack, combines these documents for a single upload. The ZIP contains the individual source documents. Upload the combined file OR the individual pack to Claude; there is no need to upload both.

## Defaults Claude may use immediately

- Windows PC, first-person 3D, 1–4 players; online cooperative play is the product target.
- Godot 4 stable + typed GDScript is the proposed greenfield stack. Pin an exact compatible release during setup; do not silently upgrade it later.
- Small stylized environments, controlled physics, readable NPC behavior.
- First playable job: **wedding crew** in one banquet room and service corridor.
- Prototype: one selectable résumé lie, one witness, three essential tasks, one recoverable disaster, an eight-minute shift.
- Two-process local networking comes early. Internet friends joining without router setup is a later, explicit release gate.
- Scripted claim-card interviews first. Live AI interviews are an optional experiment, not a launch dependency.
- Private friend groups first. Solo uses workload changes, not AI teammates.
- Three jobs are a conditional full-release target, not permission to build all three immediately.
- No real medical, aviation, or emergency simulation. All risky professions use invented arcade rules.

## Scope ladder

| Stage | Actual deliverable | Explicitly excluded |
|---|---|---|
| M0 | Running project, movement and one interactable | Content production |
| M1 | Two players can share a room and reliably exchange a prop | Steam, matchmaking, voice |
| M2 | Wedding shift works solo and with 2–4 local network peers | Career economy, extra jobs |
| M3 | Fun and understandable wedding vertical slice | More professions until test gate passes |
| M4 | Real internet friend invites and five-shift campaign shell | Public matchmaking, AI interviews |
| M5 | Conditional three-job release candidate | Expansion backlog |

## Rules for interpreting this pack

1. The milestone being built outranks a feature appearing in the vision or backlog.
2. File 02 owns gameplay rules and numbers. File 03 owns job-specific content. File 05 owns implementation contracts. File 06 owns acceptance gates.
3. If two documents conflict, document the conflict and use the narrower current milestone; do not quietly implement both.
4. Preserve the owner's existing repository and engine if one is supplied. These defaults assume a new project.
5. The goal of the first prototype is to test whether covering up a mistake is fun. It is not to demonstrate how much code Claude can produce.

## What the first build should let us say

“We promised the wedding planner we could work without instructions. Bader dropped the cake. I distracted the planner while Malek rebuilt it, but she caught him reading the setup guide. We still got paid, and now the résumé says we handle high-pressure events.”

If a session instead sounds like “we carried objects until a timer ran out,” the core hook needs another iteration before adding content.

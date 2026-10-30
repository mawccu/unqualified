# Copyable Claude Prompts

## First prompt: make the game, not another placeholder

```text
Work in C:\Users\USER\unqualified. Read docs/claude_brief/00_READ_THIS_FIRST.md through 03_PRODUCTION_PLAN.md before editing, then inspect the existing project and docs/IMPLEMENTATION_STATUS.md.

The existing pink cake and stand are M0/M1 networking placeholders, not the target game. Preserve proven host-authoritative interaction and networking behavior, but begin converting the graybox into the Wedding Crew vertical slice.

First implement only this bounded increment: a readable compact wedding-venue graybox with public/private zones, a task board listing dessert/audio/seating, and a data-backed task-state service. Keep the current pickup/place behavior working. Do not add the full shift, AI interview, apartment, other jobs, Steam, realistic procedures, or final art.

Use typed GDScript and Godot 4.7.2. Shared task/evidence/suspicion state must be host-authoritative. Add tests for pure task-state transitions. Run every check you can actually run. Update docs/IMPLEMENTATION_STATUS.md. At completion report changed files, observed test results, unverified items, known limitations, and the precise next increment.
```

## Second prompt: complete one cover-up loop

```text
Implement one complete wedding incident: Cake section falls -> visible frosting evidence -> coordinator may witness it -> suspicion changes -> photo-display distraction -> clean evidence -> reassemble dessert -> debrief log. Follow docs/claude_brief/02_WEDDING_VERTICAL_SLICE.md exactly for default values unless an existing documented decision overrides them.

It must work solo and with connected players. Authority for evidence, coordinator attention, suspicion, and task quality belongs to the host. Do not build audio or seating yet. Add focused tests for deduplication, witness result, cleanup, and disconnect recovery. Run tests, update implementation status, and report observed versus unverified results.
```

## Third prompt: finish the vertical slice

```text
Add the ceremony-audio and VIP-seating tasks, their planned event cards, one selectable claim with a visible promise card and callback, final inspection, transparent payout, and the chronological incident-report debrief specified in docs/claude_brief/02_WEDDING_VERTICAL_SLICE.md.

Keep each change runnable. Do not add a new job, progression apartment, live AI, Steam, or polished art. Test task completion, missed-task recovery, suspicion thresholds, final inspection, solo route, two-player synchronization, and repeated-shift reset. Separate actual results from tests you could not run.
```

## Review prompt

```text
Review the project against docs/claude_brief/00_READ_THIS_FIRST.md through 03_PRODUCTION_PLAN.md and the current milestone in docs/IMPLEMENTATION_STATUS.md. Find reproducible gameplay, authority, synchronization, softlock, state-reset, and clarity problems. For each issue provide: priority, reproduction steps, expected result, actual result, affected files, and the smallest appropriate fix. Do not spend the review on speculative refactors or cosmetic code style while the playable loop is incomplete.
```


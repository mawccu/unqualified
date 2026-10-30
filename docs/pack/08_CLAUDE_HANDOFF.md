# 08 — Claude handoff and working prompts

## Before starting

Use Claude in a coding environment with access to the actual project directory. A chat window that cannot create/run a Godot project can help design, but it cannot honestly validate the build. Supply this pack and an empty repository or existing project. Install an agreed stable Godot release and matching export templates if needed. Do not paste paid service keys into the game project.

Read the start prompt below into Claude. The owner does not need to answer every backlog question first: the pack provides defaults. Any budget, engine change in an existing repository, paid asset purchase, live service commitment or publication remains an explicit owner decision.

## Starting prompt — copy this

```text
You are implementing my game, working title UNQUALIFIED, from the attached
design pack. Read 00_START_HERE and the gameplay, technical and milestone
documents before editing. This is a first-person cooperative game about
unqualified workers whose résumé lies create obligations during jobs.

Your immediate task is M0 only: a runnable Godot project with one graybox
room, first-person movement, a contextual prompt, one pickup/place prop,
one valid placement socket, settings/escape behavior and restart.

First inspect the repository and available runtime. Preserve any existing
work. For a new project, use the pack's Godot 4 stable + typed GDScript
default and pin an exact available version. Verify APIs against that
version. If the repo already uses another engine, explain the mismatch
before changing engines. Do not require answers to optional design questions.

Keep the next multiplayer milestone in mind: offline actions should pass
through a single interaction service that can later validate remote intents.
Do not build a giant framework before one interaction works.

Implement actual files with simple original placeholder assets. Do not
build the whole game, a web mockup, AI interviews, Steam integration, an
apartment, or extra professions. Do not invent APIs or claim checks passed
without running them. If a GUI/export cannot run here, give exact manual
steps and label those checks unverified.

At completion, report changed files, how to open/run the project, checks
actually performed, known limitations, and the precise M1 next step.
Maintain docs/decisions and a short implementation status file. Finish this
bounded milestone before proposing more features.
```

## M1 prompt — shared object

```text
Implement M1 from 06_BUILD_PLAN_AND_TESTS using 05_TECHNICAL_DESIGN.
Add localhost/LAN host and join for up to four players, stable IDs,
host-validated interactions, shared prop ownership, replication and
disconnect cleanup. Make the host player use the same validation path.
Run two separate processes if possible. Verify simultaneous pickup yields
one owner and disconnecting releases the object. Add only the diagnostics
needed to inspect these behaviors. Do not add Steam or more job content.
Report observed results separately from unrun tests.
```

## M2 prompt — the actual game loop

```text
Implement the M2 wedding scope only. Use the exact three outcomes,
quality rules, no-manual claim, single witness, suspicion ledger,
distraction, cake recovery and shift phases in files 02 and 03.
Break work into small runnable changes; keep multiplayer working after
each one. Add pure tests for deduplication, quality and payout calculation
where those systems are implemented. Leave career persistence for M4.
Finish with a playable complete shift and a reproducible test checklist.
Do not substitute a scripted cutscene for client perception or cover-ups.
```

## M3 prompt — improve from evidence

```text
Read the attached playtest notes. Identify the three highest-impact
problems in player understanding, cooperation and reliability. Map each
to a specific rule or UI change. Implement fixes before additional content.
Preserve the core promise/witness/cover-up mechanic. Update affected docs
and rerun the relevant tests. Do not treat all tester suggestions as scope.
```

## Review prompt — use after any milestone

```text
Review the current implementation against the milestone's acceptance
criteria. Prioritize reproducible bugs, authority errors, softlocks,
duplicate payouts and contradictions with the documented rules.
For each issue, give reproduction steps, expected/actual behavior and
the smallest appropriate fix. Separate verified defects from hypotheses.
Do not spend the review on style refactors while gameplay is broken.
```

## Session discipline

Keep a small `IMPLEMENTATION_STATUS.md`: current milestone; features actually running; checks performed; known bugs; next three tasks; exact engine/plugin versions. Commit meaningful increments if Git is available. Never remove a failing test just to pass a milestone, and never replace an unimplemented system with a fabricated success message.

Use this pack as a starting specification, not immutable scripture. Record approved design changes once in the owning document and update dependent content. If a mechanic is not fun, change it based on playtests rather than defending it because it appears here.

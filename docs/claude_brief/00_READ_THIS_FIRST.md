# UNQUALIFIED — Claude-ready brief

## What this project is

**UNQUALIFIED** is a first-person 1–4 player cooperative comedy game. Four broke friends bluff their way into jobs they are nowhere near qualified to perform. Each lie in the hiring process becomes a specific obligation during the shift. The team can make mistakes; what they cannot do is let the client understand that the workers are frauds.

The game is not a realistic job simulator. It is a social-chaos game about pressure, improvised teamwork, conspicuous incompetence, and frantic cover-ups.

> “Who here knows how to land a plane?”
>
> “Apparently all of us said we did.”

## Important: what currently exists in the repository

The current Godot project is an **M0/M1 technical graybox**, not a playable version of the game. The small pink `cake section` and one placement stand prove that a shared object can be picked up, placed, and replicated between players. They are deliberately ugly placeholders. Do not interpret them as the wedding job, art direction, or finished gameplay.

Do not throw away the interaction and networking foundation without evidence: it was built to ensure later task objects can be host-validated. Replace its placeholder room and prop progressively as the wedding vertical slice is built.

## The product decision this brief makes

Build one polished **Wedding Crew** shift before adding firefighters, aircraft mechanics, careers, open-world apartments, Steam, or live AI voice interviews. A player should be able to play a 12–18 minute shift with 1–4 people, create a hilarious failure, recover from it, and understand why the final payout changed.

The existing `docs/pack` directory is the detailed first design pack. This directory is the concise implementation compass. Where they differ, this directory's explicitly stated prototype choices win until the owner changes them.

## Read order

1. `01_GAME_BIBLE.md` — fantasy, pillars, loop, rules, tone.
2. `02_WEDDING_VERTICAL_SLICE.md` — exact first playable job.
3. `03_PRODUCTION_PLAN.md` — build order, technical boundaries, tests.
4. `04_CLAUDE_WORKING_PROMPTS.md` — copyable prompts and reporting format.
5. Refer to `docs/pack/05_TECHNICAL_DESIGN.md` for networking contracts and `docs/pack/06_BUILD_PLAN_AND_TESTS.md` for prior acceptance criteria.

## Non-negotiable creative tests

Every implemented feature should reinforce at least one of these:

- **The lie comes back:** a choice made before or early in a job changes a later problem.
- **Failure is visible:** it matters who sees it, what proof remains, and whether the team can sell a believable explanation.
- **Recovery is play:** cleaning, staging a distraction, moving evidence, and convincing a witness are cooperative actions—not a reload screen.
- **Players create the stories:** systems combine; scripted jokes only support them.
- **Comprehension wins:** players always know the current task, the current danger, and one plausible next move.

## Boundaries for the first prototype

- First-person keyboard/mouse controls; voice chat is external for now.
- 1–4 players, solo must remain completable.
- One hotel wedding venue, three essential responsibilities, a small event pool.
- Simple stylized placeholder art and readable UI are acceptable. Realistic graphics are not required.
- No real medical, emergency-response, aviation, or nuclear procedures. Future jobs must remain fictionalized, reversible, and comedic.
- No live generative voice interview in the prototype. Use a branching text/voice-like interview screen first; see `03_PRODUCTION_PLAN.md`.

## Definition of “a real first playable”

By the end of the wedding vertical slice, a new player can select one résumé claim, start a shift, complete or botch three duties, trigger suspicion, use a cover-up, reach a debrief, and understand their grade and payout. It must work online and offline, and must not softlock if an object is dropped, a player disconnects, or a task is missed.


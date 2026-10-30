# 02 — Gameplay rules and balancing baseline

All values below are proposed tuning defaults. Keep them in data/configuration, not scattered constants. This file is the authority for cross-job rules.

## Loop and state machine

`Lobby → Contract selection → Claim selection → Briefing → Work → Final inspection → Debrief → Lobby`

Career inserts a week ledger after debrief. Hard failure enters debrief directly. A host disconnect ends the live session with an explanation and preserves the previous completed-shift save.

- Selection: host confirms a contract after teammates vote; no unanimity deadlock.
- Claim selection: prototype one crew claim; full game zero to two. Every claim displays its rule, trigger, and bonus.
- Briefing: up to 30 seconds; skip when all ready. Show three essential outcomes and the client's priorities.
- Work: 480 seconds on Standard difficulty.
- Inspection: maximum 45 seconds; task quality is frozen at the end of Work. NPC presents findings; cannot turn a clean finish into failure through new random events.
- Debrief: shared result, incident highlights, payout, retry/continue. No automatic purchase or spending.

In M2–M3 use Quick Shift only, with fixed briefing and claim choices. Career and contract voting belong to M4.

## Moment-to-moment verbs

Move, look, pick up, place, rotate, use, clean, read, ping, talk, and carry. A player holds one functional object at a time. Interaction resolves by contextual priority: explicit aimed target first, held tool application second, default pickup last. Show the action before input.

Use controlled placement sockets for mission-critical objects. Free props may tumble, but correct completion must not require pixel-perfect positioning. No separate left/right arm control. Throwing is optional and limited to nonessential lightweight props until multiplayer is stable.

Players are briefly stunned by slapstick accidents, not eliminated. Prototype stun: two seconds and dropped item. No lasting injury, permadeath, or waiting through an entire shift.

## Objective graph and quality

Every job has three essential outcomes, each with an integer quality score `q_i` from 0 to 100. Job-specific rules determine these values. Overall quality is `Q = round((q_1 + q_2 + q_3) / 3)`.

Success requires all three outcomes marked complete, `Q ≥ 60`, and no exposure failure. Completion does not imply quality: a table can be structurally assembled but dirty. Optional objectives improve individual outcome scores or grant a separately specified cosmetic accolade; they never secretly change the scoring formula.

At timeout evaluate the current state. Incomplete essentials cause shift failure with partial pay. A requested early finish is allowed only when all essentials are complete; the host gets a confirmation showing current Q and pending inspection evidence. Other players receive a five-second warning.

Task dependencies form a directed acyclic graph. Example: connect audio power → set symbol sequence → perform microphone test. Each required object has a recovery route. No random event can destroy the sole objective-critical object permanently.

## Résumé claims

Each claim contains an ID, public promise, eligible jobs, mechanical modifier, detectable violation, advertised bonus, and compatibility restrictions.

Prototype examples:

| Claim | Bonus | Mechanical consequence | Detectable violation |
|---|---:|---|---|
| No manuals needed | +20% | Instructions remain available | Reading a tagged manual in client view |
| Spotless finish | +20% | Client includes service threshold in inspection route | Tagged mess still present at final inspection |
| Fast turnaround | +20% | Work time becomes 420 seconds | None; shorter time is the tradeoff |

Implement only “No manuals needed” in M2. The other two are M3 variants. Bonuses add up to a maximum +40% with two claims in the later game. Never multiply bonuses together.

A detected claim violation creates suspicion and permanently removes that claim's pay bonus for the shift. It does not automatically fail the job. The UI marks the specific promise “Questioned” and shows why. Repeated incidents cannot repeatedly deduct the same bonus.

Claims are chosen before the shift and cannot be switched mid-shift. Hidden manual reading is allowed: the joke is deceiving the witness, not disabling knowledge. Familiar groups can use memory. Later procedural task variants preserve interest without randomizing controls.

## Witnesses and evidence

M2 has one client NPC. M3 adds a second only if comprehension remains good. Other guests can initially be static decorations without perception.

Each witness uses an explicit state machine: Patrol, Notice, Investigate, Challenge, Distracted, Return. A witness only learns something from:

- A visible player action after a short observation dwell.
- A visible persistent evidence object, such as a damaged cake.
- A heard tagged sound that prompts investigation; hearing alone does not reveal the cause.
- An authored inspection checkpoint with a valid route.

Proposed vision: 8 m range, 100° cone, unobstructed ray to an evidence anchor, 0.8 s dwell. Audit these in an editor debug overlay. Characters cannot act as fully opaque shields; movable approved screens can block vision but may attract inspection if placed in tagged prohibited areas.

A sound gives a location, not guilt. A loud crash can move the witness toward the cake. Only seeing the damage produces its suspicion event. No global “everybody knows” unless a later authored event explicitly communicates it.

Each incident has an ID. The shared suspicion ledger applies its penalty at most once even if two witnesses see it. Repairing the damage closes the evidence. A genuinely new break creates a new ID. Client acknowledgement and global UI must agree on what was seen.

## Shared suspicion

One shared meter `S` ranges from 0 to 100. There are no hidden personal suspicion bars in the initial design.

| Event | Proposed change |
|---|---:|
| Witnessed minor breach, e.g. reading after “no manuals” | +12 |
| Witnessed material damage | +20 |
| Visible prohibited shortcut, e.g. uncertified replacement at inspection | +15 |
| Successful requested demonstration | −15 |
| Failed demonstration | +20 |

Apply incident changes in authoritative order, clamp after each event, and emit the reason. These are event deltas; never add them every frame an NPC can see a stain.

- `S < 30`: routine behavior.
- `30 ≤ S < 60`: curious; client visits the affected area more often.
- `60 ≤ S < 100`: client may issue one face-saving challenge.
- `S = 100`: exposure, shift ends in a short walkout scene and failure payout.

Decay: after 20 seconds with no unresolved **observed** evidence and no active challenge, reduce by one point every five seconds, but not below 20 once any incident has been recorded. Starting suspicion is 0. Unseen evidence prevents nothing until observed; unresolved observed damage blocks decay even when moved out of sight. Repair it to clear that condition.

Final inspection uses the same incident IDs and never charges twice for previously observed damage. It can discover a previously hidden mess. No decay runs during inspection. Freeze scoring, resolve checkpoint observations, check exposure, then debrief.

## Cover-ups and recovery

| Response | Benefit | Cost / limitation |
|---|---|---|
| Actually repair | Restores task quality and removes persistent evidence | Time, tool, replacement material |
| Move evidence behind a screen | Prevents immediate discovery | Does not restore quality; later inspection can find it |
| Distract client | Redirects attention for 8 seconds | Player must stay engaged; once per witness per 45 seconds |
| Use an approved substitute | Restores functional completion | Job-defined quality cap, e.g. 70 rather than 100 |
| Complete demonstration | Reduces suspicion by 15 | Must meet the requested condition before deadline |

Distraction is a contextual authored interaction at a valid destination, such as the flower display. It is not free-form speech recognition. It ends early if the player leaves, if the destination is invalid, or if a critical nearby incident interrupts it. A witness cannot be trapped in a loop forever.

Challenges are eligible once S reaches 60, at most twice per shift, at least 60 seconds apart. They are selected only from feasible objectives and warn the whole crew. Deadline: 25 seconds Standard, 40 solo. After resolution, the witness returns to normal state and cooldown starts. Exposure at 100 always takes priority.

## Event director

Author events with preconditions, warning, cost, duration, cooldown, conflicts, affected tasks, and recovery. Select with a host-owned seed. Never use randomness separately on each client.

- No surprise event in the first 45 seconds of the first tutorial shift.
- Standard: at most one major event active, and up to two minor events if compatible.
- Major events at least 90 seconds apart.
- A warning must communicate what is changing before a major consequence.
- Never trigger a repair requiring an absent tool, blocked room, or already consumed unique item.
- Do not trigger a new timed event if insufficient work time remains for its advertised recovery window.
- If the director cannot find a valid event, do nothing. Quiet time is useful.

## Player-count scaling

Snapshot active count at shift start. In M2 reject new joins mid-shift with “Join after this shift.” On a disconnect, release props immediately and apply the lower-count concurrency cap; cancel now-impossible optional challenges without reward or penalty. Never increase completed requirements.

| Players | Essential task structure | Parallel timed pressures | Carry rules | Challenge time |
|---|---|---:|---|---:|
| 1 | Same three outcomes, fewer units | 1 | All required objects solo movable | 40 s |
| 2 | Moderate units | 1 | Solo carry valid; assist faster | 25 s |
| 3 | More units | 2 | Same | 25 s |
| 4 | Full units | 2 | Same | 25 s |

Do not scale every quantity linearly. More players already create congestion and communication mistakes. No objective ever requires two simultaneous inputs in solo. Solo distractions can use a placed approved display; do not demand talking and repairing at the same instant.

## Career money and progression — M4+

Fictional dollars, unrelated to real wages. Five paid shifts per week, rent due after the fifth. Start with $0, free required equipment, and no entry fee. Initial rent: $1,200. Contract base pay B: $300 beginner, $400 intermediate, $500 advanced.

Successful shift:

`gross = round(B × (0.25 + 0.75 × Q / 100) × (1 + retained_claim_bonus))`

`net = max(0, gross − damage_deduction)`

Damage deduction is the sum of authored nonduplicated monetary incidents, capped at `0.30 × B`. A claim violation removes its bonus but does not itself create a money deduction. Do not charge an invented fine for every prop collision.

Failure or exposure: `gross = round(0.25 × B × Q / 100)`; no claim bonus; same capped deduction; net cannot be negative.

Example: B=300, Q=80, one retained +20% claim, damage=20 gives gross=306 and net=286. Five identical results yield $1,430, enough for rent with $230 remaining. This is arithmetic, not validated economy balance.

Rank unlocks are based on completed contracts, not spending. Proposed intermediate: three successful shifts; advanced: seven. The first week therefore uses mostly beginner jobs. Rent does not rise until data supports an increase. Rent failure offers retry week with permanent cosmetics retained; no debt interest or unrecoverable campaign.

The crew campaign belongs to the host. Purchases require host confirmation and are shown to the crew. Cosmetics and unlocked quick-shift jobs are local profile data; joining a friend's week does not overwrite your own campaign. No progression-dependent stat advantage for returning players.

## Difficulty and replayability

M2 Standard only. Later Relaxed reduces observation pressure and removes rent failure; Standard follows the baseline; Overqualified uses stronger combinations and additional inspection checkpoints. Show rule differences explicitly.

Replayability comes from claim choice, task arrangements, client priority, tool placement, a small event pool, and player improvisation. Seeded variants must all be validated. “Procedural” is not permission to generate unsolvable jobs.

Veteran progression should add options: new contracts, alternate uniforms, apartment clutter, résumé stamps, and challenge seeds. Avoid upgrades that erase the need to cooperate.

# 06 — Build plan, acceptance tests, and decision gates

## How to use this plan

Implement one milestone at a time. A running graybox with tested behavior is more valuable than a large untested scaffold. Estimates below are rough person-day allowances for someone learning while building with AI assistance. They are not delivery promises; unfamiliar engine/network work can exceed them substantially. Art, publishing, external approvals, and iteration are additional.

| Milestone | Rough allowance | Exit gate |
|---|---:|---|
| M0: foundation | 2–4 days | Exported build moves, interacts, restarts |
| M1: shared room | 3–7 days | Two real processes share prop ownership correctly |
| M2: playable wedding | 7–14 days | Full shift, rules, result, solo and 2–4 peers |
| M3: validation slice | 5–10 days plus testing | Deception adds fun and players understand consequences |
| M4: internet and career | 7–15 days plus integration risk | Separate-network join and five-shift save loop |
| M5: more content and release | Re-estimate after M3/M4 | Quality gates pass for every advertised feature |

At 15 productive hours per week, even 20 person-days of six hours is about eight weeks. Do not confuse an AI coding session with a completed production day. A three-job commercial game is a multi-month possibility, not a weekend promise.

## M0 — Foundation

- T001: Inspect existing repository; record engine, exact version, license and export assumptions in a decision note. Preserve existing work.
- T002: Create project, bootstrap scene, input actions, settings, first-person controller, a small room.
- T003: Implement contextual prompt and one pickup/place object with a valid socket.
- T004: Add restart and a Windows export procedure. Record what was actually run.

Acceptance: a fresh launch enters the room; focus loss releases mouse capture appropriately; Esc works; a prop can be picked up, placed, dropped, and reset repeatedly; no missing resources or script parse errors. If no GUI/export runner exists in Claude's environment, provide exact human validation steps and do not label them passed.

## M1 — Multiplayer before content multiplication

- T005: Host/join on localhost or controlled LAN with peer count and connection errors.
- T006: Stable entity IDs, authoritative interaction validation, ownership and revision checks.
- T007: Interpolated remote movement and controlled prop poses.
- T008: Disconnect cleanup, object recovery, and a minimal event log.

Acceptance: two separately launched processes; both try to grab one object; exactly one wins; repeat at least 20 times without duplication. Disconnect while holding; object becomes usable. Invalid distant pickup is rejected. Three clients plus host can enter the same lobby. Test remote feel under added latency before spending time on visual polish.

## M2 — Wedding core

- T009: Shift state machine, 480-second clock, three outcome definitions.
- T010: Cake assembly/damage/repair/substitute; cards; symbol audio task.
- T011: Client patrol, vision dwell, obstruction, sound investigation.
- T012: Incident deduplication, shared suspicion, manual claim violation.
- T013: Distraction, feasible dessert challenge, timeout/exposure paths.
- T014: Frozen inspection, quality score, debrief, quick retry.
- T015: Solo quantities, disconnect scaling, essential-item recovery.

Acceptance: complete the shift normally; fail on timeout; get exposed at S=100; break and repair a cake; hide evidence without restoring score; read the manual safely behind a closed door and get detected when visibly reading; see one shared result on all peers. No career, second job, AI, Steam, or visual overhaul in this milestone.

## M3 — Test the actual idea

Add the two remaining wedding claim variants, 3–4 compatible event variants, polished detection feedback, pings, basic sound, and short onboarding. Add a second NPC only if a single NPC is understandable and too easy.

Recruit 5–8 small friend groups if feasible, plus at least three solo testers. These are qualitative prototype samples, not statistically representative market research. Observe a first run and an immediate replay. Obtain permission before recording. Do not coach every mistake away.

Questions: What did you think your promise changed? Why did suspicion rise? When did you help a friend cover something up? What felt unfair? Would you choose another shift right now? What do you remember five minutes later?

Proposed internal gate:

- At least four of five observed groups can explain the promise mechanic without a post-game explanation.
- At least three of five voluntarily choose another shift when offered.
- At least one unscripted repair/distraction/cover-up interaction occurs in most sessions.
- No repeated softlocks or unexplained exposure.
- Solo can finish without simultaneous multi-person input requirements.

These thresholds are decision aids, not claims of product-market fit. If failures cluster, change the mechanic. Do not improve the apparent result by excluding confused groups.

## M4 — Online product and week structure

Run the Steam transport/binding spike before integrating elaborate lobbies. Prove host/invite/join with two accounts on separate networks and no manual forwarding. Test firewall denial, timeout, rejected version, full lobby and host exit. Use only valid test access; do not ship another game's app ID as your production identity.

Add three offers from available job variants, claim voting, shared readiness, five-shift ledger, payouts, rank unlocks, rent, profile/campaign separation, and atomic save/backup. It is acceptable for career development to reuse wedding variants temporarily, but do not market this as a three-profession game.

## M5 — Content and release candidate

Only proceed after M3 fun and M4 network gates pass. Build the firefighter graybox using existing services, test it, then aircraft technicians. Each job repeats the same content validation and player-count tests. If the second job requires rewriting every system, stop and fix the abstractions before the third.

Polish settings, help, accessibility, export reliability, asset licensing, controller behavior if advertised, and store material accuracy. Recheck current Steam submission requirements at release; do not treat this October 2026 research as permanent policy.

## High-value test matrix

| Test | Expected result |
|---|---|
| Same damage observed twice or by two witnesses | One suspicion delta and one deduction |
| Manual read behind wall | No observation; no claim penalty |
| Loud crash behind wall | Investigation only until evidence is visible |
| Two pickup requests same revision | One authoritative owner |
| Duplicate shift-result delivery | One payout commit |
| Claim violated repeatedly | Bonus removed once; each genuinely new incident may add suspicion |
| Repair after witnessed damage | Evidence closes; historical S is not erased |
| Hidden broken cake | Still low quality/incomplete despite no discovery |
| No feasible event | Director skips event |
| Critical object falls out of map | Recover at shelf with notice |
| Solo and client disconnect | No impossible concurrency or permanently owned object |
| 100–150 ms latency / 1–3% loss simulation | No duplicated state; actions remain understandable |
| Host exit during shift | Clear return to menu; previous checkpoint intact |
| Save corruption | Backup/recovery offered; no silent overwrite of valid backup |

Automate pure payout, incident deduplication, task dependencies, claim retention, and save version behavior. Use engine integration tests or reproducible manual scenarios for visibility, physics and networking. Screenshot tests cannot prove gameplay correctness.

## Cut order if scope grows

Cut walkable apartment, built-in voice, second NPC, cosmetics, camera/photo systems, advanced physics, elaborate interviews, and public matchmaking before cutting the promise/witness/cover-up loop. If three jobs are unaffordable, reconsider product scope and positioning openly rather than pretending one job is three.

## Milestone report template

Record goal, changed files, run instructions, exact checks performed, results, known defects, footage or logs if available, and next bounded task. Distinguish “implemented,” “compiled,” “ran,” and “tested with people.” Never merge those into “done.”

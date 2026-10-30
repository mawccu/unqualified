# Wedding Crew: “Five-Star Experience” — Vertical Slice Specification

## Purpose

This is the first complete job because its stakes are instantly understood, its public/private spaces naturally create witness gameplay, and its objects can be built cheaply. The goal is not a full wedding simulator; it is proof that the résumé-debt and cover-up loop produces laughter with 1–4 players.

## Shift setup

- Venue: a compact fictional hotel ballroom with lobby entrance, dining room, stage/dance floor, dessert station, service corridor, storage room, and wash-up nook.
- Length: 12–18 minutes; pause timing during a solo offline pause menu only.
- Players: 1–4. Scale object count and simultaneous demands—not task logic.
- Select one crew claim at briefing. For the initial build, the claim is chosen by the lobby host and applies to the team, so the obligation is clear and no player is locked out.
- Goal: finish the shift with three essential outcomes in acceptable condition, keep suspicion below 100, and pass the final inspection.

## Essential outcomes

| Outcome | Correct state | Common failure | Recovery / cover-up |
| --- | --- | --- | --- |
| Dessert display | Cake assembled on clean stand; topper placed; no public mess. | Cake section falls, frosting spill, wrong piece on display. | Retrieve pieces, clean spill, reassemble while a teammate distracts the coordinator. |
| Ceremony audio | Correct source connected; five-step cue sequence performed at ceremony time. | Wrong cable, feedback, wrong cue, someone consults manual in view. | Trace labelled cables, reset mixer, use service-room notes, stage a “sound check.” |
| Guest seating | Three marked tables have correct signs, centrepieces, and clear aisles. | Wrong sign, missing centerpiece, blocked aisle. | Swap labels, move props, explain a deliberate “last-minute family request.” |

All three tasks use the same interaction vocabulary but their social danger differs. The cake is physical recovery, audio is information/sequence pressure, seating is spatial organization.

## Claims and callbacks

Start with three selectable claims; only implement one obligation at a time if needed, but preserve the data model for all three.

| Claim | Immediate benefit | Later callback | Failure when witnessed |
| --- | --- | --- | --- |
| “I’m a certified pastry chef.” | Dessert guide highlights correct cake positions. | Coordinator asks player/team to explain a cake issue and demands a clean display. | Suspicion +15; inspection of dessert station. |
| “I do live sound.” | Cable labels are readable from farther away. | Coordinator requires the ceremony cue to be run without visible manual-reading. | Suspicion +12; audio reset pressure. |
| “I organize luxury events.” | Seating plan has one useful hint. | VIP asks why their place setting moved. | Suspicion +10; demands a quick seating walkthrough. |

The claim creates a **promise card** in the HUD. It states who made the claim, the likely callback, and whether the debt is settled. The game should never surprise a player with a duty they cannot identify.

## Venue flow

1. **Briefing (1 minute):** manager points out dessert, stage, seating. HUD shows three outcomes, first due time, and team claim.
2. **Setup (4–6 minutes):** work begins; players learn their routes and tools.
3. **First pressure (3–4 minutes):** one planned event introduces a claim callback or object failure.
4. **Ceremony window (2 minutes):** audio task becomes public. Coordinator patrols more.
5. **Reception scramble (3–5 minutes):** a second event plus final essential-state checks.
6. **Inspection and debrief (1 minute):** coordinator visits all three stations; grade and story log appear.

## Minimum event cards

Implement these in a predictable order first. Add randomness only after they all work and playtesting confirms the base loop is readable.

### Event A — Cake section falls

A cake section becomes loose during setup. If ignored, it falls and makes a visible frosting spill. Players can secure it in time, retrieve it after a fall, clean the spill, and reassemble the cake. If the coordinator sees the spill or broken display, suspicion rises. A teammate can trigger the display prop to distract the coordinator for 8 seconds; this creates an audible public “photo opportunity,” so it cannot be silently spammed.

### Event B — Ceremony cue goes wrong

The mixer asks for a five-icon sequence that is not safely memorisable. The full sequence is shown in a service note and can be checked in the public stage area at a social cost. Correct cables and cue sequence complete the task. A wrong cue creates feedback; it can be reset, but nearby guests notice.

### Event C — VIP seat conflict

Two visible table signs are swapped and one aisle is blocked by a decoration crate. The player can restore the plan, move the crate, or sell a temporary rearrangement through a short dialogue choice. The fake explanation works once only; repeating it raises suspicion.

## Cover-up mechanics

### Attention

The coordinator has a readable icon and a field/route. Actions in a public zone count as witnessed only if the coordinator has attention and reasonable line of sight. Do not make exact stealth geometry a requirement; the UI must plainly communicate “being watched.”

### Distraction

The venue contains a `photo display` interaction. Starting it occupies a player briefly, then distracts the coordinator for 8 seconds. It has a 30-second cooldown. In solo it is the planned answer to “how can I repair and cover up?” In co-op it buys time while other players work.

### Evidence removal

Spills and wrong labels are persistent evidence. Cleaning takes 4 seconds; cake reassembly takes 6 seconds. These values are prototype defaults to tune in playtests. A repair should be possible during one distraction if the team is coordinated, but difficult solo without preparation.

## Suspicion defaults

- Witnessed frosting spill: +20.
- Reading service note while the coordinator is clearly watching: +8.
- Wrong ceremony cue / audible feedback: +12.
- VIP finds wrong seat: +10.
- Successful claim demonstration: -10, minimum 20 after the first serious suspicion.
- Completed essential outcome in public view: -3 once each; it rewards visible competence without deleting the whole story.

At 50 suspicion, trigger one scheduled demand: “Show me the finished dessert station.” This avoids a system that never appears in a short prototype. At 75, the coordinator patrols essential outcomes more frequently. At 100, allow the current interaction to finish, then mark the contract exposed and proceed to debrief rather than abruptly disconnecting players from the fun.

## Scoring and debrief

Each essential outcome is `excellent`, `acceptable`, or `failed`. Payout is a transparent combination of base pay, outcome quality, recovered incidents, and exposure penalty. The debrief displays a chronological “incident report,” for example:

> 14:03 — Cake dropped in view of the coordinator. (+20 suspicion)
>
> 14:04 — Team hosted an impromptu photo opportunity.
>
> 14:04 — Frosting cleaned; dessert display restored.
>
> Result — Client calls it “delightfully bespoke.”

This is a core story-delivery feature, not polish. It teaches systems and makes failure shareable.

## Solo and multiplayer requirements

- No mandatory simultaneous button hold in the initial slice.
- Every essential outcome has a solo path that takes longer or needs preparation.
- Co-op advantages are division of labour, information sharing, and witness management—not arbitrary player-count locks.
- If a player disconnects holding an object, the authoritative host drops it safely and keeps the task completable.
- Interactions affecting shared outcome, evidence, or suspicion must be resolved by the host.

## Completion checklist

A vertical-slice build is complete only when a 1–4 player team can run the full shift twice in a row with different incident outcomes; all three tasks resolve; suspicion responds to witnessed events; one cover-up succeeds; final inspection reads actual world state; and debrief explains the score.


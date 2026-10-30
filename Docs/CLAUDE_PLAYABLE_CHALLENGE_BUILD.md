# UNQUALIFIED — a deeper, replayable wedding shift

## User request and priority

The user has played the current build and is happy with it. Preserve the working apartment, landlord, CV, hands, animations, and wedding flow. The next build needs more challenge, more interactive objects including optional toys, jumping, skills that matter because of CV lies, a somewhat larger and more detailed map, and solid collision with chairs and people.

This is an implementation brief. Specific timings, counts, and difficulty values are proposed starting points, not measured balance. Deliver playable changes in stages, not another document or a room full of static decorative props. Make one wedding job worth replaying before adding professions.

## Current code findings to verify before editing

WeddingShift currently evaluates TaskBoard occupancy and pays once all tasks are done. CV claims select assignments and greeting lines; Pressure is explicitly flavor-only. Coordinator currently moves by direct transform updates through fixed route coordinates, with suspicion described as future work. These are upgrade points, not reasons to replace the whole project. Inspect current code first because concurrent work may have changed it.

## 1. Solid movement and collisions come first

Deliver jump, falling, landing, and reliable contact with the environment before adding timed challenges.

- Add a rebindable jump action, keyboard Space by default. Initial tuning: about 0.7–0.9 m jump height, short input buffer and coyote time around 0.1 s. Derive initial velocity from configured height and gravity. Allow one jump until grounded again; holding the button must not produce repeated jumps.
- Add takeoff, airborne, and landing poses with hands reacting to what they carry. Small landing response; adjustable camera shake including zero. Jumping while carrying a cake risks extra wobble, not automatic destruction.
- Capsule sweeps must stop ascent against ceilings and prevent walking through chair seats/backs, tables, closed doors, counters, walls, and NPC bodies. Check collider layers and whether render meshes have appropriate colliders. Visual models alone do not provide collision.
- Use simple compound colliders for irregular furniture. Fit them to occupied volume without giant invisible boxes blocking adjacent aisles. Keep chair collision active even when the chair is movable.
- NPCs need collision-aware movement and obstacle routing, not transform motion through furniture. Account for doors and moved chairs. Stop, yield, or repath when blocked. Provide enough aisle space for players to pass; NPCs should not shove the player through walls.
- An NPC can say “Excuse me” and turn sideways or back up slightly. Avoid deadlocks and uncontrolled body pushing. If character controller movement pushes a light prop, bound that push; it must not launch heavy furniture.
- Test stairs, chair edges, doorway corners, jumping beneath tables, landing on furniture, moving doors, and standing up from crouch under low objects. Jumping is allowed to create funny routes; define legitimate standable surfaces and block out-of-map escape.
- Held objects must also respect world obstruction. If a prop cannot fit through a gap, shorten the reach or block its passage; do not let the object pass through while the player remains outside.

## 2. CV lies must alter the actual job

Keep a simpler temp route when the player claims no experience. Expert claims unlock higher expectations and a visible bonus opportunity. Preview consequences before submitting the CV. Avoid secretly making the same pay harder just because the player engaged with the premise.

| Claim | Playable skill test | Recoverable error | Reward for doing it well |
| --- | --- | --- | --- |
| Cake specialist | Carry stable layers, align their centers, build largest to smallest, finish the topper. | A badly supported tier tilts/slides; fallen cake needs salvage or a spare. | Presentation bonus. |
| Seating coordinator | Use a guest sheet to satisfy small seating constraints, then seat arriving guests. | Guest objects or waits; rearrange signs/chairs and escort them again. | Guest-satisfaction bonus. |
| Audio technician, new | Connect labeled source/output ports, test sound, set gain, and hit the ceremony cue. | Silence, feedback, or wrong music; mute, diagnose, reconnect, retry. | Ceremony bonus. |
| Works under pressure | Opt into one additional overlapping request with explicit warning and extra pay. | Missed optional request loses its bonus; primary shift can still continue. | Pressure bonus. |

Do not advertise the audio claim until its task works. Existing saves with old claims remain valid. Model claims and modifiers as data rather than scattering string comparisons through every interaction.

### Cake: physical skill with readable feedback

Let players rotate and gently place layers using the existing hand/placement controls. Show a subtle footprint/support preview, not an invisible pixel-perfect target. A reasonable alignment tolerance should reward care without requiring perfection. Allow a carefully aligned placement to settle stably; do not fake stability by allowing any position to snap from a long distance.

Fast acceleration, sharp turns, and landing increase a visible wobble signal. Slowing down lets it settle. Hands visibly rebalance before serious trouble. Begin with one clearly warned slip state rather than an expensive soft-body cake simulation. Track real support/damage state for task completion, not merely socket occupancy.

Provide a spatula for minor repairs and a limited spare tier in storage. The player can recover at a time cost. Broken-looking cake earns a lower presentation score but does not automatically end the whole shift.

### Seating: observation and reasoning

Use a small authored guest roster with three or four clear constraints: an elderly guest wants a seat away from the speaker, two guests want to sit together, and a feuding pair must be separated. Make all clues available on the guest sheet or through short NPC dialogue. Do not require outside wedding knowledge.

Generate only solvable combinations with enough seats and accessible routes. Guests express specific problems (“That's right beside the speaker”) instead of a generic failure buzzer. Start with a small group; increase constraints or arrivals with crew size. Moving a chair must update whether a place is usable and reachable.

### Audio: a small understandable system

Provide a source device, mixer, speaker, and a few labeled cable ends. Labels and connector shapes must supplement colors. Use defined connection points and visual cable curves; full rope physics is optional and not required for a working puzzle.

Players can test before guests arrive. A meter shows signal and clipping; a speaker indicator shows output. Use a short cue sheet with two or three instructions. Correct routing, a safe gain range, and the right track at the cue produce success. Missing a cue allows an embarrassed restart, not an instant fail screen. Use original or appropriately licensed audio and record provenance.

## 3. Protect the central fantasy: hide incompetence

Add a coordinator who notices evidence and gives the crew a chance to fix it. A task difficulty increase alone does not fulfill the fraud premise.

- Separate task quality from suspicion. A wrong cable connection backstage affects sound; suspicion rises when someone hears its result or sees the crew mishandling equipment.
- Visual observation uses range, facing, and occlusion. A wall or closed door blocks seeing a mistake. Hearing is a separate bounded event; the coordinator should not identify a culprit or exact hidden action merely because a sound occurred.
- Telegraph inspection: “I'll check the cake in a moment,” a turn toward the station, and an approaching route. Show what triggered attention in plain language.
- Use restrained states: calm → watching → questioning → formal warning → exposed after repeated unresolved witnessed failures. A single dropped mug should not end the run.
- Let players recover through actions: clean a spill, fix a leaning cake, mute feedback, restore a chair, or divert attention with a believable interaction. Give distractions cooldowns and diminishing effectiveness so spamming them cannot erase every mistake.
- On questioning, offer brief contextual responses, not an unrestricted AI conversation. A bluff buys a short inspection delay; it does not magically fix the evidence. At least one excuse should reference the CV claim.
- Persist major unresolved incidents while allowing minor attention to fade. Do not charge suspicion every frame for one spill. Resolve events once, with deduplication and clear reasons.

Example target moment: one player balances the cake, another delays the coordinator with a seating question, and a third cleans up the evidence of the previous attempt. Every role should also be possible sequentially in solo play.

## 4. More things to touch, including things with no objective

Add a first set of roughly 12–16 distinct usable props across reusable interaction families. Repeated copies do not count as distinct mechanics. Objects need feedback, hands/poses where appropriate, sensible collision, and persistent local state.

| Object | Interaction and payoff |
| --- | --- |
| Chairs | Pull/push or reposition; occupy real space; nearby guests react if their chair is removed. |
| Drawers / cupboards | Open to find tools or harmless clutter; cannot slam through the player. |
| Cups / bottles | Pick up, place, knock over; controlled liquid/spill effects where supported. |
| Mop / cloth | Clean spills through a brief physical action with visible progress. |
| Serving tray | Carry several supported items; weight and sharp movement affect stability. |
| Bell | Ring it; nearby NPC looks over; repeated ringing becomes annoying. |
| Radio | Toggle and change between a few original/licensed clips. Optional amusement. |
| Fan | Switch on; papers or lightweight decorations react within a small local area. |
| Light switch | Change a local light; guests complain if their area stays dark. |
| Bin | Open lid and dispose of clutter; protected mission items are recoverable. |
| Tablecloth | Tug or straighten with a constrained animation; can disturb nearby lightweight props without requiring cloth simulation. |
| Clipboard / guest book | Inspect and turn readable pages; supplies actual seating information. |
| Party popper | Limited-use comic distraction with a small cleanup consequence. |
| Couch / spare chair | Sit and stand; freeform downtime without advancing the mission. |

Do not make every playful interaction punish the player. Give players room to mess around. Never let an essential object become permanently unrecoverable in a bin, outside the map, or under geometry. Provide a justified replacement/retrieval path rather than silently teleporting an actively held object away.

## 5. Enlarge the venue through useful spaces

Target a modest expansion, roughly 25–40% more usable area as a first proposal, not a huge walking simulator. Keep visual style and existing landmarks. Validate scale in first person before decorating.

Add connected spaces:

- **Ballroom:** existing main room, improved seating aisles, clear cake and stage destinations.
- **Prep kitchen:** safe practice counter, tray/tool storage, sink, cleanup equipment, and cake spare.
- **Service corridor:** an alternate route between kitchen and stage. Public route is shorter and more observed; service route is a little longer and more discreet.
- **Entrance / cloakroom:** guests arrive here; host checks the roster and directs them.
- **Small storage room:** shelves, cupboards, optional props, and clearly findable replacement supplies.

Build a circulation loop with at least two routes between major work areas. Avoid requiring a two-handed object to pass through an impossibly tight doorway. Use sightlines and windows deliberately for observation gameplay. Give each room a readable identity with signs, furniture, tools, and sound, rather than filling every surface with visual noise.

Detail should support life and navigation: coat hooks, stacked plates, spare linen, place cards, wall notices, scuffed service doors, and occupied tables. Keep critical tools visible and reachable. Rebuild navigation and inspect NPC paths after furniture placement. Preserve performance by using simple colliders, sleeping rigidbodies, pooled effects, and a bounded number of awake props.

## 6. A shift with progression and recoverable pressure

Target an initial 10–15 minute run, then tune from real playtests. Suggested phases: briefing and practice → preparation → guest arrivals → ceremony/service → inspection and payout. Do not finish the shift the moment a few sockets are filled.

Use a small authored event pool: a guest requests a seat change, a delivery arrives at the service entrance, or the coordinator requests a sound check. Start with no more than two concurrent demands, fewer in solo. No random event may silently invalidate a completed task; announce changed requirements.

Scale simultaneous requests and task quantity with player count rather than shrinking all timers. Solo players must be able to put objects down and handle another task. Primary deadlines need visible warnings and a recovery window. Optional excellence tasks add pressure for experienced crews.

At the end, show base pay, earned bonuses, capped deductions, and the actual reasons. Feed earned pay into the existing rent progression exactly once. Reassess rent affordability after introducing penalties; do not leave the existing economy accidentally unwinnable. On exposure, show the witnessed evidence and offer a retry path. Do not erase the player's entire career as an incidental implementation choice.

## Delivery order and acceptance

1. Jump, solid collision, and NPC path/avoidance repairs. Verify on the current map.
2. One excellent complete cake challenge with recovery, hands, and feedback.
3. Coordinator observation, questioning, and one working cover-up/distraction.
4. Expanded map and first usable prop set, with navigation and performance checks.
5. Seating and audio challenges, then shift phases, CV rewards, and replay variations.

This ordering is not permission to stop after stage one. Complete the requested build in reviewable stages and report any actual blocker. Preserve working features and check repository instructions before changing files. Do not claim network support from authority interfaces alone; verify actual transport and test available player counts.

Verify with a full fresh apartment → CV → wedding → payout run and a repeat shift:

- Player cannot walk through chairs or NPCs. NPCs do not walk through furniture or trap the player.
- Jump behaves under low ceilings, on edges, and while carrying props, without double jumping or map escapes.
- A CV claim changes a playable requirement and a reward, not only dialogue.
- A novice can understand each task from in-game information and recover from one mistake.
- Hidden visual mistakes do not trigger impossible sight detection; obvious witnessed mistakes do.
- Skillful play produces measurably better quality/pay than careless play.
- At least 12 distinct optional/utility props actually respond to interaction.
- Every new room has a purpose and reachable routes; essential props remain recoverable.
- Simultaneous pickup, task completion, inspection, and payout cannot duplicate ownership, incidents, or money.
- Test collision, jumping, carry stability, and frame pacing at representative low/high frame rates. Report measured performance and scene conditions; do not invent a universal FPS promise.

Add focused tests for suspicion occlusion/event deduplication, once-only payout, valid seating generation, and progression recovery where appropriate. Use actual gameplay inspection for collisions, clarity, physics feel, and comedy. Supply a short capture demonstrating a skilled success, a mistake-and-recovery sequence, and optional prop play. Document what shipped and what remains incomplete.

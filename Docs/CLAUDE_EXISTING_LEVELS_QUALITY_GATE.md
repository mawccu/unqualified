# UNQUALIFIED — finish the existing levels before expanding

## Mandate

The user wants a substantially more detailed and rigorous quality pass before any new levels. They should not have to act as the only tester or identify every misplaced asset. Pause new professions, new gadget families, and career expansion. Finish the apartment, wedding venue, and call center as a cohesive playable demo.

This document strengthens the earlier polish briefs. Preserve their specific requirements, including tray grips, intersecting books, right-side call controls, translator eligibility, call-center onboarding, the board/window collision, and Dana's character consistency. Preserve working mechanics and the user's progress.

Aim for consistently high quality across what is visible and playable. Do not promise literal perfection or claim a release is ready without evidence. Treat unverified work as unverified, not as passed.

## 1. Establish a reproducible baseline

Read repository instructions and inspect the latest project; do not assume previous briefs were completed. Record the commit or working-tree state, Unity version, scenes tested, quality settings, resolution, and test-save setup. Preserve concurrent edits. Back up the user's save before any test that could alter or reset progress; preferably use isolated test profiles.

Create/update `Docs/POLISH_AUDIT.md` with stable issue IDs. Each entry includes severity, exact location/object path, reproduction steps, expected/observed behavior, evidence path, confirmed root cause when known, affected shared assets, fix, and retest outcome. Distinguish pre-existing issues from regressions.

Keep a separate checklist of inspected areas, including areas where no defect was found. A list containing only user-reported bugs does not prove an independent audit.

Audit scene-generation/editor build scripts as well as saved scenes and prefabs. A manually corrected placement that reverts on the next build is not a finished fix.

## 2. Room-by-room inspection method

For each room, do a slow perimeter walk, cross the center, approach each station, crouch, look up/down, and inspect reachable surfaces. Repeat relevant observations while carrying a representative large object. Inspect at actual player scale, without editor gizmos concealing defects.

Check the following for every recurring asset family and every unique focal object:

| Area | Required inspection |
| --- | --- |
| Placement | Correct floor/table/shelf support; no hovering, unintended intersections, duplicates, or buried geometry. |
| Scale | Doors, chairs, desks, handles, tools, and people fit a coherent world scale and reachable interaction range. |
| Surfaces | No z-fighting, flipped normals, missing visible backfaces, stretched UVs, or abrupt material changes. |
| Materials | Fabric, paper, glass, plastic, wood, and metal read distinctly within the stylized direction. |
| Collision | Matches occupied volume; no giant invisible blockers, walk-through furniture, or clipping hand/prop routes. |
| Lighting | Contact reads clearly; critical objects are visible; no unexplained light leaks or extreme bloom hiding details. |
| Navigation | Players and NPCs can use doors, aisles, seats, and stations without becoming trapped. |
| Interaction | Prompt matches the object/action; reachable, not through walls; completes and cancels cleanly. |
| Persistence | Correct state after repeated interaction, scene return, and reload. |

### Apartment

- Reproduce the reported takeout-container floating issue after spawn, pickup/drop, and reload. Verify its pivot, model bounds, collider, parent, rigidbody state, and placement/restoring code.
- Mattress rests on its support; pillows and sheets have intentional contact and fabric response. Fix visible primitive geometry and stretched textures where they undermine a focal view. Keep couch/bed pose, player eye position, and standing transition aligned.
- Computer, keyboard, mouse, chair, and player hands agree spatially during use. Camera never enters the monitor or chair. Leaving restores normal movement/look.
- Landlord fits the doorway, looks at an appropriate target, and visibly transfers one bill. Paper changes ownership at contact without jumping, duplicating, or becoming unrecoverable.
- Workbench parts, tools, delivery parcels, and equipment rack have defined resting places. Inspect incomplete and completed assemblies, empty bins, and delivered packages, not only the ideal staged state.
- Window opens and closes through a plausible hinge/slide mechanism with a handle action, collision, and ambience change. Check any curtain, nearby prop, and exterior boundary. No window action should place the player outside unfinished geometry.
- Keep the room poor and lived-in through intentional design, not technical defects. Clutter should feel placed by inhabitants, not randomly scattered.

### Wedding venue

- Chair feet contact the floor; chair backs and seats block movement appropriately; tablecloths do not conceal major collider errors.
- Cake tiers, tray, stand, flowers, signs, and cables sit correctly and remain stable after interaction. Held props respect doors and corners.
- Guests use reachable seats; bodies do not intersect chairs/tables or face through walls. A moved chair triggers a sensible alternative instead of broken pathing.
- Coordinator routes remain valid after room changes. Inspect pauses, turns, yielding, observation sightlines, and actual witnessed evidence.
- Stage, sound desk, prep room, storage, and service routes have readable purposes. Every essential tool can be found without an external explanation.
- Check partial success, damage, repair, cleanup, and end-of-shift states. The room must still function after players make a mess.

### Call center

- Fix the board intersecting the window. Put it on suitable wall space or believable support; verify frame/glass clearance, readability, reach, and reload/rebuild persistence.
- Separate each reference book/binder with its own placement. Check reading and returning repeatedly so there are no duplicate models, shared transforms, or merged held books.
- Desk screens, telephones, headsets, keyboards, cables, partitions, printers, and chairs occupy plausible space. Verify seated and standing views.
- Give reference books legible purposes that match the actual content; policies must agree with the implemented cases.
- Inspect every desk and every NPC, not just the player's starting desk. Verify coworkers sit, stand, turn, answer, and move without clipping.
- Supervisor sightlines, investigation paths, and listening states must correspond to what the player can understand. No observation through solid walls.

## 3. Character identity and presentation

Create a compact character specification table for each recurring named NPC: role, intended identity/pronouns if defined, model/prefab, outfit/materials, voice, dialogue set, scale, and animation profile. Verify the runtime instance matches the table.

Dana specifically needs this check. Do not infer identity from the name alone. If the design specifies a woman, correct any accidental generic male asset/voice assignment. If unspecified, document a deliberate proposed characterization. Do not claim a model mismatch was proved without checking the source intent.

Named NPCs should be recognizable through silhouette, face/hair, outfit, posture, and behavior. A nameplate on the same unmodified placeholder is insufficient for important characters. Keep designs stylistically consistent and avoid exaggerated gender stereotypes as a shortcut.

Review skinning and joints, expression readability, gaze targets, mouth/voice timing where supported, idle variation, walk speed/foot cadence, and seated contact. NPCs should pause between gestures and turn before walking away rather than slide sideways or point continuously.

Verify subtitle speaker names and audio assignments against the actual speaking character. Ensure prefab fallback or spawn code does not undo the intended character asset.

## 4. First-person hands and physical performance

Build an inventory of grip profiles and representative props. For each, inspect idle, reach, contact, carry, acceleration, turning, crouch, jump/landing, obstruction, placement, and release.

The tray is the priority: palms support a credible underside/handle position; fingers/thumbs fit its edges; both hands remain attached as the tray moves. An IK marker near the model is not proof of contact. Inspect rendered geometry from player view.

Resolve carry collision/position before solving arms. Avoid independent late sway or animation overwriting that separates wrists from object anchors. Limit reach and provide natural elbows. If hand geometry cannot produce the grip, revise it rather than conceal it with a camera angle.

Other required profiles: paper pinch, open book/binder, cup, tool, handset/headset, and workbench action. Use an object-appropriate pose rather than the same fist for everything.

Look for sleeve gaps, wrists twisted backwards, fingers embedded in objects, floating palms, forearms crossing, hands through walls, double-rendered arms, bad shadows, and weapons-style posing on household objects. Check supported FOV limits and representative aspect ratios.

For handovers and assembly, stage anticipation → contact → ownership/state change → release/settle. Keep one authoritative object throughout. Test interruptions before and after the commit point. Pause, scene travel, cancelled interactions, and disconnect must restore valid control, object, and hand states.

Do not lengthen common actions unnecessarily. Short responsive animations with believable contact are better than an elaborate sequence players must watch every time.

## 5. UI hierarchy and call-center comprehension

The player is pretending to be qualified; they should not be fighting an incomprehensible interface. Preserve job-solving difficulty while removing ambiguity about controls, current state, and information sources.

### Consistent interface standard

- Define typography roles, spacing, panel contrast, button sizes, focus style, and status colors once and apply across screens.
- Use clear labels and concise text. Details are available on demand instead of always occupying the screen.
- Critical actions cannot be below an unscrollable boundary, obscured by another panel, or too small to read at supported resolutions.
- Support sensible focus order, visible selection, disabled-action reasons, and clean escape/back behavior. Opening menus must suppress conflicting gameplay inputs.
- Avoid text-only clutter replacing physical feedback. Eliminate duplicate prompts, repeated tutorial blocks, and notification piles.
- Test long strings and right-to-left content. Do not solve overflow by shrinking all text.

### Computer browser

Keep listings, CV, and shop distinct. Preserve vertical scrolling with mouse wheel and scrollbar drag. Header/navigation/log-off stay accessible. Reset scroll appropriately on page changes without resetting a user's draft or cart. Expanded descriptions and error messages must remain reachable.

Clearly show selected claims and consequences, final purchase total, remaining funds, and rent obligation. Repeated input cannot create duplicate application, order, or payment operations.

### Call-center arrangement

Use left/center for conversation and case workspace. Put telephone controls in a stable right-hand rail: call status/language, answer when ringing, mute/unmute, hold/resume, transfer, and separately positioned end-call control. Keep them available while scrolling conversation/history.

Mute stops the customer's hearing; hold pauses the exchange. These must have distinct feedback. End call is not resolve case. Contextual task responses belong in the case workspace, not mixed indiscriminately with phone controls.

Summarize facts the player has actually learned: customer/order identifier, symptoms, attempts, and unresolved question. Preserve history and important numbers. Allow copying known identifiers into lookup rather than testing transcription endurance.

Search failures identify what was searched and how to correct it. A disabled action explains the missing prerequisite. Feedback explains mistakes specifically. “Find the order” is a useful next-goal cue; revealing the correct remedy automatically is not.

### Teaching and pacing

Provide one short playable training call in the baseline supported language. Guide answering, asking for an identifier, lookup, reference use, choosing a remedy, confirming, and closing. Show one step at a time, with generous patience and no surprise supervisor penalty. Allow replay/skip.

The next calls remove step-by-step guidance but keep coherent state and optional help. Introduce claimed-language routing explicitly after the basic loop. Reduce simultaneous demands during onboarding; tune patience from actual reading/navigation times, not developer familiarity.

Verify a novice can solve cases entirely from in-game information. Without access to a first-time human tester, report that limitation and provide a self-contained walkthrough of where each necessary clue is presented. Do not equate memorized developer completion with comprehensibility.

## 6. Translation is deliberate, not an accidental free power

Trace every text source and translation path, including transcript, subtitles, phrase aids, gadgets, saved preferences, and transferred calls.

- Default gameplay without an active translator must leave customer speech in the customer's language. CV claims influence routing, not real player fluency.
- Equipped, active, charged hardware grants only its implemented/documented assistance. Ownership of a device left at home is insufficient.
- An accessibility translated-caption option, if retained, is explicit, separately labeled, and opt-in; it is not an unexplained normal-gameplay toggle. Preserve intentionally chosen accessibility preferences.
- General UI actions remain in the player's UI language. A phrasebook may explain generic terms without silently translating the entire live call for free.
- Define behavior for translated history after device depletion rather than leaving stale live translation through a bug.

Test no device, owned-but-not-equipped, charged active device, depleted device, explicit accessibility mode, call transfer, language change between calls, and reload. State and labels must explain why translation is present.

## 7. Audio and feedback finish

Review sounds at player distance and during concurrent events. Dialogue and critical clues remain intelligible. Impact sounds match material and meaningful collision; settled props should not repeatedly clatter. Footsteps follow actual movement/contact, not held input against a wall.

Window ambience, room tone, equipment hum, paper rustle, phone states, and workstation actions should feel local and connected to visible events. Avoid constant competing loops. Provide visual equivalents for important audio-only information.

Respect master/category volume and camera-motion settings across scenes. Verify pause/resume, subtitle length, interruption priority, hold/mute audio behavior, and unloading so sounds do not continue from an abandoned scene.

## 8. State, economy, and regression checks

Test fresh and existing saves, late-stage saves, repeated shifts, purchases, unfinished crafting, device use, and payout. No duplicate money or lost paid equipment. Transactions and recovery must remain consistent after interruption.

Exercise rapid repeated input, simultaneous interaction where supported, disconnect, menu opening mid-action, scene travel, and reload. Keep test saves isolated from the user's real progress.

Run focused automated checks for transaction/state bugs, translation eligibility, call routing, and progression. Visually verify physics, layout, grip contact, character identity, and sound. Report actual multiplayer coverage; architecture intended for networking is not evidence of a successful multiplayer session.

Profile populated scenes under recorded hardware/settings. Compare frame-time behavior before/after changes. Fix measured bottlenecks rather than lowering everything indiscriminately. No unqualified FPS or release-readiness claims.

## 9. Evidence and delivery workflow

Work in small passes, verifying shared changes across all affected scenes:

1. Reproduce and capture the user-reported defects; inspect for related errors independently.
2. Fix data/progression/interaction failures and repeated exceptions.
3. Fix spatial placement, collisions, and character identity.
4. Fix grips, motion, materials, and lighting consistency.
5. Simplify UI, correct translation behavior, and improve onboarding.
6. Verify audio, performance, persistence, and complete job cycles.

Maintain `Docs/Polish/Before` and `Docs/Polish/After` with matching-view comparisons. Record camera/FOV and conditions so lighting or framing cannot conceal a defect. Add short motion captures for hands, physics, scrolling, handover, and call flow.

For each completed issue, record reproduction before, fix, and actual retest after. If a root cause is unresolved, keep it open. If a previously working feature regresses, reopen the relevant issue.

Deliver `Docs/POLISH_DELIVERY.md` with inspected areas, changed assets/systems, test conditions/results, evidence links, remaining limitations, and an explicit pass/fail for each gate below. Update the asset register for new/replaced art and retain editable sources.

## Exit gates — no new levels until these are met

| Gate | Required evidence |
| --- | --- |
| Reported defects | Tray grips, merged books, board/window, Dana consistency, translation behavior, and call controls verified in the running game. |
| Scene quality | Apartment, wedding, and call-center inspection checklist completed; known obvious placement/material defects resolved or explicitly listed as remaining blockers. |
| Interaction quality | Contact, collision, interruption, and repeated use work across representative prop families and important stations. |
| UI quality | Required controls accessible at tested resolutions; scrolling/focus valid; localized content readable; phone controls on the right. |
| Gameplay clarity | Training and ordinary calls solvable with in-game information; clear distinction between mechanic knowledge and intended task difficulty. |
| Progression | Both existing jobs complete from apartment through payout and return, with repeat use and reload verified. |
| Reliability | No known save-loss/duplication/progression blocker or recurring unexplained runtime exception. |
| Reviewability | Updated issue log, matched before/after evidence, short full-flow captures, and honest remaining-issue report. |

Known serious blockers mean the milestone is incomplete. Small cosmetic leftovers may be reported for user review, but do not silently redefine them as fixed. The result should withstand an ordinary player looking around and interacting freely—not merely a carefully staged demonstration route.

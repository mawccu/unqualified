# UNQUALIFIED — playable opening and fake CV

## Direction from the user

Focus the next work on gameplay, character animation, and the reason the players take these jobs. The game starts in a terrible apartment. The landlord confronts the players, threatens eviction, and hands them a bill. The player then goes to their desk and creates a fake CV. The wedding shift is the first job reached through this opening.

This file is a handoff proposal, not a report of implemented features. Dialogue, timings, money amounts, and interaction details below are suggested defaults. Preserve existing Unity work and integrate with it. Prioritize a complete playable opening over another venue art pass.

## The emotional arc

We are broke → rent is due → we have no qualifications → we lie → somebody actually hires us → now we must deliver what we claimed.

The apartment should establish friendship, desperation, and comedy through things players do. Avoid a long introductory movie. Aim for roughly 3–5 minutes on a first playthrough, with a short route on repeat visits.

## 1. Wake up in the apartment

Start at player eye level on a sagging couch or bad mattress. A short sit-up animation brings the player into control. Other players spawn at distinct sleeping spots in the same apartment. Solo play must work without AI roommates.

Use a compact, navigable layout: shared sleeping/living area, kitchen corner, front door, and one battered desk with a computer. The desk should be visible or easy to find from the entrance. A handful of readable props is enough: unpaid envelopes, empty food containers, a leaking bucket, a dead plant, and a cheap chair.

Introduce movement naturally. Someone pounds on the front door. Objective: “Answer the door.” Let players explore and pick up harmless clutter; opening the door advances the scene. After a delay, repeat the knock and a muffled line rather than stalling silently.

## 2. Landlord confrontation

Keep this an in-world exchange. The landlord occupies the doorway, looks toward the interacting player, points into the apartment, and presents a bill. Players retain camera control. Do not teleport everyone into a cinematic or let the landlord trap someone against geometry.

Suggested short dialogue:

- Landlord, knocking: “I know you're in there. I can hear the microwave.”
- At the door: “Seven days. Pay the rent, or you're out.”
- Presenting the bill: “And stop putting ‘entrepreneur’ on the payment reference.”
- After the player accepts it: “Try getting a job. Apparently people do that.”

Show subtitles. Recorded voice is optional; the scene must function without a voice service. Give the player a clear “Take bill” interaction. The landlord holds the offer pose until somebody accepts; an impatient idle can loop without repeating the whole speech.

The bill is a physical prop first, with a readable inspection overlay when held. Suggested starting balance: $0. Suggested deadline: seven in-game days. Keep rent and wages configurable; choose the actual rent after checking whether the available jobs make the target achievable. Do not start a real-time seven-day countdown during the intro.

Receiving the bill sets the shared debt/deadline once. It can then be placed on the desk or pinned beside it. Dropping it must never block progress: the HUD and desk retain the debt information. The landlord leaves and the door becomes usable again.

## 3. Go to the desk

Objective: “Find something that pays.” Interacting with the desk opens a deliberately cheap job website on the computer. Prefer a readable screen interface and a short camera transition over an elaborate computer simulation.

Show the wedding crew vacancy as the first available job. Other professions may appear as clearly unavailable listings, but do not promise playable jobs that do not exist yet. Show pay, shift duties, and required experience before applying.

The punchline is visible: the employer wants experienced staff; our CV is empty. “Apply” leads into the CV editor. Do not make the player type paragraphs just to begin the game.

## 4. Build the fake CV

Make lying an actual choice. Give the player a scrappy CV template with an editable name, a role title, experience choices, and selectable skill claims. Use authored options for the first implementation; generative AI and microphone interviews are not dependencies.

The truthful option should remain funny and usable: “No experience. Available immediately.” Let it lead to basic temp work rather than a dead end. Players who exaggerate should see what they are committing to.

Initial claims must map to mechanics that actually exist:

| CV claim | Consequence in the first shift |
| --- | --- |
| “Professional wedding cake specialist” | The coordinator explicitly assigns you the cake display and expects you to handle it. |
| “Experienced seating coordinator” | The coordinator explicitly assigns you table signs and seating setup. |
| “I work well under pressure” | Flavor dialogue initially; label it as flavor in implementation notes rather than inventing an unbuilt system. |

Only offer an audio-engineering claim once the audio task is playable. Avoid a menu full of promises with no gameplay consequences.

Show a live CV preview as claims are added. On submission, save structured claim IDs, not only display text. Show a brief sending state followed by an authored acceptance: “You're hired. We were impressed by your extensive experience.” Objective: “Get to your first shift.”

For this slice, use one shared crew application. The applicant edits while roommates can watch the current draft. A second player cannot overwrite the form concurrently. If the applicant disconnects or exits, release the interaction lock and preserve the draft. Individual CVs can come later.

## 5. Leave for work

The front door now offers “Start wedding shift.” In multiplayer, use a visible crew-ready prompt so one player cannot unexpectedly pull everyone out of the apartment. Solo starts immediately. Use the project's existing session/scene flow where available; do not replace the networking stack just for this opening.

Carry the crew's chosen claims, debt, and day into the existing wedding scene. At least one coordinator line must reference an actual selected claim: “You're the cake expert, right? Good. That's yours.” This callback is the minimum proof that the CV mechanic matters.

Eventually the shift returns players to the apartment with wages, rent progress, and new listings. For this implementation, first finish and verify apartment → CV → existing wedding shift. Keep the state compatible with that return loop without building the entire career system now.

## Animation and interaction priorities

Deliver visible poses and transitions, not just animation clip names or log messages:

1. Player: brief wake/sit-up, bill receive/inspect, desk interaction, and a clean exit back to movement.
2. Landlord: knock, impatient idle, talk gestures, point, offer bill, release bill, turn, and leave. Facial animation can be simple; body language must communicate the exchange.
3. Roommates: existing locomotion, readable idle, and held-object poses. Ensure the bill and carried props sit in hands rather than floating through wrists.
4. Coordinator: a directed greeting or pointing gesture accompanying the callback to the CV claim.

Use animation events or explicit timed interaction markers for the handover. Until the acceptance marker, the bill belongs to the landlord. After it, it belongs to the player. Never duplicate it or leave it attached to both characters. Blend back to idle after interruptions. Local camera/hand presentation and third-person poses should agree about whether the player is holding something.

Do not block the slice on perfect finger IK, lip sync, ragdoll systems, or custom animations for every incidental object. Make the core handover and desk interaction convincing first.

## State and reliability

Use explicit progression: intro → door answered → bill received → desk available → CV draft → submitted → hired → ready to leave → wedding shift. Keep transitions idempotent and persist the accepted application before scene travel.

In networked play, the host owns shared progression, debt, claim submission, and scene travel. Synchronize presentation for other players, and give late joiners the current state instead of replaying the landlord sequence. Keep UI input from also moving the character. Restore controls reliably after closing the desk, disconnecting, or changing scene.

Check the existing project architecture before choosing class names or creating duplicate managers. The prior scene and gameplay code may already provide interaction, save, UI, and networking facilities.

## Delivery and acceptance

Build in this order: playable apartment route with the current interactions; landlord and bill handover; CV choices and stored consequences; transition and coordinator callback; animation polish and multiplayer checks.

Demonstrate the full sequence in Play Mode. A scene that only looks like an apartment is not completion. Verify:

- A solo player can start in the apartment, receive the bill, submit a CV, and enter the wedding shift without editor intervention.
- The landlord visibly hands over the bill; dropping it cannot soft-lock progression.
- Chosen claims survive scene travel and change at least one assignment/greeting.
- Two players can observe the same handover and application; simultaneous interaction does not duplicate events.
- Leaving the desk restores camera and movement; disconnecting the editor of the CV releases the desk.
- Repeat play does not require watching an unskippable introduction again.

Report what is playable, what is mocked, and what still needs testing. Supply a short gameplay capture showing the opening through the first shift callback. Prioritize this loop over floor seams, bloom tweaks, extra professions, or more decorative props.

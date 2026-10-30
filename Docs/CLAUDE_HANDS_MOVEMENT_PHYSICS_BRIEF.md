# UNQUALIFIED — hands, movement, physical interactions, and cinematics

## User direction

The apartment opening works. Preserve it and improve how it feels: visible player hands, richer movement animation, believable object physics, a realistic landlord bill handover, and more short cinematic moments. Keep the exaggerated stylized characters. Physical believability means contact, weight, anticipation, and follow-through; it does not require photorealistic art.

This is an implementation brief, not a claim that these features exist. Durations and motion ranges below are tuning starting points. Complete the core interactions in Play Mode before adding incidental flourishes.

## Starting points observed in the current code

- `PlayerActor` uses a CharacterController, hides its avatar using `SetShadowOnly(true)`, and supplies a camera-relative HoldPoint. It already has camera overrides and control locks.
- `PickupProp` makes held objects kinematic, disables their colliders, follows their position directly, and adds visual yaw/tilt lag. Released objects become dynamic.
- `Landlord` uses HandL/HandR with hard-coded offsets for the bill. `Use` calls `OpeningDirector.TakeBill`; verify that implementation before changing transfer timing.
- Reuse InteractionService, ownership revisions, the opening progression, and existing animation facilities. Inspect the current files again because another agent may have changed them since this brief was written.

## 1. Build actual first-person hands

Deliver a skinned pair of stylized forearms and hands with sleeves matching the selected worker. They must have readable thumbs and finger shapes, with enough articulation for a paper pinch, open palm, loose fist, keyboard pose, and two-handed support. Inspect the actual hand meshes and skeleton: if they cannot form these poses, create or revise the asset. A renamed clip or rotated mitten is not a completed grip.

Use a dedicated local first-person rig if the full-body proportions do not work near the camera. Remote players still see the world avatar performing the corresponding action. Keep action phase, grip, and object identity consistent between those views. Avoid duplicated arms or shadows. Keep hands integrated with scene lighting; do not render them through walls as a workaround for clipping.

Hands should enter the frame when needed. At idle, relaxed hands can sit low at the frame edge; do not permanently obscure the center. Use subtle breathing and pose variation, not constant shaking.

Create named grip anchors on props: paper pinch, mug handle, one-handed small object, and left/right supports for a cake or tray. Solve arm reach toward those anchors with reasonable elbow direction and reach limits. Clamp unreachable targets and reject interactions beyond physical reach. The current long aim distance may show a prompt, but must not produce three-metre arms or remotely snatch a bill.

Minimum first-person actions:

| Action | Visible performance |
| --- | --- |
| Empty idle / walk / sprint | Relaxed hands, alternating arm swing, stronger but controlled sprint swing. |
| Reach / pick up | Hand opens, approaches the correct grip, closes, then lifts. |
| Hold paper / inspect | Thumb and fingers pinch an edge; second hand supports it when inspecting. |
| Carry small prop | Wrist follows its grip with modest inertia. |
| Carry cake or tray | Both palms support it; elbows soften; small balancing corrections. |
| Place / release | Hands lower the object to the surface, release, then withdraw. |
| Door handle | Fingers approach the handle, turn or push it, then release before stepping through. |
| Desk use | Pull or settle into chair, hands on keyboard/mouse, short typing bursts. |

A single general-purpose pickup animation is insufficient for paper, a mug, and a wide cake. Build a small reusable set of grip profiles instead of bespoke logic for every prop.

## 2. Movement that has weight and remains responsive

Keep the stable character controller. Do not turn the player into an uncontrolled rigidbody just to add realism.

- Blend idle, walking, sprinting, strafing, and walking backward from actual movement speed and direction. A blocked character must not keep running in place.
- Add short start and stop transitions, restrained torso lean on acceleration/braking, and turning in place. Match foot cadence to distance travelled to reduce skating.
- Separate upper-body carrying from lower-body movement. Hands stay on the prop while legs walk; an interaction must not reset the entire body to idle.
- Add a crouch with capsule clearance checks before standing, suitable for retrieving a dropped bill or object. Keep the camera transition smooth and physically inside the capsule.
- Small steps and short falls get a brief knee/arm settle and a footfall. Do not add a new jump mechanic unless the project already supports it or gameplay needs it.
- A large carried object can modestly reduce sprint speed and trigger balancing poses. Avoid making routine movement frustratingly slow.
- Use restrained camera motion: approximately 0.5–1.5 cm walking bob as an initial range, no compulsory camera roll, and no mouse-input lag to imitate weight. Put most inertia in hands and props.
- Provide camera-bob/shake strength including zero. Scripted camera motion and gameplay sway must not stack uncontrollably.

Add surface-appropriate footstep sounds tied to contact timing, plus sleeve movement and exertion sparingly. Footsteps should stop when actual travel stops.

## 3. Rebuild the bill exchange around contact

This is the priority showcase. The paper must visibly travel from the landlord's fingers to the player's fingers, with no instant jump between hold points.

Use a bill mesh with a little thickness or double-sided material, a gentle bend, and named grip anchors. Prefer a controlled bend rig or shader for flex and flutter; full cloth simulation is unnecessary. The text remains readable in inspection mode.

Suggested sequence after “Take bill”:

1. **Ready:** landlord holds one edge at a comfortable reachable height, wrist turned toward the recipient. His gaze shifts from their face toward the paper. The player must be nearby with an available hand; otherwise show why the interaction cannot start.
2. **Reach, approximately 0–0.35 s:** the player's open hand approaches the opposite edge. Blend into a staged interaction pose without teleporting their body or abruptly aiming their camera.
3. **Contact, approximately 0.35–0.55 s:** thumb and fingers close onto the paper. Both characters visibly touch it. The bill still has one authoritative owner.
4. **Transfer marker, approximately 0.55 s:** validate and commit the handover exactly once. Preserve the bill's world pose when changing its attachment; the landlord's fingers then open. A quiet paper rustle lands at contact.
5. **Retract, approximately 0.55–1.0 s:** the player draws it toward their chest while the landlord withdraws his empty hand. Paper flex and wrist settling finish the motion.
6. **Read, approximately 1–2 s:** the second hand supports the sheet and the amount becomes legible. Let the player dismiss inspection promptly. The landlord can deliver his final line during this moment.

Use an explicit interaction transaction: reserve → reach → contact → commit → retract → complete. Before commit, cancellation returns the landlord to his offer with the bill still his. After commit, cancellation preserves player ownership and resolves to a safe hold or drop. Animation callbacks must not be the only recovery path; handle interrupted clips, pausing, skipping, scene changes, and disconnects.

Lock duplicate pickup/drop requests during the brief transfer. Allow limited natural looking where it does not break reach, and release any locomotion lock promptly. If both players press interact, only one gets the bill. Other players see the same contact and release. Do not fake this with a full-screen bill image appearing while the world bill vanishes.

## 4. Physical objects with distinct behavior

Give props per-category mass, friction, bounce, center-of-mass, and grip settings, exposed for tuning. Paper should not fall like a brick; mugs should not bounce like rubber; a cake should feel heavier than a sheet of paper.

- **Paper:** controlled flutter and mild drag when dropped, a thin robust collider, and a readable resting orientation. Approximate paper behavior rather than using unstable ultra-thin colliders or extreme mass ratios.
- **Mug / small objects:** a small amount of spin on release, appropriate impact audio, and stable resting contact.
- **Cake / tray:** supported by two hands, modest tilt under acceleration, more deliberate stopping, and a gentle surface-placement action. Preserve current task/socket rules; do not randomly destroy progress as part of this animation pass.
- **Furniture:** anchored or sufficiently constrained. Players should not casually kick the desk through the room.
- **Doors:** constrained opening arc and obstacle checks; hands release before the door moves beyond their reach. Avoid launching players with a simulated door.

Implement obstacle-aware carrying. Sweep the held object's approximate volume toward its desired pose, shorten the reach or lower the arms when obstructed, and keep it on the near side of walls. Hands follow the resolved object pose. Test corners and narrow doorways. Do not simply enable colliders on a transform-teleported kinematic object and assume collisions will resolve themselves.

Use one deliberate carry model per category. A collision-aware kinematic carry is acceptable for precise tasks; a tuned joint-driven carry is an optional later choice for larger chaotic props. If adding joints, cap forces and break conditions so the object cannot launch the player or vibrate violently.

Dropping preserves reasonable player/hand velocity with limits. Drop and throw are distinct actions; normal drop must not accidentally fling an object. Placement should ease toward a valid surface and release at contact, without a long visible snap. Free props settle and sleep. Use appropriate continuous collision settings for fast-moving props and test for tunnelling.

Sound follows physical events: paper scrape, ceramic tap, wooden thud. Scale impacts with meaningful collision energy and suppress repeated sound spam from resting contacts.

## 5. Short playable cinematic moments

Expand the opening with brief embodied actions. Prefer the player's viewpoint. Preserve movement and looking during dialogue except when a short physical action needs a stable position.

| Moment | Direction | Approximate first-play length |
| --- | --- | --- |
| Wake up | Hands push off the couch; body sits up; feet plant; camera settles at standing eye height. Knock motivates getting up. | 3–4 s |
| Open door | Hand finds handle; door opens; landlord is revealed outside, glances into the apartment, then faces player. | 1–2 s plus dialogue |
| Bill exchange | Perform the contact-based handover above; readable bill is the reveal. | 1–2 s plus optional reading |
| Sit at desk | Chair shifts slightly, body settles, hands find keyboard and mouse; blend into the usable screen. | 1–2 s |
| Submit fake CV | Finger presses Enter; a short waiting beat; acceptance arrives; restrained relieved hand gesture. | 2–3 s |
| Leave for work | Hand opens apartment door; short fade/load; arrive at wedding entrance with a nervous sleeve adjustment. | 2–3 s excluding load |

Give the landlord variation: breathing, weight shift, tapping the bill, glancing at the apartment, eyebrow/head reaction, and a turn before walking away. Add pauses between gestures. Do not loop large pointing gestures continuously or slide his feet during speech.

On repeat play, allow skipping intro performances to their valid end states. Skipping must still complete necessary ownership/progression exactly once. Never force other players into the interacting player's camera. Roommates see their world animation and can continue observing; shared scene travel still follows the crew-ready flow.

Use Timeline, Animator, or the existing director where appropriate to the project. Choose one owner for camera overrides, animation overrides, and control locks, with cleanup on completion, cancellation, disable, and scene unload. Avoid competing coroutines leaving controls locked or the camera inside a couch.

## 6. Implementation order and proof of completion

1. First-person hand assets and grip poses, with a convincing bill receive/inspect sequence.
2. Pickup, two-handed carry, placement, drop, and wall obstruction behavior.
3. Locomotion blends, footsteps, crouch, starts/stops, and animation layering.
4. Wake, door, desk, acceptance, and departure performances with skip/recovery.
5. Remote-player presentation and authority checks if multiplayer transport is implemented. Existing host-oriented interfaces alone are not proof that multiplayer works; report what was actually tested.

Deliver gameplay captures: landlord handover from first person and an observer view; walking/sprinting with empty hands and a cake; placing/dropping objects near a wall; desk interaction; skipping and restarting the opening.

Acceptance checks:

- Fingers meet the paper before release; there is one bill throughout and no visible attachment jump.
- Hands match held-object grips at different look angles and supported FOV settings; sleeves and wrists do not open into obvious gaps.
- Objects cannot be carried or placed through a wall; rapid turning does not produce explosive physics.
- Movement remains responsive at 30, 60, and high frame rates, with no growing prop spring oscillation.
- Repeated take/place/drop inputs cannot duplicate ownership or leave a prop stuck.
- Pause, skip, interruption, and scene travel restore controls and valid camera/hand states.
- The existing CV and wedding progression still complete normally.

Add focused automated checks for the handover transaction and interruption cleanup where practical; use actual visual inspection for contact, clipping, animation quality, and camera comfort. Report remaining limitations honestly. Deliver working assets and visible actions, not only infrastructure, animation names, or a document describing future work.

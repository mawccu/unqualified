# UNQUALIFIED — full-game quality and polish milestone

## User direction

Pause major feature development. The user reports numerous visual and gameplay issues, including an apartment takeout container floating in the air, weak mattress/bedding visuals, and excessive text/UI in the browser and call center. They want the existing game cleaned up before expanding further. They also request an openable apartment window.

Do not ask the user to enumerate every defect. Independently inspect the entire build, maintain an issue list, fix problems, and verify the results. Preserve the current stylized comedy direction and working game systems.

The target is a coherent, polished playable demo. “Steam game level” expresses the desired quality; it is not a measurable certification or proof of release readiness. Do not claim the game is ready to release merely because this pass compiles or some screenshots improve.

## 1. Audit the whole experience before choosing fixes

Read repository instructions and inspect the current project. This brief does not assume every previously requested feature was implemented successfully.

Play the real player route:

1. Fresh apartment opening, movement, landlord, and bill handover.
2. Computer browsing, CV editing, language claims, job selection, and scrolling.
3. Parts shopping, delivery, assembly, loadout, and charging where implemented.
4. Wedding briefing, tasks, optional props, suspicion, recovery, and payout.
5. Call-center calls, language routing/transfers, references, supervisor checks, and payout.
6. Return home, save/reload, repeat shift, and previously created save migration.

Inspect rooms from normal player height, crouched, and close to interactable objects. Inspect moving and resting objects, not only arranged editor views. Record the actual scene/build version and test conditions.

Create `Docs/POLISH_AUDIT.md`. For each issue include ID, severity, location/system, reproduction steps, expected versus actual behavior, evidence, suspected/confirmed cause, fix, and verification result. Use statuses such as observed, fixing, ready to verify, verified, or blocked with a concrete reason. Never mark an untested change verified.

Severity:

- P0: lost/corrupt saves, duplicated/lost money, blocked progression, crashes.
- P1: broken core interactions, severe collision or animation problems, inaccessible UI, recurring runtime exceptions.
- P2: obvious visible defects, confusing feedback, weak materials, distracting audio, inconsistent presentation.
- P3: small cosmetic refinements.

Prioritize by impact and frequency. Audit representative instances of repeated props and fix shared assets/prefabs where appropriate, then check affected scenes for regressions.

## 2. Fix physical and interaction defects first

Investigate the floating apartment takeout box specifically. Determine whether it comes from model origin, collider bounds, placement height, an unintended parent/constraint, rigidbody state, or a script restoring the wrong pose. Fix its actual cause; do not simply hide or delete the reported object.

Inspect all floor/table/shelf props for visible support and sensible collision. Check resting positions after scene load, pickup, drop, placement, and save/reload. An item that looks correct only before interaction is not fixed.

Check furniture and NPC collision, door movement, player capsule clearance, jumping and landing, held-object wall obstruction, dropped items, and moving chairs. Remove tunnelling, explosive forces, snagging on oversized invisible colliders, and NPC paths through furniture. Keep aisles usable.

Verify interaction reach, prompts, object ownership, busy states, and interruption cleanup. Hands must not stretch to distant targets; props must not jump between incompatible attachment poses. Prevent duplicate pickups, purchases, crafting outputs, calls, and payouts.

Essential items and purchased gadgets must remain recoverable. Test items under furniture, in bins, near room boundaries, and during scene changes. Avoid silent deletion or duplication as a recovery method.

Recurring console exceptions are defects to investigate, not noise to suppress. Check warnings individually and fix relevant ones rather than hiding the console or globally disabling checks.

## 3. Establish and apply a consistent visual standard

Create a short `Docs/ART_QUALITY_STANDARD.md` based on the strongest existing assets. Define palette, silhouette language, scale, texture/detail density, material roughness, edge treatment, lighting, and UI type hierarchy. Keep the original stylized, slightly shabby comedy identity. More polygons or stronger post-processing alone do not establish quality.

Inspect the mattress/bedding specifically:

- Does the mattress have convincing thickness and contact with its support?
- Are sheets/pillows visibly intersecting, floating, excessively blocky, or stretched?
- Do UVs, normals, shading, and material response work at player distance?
- Does bedding look like fabric rather than shiny plastic or an untextured block?

Correct geometry, UVs, normals, texture scale, and materials as needed. Replace weak meshes when small edits cannot bring them up to the chosen standard. Do not cover broken geometry with darkness, blur, bloom, or camera restrictions.

Audit couches, chairs, tables, food packaging, shelves, workbench parts, screens, doors, windows, floors, ceilings, and walls. Correct inconsistent scale, seams, gaps, z-fighting, repeated obvious texture artifacts, missing backfaces, and awkward prop placement. Check contact shadows and reflection/roughness consistency. Avoid excessive bloom and unreadable dark areas.

Set dressing should explain the place and guide movement. Keep clutter intentional and critical objects readable. Do not solve emptiness by scattering hundreds of identical props.

For any new/replacement assets, retain editable source files and record provenance/licensing in the project's asset register. Use original or properly licensed material; public availability alone does not establish reuse rights.

## 4. Redesign information density, not just font size

Goal: the player understands the current situation and next action without reading a wall of instructions. Keep information accessible; reveal secondary details on demand instead of deleting essential clues.

Establish consistent panel spacing, typography, button sizes, selected/disabled states, color meanings, and navigation. Use familiar short labels and plain language. Avoid tiny text, excessive uppercase, unexplained abbreviations, or color-only status cues.

### In-world HUD

Show the immediate objective, relevant interaction prompt, and urgent feedback. Put longer task explanations/checklists in an expandable view. Avoid multiple panels repeating the same instruction. Notifications should expire or consolidate; do not cover the player's hands or the object being manipulated.

### Apartment browser, CV, and shop

- Separate job browsing, CV editing, and parts shopping into clear destinations.
- Job cards summarize role, pay, requirements, and one primary action. Details expand when requested.
- CV claims show a short consequence; lengthy explanations should not all remain open simultaneously. Keep the preview secondary to completing the form.
- Shop cards show item, price, ownership, and compatibility concisely. A cart gives a clear checkout total and remaining funds.
- Keep navigation/log-off accessible. Preserve the scrolling fix, including wheel input, scrollbar dragging, expanded content, and reaching actions at the bottom.
- Prevent keyboard/gamepad focus disappearing offscreen. Typing or scrolling the page must not move the player or trigger gadgets behind it.

### Call-center workstation

Use a stable hierarchy:

1. Active call: caller, language, live/hold/mute/transfer status, current speech, and essential patience/attention feedback.
2. Case workspace: customer/order lookup, discovered facts, and relevant records.
3. Actions: a small contextual set of responses, hold, mute, transfer, and resolve.

Full conversation history, policies, technical references, and supervisor notes belong in deliberate secondary views. Show customer speech in readable chunks. Do not replace the whole transcript so quickly that a clue cannot be recovered. Preserve all information needed to solve the case.

Keep translated subtitles and other accessibility assistance available. Verify Arabic shaping/right-to-left flow and mixed-direction order IDs, as well as longer localized text. Do not shrink text until it fits. Check focus, overflow, scrolling, overlapping windows, and confirmation states.

Test representative display conditions: 1280x720, 1920x1080, a narrower/resized Game view, and supported UI scaling. Test controller navigation only if the build advertises controller support; report coverage honestly.

## 5. Animation, sound, and response quality

Inspect waking, sitting/standing, landlord gestures, bill contact, hands at different FOV/look angles, carrying, placing, tool use, typing, headset/handset use, and doors. Correct wrist gaps, floating grips, foot sliding, excessive reach, abrupt pose changes, and competing camera overrides.

Every action should have readable anticipation, contact, and completion where useful. Keep common actions quick and responsive. Avoid adding long mandatory cinematics as a substitute for interaction polish.

Match sound timing and character to the event: paper rustle at contact, ceramic versus wooden impact, footsteps at actual travel/contact, appropriate equipment sound. Suppress repeated resting-collision audio. Balance UI, dialogue, ambience, and effects so clues and speech remain understandable.

Check subtitle duration, interruptions, stacked messages, muted/held call behavior, and pause/resume. Respect camera-motion and audio settings. Do not require hearing alone to understand a task state.

## 6. Add the apartment window as one bounded enhancement

After serious defects are fixed, make the existing apartment window usable. Inspect its model and choose a coherent opening mechanism: hinged or sliding, not both.

The player reaches the handle, opens the panel within its proper limits, and releases. Closed/open/obstructed states need clear prompts. Closing reverses the action. Fit colliders to the moving frame/panel and prevent it sweeping through the player or nearby furniture. Keep the exterior boundary safe without an unexplained collision plane across a visibly usable opening; choose an appropriate opening size, sill, or exterior barrier.

Blend outside ambience louder when open and quieter when closed. A subtle curtain reaction is optional if a curtain exists. Do not add an entire outside map, climb-out route, weather simulation, or complex cloth system for this interaction.

Handle repeated input, pause, scene reload, and obstruction without stuck animations or duplicate audio. Keep world state and visuals consistent. In networked play, all observers see the same window state.

## 7. Verify performance and reliability

Profile real populated scenes, moving NPCs, active calls, held props, and workshop interactions. Record resolution, hardware, quality settings, and measured frame timing. Identify the biggest bottlenecks before optimizing. No universal FPS promises based on an empty scene.

Look for unbounded object/audio/effect creation, allocations in hot loops, expensive UI rebuilds, unnecessary live physics, and overlapping transparent UI. Use focused fixes; do not lower the whole game's quality to hide one inefficient system.

Exercise fresh and existing saves, old-save migration, repeat jobs, rapid repeated input, menu open/close, cancellation, scene transitions, and reload during purchase/assembly/call handling. Protect the user's current save; use isolated test saves or a verified backup for destructive test scenarios.

Verify actual multiplayer where available, including joining/disconnecting and simultaneous interactions. Authority-looking interfaces or simulated actors alone are not proof of working multiplayer. If a test cannot be run, mark it unverified with the reason.

Use focused automated tests for state/transaction bugs and progression regressions. Visual contact, legibility, animation, sound, and game feel require actual Play Mode inspection. Compilation alone is not verification.

## 8. Work in reviewable passes

1. Baseline audit and evidence capture.
2. P0/P1 fixes: saves, progression, interactions, physics, recurring exceptions.
3. Asset/environment consistency, including the reported takeout box and mattress.
4. Browser and call-center information/interaction cleanup.
5. Animation/audio refinements and the window interaction.
6. Full regression playthrough, performance check, and final evidence.

Capture representative before/after views from the same position, FOV, lighting/time, and resolution where possible. Save them under `Docs/Polish/Before` and `Docs/Polish/After`. Include short motion captures for problems screenshots cannot prove, such as floating after pickup, handover, and scrolling. Do not alter staging merely to conceal an issue in the comparison.

Preserve concurrent work. Avoid broad scene regeneration, mass asset replacement, or architectural rewrites unless a specific verified issue requires them. Recheck related features after shared prefab/material changes.

## Completion gate and final handoff

The pass is complete only when:

- Reported takeout-box and bedding issues are reproduced or concretely investigated, corrected where present, and verified in gameplay.
- No known P0 or P1 remains unreported; blockers cannot be hidden behind a “polished” label.
- Audited visible props have correct support/collision and a coherent visual treatment.
- Browser/CV/shop and call-center screens are readable, navigable, and fully usable at tested display sizes, with no unreachable required controls.
- Window opening/closing works with hands, collision, and ambience.
- A complete apartment → job → payout → return cycle works for both implemented professions; repeated use and saving remain sound.
- Runtime errors, performance findings, tested multiplayer coverage, and remaining issues are explicitly reported.

Finish `Docs/POLISH_AUDIT.md` and add `Docs/POLISH_DELIVERY.md` summarizing changed systems/assets, verification, before/after evidence, and remaining limitations. Provide a short gameplay demonstration. Do not claim Steam release readiness, complete QA, or visual perfection beyond what was actually checked.

This milestone is about finishing the current experience. Do not add another profession, gadget family, progression system, or broad redesign until this quality pass is delivered.

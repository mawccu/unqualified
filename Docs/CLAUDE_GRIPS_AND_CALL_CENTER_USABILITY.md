# Targeted fixes — grips, reference books, and call-center playability

## User feedback

Overall quality improved. Remaining reported issues:

1. Hands do not properly grip the tray and some other carried objects.
2. Call-center reference books intersect or appear merged together.
3. Move mute and the other telephone controls to the right.
4. Spanish is being translated even without a translator gadget.
5. The call center is interesting but too difficult to understand. Keep the incompetent-worker fantasy while making the actual interface and rules clearer.

Fix these before adding content. This handoff is a specification, not evidence that fixes have been made. Reproduce each issue in the current build, update the polish audit, and verify in Play Mode.

## 1. Hands must visibly grip what they carry

Inspect the tray first, then every distinct carry/grip family. Use actual rendered meshes and hand contact, not just the existence of IK targets.

- Tray: palms support the underside or fingers wrap a believable edge/handle; thumbs remain above the grip where appropriate. Both hands maintain contact while walking, turning, looking up/down, crouching, and jumping.
- Determine whether errors arise from object origins, grip-anchor placement, wrist rotation, hand dimensions, finger pose, IK reach, or update order. Fix the cause per grip profile.
- Resolve the object's final collision-aware carry pose first, then solve hands toward its anchors. Avoid separate sway applied to the tray and hands after solving, causing them to drift apart.
- Check interaction animation, procedural animation, and IK ordering so different systems do not overwrite the same bones unpredictably.
- Do not keep arms rigidly extended, clip fingers through the tray, or let the palms float below it. If geometry cannot form the required pose, adjust the hand/prop asset rather than hiding the problem.
- Use grip profiles for paper, book/binder, small prop, tool, and wide two-handed object. Preserve their distinctions.
- Blend reach, contact, carry, placement, and release. Transfer ownership at the appropriate contact marker while preserving world pose.
- Verify local first-person and observable third-person poses where supported. Check supported FOV settings, obstruction near a wall, and movement at low/high frame rates.

Capture close views of the tray standing still and a short movement clip proving contact stays intact. Check other objects against the same criteria.

## 2. Separate and stabilize the reference books

Locate the actual intersecting binders/books in the call-center scene. Inspect their transforms, mesh bounds, pivots, scale, shelf spacing, colliders, and any repeated spawn/build logic.

Give every book a distinct physical slot with enough separation for its cover/spine. Correct overlap in the source prefab or scene generator if those recreate the shelf. Do not make a temporary scene tweak that disappears on rebuild.

Verify that opening/reading does not instantiate multiple copies at the same position. If reading one book, display one coherent held/open book; hide or relocate that same world instance consistently, then return it to its own slot. Never show all binders in the same hand pose.

Readable spine titles should identify purpose: delivery, setup, and refunds. Keep book contents consistent with the console references. Do not force the player to search several identical-looking volumes for one fact.

Check initial load, repeated reads, close/reopen, save/reload, and returning from another job. Correct collision and placement after each cycle.

## 3. Put call controls in a stable right-side rail

Keep the main conversation and case workspace on the left/center. Place persistent telephone controls in a dedicated right-hand column that does not scroll away with the transcript.

Suggested arrangement:

- Call status and customer language at the top.
- Answer when ringing; otherwise show the current active call.
- Mute / Unmute.
- Hold / Resume.
- Transfer, opening a small eligible-recipient selector.
- End call, separated visually from normal actions.

Use text labels and visible active states, not color alone. Mute means the customer cannot hear the worker; hold means the conversation is paused. Explain that distinction with a short first-use hint and optional tooltip.

Keep task actions such as asking for an order number, searching records, and selecting a remedy in the case workspace. Ending a call and resolving a case are distinct actions; do not award success just because End was clicked. Guard against accidentally ending an unresolved call, without adding confirmation prompts to every routine action.

Keep controls in fixed positions as call state changes. Disable unavailable actions with short reasons. On smaller widths, reduce optional panes or use tabs rather than overlapping the rail or shrinking everything to tiny text. Test keyboard focus and pointer interaction at 1280x720 and 1920x1080.

## 4. Translation must have an explicit source

The user expects untranslated Spanish without the translator. Default gameplay must honor that. A CV fluency claim routes calls; it does not grant a magical translation ability.

Observed code pointers to verify: SupportConsole renders translations when `GameSettings.TranslateCalls` or `call.PhraseAid` is true. It also exposes a Translate customer toggle. Trace all call text, phrase-aid, HUD, response, transcript, and gadget paths before patching one label.

Define separate states:

1. **Normal gameplay, no active translator:** customer text remains in the customer's language. Do not show a second English translation automatically. General UI controls remain in the player's chosen UI language.
2. **Translator gadget active and usable:** provide only the translation/hint capabilities the implemented device actually promises. Validate equipped instance, ownership, activation, and charge. Owning a device back at home is insufficient. Make the source visible, for example “Translator active.”
3. **Optional accessibility translated captions:** if retained, put this in accessibility settings, explicitly opt-in and off by default for ordinary new games. Label that it works independently of gadgets. Do not leave an unexplained free-translation toggle in the normal call toolbar. Never silently revoke a setting the user intentionally enabled.

Audit existing saved preferences. Do not blindly reset everyone's settings: distinguish an intentionally enabled accessibility setting from a legacy automatic default where possible. If the old format cannot distinguish them, explain the change once and ask the player to choose in-game; then persist the choice.

Do not use a general phrase-reference button to reveal the full exact translation of every customer utterance for free. An ordinary phrasebook can provide short generic expressions and terms, clearly separate from interpreting the live call. Verify device depletion, unequipping, scene changes, transferred calls, and switching the accessibility option refresh the display without stale translation remaining on current live lines. Previously granted transcript translations may remain readable if deliberately designed; label their source instead of pretending they were never seen.

Only claim translator behavior that actually exists. If the current gadget is merely a hint device, either implement the requested translation capability explicitly or present that limitation; do not rename a global translation switch as hardware.

## 5. Make the job understandable, not automatically solvable

Design principle: the character may be unqualified, but the player should understand the controls, the immediate goal, and where to find information. Confusion about which panel to use is not the intended challenge.

### First-call onboarding

Give the player a short paid introductory call in the game's baseline supported language, even if they claimed other languages. Clearly identify it as training. Do not spend the entire first shift on tutorials.

Guide one complete loop through small contextual prompts:

1. Answer the ringing phone.
2. Listen/read and ask for the missing identifying detail.
3. Use that detail to find the correct record.
4. Consult the relevant policy/reference.
5. Choose a remedy and confirm with the customer.
6. Record the outcome and close the call.

Show only the current step. Highlight the relevant control briefly, then stop. Use generous patience and no surprise supervisor penalty during this introduction. Allow experienced players to skip or replay training.

The next ordinary call removes the explicit step-by-step help but retains clear state, discovered information, and optional “How does this work?” guidance. Introduce claimed-language calls after the basic workflow is demonstrated, with a clear warning that this customer was routed because of the CV claim.

### Better case workspace

- Maintain a compact, automatically updated summary of facts actually discovered: customer/order ID, described symptom, attempted remedy, and outstanding question. Do not reveal undiscovered answers.
- Provide a short next-goal cue such as “Identify the order” or “Choose a remedy.” Avoid telling the player the correct remedy.
- Keep conversation history accessible and preserve important numbers. Permit copying an already obtained order ID into lookup so transcription busywork is not the main difficulty.
- Empty search results explain what was searched and allow correction. Wrong or missing IDs should not look like a broken screen.
- When an action is unavailable, explain the missing prerequisite: “Ask for the order number first.” Never leave a dead button with no explanation.
- Separate “what the customer said” from “what the company record says.” Their contradiction should be a clue the player can reason about.
- Give specific feedback on mistakes: “This order has not been dispatched; that delivery promise is unsupported.” Let the player correct it before severe consequences.

### Pace and assistance

For the first two ordinary calls, keep one active call at a time, modest queue pressure, and no overlapping tutorial panels. Increase load only after a demonstrated success. Test timings against actual reading and navigation time, including translations and reference lookup.

Put the customer on hold to research. Hold has a clearly communicated allowance and warning before impatience escalates. Mute does not freeze patience. During a real pause menu, respect the intended pause behavior consistently.

Provide an optional contextual hint with tiers: where to look → relevant policy section → more explicit help on request. Any gadget-exclusive advantage must remain distinct from this basic usability help. Keep reduced time pressure and translated captions as separate settings rather than a single unexplained “easy mode.”

Solo must remain playable without a friend answering questions. Co-op assistance should be helpful, not mandatory. Language difficulty remains a consequence of the player's claim, but ordinary controls and navigation should stay intelligible in the selected UI language.

## Delivery and verification

### Additional user-reported blockers: board and Dana

The user reports that the call-center board intersects the window and that Dana's presentation appears to be the wrong character. These are basic scene-placement and character-consistency defects, not optional final polish. Resolve them before calling this pass complete.

**Board/window:** locate the reported board in the live scene. Move it onto an appropriate solid wall or give it a believable freestanding support. Fit its rendered bounds and collider to the available space. Do not overlap the window frame/glass, block an intended opening, or leave the board floating. Keep its content readable from an accessible standing position. Correct the source prefab/scene-generation code as well as the scene if regeneration would restore the bad placement. Verify from inside the room, beside the window, and after rebuilding/reloading.

**Dana:** inspect the character definition, dialogue/pronouns, voice assignment, model, outfit, animation, nameplate, and spawn mapping. The name alone does not establish gender; check the project's actual intended characterization. If Dana is specified as a woman, make that consistent across the implementation instead of accidentally assigning a generic male worker/voice. If the project never defined Dana, record a clear proposed character specification rather than claiming a mismatch has been proved. Give Dana an intentional, recognizable face/silhouette, hairstyle, work outfit, and mannerisms that fit the game's art direction. Avoid relying on exaggerated gender stereotypes or merely renaming the same placeholder. Confirm that the correct asset is actually used in the running scene and that no fallback overrides it on reload.

**Raise the review standard:** independently inspect every call-center wall fixture, window, doorway, shelf, desk, seated character, and NPC assignment. Look for the same classes of error beyond the two examples: intersections, unsupported objects, duplicated generic models, mismatched identities, unreadable boards, and blocked interaction positions. Do not wait for the user to discover each instance. Record what was examined and what was fixed in the polish audit.

Provide before/after screenshots of the board/window from matching viewpoints and Dana in the actual gameplay scene, plus the character specification used for the fix. A successful compile or a correct asset sitting unused in the project is not sufficient evidence.

Implement in order: tray/grip fixes; reference-book placement; right-side call controls; translation-source corrections; introductory call and clearer case flow. Preserve current saves, language routing, job rewards, and existing accessibility preferences.

Update `Docs/POLISH_AUDIT.md` with these reported defects. Add a short delivery note listing exact changes and remaining limitations.

Required evidence:

- Tray and representative props stay visibly in hand contact through movement and placement.
- Books remain separate after repeated interaction and scene reload.
- Mute/hold/transfer/end remain visible on the right with correct state and keyboard focus.
- Spanish without active translator and with accessibility translation off stays Spanish.
- Active translator produces its documented assistance; unavailable/depleted hardware does not grant it.
- Explicit accessibility captions work independently when enabled and are clearly labeled.
- A fresh player can complete training, then one ordinary case using in-game guidance without an external explanation.
- A claimed-language call and a transferred call retain case state and remain completable.
- No duplicate rewards, broken hands, stuck input, or new recurring console errors.

Use focused tests for translation eligibility, state changes, and call progression. Verify visual grips, layout, books, and comprehension in gameplay. If a first-time human playtest is unavailable, report that limitation rather than claiming a scripted completion proves usability. Supply a short capture of the revised first-call experience and translation-off/on behavior.

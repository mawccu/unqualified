# UNQUALIFIED — the apartment becomes a cheating workshop

## User direction

Expand the apartment a little and add a hardware-building table. Players spend earned money on parts through their computer, then assemble gadgets that help them cheat at jobs. Preserve the existing apartment introduction, wedding job, call center, hands, and computer scrolling.

This is the next gameplay layer, not a replacement profession. The user has not fully tested the call center yet. Verify a complete baseline call-center shift before connecting gadgets to it; do not assume every existing case and language path is already stable.

All gadget names, recipes, prices, and balance details below are proposed starting points. Implement a small complete loop before expanding the catalog.

## The hook

Lie to get hired → earn wages → buy suspiciously cheap components → build homemade cheating devices → bring a limited kit to work → use it discreetly to survive bigger lies.

The central tradeoff is rent security versus investing in the next shift, then assistance versus exposure while on the job. Gadgets make a player more resourceful; they must not automatically solve every task or turn the game into unrestricted sandbox powers.

Example target moment: a player wearing a homemade prompt earpiece tries to explain a refund while their friend feeds them the case notes. The supervisor approaches, so they hide the device and finish the conversation themselves.

## 1. A slightly larger apartment

Extend the apartment with a small workshop alcove or spare nook, roughly a few metres across rather than a separate industrial facility. Keep its poor, improvised character: reused dining table, mismatched drawers, salvaged task lamp, component bins, power strip, and a pegboard. Make it feel like unemployed friends turning household junk into equipment.

Keep the computer, front door, bill/rent reminder, and workbench close enough to form a short natural route. Add one clear parcel drop location beside the door and shelves for completed gadgets. Preserve landlord pathing, couch wake-up, collision, and existing interactables.

Workbench contents:

- A stable assembly mat with a few readable component sockets.
- Small tools with matching hand poses and feedback.
- A test jig showing charge, connectivity, and the gadget's output.
- Shared component storage and a persistent unfinished-build tray.
- A charging dock and a bag/loadout rack for the next shift.

Avoid letting clutter cover essential sockets or trapping players behind pulled-out chairs. The usable table surface and tools must remain reachable in solo and co-op.

## 2. Buy parts on the computer

Add a separate Parts Shop tab/site alongside job listings and the CV. Suggested store name: Questionable Components. Do not replace the employment flow.

Each product shows icon, part name, price, quantity owned, compatible known recipes, and delivery timing. Start with six reusable part families: casing, battery, controller board, sensor, small motor, and speaker/display module. Use fictional electronics with understandable functions; no real engineering knowledge should be required.

Use a cart and a clear final checkout displaying total cost, remaining shared cash, and rent due. Never charge on merely selecting an item. Validate balance and stock on the authority, commit the purchase once, and save an order ID. Prevent double-click purchases and simultaneous crew checkouts overspending the wallet.

For the first version, delivery is a short in-game doorstep event while at home, not a real-world wait or a paid expedition. After checkout, show “Delivered to the front door” when the parcel arrives. Opening it grants the ordered quantities exactly once. Persist undelivered orders and unopened parcels across reloads; reconcile using order IDs so a crash cannot duplicate or lose paid items.

Packaging can become a harmless physical toy after collection. Components enter tracked storage; do not make a tiny screw rolling under the sofa the only copy of a paid part.

Verify wheel scrolling and scrollbar dragging through the full shop and CV, including small game-view sizes. Do not introduce nested scroll areas that swallow input unexpectedly.

## 3. Build hardware through short physical actions

Choose a known blueprint and bring its parts onto the mat. Show missing quantities before reserving anything. The first build should take around 30–60 seconds on first use, subject to playtesting.

Use a few meaningful steps:

1. Seat the board in the casing.
2. Connect keyed modules using shape/label clues as well as color.
3. Fasten two visible points with the tool.
4. Insert the battery and close the housing.
5. Run a short test, diagnose any highlighted connection problem, and correct it.

Use hands and visible contact, not a progress bar that says “Crafting” while nothing happens. Generous snapping is appropriate at the correct sockets. A misplaced module can be removed; an assembly mistake costs time, not the player's entire savings. Don't add mandatory tiny precision clicks or punish motor-accessibility settings.

Each recipe has a simple authored sequence with a clear resulting behavior. Reuse this system across gadgets; do not build a full electronics simulation. Completed devices have a persistent instance ID, recipe ID, configuration, charge, and ownership/location state.

Reserve components into an unfinished build transaction. Leaving the bench preserves that build; cancelling returns the reserved components once. Completing consumes them once and creates one gadget. Destroying a physical model is not an inventory transaction. Permit safe dismantling at home with a preview of recoverable parts; no duplicate-parts loop.

Only one player edits a given build at a time, but friends may inspect or deliver parts. On disconnect release the editor lock and keep progress. Additional benches can be a later upgrade, not an initial dependency.

## 4. Initial gadgets: two fully playable, then one extension

### A. WhisperBox — call-center prompt earpiece

Recipe idea: casing + controller board + battery + speaker/display module.

Attach to the headset. On a limited activation, it turns information already gathered into a brief structured hint: “Order found. Carrier collection missing. Check dispatch policy.” It can highlight the relevant reference section, but must not reveal unknown order data or select the correct remedy automatically.

For claimed-language calls, it can provide a short in-world phrase prompt or terminology hint. Keep optional translated subtitles available independently: basic accessibility translation is not something players must buy or charge.

Co-op extension: a teammate at an available terminal can send a small authored hint from the case record. Solo gets the same underlying hint system locally; never require a second human.

Risk: visible hardware or an audible confirmation chirp can attract attention during an announced inspection. Telegraph attention and give the player time to stop using or conceal it. Do not listen to or transmit the user's microphone. Do not tie it to paid/live AI services for the first implementation.

Test demonstration: the player gets useful guidance but still asks for missing information and chooses the resolution. If a hint can be wrong or uncertain, clearly label that condition; do not randomly deceive players to manufacture difficulty.

### B. SteadyHand — concealed tray stabilizer

Recipe idea: controller board + sensor + small motor + battery.

Fit beneath a compatible tray. While activated, it reduces motion-induced tilt and helps the player recover from a wobble. It does not ignore collisions, prevent every fall, repair damaged cake, or align tiers automatically. Skill still matters for placement, route choice, and deciding when to spend charge.

Risk: the mechanism is bulky and produces a short mechanical hum under heavy correction. An inspecting coordinator can notice the unauthorized device with valid sight/hearing evidence. Normal carrying at a distance is not automatically suspicious.

Hands and tray should visibly respond to stabilization. Show remaining charge. At zero, smoothly return to ordinary carrying; do not deliberately fling the cake or apply a sudden arbitrary failure impulse.

### C. BusyBee — optional distraction device after A and B work

Recipe idea: casing + controller board + speaker module + battery.

Place it in an accessible valid location and trigger a believable short chime. A nearby eligible supervisor may investigate, creating a brief opportunity. It cannot teleport NPCs, cancel an active confrontation, or force every NPC to abandon their job.

Use a visible investigation route, cooldown, and diminishing effectiveness. Repeated use from the same spot makes staff suspicious. It remains a physical device that can be discovered, switched off, and recovered. This expands existing distraction behavior rather than creating a parallel suspicion system.

## 5. Loadout and exposure

Start with one active gadget slot per player. A later progression upgrade can allow two only after balance testing. Devices and parts are shared crew property, but equipped instances are reserved to individual players. No two players may equip the same instance.

At the apartment rack, show which job each device supports, charge, and inspection risk. Disable meaningless activation in unsupported contexts with a clear explanation; never silently consume charge. Let players switch or remove equipment before leaving.

Use a shared, small set of inputs with contextual prompts; don't introduce a different key for every gadget. Support remapping. UI/desk controls must not accidentally activate equipment behind a menu.

Recharge at the apartment dock. Start with free charging to avoid constant consumable grinding; time/charge limits belong inside the shift. Dropped equipment persists or enters a clearly explained lost-and-found retrieval path after the shift. Do not permanently delete a paid device on a minor clipping bug.

Integrate device discovery into existing suspicion/evidence logic. Staff notice actual visible use, a nearby sound, or a discovered device. Walls block sight. A hidden device must not produce global suspicion simply because an activation flag is true.

Initially use a warning and demand to put the device away. Repeated witnessed misuse can escalate. Confiscation/permanent losses are later balance decisions, not necessary for the first loop. Existing job success without gadgets remains possible.

## 6. Economy must support experimentation

Observed current base values: rent 840, seven rent days, wedding base pay 140, call-center base pay 120. Verify current bonus, day, rent-payment, and repeat-shift logic before changing prices. Six base wedding payouts already consume the whole rent target; adding purchases without rebalancing could make normal play a trap.

Proposed approach:

- Give a modest starter component bundle after the first paid shift, enough to introduce one assembly. Explain it as salvaged parts, not a hidden cash deduction.
- Price a first additional working gadget at roughly 20–30% of a typical successful shift's take-home pay as an initial tuning target.
- Keep parts reusable across recipes so one purchase doesn't force a single profession.
- Increase attainable earnings or adjust rent progression as necessary so a player can afford rent and one introductory gadget within the intended week through competent normal play.
- Show cash, rent obligation, and post-purchase balance. Do not call money “available for upgrades” if it is the same cash needed for rent.
- No maintenance taxes, random destruction, or paid recharges in the first version. Gadget bonuses should reward good use, not guarantee an automatic profit multiplier.

Document an actual example week containing both job types, one purchase, and rent payment using implemented payouts. Check a modest-performance path as well as perfect play. Do not promise affordability from a theoretical maximum score.

## 7. Persistence and integration

The current CrewState already stores shared savings and migrates older saves through FromJson. Extend that migration for parts, orders, gadget instances, unfinished builds, and equipped reservations. Do not reset the player's existing money, claims, completed shifts, or language profiles.

Use stable IDs for orders, components, recipes, device instances, and player ownership. Authority validates shopping, assembly completion, activation charges, and loadout changes. Couple state updates so money cannot be removed without an order, and a completed build cannot consume parts without granting its device. Save safely with a recovery strategy; test interruption at purchase and assembly boundaries.

Where networking is implemented, synchronize persistent outcomes and charge/activation events; local cosmetic hand animation is not authority. Test contention with two players. If the current build is still offline, report that rather than presenting host-oriented code as tested multiplayer.

## Build order

1. Expand the apartment minimally, place the bench/delivery/rack, and verify paths and existing opening.
2. Deliver one shop order → parcel → components → physical assembly → test → saved device loop.
3. Implement SteadyHand in the already-tested wedding job, including charge and visible inspection behavior.
4. Verify a baseline call-center run, then implement WhisperBox without replacing language routing or accessibility translation.
5. Add loadout presentation, charging, economy tuning, and optional BusyBee only after the first two gadgets work.

Complete the two-gadget loop before adding tech trees, procedural recipes, an expanded career, or more professions. All baseline missions should still work without purchasing anything.

## Acceptance and delivery

- Earn wages in a real shift; shop, buy, receive, assemble, equip, use, return, recharge, and reuse the device.
- Show two visible gadget effects in their appropriate jobs, with player judgment still needed.
- Show one telegraphed device-discovery incident and a recoverable response.
- Reload during an order, after parcel collection, during assembly, and after equipping: no lost money, duplicated parts, duplicated devices, or permanent bench locks.
- Double-click and concurrent checkout/assembly cannot spend or grant twice.
- Empty batteries, unsupported jobs, pause, disconnect, and scene transition resolve safely.
- Existing apartment introduction, computer scroll, wedding completion, call language transfer, payout, and old saves remain functional.
- Demonstrate an affordable example week with a gadget purchase and rent paid from real implemented reward values.

Provide a short gameplay capture of the complete loop and list actual new assets/files, working mechanics, placeholders, and limitations. Test transaction and save behavior automatically where practical; inspect hand contact, readability, collision, charge feedback, and gameplay usefulness in Play Mode. A shop screen and decorative workbench alone are not completion.

# 03 — Jobs, playable scenarios, and content specifications

## Scope

Wedding crew is the only fully specified prototype job. Firefighters and aircraft ground technicians are conditional later jobs. Their settings use invented, deliberately simplified procedures; they are not training simulations. Real-world procedural correctness is not a design objective.

All jobs reuse pickup/place, sockets, tagged tools, evidence, witnesses, claim triggers, task graphs, and debriefs. A job may add at most one major new interaction system before its value is tested.

## Job A: Wedding crew — “Five-Star Experience”

**Pitch:** You claimed to be an experienced event team. The planner returns in eight minutes. The cake, ceremony setup, and audio must look professional.

### Room layout

One 18 × 12 m banquet room connected to a 6 × 4 m service room by two doors. The dimensions are graybox proposals. Spawn near the service entrance, with visible signage to each task area.

| Area | Gameplay purpose | Necessary contents |
|---|---|---|
| Dessert corner | Fragile assembly and evidence | Cake stand, three cake sections, replacement icing, cloth |
| Stage | Short tool puzzle | Fictional symbol panel, microphone socket, decorative speaker |
| Seating area | Sorting and presentation | Marked tables, place-card sockets, flower display |
| Service room | Private learning and recovery | Setup manual, spare cake shell, cleaning tool, supply shelf |
| Service threshold | Risk boundary | Openable door, visible client route, no invisible safe-zone rule |

The client patrols the public room on a roughly 30-second loop with an audible approach cue. The service room is normally private but can be inspected when a selected claim explicitly includes it. Doors physically occlude vision. No lock can permanently trap the NPC or seal a required route.

### Three essential outcomes

| Outcome | Completion condition | Quality rule |
|---|---|---|
| Dessert presentation | Three cake sections in correct sockets on stand | 100 clean/intact; 70 with approved spare shell; −15 per unresolved cake-zone mess, minimum 0 |
| Ceremony arrangement | Required place cards in their matching sockets | 100 if all match; each approved generic replacement subtracts 10, minimum 0 |
| Audio ready | Panel sequence correct and microphone test completed | 100 clean setup; 70 if emergency battery substitute used |

Incomplete outcomes score 0. “All match” includes explicit generic replacement sockets: missing identity cards can be replaced with “Honored Guest” cards, which count as completion but cost quality. One unresolved mess per bounded cake-zone patch; there is no unlimited stain stacking.

Required place cards by crew size: 2, 3, 4, 5 for 1, 2, 3, 4 players. Cake sections and audio steps remain constant. Optional centerpiece arrangement is an accolade in M3, not a fourth essential outcome.

The audio panel uses three large fictional icons—bell, ribbon, star—with an order shown in the manual. A seed may choose one of six permutations. Wrong input produces a warning beep and clears the attempt; three wrong attempts create one loud pop event and a recoverable “tripped” state. Hold reset for three seconds, then retry. Never shock or permanently disable the player.

### First playable content budget

- One small room plus service room, one client NPC, four simple player avatars.
- Three essential outcomes; three cake sections; up to five place cards.
- Three primary tool categories: cloth, manual, setup/reset interaction.
- One claim: “No manuals needed.”
- One guaranteed recoverable cake accident or deliberate cake drop trigger.
- One eight-second distraction at the flowers.
- One inspection route with three task checkpoints.
- One challenge: “Show me the finished dessert table.”
- Graybox meshes and temporary original text/audio only.

The earliest build can use colored blocks. It must support readable interactions and the whole shift before final art.

### Cake accident and recovery contract

A cake section dropped above the configured safe-impact threshold becomes damaged, spawns one mess patch, and reduces dessert potential. It does not disintegrate into physics fragments. On reset it remains reachable or moves to the recovery shelf.

Repair routes:

1. Retrieve the damaged section, place it at the repair station, hold icing action for six seconds, and clean the mess for four seconds. Returns the section to intact quality.
2. Use the spare shell instantly, completing the structure at a 70 quality cap. Clean separately or accept mess penalties.
3. Hide the damaged section and evidence temporarily. This buys time but provides no task completion or score restoration.

Repairing a section does not magically clean the floor. If the client already saw the accident, repairing closes the evidence but leaves the event's historical suspicion. A later demonstration can reduce suspicion under the shared rules.

### Inspection

At Work end, stop new events and freeze task quality. The client visits cake, seating, audio in that order. At each checkpoint, observe remaining tagged evidence using the same incident ledger. Do not run another puzzle or require players to repeat finished work. Maximum total duration 45 seconds; offer a group skip after findings are resolved.

Under “Spotless finish,” add the service threshold as a checkpoint, but not the entire room. The briefing must identify that extra checkpoint before the shift starts.

### Event variants — add only after M2

| Event | Warning | Effect | Recovery | Restrictions |
|---|---|---|---|---|
| Draft from side door | Door rattles for 4 s | One unsecured place card moves to a nearby reachable spot | Retrieve and socket | Never choose a completed secured card |
| Curious planner | “I'll check dessert shortly” | Adds a cake visit in 15 s | Repair, prepare, or distract | No concurrent challenge |
| Wrong delivery | Clearly labeled extra box | Tempts crew with a mismatched cake section | Use correct existing supply | Never removes required parts |
| Microphone feedback | Rising warning tone | Interrupts audio test, attracts investigation | Reset and correct sequence | Only before audio completion |
| Leaky centerpiece | Visible drip before puddle | Creates bounded mess near public path | Move container, clean | Minor event; no liquid simulation |
| Photo moment | Planner announces picture | Wants the cake table presentable in 25 s | Complete dessert demonstration | Counts as challenge, not stacked event |

### Team stories to support

- One player memorizes instructions; another confidently repeats the wrong order.
- The client is praising the flowers while a teammate rebuilds the cake behind her.
- Someone opens the service door just as another player starts reading the forbidden manual.
- The crew accepts a lower-quality substitute because hiding the repair would take too long.

These are design targets to observe in testing, not guaranteed “viral” outcomes.

## Job B: Firefighters — “Nothing to Worry About”

**Conditional second job.** A small community center is experiencing extremely theatrical arcade fires. Rescue the mascot, stabilize the fictional alarm unit, and clear the marked hazard zones before the building manager's assessment.

### Shared mechanics and one new system

Reuse carrying, cleaning-like tool application, manuals, sockets, inspection and suspicion. New system: bounded hazard tiles with states Safe, Smoking, Burning, Cooling. No fluid simulation, dynamic building collapse, real gas behavior, or technical firefighting procedures.

The manager stays in a protected observation area and follows safe navigation routes. It is reasonable for a firefighter to consult instructions in some circumstances; the violation only exists when the crew explicitly claimed it never needed them.

### Essential outcomes

1. Rescue mascot: carry a cartoon foam mascot mannequin to its marked safe area. Completion 100 if undamaged, 70 after a slapstick tumble and repair wrap.
2. Clear hazard zones: apply the matching invented foam tool to marked cells until their meter clears. Completion at all required cells safe; 100 with no damaged display props, minus 10 per damaged tagged display, floor 0.
3. Restore demonstration alarm: match the three fictional symbols and test the bell. 100 normal, 70 using approved backup bell.

Define exact quantities in its own tuning resource before implementation. Solo: mannequin fits a rolling cart, hazard count is lower, no required simultaneous switch holds.

### Claims and consequences

| Claim | Implementation idea |
|---|---|
| Museum-safe methods | Witnessed foam on protected displays produces an incident |
| Equipment experts | Manual reading creates the shared no-manual violation |
| We leave everything tidy | Visible residue is included in final inspection |

The tool's big cartoon recoil is an animation and bounded movement impulse. Never make the player camera violently shake by default. No realistic fire hose control needed.

### Candidate events

Foam cartridge runs low with a visible warning; mascot rolls down a shallow ramp; manager asks why a display is covered in foam; a safe-zone door sticks and needs an alternate route; a celebratory confetti cannon looks suspiciously like a fresh hazard.

### Failure and production boundary

Failure is evacuation and an embarrassed debrief. No visible deaths. Keep three connected rooms maximum. If tile hazard networking takes more effort than the shared systems, cut spread and use fixed timed hazard sources first.

## Job C: Aircraft ground technicians — “Looks Airworthy”

**Conditional third job.** Your crew is sent to service a fictional cartoon aircraft parked in a hangar. You are ground technicians, not pilots. A cockpit disaster is a later standalone finale, not a hidden flight-simulator requirement.

### Shared systems and one new system

Reuse socket placement, inventory, symbol puzzles, witness routes, substitutions, and evidence. New system: a simplified diagnostic panel linking three modular components. Plane remains stationary; no aerodynamic simulation.

### Essential outcomes

1. Cargo secure: marked crates in correct bays with latch state complete. 100 correct, 70 with approved temporary cargo net.
2. Panel assembled: three oversized fictional modules in matching slots, cover attached. 100 standard parts, 70 approved spare module.
3. Cabin demonstration: cabin lights, announcement, and seat-sign sequence pass a short symbolic test. 100 normal, 70 using manual backup controls.

All sequences are invented. Do not copy genuine aircraft maintenance instructions, airline branding, or checklists.

### Claims and consequences

- “Cabin demonstration specialists”: client asks for a coordinated demo at a known checkpoint.
- “No spare parts needed”: using the approved spare in view violates the claim, but remains a legal game solution.
- “Silent maintenance”: repeated noisy tool actions produce tagged incidents when observed; configure cooldowns to avoid meter spam.

### Candidate events and moments

A baggage cart rolls toward the inspection path; a module label rotates to reveal the matching icon; the client requests an announcement while the audio unit is disconnected; a teammate carries an oversized component through the only clear doorway; the crew hides an unused part, then discovers it belongs to the panel cover.

Critical items never permanently leave the hangar. The recovery shelf brings back out-of-bounds objects with an explicit UI notice.

## Expansion professions: evaluated, not approved

| Job | Comic potential | Reusable systems | New cost | Priority |
|---|---|---|---|---|
| Hotel maintenance | Guest walks in during repair | Nearly everything | Pipes/switch variants | High fallback |
| Restaurant catering | Public service vs hidden mess | Wedding props and scoring | Food preparation chain | Medium; genre crowded |
| Museum movers | Pretend replica is priceless original | Carry, sockets, witnesses | Fragile art forms | High expansion candidate |
| Cartoon surgery | Supervisor watching an absurd operation | Tools, modules, quality | Body/animation/tone complexity | Later |
| Nuclear control room | Massive confidence mismatch | Panels, manuals, alarm events | Large readable interface | Later finale |
| Pilot emergency | Strong screaming-with-friends image | Panels and communication | New movement/game format | Separate prototype only |
| Wedding photographer | Composition and hiding disasters | Existing map and NPCs | Screenshot/framing validation | Later variant |

## Job-authoring acceptance checklist

A new job needs three named outcomes, explicit completion/quality rules, solo quantities, reachable tools, recoverable critical objects, two meaningful claim options, one visible cover-up opportunity, a valid inspection route, and at least one event that reuses existing systems. Its first task must be understandable in under a minute. Every new mechanic must justify its production and multiplayer cost.

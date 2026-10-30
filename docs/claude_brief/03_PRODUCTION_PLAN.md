# UNQUALIFIED — Production Plan and Technical Boundaries

## Build in this order

1. **Audit the current M0/M1 build.** Run existing tests. Preserve host-validated interactions, object ownership, and disconnect cleanup unless a test proves a replacement is needed.
2. **Make the room legible.** Replace the empty test room with a simple wedding venue layout, interaction labels, task board, and clear public/private zones. Keep art primitive but intentional.
3. **Build task state before spectacle.** Data-driven essential outcome states, due windows, evidence, and HUD task cards.
4. **Add one complete cake incident.** Fall, spill, witness check, distraction, cleaning, restoration, and debrief log. Test solo and two-player first.
5. **Add audio and seating from the same contracts.** Do not create bespoke untestable systems for each task.
6. **Add suspicion, coordinator, inspection, and scoring.** The coordinator's state must be visible and debuggable.
7. **Playtest and iterate.** Fix clarity, role balance, and softlocks before expanding content.

## Technical decisions

- Engine: Godot 4.7.2, typed GDScript, first-person 3D.
- Networking: Godot high-level multiplayer / ENet, host authoritative for task state, objects, evidence, suspicion, scoring, and claim callbacks.
- Current project status: M0/M1 networking testbed only. It should evolve into the vertical slice rather than be mistaken for a demo.
- Source of truth: all shared mutations pass through an interaction/task service. Clients send intent; host validates range, state, actor, cooldown, and timing; host broadcasts resulting state.
- Build scenes and data assets rather than continuing to generate the whole venue in one root script. Existing code-generated blocks may remain while migrating incrementally.

## Suggested data contracts

Use Resources or typed dictionaries with validation at load time. Keep IDs stable for logging/networking.

```text
TaskDefinition
  id, display_name, zone, due_window, requirements[], quality_rules[]

TaskState
  id, phase, quality, completed_at, public_failure_count, evidence_ids[]

EventDefinition
  id, trigger_window, prerequisites, spawn_actions[], recovery_routes[],
  suspicion_effects, cooldown

EvidenceState
  id, type, zone, visible, cleaned, created_by_event

ClaimDefinition
  id, benefit, callback_event_id, promise_text, settled

SuspicionEvent
  source_id, amount, witness_id, timestamp, explanation
```

Every state-changing event should create a short record for the debrief. This makes debugging and comedy share the same instrumentation.

## Presentation priority

Prioritize information over graphics:

- Current objectives and due time stay visible but compact.
- Interactable highlight plus one short verb: `E: Pick up cake section`, `E: Clean frosting`.
- Coordinator attention must be readable from across the room: portrait/icon, state label, and simple world indicator.
- Show a brief cause when suspicion changes: `COORDINATOR SAW THE SPILL +20`.
- The task board shows desired final states, not implementation jargon.

## Test cases to automate or run manually

### State and authority

- Two players race for one prop; exactly one receives it.
- A client cannot clean evidence, settle a claim, or change suspicion from out of range.
- Repeated/late requests do not duplicate a payout, evidence cleanup, or suspicion effect.
- Disconnecting holder releases object and leaves task solvable.
- Host and clients reach the same essential-state, suspicion, and debrief result.

### Gameplay

- Cake may be dropped, recovered, cleaned, and restored; no state becomes permanently unreachable.
- Coordinator sees a public failure only when attention/line-of-sight rule says so.
- Distraction expires and respects cooldown.
- A solo player can finish each outcome with the display-prop route.
- At least one claim callback occurs every shift.
- Inspection catches a genuinely incomplete outcome and passes a genuinely restored one.

### Experience

- A first-time player can identify the three essential outcomes within 30 seconds.
- A player understands why suspicion changed without asking the developer.
- A missed task creates a recoverable problem before it becomes a failure.
- Debrief names events in a way players recognize.

## AI interview: deliberate staging

The live-microphone interview is a strong future hook, but it is not required to prove the game. A hosted live AI system adds cost, moderation, latency, privacy, localization, and unreliable evaluation. It could consume the entire prototype while contributing little to the moment-to-moment shift.

Stage it:

1. **Prototype now:** a branching “video call” interview with three boasts. Player picks a claim and sees its promised consequence.
2. **Playtest:** verify that players remember their selected lie and enjoy the callback.
3. **Optional experiment:** record local microphone input only as a cosmetic confidence meter or transcribe through a consented service; it must not determine whether players are allowed to play.
4. **Only after product validation:** investigate a real interview with explicit privacy notice, moderation, fallback selections, spend limit, and no dependence on a proprietary model response for core progression.

## Content and scope rules

Do not add a new job until the wedding slice demonstrates: repeatable 12–18 minute fun, clear social consequences, a working solo route, stable networked task state, and a debrief people want to share. Prefer one new event that recombines existing verbs over a new profession or new subsystem.

Potential future jobs must use fictional equipment and simplified, non-instructional tasks. The player fantasy is bluffing, not learning real emergency, medical, or aviation practice.

## Required end-of-task report

After each implementation increment, report:

1. Changed files and why.
2. What was run and what actually passed.
3. What was not run or remains unverified.
4. Known bugs / design compromises.
5. Exact next smallest playable task.

Never claim a GUI, network scenario, or export was tested when it was not. Keep `docs/IMPLEMENTATION_STATUS.md` accurate.


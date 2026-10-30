# 05 — Technical design and implementation contracts

This is a proposed architecture, not verified code. Exact engine/plugin versions, APIs, export behavior, and transport compatibility must be checked in the target project. Do not claim this document is a working multiplayer implementation.

## Engine decision

Default greenfield choice: Godot 4 stable, typed GDScript, Windows desktop, Git. Rationale: a compact scene workflow, editable text resources, low-friction grayboxing, and a manageable prototype codebase. This is an engineering judgment for a small experimental project, not evidence that Godot is universally better.

Alternatives: Unity/C# is reasonable if the team already knows it or owns suitable assets/network tooling. Unreal becomes attractive with experienced Blueprint/replication developers but is not the default for this project's small first room. Do not switch an existing project without identifying concrete benefit and migration cost.

Godot documents high-level multiplayer and ENet support; the same documentation notes that direct Internet hosting can require UDP port forwarding [S6]. Consequently a successful LAN prototype is not evidence that Steam friend joining works. Steam integration needs a separate adapter/spike. Valve's networking APIs provide transport options, but an engine binding and its compatibility must still be verified [S7].

## Milestone infrastructure

| Stage | Transport / runtime |
|---|---|
| M0 | Offline authority in the same gameplay services |
| M1–M3 | ENet, local machine or controlled LAN, host plus up to three clients |
| M4 | Tested Steam-compatible lobby/invite/relay path, or explicitly documented alternative |
| Release | Packaged Windows clients connecting from separate home networks without manual router configuration |

Do not build web accounts, a custom matchmaking backend, dedicated servers, or cloud saves for the prototype. Do not assume a plugin is maintained merely because its name sounds familiar. At the M4 spike, record repository, license, version, supported engine release, unresolved issues, and a real two-network result.

## Folder structure

```text
project.godot
scenes/
  bootstrap/  lobby/  player/  props/  npc/  jobs/wedding/  ui/
scripts/
  core/  interaction/  tasks/  witnesses/  claims/  networking/  saves/
data/
  jobs/  claims/  events/  tuning/  localization/
assets/
  placeholder/  final/
tests/
docs/
  decisions/  playtests/  asset_ledger.md
```

Use a few clear services rather than dozens of global singletons. A session root owns the runtime services. Data resources describe content; they do not directly perform gameplay mutations.

## Ownership of behavior

| Component | Responsibility | Must not own |
|---|---|---|
| SessionController | Lobby, shift phases, load/unload | NPC perception logic |
| InteractionService | Validate and execute interaction requests | Currency balance |
| TaskService | Completion graph, per-outcome quality | Input reading |
| WitnessController | Perception, navigation, challenge presentation | Direct payout changes |
| IncidentLedger | Incident IDs, observed/closed state, deduplication | Mesh effects |
| SuspicionService | S, decay, thresholds, exposure | World raycasts |
| ClaimService | Claim eligibility, violation, retained bonuses | Procedural dialogue |
| EventDirector | Seeded eligible events, pressure budget | Arbitrary client-side spawns |
| PayoutService | Pure calculation from frozen result | Live physics |
| SaveService | Versioned profile and campaign snapshots | Credentials |
| TransportAdapter | Session connect/disconnect and peer identity | Gameplay rules |

## One mutation path

Offline and host-player interactions use the same validated request path as remote clients. The host owns truth: task state, object claims, witness knowledge, suspicion, RNG, clock, and payout. Clients submit intent and display replicated state.

Example conceptual request, not a copy-paste engine API:

```json
{
  "request_id": 184,
  "actor_id": "peer_3",
  "action": "pickup",
  "target_id": "cake_section_02",
  "expected_revision": 7
}
```

Derive actor identity from the authenticated peer/session mapping, not the claimed JSON field. Validate phase, range, target existence, line of interaction, cooldown, current ownership, revision and action eligibility. Reject invalid input with a bounded reason code. Clamp vectors and rates; never accept arbitrary file paths, serialized objects, scripts, or resource paths from clients.

For simultaneous pickup requests, the host accepts exactly one, increments the object's revision, and rejects the other. A client may predict a hand animation, but cannot authoritatively steal the object. Never let both peers simulate its ownership independently.

## Prop and movement replication

Use controlled held-object poses rather than synchronizing a chain of arm physics. When held, a prop follows a validated hold target; when released, only the host simulates authoritative rigid-body physics. Remote clients interpolate snapshots and apply reliable state transitions for pickup, socket, damage, despawn, and recovery.

Starting budgets to measure: physics 60 Hz; transforms 15–20 Hz; NPC perception 5 Hz with staggered queries; cosmetic particles local only. These are proposed settings, not engine guarantees. Do not send every prop every frame. Sleeping objects need no continuous transform stream.

Player movement may start host-authoritative with local visual prediction, then reconciliation if needed. Document the compromise and test remote feel. Do not ship a multiplayer prototype that feels good only to the host. Use separate channels for high-frequency snapshots and reliable gameplay events where the selected transport supports them.

Every replicated entity has a stable session ID and revision. State transitions are idempotent. Timeouts and countdowns use host ticks/deadlines, not each client's local wall clock.

## Useful data contracts

Recommended content resources:

```text
JobDefinition
  id, scene_id, work_seconds, base_pay
  outcome_definitions[3], claim_allowlist[], event_allowlist[]
  player_count_quantities, inspection_checkpoints[]

ClaimDefinition
  id, promise_text_key, eligibility_tags[]
  bonus_fraction, modifier_id, violation_event_tags[]
  incompatible_claim_ids[]

TaskDefinition
  id, dependency_ids[], required_state, quality_rule_id
  target_entity_ids[], recovery_rule_id, solo_override

Incident
  id, type, source_entity_id, cause_actor_id?
  created_tick, observed_by[], suspicion_applied
  evidence_state, monetary_deduction, closed_tick?

ShiftResult
  shift_id, completed, exposed, outcome_scores[3], Q
  retained_claim_ids[], deduction_items[], gross, net, incident_summary
```

IDs resolve through a whitelist of authored resources. Do not execute arbitrary content-defined code. M2 can use typed Resources rather than introducing a full modding schema.

## Wedding task state example

```text
Cake: Unassembled → Assembled → [Damaged] → Repaired or Substitute
Audio: Unconfigured → SequenceEntered → Tested → Complete
Cards: Missing → Partial → Complete
```

Cake damage can invalidate its completion before Work ends. Every quality recalculation reads canonical state. Final inspection freezes that snapshot once. Re-entering Debrief or resending a result must not award money twice: persist `last_committed_shift_id` and reject a duplicate payout commit.

## Witness implementation

Use a finite-state machine and navigation on a small authored mesh. Perception samples tagged evidence anchors, not every triangle. Test cone/range, then obstruction, then dwell. Interrupt dwell when obstruction resumes. Sound queues an investigation destination; visibility generates the incident observation.

A blocked route replans once, then selects a valid authored fallback. It never teleports through a locked wall to punish the player. If an inspection checkpoint becomes unreachable due to a bug, flag the run invalid in development; in production recover blocked props or use a visible fallback inspection position. Do not silently invent detection.

## Saves and disconnects

M2 saves settings only. M4 adds local JSON or equivalent structured saves with schema version, profile unlocks, campaign seed, completed shifts, cash, rent checkpoint, and last committed shift ID. Write to a temporary file then replace atomically; retain one previous valid backup. Validate ranges and handle corruption with a clear recovery choice.

No mid-shift saves in the first release target. Saving occurs after payout and between days. Host migration is explicitly excluded. Host exit returns clients to the menu with explanation; reload restores the last completed shift, not a half-written payout. A disconnected client's held items drop safely, NPC interactions cancel, and required quantity scaling follows file 02.

M2–M3 support joining only in lobby. Rejoining during a shift can wait for the next lobby. Release may add snapshot-based reconnection, but do not advertise it before implementation and tests.

## Diagnostics and performance

Developer overlay: phase, host tick, peer count, RTT estimate, active rigid bodies, event seed, S, client state, perception rays, object ownership and last incident. Event log uses IDs and timestamps. Do not log microphone audio or full personal chat.

Target 60 FPS at 1080p on an agreed midrange test PC; choose actual minimum specs after profiling. Initial scene cap: roughly 40 interactable props, one or two active witnesses, bounded decals and particles. A cap is a starting budget, not proof of performance.

## AI interview boundary

No API calls in M0–M5. If a later experiment is approved, a separate backend owns provider keys and rate limits. Speech/text results map only to validated claim IDs. The game never executes model-produced code, accepts arbitrary rules, or lets model output directly modify pay or suspicion. Timeouts fall back to the authored interview. See file 07 for costs and product tradeoffs.

## Source references

[S6] https://docs.godotengine.org/en/stable/tutorials/networking/high_level_multiplayer.html

[S7] https://partner.steamgames.com/doc/features/multiplayer/networking

# Implementation status

Updated 2026-10-30.

## Current milestone

M0 (foundation) and M1 (shared room over the network) are written and machine-checked. Neither has been played by a person yet.

## Versions

- Godot 4.7.2 stable (official build ed1daf0bf), Windows, Forward+ renderer
- Typed GDScript, with the untyped-declaration warning switched on in project settings
- Networking: Godot high-level multiplayer over ENet (built in). No plugins

## What exists

M0:
- Graybox room (12 x 9 m), a side table with one cake section, a stand table with one socket
- First-person movement, mouse look, sprint
- Contextual prompt shown only when the action would be accepted
- Pick up, place on a socket (with ghost preview), drop, pick up again from the socket
- Every world change goes through `InteractionService.request()`, which validates and then applies. Players never change props directly
- Pause menu on Esc with FOV and sensitivity sliders (saved to `user://settings.cfg`), mouse released on focus loss, restart on F5
- Input actions registered in code (`scripts/core/bindings.gd`), ready for a rebinding screen

M1:
- Host a game or join one from the pause menu (Host a game, address box plus Join, Leave game). Up to 4 players
- The host owns all truth: props, who holds what, revisions. A client sends only its intent (pick up, place, drop) and its own pose. The host decides who the sender is from the connection, not from anything in the message
- Roster: the host assigns each player a slot (distinct spawn point and colour) and tells everyone
- Remote players are visible as coloured capsules with a face block, smoothed between updates. A prop someone holds follows their hold point on every screen
- Disconnect cleanup: if a player leaves (or their process dies) holding the prop, the host drops it where it is and tells everyone; it can be picked up again
- Simple event log (`NET <time> ...` lines) and a status line in the menu (Offline, Hosting: n of 4, Connected: n)
- Solo menu pauses the game. Once online the game keeps running for the others

## Checks actually run

- `godot --headless --path . --import`: all classes registered, no errors
- `tests/m0_selftest.tscn`, headless and in a real window: 46 checks, 0 failed, nothing on stderr. Interaction rules, 20 pickup/place/drop/reset cycles, positions checked several frames after the action
- `tests/run_net_tests.ps1`: 35 checks, 0 failed, nothing on stderr. It starts real separate Godot processes on this PC over loopback and checks what each logged:
  - lobby4: host plus 3 clients, all four see all four players, 4 distinct slots, exactly one local player each
  - contend3: two clients race for the prop at the same wall-clock moment, 24 rounds. Every round exactly one winner, the host's record names that winner, the loser is told `already_held`
  - contend_host: the same with the host as one of the two racers, 24 rounds
  - disconnect: a client leaves while holding the prop. Host notices, prop is free, another client sees it freed and picks it up
  - disconnect_kill: same but the client process is killed with no goodbye. The host noticed about 4 to 6 seconds later
  - far: pickup from across the room is rejected (`out_of_range`). Teleporting next to the prop and asking at once is also rejected, because the host has not accepted the move. A few seconds later, once the host has followed at a believable speed, it succeeds. Another client and the host both saw the walk reach the far wall
- `tests/screenshots.tscn` and the visual scenario in `tests/net_scenario.gd` render real windows. Reviewed by eye: prompts, ghost preview, placed prop, and a remote player carrying the cake as seen from the host

## Bugs found by real runs and fixed

- M0: placing a prop did not move it to the socket in a real window, and reset did not move it back (a frozen body's transform set between physics steps is overwritten). All prop transforms are now set inside the physics step. Headless did not reproduce one of them, so physics behaviour must be checked in a real window and a few frames after the action
- M1: the host sent to peers that were mid-disconnect, which logged engine errors. It now sends only to fully connected peers
- M1 test only: three clients quitting in the same instant make Godot itself log "Unable to send packet on channel 0" on the host. That is engine noise outside our code. The lobby test staggers the exits. Real players leaving together may still show it in the log

## Not checked

- Anyone playing it: how movement, aiming, carrying and the menu feel is unknown
- Clicking Host and Join in the real menu (the tests call the same functions, but no human pressed the buttons)
- Added latency (100 to 150 ms) and packet loss. No tool for it is set up
- Two different PCs or two different networks. Everything ran on one PC over loopback
- Joining while the prop is held or placed (a join always started with the prop at its start)
- The host pressing restart with clients connected
- Windows export. No export templates are installed, so no exported `.exe` has been built or run
- A Windows firewall prompt when hosting on all interfaces (tests bind loopback only)

## Known limitations

- Movement is not host-authoritative yet. Each client reports its own position and the host only limits its speed and keeps it inside the room. A client that lies is not corrected on its own screen; the host just does not act on a position it has not accepted. This is the compromise the pack allows for M1
- Others see a held prop at a fixed distance from the holder; the holder sees it pulled closer near walls
- No rate limit on requests and no size limit on the text in a request yet
- No lobby phase: anyone can join at any time. Mid-shift joining rules come with M2
- The room and props are built in code, not editor scenes. Move them to scenes before art arrives
- Players do not collide with props (player collision mask is world only). Revisit when props need to push back
- No crouch, no rotate-while-placing, no ping

## Looks wrong in the screenshots, not yet changed

- The held cake section is large in the lower right of the view (about 0.4 m wide at 0.95 m from the eye). Probably needs a smaller hold offset or scale. Left alone because it is a feel judgement for Malek

## Windows export procedure (not yet run)

1. In the editor: Editor menu, Manage Export Templates, Download and Install (4.7.2).
2. Project menu, Export, Add Windows Desktop preset, Export Project.
3. Run the produced `.exe` on a PC without Godot installed.

## Next tasks

1. Malek plays it. To try multiplayer on one PC: run the game twice, press Esc, Host a game in one, then Join (127.0.0.1) in the other
2. Decide items 1 to 5 in `docs/decisions/0001-pack-review-findings.md`
3. Latency and loss testing, and a test across two machines or networks
4. M2: the wedding shift (needs the decisions above first)

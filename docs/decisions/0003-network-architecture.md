# 0003 Network architecture for M1

Date: 2026-10-30. Status: ACCEPTED for M1, revisit at M4.

## Choice

Godot high-level multiplayer over ENet, one host and up to three clients. The host is also a player. No Steam, no relay, no matchmaking: that is the M4 spike.

## Who owns what

- Host owns: props, ownership, revisions, the roster, every validation.
- A client sends: its request (pick up, place, drop with the revision it last saw) and its own pose.
- The host answers each request with ok or a reason code, and broadcasts state changes reliably and positions about 20 times a second.
- Peer identity is the connection (`get_remote_sender_id`), never a field in the message. Offline, host and client all go through the same `InteractionService.request`.

## Client prop copies

On a client a prop never simulates. It adopts the host's state and moves toward the newest snapshot. A snapshot older than the ownership state already applied is ignored (revision check), so a late packet cannot move a prop that has since been placed. All transform changes happen inside the physics step.

## Movement compromise (allowed by the pack for M1)

Clients report their position. The host limits speed to 1.5 times sprint speed and keeps players inside the room, and checks range against the position it has accepted, not the one claimed. Proper host-authoritative movement with client prediction and reconciliation is deferred. Reason: the first goal is shared props, and this is enough to test it honestly.

## Other choices

- Actor ids are `peer_<connection id>`; offline the player is `peer_1`.
- Spawn slots are distinct per player and all within reach of the cake section.
- The host sends to each fully connected peer individually, because sending to one that is mid-disconnect logs an engine error.
- ENet timeouts for a vanished client are set to 3 to 8 seconds (default 5 to 30).
- Restart is host only. Clients are told to reset their own position.

## Not decided yet

Mid-shift joining, rejoin after a drop, host migration (excluded by the pack), request rate limits, and how Steam invites will replace the address box.

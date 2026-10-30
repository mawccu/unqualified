# 0001 Design pack review findings

Date: 2026-10-30. Status: PROPOSED. Nothing here changes the pack until Malek approves it.

Source: review of the UNQUALIFIED design pack v0.1 (docs/pack). Each item names the file that owns the rule, so an approved change is made once there.

## 1. The face-saving challenge is unreachable in normal M2 play

Owner: 02_GAME_SYSTEMS (suspicion, challenges).

Problem: challenges unlock at S 60 or more. The guaranteed prototype events give 20 (damaged cake) plus 12 (manual violation) = 32. The challenge and the exposure test in M2 acceptance need repeated deliberate failure.

Proposal: in M2 add one scheduled challenge ("Show me the finished dessert table") at a fixed point in the shift, independent of S. Keep the S 60 rule for later shifts.

## 2. "No manuals needed" is a weak prototype claim

Owner: 03_JOBS_AND_SCENARIOS (wedding layout, audio task).

Problem: the manual is in the private service room and the audio sequence is three icons, so players memorise it and are never seen reading.

Proposal: make the manual necessary at the stage (for example the full sequence is five icons, or a reset reshuffles it), so reading it happens in the public room.

## 3. The claim bonus means nothing in Quick Shift

Owner: 02_GAME_SYSTEMS (claims, career money).

Problem: pay and rent exist from M4 only, and M2 has a single forced claim. The prototype tests being handicapped, not choosing a lie.

Proposal: show payout and a rent progress bar in the Quick Shift debrief, and ship at least two claims in M3 so the choice is real.

## 4. Solo cover-up is undefined

Owner: 02_GAME_SYSTEMS (distraction, scaling), 03_JOBS_AND_SCENARIOS (content budget).

Problem: solo relies on "a placed approved display" that is not in the wedding content list. One player cannot distract and repair at once.

Proposal: add a placeable display prop to the wedding content list. While it runs, the witness is distracted for 8 s without the player present.

## 5. Tuning interactions to check in the graybox

- Distraction lasts 8 s. A full cake repair takes 6 s plus 4 s of cleaning.
- The client walks a fixed 30 s loop with an audible cue, so groups can time it. Consider seeded route variation.
- Unspecified: can a successful demonstration push S below the decay floor of 20?
- The "Photo moment" event states 25 s but solo challenge time is 40 s.
- File 00 says "one selectable lie" while file 03 has one claim.

## Estimates

M0 to M2 is a multi-week project. Re-estimate after M1.

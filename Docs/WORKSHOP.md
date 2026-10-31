# The workshop: what is built

Implements `CLAUDE_APARTMENT_GADGET_WORKSHOP.md` up to its step 5, without BusyBee. Offline only.

## The loop

1. **Earn.** Work a paid shift. After your first one, your cousin Dev leaves a box of salvaged parts at the door. It holds enough to build the gadget for the job you were hired for. It is a gift, not a deduction.
2. **Shop.** On the computer, the **Questionable Components** bookmark sells six parts:

   | Part | Price |
   |---|---|
   | Casing | $8 |
   | Controller board | $14 |
   | Battery | $10 |
   | Tilt sensor | $9 |
   | Vibration motor | $11 |
   | Speaker/display module | $9 |

   You can buy up to 4 of each part per day. Quantities go into a cart and nothing is charged until you review the order and pay. The review shows your cash, what is left afterwards, and how far that is from the rent. The order id is made with the cart, so a second click or a resent request never charges twice.
3. **Delivery.** About 14 seconds later, if you are at home, the courier knocks and the door opens. The parcel lands on the doormat by the front door. Pick it up and click to tear it open, and its parts go into the shared bins.
4. **Build.** At the workbench in the new alcove (through the opening beside the computer desk):
   - Look at the blueprints and start a recipe. This takes its parts out of storage and lays them in the tray.
   - The steps, which tick off on the HUD card:
     1. The casing starts on the mat.
     2. Put the board into the casing.
     3. Plug each module into its keyed port: SNS round and blue, AUD triangle and green, MTR square and orange.
     4. Fasten two screws by holding E with the screwdriver from the cup.
     5. Put the battery in its bay.
     6. Close the lid.
     7. Press the jig's red button.
   - A module in the wrong port fails the test and lights that port red. Open the lid and swap it. This costs time, never parts.
   - A passed test turns the reserved parts into exactly one gadget.
5. **Pack.** At the charging rack, each gadget shows:
   - its charge,
   - which job it helps (and "useless tonight" if it is for the other job),
   - what gives it away.

   Pack one gadget per player. Charging on the rack is free.
6. **Use it at work** with the gadget key, **G**. You can change the key in the Esc menu under "Gadget key".

## The two gadgets

**WhisperBox (call center).**
- **What it does.** At your desk, on a live call, tap G for a hint. Each hint uses 20% of the battery. The hint is built only from what the call has already shown:
  - the next question to ask,
  - the line of the record you looked up,
  - the help page that covers the case. That page is opened and highlighted in the console.

  On a call in a language you claimed, it also glosses the key words of the customer's clue, once they have said them.
- **What it never does.** It never states a detail you have not been told and never names the fix. Tests check this for all six cases.
- **Risk.** It chirps. If the supervisor is within 3.5 m or listening in, she hears it:
  - The first time is a warning.
  - Each repeat costs more suspicion.
  - The HUD warns you when she is close or coming over to listen in.

**SteadyHand (wedding).**
- **What it does.** Tap G to switch it on. While you carry something wide it does three things, and drains while it works (more the harder it works):
  - cuts the wobble the carrier causes by 60%,
  - damps the lean and the rocking,
  - settles a wobble faster.
- **Measured in the wedding scene.** Sprinting with a cake layer, it slipped out of the hands within a second with the SteadyHand off. With it on, the wobble held at 40%. That cost about 6% of the battery a second, with a loud hum.
- **What it does not do.** Jump landings still add wobble, so it does not stop every fall.
- **Risk.**
  - Working hard, it hums, which brings the coordinator over to look.
  - If she has a clear look at it from 5.5 m or less, the HUD warns you first ("She's looking at your tray! Switch it off"). After a 1.5 s grace it is a witnessed incident and she orders you to put it away.
  - Switching it off resolves the incident. Ignoring her order for 6 s costs more.
  - At 0% it eases off over a second. There is no fling and no drop.

Using a gadget in the wrong job, with no call, or with a flat battery explains why and uses no charge.

## Rent is now paid

- **Paying.** The **FlatPay** bookmark pays the rent from savings, and the next rent is due a week later. A rent week fits 7 shifts (days 1 to 7). On the morning after the due day you cannot leave for work until you pay.
- **If you are short.** Once per week you can beg for 2 more days, for a $60 late fee.
- **"Work tomorrow's shift too".** This button on the payout screen now advances the day. Before, repeating a paid shift paid again without the day moving. It sends you home if the rent is due.

## An example week, using the game's actual payouts

The CV says: Customer Experience Professional, 5 years, technical expert, fluent Spanish. That is 4 lies, so the call center pays $178 a shift. On day 4 the crew re-applies as wedding staff, claiming cake specialist: 3 lies, so the wedding pays $190 a shift.

| Day | Shift | Pay | Spent | Savings |
|---|---|---|---|---|
| 1 | Call center: 2 fixed (one in Spanish), 2 hung up: 178 + 12 + 12 − 20 | $182 | | $182 |
| | Dev's box arrives; WhisperBox built from it | | $0 | |
| 2 | Call center with WhisperBox: 3 fixed (1 Spanish, 1 technical), 1 hung up: 178 + 18 + 12 + 15 − 10 | $213 | | $395 |
| 3 | Same again | $213 | | $608 |
| 4 | Parts for a SteadyHand | | $52 | $556 |
| 4 | Wedding: cake 70% (+30), one wrong table sign (−10) | $210 | | $766 |
| 5 | Wedding with SteadyHand: cake 85% (+60) | $250 | | $1016 |
| 6 | Wedding: +30 − 10 | $210 | | $1226 |
| 7 | Wedding: base only | $190 | | $1416 |
| 8 | Rent | | $840 | **$576** |

**A bad week with the same CV.** In my test, an idle player at the call center was paid $138 ($178 base minus 4 hang-ups). Seven of those make $966. That pays the rent with $126 left, and buying one gadget's parts ($41 or $52) mid-week still leaves enough for the rent.

**An honest CV.** It pays $120 at the call center and $140 at the wedding. A modest honest week (about $110 a shift) does not make rent; a near-perfect one just does. That is the lie economy working as designed: the gadgets help you survive bigger lies; they do not replace them.

The first gadget's parts cost 28% (WhisperBox, $41) and 30% (SteadyHand, $52) of a 2-lie shift's base pay. The test `TheFirstGadgetCostsAboutAQuarterOfADecentShift` keeps that true if prices change.

## Saving and safety

- **What is saved.** Parts, orders (paid, then delivered, then opened), gadgets (charge, who packed them), and the build on the bench (reserved parts, where each part sits, screws, lid) are all in `crew.json`.
- **Writing the save.** The save is written to a temporary file and swapped in, keeping `crew.json.bak`; loading falls back to the backup. Saves from earlier versions load with their money, claims and languages intact.
- **What survives a reload.** An order still on its way is delivered again. A delivered parcel is back by the door. An opened one never comes back. A half-built gadget comes back with every part where it was.
- **Locks.** Nobody holds the bench after a reload or after leaving the flat.

## Tests

- **EditMode: 57 tests.** New: `WorkshopLedgerTests` (17) and `WhisperHintTests` (4).
- **PlayMode: 25 tests.** New: `WorkshopTests` (4) and `GadgetTests` (5).
- **What the new tests cover:**
  - a double checkout,
  - stock,
  - one delivery and one unpack per order,
  - reserve, return and consume for builds,
  - dismantling,
  - one gadget per player,
  - charge,
  - the migration of old saves,
  - a reload mid-order, mid-build and while packed,
  - rent and the extension,
  - the hint rules,
  - the full build by hand in the real flat, including the wrong module, and that the crosshair can reach the board slot, a screw and a port,
  - parcels across reloads,
  - SteadyHand's damping, drain, sighting, recovery and flat battery,
  - WhisperBox hints and hearing,
  - a full call center shift to payout with no gadget.

## Not done, or placeholders

- **BusyBee:** not built.
- **Multiplayer:** not implemented. The locks and the one-gadget-per-player rule are written for a host, but only tested solo. There is no co-op teammate hint.
- **Lost and found:** there is none. Gadgets never leave the bag, so there is nothing to drop.
- **Confiscation:** none. Being caught is a warning and suspicion, as the handoff asked for the first loop.
- **The Spanish and Arabic glosses:** these are words taken from the existing (unreviewed) customer lines.
- **Sound:** all synthesised (procedural).
- **The parcel:** it appears on the doormat while the door opens. No courier is shown.

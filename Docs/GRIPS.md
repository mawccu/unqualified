# Grips: how the player's hands hold things

The first-person arms (`Assets/Scripts/Player/FirstPersonArms.cs`) solve each arm from a
shoulder to a wrist target every frame, after the carried thing has been placed for
that frame. `Grips.ForProp` gives the wrist targets and finger shapes;
`PlayerActor.HoldOffset` places the carried thing so the hands holding it are in view.

Every number below was measured on the rendered hand models in play with
`Tools/Eval/grip_probe.cs`. It reports the gap from the palm, each fingertip and the
thumb to the carried thing's box (or, for a fist, to its handle's surface). Positive
means outside, negative means inside. `Assets/Tests/PlayMode/GripTests.cs` enforces
them: within 6 mm, every frame, through movement.

## Profiles

| Profile | Applies to | Hands | Shape | Measured contact |
|---|---|---|---|---|
| Wide | tray, cake layers | both palms up under the side edges, fingers inward under it | `Tray` (flat things, under 45 mm: thumbs up the side and over the rim) or `Support` (tall: thumbs kept down) | tray: palms +1 to +2 mm, fingertips 0 to +3 mm, thumbs 25 mm in from the edge and 4 mm above the rim |
| Paper | rent bill, printouts (a sheet held up) | both, at the side edges a third of the way up | `Sheet`: palm and fingers flat on the back, thumb round the edge onto the front | palms +1 mm, fingertips 0 to +3 mm, thumbs +2 mm from the front |
| FlatPaper | envelopes, order slips | right, pinched from above at one end | `Pinch` | palm on the slip, -2 mm |
| Book | reference binders, guest book | both palms under, the book laid down cover up and leaning back toward you | `Tray` (guest book: thumbs on the pages) or `Cover` (binder, 60 mm thick: thumbs up its sides) | binder: palms +1 to +2 mm, fingertips 0 to +3 mm, thumbs 2 mm off the sides |
| Tool | mop, screwdriver, spatula, party popper, table sign, coiled cable | right fist round the handle (table in `Grips.Handles`, measured from the models) | `Fist`, `FistThin` (handles under 12 mm radius) or `FistThick` (over 20 mm) | mop: front of palm +3 mm, fingertips -4 to 0 mm, thumb +1 mm; popper: fingertips 0 to +4 mm; cable: fingertips -4 to 0 mm |
| OnPalm | takeout box, cloth, cake topper | right palm up under its middle | `Support` | palms +1 to +2 mm |
| Small | mug | right hand round its side | `Wrap` | palm -2 mm |
| Furniture | chairs | both palms on the top of the backrest | `Wrap` | palms 0 mm, looking level or 60 degrees down |

A binder is laid down with its labelled spine toward you, so you can see which one
you hold; its page is on screen while you hold it. Each binder goes back into its
own slot on the reference desk.

## Where things are carried

- One-handed things put the right wrist low on the right, inside the frame at the
  current field of view (`PlayerActor.OneHandInView`).
- A tray comes up until the thumbs on its rim are in view; a tall cake stays low
  with its top below your line of sight (`WideHoldOffset`).
- A book sits low in the middle (`BookHandsInView`).
- Things you read (paper, books) follow 75% of your look up or down; everything else
  follows 50%. The shoulders follow 30% (`FirstPersonArms.ShoulderPitchFollow`), as
  they sit on the torso, not the head.
- A tray's wobble comes from how you move (starts, stops, sharp turns, running), not
  from where you look.

## Causes fixed in the quality gate (2026-11-01)

1. **Below the view.** Everything carried was placed at a fixed height about 39 degrees
   below the middle of the view. The view's half-height is 22 to 25 degrees, so a tray,
   a mug or a guest book and the hands holding them were out of the frame.
2. **Beside, not under.** The tray grip put each wrist 45 mm above the tray's bottom
   and outside its edge, palm up: palms 29 mm off it, thumbs 92 mm.
3. **One grip for everything.** Every small thing got "hand on the right side of its
   box". For a mop that left the hand 15 cm from the shaft. It sank fingers 3 to 4 cm
   into a takeout box. It put the hand in the empty middle of a cable coil.
4. **Shoulders on the head.** The shoulders turned fully with the look, so looking
   down at a chair being dragged swung them back. That left the hands 9 to 14 cm
   short of the backrest, and the backrest target itself was 7 cm behind it.
5. **Paper.** The palm was beside the sheet and the fingers 22 to 30 mm behind it.
6. **Nodding shook the tray.** Wobble read the hold point, which rides on the head.

## Checking a grip

```
bash Tools/grip_session.sh tray            # wedding session, pick something up
echo measure > Tools/Eval/.grip && ucmd script eval_file --file Tools/Eval/grip_probe.cs
echo "shot name side" > Tools/Eval/.grip    # side | left | front | below | hand | fp
bash Tools/grip_survey.sh tag id1 id2 ...  # pick up, measure and photograph each
```

Evidence: `Docs/Polish/Grips/` (`tray_before_*`, `tray_after_*`, `motion/` with the
walking, turning, crouching, looking and jumping frames, `tray_motion_fp.gif`,
`tray_motion_side.gif`, and `survey/` before and after for every prop).

# Characters

The named people in the three levels, and what makes each one recognisable. All of
them use the same four rigged worker bodies (`Assets/Settings/Workers.asset`). Each is
told apart by:
- **costume** (`Assets/Scripts/Avatar/WorkerCostumes.cs`): shirt, vest, trousers, skin and hair colours, hairstyle, accessories;
- **face**: the shared face with or without its moustache (`Assets/Editor/WorkerFaces.cs` builds the clean-shaven body meshes);
- **head extras**: a bun, hair puffs or glasses, rigid pieces on the head bone (`WorkerAvatar.AddExtras`).

Each named character's costume is chosen in code by who they are, not by their seat.

**Pronouns.** Only the landlord, the coordinator and the supervisor have pronouns in
the game's own text; those are marked *sourced*. The game never refers to Dana,
Omar or Kim with a pronoun. Their entries are marked *proposed* (a design proposal
for review, not a fact found in the project), or *undefined*.

**Voice.** No character has recorded or synthesised speech. Lines appear as
subtitles under the speaker's name. A caller's voice is a murmur through the desk
phone's handset (`DeskPhone`, `Tones.Murmur`).

| Character | Role | Pronouns | Body and face | Costume and extras | Lines and subtitle name | Animation |
|---|---|---|---|---|---|---|
| Landlord | Flat: knocks, hands over the rent bill | he/him (sourced: `Landlord.cs`) | worker_host, moustache | Brown vest, cream shirt, grey short hair, belt | Subtitles "Landlord" | knock, offer (bill hand-over), walk |
| Coordinator | Wedding: runs the event and watches the crew | she/her (sourced: `CoordinatorHandoff.cs`, shift text) | worker_host, clean-shaven | Dusty-rose blazer, headset, lanyard, hair tied back with a **bun** | Subtitles "Coordinator" | walk, point, talk |
| Supervisor | Call center: walks the floor, listens in on calls, reads notes | she/her (sourced: `Supervisor.cs`, "What she saw", "at her desk") | worker_host, clean-shaven | Navy cardigan, grey-streaked short hair, lanyard, radio, **glasses** | Subtitles "Supervisor" | walk, talk (greeting), hold paper (listening in) |
| Dana | Senior agent on your pod, takes the technical calls, leans over to help | she/her (**proposed**) | worker_sound, clean-shaven | Mustard vest, dark short hair with two **hair puffs**, headset | Coworker lines under "Dana"; console "Dana leans over" | seated: type, hold paper |
| Omar | Agent opposite you | he/him (**proposed**) | worker_catering, moustache | Slate vest, pompadour, headset, rust tie | Coworker lines under "Omar" | seated: type, hold paper |
| Kim | Agent who is usually on a break | undefined | worker_runner, clean-shaven | Green vest, pink beanie, lanyard | Coworker lines under "Kim" | standing at the break corner; seated: type when back |
| Guests | Wedding guests | n/a | any of the four bodies | Varied colours, bow ties on some; most clean-shaven, one in four with a moustache | none | walk to their seats, sit, impatient when kept waiting |
| You (and teammates) | The player | chosen by the player | by seat in the crew (`ForSlot`) | Crew variants; a shadow-only body in first person, with first-person arms | n/a | full body for others; arms in first person |

## Why Dana changed

The user reported Dana looking like "the wrong character". The game never described
her: no pronoun, no appearance. Every call-center NPC was the same moustached
placeholder recoloured by seat. The supervisor even wore the wedding coordinator's
exact costume. So no mismatch with a written intent could be proved. What could be
fixed was that nobody was recognisable:
- Dana got a deliberate design (above) on a clean-shaven face, with puffs, a headset and a mustard vest;
- Omar and Kim got their own designs;
- the supervisor got her own, no longer the coordinator's clothes.

The proposal is recorded here for the user to confirm or change.

## Where to check it

- Cast sheet from three angles: `Docs/Screens/cast_faces.png`, `cast_3q.png` and `cast_back.png` (`Tools/Eval/cast.cs`).
- In the running call center (After2): `dana.png`, `omar.png`, `kim.png`, `supervisor.png` and `supervisor_glasses.png`, against `Docs/Polish/Before2`.

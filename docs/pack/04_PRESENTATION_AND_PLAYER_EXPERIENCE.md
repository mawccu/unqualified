# 04 — Presentation, controls, and player experience

## Visual direction

Stylized 3D with clean shapes, restrained textures, warm interiors, and exaggerated props. Characters are ordinary adults wearing visibly ill-fitting professional uniforms. Their faces should communicate panic at a distance. The world takes itself more seriously than the crew does.

Avoid photorealism, stock corporate UI, overused neon gradients, and a random mix of asset-store styles. Choose one character scale, one material roughness range, one outline treatment if any, and one lighting reference before buying assets.

The joke needs readability: an obviously crooked cake, a giant manual hidden behind a tiny clipboard, a professional badge attached upside down. A player should understand the physical mistake in a silent three-second clip.

## Camera and character feedback

First-person camera with visible held prop and simple hands; remote players have full bodies. No full-body first-person inverse kinematics requirement in the prototype. Start with a capsule controller, 80° default horizontal-equivalent view tuned per engine, adjustable FOV, disabled head bob by default, and a separate toggle for camera shake.

Do not attach camera rotation directly to ragdoll collisions. Stuns use small animation/audio cues and a short input interruption. The player always knows which direction they will face when control returns.

## Proposed keyboard controls

| Input | Action |
|---|---|
| WASD / mouse | Move / look |
| E | Context interaction or pickup |
| Left mouse | Apply held tool or place at valid socket |
| Right mouse | Rotate held object while placement preview is active |
| Q | Safely drop held object |
| Shift | Sprint with limited handling precision, no stamina bar initially |
| C | Crouch for access; not an invisibility mechanic |
| Middle mouse | World ping |
| Tab | Objectives and chosen promises |
| Esc | Settings; pauses solo, not multiplayer |

Use input actions and rebinding. Controller support is a later release requirement if marketed, not assumed from having a movement stick. Avoid mapping primary interaction and throwing to the same input.

## UI hierarchy

During work, show: time remaining; three compact objective rows; shared suspicion with its latest reason; current interaction prompt; temporary claim warning when relevant. No screen-filling task checklist by default.

Suspicion is a labeled meter, not only a color: Routine, Curious, Watching Closely, Exposed. When it changes show “Planner saw the damaged cake +20.” A small eye cue previews observation dwell before detection completes. Do not expose NPC vision cones outside tutorial/debug modes.

Promises are pinned as short statements with rule tooltips. A question mark alone is not sufficient feedback. The player must see which behavior broke the promise and whether its bonus remains active.

At selection, show contract location, three responsibilities, pay, client temperament, and proposed risk. At debrief, show success/failure, Q, retained claim bonuses, deductions, final payout, and two or three incidents. Individual accolades are playful, never tied to competitive rewards.

## First ten minutes

1. Open to a short agency letter: “Your experience is impressive. Please stop listing relatives as references.”
2. Select Play Training; jump straight into the wedding room. The apartment is not mandatory onboarding.
3. Pick up and place one safe cake section with a clear ghost preview.
4. Open the manual in private. Read three illustrated symbols.
5. Choose the no-manual claim for the real shift. Explicitly explain that being seen matters.
6. A scripted rehearsal shows the client approach cue; close the manual or step behind the door.
7. Begin the normal shift, with no major event for 45 seconds.
8. Let a recoverable mishap teach the repair vs hide tradeoff. Never require deliberately failing the live contract to progress.
9. Debrief identifies the actual incident and the team's recovery.
10. Offer Replay with one new variation, not a dozen unlock screens.

Experienced players can skip training and access the same instructions in the pause/help menu. Joining a friend does not force a tutorial replay.

## Sound and writing

Use distinct approach footsteps, a short suspicion sting, a tool success sound, a warning before a major event, and a small payoff when a cover-up succeeds. Keep dialogue out of the frequency range and volume that bury friend conversation. Subtitle every consequential spoken cue, with speaker name and direction when useful.

NPCs speak with dry certainty: “I assume that noise was included in the quote.” Players supply the swearing; authored dialogue need not shout constantly. Avoid repetitive voice lines after every pickup. Temporary lines can be text with simple nonverbal vocal sounds.

The joke is institutional confidence and human improvisation, not ridiculing a nationality, disability, or accent. “Unqualified” refers to the fictional characters, not the player failing an English-speaking test.

## Communication

Prototype: pings and short authored messages such as “Need help,” “Client coming,” “Bring this,” and “I'll distract.” External friend voice chat is sufficient during internal tests. Built-in voice is a separate feature decision; do not make voice infrastructure block core play.

If added: push-to-talk default, device selection, mute/block controls, individual volume, visible speaking indicator, and no recording by default. Proximity voice is optional flavor; a global team channel must remain available for accessibility.

## Accessibility and comfort

Rebind inputs; adjustable text and subtitle size; high-contrast symbols; color plus shape for object matching; hold/toggle options; separate music, effects, voice sliders; FOV and motion controls; no mandatory speech; no rapid button-mashing; no essential tiny world text.

Relaxed difficulty can extend deadlines and reduce suspicion pressure. The first release can launch in English, but all player-facing strings must use localization keys. Arabic localization is a later product decision requiring right-to-left layout testing, not merely translated strings.

## Asset list and pipeline

Prototype assets: modular floor/walls/door; cake sections and stand; table and cards; flower display; panel and microphone; cloth/manual/spare shelf; one client; one reusable player body with four colors; basic icons and 8–12 feedback sounds.

Track each final asset's author, source, license, modification rights, and attribution obligations in an asset ledger. Placeholder art must be clearly labeled and never marketed as final. Generated or purchased assets still need consistent visual review and usage checks. Claude should use simple original primitives first and never download arbitrary packs without an identified source and permission to use them.

## Trailer experiment

A proposed 20-second clip: résumé says “Five years of event experience”; cake falls; client footsteps; teammate distracts; crew hurriedly rebuilds; client says “Excellent”; camera reveals the manual taped under the table. Only capture mechanics that really work. Compare silent comprehension and viewer curiosity before paying for a cinematic trailer.

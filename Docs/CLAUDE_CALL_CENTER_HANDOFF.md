# UNQUALIFIED — call-center job and language lies

## User-approved direction

Build the next job around a call center. The user specifically wants language claims on the fake CV to have consequences: if a player says they speak a language, customers speaking that language are transferred to that player.

This supersedes the proposed aircraft-mechanic next-job brief. Preserve the apartment, existing wedding job, player hands, and computer scrolling fix. Other details below are proposed implementation defaults. Start with a small playable shift and extend it after validation.

## Premise and first slice

The crew gets hired by a questionable company supporting cheap household electronics. They claimed customer-service experience, technical expertise, and language fluency. Now real customers expect answers.

Start with one office, four desks, a supervisor, a shared reference shelf/printer, and a break corner. Build six short authored cases across three problem families: delayed delivery, product setup, and a refund request. These are fictional company workflows; all required information must be discoverable inside the game.

The loop is answer → identify the problem → search the company system → choose an action → confirm the outcome → record the case. Closing or transferring a call is not automatically solving it.

## 1. Language claims on the fake CV

Add a Languages section with explicit claim toggles. For the initial supported set, propose English, Arabic, and Spanish; ship only languages with completed and reviewed dialogue content. Store stable language IDs and the claiming player's ID, never just display text.

Each toggle previews its consequence: “Calls in this language may be assigned or transferred to you.” Claiming more languages creates access to a multilingual bonus and more responsibility. It should not secretly increase difficulty for the same reward.

The existing CV is crew-wide. Extend it carefully: each player needs a personal language profile for routing, while shared career/debt data remains shared. In solo, that profile belongs to the local player. Keep old saves valid with an empty language-claims list. Do not invent language claims for existing saves.

Example: Sam claims Spanish. A Spanish-speaking customer reaches another worker. The system highlights Sam as the claimed Spanish speaker. A teammate can transfer the case with one action; the supervisor can also explicitly route an incoming Spanish case to Sam. Sam's phone rings with “Spanish support — assigned from your CV.”

## 2. The customer actually uses that language

The customer greeting, problem description, follow-up questions, and reactions use the selected case language. Do not implement this as an English call with a language badge.

Use an authored semantic case definition with localized dialogue variants: the underlying order, problem, clues, and valid outcomes stay the same. Translate the full interaction, not only the first line. Include understandable names, order numbers, dates, and product references without making the puzzle depend on obscure idioms.

Display customer-language captions. Offer a translation/assistance setting so players can enjoy the lie without actually speaking every language. Translated subtitles can reveal meaning while the player still has to diagnose the issue and navigate the workflow. Keep UI language, customer speech language, and optional translation language separate. Do not punish players for enabling accessibility settings.

For Arabic, verify actual font glyph coverage, shaping, right-to-left layout, and mixed-direction order IDs. Test with complete sentences and numbers, not isolated characters or manually reversed strings. Get linguistic review before describing translations as verified.

No microphone, speech recognition, or live generative conversation is required for this slice. Use selectable response intents such as ask for order number, clarify, apologize, hold, transfer, and confirm resolution. Display their meaning in the player's UI language; the character delivers the authored customer-language response. If voice assets are unavailable, deliver clear text calls with telephone sounds and mark voice as incomplete rather than claiming it shipped. Use original or licensed recordings/TTS with documented provenance; do not clone real people's voices.

## 3. Routing and transfers

Represent each call with an ID, language, customer/case state, current owner, queue state, patience, and transfer history. The host/session authority decides ownership; UI and ringing are presentation.

- When a suitable worker is free, route the language call to them. If several claim fluency, choose a free worker using fair rotation.
- When all suitable workers are busy, queue the call with a visible status. Do not interrupt an active call with a second simultaneous conversation.
- A teammate can request a transfer to a suitable worker. Reserve/accept the transfer atomically; the same call cannot be active on two desks.
- Pass collected details, notes, attempted fixes, and remaining patience to the recipient. Never restart the customer's patience or erase mistakes on transfer.
- If nobody claimed the language, provide a general-support fallback with an in-game phrase/reference aid. Do not create an unwinnable assignment.
- On disconnect, release the desk and return the case to the queue or another eligible worker. Late joiners receive the current queue and assignments.
- Rate-limit bouncing a call back and forth; repeated needless transfers visibly frustrate the customer. Legitimate specialist transfers should not be punished.

Initially the caller uses one language throughout. Mid-call language switches are optional later content. Do not introduce that edge case until basic routing is reliable.

## 4. Playable skill and bluffing

The challenge is listening, remembering clues, locating information, choosing the correct action, and handling interruptions—not random answer guessing or real-world language exams.

Examples:

- Delayed delivery: ask for an order ID, locate its dispatch record, distinguish “label created” from “collected,” and choose the company's permitted remedy.
- Product setup: follow the customer's described indicator pattern to the correct fictional troubleshooting branch. Let the customer test the suggestion and report a result.
- Refund: compare the receipt with a short policy, ask for missing information, and select replacement/refund/escalation appropriately.

Technical-expert CV claims route the harder diagnosis variant. Language claims route the language variant. These are separate dimensions. Customer-service claims can add a difficult caller or quality bonus opportunity once the basic cases work.

Bluff responses buy limited time but create promises. “It will arrive today” becomes a recorded commitment that can be checked against the order. An honest clarification can be safer than guessing. Mistakes remain recoverable through correction or an appropriate escalation.

The supervisor occasionally announces a quality check and listens to one call. Suspicion comes from observed contradictions, unsupported promises, fake case closure, or repeated inability to perform claimed skills. A foreign-language call is not itself evidence of fraud. A wrong answer should not instantly end the shift.

## 5. Make co-op physical and funny

Hands lift the handset or adjust a headset, type, use a mouse, write brief case notes, and press hold/mute/transfer. Make call state unmistakable: live, muted microphone, customer on hold, queued transfer, and disconnected are different states.

Friends can bring a printed reference sheet, pass a sticky note, or locate an answer while the assigned worker keeps the customer occupied. Shared references have readable copies or an alternative access path so a missing paper cannot block solo play.

Optional props: rolling chairs with bounded motion, desk drawers, stapler, ringing service bell, radio, vending buttons, mugs, and a printer producing actual case references. Preserve solid collision and avoid physics toys constantly knocking players off their calls.

Do not record or broadcast the user's real microphone as a joke. Without an explicitly enabled voice system, an “unmuted mistake” is an authored character utterance or an in-world sound event.

## 6. Shift balance and progression

Target a short first shift around 8–12 minutes, subject to playtesting. Start with one call per worker and a small bounded queue. Solo receives fewer calls and generous hold time; do not require two distant simultaneous interactions. Introduce the first claimed-language transfer early so players see the mechanic without waiting for random chance.

Make outcomes concrete: correct resolutions, customer satisfaction, justified transfers, unsupported promises, and supervisor findings. Pay base wages plus earned quality/language bonuses with clearly explained deductions. Save payout once and return to the existing apartment/career loop.

On the job website, preserve Wedding Crew and add Call Center Support Agent once its scene is functional. Verify scroll wheel and scrollbar access to every job and the full CV. Do not offer unsupported languages as selectable skills or advertise aircraft mechanics as playable.

## Delivery sequence

1. One desk and one complete English case: answer, query records, resolve, close, score.
2. Personal language CV claims, localized variants, and a working transfer between two workers. Solo must demonstrate automated language assignment too.
3. Remaining case families, supervisor checks, queue balancing, and recoverable mistakes.
4. Hand animations, shared physical references, optional prop interactions, payout, and apartment return.

Use existing project systems and inspect current repository instructions before editing. Do not replace working gameplay architecture merely to add this job. If multiplayer is not actually implemented, report that limitation and do not claim a simulated second worker proves networked routing.

## Acceptance evidence

- Claiming Spanish causes an actual Spanish-language case to reach that player; changing claims changes routing.
- Arabic dialogue renders correctly, including mixed text/order IDs; all enabled languages have complete case content.
- A transferred call retains its exact customer, clues, previous actions, notes, and patience.
- Simultaneous answer/transfer requests never duplicate calls, ownership, resolution credit, or pay.
- Busy/disconnected recipients, empty eligible-worker sets, late joining, and cancelled transfers recover without losing cases.
- A novice can solve each case using in-game information with translation enabled.
- A skillful player earns better results through correct diagnosis and professional handling; guessing has understandable consequences.
- Solo and available multiplayer paths complete job selection → CV → shift → payout → apartment.

Provide a short gameplay capture of a CV language claim, a teammate receiving and transferring that language call, the recipient answering in that language, and the final resolution. Also show the solo routing fallback. Add focused tests for routing, transfer ownership, case preservation, save migration, and once-only payout. List what is implemented, what is placeholder, and what still needs testing.

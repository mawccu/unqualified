# 07 — Research, differentiation, and commercial experiments

Research checked 30 October 2026. Listings can change. This is a targeted comparison of publicly accessible descriptions, not an exhaustive global novelty search, trademark clearance, or hands-on review of unreleased games. Source facts and design inferences are separated below.

## Competitor verification

| Game | Verified from primary listing | Design implication |
|---|---|---|
| Fake It Till You Make It. Online Guru Simulator [S1] | Coming soon; single-player satire about simulated online success and avoiding exposure | The proposed title already has a clear commercial collision; use a working name until clearance |
| Odd Jobs: On The Lot [S2] | 1–4-player physics jobs, changing shifts, wages and upgrades; local shared-screen and Remote Play Together listed | Multiple jobs, comedy, wages and progression are not distinctive alone; do not describe it as native online co-op without evidence |
| Job Simulator: Human Relations [S3] | Coming soon; up to four players, local/online cooperative workplace comedy, tasks and promotion | Significant adjacent competition. The checked listing does not substantiate the pasted “late 2027” release date |
| Clumsy Killers [S4] | Unqualified hitmen, physics stealth, online co-op, avoiding detection and removing evidence; release date TBA | Incompetence plus cover-ups also has adjacent competition, even though the profession and premise differ |
| Surgeon Simulator 2 [S5] | Current publisher note says multiplayer was removed and is being evaluated | Useful historical surgery-chaos reference; do not promise it is currently a working four-player alternative |

The research did not establish an exact match for a rotating-profession co-op game whose selected résumé claims become enforceable shift conditions. That is a bounded finding, not proof that no such game exists. Store copy can omit mechanics, and unreleased projects may be undiscoverable.

## What we can reasonably differentiate

Our proposed combination is voluntary résumé promises, client witnessing, physical cover-up work, and promotion into more absurd responsibilities. The claim-to-consequence mechanic must be visible in the first playable minutes. A logo, setting change, or larger job list is not enough.

No defensible revenue estimate follows from this search. “Viral potential 9/10” is not a metric and should not appear in a pitch as research. Similar listings show an active design space, not verified demand for our implementation.

## AI interview: three designs compared

| Approach | Benefit | Cost/risk | Decision |
|---|---|---|---|
| Scripted interviewer + claim cards | Exact gameplay consequences; offline; fast; accessible | Less free-form surprise | Default |
| Optional typed/speech roleplay mapped to cards | Friends improvise; constrained output | Speech errors, service latency, extra systems | Later experiment |
| Fully live AI recruiter deciding employment and rules | Maximum conversational freedom | Unreliable rules, waiting, costs, abuse, accessibility | Reject as core dependency |

An interview should take roughly 30–45 seconds and be skippable after first exposure. Getting hired should not depend on a speech model correctly hearing a Jordanian accent. Invalid output, timeout or disconnected service must yield the authored selection path.

If explored later, the model may suggest wording and map an answer to a small enum of allowed claims. The player confirms the actual mechanical obligation. Do not infer a severe rule from a joke without showing it. Use a server-held key, per-session limits, bounded text, output validation, consent before transmitting audio, and deletion/retention rules. Do not store recordings by default.

Cost model, with no invented vendor prices:

`monthly cost = interviews × (speech recognition + model input/output + optional speech generation cost) + backend overhead`

Measure actual usage and retry rates before pricing. A one-time game purchase with indefinite service costs is a different business than an offline game. The complete game must remain playable if the interview service ends.

Steam's checked content-survey documentation distinguishes pre-generated and live-generated player-consumed AI content, and asks for guardrail details for live generation [S8]. Review current requirements before submission. AI-assisted coding is not automatically the same category as player-facing generated artwork, narration or live dialogue; report actual use accurately.

## Product hypotheses to test

Audience hypothesis: small friend groups who enjoy short cooperative physical comedy and sharing their mistakes. Positioning: “You lied on the résumé. Now your friends have to back you up.”

Proposed model: premium purchase, complete offline solo and private online co-op, no subscription or microtransactions. Test an illustrative US$9.99–14.99 price range only after scope, playtime and replay value are known. This is a planning range, not a researched optimal price or a commitment.

Avoid a permanent content-service promise. No battle pass. Future jobs could be updates or paid expansions only after the base game satisfies its advertised promise. Price and support costs must be evaluated with real platform terms, refunds, taxes, localization and production spending before a commercial forecast.

## Experiments before a large investment

1. **Hook comprehension:** show an actual short clip without explaining. Ask viewers what makes the game different. If they only say “Overcooked in first person,” the deception is not visible enough.
2. **Replay:** offer a second shift with a different promise. Observe choice and behavior, not only polite praise.
3. **Witness toggle test:** compare a task-only build with the same room's client pressure. Does hiding mistakes create cooperation or merely annoyance?
4. **Interview test:** compare cards against a manually facilitated roleplay interview. If the extra conversation is not fun even with a good human facilitator, do not pay to automate it.
5. **Content production test:** time how long the second job takes using shared systems. That supplies a more honest three-job estimate.

Marketing can begin with real prototype clips after naming checks. Use a clear store pitch and accurate screenshots; do not advertise AI interviews, native online invitations or three jobs until the corresponding commitments are justified. A Steam demo should be a representative slice, not a separate feature-heavy project.

## Risks and responses

| Risk | Early signal | Response |
|---|---|---|
| Looks generic | Viewers cannot identify the promise mechanic | Put claim and consequence in first 20 seconds |
| AI-generated code grows uncontrollably | Huge files, untested RPCs, repeated rewrites | Small tickets, ownership rules, runnable milestones |
| Each job is a separate game | Second job needs new systems everywhere | Reuse verbs and reduce new mechanics |
| Solo is tedious | Player waits for distract/repair windows | Lower quantities, solo tools, sequential objectives |
| Experienced crew trivializes witnesses | Permanent distractor or hidden mess exploit | Cooldowns, inspection and honest quality scoring |
| Too stressful | Players fear touching props | Clear warnings, recoverable damage, Relaxed mode |
| Online support overwhelms team | Invite failures across routers | Prove transport early, friends-only launch scope |

## Primary source register

- **S1:** https://store.steampowered.com/app/4321370/Fake_It_Till_You_Make_It_Online_Guru_Simulator/
- **S2:** https://store.steampowered.com/app/3826040/Odd_Jobs_On_The_Lot/
- **S3:** https://store.steampowered.com/app/4965500/Job_Simulator_Human_Relations/
- **S4:** https://store.steampowered.com/app/4579520/Clumsy_Killers/
- **S5:** https://store.steampowered.com/app/774791/Surgeon_Simulator_2/
- **S6:** https://docs.godotengine.org/en/stable/tutorials/networking/high_level_multiplayer.html
- **S7:** https://partner.steamgames.com/doc/features/multiplayer/networking
- **S8:** https://partner.steamgames.com/doc/gettingstarted/contentsurvey

Technical sources support platform capabilities; the architecture and tuning choices elsewhere are original proposals. No source is being cited as proof of this game's commercial success.

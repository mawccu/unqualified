# UNQUALIFIED — Game Bible

## Elevator pitch

Four unemployed friends use lies, fake references, and absurd confidence to get hired for jobs they cannot do. A shift is a chain of practical problems under observation. The crew has to learn on the fly, coordinate physical tasks, and keep clients from connecting the disasters to their incompetence.

Working title: **UNQUALIFIED**. Do not market or ship under this name without a trademark/storefront check; a different game already uses a near-identical title. Internally, it is fine.

## Player fantasy

You are not a hero becoming an expert. You are an apparently capable adult one sentence ahead of exposure. Competence emerges from teamwork, audacity, and recovering fast enough that the customer calls the outcome “creative.”

The best stories have a clear arc:

1. A player confidently claims they understand something.
2. The game asks them to prove it at the worst possible time.
3. The group panics, makes the situation worse, then divides into visible work and secret repair.
4. The client is distracted or convinced.
5. The debrief reports the ridiculous chain of events.

## Design pillars

### 1. Résumé debt

Before a shift, each player selects or is assigned one claim from a short list. A claim produces an upside now and a concrete obligation later. Examples: “Certified Sound Engineer” unlocks a faster audio setup but later requires that player to answer a client’s technical question or repair a mixer under observation. Never punish a player with an opaque random demand unrelated to their claim.

### 2. Visible incompetence is the real threat

Breaking a vase is a recoverable production problem. Breaking it while the bride is watching creates suspicion. The important systems are witnesses, evidence, line of sight, attention, and plausible explanations—not a generic health bar.

### 3. Cover-ups are cooperative work

One player distracts a witness, one repairs, one hides evidence, and one continues the public-facing task. A cover-up should cost time, create risk, and give everyone a role. It is never an instant “erase mistake” button.

### 4. Simple verbs, deep combinations

Core verbs: move, look, carry, place, connect, clean, inspect, read, operate, speak/choose, distract, and ping. Build surprising interactions from consistent rules rather than an encyclopaedia of minigames.

### 5. Comedy with stakes, never cruelty

Characters are overconfident and jobs are stylized. Clients get frustrated but are not harmed. Failure should produce a new messy problem or a funny debrief, not a hard reset after one mistake.

## Session loop

| Phase | Player experience | System responsibility |
| --- | --- | --- |
| Apartment / job board (later) | Choose a risky job and what to bluff. | Career framing, money, claim selection. |
| Interview | Sell the lie and establish obligations. | Claim selection and future callbacks. |
| Briefing | Learn three outcomes and their due times. | Clear goals, venue orientation, role cues. |
| Shift | Do work, respond to events, hide mistakes. | Task graph, witnesses, suspicion, event director. |
| Inspection | Protect the illusion during a high-pressure check. | Timed observation and proof/evidence rules. |
| Debrief | See what went wrong, what was saved, and what was paid. | Story log, quality grade, payout. |

The prototype starts at briefing and uses a lightweight claim selection screen. Apartment progression is deferred.

## Shared suspicion

Suspicion is a **shared 0–100 meter** representing how close the client is to realizing the crew is fraudulent. It is not a morality score.

- 0–24: normal; the crew has room to learn.
- 25–49: clients notice inconsistencies; more inspections and witness questions.
- 50–74: the client shadows players or demands demonstrations.
- 75–99: crisis; fewer chances to recover and the final inspection becomes strict.
- 100: exposed; finish the remaining moment if possible, then fail the contract in the debrief.

Suspicion rises only from understandable causes: witnessed unsafe/wrong action, neglected public task, contradictory answer, obvious evidence, or a failed cover-up. It decays slowly only while the venue looks acceptable. A successful demonstration can reduce it, but never below 20 once the crew has been meaningfully suspected; the shift remembers the scare.

## Witnesses and evidence

Each important action needs a readable answer to three questions:

1. Who can see or hear this?
2. What physical or visual evidence remains afterward?
3. How can the crew plausibly remove, fix, hide, or explain it?

Witnesses have states: `busy`, `casually watching`, `suspicious`, `inspecting`, `distracted`. A distraction changes attention temporarily; it does not freeze the world. Evidence is a stateful object or venue condition—spilled frosting, wrong cables, burnt food, a broken centerpiece—not an invisible flag.

## Event director rules

The director selects problems based on shift phase, unfulfilled claims, current suspicion, player count, and recently used events. It must not stack two mandatory emergencies on a solo player without recovery time.

An event card contains: trigger window, setup, public risk, required verbs, possible recovery routes, failure result, and cooldown. Every event needs at least two solutions: correct execution and believable recovery. Randomness should alter circumstances and order, not erase player agency.

## Progression and jobs

Long-term progression is a **scope promise**, not power inflation. Higher-paying jobs have more obligations, stranger equipment, and less time—not larger damage numbers. Initial release target: three distinct jobs after the wedding slice.

- Wedding crew: performance under social observation; decorations, catering, sound, ceremony timing.
- Fire station callout: staged emergency set pieces; containment, communication, public reassurance. No realistic firefighting instruction.
- Aircraft ground service: fictional airline and simplified equipment; checklists, baggage, cabin turnaround, gate pressure. No real aircraft maintenance procedures.

The escalation joke is that the group keeps getting hired for more responsibility because each escaped disaster appears on a résumé as experience.

## Tone, writing, and presentation

Visual language: colourful, slightly cheap event-service world; chunky interactables; exaggerated readable animations; public areas feel polished while staff spaces are chaotic. Dialogue is short, specific, and reactive. Avoid a nonstop meme voice. The funniest line is often a calm professional sentence delivered next to a disaster: “That is an intentional seating-flow intervention.”

Use fictional companies, venues, clients, certifications, and equipment. Never parody a real employer or teach a dangerous procedure.

## Guardrails against the wrong game

Do not turn UNQUALIFIED into:

- a list of unrelated timed chores with no social consequence;
- a pure physics sandbox where mess is automatically funny;
- a realistic professional simulator;
- a PvP sabotage game where friends are the main enemy;
- a punishment-heavy stealth game where one mistake ends a shift;
- a massive life sim before a single job is fun.


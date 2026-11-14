# SWARMFORGE — QA-005 / GDD v1.2 configurable playtest and regression plan

**Owner directive:** [GITHUB-005 / Issue #18](https://github.com/Gogenco/swarmforge-studio/issues/18#issuecomment-6880330660)  
**Role:** 09 — Senior Manual & Automation QA Engineer; [QA Issue #9](https://github.com/Gogenco/swarmforge-studio/issues/9)  
**Baseline:** DEV-002.2 `main`, merge `99cfcdc0b4982e9e441cf23a8669a487a7d75ea9`; [PR #10](https://github.com/Gogenco/swarmforge-studio/pull/10)  
**Design proposal:** [GDD v1.2 Draft PR #19](https://github.com/Gogenco/swarmforge-studio/pull/19) — **MUST be reconciled with later Owner decisions**  
**Legacy independent regression:** [QA-003 Draft PR #11](https://github.com/Gogenco/swarmforge-studio/pull/11) — preserved independently  
**Status:** `TEST_PLAN_READY_FOR_REVIEW / EXECUTION_BLOCKED`. No runtime build or Unity tests under this task.

## 1. Authority and test-oracle matrix

The following are **Owner-approved initial design constraints** from successive [#18 decisions](https://github.com/Gogenco/swarmforge-studio/issues/18). They are **not proof of runtime implementation or tuned balance**.

| Rule | Approved target | QA oracle / configuration requirement |
|---|---|---|
| Box damage | **One friendly MiniBot consumed for each successful box HP hit**; no separate box projectile mode | One authoritative hit reduces box HP once, consumes exactly the hitting MiniBot, and does not depend on that MiniBot's remaining combat HP; box reward granted once at HP zero |
| Multipliers / boxes | ×2 box **5 HP**; ×3 **10 HP**; ×5 **25 HP** | 5/10/25 successful 1-damage hits for initially undamaged boxes; partial hits persist; damage and reward logged with stable IDs |
| New arena target | Destructible side boxes **replace fixed ×1/×2/×3 gates** | New v1.2 arena has no automatic legacy gate multiplier. **Do not delete** DEV-002.2 gates, tests, scene or evidence before approved migration |
| Boost ×2 | **12 s**, time-only, no shot quota | Authoritative activation starts timer, nominal 360 fixed ticks at 30 Hz; exact inclusive tick boundary deferred to Architect |
| Boost ×3 | **10 s**, time-only, no shot quota | Nominal 300 ticks; applies to eligible future ATTACK shots, not retroactive in-flight shots |
| Boost ×5 | **6 s**, time-only, no shot quota | Nominal 180 ticks; no unlimited extra multiplier stacking |
| Boost queue | **One active + one pending**; sequential, no stacking | Pending reward receives full duration after current expires; while both slots full, other multiplier boxes **visibly locked before further MiniBots are spent**; retain existing box HP; no silently lost reward |
| Friendly combat | Friendly MiniBots survive **some** enemy contacts (multi-HP) | Nonlethal enemy contact preserves survivor and HP. Exact HP/damage/contact cadence still OPEN |
| Waves | Enemies arrive in **simultaneous groups**; escalation in number, durability and type | Multiple enemies share a planned spawn event/tick as approved schedule specifies; no isolated trivial one-by-one waves |
| Warnings | Wave countdown, `WAVE n/N`, boss entrance, base-under-attack; **gameplay continues** | No pause/block of aim, box target or simulation; mobile safe-area/readability |
| Balance | Editable, versioned HP/damage/wave/box/boost values; **HP=2 vs HP=3 is NOT decided** | Compare two presets under identical conditions, archive config version/hash and empirical outcomes; Owner selects final tuning only after playtest |

### Pending Owner / Architect decisions (not test assertions yet)

- Combat damage exchange, cooldown/contact cadence, survivor target persistence, enemy HP/armor and boss HP.
- Exact world-space 2D contact geometry and equal-time priority among box, enemy, base, fortress, spawn and boost expiry.
- Box targeting/reservation state while units are in flight; same-tick box destruction, reward activation and newly fired shots.
- Precise inclusive/exclusive activation and expiration tick boundaries; boost interaction with Forge/non-multiplier rewards.
- Enemy group sizes/spawn times, baseline damage budgets, level definitions and fairness gates.
- First implemented VS-01 slice and legacy-gate migration/replay compatibility.

Do **not** freeze these as pass/fail or copy obsolete numerical rules from early [PR #19](https://github.com/Gogenco/swarmforge-studio/pull/19), which still proposes shot-charge boosts and 1-HP friendly collision losses at its last reviewed HEAD.

## 2. Matched playtest presets — HP2 vs HP3

Only the **friendly MiniBot maximum combat HP** differs between matched runs, unless Owner/Architect expressly approves a controlled follow-up tuning variable.

| Field | Preset A — HP2 candidate | Preset B — HP3 candidate |
|---|---|---|
| `friendlyMiniBotMaxHP` | **2 — NOT APPROVED** | **3 — NOT APPROVED** |
| Friendly damage / hit cadence | Same approved config version | Identical |
| Enemy HP/damage/archetypes | Same approved config version | Identical |
| Wave groups / spawn schedule | Same event script and seed | Identical |
| Box HP | ×2=5, ×3=10, ×5=25 | Identical |
| Boost durations | 12s, 10s, 6s | Identical |
| Boost queue | active1 + pending1, no stacking | Identical |
| Arena geometry and fixed-gate mode | Same v1.2 test arena | Identical |
| Input commands / aim sequence | Same deterministic input log | Identical |
| Rendering/FPS/device | Same test device & build; paired comparison | Identical |
| Config provenance | Separate full version and content hash (A) | Separate full version and content hash (B) |

**Protocol:** first run both presets with a deterministic scripted input stream and matching wave schedule, then an equal number of manual playtests with randomized presentation order. Candidate starting sample: ≥10 reproducible simulated runs **per preset per scripted scenario** and ≥5 paired human playtest sessions, **subject to Owner approval and actual tool/device access**. These are planning targets, not completed runs.

**Metrics:** win/loss and reason; base HP at T=15/30/45/60 s; fortress HP remaining; time-to-kill by enemy archetype; nonlethal contact count; surviving MiniBot fraction and average lifetime; box completion time, MiniBots consumed for reward, time diverted from defense; boost uptime/queue wait; group leak count; frame time p50/p95, CPU/GC, memory/thermal (with Performance #8); UI hit/aim accuracy and player-reported clarity. Record **raw values**, source logs and replay hashes, not an invented overall score.

**Fairness probes:** early ×2, early ×3, early ×5, skip all boxes, defend-first, delayed-box, failed-box-attempt, back-to-back rewards, boss/wave overlap. The stronger HP3 preset must not be assumed preferable before actual results; neither preset is a final default.

## 3. QA-005 planned test matrix (all NOT RUN)

Legend: `P0` = release-critical integrity; `P1` = high risk; `P2` = visual/usability. Fixtures and exact numerical expectations require approved GDD/Architect versions.

| ID | P | Scenario | Expected / evidence |
|---|---|---|---|
| V12-Q01 | P0 | Box ×2 starts at 5 HP, receives five successful hits | HP 5→0, exactly five consumed bots and one reward; no award at 1 HP |
| V12-Q02 | P0 | Box ×3 at 10 HP and box ×5 at 25 HP | Precisely 10/25 successful hits per intact box; no double damage/reward |
| V12-Q03 | P0 | Friendly combat HP2 or HP3 MiniBot successfully hits a box | **Consumed** regardless of remaining combat HP; box hit counted once |
| V12-Q04 | P0 | One bot collides with box and enemy in same tick at varying sub-tick times | Earliest valid event wins; equal-time follows signed Architect priority; no double consumption |
| V12-Q05 | P0 | Two bots impact a 1-HP box simultaneously | One HP transition, reward exactly once; surplus handling follows signed reservation rules; stable event hash |
| V12-Q06 | P1 | Partially damaged box remains after player switches to defense | HP and visual damage remain; new acquisition resumes correctly |
| V12-Q07 | P0 | New v1.2 arena uses side boxes rather than fixed gates | No automatic ×2/×3 from legacy gates; old baseline tests retained on legacy fixture |
| V12-Q08 | P0 | Earn ×2 and fire throughout its active timer | Multiplier ×2 for eligible future ATTACK shots during 12s; **no shot-quota decrement** |
| V12-Q09 | P0 | Earn ×3 and ×5; time-boundary shots | ×3 10s, ×5 6s; no retroactive boost to in-flight shots; exact boundary per Architect |
| V12-Q10 | P0 | Earn ×3 while ×2 active | ×2 continues through expiry; ×3 enters pending then activates for **full 10s**; no ×6 stacking |
| V12-Q11 | P0 | Both slots full and player aims at third multiplier box | Box visibly locked and untargetable **before bot spending**; no reward disappears; prior partial HP stays |
| V12-Q12 | P0 | Pending reward and active boost expire / terminal / restart at same tick | No duplicate activation or leaked pending state; deterministic approved tie policy |
| V12-Q13 | P0 | Compare combat HP2 and HP3 after one or more nonlethal enemy hits | Survivors retain IDs and correct reduced HP; death only at approved threshold/cadence |
| V12-Q14 | P0 | Friendly bot survives contact and meets same enemy again | Damage and cooldown follow signed repeat-contact semantics; no per-render-frame duplicate damage |
| V12-Q15 | P0 | Armored enemy survives nonlethal hits from several bots | HP/damage event ledger exactly once per hit; correct death/breach and remaining HP; no one-tap kill |
| V12-Q16 | P0 | Planned enemy group spawns at one 30-Hz tick | Stable unique IDs; approved group count and placement; no unintended serial trickle |
| V12-Q17 | P1 | Later groups grow in number/HP/archetypes without instant post-reward counter-scaling | Compare actual authored schedule and risk window; report balance metrics, not invented PASS |
| V12-Q18 | P0 | Burst ×5 near dense grouped wave and boss; high unit counts | No lost logical units, cloned IDs unique, valid chronological combat; replay stable |
| V12-Q19 | P0 | Re-run identical 30-Hz simulation with HP2 vs HP3 config and 30/60 display FPS | Same hash within **each** preset across render FPS; differing preset hashes are expected when state differs |
| V12-Q20 | P0 | Change one editable balance field without code modification | Config version/hash changes, expected behavior changes, no stale Inspector value or silent defaults |
| V12-Q21 | P1 | Invalid bounds, fractional duration in 30-Hz config, missing wave archetype | Explicit validation/fail-closed behavior; no nondeterministic coercion |
| V12-Q22 | P1 | Wave warning, `WAVE n/N`, boss entrance and base-threat at same time | Gameplay runs uninterrupted; alerts follow approved priority and cannot hide box/aim targets |
| V12-Q23 | P2 | 320×568, 360×640, 390×844 portrait and notched safe areas | Readable HP/lock/queue/timer/wave state, touch targets accessible; record screenshots and UI approval |
| V12-Q24 | P1 | Background/foreground during active/pending multiplier and group warning | No lost/extra ticks, duplicated reward, timer skip or stale UI on Android/iOS; report unavailable device as NOT TESTED |
| V12-Q25 | P1 | HP2/HP3 scripted balance comparison, with/without box choices | Paired objective base survival, TTK, win rate, reward cost, available decision windows |
| V12-Q26 | P1 | Repeated 60s matches at peak group/box/boost load on entry-class Android/iOS | Device FPS/frametime, GC, memory, temperature, startup/crash evidence; Performance #8 owns target budget |
| V12-Q27 | P0 | Legacy DEV-002.2 gate/contact bug QA2-01 regression after migration | Old test fixture remains reproducible and outcome explicit; new box chronology independently verified |
| V12-Q28 | P1 | Boss/wave audio cues and optional haptics during pause or alert overlap | Exactly one per authoritative event, settings honored; no simulation effect |

## 4. How to capture execution evidence

For each test use: `Test ID | status PASS/FAIL/NOT TESTED | exact GDD/ARCH approval SHA | PR/build SHA | config version/hash | seed | input/replay log | expected vs actual | Unity/device version | screenshot/XML/log | bug severity + owner`.

**Commands to preserve:** target sector/aim position, fire timing, box lock states, wave triggers and pause/restart.  
**Snapshots to preserve:** tick, stable BotId/EnemyId/BoxId, respective HP, active/pending boost type + remaining ticks, events, StateHash/EventHash/CommandHash, base/fortress HP and terminal reason.

For matched HP2-vs-HP3 runs, attach a reproducible pair of input command logs and output tables under identical scene, wave and enemy config. Do not blend manually played results with deterministic replay observations.

## 5. Dependencies, review and release boundary

| Owner | Required input / review |
|---|---|
| Game Designer #3 / [PR #19](https://github.com/Gogenco/swarmforge-studio/pull/19) | Reconcile obsolete shot-charge, friendly 1HP and isolated-wave assumptions; specify both editable HP2/HP3 presets and comparable waves |
| Architect #2 / [ARCH-003 PR #14](https://github.com/Gogenco/swarmforge-studio/pull/14) | Approve 2D chronology, survivable hits/repeat cadence, queue/timer exact tick semantics, box target locking, config validation and QA2-01 migration |
| Developer #4 | Provide approved staged build, runtime instrumentation, config loader/version/hash, tests and PlayMode scenes **after authorization** |
| UI #6, Art #5, Sound #7 | Box HP/lock state, active/queued boost, surviving-hit visuals, warnings and nonblocking one-thumb controls |
| Performance #8 | Device class and CPU/memory/GC targets, worst-case swarm and group budgets; independently measured device metrics |
| Owner #1 | Approve updated GDD/ARCH and implementation slice, tune final HP only after actual playtest, authorize eventual merges |

**Non-goals:** no Unity code/assets/settings changed, no old gate system removal, no arbitrary final HP selection, no unapproved final wave/HP numbers, no runtime PASS claims, no PR merge, no VS-02.

**Current status:** `QA005_PLAN_PREPARED / QA003_RUNTIME_BLOCKED / WAITING_GDD_ARCH_AND_OWNER`. This document is a **proposed acceptance and playtest plan**, not an implementation result or release sign-off.

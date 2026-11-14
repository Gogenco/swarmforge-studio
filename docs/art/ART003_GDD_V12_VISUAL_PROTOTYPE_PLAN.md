# ART-003 — GDD v1.2 Visual Prototype Preparation and Art State Contract

**Status:** DRAFT / READY FOR OWNER REVIEW — DOCUMENTATION ONLY  
**Role:** 05 — Lead Game Graphic & Media Designer / Art Director  
**Owner task:** [GITHUB-005 / #18](https://github.com/Gogenco/swarmforge-studio/issues/18#issuecomment-6880330660)  
**Art thread:** [#5](https://github.com/Gogenco/swarmforge-studio/issues/5)  
**Code baseline:** DEV-002.2 `main` [`99cfcdc0b4982e9e441cf23a8669a487a7d75ea9`](https://github.com/Gogenco/swarmforge-studio/commit/99cfcdc0b4982e9e441cf23a8669a487a7d75ea9)  
**Design proposal undergoing revision:** [GDD v1.2 Draft PR #19](https://github.com/Gogenco/swarmforge-studio/pull/19)  
**Previous art baseline (preserve unchanged):** [ART-002 Draft PR #13](https://github.com/Gogenco/swarmforge-studio/pull/13)  
**Handoffs:** [UI #6 / PR #17](https://github.com/Gogenco/swarmforge-studio/pull/17), [Architecture #2](https://github.com/Gogenco/swarmforge-studio/issues/2), [Developer #4](https://github.com/Gogenco/swarmforge-studio/issues/4), [Sound #7](https://github.com/Gogenco/swarmforge-studio/issues/7), [PERF #8](https://github.com/Gogenco/swarmforge-studio/issues/8), [QA #9](https://github.com/Gogenco/swarmforge-studio/issues/9)  
**Date:** 2026-11-14

> This document prepares the **visual specification and acceptance checks**, not game art implementation. Do not change Unity source, meshes, textures, shaders, prefabs, URP assets, levels, or old mechanics from it. Owner-approved GDD v1.2 and Architect contracts are required before implementation. No VS-02 work. QA2-01 remains open.

## 1. Decision authority and versioning

The canonical **Owner [GITHUB-005 decision](https://github.com/Gogenco/swarmforge-studio/issues/18#issuecomment-6880330660)** overrides any earlier **hypotheses** in the initial Draft PR #19. In particular, its original shot-charge multipliers and one-hit-per-MiniBot enemy combat are superseded. This is not a silent amendment to Draft PR #19; the Game Designer owns its revision.

| Topic | Now approved by Owner | Still configurable / open |
| --- | --- | --- |
| Box hit | One successful box-damaging MiniBot hit reduces box HP by one and **consumes that MiniBot**; no separate cannon box-projectile | Contact timing/reservations, shot visual, safe targeting and exact event naming require Architect |
| Field | **Replace fixed ×1/×2/×3 gates** with destructible **side boxes in new v1.2 arenas** | Preserve DEV-002.2 gate scenes/code/tests until separate migration approval |
| Initial multiplier box durability | **×2: 5 HP; ×3: 10 HP; ×5: 25 HP** | Values must be in editable/versioned balance configuration for playtests; no hard-coding in art |
| Timed boost | **×2: 12 s; ×3: 10 s; ×5: 6 s**; no shot quotas | Exact expiry boundaries/simulation clocks by Architect; config remains editable |
| Multiplier order | No stacking: **one ACTIVE and one QUEUED**; queue starts after active expires | Other multiplier boxes are visibly **TARGET-LOCKED** while queue full; exact targeting contract by Architect |
| Friendly MiniBot combat | **Can survive some enemy hits**; not consumed on every enemy contact | Exact HP, damage, hit cadence, survivor indicators. **HP 2 vs 3 explicitly deferred to playtest**, neither final |
| Enemies | Spawn in **simultaneous groups** with increasing toughness/variety, boss may have escorts | Archetype numbers, damage and group pacing, target route and counts |
| Alerts | Nonblocking wave, boss and base-threat warnings; one-thumb control remains live | Exact timing/priority/layout to reconcile GDD, UI, Sound, Architect |
| Future mechanics | No VS-02; no Forge/Mech/Titan implementation here | Separate Owner authorization |

Design strategy: **one silhouette = one gameplay meaning**; HUD/badges must provide non-color confirmation. All world-rendered visuals consume authoritative presentation state, not simulated effects that can alter 30 Hz logic or replay hashes.

## 2. Visual style and object grammar (proposed, not approved assets)

Reuse ART-002's compact tactical sci-fi identity and role palette: dark navy field, friendly mint-green capsules, hostile coral-red machines, ×2 cool cyan, ×3 warm amber, neutral slate. ×5 may use a *distinct high-value amber/gold rim plus five-notch shape*; do not confuse it with a guaranteed winning choice. These are **art tokens**, not finalized Unity material values or verified contrast.

| Object / category | Distinct visible shape and persistent information | Avoid |
| --- | --- | --- |
| Friendly MiniBot | Slim upright capsule / rounded robot, mint body; repeatable short impact reaction that returns to the alive pose when HP remains | Vanishing on every enemy hit; fixed 1-HP model; per-bot complex UI in dense crowds |
| Enemy Scout/Runner | Round / elongated hostile silhouette; red/coral family, motion posture cue independent of speed | Hue-only separation or enemy-vs-MiniBot same silhouette |
| Armored enemy | Hostile silhouette + **outer armor ring/plating**, damage to plates visible after nonlethal hits | Assuming all armor breaks in one hit; hardcoding enemy HP |
| Boss | Large unique asymmetrical hostile profile, prominent body/core/health indication; escorts remain visually distinct | Full-screen boss model or entrance covering boxes, aim lane or base |
| Side multiplier box | **Distinct crate/hex-corner silhouette** with persistent `×2` / `×3` / `×5` badge, HP text, health fill and class notches | Drawing boxes as old straight gate strips or as identical enemy units |
| Other future reward box | Shield icon / weapon reticle / rate symbol with text cue, visually different reward family | Implementing these reward systems/assets without separate approved GDD slice |
| Base / cannon / fortress | Retain DEV-002.2 identity for now, separate cyan base from bonus glyphs | Unapproved scene/geometry changes |

**Approved box example labels** (data driven): `×2 · 5/5`, `×3 · 10/10`, `×5 · 25/25`. When config changes later, labels must show actual `HP / MaxHP` without replacing textures manually.

### 2.1 Dynamic damage representations

**Box:** show an icon, numeric remaining HP, and a short horizontal progress fill `remaining/max`. Optional crack/detail thresholds at remaining ratio `<=0.75`, `<=0.5`, `<=0.25`. Crack stages are **visual-only**: neither HP values nor number of required hits are derived from graphics. Use matte fill and avoid stochastic debris obscuring targets.

**Friendly survivor (HP 2 vs HP 3 preset):** same base model, rig and color. Show each confirmed nonlethal damage event using a short directional/pulsed hit cue; health display must derive from **currentHP / maxHP**, never assume exactly 2 or 3 in art code. Proposed selection for Owner/UI review: do not attach individual persistent bars to all 120 visible bots; use context-sensitive small pips on emphasized/frontmost robots, with optional fading damage rim elsewhere. Actual emphasis policy depends on renderer and QA readability. Destroy/return to pool **only when authoritative HP reaches zero**. Exception: confirmed successful **box hit consumes** the MiniBot regardless of its surviving-combat HP.

**Enemy multi-hit:** when health remains, animate visible armor crack or brief hit flash and keep the unit active. Enemy death/despawn FX only on authoritative defeat event. Boss can have an on-screen health indicator **if UI approves its area**; health is configurable.

**Group spawns:** preserve individual category silhouettes within a pack; use a clear group arrival cue at the spawn line but avoid a solid screen-wide flash that hides first attacks. Enemy numbers/colors are not rendered as a separate intensity-based difficulty promise.

## 3. Prototype visual state matrix (presentation-only proposal)

| Entity state | UI/world appearance | Transition/input rule |
| --- | --- | --- |
| `BOX_AVAILABLE` | Full-color crate, visible reward and `HP / MaxHP` | No visual claim that the reward has been obtained |
| `BOX_TARGETED` | Narrow high-contrast contour / aim marker | Follow **authoritative** target eligibility and input state, never choose target in visuals |
| `BOX_DAMAGE_CONFIRMED` | Small HP decrement and brief hit/crack cue; consumed bot vanishes once | Damage cue only on a **successful** event, not projectile proximity |
| `BOX_IN_FLIGHT_PENDING` | Optional `PENDING` indicator when missing HP is already covered by reserved in-flight hits | **Conditional** on final reservation contract; initial GDD PR #19 proposal is not yet an approved Architect contract |
| `BOX_LOCKED_QUEUE_FULL` | Desaturated/dashed border, distinct lock pictogram and `LOCKED` word | While ACTIVE+QUEUED is full, player must not be visually invited to spend MiniBots; no misleading hit flash |
| `BOX_BROKEN` | One short collapse/fade animation and reward badge; box no longer selectable | Exactly one break cue for one authoritative box-destroyed event |
| `BOOST_ACTIVE` | Prominent `ACTIVE ×N` + time remaining, with distinct active border | Countdown is derived from **authoritative activation/expiry ticks**, no local timer deciding expiry |
| `BOOST_QUEUED` | Smaller `NEXT ×N · WAIT` badge and queue slot outline | **No countdown while queued**; starts full duration only on activation |
| `BOOST_EXPIRED` | Brief low-noise fade; queued badge moves into active position | No visual or gameplay stacking |
| `MINIBOT_HIT_SURVIVED` | Fast nonlethal hit reaction; robot stays in lane | Does not imply consumption; HP2 and HP3 prototypes both supported |
| `MINIBOT_DEFEATED` | Minimal death/fade and pool return | Only on authoritative 0 HP |
| `ENEMY_ARMOR_HIT` | Visible plate damage/impact, hostile remains | Repeatable without spawning a new enemy instance |
| `WAVE_WARNING` | Compact `WAVE n/N IN …` with clear countdown | Nonmodal, no pause and no touch interception |
| `WAVE_SPAWNED` | Brief wave-name callout; pack remains fully visible | Group spawns concurrently; art must not serialize their appearance as single-file |
| `BOSS_INCOMING` | Distinct high-salience boss warning + arrival silhouette cue | Does not hide cannon, side boxes, base or threat lanes |
| `BASE_THREAT` | Persistent but restrained `BASE UNDER ATTACK!` pulse | Must not spam 30 times/second; hysteresis and event order owned by Architect |
| `TERMINAL` | Existing readable Victory/Defeat/Time Up card | Existing DEV-002.2 terminal priority retained until Owner signs GDD migration |

All state names above are **handoff labels**, not approved runtime enum/API identifiers. UI #6 owns screen-space layout and typography; Architect owns event/state contract; Developer owns Unity integration.

## 4. Timed boost display, queue and target lock

Examples use approved **initial balance values**, but must be driven by configurable max duration and authoritative remaining time.

| Reward | Box display | When ACTIVE | When QUEUED | When capacity full |
| --- | --- | --- | --- | --- |
| ×2 | `×2 · 5/5` | `ACTIVE ×2 · 12 s` at activation | `NEXT ×2 · WAIT` | Other boost boxes show `LOCKED` |
| ×3 | `×3 · 10/10` | `ACTIVE ×3 · 10 s` at activation | `NEXT ×3 · WAIT` | Other boost boxes show `LOCKED` |
| ×5 | `×5 · 25/25` | `ACTIVE ×5 · 6 s` at activation | `NEXT ×5 · WAIT` | Other boost boxes show `LOCKED` |

- Visual `remainingSeconds` convention, fractional/rounded boundary and queue transition exact tick are **Architect + UI decisions**; never locally count Unity rendered frames as an authority.
- An earned queued reward **cannot disappear silently** when current active boost ends. Transition is observable and retains distinct ×N type.
- A box should not continue to show an attractive `AVAILABLE` targeting state if the authoritative system says queue capacity is full.
- An active boost should be visibly different from a permanent gate bonus. New arenas have **no fixed multiplier gates**; preserve old gate assets/scene until approved migration.
- If an unlocked bonus appears during `BASE UNDER ATTACK!`, the danger alert has foreground priority without suppressing the persistent ACTIVE/QUEUED indicator.

## 5. Animation and VFX cues (production briefs only)

| Trigger from authoritative event | Visual cue brief | Review condition |
| --- | --- | --- |
| MiniBot hits box | Single contact pulse on crate + HP decrement; consumed unit removes at hit confirmation | Exactly one cue per event ID; do not preplay at target reservation |
| Box breaks | Brief controlled crumble/scale and unique reward glyph appearing once | No duplicate grant visuals after simultaneous contacts/replay |
| Friendly survives enemy contact | Short nonlethal flash/tilt (directional optional) then returns to moving pose | Clearly distinct from robot defeat; HP2/HP3 both readable |
| Friendly defeated | Small fade/collapse, no lingering collider appearance | Not triggered by nonlethal contact |
| Armor takes damage | Plate flash/crack or colored slash, no desaturating whole scene | Multiple consecutive hits cannot flicker uncontrollably |
| Group wave arrives | Short lane-line/entry marker for all concurrently spawned enemies | No artificial sequential stagger that implies one-by-one spawns |
| Boss appears | Distinct silhouette reveal plus compact warning card | Escorts not hidden, no blocking modal overlay |
| Boost granted/activated/queued/expired | Different quiet icon motion for each state; queued acquisition does **not** pretend boost is active | Timing from approved events only; no stacking cues |
| Base threatened | Sparse pulse of threat outline/text | Clear on authoritative threat exit, no persistent strobes |

**Accessibility:** maintain text+symbol+shape redundancy; have a reduced-motion / no-flash alternative for all essential signals. No heavy screen shake, strobe, automatic full-screen cutscene, particle wall, mandatory audio cue or touch-blocking UI. Do not assume a specific effect length without UI/Sound/Owner acceptance.

**Performance principles:** prefer pooled primitive/simple mesh variants, shared materials, atlas-compatible glyphs, few simultaneous one-shot cues, no per-unit unique material allocations or particle carpets. Baseline `MaxVisibleMiniBots=120` is a **render cap only**, not a game-balance population limit. Ask PERF #8 for approved draw-call, VFX concurrency and memory budgets; do not invent measured FPS.

## 6. Information hierarchy and mobile-safe layout dependency

Persistent: base HP / fortress HP / time / `WAVE n/N` plus current and queued boost. World: visible left/right box reward and durability, group enemies, aim destination. Transient messages (proposed order): **terminal > base threat > boss warning > wave warning/name > reward toast**. UI #6 and Owner must approve final priorities.

- Wave/boss/base messages must run while gameplay and one-thumb aiming remain active. Do not place hit-test blockers over the bottom quarter drag zone.
- Side boxes at both edges must remain fully visible, including their HP+reward badge on 320×568 and 390×844 notched layouts. If world-space badges collide with HUD or cutouts, UI #6 must resolve placement without falsifying world target coordinates.
- Old `UI-002` SVGs in PR #17 show the **DEV-002.2 fixed-gate layout**. They are useful for typography/safe-area standards but **not** authoritative v1.2 box/wave mockups. UI must author a distinct future design delta after GDD review.
- Existing Action yellow and ×3 amber can compete. Keep action buttons as **filled rounded controls** and multiplier boxes as **crate glyphs/numeric badges**, differentiated by shape and location as well as color.

## 7. Prototype art preparation and asset inventory

**Allowed now (documentation/review):** this state contract, lists of icon silhouettes, proposed color semantics, animation briefs, mockup instructions, test plan. **Not authorized yet:** Unity meshes, textures, runtime sprites, shader changes, Animator or VFX implementation, packages, external asset purchasing, scene editing.

**Minimum eventual prototype asset slots after separate implementation approval:**

| Proposed asset slot | Variants | Low-cost approach |
| --- | --- | --- |
| `Box_Multiplier` | ×2 / ×3 / ×5 via badge data; available / targeted / locked / broken | One reusable low-poly crate mesh; reward/HP as data-fed UI |
| `MiniBot_Alive` | Friendly HP2 / HP3 **same geometry**; damage indicator from normalized health | Retain existing capsule until Owner signs production art |
| `Enemy_Standard`, `Enemy_Armored` | Small round/elongated vs plate/ring | Primitive silhouettes and shared materials |
| `Boss_Hostile` | Singular large unique hostile shape with escorts | Simple geometry until performance budget approved |
| `Boost_ActiveQueued` | ×2 / ×3 / ×5, time progress; LOCKED indicator | Reusable text/icon atlas or UI shapes |
| `Alert_WaveBossBase` | Wave warning, boss warning, base danger | UI-owned safe-area reusable nonblocking banner |
| `FX_HitBreakArmor` | Hit, survival, defeat, armor crack, box collapse | Reuse/pool minimal effect instances |

**Explicit deferment:** Forge/Mech/Titan art, advanced weapon/shield/rate FX and high-detail models are **not** part of this deliverable. Whether any of them enters the first playable prototype requires explicit Owner scope approval; no VS-02.

## 8. Independent visual QA / playtest readiness matrix (ALL NOT RUN)

| ID | Scenario | Visual acceptance condition |
| --- | --- | --- |
| ART5-01 | Fresh new arena: ×2 HP5, ×3 HP10, ×5 HP25 | Reward and current/max HP legible before aiming; no old fixed-gate affordance in **new** arena; old arena unchanged |
| ART5-02 | Spend 1/5, 1/10 and 1/25 successful hits | Every hit consumes one box-directed MiniBot, removes 1 HP on display; misses/reservations do **not** falsely decrement |
| ART5-03 | Last two hits resolve near-simultaneously | Exactly one box break/reward animation; no negative HP or duplicate toast |
| ART5-04 | ×2 active for 12s; ×3 acquired and queued | Active countdown continues, queued reads WAIT; at expiry ×3 starts **full 10s**; no ×6 stack |
| ART5-05 | ACTIVE ×3 and QUEUED ×5 while another multiplier box visible | Other multiplier box reads LOCKED before targeting; no misleading available/HP-hit cue |
| ART5-06 | Compare matched scripts with MiniBot maxHP=2 vs maxHP=3 | Same silhouette/layout; correct alive/nonlethal/death cues and readable HP status; no final HP preference asserted |
| ART5-07 | Multi-hit armored enemy with allies nearby | Enemy persists across nonlethal hits; armor feedback visible without making a friendly bot appear hostile |
| ART5-08 | Simultaneous grouped wave, 120 visible bots plus enemies | Correct group impression; no unit-recognition failure, occlusion, frame-rate claim or silently culled gameplay HP |
| ART5-09 | Wave warning + boss entrance + base threat + reward | Foreground priority clear, persistent wave/boost visible; no missed/blocked aiming or concealed side-box HP |
| ART5-10 | 320×568, 360×640, 390×844 notched, 412×915 notched | Colors/shape and box/buff labels readable; full safe-area and small-device checks with UI #6 |
| ART5-11 | Grayscale/color-vision simulation and reduced-motion | Box tiers, enemy armor, active/queued/locked and base danger distinguishable without color/flash/sound |
| ART5-12 | Unity reload/replay/suspend/restart | Visuals follow authoritative state and do not duplicate hit/reward/alert effects; active/queued timers sync to sim |
| ART5-13 | Final approved box HP/durations modified in config | Correct UI/text/progress, no retouching assets or one-off hardcoded thresholds |
| ART5-14 | Android/iOS profiler under dense effects and wave bursts | PERF establishes measurable limits and reports actual GPU/CPU/memory/thermal; no claim of PASS until measured |

**Matched HP2/HP3 protocol:** use identical RNG seed/command script/arena/waves and all balance settings except friendly MaxHP; capture stills/video at identical moments (before first collision, after surviving a hit, after repeat-hit defeat), plus base survival and device FPS **separately** by QA/PERF. Result must not declare a winner until Owner plays/reviews.

## 9. Risks, dependencies and open decisions

| ID | Severity | Issue / action owner |
| --- | --- | --- |
| ART5-R01 | HIGH | Nonlethal MiniBots mistaken for dead or box-consumed units → distinct survival / defeat / box-hit cues; Architect/GDD semantics required |
| ART5-R02 | HIGH | Queue-full boxes look selectable and waste fire → unambiguous LOCKED; UI/Developer authoritative eligibility |
| ART5-R03 | HIGH | Wave + boss + base-alert overlap covers attackable boxes → UI #6 safe-zone/priority mockups |
| ART5-R04 | MEDIUM | 5/10/25 HP becomes hardcoded art and breaks rebalancing → UI `current/max`, normalized fill, dynamic badges |
| ART5-R05 | MEDIUM | Grouped enemies and 120 rendered MiniBots hide friend/foe semantics → silhouette/shape and PERF stress evidence |
| ART5-R06 | MEDIUM | ×2/×3/×5 timed indicators incorrectly use shot charges or queued clock → display authoritative timers/WAIT state |
| ART5-R07 | HIGH | Existing ART-002 fixed gates in PR #13 accidentally treated as v1.2 → preserve historical baseline and approve separate migration |
| ART5-R08 | HIGH for release, outside Art | QA2-01 event ordering unresolved; ART plan cannot unblock VS-01; Architect/Developer/QA |

**Owner/Game Designer required before implementation:** revised GDD PR #19 with final first playable scope and MiniBot damage/combat cadence (but HP can remain **playtest-configurable**); box reservation/targeting/respawn; allowed enemy variants, boss visuals and authored grouped-wave bounds; alert timing. **Architect:** exact deterministic event IDs/states and 30Hz timer boundaries for box hits, survival, boosts and queued lock. **UI #6:** v1.2-specific mobile mockups, progress/buff/alert layout. **Sound #7:** synced one-shot cues and reduced effects. **PERF #8:** effect/mesh/concurrency budgets and device matrix. **QA #9:** matched HP2/HP3 and alerts/collision visual evidence. **Owner:** design sign-off, separately scoped implementation authorization and controlled merge.

## 10. Delivery verification and remaining gates

- [x] Re-read Owner #18/GITHUB-005 canonical decisions and latest Art #5 instruction.
- [x] Reviewed current GDD v1.2 Draft PR #19 as **older proposal requiring update**; did not adopt its superseded shot-quota and one-hit MiniBot assumptions.
- [x] Kept existing Art Draft PR #13 intact and new prototype art plan separate.
- [x] Documented editable HP/config response, visual states, timing/queue/lock, grouped-enemy readability and HP2/HP3 matching checks.
- [ ] Owner reviews this ART-003 proposal and confirms visual approach.
- [ ] Architect/GDD approve event and gameplay contracts.
- [ ] UI creates versioned v1.2 state/mockups, QA verifies actual outputs, PERF measures device impact.
- [ ] **No runtime code/assets/graphics implemented or claimed tested.**
- [ ] **No PR merged, QA2-01 unresolved, VS-01 unapproved, VS-02 prohibited.**

**Document outcome:** `ART_GDD_V12_PROTOTYPE_PREP_READY_FOR_REVIEW` (docs only).
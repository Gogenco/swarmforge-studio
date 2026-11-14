# SWARMFORGE — GDD v1.2 / GD-006

**Status:** DESIGN PROPOSAL — NOT APPROVED FOR IMPLEMENTATION  
**Owner directive:** [#18](https://github.com/Gogenco/swarmforge-studio/issues/18); role thread [#3](https://github.com/Gogenco/swarmforge-studio/issues/3)  
**Prepared by:** 03 — Game Designer / Level Designer  
**Date:** 2026-11-14  
**Source baseline:** `main` DEV-002.2, merge `99cfcdc0b4982e9e441cf23a8669a487a7d75ea9` / [PR #10](https://github.com/Gogenco/swarmforge-studio/pull/10)  
**Related, not superseded:** [GD-005 Draft PR #15](https://github.com/Gogenco/swarmforge-studio/pull/15); [ARCH-003 Draft PR #14](https://github.com/Gogenco/swarmforge-studio/pull/14)

> **Reading rule:** The Owner has approved the *direction*: breakable side upgrades, multi-hit enemies, staged waves, warning UI, original one-thumb SWARMFORGE gameplay. **Every numeric value, diagram coordinate, target priority and state contract below is a DESIGN HYPOTHESIS until Owner + Architect sign-off and runtime tests.** This document does not change the approved GDD v1.1/L3 balancing, Unity scenes, or existing VS-01 code. No VS-02 work is authorized.

## 1. Player fantasy and design pillars

*Fire a stream of cute robots; decide whether to protect the base, crack open a tempting upgrade, assault the fortress, or send units toward your Forge; feel an earned burst of power before tougher enemies arrive.*

1. **Readable one-thumb choice.** The cannon fires automatically; dragging changes the target sector. Every diversion into a box or Forge spends genuine opportunities to stop enemies.
2. **Acquisition is a bet.** Harder boxes need more MiniBots and delay defensive damage. ×5 gives a larger local burst, but is not automatically the best response to an imminent breach.
3. **Earn the advantage.** An upgrade affects forthcoming shots, not enemies retroactively; waves are pre-authored, with a visible warning and breathing space after an upgrade.
4. **Siege versus survival.** The enemy fortress remains the victory target, while the friendly base (3 HP in existing baseline) must survive; dangerous enemies cannot be ignored for a box.
5. **Original identity.** Friendly robotic designs, modular enemy machines, Forge/Mech/Titan concept, playful factory destruction. No copied visuals, level assets or trade dress.

## 2. Exact scope boundary and continuity

| Area | DEV-002.2 / GDD v1.1 baseline | v1.2 proposal — **not yet implemented** |
|---|---|---|
| Generator | Automatic 3 raw MiniBots/s; fixed 30 Hz simulation | Retain cadence; allow player to spend a raw shot on a destructible box; numerical rate remains hypothetical for future levels |
| Multiplication | Fixed ×2 and ×3 gates; fixed gate handling precedes contacts in tick | Finite side boxes ×2, ×3, ×5 grant an **ATTACK-only, shot-count-limited** multiplier; choose based on opportunity cost |
| Contact | Same-lane MiniBot/Drone mutual consumption | Enemy HP and armor; each MiniBot deals one hit and is consumed; enemies persist until HP zero |
| Encounter | VS-01 smoke fixture: two enemies at ~12 s | Multiple curated waves, warning at T−3 s, boss arrival; no claims that old smoke level includes these |
| HUD | Base, fortress, time; existing limited UI | Add `WAVE n/N`, small nonblocking entry banners, boss/base-threat emphasis, box HP/reward, active boost charges |
| Arena | Scalar lanes, no verified 2D shared-choke | Explicit side-box target geometry and lane intersections **requiring Architect feasibility check** |
| Forge, Mech, Titan | GDD v1.1 mechanics; not part of DEV-002.2 VS-01 smoke | **Retained, not rewritten**; raw shots spent in Forge remain separate from box/ATTACK buffs |
| First build | DEV-002.2; QA2-01 open | No runtime change. Later implementation slice must be separately approved; VS-02 forbidden |

The original GD-004 three-level geometry and GD-005 source audit remain references, **not** evidence that a 2D defense intersection is currently simulated. ARCH-003 #14's `CONTACT → GATE` tie rule is **not automatically** the box-contact rule.

## 3. Core loop, route control and one-finger input

1. At start, show fortress/base objectives and the first visible side boxes; no upgrade is mandatory for the early tutorial.
2. Player drags the cannon along the bottom aim bar. Firing automatically emits one **raw shot** every `1/3 s` (hypothesis retained from baseline). A shot's target is chosen **once at spawn**.
3. The chosen sector sends the MiniBot to an enemy/fortress attack lane, a selected box, or (when explicitly in FORGE mode) the Forge.
4. Each successful box impact consumes **one** raw MiniBot and damages the box by `1 HP`. On the last required hit the box awards **one exactly-once** boost.
5. ATTACK shots fired after activation consume boost charges; e.g. an ×3 boost creates **three MiniBots per one raw ATTACK shot**, reducing its remaining charge count by one.
6. Enemy contact costs one MiniBot per damage point, with enemy HP retained across contacts. If a wave threatens the base, aim back to ATTACK even if the tempting box is not destroyed.
7. Survive waves and destroy the enemy fortress before timer expiry. A simultaneous enemy fortress/base death in one logical tick is **LOSS**, preserving Owner precedent.

### Input and target-sector proposal

**Attack mode:** thumb drag moves cannon/aim horizontally; five *logical* target sectors left-to-right: `BOX LEFT | DEFEND LEFT | FORTRESS/CENTER | DEFEND RIGHT | BOX RIGHT`. The cannon continually displays the selected target arrow and a strong target outline. For narrow portrait phones, aim zones have a minimum `48 CSS px` interactive width (hypothesis) and snap/hysteresis to avoid unwanted box spending. Finger-lift preserves last selected sector, no hold-to-fire and no second finger required. The physical cannon movement, lane selection and 2D interception are subject to Architect validation.

**Forge mode:** a persistent accessible `ATTACK / FORGE` tap control remains from v1.1. In FORGE, auto-fire routes raw MiniBots to the selected blueprint; box sectors are unavailable with a clear `RETURN TO ATTACK TO OPEN` indicator. No x2/x3/x5 applied to FORGE contributions; assembly thresholds, reservations and combat slots remain GDD v1.1 contracts pending separate approval. In L3, selecting blueprint before any Forge resource is spent remains mandatory.

**Aiming at a saturated box:** once all missing HP are covered by *reserved in-flight hits*, show `PENDING` and temporarily pause box-directed firing, **without consuming raw shots or silently redirecting any spawned robot**. The player can drag to defense immediately while the already-dispatched shots complete their route. If a reserved robot is destroyed en route, its reservation releases and box can receive a replacement. This provides an intentional advanced tactic: attack while waiting for an upgrade to unlock. The conservative calculations below assume the player stays on the box until award.

## 4. Proposed field layout, dimensions and routing

These are **hypothetical world-space geometry values** for a 60 s test arena, **not a replacement** for approved L1–L3 authored coordinates or verified GD-004 geometry. Let X be screen horizontal and Z point upfield. Cannon neutral muzzle `(0, 0.6)`; friendly breach plane `Z=0.6`; front of enemy fortress `Z=17.6`; fortress visual center approximately `(0,18.8)`; enemy spawn plane `Z=17.6`; attack lanes near `X=-1.2, 0, +1.2`; left/right box centers `(-2.5,5.0)` and `(+2.5,5.0)` with approximate `0.55` contact radius; visual width/bounds depend on camera framing.

| Object | Initial proposal | Design purpose / concern |
|---|---|---|
| Cannon, base | `(0,0.6)`, movement/aim constrained to bottom band | Never occlude base HP, aim sector or ATTACK/FORGE |
| LEFT upgrade slot | `(-2.5,5.0)`: ×3 / HP10 | Faster tactical boost; enemy pressure opportunity cost |
| RIGHT upgrade slot | `(+2.5,5.0)`: ×5 / HP25 | Higher-risk burst; long uninterrupted diversion unsafe |
| ×2 box template | HP5, available in L1 or an alternate authored loadout | Introduce concept safely; **not a third simultaneous UI-critical slot** |
| Enemy LEFT lane | from `(-1.2,17.6)` toward `(-1.2,0.6)` | Must be interceptable by corresponding DEFEND LEFT sector |
| Enemy RIGHT lane | from `(+1.2,17.6)` toward `(+1.2,0.6)` | Likewise right; no assumed cross-lane collision |
| Enemy CENTER / boss | from `(0,17.6)` toward `(0,0.6)` | Telegraph large boss without hiding box controls |
| Fortress hit plane | front face `Z=17.6`, proposed full-width target | Surviving MiniBots reaching it damage fortress |

At neutral aim, box path length is `sqrt(2.5² + 4.4²) = 5.060 u`, travel at **hypothetical** `3.4 u/s` is `1.488 s`. Direct assault reference distance is `17.0 u`, travel `5.000 s`; extreme ±X aiming makes physical distances marginally longer and must be measured in implementation. Enemy breach from `Z=17.6` at speed `2.3 u/s` is `17/2.3 = 7.391 s` after enemy spawning. The old ~7.4 s breach risk therefore remains relevant, but no box/contact win is proven.

**Geometry must be reviewed before implementation:** side boxes must not intercept ATTACK rays unintentionally; box hit radii and enemy hit radii must be distinct; a MiniBot may collide with an enemy on its path to a box and die, releasing its reservation; narrow-screen camera must not hide rightmost `×5` badge; two side enemy lanes cannot magically merge under the old same-lane scalar contact model. Whether to use genuine 2D paths, dedicated authored lane crossing, or a separate validated approximation belongs to Architect.

## 5. Box reservation and reward-state design

### Recommended state machine (game-design semantics)

`AVAILABLE → TARGETED → PENDING(HP covered by in-flight reservations) → DESTROYED → REWARD_GRANTED(once) → EMPTY_SLOT`.

Each box has a stable `BoxId`, immutable reward definition, current `HP`, `ReservedHits`, `InFlightShotIds`, `RewardGranted` boolean / authoritative event ID, and presentation state. Each shot knows its target at spawn. A shot can only hit one box **once**, and a box receives a reward only upon the one terminal `HP > 0 → HP = 0` transition. The visual destruction effect is *not* allowed to issue the boost. All targets and results are recorded in deterministic replay.

- **Target eligibility:** may reserve one box-hit capacity only if `HP - ReservedHits > 0`; reservation is entered before an outgoing unit is added to the world. No oversubscription. If a shot dies, its reservation disappears; no HP reduction or reward.
- **On impact:** commit `HP -= 1` and release this unit's reservation atomically. If HP becomes zero, emit `BOX_BROKEN` and grant exactly one reward; any already-spawned MiniBots keep their original target and attributes. If a box is unavailable, no new shots are routed into it.
- **Expiry/end:** when battle ends, freeze all pending shots, box events, buffs and reservations on the terminal tick. No late-arriving box reward after victory, defeat or timeout; no carryover to the next match without a separately approved meta-system.
- **Respawn:** no automatic box respawn. An authored wave may place a new box in an EMPTY slot, with a new ID and explicit telegraph; do not replace a still-available box behind the player's back.
- **Simultaneous terminal impacts:** deterministic ID ordering; only one `BOX_BROKEN`. The order against enemy contact, buff activation and shot-spawn at equal logical time requires Owner/Architect sign-off (§12), **not** reuse of fixed-gate tie by assumption.
- **Display:** visible `HP/current HP`, clear reward symbol, impact feedback at ~75/50/25% health, `PENDING` feedback, then a nonblocking `×N READY` highlight. No random reward; the player chooses knowingly.

### Recommendation: finite, nonstacking ATTACK boosts

| Box / reward | Box HP | Raw shots committed to break | Earned ATTACK payload | Duration/charges | Raw net additional units after box cost¹ |
|---|---:|---:|---|---|---:|
| ×2 | 5 | 5 | ×2 | next 18 ATTACK shots, max 12 s | `18·(2−1)−5 = +13` |
| ×3 | 10 | 10 | ×3 | next 15 ATTACK shots, max 10 s | `15·(3−1)−10 = +20` |
| ×5 | 25 | 25 | ×5 | next 12 ATTACK shots, max 8 s | `12·(5−1)−25 = +23` |
| Shield | 7 | 7 | absorb **2** friendly-base breaches | 12 s after activation | Depends on actual threats |
| Pulse weapon (later) | 12 | 12 | special attack on 8 ATTACK shots | max 10 s | Must measure damage/collision/cost |
| Rapid Fire (later) | 6 | 6 | +1 raw ATTACK shot/s | 6 s, cap 4/s | Must verify total shots and device load |

¹ **Ideal arithmetic only**: assumes all bonus units attack useful targets before timeout; does *not* include travel, armor, mis-aim, wasted shots, enemy interception, or ongoing enemy risk. **All HP, counts and times are hypotheses.** The exact penalty is 5/10/25 *raw opportunities*, not a guaranteed 5/10/25 actual lost fortress damage. A shield charge is consumed only when an enemy would breach; enemy also despawns; shield charge is not counted as damaging the fortress.

**Activation:** only after the final committed hit is chronologically resolved. Grants affect **future** raw ATTACK shots spawned at or after the activation boundary per Architect-approved same-time ordering. Shots already flying, including box-bound shots and Forge contributions, never retroactively multiply. A boost charge is spent **once per raw ATTACK shot** (not per spawned child); clones retain the parent target and receive unique stable IDs. Shooting at a box/Forge does not consume a multiplier charge, but the active boost's clock keeps advancing. No boost-to-box damage amplification, and no recursive multiplicative stack.

**Queue for fairness:** only one active boost and one ready-to-activate pending boost. A second completed reward goes to a 1-item visible queue and starts when the active one ends; while both are occupied, remaining reward boxes are target-locked *before* player can spend units on them. Expiration clock of a queued reward begins on activation, not award. Future weapon/shield interactions with this queue need Owner approval: recommended first implementation only ×2/×3/×5 with a single type of multiplier queue, not all special rewards.

**Important alternative for Owner:** use a separate nonconsuming cannon projectile for boxes. It reduces the cost of upgrading and makes aiming visually easier, but weakens the core tradeoff and adds second projectile contact semantics, combat/VFX/QA scope. **Recommended for this design: one consumed MiniBot per box HP**, subject to Owner approval.

## 6. Time-to-acquire mathematics and ×5 risk

Assume cannon starts directly on one box at `t=0`, 3 raw shots/s, no box-path losses, initial muzzle at X=0, box travel `1.488 s`. Shots fire at `t = 0, 1/3, 2/3, …`. The `N`th dispatched shot leaves at `(N−1)/3`; final impact is `(N−1)/3 + 1.488`.

| Upgrade | Last required shot dispatched | Last hit / reward becomes active | First possible boosted ATTACK shot² | Time to first boosted enemy contact³ | W1 earliest base breach |
|---|---:|---:|---:|---:|---:|
| ×2, HP5 | 1.333 s | **2.821 s** | 3.000 s | **7.19 s** | 13.391 s |
| ×3, HP10 | 3.000 s | **4.488 s** | 4.667 s | **8.19 s** | 13.391 s |
| ×5, HP25 | 8.000 s | **9.488 s** | 9.667 s | **11.17 s** | 13.391 s |

² Next 1/3 s cadence, **conservative player** holds box sector until reward, even though they could steer to ATTACK earlier while last reserved box-shots travel. ³ For first Scout at `t=6`, front at `17 u`, speed `2.3 u/s`, aligned attacker speed `3.4 u/s`: `t_contact = (17 + 3.4·t_attack_shot + 2.3·6) / (3.4 + 2.3)`, valid if meeting occurs after both spawns and paths actually intersect. Rounded to two decimals. Single shot contacting a Scout does **not** prove whole-wave interception.

Early ×5 thus leaves only **~2.22 s** from first *boosted* contact to a Scout's earliest breach; ×3 leaves **~5.20 s**. ×5 nets only **three** more theoretical units overall than ×3 while delaying its boosted defense roughly **2.98 s**. That makes ×5 a situational late-threat/boss burst, not a free universal pick. **Counterexample requiring QA:** if six fast enemies split evenly and cannon spends `t0–9.5` on ×5, incoming enemies could escape even though ×5 is stronger later. **Positive scenario requiring QA:** when first lane is safe / shielded, ×5 may supply enough burst to break boss armor and fortress faster. Neither outcome has been simulated.

If player retargets ATTACK after all hits are **reserved** (before actual box break), their first ordinary ATTACK shot could leave at `1.667/3.333/8.333 s` respectively; those shots are **unboosted**, but can intercept the early wave. This separate higher-skill path must be in test scripts. If the box-targeted MiniBots are killed, the reservation/arrival times rise and all numbers change.

## 7. Enemy archetypes, armor and deterministic damage

Proposal: a MiniBot is `1 HP / 1 contact damage`. On contact, one MiniBot is consumed, and an enemy loses **exactly 1 HP** after any explicit armor reduction; no one-touch death for armored foes. Damage is integer, not frame based. Enemy base breaches normally cost **1 friendly-base HP** regardless of enemy remaining HP; boss extra breach damage is not part of this proposal. No enemy healing or invisible DPS. Enemies damage MiniBots via contact, while turret/ranged attacks are **not** proposed in this first direction.

| Enemy | HP | Speed, u/s | Pressure identity | Visual reading | Hypothesis |
|---|---:|---:|---|---|---|
| Scout | 1 | 2.3 | Tutorial pacing / early threat | Small orange drone, no armor | 1 hit |
| Runner | 2 | 2.7 | Shorter defense window | Elongated agile drone, two damage stages | 2 hits |
| Armored Drone | 3 | 1.8 | Forces small-army resource commitment | Bright plated shoulder / HP pips | 3 hits |
| Bulwark (later) | 5 | 1.4 | Optional stronger mini-boss | Wide shield-like body, readable 5 pips | Deferred for first 60 s example |
| Boss: Siege Behemoth | 18 | 1.1 | Spectacle / end-of-round pressure | Large original Titan-scale hostile machine | 18 hits; 1 base HP if breached |

For this proposal, `armor` is *visual and HP-based* (`HP > 1`), not a separate percentage reduction/penetration system. This is intentionally testable; introducing armor resistances or abilities requires a new Owner choice. Each contact's exact time and survivor HP enter replay state; dying enemies despawn on HP→0, do not breach. Three enemy breaches, if no shield and base starts at3 HP, cause loss. Same-tick friendly fortress and friendly-base death results in loss.

## 8. Wave schedule and complete 60-second illustrative encounter

**Purpose:** demonstrate pacing and user decisions, **not final L3 balancing**. This is a separate tuning sandbox with hypothetical fortress **100 HP** and baseline friendly base **3 HP**. Existing GDD v1.1 L3 fortress **270 HP**, enemy count and other values are **not modified** by this document; porting boxes into L3 requires a new Owner-approved balance pass.

| Wave | Warning T−3s | Spawn T | Composition / sides | Raw enemy HP budget | Earliest breach if unopposed | Callout |
|---|---:|---:|---|---:|---:|---|
| 1 | 3 | 6 | 2 Scouts LEFT + 2 Scouts RIGHT | 4 | `6+17/2.3 = 13.39s` | `WAVE 1/5 — SCOUTS` |
| 2 | 13 | 16 | 2 Scouts LEFT + 2 Scouts RIGHT + Runner each side | 8 | Scouts `23.39s`; Runners `22.30s` | `WAVE 2/5 — RUNNERS` |
| 3 | 23 | 26 | 1 Runner LEFT + 1 Runner RIGHT; 2 Armored LEFT + 1 Armored RIGHT; 1 Scout RIGHT | 14 | Runner `32.30s` | `WAVE 3/5 — ARMORED` |
| 4 | 32 | 35 | 1 Runner LEFT + 1 Runner RIGHT; 2 Armored each side | 16 | Runner `41.30s` | `WAVE 4/5 — PRESSURE` |
| 5 — BOSS | 40 | 43 | Boss CENTER + 1 Armored each side + 1 Runner each side | 28 | Runner `49.30s`, Boss `58.45s` | `BOSS INCOMING` then `WAVE 5/5 — SIEGE BEHEMOTH` |
| **Total** | — | — | **27 enemies** across five waves | **70 HP** | — | — |

Enemy count check: W1=4; W2=6; W3=6; W4=6; W5=5; total 27. Enemy HP check: `4 + (4·1+2·2)=8 + (2·2+3·3+1·1)=14 + (2·2+4·3)=16 + (18+2·3+2·2)=28`, total 70. All incoming waves are **fixed by schedule independent of whether the player just unlocked ×5**. Upgrade does not instantly scale already spawned enemies; authored wave escalation occurs at next spawn event, with 3 s advance warning.

### Event-by-event player-facing example (one of several possible attempts)

| Time | Event / player opportunity | Result / constraint — not a promised victory |
|---|---|---|
| 0–3 s | Fire 10 shots at LEFT ×3; its last box hit is due at `4.49s` | 10 raw opportunities committed to the box. Strong early ×5 alternative consumes 25 instead |
| 3 s | Nonblocking `WAVE 1/5 IN 3…` appears | Continue aiming; controls remain live; can attack with unboosted shots while box hits are flying |
| 4.49–9.49 s | ×3 active; direct up to 15 ATTACK raw shots into 45 MiniBots | Bonus only on shots **spawned after activation**; aim LEFT/RIGHT to stop W1 |
| 6–7.25 s | Four Scouts spawn at 6; wave callout runs for 1.25 s | Title banner is transparent to hits/drag; W1 unopposed breach at13.39 |
| 9.5–13 s | Continue ATTACK, or pay for an optional HP7 shield only if box slot is free and authored | Don't spend box time if W2 forecast is too threatening |
| 13 s | W2 warning. `WAVE 2/5 IN 3…` | Clear previous wave before Runner window closes |
| 16–17.25 s | W2 appears, including two 2-HP Runners | A single MiniBot hit will not kill a Runner; player sees HP reduction |
| 18–23 s | Tactical defense / fortress shots / optional queued box decision | If a ×5 slot is still unopened, the 8.3 s raw cost has to fit a safe window |
| 23 s | W3 warning | New armored threat is telegraphed, not tied to previous ×3 pickup |
| 26–27.25 s | Armored wave begins; new side reward may appear only in an empty slot | 3 hits per armored enemy; damage state visible |
| 29–34 s | ATTACK lanes, possibly Forge if a separately approved full level supports it | All shots assigned a route at spawn; no midflight redirects |
| 32 s / 35 s | W4 warning / spawn | Callout never pauses attack; short Runner breach window begins |
| 40 s | Distinct `BOSS INCOMING · 3…` warning, low-key optional haptic | Priority over generic W5 banner, but below immediate base threat |
| 43–44.25 s | Boss entrance, W5/5 progress, two side escorts | Boss has 18 HP, 1 damage per MiniBot; earliest breach58.45 if untouched |
| 44–58 s | Focus escorts and boss or briefly claim ×5 for a fortress burst | Diversion can be fatal; no guaranteed outcome without simulation |
| 58–60 s | Last interception / assault, then terminal resolution | At exactly 60 s, unresolved fortress = loss; same-tick fortress/base destruction = loss |

**Crucial ammo-budget sanity check.** At 3 raw shots/s, at most ~166 shots leave by `t=55` and can cross a `5s` fortress route before `t=60` (exact endpoint/inclusive tick defined by Architect). If a player takes only ×3, optimistic effective attack capacity is `166 - 10 box shots + 15·(3−1) = 186` MiniBots; after all 70 enemy HP are paid, at most **116** remain for the hypothetical 100-HP fortress, a narrow ideal headroom **16** before aim/path losses. Only ×5: `166−25+12·(5−1)=189`, minus70 = **119**, headroom19 but a much worse *early* breach risk. Only ×2: `166−5+18=179`, minus70=**109**, headroom9. These **upper bounds** ignore timing of actual awards, missed hits, potential simultaneous waves, traveling enemies and unused boost charges. They do not prove any route can win. Applying this unchanged to old L3 fortress 270 would fail the optimistic `>270` check; **therefore 100HP in this sandbox is illustrative and Owner must approve a full L3 retune before replacement**.

**Three-lane pressure note:** an actual 3-lane/2-side-lane simulator must ensure MiniBots are aimed to physically intersect the correct threats. Counting raw production alone never proves that 4 of 6 enemies are intercepted. The earlier GD-004 first-wave-4-of-6 criterion must be re-tested using actual wave definition, target geometry and collisions.

## 9. Alert, UI and accessibility contract

| State/event | Trigger & lifetime (hypotheses) | Text and priority | Interaction rule |
|---|---|---|---|
| Persistent HUD | Entire match | `BASE 3`, `FORT 100`, `TIME 60`, `WAVE 0/5`, boost icon/charges | Safe-area anchored; no interception of gameplay drag |
| Pre-wave | Precisely 3 s before authored spawn | `WAVE n/N IN 3 · 2 · 1` | Small top/middle overlay; 1 countdown event/sec, not 30Hz spam |
| Wave start | At spawn, 1.25 s | `WAVE n/N — [TYPE]` | Drawn over game but pointer-transparent and nonmodal |
| Boss warning | 3 s before boss | `BOSS INCOMING` + silhouette/optional cue | Supersedes generic pre-wave banner; WAVE progress always visible |
| Boss arrival | At boss spawn, 1.25 s | `SIEGE BEHEMOTH` | Subordinate to live base threat, never stop firing |
| Base threat | Enemy predicted breach in ≤2.0 s; or an enemy inside base danger band | **`BASE UNDER ATTACK!`** (highest) | Pulse, not fullscreen cover; clear only after **≥2.5 s** predicted safe / no danger band, to prevent flicker |
| Box award | Once on box break | `×3 READY` / shield badge, nonmodal ≤0.9 s | Active boost charges remain readable; don't occlude world boxes |
| Terminal | Server/logic match outcome | VICTORY / DEFEAT | Only terminal state may stop live interaction |

**Priority order:** terminal > BASE THREAT > BOSS > WAVE > UPGRADE; persistent BASE/FORT/TIME/WAVE/boost counters remain visible regardless of transient callouts. If events overlap, lower-priority banner can be skipped (not delayed misleadingly), while its state/event is still recorded. No repeated `BASE UNDER ATTACK` each tick; emit enter/exit transitions and optional cue on first enter with a cooldown. Wave UI is a **read-only consumer** of deterministic gameplay events, not a scheduler of spawns.

**Mobile/accessibility acceptance:** playable at 320×568 portrait and notched/tall displays; minimum touch targets roughly 44–48 px physical/logical depending on platform; warning stays out of thumb path and side-box HP labels; high contrast, noncolor HP + shield indication, bright flashes controlled, reduced-motion option, audio/haptics individually optional, no mandatory sound cues. Animated banners do not pause simulation or create invisible touch overlays. Art/UI must agree on final badge palette (existing Art #13 vs UI #17 tentative mismatch) before asset implementation.

## 10. Multi-hit contacts, waves and engine handoff (DESIGN → ARCH)

| Contract | Existing code reference / known behavior | Proposed event/state API questions for Architect |
|---|---|---|
| Spawn/aim | 30Hz auto-spawn and lane-based route assignment | `ShotSpawned(id, tick, targetKind, targetId, baseRate)`; target chosen once and never retarget midflight |
| Gate | Fixed `ApplyGates()` before `ResolveContacts()` | `BoxHit` versus `EnemyHit` must be chronological by actual contact TOI; does **not** inherit gate precedence |
| Box | No HP/reservations/reward state in VS01 | `BoxState{Id,HP,ReservedIds,reward,phase}`; `BoxReserved/ReservationReleased/BoxDamaged/BoxBroken/RewardGranted` exactly once |
| Buff | Fixed multiplier clone in gate | `BoostState{type,charges,expiryTick,sourceBoxId}`, pending1; issue children with unique ordered IDs; no same-tick retroactive buff |
| Combat | One MiniBot vs one Drone mutual kill, lane equality | `EnemyState{Id,Hp,MaxHp,Type,Route,Speed}`, contact reduces1 HP; separate `EnemyHit` and `EnemyDefeated` |
| Waves | Simple enemy spawn schedule | `WaveSchedule{waveId,startTick,warningTick,composition,boss}`, `WaveWarning/WaveStarted/BossArrived` no time pause |
| Base threat | Breach-based friendly-base HP | Predict ETA from authoritative enemy position/speed; threat enter/clear hysteresis; shield breach absorb atomic |
| Determinism | ARCH-003 #14 addresses gate-vs-contact ties; unmerged | Same-tick order for box/Enemy/Spawn/Buff/Wave/Base/Terminal; stable shot/clone IDs, deterministic replay/event/state hashes; formal ARCH-004 delta needed |
| Geometry | Old scalar same-lane lanes | Explicit 2D segment collider/hit radii or Architect-approved simplification; avoid invisible cross-lane kills |
| Simulation/visual | Fixed 30Hz model and visual pool cap | Logical MiniBot counts independent of rendered cap; batch/spatial collision tests, cull visuals only, no loss of logical damage |

**Recommendation for equal exact contact times (not approved):** if a single MiniBot collides with both an enemy and box at same TOI, resolve *enemy contact first*, consuming it, with no box damage; for equal events of same kind, deterministic ascending UnitId then TargetId. Chronological event time dominates kind priority. If terminal destruction and box completion occur at same logical instant, terminal outcome resolves before any post-terminal reward; Owner must confirm exact event phase and hashing with Architect. No gameplay code should be produced from these recommendations until sign-off.

**Architect performance bound:** at 3 raw/s, a permanently active unlimited ×5 would create **900 MiniBots per 60 s**, while ×3 would create 540, prior to deaths—unacceptable to assume a render cap of 120 equals a logical cap. With the **proposed finite ×5 12-shot reward and no stacking**, a single 60 s ×5 box creates at most `180−25+48=203` total logical MiniBots over the full match, not 900; ×2+×3+×5 boxes once each create at most `180−40+18+30+48=236`, assuming all boosts fully used, no wasted idle time. **This is a total-spawn budget, not a proven maximum simultaneous-live unit count.** Actual concurrent count, fixed 30Hz CPU cost, physics broadphase, GC, 30fps floor on representative Android/iOS, overdraw, haptic/FX rate and thermal behavior need device profiling. No arbitrary simulation cap may silently delete units or change HP damage.

## 11. Explicit battle and balance test matrix (all UNRUN)

| Test ID | Setup | Expected acceptance / reason |
|---|---|---|
| GD12-B01 | W1 4 Scouts, straight ATTACK, no upgrades | Able to save base with correct cross-lane aiming; no surprise collisions; win not inferred |
| GD12-B02 | Claim ×3 from t0, then counter W1 | First award around4.49 s only in neutral ideal path; ≥reasonable safety window; record actual 2-lane intercept count |
| GD12-B03 | Claim ×5 from t0 without switching away | Expose honest defensive risk; compare breaches vs ×3; **×5 may lose** |
| GD12-B04 | Dispatch needed ×5 shots, immediately steer to defense while reservations travel | Unboosted defense shots should hit safely; no reroute of old box shots; late reward affects only new shots |
| GD12-B05 | Destroy Box with 2+ shots at same logical TOI | HP never <0; exactly one grant; stable replay hash / unique IDs |
| GD12-B06 | Target box then enemy kills a reserved MiniBot | Reservation releases, box stays unbroken; can replace required hit; no free reward |
| GD12-B07 | Enemy and box TOI exact tie on same MiniBot | Outcome follows **Owner-approved** deterministic priority; no ghost hit or duplicate reward |
| GD12-B08 | Boost is active; destroy second box; third box visible | Pending queue holds one, third target locked; no wasted box costs, no stacks, expiry predictable |
| GD12-B09 | ×5 buff12 charges, expire timer, ATTACK/FORGE switches | Exactly N valid ATTACK raw shots boosted, charges count raw shots, clock advances in Forge; no ×5 Forge generation |
| GD12-B10 | Runner HP2 and Armored HP3 take staggered contacts | Survive 1/2 hits, die at2/3, consume proper robots; deterministic enemy HP |
| GD12-B11 | Unlock reward just before W3; enemy HP next wave higher | Genuine advantage during existing wave; no instantaneous adaptive power cancellation |
| GD12-B12 | Warnings at3/13/23/32/40, spawn6/16/26/35/43 | Exact 3s advance warning; gameplay/input uninterrupted; only one banner priority at a time |
| GD12-B13 | Boss, base-threat and wave overlap | Base threat highest; boss/WAVE progress retained; no input blockers, spam or false clear flicker |
| GD12-B14 | Fortress and base hit0 on same tick or timeout at60s | LOSS wins ties; late box/buff events ignored; Restart clean state |
| GD12-B15 | Baseline 60s attack cost, ×2 vs ×3 vs ×5 | Compare actual win rate and TTK, not net-unit budget alone; avoid universal x5 optimality |
| GD12-B16 | Low-end 320×568 device, simultaneous effect stress | Stable control/safe areas, screen readability, 30Hz fixed sim and device FPS target measured, not asserted |
| GD12-B17 | Same scripted inputs at differing render frame rates | Identical deterministic state, ID assignment and event hash for 30/60/variable FPS |
| GD12-B18 | L3-original 270HP/75 enemies imported unchanged | **Expected balance risk**: do not claim achievable; requires signed off retune and full simulation |

**Suggested initial playtest success gates** (design targets, not measured): >90% of players recognize the side box's stated reward before shooting; >80% accurately understand `2/3` armor after first hit; ≥70% complete a guided practice without prompts after tutorial; each of ×3 and ×5 wins *some*, but neither dominates >80% of matched scenarios; no HUD overlays prevent aim or box use; at least two viable choices in authored late-game encounter; base defeat feels telegraphed in interviews. Use 20–30 first-time players across varied mobile devices for early directional evidence; no universal statistical claim from small sample.

## 12. Owner approval matrix — explicit unanswered decisions

| ID | Owner decision needed | Recommended **hypothesis** | Tradeoff / falsification criterion |
|---|---|---|---|
| O12-01 | Box takes MiniBot vs new projectile? | Consumes one raw MiniBot per HP | Meaningful offense-cost; reject if users find shot/box rules unintelligible |
| O12-02 | What counts as armored/multi-hit damage? | One MiniBot =1 hit consumed; enemy HP integer, no extra armor reduction | Readable pips; re-evaluate if too much contact churn/performance cost |
| O12-03 | Are boosts per shot or timed? | Next 18/15/12 ATTACK shots, with 12/10/8s expiry | Predictable rewards; timed safety cap prevents holding charges indefinitely |
| O12-04 | Multipliers stack or boost boxes? | No stacking, max active1+queued1; boxes always take raw unboosted shots | Limits clone explosion; one pending slot adds a small visible UI state |
| O12-05 | Keep fixed ×2/×3 gates anywhere? | Replace in **future authored v1.2 arenas**; keep DEV-002.2 tests/gates until compatibility plan approved | Avoid unnecessary loss of QA2-01 regression; mixed design may confuse players |
| O12-06 | Box HP/reward numbers? | HP5/10/25 for ×2/×3/×5; charges as §5 | ×5 net advantage marginal vs ×3, but late burst higher; adjust after replay |
| O12-07 | Exact box coordinates/size/visible count? | Two simultaneous side slots at (±2.5,5), radius0.55; ×2 alternate | Keeps portrait legible; verify 320×568 and 2D collision |
| O12-08 | Wave timings, count and enemy stats? | §8 sandbox: 5 waves in60s, 27 enemies70HP; §7 types | **Not a retrofit** of old L3; requires independent balanced pass and Owner approval |
| O12-09 | Boss breach damage/abilities? | Boss18HP at1.1u/s, standard 1 base HP on breach, no special ability initially | Avoid instant cheap defeat at base3HP; boss must still be threatening |
| O12-10 | How much progression in MVP? | L1 ×2 intro, L2 ×2/×3 comparison, L3 ×3/×5 and armored/boss; no meta economy | Three brief teachings, but must not silently change already approved three-level balance |
| O12-11 | What implementation starts first and when? | **Design sign-off → Architect contract → isolated DEV/QA slice**; don't start before explicit Owner directive | Avoid replacing QA2-01 work before chronology/migration decision |
| O12-12 | Event ties & cloning ID order? | Exact TOI; enemy-first when same unit simultaneously contacts box/enemy; ID ascending | Architect must formalize with same-tick buff grant/terminal precedence and replay tests |
| O12-13 | Optional buffs first slice? | Start with multiplier boxes only; shield/weapon/fire-rate remain documented candidates | Reduces testing load, but complete design supports later expansion |
| O12-14 | Alert priority/timing & base threat hysteresis? | Warning3s, banner1.25s, base threat ETA≤2s, clear ETA≥2.5s | Nonblocking but meaningful; test visual crowding and cue cooldown |
| O12-15 | Minimum device targets and performance budget? | 30Hz deterministic; device-render target at least30fps on chosen entry class; no logical bot deletion | Performance Engineer defines test devices and confirms, not assumed by design |
| O12-16 | Old L3 270HP versus v1.2 sandbox100HP? | Preserve 270HP in existing files; assess rebalancing **only after** Owner authorizes level migration | Current upper-bound contradicts casual 270HP victory claims; avoid secret balance edit |

## 13. Role dependencies, risk registry and staged next steps

| Owner role | Required handoff / dependency | Work **not** authorized |
|---|---|---|
| 01 Product Owner | Sign off O12-01…16, especially VS-01 compatibility scope and fixed gates | No silent design-to-code approval |
| 02 Architect [#2](https://github.com/Gogenco/swarmforge-studio/issues/2) | Formal box-vs-enemy chronology; post-award exact tick semantics; stable IDs; 2D hit geometry; state hashes; `ARCH-003 #14` migration | Do not treat old CONTACT→GATE rule as box rule |
| 04 Developer [#4](https://github.com/Gogenco/swarmforge-studio/issues/4) | Data/config/API only **after** explicit scoped Owner authorization; preserve current DEV-002.2 and QA2-01 repair baseline | No Unity code change now; no VS-02 |
| 09 QA [#9](https://github.com/Gogenco/swarmforge-studio/issues/9) | Execute B01–B18 under approved build; report counterexamples and full replay/device evidence | No claim that math-only scenarios passed |
| 06 UI [#6](https://github.com/Gogenco/swarmforge-studio/issues/6) | Target sector readability, World-Space box HP and `WAVE n/N`, banner priority, safe areas | No approved HUD implementation yet |
| 05 Art [#5](https://github.com/Gogenco/swarmforge-studio/issues/5) | Box identity/damage stages, armored visual states, boss silhouette; coordinate Art #13/UI #17 palette | No full assets yet |
| 07 Sound [#7](https://github.com/Gogenco/swarmforge-studio/issues/7) | Distinct BoxHit/BoxBroken, nonlethal EnemyHit, WaveWarning/Start, Boss/Threat Enter/Exit, optional haptic | No new audio implementation yet |
| 08 Performance [#8](https://github.com/Gogenco/swarmforge-studio/issues/8) | Benchmark finite ×5, 2D box/Enemy collision, mass clone logic, FX alerts and mobile budget | No assumed device PASS |

**Risks**

1. **R12-01 / CRITICAL — Old QA2-01 unresolved** and ARCH-003 #14 unapproved; a new contact system could entrench replay divergence. **Mitigation:** Owner chooses legacy repair vs versioned migration; Architect approves chronology before any Developer change.
2. **R12-02 / HIGH — Delayed ×5 causes cheap early loss.** Mitigation: guaranteed readable warning, optionally stronger passive W1 buffers, no late hidden threat scaling; test ×3 vs ×5 defense on two lanes.
3. **R12-03 / HIGH — 60s sample ammo is tight.** Mitigation: separate 100HP sandbox; inspect useful damage/time windows; do not transplant old 270HP values without verified retune.
4. **R12-04 / HIGH — Portrait boxes/aim sectors conflict.** Mitigation: UI safe-area tests at320×568 and thumb tests; allow sector hysteresis, no modal announcements.
5. **R12-05 / HIGH — Ambiguous 2D collisions.** Mitigation: authored collider/radius geometry, exact TOI and deterministic tie, no extrapolation from old scalar lane model.
6. **R12-06 / HIGH — Performance spikes from many clones.** Mitigation: finite charges/no stack, event batching, quality-tier presentation; logical unit lifetime profiled separately from render cap.
7. **R12-07 / MEDIUM — Box resource grief and reward duplication.** Mitigation: reservations, once-only BoxId event, no invisible reroute, explicit pending UI and locked queues.
8. **R12-08 / MEDIUM — UI alert fatigue / flashing.** Mitigation: event-based warnings, priority, short duration, reduced motion and haptic opt-out.
9. **R12-09 / MEDIUM — Armor reads as unfair invulnerability.** Mitigation: large HP pips, distinct nonlethal impact, damage effects per contact.
10. **R12-10 / MEDIUM — New boxes erase Forge identity.** Mitigation: ATTACK-only boosts, preserve Mech/Titan blueprint and reservations; postpone Forge rebalancing to signed-off full GDD migration.

**Recommended Owner-gated sequence:**

1. Owner reviews this GDD v1.2 proposal and records decisions O12-01…16; retains GD-005 PR #15 and ARCH-003 PR #14 untouched until individual approvals.
2. Architect issues a versioned simulation/event/geometry contract for the approved box/damage/wave rules and the QA2-01 migration. This is design-only until code directive.
3. Owner separately authorizes a bounded first implementation and corresponding QA plan; Developer works isolated branch/PR, never VS-02 by implication.
4. Independent QA performs real Unity deterministic and mobile replay/balance tests; only then tune L1–L3 and claim a build is playable.

**Verification performed for this document:** reviewed Issue #18, role Issue #3 latest Owner message, cross-role #18 handoffs, merged DEV-002.2 baseline disclosures, and arithmetic consistency of the proposed 60 s wave/box model. **Not performed:** Unity compilation, any EditMode/PlayMode or mobile execution, actual collision simulation, measured frame rate, player test, or level pass/fail certification.

**Document deliverable status:** `GDD_V12_PROPOSAL_READY_FOR_OWNER_REVIEW` — review of *proposal* only; implementation, balance, VS-01 sign-off and all Owner open choices remain pending.

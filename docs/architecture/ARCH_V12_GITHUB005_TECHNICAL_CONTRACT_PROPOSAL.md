# SWARMFORGE — ARCH-v1.2 / GITHUB-005 Technical Contract Proposal

**Owner review status:** `ARCH_V12_EXACT_HEAD_ALIGNED` for **provisional P2 C1–C6 only**; **P2 execution remains gated by actual P1 Unity config/replay test PASS**.  
**Role:** 02 — Senior Game Architect / Tech Lead  
**Date:** 2026-11-14  
**Owner source:** [#18](https://github.com/Gogenco/swarmforge-studio/issues/18#issuecomment-6880330660), [architect task #2](https://github.com/Gogenco/swarmforge-studio/issues/2#issuecomment-6880337664)  
**Latest exact design source verified:** [GDD v1.2 Draft PR #19](https://github.com/Gogenco/swarmforge-studio/pull/19) at [`b70da4e478b7a4b6dc54a403b130a20facf4e62f`](https://github.com/Gogenco/swarmforge-studio/commit/b70da4e478b7a4b6dc54a403b130a20facf4e62f) (Director GITHUB-006.1 C1–C6 signed by Designer).  
**GITHUB-006 source:** [Owner conditional P1/P2 approval](https://github.com/Gogenco/swarmforge-studio/issues/18#issuecomment-6880563570), [architect exact-head task](https://github.com/Gogenco/swarmforge-studio/issues/2#issuecomment-6880587557).  
**Binding GITHUB-006.1 decision:** [Director C1–C6](https://github.com/Gogenco/swarmforge-studio/issues/18#issuecomment-6880765413), [Designer exact-HEAD countersign](https://github.com/Gogenco/swarmforge-studio/issues/2#issuecomment-6880812611).  
**Legacy architecture, preserved:** [ARCH-003 Draft PR #14](https://github.com/Gogenco/swarmforge-studio/pull/14)  
**Baseline:** merged DEV-002.2 `main` commit `99cfcdc0b4982e9e441cf23a8669a487a7d75ea9`.

> This is a **versioned, docs-only trial contract**. Director GITHUB-006.1 C1–C6 **supersedes earlier draft disagreement and is approved for bounded P2 only**; remaining sections marked **ARCHITECT PROPOSAL** concern unapproved geometry/balance or future scope. Architect has not performed Unity runtime tests, changed C# or assets, merged PRs, or started VS-02.

## 1. Decision register: approved versus open

| Rule | Status | Contract impact |
| --- | --- | --- |
| Side-field destructible boxes replace always-on ×1/×2/×3 gates in **new v1.2 arenas** | **OWNER APPROVED** | New ruleset; do not delete or rewrite old gate scenes/tests yet. |
| A successful damaging box hit consumes exactly **one MiniBot**; no new projectile mode | **OWNER APPROVED** | Box hit is a terminal event for that MiniBot; on-flight death cannot deal box damage. |
| Initial multiplier box HP: ×2 **5**, ×3 **10**, ×5 **25** | **OWNER APPROVED initial tune** | Integer HP, editable in versioned balance data; no claims of balanced acquisition cost. |
| Multiplier time only: ×2 **12 s**, ×3 **10 s**, ×5 **6 s**, no shot charges | **OWNER APPROVED initial tune** | Durations at 30 Hz: **360/300/180 ticks**; expiry based on simulation clock, not render time. |
| Multipliers do not stack; **active1 + pending1**, FIFO, full pending time begins on activation | **OWNER APPROVED** | Exactly two entitlement slots; third multiplier box locked before further MiniBots are spent. |
| Box HP/progress preserved while locked; never silently lose earned reward | **OWNER APPROVED** | Prevent in-flight third-reward oversubscription with deterministic terminal-hit reservation. |
| Friendly MiniBots have >1 HP and may survive enemy fights, then fight again | **OWNER APPROVED principle** | Integer HP, persistent units, multi-hit combat and explicit cooldown; no automatic consumption on enemy contact. |
| Exact friendly HP **2 vs 3**, friendly/enemy damage, contact rate, enemy HP, level/wave counts | **DEFERRED / TEST PRESETS** | Editable balance data; implement neither as hard-coded canonical balance. |
| Enemy waves spawn simultaneous groups; tougher/more varied later; no immediate adaptive counter to boost | **OWNER APPROVED direction** | Group-spawn event and data-authored schedule; actual level content pending Designer/Owner. |
| Wave countdown, WAVE n/N, boss warning and base threat are nonblocking | **OWNER APPROVED direction** | Presentation subscribes to events; it never controls authoritative simulation. |
| Shield, weapon, rapid-fire interactions with multiplier queue | **OPEN** | Model as optional future reward types; no implicit shared queue or new scope. |
| Multi-ally combat, rational exact-time cooldown, enemy-before-box survivor tie, terminal reward promises, boost tick timing | **DIRECTOR GITHUB-006.1 C1–C5 — PROVISIONAL P2 APPROVED** | Designer #19 exact HEAD `b70da4e...` countersigned Architect #23 previous HEAD `575f7ecd...`; single algorithm in §12. |
| Exact 2D hitbox dimensions, route feasibility, broader game balance | **OPEN / TRIAL CONFIG** | AABB proposal requires authored geometry verification; never treat sample values as approved final balance. |

**Latest Designer revision verified:** PR #19 HEAD `b70da4e...` supersedes original `5bee02c...`, intermediate `2089e646...`, and the contradictory `63b7b6a...` pairing/cooldown proposal. The GDD five-wave sandbox (34 enemies/93 hypothetical enemy HP) remains **unapproved** level content. P2 is **one** temporary 60-second arena, **one** ×2 box and **one** simultaneously spawning enemy group under Director GITHUB-006.1; never treat the full GDD sample as P2 authorization.

## 2. Scope and data boundaries (proposed)

- Keep Unity 6.3 LTS, pure C# authoritative 30 Hz simulation, integer fixed-point coordinates, offline play and local save. Renderer, animation, effects, HUD, SFX and device FPS **cannot author damage or time**.
- Define **two explicit rulesets**: `LegacyVs01V1` reads existing `LevelDefinition`/gates; `BoxCombatV12` reads versioned `GameplayRulesV12` and `ArenaV12`. A level selects a ruleset once at match creation. Do **not** add hidden `if (new design)` branches that silently change old saves/replays.
- Recommended future Unity assets: an Inspector-editable `BalancePresetAsset` and `ArenaLayoutAsset` backing pure serializable DTOs. Baseline already exposes `LevelConfigAsset.CreateDefinition()` and `LevelDefinition.ConfigVersion=1`; extend by adapter/new DTO in a separately authorized implementation, not today.
- `MatchConfigSnapshot` is a deep immutable copy at match start: `RulesetVersion`, `BalanceVersion`, `ArenaId`, `PresetId`, `ConfigHash`, `TickRate=30`, fixed positions/hitboxes, durations/cooldowns, enemy archetypes, wave schedules and all relevant HP/damage. No live Inspector mutation during a match or replay.
- **Tune without code changes**: MiniBot max HP and damage; enemy HP, damage, speed and cooldown per archetype; box HP; durations; raw-shot period; spawn group count/lane/start tick; boss HP; fortress/base HP; collision distances; view pool budget (non-authoritative). Validate all values and references before play; reject negative HP/damage, ambiguous IDs, nonintegral durations, unsupported versions, impossible geometry and out-of-range tick arithmetic.
- Preserve existing save and replay namespaces. Never auto-upgrade legacy match data, do not overwrite older presets, and do not claim GDD v1.1 L1–L3 balance is valid under BoxCombatV12.

### Minimal pure-state records — proposed, NOT C# implementation

| Record | Authoritative fields |
| --- | --- |
| `MiniBotState` | `Id`, `BornTick`, `Hp`, `MaxHp`, `Damage`, `RouteId/TargetId`, integer `X/Z`, speed, `Alive`, optional engaged EnemyId, exact `NextEligibleStrikeTime`, buff snapshot at birth. |
| `EnemyState` | `Id`, `WaveId`, `ArchetypeId`, `Hp`, `MaxHp`, `Damage`, route/position, `Alive`, ordered participant IDs, exact `NextEligibleStrikeTime`. |
| `BoxState` | `BoxId`, reward ID, `Hp`, `MaxHp`, targetable/locked, in-flight reserved bot IDs, `TerminalAwardSlotHeld`, `RewardGranted`, `EntityVersion`. |
| `BoostQueue` | optional `Active{kind,multiplier,startTick,endExclusiveTick,sourceBoxId}`, optional `Pending{kind,durationTicks,sourceBoxId}`, count of promised terminal box awards. |
| `EngagementState` | one stable EnemyId with **multiple** participant MiniBotIds permitted, each MiniBot bound to at most one enemy; contact/attack times, sorted target choice and next strikes; no render collider reference. |
| `WaveDefinition` | stable `WaveId`, integer `StartTick`, warning lead ticks, ordered group elements `(ArchetypeId,LaneId,Count,FormationSlots)`, boss marker. |
| `EventRecordV12` | `RulesetVersion`, match ID, tick, exact sub-tick time numerator/denominator, kind, actor/target IDs, deterministic event ordinal, value(s). |

`MiniBot HP=2` and `HP=3` are comparable **playtest presets**, not selected shipping values. All other parameters in those two presets must be byte-for-byte identical so differences can be attributed to friendly durability.

## 3. Deterministic 30 Hz event clock

**ARCHITECT PROPOSAL.** Let tick `k` cover `[k/30,(k+1)/30)` and event time `(k, τ)`, `0≤τ<1`, with exact rational `τ=num/den` (`den>0`). A boundary event at `τ=1` belongs to the next tick `k+1,τ=0` and must not be emitted twice. Positions, speeds, HP and attack damage are integers; compare TOI by exact cross-products with overflow-checked wide integers. A 2D circle/sqrt collision approximation using FPS-dependent floats is **not** admissible for authoritative decisions.

### Proposed per-tick sequence

1. If already terminal, return frozen immutable snapshot/history. Consume valid user commands in canonical `(Tick,Sequence)` order and update `CommandHash`; route/aim is snapped at raw-shot creation, never rerouted midflight.
2. At the **start of tick**, expire an active boost when `k == EndExclusiveTick`; activate FIFO pending at this same boundary, setting `StartTick=k`, `EndExclusiveTick=k+DurationTicks`. If an award was earned during previous tick and no active boost exists, its scheduled activation also occurs now. Recalculate visible box lock/eligibility before any new raw shot.
3. Process scheduled raw-shot opportunity in legacy order **before** grouped enemy spawns to retain a predictable ID hierarchy; a locked/PENDING box suppresses this opportunity with a deterministic `ShotSuppressed` record, **not** a hidden diversion to ATTACK and **not** a consumed MiniBot. ATTACK raw shot born now is multiplied by the active boost (if any); a BOX-targeted or FORGE-bound raw shot is **unmultiplied**. Units born in tick `k` do not move until tick `k+1` (legacy invariant).
4. Emit nonblocking `WaveWarning` at authored tick; spawn each group with a **single** authoritative `WaveStarted` and a deterministic, sorted batch of `EnemySpawned` (all born at one tick). Sort by `(WaveId, GroupId, LaneId, FormationSlot)`; never rely on input array enumeration. Warnings only announce; they do not delay or spawn enemies themselves.
5. Snapshot existing movable entities at tick start, generate TOI candidates for eligible 2D contacts (enemy↔bot, box←box-targeted bot, fortress←ATTACK bot, base breach←enemy) and all **due** engagement strikes. Candidate generation **does not** allocate IDs, HP, rewards or event history. Process by **exact time buckets** `(tick, rational τ)`. Within each bucket, assign eligible combat targets by deterministic distance²/IDs, snapshot alive/HP/cooldowns, resolve simultaneous eligible combat damage **as a batch**, then process still-eligible box impacts, goals and breaches. Revalidate alive, route and `EntityVersion` on commit; recompute affected candidates after locks/deaths.
6. Publish the committed batch in a canonical order, never from candidate discovery order. A combatant may make **at most one combat strike per attack-cadence instant**. If a box-targeted bot also reaches the box at **the exact same time as enemy contact**, it may make **one** box hit **after** combat only when it remains alive (explicit reconciliation to Designer #19 §10). That successful box hit consumes it; there is no retroactive later impact.
7. Resolve fortress/base impact and breach on the authoritative path crossing, then process terminal result once after all in-tick physical effects. Existing priority survives: **base HP≤0 → LOSS**, else fortress HP≤0 → WIN, else time expiration → LOSS. Clamp HP to zero, publish at most one MatchEnded, then freeze future rewards/spawns.
8. Normalize survivors and all hashed state by stable IDs, verify conservation, construct immutable snapshot/event batch and advance tick. Display/UI subscribers do not feed back damage, locks or waves.

**Same-time tie — DIRECTOR-APPROVED P2 C4:** `COMBAT_CONTACT/ENGAGED_STRIKE [batched] → BOX_HIT [alive survivors only] → FORTRESS_IMPACT → BASE_BREACH`, then stable `(TargetId,ActorId)`; terminal outcome follows all physical events. Thus a BOX-bound MiniBot at the exact enemy-and-box TOI first exchanges combat HP, then—**only if HP>0, box reservation valid and BoxHP>0/unlocked**—deals **one** box damage and is consumed. If combat killed it, no box hit/reward. The box sacrifice is **not a second combat strike**. This is bounded P2 behavior, not permission to damage arbitrary overlapping objects. Time order outranks type. Legacy CONTACT-before-GATE does not transfer by implication.

## 4. Repeated enemy contacts with surviving MiniBots

**OWNER APPROVED principle:** friendly MiniBots persist across some enemy contacts. **RECONCILED ARCHITECT TRIAL PROPOSAL against GDD #19 §7** (not final HP/balance):

1. **Many allies, one enemy:** each MiniBot may target **only one** EnemyId while engaged; an enemy may simultaneously have **multiple** MiniBots fighting it. Acquire by swept real 2D contact within configured engagement boundary; among simultaneous eligible targets choose nearest fixed-point squared distance, then ascending EnemyId/MiniBotId. Existing engagement does not block another allied MiniBot from joining.
2. **Initial batch:** at a common rational time `(k,τ)`, freeze eligible combat participants **after target assignment**. Each eligible MiniBot strikes its chosen enemy **once**; each eligible enemy strikes **one** chosen MiniBot (nearest distance² then lowest ID), *not every participant*. Compute all eligible outgoing integer damage from the **pre-exchange** HP/alive snapshot. Aggregate damage per target, apply simultaneously and clamp HP to zero. An actor killed in that batch still delivers its already-eligible strike, never a future strike.
3. **Cooldown:** every actor (MiniBot and enemy) has one global positive configurable `AttackCooldownTicks` (provisional P2/P3 **15 ticks=0.5s**) and exact `NextEligibleStrikeTime`. First contact can attack immediately if eligible; after a strike at absolute rational time `k+τ`, that actor is next eligible **at or after `k+τ+CooldownTicks`**, not `k+1+CooldownTicks`. A newly joining bot can strike if eligible, but **does not reset** the enemy's own cooldown or trigger extra enemy retaliation. Zero cooldown or a hit every rendered frame is prohibited.
4. **Position and exit:** while engaged, participants pause forward route movement. On enemy death, surviving MiniBots (including those with HP1) resume their **original immutable target/route on the next tick**; on Bot death, remove it once and release any box reservation. If all bots die while enemy survives, the enemy resumes its base route next tick. If both die, neither resumes. A bot whose enemy dies at contact time **cannot instantly retarget a different enemy in the same tick**; if its box impact is at that identical time, apply the one-time same-TOI box rule in §3.
5. **Stable batch semantics:** all same-time new contacts are considered before combat damage; sort all candidates by exact rational time, then squared distance and stable IDs. One MiniBot cannot newly join two enemy engagements at the same instant. One enemy may accept several allies. Group combat is not resolved by greedily killing one pair before another eligible attacker can contribute. No separate armor percentage/reduction is assumed; HP is per archetype and editable.

**Worked proposed P2 oracle:** Enemy HP2/damage1 and two simultaneous allied MiniBots HP2/damage1 join at the same TOI. Both bots deal1 to Enemy (total2); Enemy strikes **one** bot chosen by stable distance/ID for1. Enemy dies, chosen bot remains HP1, other remains HP2; both may resume next tick. A simultaneous dying actor still applies its committed attack. No actor strikes twice in one bucket or before its next eligibility time.

**Designer + Director sign-off received on multi-attacker P2 batch:** [Designer exact-HEAD sign-off](https://github.com/Gogenco/swarmforge-studio/pull/19#issuecomment-6880812932) and [Director C1–C3](https://github.com/Gogenco/swarmforge-studio/issues/18#issuecomment-6880765413). This does **not** finalize HP2/HP3, the proposed 15-tick initial tuning, 0.6u radius, AABB dimensions or arena balance; they remain adjustable trials. A moving/overlap alternative requires a new signed trial contract.

**Conservation changes:** baseline `ProducedTotal = ActiveMiniBots + FortressHits + EnemyLosses` must become `Produced = Active + BotDeathsInCombat + BoxConsumed + FortressConsumed + OtherExplicitTerminalConsumes` (each unit exactly once); enemy counts similarly `EnemiesSpawned = AliveEnemies + DefeatedEnemies + BreachedEnemies`. Repeat damage does not add units or deaths. Never issue `MiniBotLost` per hit if the robot actually survived; snapshots/hashes must include current HP and engagement/cooldown state.

## 5. Box geometry, routing and collision feasibility

**ARCHITECT PROPOSAL:** v1.2 uses authored fixed-point X/Z 2D paths. Every raw shot captures immutable `TargetKind=ATTACK_ENEMY/ATTACK_FORTRESS/BOX/FORGE` (latter only in a separately approved slice), `TargetId` and waypoints. Side boxes are collidable only by their **box-targeted** shots; an ATTACK shot must not accidentally spend itself on a nearby box. **Enemy contact is incidental and must be checked for all moving MiniBots** on actual crossing paths.

For deterministic narrow-phase, approximate target bodies with **configured axis-aligned rectangles expanded by the moving bot's configured rectangular half-size** (Minkowski sum); for two moving bodies use relative velocity and a swept 2D slab intersection over `[0,1)`. Solve linear inequalities per axis with exact rational entry/exit fractions. `TOI=max(xEntry,zEntry,activeFrom)` if it does not exceed `min(xExit,zExit,1)`; reject an already passed pair. No Unity PhysX callback, no variable-FPS `Rigidbody` contact or unsorted `OnTriggerEnter` may author damage. **Rectangular logical hitboxes are a proposed technical simplification**, not approval to change the silhouettes or gameplay reach without designer verification.

Evaluate per-route cross-lane intersections; prevent invisible contact between nonintersecting paths. GDD PR #19 suggests boxes near `(±2.5,5.0)`, enemy lanes at `±1.2`, center fortress and two side defense sectors; those numbers are **unapproved sample geometry**, not L1–L3 replacements. Before coding require explicit waypoint sets, logical rectangle sizes and an interception test for every wave lane/box route on a 320×568 portrait camera.

## 6. Multiplier box reservations, queue safety and rewards

### Contract invariants

- For each live box, `0≤Hp≤MaxHp`, `ReservedHits≥0`, `ReservedHits≤Hp`, and each in-flight BoxShot has at most one reservation. `Hp−ReservedHits` is missing unreserved damage; if zero, the box is `PENDING` and must suppress further box-directed shots.
- `0≤ActiveCount≤1`, `0≤PendingCount≤1`; multipliers **never stack**. To prevent overflow from in-flight terminal hits, introduce `TerminalAwardPromises`: one promise exists when a box has enough **living in-flight reserved hits to reach HP=0** (but has not yet broken). **Entitlement invariant:** `ActiveCount + PendingCount + TerminalAwardPromises ≤ 2`. Award on destruction atomically converts that promise to an active-slot or pending-slot entitlement; no third reward can appear without capacity.
- Before dispatching a shot that would complete `ReservedHits==Hp`, atomically check entitlement availability and reserve the terminal promise if there is room. **Whenever entitlement capacity is already full, every *other* multiplier box is `LOCKED` and cannot receive *any* new box-directed MiniBot, including non-finishing hits**; an auto-fire opportunity aimed at it is suppressed (no robot spawned, no silent reroute). Existing box HP, previous successful hits and legal already-in-flight reservations remain unchanged. Partially damaged boxes remain visible but disabled until space opens. This enforces the Owner rule that no additional MiniBot can be spent on a locked box, not merely its final hit.
- Each reservation is keyed by `(MatchId,BoxId,BotId)`; commit or cancel exactly once. On successful hit: revalidate live shot and box, decrement `Hp` exactly **1**, consume that MiniBot, release its reserved hit, emit `BoxDamaged`; when HP becomes 0, emit one `BoxBroken` and `RewardGranted` with unique `AwardId=(MatchId,BoxId)` and permanently mark `RewardGranted=true`. The visual explosion grants nothing.
- If an in-flight shot dies to an enemy, cancel its reservation; if it was the terminal-hit promise, atomically release the promised slot and restore targetability. No HP decrement or reward. For other live reservations that still cover HP, recompute terminal promise eligibility deterministically (normally one of the outstanding shots must become the terminal promise).
- Box state exists independently of `LOCKED`, `PENDING` and visual mode. Locking/unlocking never resets accumulated damage. No shot automatically retargets if eligibility changes after its spawn. If active/pending queue is full while a prior promise exists, the entitlement invariant must still hold: never drop third award, never let an already-in-flight final hit force an overflow.

### Potential edge case and required atomicity

Suppose multiplier A is active, a final Box B shot is in flight and has reserved the pending entitlement, while Box C is 1 HP from breaking. C's finishing shot **must not be dispatched**, although B has not yet rewarded. If B's final shot dies, C may unlock on the next authoritative eligibility update. If B rewards, it occupies Pending and C remains locked until a slot opens. With simultaneous final impacts, process ordered `(exact TOI,BoxId,BotId)`; the pre-reserved claims guarantee capacity regardless of list/renderer order.

### Activation and expiry (tick-exact proposal)

1. Award at intra-tick `(k,τ)` never retroactively changes **any** MiniBot spawned earlier in tick `k`. It obtains an available active entitlement, but activation is scheduled at `k+1` tick start; otherwise store FIFO Pending.
2. On activation at tick `a`, record `[StartTick=a, EndExclusiveTick=a+DurationTicks)`. For ×2, ×3 and ×5, respective durations are **360, 300 and 180 simulation ticks**. At `k==EndExclusiveTick`, expire **before** that tick's raw shot; queued reward activates **on the same boundary** with its entire time quota, then shot spawn uses the newly active multiplier. No idle gap and no overlap.
3. ATTACK-only raw shot at tick `k` spawns one original plus `(multiplier−1)` additional MiniBots; all snapshot the same route/HP/buff provenance. BOX and FORGE routes remain **unboosted** and never consume a shot quota (none exists); active timer still runs when aiming elsewhere. An earlier airborne MiniBot never gets retroactive copies.
4. If a reward is granted on the exact tick in which an active boost expires, expiry/queued promotion occurred at tick start; the new award joins the available slot or pending FIFO and begins no earlier than `k+1`. If multiple box rewards arrive in one tick, exact TOI then BoxId establishes queue order.
5. At terminal state, cancel inactive/pending future activations and all reservations; events committed before terminal remain evidence but **never** apply after terminal, and Restart creates fresh IDs, box/boost state and hashes.

**GITHUB-006.1 resolution:** Director approved activation no earlier than tick `k+1`, exact half-open expiry, pre-dispatch terminal entitlement and no-spend `ShotSuppressed` for locked multiplier targets (C5); Designer confirms. Visual feedback remains UI-owned. No alternative box-side escrow may silently replace the signed two-slot promise model.

## 7. Group waves, event IDs, display contract

- Wave spawns are immutable records keyed by `WaveId`, with integer `StartTick`, warning lead, one or more grouped `(ArchetypeId,LaneId,Count,FormationSlot)` members. At StartTick, **all** group members have the same `BornTick` and do not move until next tick. Allocate IDs ascending `WaveId → GroupId → LaneId → FormationSlot`. A group with 4 or 6 enemies is one simultaneous spawn, not four separate periodic spawns.
- `WaveWarning` occurs `StartTick−WarningLeadTicks` (validate nonnegative), e.g. 3 s→90 ticks when approved; `WaveStarted` is authoritative and lists WaveId/count; `EnemySpawned` emitted per spawned actor. Boss wave may include escorts. UI banner/sound/haptic never pauses sim.
- Terminal > base threat > boss > wave > reward callout presentation priority is a **UI recommendation**; constant base/fortress/time/boost indicators remain visible. BaseThreat enter/exit is authored from deterministic enemy ETA and hysteresis, never visual distance. No alert repeats at 30 Hz every tick.
- Do **not** implement real-time adaptive enemies that scale immediately because the player acquired ×5. Waves are authored from config, with any eventual dynamic director a separately approved feature.

## 8. Stable IDs, event order, hash and replay migration

**ID issuance must be tied to committed events, not candidate discovery or collection order:**

1. On each tick, spawn one `RawShot` original if due and allowed; allocate its original ID first. Immediately after that, for a boosted **ATTACK** raw shot allocate children in clone index `1..multiplier−1` ascending. All are born the same tick, first move next tick. (No gate multiplication in v1.2.)
2. Spawn simultaneous groups sorted `(WaveId,GroupId,LaneId,FormationSlot)` next, each enemy `IssueId()` monotonic. Stable BoxIds come from authored `ArenaV12` and are **not** allocated from mutable pooled visual objects.
3. Engagement/impact/reward events use their stable source and target IDs, ordered by exact TOI and agreed tie key. No allocation on stale candidates; `RewardGranted` uses unique `(MatchId,BoxId)` and thus cannot double grant even on event replay.
4. Hash canonical ruleset/config snapshot, ordered commands, per-tick authoritative events (`tick, rationalτ, kind, source, target, ordinal, value`), sorted living entity HP/engagement/cooldown/positions, box HP/reservation identities, active/pending/promise/timer state, and terminal outcome. **Hash new fields**; a no-op visual action must not change hash. Avoid dictionary enumeration, renderer FPS, Unity object instance IDs and floating positions.
5. Keep existing `CommandHash`, legacy EventHash/StateHash and event history unchanged under `LegacyVs01V1`. BoxCombatV12 must use a **new schema discriminator and versioned event/hash definition**. Hash equality is required **within the same version/config/commands** at 30/60 FPS, **not** between legacy and v1.2 rule systems. Save/replay includes ruleset and balance config hash, and refuses incompatible playback with a clear validation error.
6. At Restart, reset all match-scoped IDs, wave counters, boxes, reservations, boost timers, HP, pending attacks, event batches, hashes and commands; ignore pending callbacks from previous visuals or jobs.

### Collision/reward exact-tie examples (required proposed oracle)

| Same-tick scenario | Director-approved provisional P2 result, except unapproved future-wave/geometry cases |
| --- | --- |
| Enemy touches a BOX-bound MiniBot at `τ=1/4`; box hit would occur at `τ=3/4` | Enemy combat first; if MiniBot dies, release reservation; **no box damage/reward**. |
| Box hit at `τ=1/4`; enemy contact would occur at `τ=3/4` | MiniBot consumed by box; enemy candidate stale, no later hit from dead MiniBot. |
| Enemy and box exact same `τ`, same MiniBot | Batch CombatContact first; surviving BOX-bound MiniBot with valid reservation then damages box once and is consumed. Dead bot cannot damage box. |
| Two finishing shots for same 1-HP box at exact same `τ` | Cannot legally both have terminal reservations; one commit, no negative HP/duplicate grant. |
| Active boost expires at tick `k`, pending exists, shot due tick `k` | Pending becomes active **before** shot spawn; old multiplier not applied. |
| Active1, Pending0, Box A final hit reserved; Box B final hit attempted | B finishing shot blocked/box locked; active+pending+promise never exceeds 2. |
| One enemy attacked by two MiniBots at same `τ` | Both eligible bots participate in one simultaneous HP-damage bucket; enemy strikes at most one chosen bot; result independent of list order. |
| Fortress and base both reach zero in same tick | Existing `LoseBase` priority; exactly one terminal event. |

## 9. QA acceptance matrix — SPECIFIED, NOT RUN

| ID | Required scenario / assertion |
| --- | --- |
| A01 | Legacy DEV-002.2 match and old gate scenes/configs still load; no runtime change from docs-only PR. |
| A02 | QA2-01 legacy collision-before-gate repro retained; after separately approved fix dead MiniBot never clones. |
| A03 | Box hit before enemy contact: consumes once, box HP−1, enemy not hit by dead bot. |
| A04 | Enemy contact before box: on MiniBot death cancel reservation, box HP stable, no reward. |
| A05 | Exact enemy/box TOI tie follows approved priority; collection permutation does not alter outcome. |
| A06 | Two boxes destroyed same tick with capacity2: chronological FIFO, exact one award per BoxId. |
| A07 | Active1+Pending1 prevents third terminal box reservation before spending MiniBots; prior HP unchanged. |
| A08 | Active1+TerminalPromise1 locks another finishing shot; after promised bot death slot releases and box unlocks. |
| A09 | Box PENDING with enough in-flight hits suppresses redundant raw shot; no zombie reservation/negative HP. |
| A10 | Timer ×2/×3/×5 exact exclusive boundaries 360/300/180 ticks; queued full-duration activation, no stacking. |
| A11 | Last pre-activation ATTACK shot unboosted; first at activationTick boosted; in-flight bots unchanged. |
| A12 | ATTACK multiplied, BOX/Forge unmultiplied; no shot-charge logic; clock advances during other routes. |
| A13 | MiniBot HP2 preset survives at least one representative contact if damage allows, fights again; no duplicate death. |
| A14 | Matched HP2 and HP3 presets use identical seed, waves, enemy values and commands; record base HP, breaches, TTK. |
| A15 | Enemy HP2 targeted by two MiniBots HP2 at exact same TOI: bot damage2 total, enemy damage1 to one stable chosen bot, both bots survive with HP1/HP2; permutation invariant. |
| A16 | At initial contact rational `k+τ`, first hit eligible; next hit exactly `k+τ+15` ticks (preset), never earlier. A third bot joining mid-cooldown does not grant enemy extra attack. |
| A17 | Waves grouped at same tick, sorted IDs independent of wave record/collection permutation; warning precedes spawn. |
| A18 | Boss with escort spawn same tick; warning/UI does not pause sim or block one-thumb aiming. |
| A19 | Non-collinear box/enemy 2D swept rect collision; no cross-lane ghost hits or high-speed tunneling. |
| A20 | 30/60/variable rendering of identical command log: per-tick EventHash, StateHash, events, ID sequence, outcome equal **within v1.2 version**. |
| A21 | Replay from different config hash is rejected; legacy history remains readable under legacy ruleset. |
| A22 | Base+fortress simultaneous zero, timeout, mid-boost terminal, 20× restart: one terminal event, no post-match rewards/IDs leak. |
| A23 | Invariants per tick: resource conservation, box reservation reconciliation, reward at most once, entitlement count≤2, HP bounds. |
| A24 | Mobile low-end grouped-wave + ×5 worst case: **measure** CPU/GC, logical vs visible peak, RAM, thermal, FPS and battery; no invented PASS. |
| A25 | Same MiniBot exactly touches enemy and reserved box: first batched exchange, then **only if alive** BoxHP−1 and MiniBot consumed; if killed, box HP unchanged. Both branches with same input/order replay hashes. |
| A26 | One Enemy with existing cooldown, a new allied MiniBot joins before enemy cooldown ends: new bot may strike once, **enemy may not attack again early**, no infinite zero-tick strike loop. |

Performance sensitivity: at nominal 3 raw shots/s, each fully utilized ×2 12s adds up to `36` extra bots, ×3 10s up to `60`, ×5 6s up to `72`. These are **conditional emitted-unit deltas**, not live-peak measurements. Robots can now survive and remain active longer, so actual concurrent logical counts and contact pairs may increase even if 6-second ×5 is shorter. Maintain logical units regardless of rendering proxy cap (baseline `MaxVisibleMiniBots=120` is visual only); use deterministic broad-phase or fixed lane/spatial grid if profiling demonstrates need, never silently delete game units.

## 10. Migration, delivery, dependencies and blockers

**Owner GITHUB-006 granted a specific conditional code boundary for Game Developer #4** (not for Architect; no code in this PR):

1. **Stage P1 NOW (Developer only):** isolated Draft PR from exact merged main, versioned editable validated balance schema, immutable config snapshots + ConfigHash, equivalent P2-HP2 and P3-HP3 presets, opt-in v1.2 ruleset/replay seam, focused tests. Legacy DEV-002.2 behavior retained byte-for-byte. **No combat, boxes, waves or new playable gameplay** in P1.
2. **P2 only after hard gates:** Architect must publish exact-HEAD alignment, Designer must explicitly countersign **identical** provisional combat/box/chronology trial rules, and P1 validation/replay tests must pass. P2 is a **separate** Draft PR, implementing only **one** temporary 60-second arena: auto one-thumb MiniBots, HP2/HP3 combat survivors, **one** ×2 HP5 side box (1 MiniBot per successful hit), timed ×2 12 s, **one** simultaneously spawned enemy group, base/fortress outcomes and Restart. Test in real Unity where possible. Owner's P2 gate is automatic only when all conditions are demonstrably met.
3. **Not P2:** full ×3/×5 boost lineup, boss, five-wave GDD sample, Mech/Titan/Forge gameplay, art-heavy UI/audio, original L3 migration, VS-02, any main merge or release sign-off. All require later Owner approval. Ruleset may hold future-safe data structures without exposing unapproved runtime features.
4. **Legacy QA2-01:** preserve existing regression fixture and ARCH-003 Draft #14. Do not claim it is fixed by this proposal or by introducing BoxCombatV12; migration/legacy repair is a separately authorized work item.
5. **Tests after P2 code exists:** comparable HP2/HP3 scripted runs with same wave inputs, full 26-case architecture QA matrix scaled to actual slice, Unity EditMode/PlayMode and independently verified replay/damage outcomes, plus scoped device profiling. Design mathematics are not runtime tests.

**Dependencies:** [Designer #3](https://github.com/Gogenco/swarmforge-studio/issues/3)/[Draft #19](https://github.com/Gogenco/swarmforge-studio/pull/19) must update combat and all math; [Developer #4](https://github.com/Gogenco/swarmforge-studio/issues/4) prepares no-code prototype plan; [QA #9](https://github.com/Gogenco/swarmforge-studio/issues/9) owns matched test scripts; [Performance #8](https://github.com/Gogenco/swarmforge-studio/issues/8) owns live robot budget; [UI #6](https://github.com/Gogenco/swarmforge-studio/issues/6) owns locked box/active+pending/wave affordances; [Art #5](https://github.com/Gogenco/swarmforge-studio/issues/5) owns multi-HP hit feedback and group readability; [Sound #7](https://github.com/Gogenco/swarmforge-studio/issues/7) owns non-authoritative cues; [Owner #1](https://github.com/Gogenco/swarmforge-studio/issues/1) owns ruleset and implementation/merge approvals.

### Historical GITHUB-006 sign-off gate register (C1–C5 now resolved by §12)

1. **RESOLVED for P2 by Director C1–C3:** multi-ally engagement, simultaneous batch, exact rational `T+C` repeat and next-tick resume; configure test damage/cooldown. Friendly HP remains a playtest variable.
2. Approve or revise **swept fixed-point AABB logical hitboxes** and actual side-box route/interception geometry; prohibit fake cross-lane collision.
3. **RESOLVED for P2 by Director C4–C5:** same-TOI enemy-first and surviving bot's valid one box impact, terminal award promises/locks/`ShotSuppressed`, tick `k+1` reward activation and half-open expiry. These are **provisional approved P2 mechanics**, not final balance.
4. **CURRENT exact-head GDD #19 `b70da4e478b7a4b6dc54a403b130a20facf4e62f` ↔ ARCH #23:** see actual two-way confirmation §12. Five-wave sample (4/6/7/8/9, 34 enemies, 93 hypothetical enemy HP) remains *design-only*; P2 permits **one** grouped wave and **one** ×2 box.
5. Explicitly define first runtime slice and ARCH-003/QA2-01 migration; no Unity code, Draft merge or VS-02 before separate authorization.

**Final architect status is updated by §12:** `ARCH_V12_EXACT_HEAD_ALIGNED` for approved provisional C1–C6; P2 still awaits executed P1 tests. No Architect runtime tests, compilation, mobile profiling, code, balance mutations or merges have occurred.

## 11. HISTORICAL GITHUB-006 exact-HEAD comparison (superseded by §12)

**Source evidence:** [GDD #19 `2089e646223c9f7ca2de001e00a09337251ccac6`](https://github.com/Gogenco/swarmforge-studio/commit/2089e646223c9f7ca2de001e00a09337251ccac6) versus original [ARCH #23 `33d45659ec072523b6a709677fe827c9f9ab606e`](https://github.com/Gogenco/swarmforge-studio/commit/33d45659ec072523b6a709677fe827c9f9ab606e). Both remain Draft against `99cfcdc0b4982e9e441cf23a8669a487a7d75ea9`; nothing is merged.

| Design rule (GDD #19 at historical HEAD `2089e646...`) | Prior ARCH-v1.2 mismatch or agreement | GITHUB-006 revision; current resolution in §12 |
| --- | --- | --- |
| HP2 vs HP3 are **both** trials; damage1, enemy damage1, interval15 ticks only hypotheses | Already matched | Keep editable immutable presets; test both under same seed/commands. |
| Multi-ally swarm attacks a shared enemy, all simultaneous damage from pre-batch HP | **CONFLICT:** old exclusive pair prevented multiple allies focusing the same enemy | **RESOLVED / SIGNED:** many-to-one enemy engagement; one enemy retaliation per eligible cadence, one bot strike each, same-time HP deltas together; Director+Designer C1–C2. |
| Equal-TOI enemy+box: combat first, **surviving** MiniBot damages box, then is consumed | **CONFLICT:** old ARCH forbade even survivor's box hit | **RESOLVED / SIGNED:** alive reserved survivor makes one valid box hit after combat within same time bucket; Director+Designer C4. |
| Positive configurable contact interval; nearest then stable ID target priority | Old `k+1+CooldownTicks` delayed repeats beyond GDD's 15-tick period | **REVISED:** next strike at **absolute rational** last strike time + configured interval (initial 15 ticks); no extra enemy strike triggered by a joining ally. |
| Boxes HP5/10/25, successful consumed bot, reward exactly once | Compatible | Reserve in-flight box hits; hold entitlement on final reservation; award once on terminal HP change. |
| Active1+Pending1 nonstacking, locked extras, no earned reward dropped | Prior GDD proposed post-hoc box-side escrow for earlier in-flight third award | **RESOLVED / SIGNED:** reserve entitlement **before** final shot and lock all other multiplier boxes when full; no side escrow or third reward, Director+Designer C5. |
| Timed ×2/×3/×5 12/10/6s; GDD sample times from sub-tick physical award | Provisional ARCH starts at tick **k+1** boundary; sample assumes activation at exact hit | Keep deterministic tick-boundary activation. Sample start/end estimates can shift by **<1/30 s**; GDD must label them ideal estimates and use actual tick ledger in P2. |
| Side slots and 2D engagement radius; sample world coords are hypotheses | Rectangular fixed-point swept collider proposed by ARCH | Use explicit logical rectangle and tested route geometry for **P2 one-box** arena; neither box radius0.55 nor engagement radius0.6 is a finally approved physical collider. |
| GDD 60s five-wave example 34 enemies/93HP; old L3 270HP unchanged | Old ARCH roadmap could be read as full-wave near-term implementation | Owner **P2** contains **one grouped wave**, not five, and one ×2 box. Five-wave GDD remains later design/QA scenario; old L3 untouched. |
| Stable unit IDs, immutable replay/config versions, no legacy gate deletion | Compatible with required versioning | Explicit P1/P2 schema guard and replays, original QA2-01 independent blocker. |

**Historical P2 status (GITHUB-006 only; SUPERSEDED by §12):** Designer confirmation was previously pending at HEAD `2089e646...`; Director GITHUB-006.1 resolved the disagreement and Designer later signed new GDD HEAD `b70da4e...`. **Do not reuse `BLOCKED_BY_DESIGNER_COMBAT_SIGNOFF` as the current blocker.** The remaining hard gate is executed P1 Unity config/replay validation evidence; no code or merge by Architect.

## 12. DIRECTOR GITHUB-006.1 — exact-head bilateral combat contract and deterministic oracle

**Authoritative references:** Director [#18 C1–C6](https://github.com/Gogenco/swarmforge-studio/issues/18#issuecomment-6880765413); Game Designer [GDD Draft #19 exact HEAD `b70da4e478b7a4b6dc54a403b130a20facf4e62f`](https://github.com/Gogenco/swarmforge-studio/commit/b70da4e478b7a4b6dc54a403b130a20facf4e62f), GDD §7/§10/§14.2, [explicit Designer countersign of ARCH HEAD `575f7ecd8fa0cecee7b8e3764890b343421ff57a`](https://github.com/Gogenco/swarmforge-studio/issues/2#issuecomment-6880812611). Verified against the actual remote Markdown at these exact commits. **C1–C6 agree; older exclusive 1:1/no-survivor-box/`k+1+C` trial oracles are formally superseded.** This section is the binding interpretation if earlier historical §1/§11 wording is read differently.

### 12.1 One exact-time event algorithm — no list-order dependency

1. **Canonical time:** one authoritative update tick `k` covers `[k,k+1)` in 30-Hz tick units. Each within-tick instant is normalized exact rational `T=k+n/d`, with `0≤n<d`, `d>0`; compare by overflow-checked wide-integer cross-multiplication. A time at `(k,1)` is `(k+1,0)` exactly once. Rendering does not enter the time calculations. The configured positive actor cooldown `C≥1` retains the fractional part: `nextEligibleStrikeTime=T+C` after **each actual combat strike**, never `floor(T)+1+C` or `ceil(T+C)`.
2. **Boundaries and candidate generation:** at `T=(k,0)`, expire/promote boosts and settle already-due activations **before** authorized raw-shot creation; emit same-tick grouped spawns sorted `(WaveId,GroupId,LaneId,FormationSlot)`. New units receive monotonic stable IDs in committed spawn order (raw original, boosted clones by index, then enemies) and cannot move until `k+1`. From authoritative fixed-point 2D trajectories, derive swept AABB first-contact TOIs; include due rational cooldown strikes for existing overlapping engagements plus box, fortress and base crossings. Candidate discovery is side-effect-free; never issue IDs/rewards/hashes while enumerating candidates.
3. **Time bucket:** pop the smallest exact `T`, collect **all** candidate contacts/eligible strikes at that same rational instant, sort by `(squaredDistance,EnemyId,MiniBotId)` for combat target acquisition, preserving each MiniBot's at-most-one selected Enemy. Existing engagement can accept newcomers. Before damage, freeze alive/HP and eligibility **per attacking actor**. Each eligible bot strikes its one target **once**; each eligible enemy retaliates against **one nearest live engaged bot** (distance² then smallest BotId) **once**, regardless of whether that bot's own attack cooldown has expired or how many other bots arrived. A new bot joining does not reset enemy cooldown. Emit each unique strike record sorted `(EnemyId,MiniBotId,StrikeSide)`; aggregate target integer damage from the **pre-batch** snapshot and commit sorted target HP deltas simultaneously. Actors killed by this batch still deliver already-authorized outgoing strikes, but cannot act later.
4. **Equal-time physical follow-ups:** after combat HP commit, take current **post-combat** alive/reservation/lock state. A BOX-directed surviving bot at the exact box TOI may apply **one** reserved 1-HP box hit and is then consumed; dead bots never damage boxes, locked/destroyed boxes never consume bots. Revalidate and consume box reservations exactly once, order multiple eligible box hits by `(BoxId,BotId)`, emit one `BoxBroken/RewardGranted` per `(MatchId,BoxId)` and convert a pre-held terminal promise. Continue with valid fortress impacts and base breaches in stable `(TargetId,ActorId)` order. Box impact is a sacrifice, **not** a second enemy combat strike. Calculate final outcome after in-tick effects: base HP≤0 **LOSS**, otherwise fortress HP≤0 **WIN**, otherwise timeout **LOSS**.
5. **Recalculate after commits:** invalidate contact candidates for dead/consumed/locked actors and changed routes/boxes; regenerate affected future candidates strictly after `T`. Engagement participants pause movement; on enemy death surviving bots resume original immutable route **at next tick**, with no second enemy acquisition in the death tick. Due repeat strikes are scheduled at the exact rational threshold `T+C` if still in contact, even at `τ=1/4`; never add an integer tick. Enforce one strike per actor per time bucket; never spin on a zero-time contact. Sort canonical event payloads and living state by persistent IDs before versioned `EventHash/StateHash`, independent of collider callbacks, collection order or 30/60 rendering FPS.

### 12.2 Mandatory exact oracle examples (P2, **SPECIFIED NOT EXECUTED**)

| Scenario | Required authoritative result |
| --- | --- |
| C1/C2: enemy `E=HP2,dmg1` simultaneously meets `B10=HP2,dmg1` and `B20=HP2,dmg1`, equal squared distance | Both bots hit E once (E receives2→HP0); E retaliates **once** against lower-ID B10 (HP1), B20 stays HP2. Both bots survive and resume their original routes at next tick; permutations of input lists produce the same IDs/events/hash. |
| C2: `B10` delivers lethal hit while `E` also has eligible attack at same T | Both outgoing attacks legal from pre-batch snapshot and commit simultaneously; no retroactive cancellation of E strike. |
| C2 newcomer: E hit at `100+1/4`, B20 joins at `102+1/3` | Newly eligible B20 can hit; E **does not** retaliate again before `115+1/4` solely because B20 joined. |
| C3 fractional cooldown, `C=15` and initial strike `T=100+1/4` | **Next eligible is exactly `115+1/4`**, not `115`, `116` or `116+1/4`; no earlier repeated strike, frame-rate independent. |
| C4 equal TOI with box, `B10=HP2,dmg1` vs Scout HP1/dmg1 and BOX HP5 | Combat first: Scout HP0; B10 HP1. B10 is still alive/reserved, so box HP5→4 **once**, B10 consumed exactly once. Event order combat then box. |
| C4 equal TOI, `B10=HP1` vs same Scout HP1, valid box reservation | Simultaneous combat kills B10; box HP remains5, cancel reservation/terminal promise, no BoxDamaged/RewardGranted. |
| C5 full entitlement, `Active1+Pending1` or `Active1+Promise1` with third box target | `ShotSuppressed`, zero new/spent MiniBots for locked box, existing box HP untouched; no third reward and no box-side escrow. |
| C5 terminal box reservation and grant | Reserve entitlement **before** terminal shot spawn; final legal impact consumes bot and awards boost once; death/cancellation releases promise. A box reward on tick k can activate no sooner than k+1; ×2 window `[start,start+360)`; expiry/pending promotion before boundary raw shot. |
| C6 scope/replay | Only opt-in 60s arena, one ×2HP5 box, one simultaneous enemy group, P2-HP2 vs P3-HP3 matched presets, restart and terminal outcomes. Same config+commands under30/60renderFPS yields identical 30Hz event/state hashes and ID history; legacy gates and QA2-01 remain separate. |

### 12.3 Hand-off / release gates

- **Two-way docs sign-off: `ARCH_V12_EXACT_HEAD_ALIGNED`** — latest GDD PR #19 exact `b70da4e478b7a4b6dc54a403b130a20facf4e62f` ↔ Architect PR #23 predecessor exact `575f7ecd8fa0cecee7b8e3764890b343421ff57a`, followed by **this docs-only Architect correction commit** (its exact SHA published in PR #23/Issue #2). No conflicting current C1–C6 test oracle remains.
- **Developer P1:** separate [Draft PR #24](https://github.com/Gogenco/swarmforge-studio/pull/24) at reported HEAD `1281aed803a9f1bcdfb9de649623c6b783850762`; static checks alone **are not** real Unity EditMode/PlayMode/replay test execution. Verify current P1 HEAD and actual PASS evidence before any P2 authorization. Missing PASS means `P2_BLOCKED_PENDING_P1_UNITY_CONFIG_REPLAY_TESTS`, even though docs now align.
- **Ownership:** Developer owns implementation and P1 tests; Designer owns GDD v1.2; Architect owns this contract; QA owns independent test oracle and actual execution. Changed source documents only, **zero Unity code/asset or stable main edits**, no merge, VS-02, or declaration that QA2-01 is fixed. HP2/HP3 and hypothetical C15 remain adjustable comparison settings, **not** final game balance.

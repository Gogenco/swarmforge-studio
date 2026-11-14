# PERF-002 — GITHUB-005 Configurable Prototype Performance Preparation

- **Role:** 08 — Mobile Performance / Optimization Engineer
- **Task:** [PERF-002 / Issue #8](https://github.com/Gogenco/swarmforge-studio/issues/8)
- **Canonical Owner decisions:** [GDD v1.2 / Issue #18](https://github.com/Gogenco/swarmforge-studio/issues/18), especially [GITHUB-005](https://github.com/Gogenco/swarmforge-studio/issues/18#issuecomment-6880330660)
- **Existing report / Draft PR:** [#16](https://github.com/Gogenco/swarmforge-studio/pull/16), [`PERF002_VS01_PERFORMANCE_REVIEW.md`](PERF002_VS01_PERFORMANCE_REVIEW.md), [GITHUB-004 exploratory impact note](PERF002_GDD12_IMPACT_REVIEW.md)
- **Source baseline:** DEV-002.2 merged `main` `99cfcdc0b4982e9e441cf23a8669a487a7d75ea9`, 30Hz fixed-step Unity `6000.3.28f1`
- **Status:** `PERF_G005_DESIGN_AND_TEST_PLAN_READY_FOR_REVIEW` — documentation only, **no Unity/mobile profile performed**

## 1. Approved decisions vs remaining questions

This plan applies the **latest Owner choices**; the older GITHUB-004 document is historical and its speculative stacking/shot-quota assumptions are superseded.

| Area | Owner-APPROVED initial design | Still NOT approved; must remain editable / under Architect decision |
| --- | --- | --- |
| Box contact | MiniBot is consumed after **each successful box HP hit**; no separate box-damage projectile. | Exact collision/target priority, time-of-impact tie policy, simultaneous box destroy/award ordering. |
| Side boxes vs gates | New v1.2 arenas earn boosts from **destructible boxes**, no always-on ×1/×2/×3 gate path. | Gate-code migration and preservation of DEV-002.2/ARCH-003 regression history. Do not delete existing mechanics yet. |
| Box HP | ×2: **5 HP**; ×3: **10 HP**; ×5: **25 HP** (1 successful MiniBot box hit = 1 HP). | Final values may be retuned following playtests; other box types not yet specified. |
| Boost duration | **Time-only**: ×2 **12s**, ×3 **10s**, ×5 **6s**, counted from authoritative activation, **no shot quota**; in-flight shots not retroactively changed. | Exact tick boundary, pause semantics, timing relative to spawn/box award, interactions with Forge/other buffs. |
| Boost queue | **One active + one pending**, nonstacking; pending activates after active expires. Other multiplier boxes visibly locked while both slots full; do not reset partial box HP or silently lose rewards. | Exact same-tick lock/expiry/award ordering. Other reward types and queue interactions unapproved. |
| Friendly MiniBot combat | MiniBots survive some enemy contacts (**multi-HP**) and can fight again; successful **box hit still consumes** a bot. | Friendly HP **2 vs 3 deliberately deferred**; damage/cooldown/target persistence/once-per-hit, enemy HP/damage and death rules unapproved. No hard-coded final HP. |
| Enemy waves | Spawn in **simultaneous groups**; increase size, HP and archetype complexity gradually; do not instantly erase acquired advantage. | Group sizes, timing, exact enemy HP/mix, boss composition, first implementation slice. |
| UI/VFX | Nonblocking warning, `WAVE n/N`, boss/base alert; one-thumb controls/box interaction remain usable. | Exact effect/presentation/audio caps and trigger priorities, device fallback. |

**Critical dependency:** ARCH-003 [Draft PR #14](https://github.com/Gogenco/swarmforge-studio/pull/14) models the **old gate** timeline; Issue #18 still requires a revised 2D box/multi-hit/time-buff contract. No DEV-003/Unity implementation until Owner approves this and the reconciled [GDD PR #19](https://github.com/Gogenco/swarmforge-studio/pull/19). QA2-01 is still open; no VS-02.

## 2. Recalculated boost math (source arithmetic, NOT device or balance evidence)

### Explicit assumptions

- Baseline **30Hz**, one original `ATTACK` shot per 10 ticks = **3 raw shots/s** (from `LevelDefinition`/`MatchSimulation`, not a new fire-rate design).
- The boost covers exactly 360/300/180 consecutive simulation ticks, respectively; shots at a boundary are counted using a hypothetical **half-open** interval `[activate, expire)`. The Architect must approve exact timing/tie handling before code. In this idealized cadence the window contains 36/30/18 raw shots.
- Every raw shot stays in ATTACK mode, and its active multiplier produces exactly ×N MiniBots. There are no separate attack-speed/Forge effects, misses in box acquisition, replay rewrites, early terminal states or effects from intermediate combat.
- **Produced** counts every robot created, including one later consumed by an enemy or box. **Peak concurrently alive** is a different metric and cannot be derived from the production totals without the new HP, motion and collision rules.

| One earned buff | Duration = ticks | Raw shots during buff | Total bots **produced during buff** | Extra vs unboosted shots | Box HP / consumed successful MiniBots | Hypothetical extra bots minus box cost* |
| --- | --- | --- | --- | --- | --- | --- |
| ×2 | 12s = 360 ticks | 36 | **72** | +36 | 5 | +31 |
| ×3 | 10s = 300 ticks | 30 | **90** | +60 | 10 | +50 |
| ×5 | 6s = 180 ticks | 18 | **90** | +72 | 25 | +47 |

\* The last column is **only an illustrative robot-count opportunity cost** under immediate acquisition with one successful 1-HP box hit per sacrificed robot and no missed shots. It is **not** a kill rate or gameplay value estimate. A surviving HP2/HP3 MiniBot sacrificed to break a box may forgo more future combat capacity than a disposable one-HP bot. Box acquisition itself requires time/travel and draws fire away from defense; those costs are not included.

At constant 3 raw shots/s, a full hypothetical 60-second match with no boosts creates **180** original bots. If exactly one uninterrupted buff operates and all other shots remain unboosted, illustrative full-match production is **216 (×2)**, **240 (×3)** or **252 (×5)**. For two **sequential** nonstacking boosts in a 60s match:

| Two earned, fully delivered boosts | Active time | Total full-60s bot production | Extra vs baseline 180 |
| --- | --- | --- | --- |
| ×2 then ×3 | 12 + 10 = 22s | **276** | +96 |
| ×2 then ×5 | 12 + 6 = 18s | **288** | +108 |
| ×3 then ×5 | 10 + 6 = 16s | **312** | +132 |
| ×5 then ×5 | 6 + 6 = 12s | **324** | +144 |

**One-active/one-pending does not imply only two rewards per match:** later boxes may be earned once queue slots free. Under the restrictive mathematical assumption of a continuous ×5 boost across all 180 shots, a **theoretical produced-total ceiling is 180×5 = 900 bots/60s**, but actual match acquisition time and box costs make that idealization unrealistic. It is **not** an observed spawn count, live-object cap, or a bound if an independently approved attack-speed modifier changes shot cadence. ×2×3=×6 simultaneous multiplication is **not approved**.

### Live MiniBot / 120-visual-cap caveat

The original path (`AttackPathLength=51000`, speed `340` units/tick) implies approximately 150 movement ticks, or ~5s, for an **unopposed travelling** MiniBot to reach the enemy fortress. A solitary steady ×5 window could imply ~`15 robots/s × 5s = 75` concurrent travellers **if all move continuously**. That estimate **cannot be applied to the new multi-HP combat model**: survivor bots can be held up, fight repeatedly or remain active longer; grouped enemy contacts can keep logical B above the 120 **presentation-only** cap. This is the main new CPU/GPU correctness and readability stress case.

## 3. Revised cost model and configurable performance boundaries

- Let `B = alive logical MiniBots`, `E = alive enemies`, `X = targetable boxes`, `K = candidate contact/box/wave/reward events`. Existing DEV-002.2 enemy pair loop is **O(B×E)** per 30Hz tick; a naïve additional box loop would add **O(B×X)**, plus potential event ordering cost **O(K log K)**. With multi-HP survivors, both B and E may persist longer, and repeat-contact / cooldown state can grow. **No actual CPU time has been measured.**
- Group spawns are bursty: observe time of first instantiate/pool expansion, active enemy mesh count, concurrent hit VFX, overlap with wave callouts and queued reward unlocks. Do not mistake the 120 bot mesh cap for a total particle/enemy/draw-call cap.
- Balance must be **versioned config**, separate from deterministic simulation code: friendly MaxHP and damage, enemy archetype HP/damage, contact cooldown/cadence, group wave tick/count/lane mix, box HP and lifetime, boost durations, boss HP and relevant VFX limits. Record a stable `configVersion` and config checksum with every trial; avoid an in-game balance editor unless Owner authorizes it.
- **Two required comparable variants:** `HP2` and `HP3` differ **only in friendly MiniBot MaxHP**. All enemy, box, cooldown, wave, input, graphics and device parameters must match for a valid pairwise comparison. `HP2` and `HP3` outcome hashes **need not equal each other**; each preset must have repeatable hashes across render rates and repeated runs.
- Record `PeakAliveBots`, `PeakAliveEnemies`, `PeakActiveBoxes`, `DamageEventsPerTick`, `ContactCandidatesPerTick`, `BoxHits`, `BoxConsumedBots`, `ActiveBoost`, `PendingBoost`, `LockedBoxCount`, visible/pool mesh counts, `SimTickP95/P99`, frame p50/p95/p99, CPU/GPU frame, `GC.Alloc` B/frame/B/s and memory evolution. Proposed counters/instrumentation require later approval in a separate PR.
- **Provisional, not approved budget:** mainstream 60 FPS = ≤16.67ms p95 frame, entry 30 FPS = ≤33.33ms p95 frame; performance headroom and sim tick goals in existing PERF-002 are preliminary. No absolute RAM/heat/energy guarantees or measured 60 FPS claims until QA captures physical devices.

## 4. Updated GITHUB-005 stress and playtest matrix (all NOT RUN)

Run **real gameplay acceptance** with Owner-approved normal HP/base settings, and **separate CPU stress fixtures** with boosted base/fortress HP to prevent early terminal truncation. Never present stress-only values as tuned L1-L3 game balance. The following grouped waves are *proposed diagnostic fixtures only*, not Owner-approved gameplay timing:

`W1 @ tick 180: 8 enemies (4 left/4 right)`, `W2 @ tick 540: 12 (6/6)`, `W3 @ tick 900: 24 (12/12)`, all entering **within their respective single ticks**. Proposed enemy HP/damage/cooldown must come from the same not-yet-approved test config for paired HP2/HP3 runs.

| ID | Scenario (test-only, after GDD + Architect + implementation approval) | Required evidence / acceptance question |
| --- | --- | --- |
| G005-P01 | **HP2 vs HP3**: identical scripted aim/box decisions, W1/W2/W3 grouped waves, other parameters fixed. Run 30Hz sim with 30/60 render FPS per preset. | Base/fortress HP, survival rate, kills, time-to-kill, contact repeats, peak B/E, CPU p95/p99, per-preset replay stability. Neither preset is approved as final. |
| G005-P02 | **×2 12s / ×3 10s / ×5 6s** each alone, three reproducible exact activation ticks. | 36/30/18 shot-window production (subject to approved tick-boundary contract), expected 72/90/90 created, no retroactive in-flight changes, damage box-cost metrics. |
| G005-P03 | **Active1+pending1**: queue ×2 then ×5; attempt a third box while full, unlock after expiry; repeat around exact same-tick award/expiry. | No stacking; one pending max; locked boxes clearly shown and cannot consume further bots, partial box HP remains; stable IDs/events; no silently discarded boost. |
| G005-P04 | **Dense survivors**: HP3, repeated enemy multi-hit, simultaneous 8/12/24 group spawns; increasing number of still-alive bots. | Collision work vs B×E, repeat-hit once-only/cooldown, event-history growth, 120 mesh cap without logical damage loss; profiler spikes and longest tick. |
| G005-P05 | **Maximum time-only boost exposure**: consecutive ×5 6s windows while box acquisition is allowed, combined with grouped armored/boss enemies. | Actual peak alive B/E, time-varying bot count, queue behavior, object pool high-water; compare to the **900 produced-total theoretical upper bound**, not assume it is achievable. |
| G005-P06 | **Wave/UI/box overlap**: pre-warning begins 90 ticks before a grouped wave; show WAVE, boss, BASE alert, locked/queued boost state and box damage on the same portrait frame. | Safe-area/readability + active one-thumb aim, CPU/UI GC, render batches/overdraw, particles/audio voices and haptic throttling; gameplay remains unpaused. |
| G005-P07 | **Low-end Android & supported iPhone matrix**: run both HP2/HP3 with P01/P04/P06; each profile attached to Player on actual hardware, not Editor. | Exact model/SoC/GPU/OS/RAM, render scale/API, p50/p95/p99, CPU/GPU if supported, GC B/s, resident memory, screenshots and traces. Mark missing device metrics `UNAVAILABLE`, not zero. |
| G005-P08 | **20-minute endurance**: repeat same battle/wave/boost script, at least ten restarts, one background/resume during wave; cool device between comparable trials. | 1/5/10/20min frame-time, thermal state, throttle events, memory/GC trend, no backlog accumulation across suspended time, no performance regression after restart. |

**Script and evidence identity for each run:** `(source SHA, GDD/ARCH version, configVersion/checksum, preset HP2/HP3, wave fixture ID, command log/hash, build ID, device/OS/API, graphics quality, profiler trace path, UTC timestamp)`. Only pair runs with identical inputs, environment and build (except the one intentionally varied HP field). For reproducibility, a replay state hash should match within the same config across render FPS; **different** HP configurations may legitimately produce different hashes and outcomes.

## 5. Required Owner, Architect and QA handoffs

1. **Game Designer / PR #19:** reconcile fixed-shot draft to approved time-only 12/10/6s, consumed box hits, new no-gate arenas, active1+pending1, group waves and survivor HP2/HP3 presets; do not adopt the diagnostic W1/W2/W3 example as game balance.
2. **Architect:** define integer-tick activation/expiry inclusive/exclusive boundaries, box vs enemy TOI priority, deterministic repeated damage and cooldown, bot/enemy survival logic, queue locks and reward once-only, how old QA2-01 gate tests remain valid for historical DEV-002.2. Agree configVersion/hash and perf markers before Developer changes code.
3. **Developer:** prepare (not implement yet) a first-playable plan with editable versioned balance profiles. No hard-coded final friendly HP and no deletion of legacy source without Owner migration approval.
4. **Art/UI/Sound:** enforce visibility of multiplier lock/queue and remaining box HP at peak group density; design bounded effects/audio/haptics tiers while wave warnings do not hide interaction. Final cosmetic caps and performance envelope await assets and target devices.
5. **QA + Owner:** choose physical minimum Android/iOS targets and data-capture tools, review these test matrices, collect real paired HP2/HP3 outcomes after authorized build, then tune final HP and performance budgets with measured evidence.

**Current gate:** GDD PR #19 revision + ARCH-v1.2 docs proposal + explicit Owner authorization **before Unity/runtime code changes**. Existing [PERF Draft PR #16](https://github.com/Gogenco/swarmforge-studio/pull/16) remains unmerged. **No claimed FPS, memory, temperature, QA PASS, GDD merge approval or VS-02.**

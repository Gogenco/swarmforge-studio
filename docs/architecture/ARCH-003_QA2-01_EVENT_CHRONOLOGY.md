# SWARMFORGE — ARCH-003 v1.0: QA2-01 deterministic intra-tick chronology

**Task:** GAME-001 / ARCH-003 (Senior Game Architect / Tech Lead)  
**Status:** ARCHITECT_SIGNED_OFF / OWNER_REVIEW_REQUIRED  
**Scope:** VS-01 only — technical specification; **no gameplay code changes**  
**Source:** DEV-002.2 in `main` at `99cfcdc0b4982e9e441cf23a8669a487a7d75ea9`  
**Issue:** [#2](https://github.com/Gogenco/swarmforge-studio/issues/2)  
**Dependencies:** [master #1](https://github.com/Gogenco/swarmforge-studio/issues/1), [DEV-003 #4](https://github.com/Gogenco/swarmforge-studio/issues/4), [QA-003 #9](https://github.com/Gogenco/swarmforge-studio/issues/9)  
**Historical source:** [merged baseline PR #10](https://github.com/Gogenco/swarmforge-studio/pull/10)

## 1. Confirmed defect and non-goals

In `Assets/Swarmforge/Scripts/Simulation/Core/MatchSimulation.cs`, `AdvanceOneTick()` currently calls `ApplyGates(tick)` before `ResolveContacts(tick)`. Contacts use swept time-of-impact (TOI), so the current phase order can multiply a bot that physically died earlier in the same tick. QA-002 classifies this **QA2-01 HIGH / source-confirmed**, not a Unity runtime reproduction.

**Non-goals:** changing levels/balance, x1/x2/x3 values, 30-Hz simulation, Forge, Mech, Titan, VS-02, UI, shaders, audio, or URP configuration. The existing `IMatchSimulation`, `GameEvent`, `GameEventBatch`, `MatchSnapshot`, `OutcomeResolver`, `CommandHash`, and event publication contract remain in place.

## 2. Architect sign-off — unambiguous decisions

1. **Equal time: CONTACT before GATE.** A MiniBot killed by the contact does not get GateUsed and cannot create children.
2. **Gate before contact:** the parent multiplies at the actual gate-crossing time; new children become active **immediately** and may collide or hit the fortress in the remaining fraction of **that same tick**. They cannot interact before their gate birth time.
3. **Simultaneous contacts:** sorted by **EnemyDrone.Id ascending, then MiniBot.Id ascending**. Greedy consumption: commit a pair only if both are still alive; every unit participates in at most one successful contact.
4. **Simultaneous gates:** parent `MiniBot.Id` ascending. A gate creates exactly `GateForX(parent.CannonX)-1` children, in ascending clone-index order; allocate IDs only on *committed* gate processing.
5. **Fixed tie priority:** `CONTACT (0) → GATE (1) → FORTRESS (2) → BASE_BREACH (3)`. `FortressStageChanged` and `MatchEnded` remain derived end-of-tick events.
6. **Outcome:** finish all physical events in the tick, then call `OutcomeResolver` once. Existing `BaseHP <= 0` beats `FortressHP <= 0` when both reach zero in one tick.
7. **No double multiplication:** `GateUsed=true` on parent after committed gate and on each child at birth; stale/dead gate candidates do nothing.

## 3. Exact event time (fixed 30 Hz, no float comparisons)

At tick `k`, define normalized sub-tick `τ ∈ [0,1]`. For an existing MiniBot, `b(τ) = b0 + vB·τ`; for a head-on EnemyDrone, `e(τ) = e0 + vE·τ`, where all positions and per-tick velocities come from the current integer `LevelDefinition`. A unit with `BornTick == k` has velocity **0** for this tick (unchanged rule).

**Contact candidate for the same non-neutral lane:**  
`d0 = AttackPathLength - b0 - e0`, `R = ContactRadius`, `closing = vB + vE`.

- If `-R <= d0 <= R`, contact is already active at `τ=0`.
- If `d0 > R` and `closing > 0`, earliest contact `τC=(d0-R)/closing`, valid only if `τC <= 1`.
- If `d0 < -R`, the pair has already passed each other; no new head-on contact.
- If `closing=0` and there is no initial overlap, there is no contact.
- If lanes differ, no contact. Check `Alive` again at commit time.

**Gate candidate for a live MiniBot with `!GateUsed`:**

- If `b0 >= GatePosition`, handle as an immediate pending gate at `τG=0` (includes `GatePosition=0` tests).
- Otherwise, when `b0 < GatePosition <= b0+vB` with `vB > 0`: `τG=(GatePosition-b0)/vB`.
- Otherwise, no gate candidate in this tick.

**Terminal path crossings:**  
`τF=(AttackPathLength-b0)/vB` (fortress), `τB=(EnemyPathLength-e0)/vE` (base), valid when the positive-velocity trajectory first crosses its endpoint within `[0,1]`; already-at-endpoint means `τ=0`. If velocity is zero and endpoint not reached, no candidate.

Represent each candidate time as an exact **numerator/positive denominator** pair. Compare fractions by **exact cross-products**, never `Time.deltaTime`, `float`, arbitrary epsilon or rounded sub-tick buckets. Existing `decimal` cross-product comparison is acceptable for validated 32-bit config ranges; guard configured extremes/overflow rather than silently approximating. Equivalent fractions, e.g. `1/2` and `2/4`, must compare equal.

### Dynamic children, exact activation

A child created at `τG` inherits the parent's **virtual trajectory** for `τ >= τG`, but has an internal per-tick `ActiveFrom = τG`. This prevents retroactive contact before it exists while avoiding fractional position rounding. Its new Contact/Fortress candidates must be recalculated only over `[τG,1]`, including a contact **at** `τG` if already overlapping.

Preserve current public snapshot fields: child `BornTick`, `CannonX`, `Lane` copy from parent; `GateUsed=true`; `PreviousPosition=parent.PreviousPosition`; final `Position=parent position at τ=1`. `ActiveFrom` is an ephemeral scheduling property, **not** a replacement for public `BornTick`. Presentation interpolation never decides damage.

## 4. Canonical per-tick algorithm (implementation pseudocode)

1. Commit tick input in existing `InputCommandQueue` order; hash effective commands exactly as before.
2. Spawn raw MiniBot (if due), then current-tick enemies; no newly spawned unit moves this tick.
3. Freeze per-unit start positions and integer movement speeds. Compute straight-line swept trajectories, not authoritative end-of-tick gate effects.
4. Construct candidate Contact/Gate/Fortress/Base events with exact rational `τ∈[0,1]`. Candidate creation must **not** allocate stable IDs, publish events, or mutate counters.
5. Place candidates in a reusable event list/priority queue ordered by `(τ exact, kind priority, stable IDs)`. For equal-time contacts use `(EnemyId, MiniBotId)`; for gates use parent MiniBotId; for goals use the affected unit ID.
6. Pop one candidate. Revalidate `Alive`, `GateUsed`, lane, active-from time and (if used) entity-version. Ignore candidates invalidated by earlier events.
7. **CONTACT:** consume exactly one alive bot and one alive enemy. Increment `EnemyLosses` and `EnemiesDestroyed` once; `Publish(EnemyDefeated)` then `Publish(MiniBotLost)`. Future candidates for either ID become stale.
8. **GATE:** only if parent still alive and not `GateUsed`. Mark `GateUsed`; determine multiplier by existing `GateForX(CannonX)`. Issue `multiplier-1` IDs in clone-index order; update `GateExtraProduced` and `ProducedTotal` once per clone. Publish `GateMultiplied` once, including existing x1 semantics.
9. Immediately enqueue candidates for each new child in `[τG,1]`. A newly inserted same-time CONTACT takes priority over remaining equal-time gates/goals. New children never receive a gate candidate.
10. **FORTRESS:** consume only a live bot that actually reached the fortress; `FortressHits++`, clamp HP, publish damage once. **BASE_BREACH:** analogous for enemy and `Breaches`.
11. After all candidates: set final positions for survivors at `τ=1`, remove consumed units, normalize living MiniBot/Enemy collections by stable ID before snapshot construction. Compute final `FortressStage` once; call `OutcomeResolver` once; check conservation; construct immutable event batch, append history and snapshot.
12. Restart creates a fresh match-scoped simulator. No queued candidates, pending IDs or per-tick `ActiveFrom` may survive restart.

### Stable ID issuance

Within a tick allocate IDs in this order:

1. `RawShot`, if due.
2. `EnemySpawned`: combine same-tick waves as `Left` and `Right` counts, then spawn left lane `-1` before right lane `+1` (independent of array ordering).
3. Additional clones **at committed Gate events** sorted by exact `τG`, parent `MiniBot.Id`, and clone index `1..multiplier-1`.

`IssueId()` is never called for a future/stale candidate or a dead parent. IDs remain monotonic and are not reused during a match. No ordering rule may depend on `List<MiniBot>`, `List<EnemyDrone>`, dictionary iteration, Unity object order, or renderer FPS.

## 5. Exact counterexample and reference results

From QA-002: `AttackPathLength=10000`, `GatePosition=9000`, `MiniBotSpeedPerTick=10000`, `EnemySpeedPerTick=9000`, `ContactRadius=25`, one left-lane x2 MiniBot and one left Enemy born on tick 0.

On tick 1:

- `τContact=(10000-25)/(10000+9000)=9975/19000=0.525`.
- `τGate=9000/10000=0.9`.
- Contact wins before gate. Expected: `ProducedTotal=1`, `GateExtraProduced=0`, `EnemyLosses=1`, `EnemiesDestroyed=1`, `FortressHits=0`, **no `GateMultiplied` event**. The center-to-center overlap coordinate mentioned by QA (~5263) is later than the first physical contact with radius 25 (~5250).

**Gate-first control:** only change `GatePosition=3000` in the isolated test fixture. Gate occurs at 0.3, contact at 0.525; x2 creates one child that is active within the same tick. One bot and the enemy die; one bot hits the fortress at `τ=1`. Expected `ProducedTotal=2`, `GateExtraProduced=1`, `EnemyLosses=1`, `FortressHits=1`.

**Exact tie:** with `GatePosition=5250`, contact and gate both occur at `τ=0.525`. Contact first; **zero clones**.

**Simultaneous pairing oracle:** left x3 (only in fixture), `GatePosition=3000`, two left enemies. Tick 0 assigns `raw=1, enemy=2,3`. Tick 1 gate assigns clones `4,5`; at 0.525, contacts commit `(enemy2, bot1)`, then `(enemy3, bot4)`; bot5 reaches Fortress at 1. End totals: `ProducedTotal=3`, `EnemyLosses=2`, `FortressHits=1`.

## 6. Event history, determinism and invariants

- Keep current `CommandHash` input hashing; 30/60 rendered FPS must feed the exact same commands into the 30-Hz simulator.
- Continue to call existing `Publish(tick,kind,source,target,value)` **only on committed events**. Candidate generation, stale-event skips, visual callbacks and rejected gates never affect `EventHash`.
- `LastEventBatch` / `GetEventHistory()` remain immutable copies. The order of `GameEventBatch.Items` is event chronology followed by stable tie-break.
- Prior to `MatchSnapshot`, sort both live collections by `Id` so state hashing is independent of internal collection permutation.
- Same source baseline, config, command log, match ID and seed (if any) must yield identical per-tick `StateHash`, `EventHash`, event history and match result at 30 and 60 render FPS.
- **Expected compatibility break:** previously incorrect QA2-01 cases produce different EventHash/StateHash after the fix. Preserve deterministic hashing *within DEV-003*, not exact equality with the buggy DEV-002.2 hash. Record simulation ruleset `ARCH-003/DEV-003` in QA evidence.
- After every tick: `ProducedTotal = ActiveMiniBots + FortressHits + EnemyLosses`; `GateExtraProduced >= 0`; `TerminalEvents <= 1`; no duplicated impact, death, clone or breach.

## 7. Required regression matrix (expected; not run by Architect)

| ID | Scenario | Mandatory assertion |
|---|---|---|
| R01 P0 | Enemy contact before gate, QA2-01 high speed | No clones; no Fortress hit; ledger exact |
| R02 P0 | Gate before enemy contact | Child participates in same tick, survives as expected |
| R03 P0 | Contact and gate at exact rational tie | CONTACT wins; zero clones |
| R04 P0 | x3 parent, two same-time enemy contacts | EnemyId/BotId stable matching, one surviving clone |
| R05 P0 | Dead parent would also cross Fortress in same sweep | No gate/goal from dead unit |
| R06 P0 | Child contact at or after its birth fraction | No retroactive contact; same-tick events enabled |
| R07 P0 | One enemy overlaps two bots simultaneously | One successful pair only |
| R08 P0 | Two enemies and two bots on same timestamp | Pair order EnemyId then MiniBotId |
| R09 P1 | GatePosition=0 and born tick0 | Immediate Gate even without movement; next-tick movement |
| R10 P1 | Gate and Fortress crossed inside one tick | Children hit Fortress at most once each |
| R11 P0 | Contact and Gate tied at τ=0 | Contact first; no resurrection or multiplication |
| R12 P0 | Two gates tied with parents IDs 7 and 10 | Parent7 first, clone-index issue order |
| R13 P0 | Permute internal entity lists before tick | Same IDs/events/StateHash |
| R14 P0 | Identical command log at 30 vs 60 render FPS | Per-tick hashes/outcomes match |
| R15 P0 | Keep previous `GameEventBatch` across 10 ticks | Immutable Items and CumulativeHash |
| R16 P1 | Fortress reached before contact vs after | Physical τ, not old phase order |
| R17 P1 | Base breach before contact vs tied | Earlier breach commits; tie contact prevents breach |
| R18 P1 | Base and Fortress both 0 in one tick | Existing LoseBase priority; one terminal event |
| R19 P1 | Restart 20× after contact/gate/terminal | Fresh ID1, no stale events/history |
| R20 P1 | Neutral x1, repeat gate, clone near gate | Zero added x1 units; GateUsed blocks second multiplication |
| R21 P1 | Reorder same-tick EnemyWave records | Same spawn counts/ID/event/hash |
| R22 P1 | Multi-bot conservation every tick | ProducedTotal identity always holds |

Keep all existing DEV-002.2 tests. Unity 6000.3.28f1 compilation, EditMode results, PlayMode results and independent QA-003 must be reported separately with screenshots/XML where available. **These new tests have not been executed by the Architect.**

## 8. Handoff, ownership and approval gate

- **Architect (#2):** owns this contract and reviews DEV-003 implementation against exact rational times, stale candidates, clone activation, ID order and replay hashes.
- **Game Developer (#4):** after Owner approval, changes only relevant simulation/tests on scoped branch `fix/dev003-qa201-ordering` from current `main`, submits a **Draft PR**, does not self-merge.
- **QA (#9):** independently reviews regression evidence and Source vs Unity runtime claims.
- **UI/UX:** unrelated QA2-02 handled in its own task.
- **URP QA2-03:** separately documented in `ARCH-003_QA2-03_URP_PERSISTENCE_NOTE.md`; not part of this code fix.

**Architect result:** `ARCH-003_SPEC_READY_FOR_OWNER_SIGNOFF`. Owner must explicitly approve the linked docs PR/contract **before DEV-003 implementation or merge**. This docs PR does not fix QA2-01 or declare VS-01 ready. **VS-02 remains prohibited.**

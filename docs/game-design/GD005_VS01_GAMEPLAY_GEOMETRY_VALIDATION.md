# SWARMFORGE — GD-005: VS-01 Gameplay & Geometry Validation

**Role:** 03 — Senior Game Designer / Level Designer  
**Date:** 2026-11-14 (Asia/Yerevan)  
**Scope:** Static-only design audit, data/geometry comparison; no gameplay or Unity changes  
**Review state:** `READY_FOR_OWNER_REVIEW`, not a VS-01 or L3 balance sign-off  
**Base:** `main` / Owner-approved DEV-002.2 merge [`99cfcdc0b4982e9e441cf23a8669a487a7d75ea9`](https://github.com/Gogenco/swarmforge-studio/commit/99cfcdc0b4982e9e441cf23a8669a487a7d75ea9)  
**Issues:** [Game Design #3](https://github.com/Gogenco/swarmforge-studio/issues/3) · [Master #1](https://github.com/Gogenco/swarmforge-studio/issues/1) · [Architect ARCH-003 #2](https://github.com/Gogenco/swarmforge-studio/issues/2) · [Developer DEV-003 #4](https://github.com/Gogenco/swarmforge-studio/issues/4) · [QA #9](https://github.com/Gogenco/swarmforge-studio/issues/9)  
**Baseline PR:** [#10 — DEV-002.2](https://github.com/Gogenco/swarmforge-studio/pull/10), merged; **this document** is a separate documentation-only proposal.

## 1. Executive decision

The DEV-002.2 baseline is a **VS-01 test arena**, not completed designed levels L1–L3. It demonstrates a fixed-step, one-dimensional confrontation model with three gate choices and a single two-enemy wave. It must **not** be interpreted as a runtime validation of GD-004's two-dimensional shared defensive choke, nor of the two L3 winning strategies.

Four actionable findings:

1. **QA2-01 — HIGH / source-confirmed:** `ApplyGates()` precedes `ResolveContacts()` for an entire tick. It can multiply a MiniBot that physically collided with an Enemy earlier inside that same tick. [ARCH-003 #2](https://github.com/Gogenco/swarmforge-studio/issues/2) must define chronological resolution before [DEV-003 #4](https://github.com/Gogenco/swarmforge-studio/issues/4) changes code. This is a source-order finding, **not** a Unity runtime reproduction.
2. **GD-004 alignment — HIGH for future L3:** current `ResolveContacts()` requires `bot.Lane == enemy.Lane` and works with scalar path progress. GD-004's shared choke lets ATTACK MiniBot contact Enemy from either road. That behavior cannot be validated by the current VS-01 logic or display without a separately approved future implementation.
3. **Level content — expected scope gap:** current asset contains **only 2 Enemy at t=12, Core 90 HP**, not L1's 8 total Enemy or L2/L3. This is **not** a confirmed defect of the deliberately limited VS-01 smoke arena.
4. **Gate presentation — future level-design question:** current arena puts ×2 on the left, ×1 in the center, ×3 on the right. Proposed GD-004 L1 teaches a centered ×2 gate; both are valid individually but are **not equivalent playable level designs**. Owner must confirm the eventual tutorial sequence; no reconfiguration requested now.

### Evidence confidence

`SRC` = inspected exact DEV-002.2 source from merge commit; `DESIGN` = approved/planned GDD v1.1 / GD-004; `CALC` = deterministic arithmetic based on explicitly stated assumptions; `UNKNOWN` = Unity/replay/device observation not performed in GD-005. **No PASS claim is inferred from source inspection.**

## 2. Exact DEV-002.2 source baseline (`SRC`)

| Field | `VS01_Arena.asset` / simulation value | Game-design meaning |
|---|---:|---|
| `LevelId` | `VS01_TEST_ARENA` | Test configuration, not published L1 |
| `TickRate`, `MatchTicks` | 30 Hz; 1800 | t=0…60 s, final outcome on tick 1800 |
| `FirstShotTick`, `ShotPeriodTicks` | 0; 10 | 180 raw shots at ticks 0,10,…,1790 if match lasts |
| `BaseHP`, `FortressHP` | 3; 90 | L1-compatible HP only |
| `FixedUnitsPerWorldUnit` | 3000 | Fixed integer path progress conversion |
| `MiniBotSpeedPerTick` | 340 | 340×30/3000 = **3.4 world units/s** |
| `EnemySpeedPerTick` | 230 | 230×30/3000 = **2.3 world units/s** |
| `GatePosition` | 9000 | 3.0 world units along MiniBot scalar path |
| `AttackPathLength`, `EnemyPathLength` | 51000 each | 17.0 units for each unit type |
| `GateLeftMaxX`, `GateRightMinX` | -120; +120 | ×2 if ≤-120, ×1 between, ×3 if ≥+120 |
| `CannonMinX`, `CannonMaxX` | -1000; +1000 | Simulation input coordinate, **not** world X in metres |
| `ContactRadius` | 450 | Scalar sweep tolerance = 0.15 units, **not** a 2D capsule radius |
| `EnemyWaves` | `(tick360, Left1, Right1)` | Exactly 2 Drone spawned at t=12 |
| `MaxVisibleMiniBots` | 120 | Presentation cap only; not a logical unit cap |

**Code evidence:**

- [`LevelDefinition.cs`](../../Assets/Swarmforge/Scripts/Simulation/Core/LevelDefinition.cs) defines the fields, `GateForX()`, `LaneForX()`, and validation.
- [`VS01_Arena.asset`](../../Assets/Swarmforge/Configs/Levels/VS01_Arena.asset) stores those concrete values.
- [`MatchSimulation.cs`](../../Assets/Swarmforge/Scripts/Simulation/Core/MatchSimulation.cs) implements spawn, gates, same-lane contacts, Core damage, Base breaches and precedence.
- [`PrimitiveBattleView.cs`](../../Assets/Swarmforge/Scripts/Presentation/PrimitiveBattleView.cs) maps simulation X to visual X and draws separated visual lanes.
- [`BattleEntryPoint.cs`](../../Assets/Swarmforge/Scripts/Presentation/BattleEntryPoint.cs) implements portrait input, basic HUD and Restart.
- [`Vs01SimulationTests.cs`](../../Assets/Swarmforge/Tests/EditMode/Vs01SimulationTests.cs) has pre-existing test scenarios. Their presence is **not** a new test run.

### 2.1 Visual-to-simulation coordinate warning

`PrimitiveBattleView.WorldX()` maps the input domain [-1000,+1000] to visual x∈[-3.2,+3.2]. Thus the neutral thresholds ±120 display at approximately **visual x=±0.384**. At match start `CannonX=-650` displays at **visual x=-2.08** (left ×2 lane); a player dragging to zero enters neutral ×1. Enemy visuals are fixed to x=±1.85, and MiniBot visuals interpolate from initial X toward their **own** lane x=±1.85.

This is an internally understandable **VS-01 greybox convention**, but importing GD-004 XYZ positions (e.g., muzzle x=±0.9) as raw `CannonX` integers would be incorrect. Neither the asset nor `LevelDefinition` contains actual enemy waypoint arrays, Forge waypoint fields, or shared-choke collision geometry.

## 3. Design baseline GD-003 / GD-004 (`DESIGN`, not implemented)

| | L1: FIRST SHOT | L2: FIRST FORGE | L3: TITAN HEART |
|---|---|---|---|
| Core HP | 90 | 190 | 270 |
| Enemy total | 8 (4×2) | 30 (6×5) | 48 (8×6) |
| Wave times, seconds | 12,20,28,36 | 10,17,24,31,38,45 | 20,25,30,35,40,45,50,55 |
| Tower | OFF | every 3 s from t12 | every 4 s from t12 |
| Gates | centered ×2 | ×2 left, ×3 right | ×2 left, ×3 right |
| Forge machines | None | Mech 50 / 2 s | Mech 50 / 2 s, Titan 120 / 4 s |
| Base / timer | 3 HP / 60 s | 3 HP / 60 s | 3 HP / 60 s |
| In current VS-01 asset? | **Partial smoke only** | **No** | **No** |

Geometric references from GD-004 (all are **proposal coordinates**, not current Unity state):

- L1 cannon `(x=0,z=0.6)` → center x2 `(0,3)` → Router `(0,5.5)` → Core `(0,17.6)` = **17.0 units**.
- L2/L3 gate routes: cannon at x≈±0.9, gate centers `(±0.9,3.0)`, shared Router `(0,5.5)`, Core `(0,17.44)`; ATTACK path **≈16.997 units / 4.999 s**.
- Optional FORGE intake `(3.1,8.0)` with locked routed length **≈10.207 units / 3.002 s**; Mech/Titan exit `(1.95,9.85)`, Titan-to-Core **≈9.196 units / 7.997 s**. **Not part of VS-01**.
- Two Enemy road groups, **3 strands per side** in L3, converge around x=±(0.18,0.31,0.44) over z=5.5…10; each Drone route length **17.0 units** to Base breach plane. These are 2D circle/capsule interactions, **not** current scalar lane checks.

## 4. First-wave and encounter arithmetic (`CALC`; not playtest evidence)

### 4.1 Current VS-01 arena

- Enemy wave at tick 360 (t=12); every enemy has 51000 fixed units left at 230 units/tick.
- Enemy requires `ceil(51000/230)=222` moving ticks to breach, under the present born-on-this-tick-does-not-move convention.
- **Unopposed earliest breach:** tick `360+222=582` = **t=19.4 s**, with 2 total Enemy. This is an **arithmetic trajectory**, not a measured runtime event.
- A raw MiniBot requires `ceil(9000/340)=27` moving ticks to pass the gate and `ceil(51000/340)=150` to reach the Core, in absence of contacts; spawn tick0 begins movement on tick1.

### 4.2 Proposed future L3 first wave — NOT currently executable with GD-004 geometry

- First spawn t=20 = tick600; each Enemy needs 17.0 /2.3≈7.391 s → unopposed logical breach around **tick822 / t27.4 s**.
- Six first-wave Drone split **3 left / 3 right**. Since Base starts with 3 HP, **at least four distinct Enemy kills are required** before the third breach, unless the Core is already destroyed without a simultaneous Base loss. GDD requires both ATTACK-only and early Titan strategies to be viable.
- GD-004 first shared-choke entry around **t23.23**, exit around **t25.19**; matching ATTACK shot-time opportunity roughly **t20.42…23.70**. The estimate of **20–28 potential bots** over 10 shots depends on ×2/×3 share, successful aiming, no earlier losses, and eventual 2D collision. It is **not** a proven kill count.
- With `AttackPathLength=17`, even a hypothetical all-×3 attacker produces at most about `9×(27.4−5)=201.6` potential Core impacts by the first breach time before any Enemy/tower losses, **below L3 Core 270 HP**. Therefore the game cannot safely ignore the first 6 Drone even when the attack stream is strong.

### 4.3 A deterministic counterexample if L3 were ported without redesigning contact semantics

`ResolveContacts()` explicitly filters out `bot.Lane != enemy.Lane`. If the player uses only ×3/right (`Lane=+1`), all left `Lane=-1` Enemy are unreachable to those MiniBot. Under a 3L+3R first-wave schedule, **all three left enemies breach at≈t27.4**, causing `LoseBase` even if every right enemy is killed. This is a **deduction from source conditions + hypothetical L3 data**, not an executed L3 replay, and is an explicit barrier to claiming GD-004 validation from DEV-002.2.

## 5. QA2-01 effect on game balance

**Source sequence today:** advance scalar positions → `ApplyGates(tick)` → `ResolveContacts(tick)` → Fortress impacts → breaches → outcome. `ApplyGates()` checks end-of-tick position `Position>=GatePosition`; it does not compare its own gate crossing fraction against an earlier collision's time of impact. A bot may be multiplied and its clones participate in the contact list before that original should have survived to the gate. Consequences can include extra defenders and extra future Core damage, violating intended ×2/×3 economy.

The bug can matter near L3's first potential breach: to reach a point ≈3 units from the player's side, an Enemy covers ≈14 units from spawn, taking ≈14/2.3=**6.09 s**; for a t20 wave this is around **t26.09**, leaving only ≈1.30 s until t27.4. Such late contacts can change whether Base HP crosses zero.

**Design judgment:** correcting event chronology should implement the *existing intended rule* (dead-before-gate units do not multiply), not increase Core HP, decrease wave counts, increase bots/second, or change Titan cost. Any apparent change in success rate after DEV-003 is a measurement request, not an automatic authorization to rebalance.

ARCH-003 must settle: intra-tick event time, exact tie priority, whether gate-born children can participate in the remainder of the tick, ID issuance and determinism. GD-005 makes **no claim** that these decisions are final at the time of this document.

## 6. Win/Lose behavior and semantics

`OutcomeResolver.Resolve()` checks in order: `(1) BaseHP<=0 => LoseBase; (2) FortressHP<=0 => Win; (3) tick>=1800 => LoseTimeout; (4) Playing`. This **matches** the agreed Base-priority same-tick tie. A Core destroyed on the final permitted tick with Base≥1 wins. This is a **source contract**, not a newly exercised regression test.

Review requirements after DEV-003:

1. Paired contacts must be earlier than goal crossings if their time-of-impact is earlier within the tick; one MiniBot cannot destroy two enemies.
2. Fortress and Base reaching zero in the same logical tick must still yield `LoseBase`, regardless of event publication order.
3. Restart must create a new MatchID and independent State/Event hashes; repeated result UI must not award two wins.
4. A t60 terminal outcome must accept only shots that actually spawned by tick1790 and completed valid impacts by the final outcome resolution.

## 7. Findings, severity and ownership

| ID | Type / priority | Finding | Owner / action |
|---|---|---|---|
| GD005-R01 | **SRC / HIGH** | QA2-01 gate-before-earlier-contact ordering in `MatchSimulation` | Architect ARCH-003 → Developer DEV-003 → QA-003; **not** a design-code fix |
| GD005-R02 | **DESIGN GAP / HIGH for L3** | Same-lane-only scalar collisions cannot validate GD-004 shared choke | Game Architect to select approved future geometry representation; Owner to approve scope |
| GD005-R03 | **SCOPE GAP / MED** | Current arena has only one `1+1` wave vs designed L1 8 Enemy | Keep VS-01 smoke baseline; implement actual L1 only on later Owner authorization |
| GD005-R04 | **DESIGN GAP / MED** | Center ×1 VS-01 differs from L1 centered ×2 teaching flow | Owner/Game Designer tutorial decision before authored L1 |
| GD005-R05 | **DESIGN GAP / MED** | `CannonX` input coordinates are not direct world X; GD-004 coordinates cannot be pasted into config | Architect + Gameplay define versioned world->logic mapping before VS-03 |
| GD005-R06 | **UNKNOWN / HIGH** | No verified L3 kill≥4, two-win-strategy replays, or cross-device data | Developer + QA after authorized work; keep L3 `NOT VERIFIED` |
| GD005-R07 | **SOURCE / MED** | Visual and gameplay proximity differ: `ContactRadius=0.15` scalar vs GD-004 proposed 2D radii | Architect to distinguish current 1D semantics from future 2D collision design |
| GD005-R08 | **DEPENDENCY / MED** | QA2-02 mobile HUD, QA2-03 URP persisted assets remain open | UI/UX #6, Architect #2, Developer #4, QA #9 |

## 8. Replay acceptance matrix (planned; **NOT RUN**)

| ID | Stage / prerequisite | Required evidence |
|---|---|---|
| G-01 | VS-01 baseline | 3× gate 1/2/3 with 10 raw shots each; 10/20/30 outputs; include simultaneous Enemy contact near gate |
| G-02 | After approved ARCH-003/DEV-003 | Enemy-before-gate produces **no clones**; gate-before-enemy creates valid copies; exact-tie case follows approved policy |
| G-03 | After DEV-003 | Stable paired-event IDs/ReplayHash for identical commands at 30 vs 60 render FPS; no duplicate death/hit |
| G-04 | VS-01 smoke | A single t12 `(1L,1R)` wave and Core90/3HP base still yield reproducible outcomes |
| G-05 | Future owner-approved L1 | x2-centered tutorial, 4 waves ×2 at 12/20/28/36; actual 8 Enemy / Core90 |
| G-06 | Future owner-approved L2 | 6 waves ×5; 190 HP Core; real Forge/Mech (not VS-01) |
| G-07 | Future owner-approved L3 geometry | 3+3 first wave t20, 17-unit routes, expected no-intercept breach around tick822, screenshot+event logs |
| G-08 | Future owner-approved L3 | ONLY_ATTACK p40/60/80 and EARLY_TITAN p40/60/80: ≥4 first-wave kills, no 3rd breach before Win, Win by t60 |
| G-09 | Future owner-approved L3 | Tower t12,16,…,60 (13 eligible checks), tie->LOSE, exact early/late hit tick traces |

**No entries are being claimed `PASS` by GD-005.** QA-002 is source-only; the Owner reported Unity DEV-002.2 compilation, 24/24 EditMode screenshot and 2/2 PlayMode verbally. Those historical observations are **not** substitutes for independent DEV-003/QA-003 runtime evidence.

## 9. Decisions requested from Owner (none applied)

1. **O-GD005-01:** Confirm the current mixed ×2/×1/×3 VS-01 test arena remains a smoke fixture; designed L1 will retain a centered ×2 tutorial only when actual Level 1 work is separately authorized. **Recommendation: YES.**
2. **O-GD005-02:** Confirm GD-004's future shared defensive choke is still the L3 design target (separate from VS-01 1D lane mechanics). **Recommendation: retain as provisional geometry, pending runtime collision proof.**
3. **O-GD005-03:** Authorize Architect to propose an explicit 2D waypoint/collision data contract for future L1–L3. **No implementation now**; must not be confused with ARCH-003's immediate timing fix.
4. **O-GD005-04:** Retain all approved L1/L2/L3 HP, wave schedule, speed, multiplier, machine prices and timer pending post-DEV-003 measurements. **Recommendation: YES.**

## 10. Checks performed and next steps

**Performed:** read-only GitHub review of merge commit `99cfcdc…`, merged PR #10, Issue #1/#2/#3/#4/#9 requirements, LevelDefinition/VS01_Arena, MatchSimulation, MatchState/MatchSnapshot, PrimitiveBattleView, BattleEntryPoint, QA-002 findings and VS-01 release gate. Checked arithmetic for ticks 582 and 822 and relative world/input mapping.

**Not performed:** compiling Unity, executing EditMode/PlayMode, replay collision simulation, device FPS measurement, mobile build, Level 2/3 runtime validation or changes to gameplay code.

**Files changed:** this document only, on a new design-owned branch and a Draft PR (no merge).  
**Blocked dependency:** [ARCH-003 #2](https://github.com/Gogenco/swarmforge-studio/issues/2) → [DEV-003 #4](https://github.com/Gogenco/swarmforge-studio/issues/4) → [QA-003 #9](https://github.com/Gogenco/swarmforge-studio/issues/9).  
**Next step:** Owner reviews GD005-R01…R08 and O-GD005-01…04; Gameplay/QA rerun defined regression scenarios only once the relevant fixes and stages are explicitly authorized.

**Final design handoff:** `GD-005_READY_FOR_OWNER_REVIEW`; **VS-01 RELEASE: BLOCKED**; **L3 BALANCE: NOT VERIFIED**; **VS-02: NOT STARTED**.

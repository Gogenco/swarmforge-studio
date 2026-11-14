# PERF-002 — VS-01 Performance Review

- **Task:** [#8 — PERF-002](https://github.com/Gogenco/swarmforge-studio/issues/8)
- **Owner:** 08 — Mobile Performance / Optimization Engineer
- **Status:** `READY_FOR_REVIEW` — static review and reproducible test plan; **device performance NOT VERIFIED**
- **Review baseline:** `main` merge commit `99cfcdc0b4982e9e441cf23a8669a487a7d75ea9` (DEV-002.2 / PR [#10](https://github.com/Gogenco/swarmforge-studio/pull/10))
- **Engine:** Unity `6000.3.28f1`, URP `17.3.0`, C#, portrait Android/iOS
- **Scope:** VS-01 only. **No game-code, balance, rendering-setting, or scene modifications in this review.**

## 1. Evidence and limits

- **Verified from source:** `LevelDefinition.TickRate = 30`, `MatchTicks = 1800`, `ShotPeriodTicks = 10`, `MaxVisibleMiniBots = 120`; `BattleEntryPoint.Start()` requests 60 rendering FPS. The runtime Unity project does not contain persisted URP renderer/pipeline assets in this baseline.
- **Existing Owner-reported evidence:** Unity compilation, EditMode 24/24 and PlayMode 2/2 passed in the Owner's Unity editor; these are *functionality* checks, not mobile benchmarks. See [`VS01_GATE`](../release/VS01_GATE.md).
- **Not performed for PERF-002:** Unity Editor runtime profiling, Android/iOS builds, physical-device frame/GPU/memory sampling, thermal sampling, benchmark comparisons, additional Unity tests, and DEV-003 performance measurements. No FPS, draw-call, memory-footprint, temperature, or battery-life figures are presented as measured values.
- **Known release blocker outside this task:** QA2-01 HIGH, chronologically incorrect gate-vs-collision ordering within a tick; architecture [#2](https://github.com/Gogenco/swarmforge-studio/issues/2) must be approved before developer [#4](https://github.com/Gogenco/swarmforge-studio/issues/4) implements DEV-003; QA [#9](https://github.com/Gogenco/swarmforge-studio/issues/9) independently verifies it.

This is a source-derived risk assessment, not a claim that a particular device is slow or overheats.

## 2. Static findings and prioritized risks

| ID | Severity / evidence | Exact source | Finding and performance consequence | Proposal / owner |
| --- | --- | --- | --- | --- |
| P01 | **HIGH potential, SOURCE-CONFIRMED structure** | `MatchSimulation.cs:210–251`, `ResolveContacts` | Every live enemy iterates all live MiniBots: **O(E × B)** pair checks each simulation tick, even when lanes differ. Crossing candidates accumulate in a new list and are sorted, up to **O(C log C)** for `C` candidate contacts. Candidate comparison uses `decimal` cross-products. A high-density DEV-003 event timeline could increase this cost. | Measure B/E/C and the exclusive CPU time of contact generation/sort on identical scripted seeds before/after DEV-003. Any lane partitioning, spatial indexing, or buffer reuse requires Architect/Developer approval and deterministic-order regression tests. |
| P02 | **MEDIUM–HIGH potential, SOURCE-CONFIRMED allocations** | `StableVisualSelection.cs:12–32`; `PrimitiveBattleView.cs:75–120` | **Every rendered frame** builds a dictionary, result list, hash set, copied/sorted candidate list, and another ID→UnitView dictionary. The sort occurs even when fewer than 120 logical bots exist. Large B affects presentation CPU and GC even though only 120 bots are displayed. | Profile `Render`/`Choose` CPU and GC at 30/60 FPS, especially B>120; propose reusable working buffers or less frequent recomputation in a separate approved presentation change. Preserve stable-ID selection and the 120 visual cap. |
| P03 | **MEDIUM potential, SOURCE-CONFIRMED allocations** | `MatchSnapshot.cs:56–105`; `MatchState.cs:28–42`; `MatchSimulation.cs:124–129` | Each executed 30-Hz tick builds a `MatchSnapshot`, two unit arrays and read-only wrappers; each `GameEventBatch` copies events to an array/wrapper. `history` retains a batch for every processed tick until the `MatchSimulation` instance is discarded. Total memory depends on active units/events and match length; it is bounded per match, but can create periodic GC pressure. | Sample allocations per tick and after repeated restarts; preserve immutable snapshots/replay evidence. Do not pool publicly exposed snapshot/event storage without a contractual redesign. |
| P04 | **MEDIUM potential, SOURCE-CONFIRMED allocations** | `BattleEntryPoint.cs:105–201`; `InputCommandQueue.cs:31–49` | `OnGUI` constructs `GUIStyle` instances, UI strings and other temporary data on its invocations; the command queue allocates a string key on submission and an empty list for a tick without commands. `GetSnapshot()` **returns a cached object** and is not itself a snapshot allocation. | In device CPU Profiler, record OnGUI frequency/allocations, interaction burst, and idle tick allocation. Route UI optimizations to #6; gameplay input changes to #4 only after coordination. |
| P05 | **MEDIUM potential, SOURCE-CONFIRMED scope limit** | `LevelDefinition.cs:30`; `PrimitiveBattleView.cs:82–120`, `:151–178` | The **120 cap applies only to rendered MiniBots**, not to simulation entities or rendered enemies. `botPool` and `enemyPool` reuse objects after peak creation, but first expansion uses `GameObject.CreatePrimitive`, creating renderer/mesh/collider components (colliders are disabled). Enemy visuals have no analogous cap. | Profile peak and steady-state active objects, pools, instantiate spikes, draw calls, SetActive work, and enemies at dense waves. Do not change caps or hide logical units without Art/Game Design/Architect approval. |
| P06 | **MEDIUM potential / NEEDS DEVICE MEASUREMENT** | `PrimitiveBattleView.cs:27–73`, `:166–205`; `SwarmforgeProjectSetup.cs:50–89` | Greybox creates primitive meshes, shared materials and a camera/light at startup; unlit/material fallback is pipeline-aware. Real URP render passes, batches, triangles and GPU load depend on the actual generated pipeline, resolution, shader variants and hardware. No validated mobile values exist. | Persist/verify URP configuration through QA2-03 (#2/#4), then capture Rendering module, Frame Debugger and available device GPU timings. Art Lead #5 owns visual changes. |
| P07 | **MEDIUM potential / NEEDS DEVICE MEASUREMENT** | `SimulationClock.cs:20–35`; `BattleEntryPoint.cs:76–84` | At 30Hz, simulation may run 0, 1 or several ticks during a rendered frame. `MaxTicksPerFrame=8` prevents unlimited catch-up in one frame, but accumulated `PendingSeconds` is not discarded. Under sustained overload backlog can grow and produce visible stutter. | Log pending seconds, executed ticks per rendered frame and worst tick duration in scripted stress. Preserve fixed-step/replay invariants; no silent tick dropping. |
| P08 | **LOW–MEDIUM potential / SOURCE-CONFIRMED** | `MatchSimulation.cs:287–290`, `:93–110`; `InputCommandQueue.cs:27–54` | `RemoveAll` scans active lists each tick; command-key HashSet retains keys for the lifetime of a match. These are secondary costs relative to pairwise collision work, but should be checked under sustained interaction and restart loops. | Include in CPU/memory capture. No refactor until data justifies it. |

**Gameplay correctness is not a performance optimization opportunity:** the QA2-01 gate/contact bug must be fixed according to ARCH-003 even if the algorithm is more expensive. PERF-002 should measure and recommend safe improvements without weakening its chronological ordering or stable hashes.

### Implications of current level configuration

- [`VS01_Arena.asset`](../../Assets/Swarmforge/Configs/Levels/VS01_Arena.asset) has one raw shot every 10 ticks, movement 340 integer units/tick, and an attack path of 51,000 units. A raw bot needs approximately 150 movement ticks to reach the fortress, ignoring collisions and terminal victory.
- On a continuous ×3 path, this suggests **approximately 45 in-flight multiplied MiniBots** in a steady state (15 original shots in a ~150-tick window ×3); the count can vary near movement/gate boundaries. This is a **mathematical estimate**, not an observed runtime maximum. Normal VS01_Arena may never exercise the visual cap of 120. A dedicated high-density test configuration is therefore mandatory.
- Simulation logic is **not visually capped**. Avoid inferring its workload solely from displayed capsules.

## 3. Proposed provisional performance budgets (not measured / not release approval)

These are **initial triage thresholds to be reviewed with Product Owner and QA**, not hardware guarantees. A 60-FPS frame allows `1000 / 60 = 16.67 ms`; a 30-FPS frame allows `33.33 ms`. Suggested CPU/GPU headroom protects against OS activity, presentation and thermal throttling. CPU and GPU work may overlap; **do not add their independent timings as if they were sequential**.

| Measure | Mid/high target, 60 FPS mode | Entry target, 30 FPS mode | Interpretation |
| --- | --- | --- | --- |
| Frame time (p95) | ≤16.67 ms | ≤33.33 ms | Measured in sustained on-device run, not Editor Game View. Record p50/p95/p99 and >33ms spikes. |
| CPU main-thread active time (p95) | Aim ≤13 ms | Aim ≤28 ms | Headroom target; verify render-thread and present/wait effects before attributing blame. |
| GPU frame time (p95) | Aim ≤13 ms | Aim ≤28 ms | Only if GPU timing supported; otherwise mark `UNAVAILABLE` and use platform GPU tools. |
| Single simulation tick (p95) | Initial aim ≤2 ms | Initial aim ≤4 ms | Separate per-tick profiling metric for 30Hz; compare normal and high-density scenario. |
| Steady-state managed GC allocations | Aim at 0 B/render-frame after warm-up | Same aim | **Diagnostic optimization goal**, not a claim current code meets it. Report B/frame, B/s, and `GC.Collect` pauses. |
| Draw calls / batches, triangles | **Measure baseline first** | **Measure baseline first** | No arbitrary fixed cap; compare same camera/device/quality. |
| RAM / native memory | **Measure baseline first** | **Measure baseline first** | Log total resident/allocated/graphics/managed as available, plus growth over repeated matches; no unverified MB limit. |
| Heat / power | **Measure after 20 min** | **Measure after 20 min** | Watch sustained FPS degradation, thermal warnings/throttling, and battery discharge; no invented temperature guarantee. |

**Promotion rule:** do not convert provisional budgets into an acceptance gate before real measurements on agreed representative devices and an Owner decision. If 60 FPS is not sustainable on a supported class, propose an explicitly approved 30-FPS quality tier; never silently change balance, fixed simulation 30Hz or graphics to make a benchmark pass.

## 4. Reproducible scenario matrix (PROPOSED; not executed)

All simulations use `TickRate=30` and `MatchTicks=1800`. Stress-only overrides are applied to a **temporary copy** of `LevelDefinition` in a separate approved test harness/profile; do **not** overwrite the shipped `VS01_Arena.asset`. Use fixed command sequence and compare `StateHash`, `EventHash`, results and tick counts. A terminal outcome may end a baseline run early; record the terminal tick rather than pretending 1,800 ticks ran.

| Scenario | Exact scripted setup | What it exposes | Expected evidence |
| --- | --- | --- | --- |
| S0 — stock baseline | Unchanged `VS01_Arena.asset`, default `CannonX=-650` (×2); no input; start from clean match, play to terminal. | Typical presentation and simulation. | CPU/GPU/GC, active bot/enemy count, terminal tick, hashes. |
| S1 — stock ×3 | Same stock level; `InputCommand(tick=0,sequence=1,MoveCannon,value=700)`, then no further input. | Increased bot production; deterministic gate policy. | Same metrics and hashes; compare S0. |
| S2 — 120-cap stress | Temporary config: `ShotPeriodTicks=1`, `FortressHP=100000`, `BaseHP=100000`, `EnemyWaves=[]`, input ×3 at tick 0; otherwise same level. Full 0..1800 ticks. | >120 logical MiniBots with no enemies; stable visual cap, first pool expansion, render selection GC/sort, restart/reuse. | Log logical bots, visible bots, pooled objects and CPU/GC at warm-up and after crossing 120. Test run must be **visibly identical across 30/60 render FPS in simulation hashes**, but rendering metrics may differ. |
| S3 — collision density | S2 config plus `EnemyWaves=[EnemyWave(360,100,100)]`; keep very high base/fortress HP to avoid early terminal. Full 0..1800 ticks unless gameplay finishes for another reason. | Cross-pair `B×E` iteration, candidate sort, event growth, impact of DEV-003 ordering. | Per-tick p95/p99, peak B/E, candidate count if instrumented with approved markers, GC, hashes and invariants. |
| S4 — GC / restart | Run S0 ten times through **existing** restart path; observe idle→battle→terminal→restart. | Retained history, old snapshot/event allocations, pool high-water, memory recovery. | Resident/managed memory trajectory, GC timeline, object counts before and after each restart. |
| S5 — lifecycle | During S0: background app for 10 s; foreground, press RESUME; repeat with interrupted screen touch. | Pause/resume clock and catch-up spikes. | 0 simulation ticks while suspended, bounded/cleared backlog after resume, unchanged deterministic replay. |
| S6 — QA2-01 chronology | After approved DEV-003, use exact ARCH-003 cases: enemy-before-gate, gate-before-enemy, exact tie, multiple contacts, same-tick clones; run both 30- and 60-FPS rendering. | Correctness and any extra sort/allocations introduced by chronological event scheduling. | Required regression tests **PASS** plus before/after CPU/GC vs same scenario; no gameplay weakening for speed. |

**Hard correctness checks for all synthetic cases:** no infinite catch-up; no missed/duplicated ticks; hash equality for identical commands independent of render FPS; all conservation assertions hold; events have stable ordering; only 120 visual bots active when `MaxVisibleMiniBots=120`; actual logical bots and damage remain unchanged by visual cap.

## 5. Device compatibility matrix (minimum proposed coverage)

The Studio Owner/QA must name and supply the *actual* devices, SoC/GPU, OS version, RAM, native refresh rate and graphics API. These are **classes to select**, not claims about a tested phone. Run the same scenario IDs, commit and build on each selected device.

| Tier | Android representative | iOS representative | Required focus |
| --- | --- | --- | --- |
| A — entry | Supported entry-range phone, roughly 4 GB RAM, 60-Hz display | Oldest iPhone explicitly included in product's supported-device policy | Sustained 30 FPS, memory pressure, long-run heat, startup spikes. |
| B — mainstream | Midrange Android, roughly 6–8 GB RAM, 60/90/120-Hz display | Supported non-Pro iPhone, 60-Hz display | Primary 60 FPS feasibility, contact stress and GC. |
| C — high-end | Modern high-end Android GPU/SoC | Supported recent Pro iPhone with high-refresh display | GPU ceiling, 60-FPS consistency, optional higher refresh only if Owner approves. |

At least one Android run must use the production-target graphics API, and at least one iOS run must use Metal. Capture actual screen resolution/render scale and URP Renderer settings; do not compare devices that silently use different quality profiles. For every row record thermal state at start and finish.

## 6. Exact Unity/device profiling procedure (PROPOSED; not executed)

1. **Pin source:** record repository URL, exact commit, Git status, Unity Editor `6000.3.28f1`, URP package `17.3.0`, scenario configuration values and command sequence. First establish metrics on this DEV-002.2 baseline; repeat on exact DEV-003 SHA after it exists.
2. **Open & verify:** in a clean Unity workspace import `Assets/`, `Packages/`, `ProjectSettings/`; open `Assets/Swarmforge/Scenes/Battle.unity`. Run `SWARMFORGE → Configure VS-01 project` if required. Confirm URP assigned in *both* Graphics and Quality; log whether pipeline/renderer assets were persisted (QA2-03). Do not substitute a different pipeline without Architect sign-off.
3. **Functional preflight:** Console compilation clear; EditMode and PlayMode existing tests run and XML/screenshots archived; Battle/Win/Restart visible. This is a prerequisite, not performance proof. The current report does not claim to have performed these checks.
4. **Build per device:** create an actual Android or iOS **Development Build** with profiling access; use IL2CPP/ARM64 where appropriate to production. Connect Unity Profiler to the running **Player**, not the Editor. Record scripting backend, architecture, quality tier, render scale, display and target FPS. Also create a matching **non-development build** for frame-pacing cross-check because profiling adds overhead.
5. **Keep runs comparable:** same device brightness (e.g. 50%), battery/power condition, screen orientation, device temperature/ambient room (record, do not assume), background apps and initial level. Avoid comparing plugged/charging and unplugged runs as equivalent. Run in portrait at native screen conditions; record thermal warnings/system throttling where instrumentation permits.
6. **Warm-up:** launch/restart once, allow compilation/initial allocations to settle; record startup/pool-creation burst separately. For each scenario capture at least 60 seconds of representative gameplay, or its actual earlier terminal time. For sustained heat, loop the scenario for **20 minutes** using an approved reproducible automation/manual sequence; sample at minutes 1, 5, 10 and 20.
7. **CPU timeline:** capture `PlayerLoop`, script `Update`, `BattleEntryPoint.Update`, `SimulationClock.Advance`, `MatchSimulation.AdvanceOneTick`, `ResolveContacts`, `PrimitiveBattleView.Render`, `StableVisualSelection.Choose`, `OnGUI`; collect p50/p95/p99 frame times and executed ticks/frame. If method-level names are absent in a Player capture, propose **temporary `ProfilerMarker`s in a separate approved instrumentation PR**. Do not infer exact method time from an uninstrumented sample.
8. **GC & memory:** enable CPU Profiler `GC.Alloc` call stacks on a focused capture (avoid always-on Deep Profiling when timing throughput). Record B/frame, B/s, managed heap, `GC.Collect` frequency/duration; use Memory Profiler snapshots at start, after cap crossing, after 10 restarts and after sustained operation. Do not call source allocations a measured leak.
9. **Render/GPU:** capture Rendering stats (batches, SetPass, triangles/vertices where supported), Frame Debugger of an identical gameplay state, overdraw/shader pass counts as applicable, and GPU frame time only where the device/graphics API supports it. Otherwise attach platform GPU trace and mark Unity GPU timing `UNAVAILABLE` — **not** `0 ms`.
10. **Replay & correctness:** after profiling, run deterministic command-log comparison at 30/60 render FPS and the S6 chronology regression cases. Record event/state hashes and actual pass/fail. During S2/S3 verify the 120 **presentation-only** cap and unchanged logical production/damage.
11. **Thermal/power:** use platform tools for thermal state/throttling and battery drain where accessible; record ambient, brightness, battery level, charging state and OS. Compare FPS first 2 minutes vs final 5 minutes; no guessed Celsius threshold. For a hot device, allow cooldown before next run.
12. **Export evidence:** save `.data`/Profiler captures, test result XML, frame captures, annotated screenshots and table below in an agreed test-artifact location, then submit a results PR/QA handoff. Do **not** commit machine-local secrets, build binaries or oversized profiler captures to this public repository.

### Measurement result template — fill after *actual* profiling

| Field | Captured value |
| --- | --- |
| Source commit / scenario / command log hash | `NOT_TESTED` |
| Unity / URP / Development or Release / scripting backend | `NOT_TESTED` |
| Model / SoC / GPU / OS / RAM / screen / graphics API | `NOT_TESTED` |
| Target FPS; actual p50/p95/p99 frame time (ms) | `NOT_TESTED` |
| CPU main and render thread p95/p99 (ms) | `NOT_TESTED` |
| Simulation tick p95/p99 (ms); ticks per frame; backlog | `NOT_TESTED` |
| GPU p95/p99 (ms or `UNAVAILABLE` with reason) | `NOT_TESTED` |
| Batches / SetPass / triangles / visible bots / logical bots / enemies | `NOT_TESTED` |
| GC B/frame and B/s, collections and pause durations | `NOT_TESTED` |
| Managed / native / resident / graphics memory and 10-restart delta | `NOT_TESTED` |
| 1/5/10/20-minute FPS, thermal state, battery/power conditions | `NOT_TESTED` |
| EditMode/PlayMode counts; regression result; StateHash/EventHash | `NOT_TESTED` |
| Capture paths / evidence / tester / date | `NOT_TESTED` |

## 7. Dependencies and handoff

| Dependency | Blocking for | Owner/decision |
| --- | --- | --- |
| [#2 — ARCH-003](https://github.com/Gogenco/swarmforge-studio/issues/2) approved deterministic intra-tick spec | Correct DEV-003 performance comparison and S6 chronology expectations | Architect + Owner |
| [#4 — DEV-003](https://github.com/Gogenco/swarmforge-studio/issues/4) exact source/PR and test results | Final post-fix performance verdict | Developer |
| [#6 — UI-002](https://github.com/Gogenco/swarmforge-studio/issues/6) portrait HUD work | Stable final UI render/GC measurement | UI/UX |
| QA2-03 persisted URP assets | Reproducible rendering comparisons | Architect / Developer / QA |
| [#5 — ART-002](https://github.com/Gogenco/swarmforge-studio/issues/5) renderer/asset changes | Stable graphics baseline | Art Director |
| [#9 — QA-003](https://github.com/Gogenco/swarmforge-studio/issues/9), real physical devices | Independent performance verification and device gate | QA + Studio Owner |

### Decision requests to Product Owner

1. Approve a **minimum device list** and supported lowest tier before declaring a frame-time pass/fail.
2. Confirm whether 60 FPS mainstream and 30 FPS entry tier are acceptable **provisional** targets (not yet verified).
3. Authorize a **separate, scoped profiling harness/instrumentation PR** only if existing Unity Profiler captures cannot isolate the bottlenecks; preserve simulation logic and all test hashes.
4. Schedule on-device replay/thermal evidence **after DEV-003 and URP corrections**. The QA2-01 correctness blocker remains open until signed off by Owner.

## 8. Review disposition

- **Static review:** `READY_FOR_REVIEW` — code references and test plan prepared, no runtime assertions.
- **Mobile benchmark:** `NOT_STARTED` — no physical-device evidence.
- **Runtime verification of PERF-002 targets:** `BLOCKED_BY_DEVICE_PROFILING_AND_DEV-003`.
- **VS-01 release:** still blocked by QA2-01 and remaining independent QA/Owner gates.
- **VS-02:** not started and **not authorized**.

**No existing scripts, assets, scene, gameplay, input policy, or project settings were edited for this report. No merge is authorized.**

### Reference material

- Source of truth: [GAME-001 / Issue #1](https://github.com/Gogenco/swarmforge-studio/issues/1), [PERF-002 / Issue #8](https://github.com/Gogenco/swarmforge-studio/issues/8), [DEV-002.2 PR #10](https://github.com/Gogenco/swarmforge-studio/pull/10).
- Unity official: [Tracking garbage collection allocations](https://docs.unity3d.com/6000.0/Documentation/Manual/performance-track-garbage-collection.html), [Memory Profiler](https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.memoryprofiler.html), [Profiling tools](https://docs.unity3d.com/2022.2/Documentation/Manual/performance-profiling-tools.html). Documentation is reference for measurement setup; **no Unity tools were executed by this review**.

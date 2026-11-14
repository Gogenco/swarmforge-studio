# PERF-002 — GDD v1.2 Performance Impact Review (DESIGN ONLY)

- **Primary task:** [#8 — PERF-002](https://github.com/Gogenco/swarmforge-studio/issues/8)
- **New Owner direction:** [#18 — GD-006 / GDD v1.2](https://github.com/Gogenco/swarmforge-studio/issues/18)
- **Current performance review:** [Draft PR #16](https://github.com/Gogenco/swarmforge-studio/pull/16), [`PERF002_VS01_PERFORMANCE_REVIEW.md`](PERF002_VS01_PERFORMANCE_REVIEW.md)
- **Code baseline:** merged DEV-002.2 `main` SHA `99cfcdc0b4982e9e441cf23a8669a487a7d75ea9`
- **Status:** `GITHUB-004 HISTORICAL EXPLORATORY REVIEW`; **no runtime measurements or gameplay/Unity changes**

> **Important GITHUB-005 supersession:** Several choices described as undecided below have **since been approved by Owner** in Issue #18. The current canonical decisions, corrected time-only boost math, matched HP2/HP3 scenarios and grouped-wave test matrix are in [PERF-002 GITHUB-005 Prototype Preparation](PERF002_GITHUB005_PROTOTYPE_PERFORMANCE_PLAN.md). Read that document **first**; its approved design constants supersede older speculative assumptions below. Neither report authorizes Unity code changes or claims measured FPS.

## 1. Scope and explicit constraints

At the time of the original GITHUB-004 exploration, Issue #18 approved design direction but had not resolved box HP, hit rules, boost stacking, multi-HP combat, waves, migration or first implementation scope. **This is a historical baseline**; later Owner decisions are consolidated in the linked GITHUB-005 document. Any remaining speculative choices below must **not** be implemented as accepted mechanics.

The baseline currently has a 30-Hz deterministic integer simulation, one-bot/one-enemy mutual-consumption, fixed ×2/×1/×3 lanes, 120 **rendered** MiniBot cap, one initial enemy wave and primitive greybox. Architect [#2](https://github.com/Gogenco/swarmforge-studio/issues/2) / Draft [#14](https://github.com/Gogenco/swarmforge-studio/pull/14) address fixed-gate chronology only; Issue #18 explicitly states **PR #14 is not yet Owner-approved**. New box contacts and multi-hit behavior require an architectural compatibility decision *before* DEV-003 [#4](https://github.com/Gogenco/swarmforge-studio/issues/4) coding. Do not start VS-02 or merge any PR.

This document is an **additive impact note** to PERF-002; it does not supersede the baseline audit, turn provisional budgets into guarantees, or require any change to an existing code file.

## 2. New mechanics → performance impact and dependencies

| Proposed mechanic | CPU / simulation risk | Memory / GPU / UX risk | Decision required before implementation |
| --- | --- | --- | --- |
| Destructible side boxes | Additional time-of-impact/contact candidates; likely `B×X` MiniBot-to-box checks for `B` bots and `X` boxes if naive pairwise. Interleaves with `B×E` bot-enemy checks. Stable hit order and deterministic box destruction/reward grants required. | Box mesh/material/pool, hit/debris FX, target highlighting, readability while one-finger controls remain active. | Whether bots are consumed by box hits or cannon fires a separate projectile; max active boxes, HP, spawn schedule and effects. Designer → Architect → Developer. |
| ×2/×3/×5 rewards | Higher live bot counts; if upgrades stack or boost firing rate, candidate-pair counts can increase sharply. Verify exact order of box destruction vs new spawns, clone IDs and replay hashes. | **120 is only a visual limit**; it does not cap logical units or damage. Many visually hidden robots may make a new upgrade feel unresponsive. | Duration (N shots/time/level), stack rules, boost cap, shot-rate cap, maximum concurrent logical bots; Game Designer/Owner. |
| Multi-hit/armored enemies | Current `EnemyDrone` has no HP and one contact consumes both actors; persistence over multiple hits can raise active enemy count and repeat-contact costs. Must prevent duplicate same-pair damage within an event/tick. | Enemy health displays, armor/hit materials, impact animation and audio voice count. | Enemy types, HP/damage, robot survival and hit cooldown/once-per-contact policy; Designer and Architect. |
| Denser, evolving waves | Simultaneous enemy bursts grow `E`, cause contact-list and allocation spikes, enemy visual pool expansion and event-history growth. | More visible enemies, overdraw, animations, large first-frame prefab creation. | Maximum enemies concurrently alive, batch size per frame/tick, spawn pacing, wave count and lifetime; Designer / Architect / Art. |
| Boss encounter | Potential larger/complex collider and multi-hit state, special event chronology and wave-related triggers. | Larger mesh, impact VFX, post-processing/bloom, draw calls and sound; no boss asset budget approved yet. | Boss HP, attack pattern, visual effects, spawn timing, silhouette/LOD and low-tier fallback; Designer / Art / Sound. |
| Wave alerts, `WAVE n/N`, `BASE UNDER ATTACK!` | Events must not pause or change 30Hz simulation. Avoid per-frame UI layout/string generation and bursts during combat; event state must be deterministic where gameplay-relevant. | Overlay overdraw, text readability/safe areas, audio voices, vibration/thermal/battery; warning must not obscure box/controls. | UI appearance, concurrency cap, warning trigger, cancel/priority rules, accessibility, sound/haptic policy; UI/UX / Sound / QA. |

`B = active logical MiniBots`, `E = active enemies`, `X = active boxes`. Existing contact detection has a source-confirmed **O(B×E)** pair loop (`MatchSimulation.ResolveContacts`, pre-DEV-003); a naive new box pass adds **O(B×X)** checks. Proposed chronological event queues may also require `O(C log C)` sorting for `C` candidates. These are **complexity statements, not measured frame times**. Actual implementation, broad phase, event ordering and correctness are Architect-owned.

### Illustrative generation-rate calculation — assumptions only

In DEV-002.2 the baseline `30Hz / ShotPeriodTicks=10` yields **3 original shots/s**. A bot takes approximately `51000 / (340 × 30) = 5s` of movement to travel the default attack path, ignoring spawn-tick and contact details. Thus a continuous no-contact flow gives rough concurrent-bot estimates:

| Assumed condition (not approved GDD design) | Approximate production | Rough in-flight count during 5s |
| --- | --- | --- |
| Original ×3, 3 raw shots/s | 9 bots/s | ~45 bots |
| New ×5, 3 raw shots/s | 15 bots/s | ~75 bots |
| ×5 and **hypothetical** 2× firing rate (6 raw shots/s) | 30 bots/s | ~150 bots (**over 120 visual cap**) |

These estimates ignore enemies, shortened/extended lifetime, exact tick boundaries, box-hit consumption, level termination, stacking, further multipliers and special enemies. They are **not** a measured cap. If design permits arbitrary stacking or changes shot cadence, GDD must specify a maximum or clearly define a stress upper bound; otherwise a meaningful perf budget cannot be evaluated.

## 3. Design-stage budgets and guardrails (PROPOSED, not Owner-approved)

1. **Preserve the 30-Hz authoritative simulation.** Keep deterministic state/event hashes regardless of 30/60 render FPS, including equal-time box hits, multiple enemy hits and simultaneous wave spawns.
2. **Keep provisional tier targets from PERF-002:** mainstream 60 FPS (`16.67ms/frame`) and entry 30 FPS (`33.33ms/frame`) for agreed device classes; provisional main-thread/GPU p95 headroom `13ms/28ms` respectively. These are *objectives for later profiling*, not measured performance.
3. **Track simulation tick time separately:** provisional mid-tier p95 `≤2ms`/tick and entry p95 `≤4ms`/tick from the baseline review; establish actual B/E/X upper bounds and revise thresholds only with Owner/QA using on-device data.
4. **Max logical bots, enemies, boxes, live VFX and audio voices:** all **TBD BY GDD / ART / SOUND**. The 120 rendered MiniBot limit must never be cited as a logical population limit. New enemy/box visual caps must be designed with gameplay readability, not chosen unilaterally for FPS.
5. **No recurring hot-path allocations as an optimization goal:** profile frame `GC.Alloc` and tick allocations; target zero avoidable steady-state allocations after warm-up where practical, without breaking immutable state history or event correctness.
6. **Wave alert overlap:** proposed 3-second advance warning = about **90 simulation ticks**; proposed 1–1.5-second label = **30–45 ticks** at 30Hz. Show the announcement while the game continues; measure presentation overdraw/CPU and concurrent VFX/audio during a real wave burst. These timings are **draft suggestions from #18**.
7. **Adaptive quality by device (design recommendation):** downgrade cosmetic particle density, shadow/overdraw and optional haptics on lower-tier hardware; do **not** hide actionable boxes/warnings or alter authoritative combat logic. Requires Art/UI/Architect/Owner agreement.
8. **Thermal endurance:** preserve 20-minute device-loop sampling (1/5/10/20min) with 60/30 FPS frame-time p50/p95/p99, GC, memory and thermal state. Do not declare a safe temperature, battery drain or FPS based on static code.

## 4. Additional profiling / QA scenario proposals

All cases **BLOCKED BY APPROVED GDD + IMPLEMENTATION** until mechanics exist. Use a temporary deterministic test harness/level copy with explicit Owner permission, not silent edits to shipped levels. First measure stock DEV-002.2 baseline from PERF-002; then compare identical scenario and hardware across exact new build SHAs.

| Case | Procedure after GDD and code approval | Required evidence |
| --- | --- | --- |
| V12-P01 — ×5 reward only | Fixed timestamp, one box earned, ×5 on defined shots/duration; no enemies in isolated test. | Peak B, visible bots≤approved cap, CPU/GC, hashes, on-screen boost feedback. |
| V12-P02 — ×5 + faster firing | Approved max stack/shot-rate combination for a sustained interval. | Peak B, first pool-expansion spike, stable visual selection, no loss of logical damage, frame p95/p99. |
| V12-P03 — crowded armored wave | Maximum approved concurrent enemies with multi-hit HP and bots in both lanes. | Peak E/B, candidate contact count, collision time, no double-hit, stable chronological replay. |
| V12-P04 — box/contact race | Box hit before enemy, enemy before box, exact same-time, box hit triggers new buff, simultaneous box destroy. | Exact event order/IDs/hashes at 30/60 render FPS, 0 duplicate awards. Architect must define expected results. |
| V12-P05 — boss + alerts + VFX | High-water wave while `WAVE n/N`, boss warning, base alert, box feedback, audio and optional haptics overlap. | UI legibility, draw calls, overdraw, GPU/CPU p95/p99, audio voices, thermal state, no input occlusion. |
| V12-P06 — endurance and restart | Repeat representative 45–60s battle for 20min; 10 consecutive restarts; background/foreground during wave. | Managed/native memory trend, GC pauses, battery/thermal records, catch-up ticks, no retained visual leaks. |
| V12-P07 — three device tiers | Owner/QA-approved entry Android, mainstream Android, supported iPhone, with an optional high-end comparison. | Exact model/OS/GPU/render scale, build SHA, target vs actual FPS, profiler captures, unsupported GPU metrics marked `UNAVAILABLE`. |

**Recommended instrumentation (not approved implementation):** counters for active logical bots/enemies/boxes, visible/pool objects, candidates processed, hits awarded, buff stacks, wave events, VFX/audio concurrency, per-tick CPU cost, per-frame managed allocations, render batches/GPU time where available. Use Unity Profiler and platform tooling. Any production instrumentation or test-harness code must be a separate Architect/Owner-approved PR.

## 5. Required decisions and integration order

| ID | Owner/Designer decision required | Performance dependency |
| --- | --- | --- |
| D1 | Bot consumed on upgrade-box contact **or** separate cannon projectile; simultaneous contact tie semantics. | Defines B×X workload, event queue, clone ordering and test oracle. |
| D2 | Box HP/reward types, max active boxes, destruction VFX, spawn timing. | Bounds box collisions, pooled meshes, draw calls and particle spikes. |
| D3 | Multipliers ×2/×3/×5, stack maximum, duration, firing-rate limit, bot-count policy. | Bounds maximum logical bot density and pressure beyond 120 visuals. |
| D4 | Enemy archetypes, armor/HP/hit survival, max concurrent enemy count and wave burst size. | Bounds B×E, enemy state retention, memory and render spikes. |
| D5 | Boss treatment and simultaneous alert priority, effects/audio quality tiers. | Bounds GPU/overdraw, audio/haptics and gameplay readability under load. |
| D6 | Whether fixed gates are retained/replaced; compatibility of ARCH-003 v1.0 with boxes/multi-hit. | Prevents implementing obsolete DEV-003 logic; requires Architect/Owner decision. |
| D7 | Actual low/mid/high Android and iOS devices, final target FPS and acceptable fallbacks. | Enables real acceptance budgets and test evidence. |

**Sequence:** Game Designer proposes GDD v1.2 → Owner chooses unresolved mechanics/scope → Architect revises/approves event chronology and budget constraints → Developer implements **authorized VS-01 slice only** → Art/UI/Sound deliver within budgets → QA verifies correctness → PERF measures physical-device CPU/GPU/GC/thermal → Owner accepts or requests changes. **No VS-02.**

## 6. Current status and evidence

- **Completed:** Source-backed design impact assessment of #18 and latest #8 Owner comment, with risk/budget proposals and dependencies. No code changed.
- **Pending:** Approved GDD v1.2, final game rules, Architect/Owner chronology decision, implementation, device selection and runtime profiles.
- **Not tested:** all V12-P01..P07 scenarios; no Android/iOS FPS/RAM/thermal claims.
- **Existing PR preserved:** [#16](https://github.com/Gogenco/swarmforge-studio/pull/16) remains Draft and must **not** be merged without separate Owner approval.
- **QA2-01:** remains open. Existing ARCH-003 Draft PR #14 is not Owner-approved per #18. No VS-02 work.

**Handoff requested:** Game Designer / Owner to respond to D1–D7 in GDD v1.2; Architect to establish deterministic box/multi-hit event semantics; QA/Owner to nominate physical target devices. Until then PERF's new-mechanic performance verdict is `BLOCKED_BY_GDD_AND_DEVICE_EVIDENCE`.

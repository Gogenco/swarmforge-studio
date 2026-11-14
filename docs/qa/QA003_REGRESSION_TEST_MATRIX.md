# QA-003 — VS-01 independent regression test matrix (PREPARATION ONLY)

**Owner:** 09 — Senior Manual & Automation QA Engineer  
**Issue:** [#9](https://github.com/Gogenco/swarmforge-studio/issues/9)  
**Master gate:** [#1](https://github.com/Gogenco/swarmforge-studio/issues/1)  
**Baseline:** DEV-002.2 on `main`, merge commit `99cfcdc0b4982e9e441cf23a8669a487a7d75ea9` ([PR #10](https://github.com/Gogenco/swarmforge-studio/pull/10)).  
**State:** PREPARED / NOT EXECUTED. Do not interpret this document as QA PASS or VS-01 sign-off.

## Preconditions and dependencies

1. [ARCH-003 / #2](https://github.com/Gogenco/swarmforge-studio/issues/2): signed, versioned definition of chronological sub-tick ordering, event tie policy, same-tick clone behavior, numeric overflow policy and event hash semantics.
2. Owner approves ARCH-003 before implementation.
3. [DEV-003 / #4](https://github.com/Gogenco/swarmforge-studio/issues/4): separate scoped PR implements the approved algorithm and regression tests.
4. Relevant UI-002 / [#6](https://github.com/Gogenco/swarmforge-studio/issues/6) and URP PRs available before visual/configuration verification.
5. Unity 6000.3.28f1 and a playable Android/iOS build, where relevant. Record separately if unavailable.

**Comparison rule:** independent QA reviews a precise DEV-003 commit SHA against the corresponding approved ARCH-003 version/SHA. Unmerged source reviews are not runtime passes. Do not assume ARCH-003 tie semantics before sign-off.

## A. Intra-tick chronology — CRITICAL

| ID | Scenario / fixture | Assertions and evidence | Required mode |
|---|---|---|---|
| QA3-T01 | **Enemy contact before gate.** One MiniBot and one same-lane enemy traverse the tick; their swept contact time is earlier than the MiniBot's crossing of GatePosition. A candidate stress fixture uses gate=9000, bot speed=10000/tick, enemy speed=9000/tick, radius=25, path length=10000, spawn tick=0, movement tick=1 (validate with ARCH-003 before freezing). | Enemy and original bot consumed once; **no gate event or clone** from a bot already destroyed; no phantom fortress hit; event order chronological. | EditMode, required |
| QA3-T02 | Gate crossing strictly **before** enemy contact. | Multiplication fires once at actual crossing; all surviving original/child events follow ARCH-003 same-tick participation rules; expected totals and IDs deterministic. | EditMode, required |
| QA3-T03 | Gate crossing and enemy contact at **exact equal sub-tick time**. | Apply the exact ARCH-003 tie policy (do not invent it here). Repeat run produces identical event sequence and hashes. | EditMode, required |
| QA3-T04 | Gate and multiple contacts within same tick; originals and newly created clones. | Process all eligible remaining-time events; no missed hit, stale contact, duplicated clone, or double damage. | EditMode, required |
| QA3-T05 | Two+ enemies contest same MiniBot at equal time; repeated test with reversed container/insertion order where permitted. | Stable ID-based tie ordering as ARCH-003 defines; exactly one death/consumption per entity; identical accepted logs/hashes. | EditMode, required |
| QA3-T06 | High-speed bodies cross with no end-of-tick overlap. | Swept collision detects eligible contact; no tunneling or spurious collision across lanes. | EditMode, required |
| QA3-T07 | MiniBot reaches gate, is moved through subsequent ticks, and restarts match. | At-most-once multiplication per MiniBot per match; reset has fresh logical state/IDs. | EditMode, required |
| QA3-T08 | Contact, fortress impact and base breach in one tick. | Exact chronological resolution, at-most-once damage, terminal precedence consistent with approved rules; a tied base+fortress death follows approved loss rule. | EditMode, required |
| QA3-T09 | Extreme speed, zero relative speed, zero-distance contact, boundary coordinates and overflow-prone numeric values **within validated config bounds**. | No exception/overflow/undefined tie; stable ordering and conservation counters. Avoid introducing invalid game balance. | EditMode, required |

## B. Deterministic simulation and replay — CRITICAL/HIGH

| ID | Scenario | Assertions | Required mode |
|---|---|---|---|
| QA3-D01 | 30-Hz fixed simulation with identical command log rendered at 30/60 FPS and irregular frame deltas. | Same ticks, state hash, event hash, command hash, ID sequence and terminal outcome. | EditMode |
| QA3-D02 | Identical commands submitted in varied permissible arrival order. | Canonical command order and hash stable; duplicate or stale commands rejected. | EditMode |
| QA3-D03 | Re-run identical seeded/defined scenario 100 times including same-tick collisions. | Byte-for-byte identical serialized logical event order and replay hashes, with no reliance on Unity physics callbacks. | EditMode |
| QA3-D04 | Compare `RawShots`, `ProducedTotal`, gate extras, consumed units, active units across 1800 ticks. | Exactly 180 raw shots at 0..1790 under baseline; count conservation and stable IDs, subject to approved level config. | EditMode |
| QA3-D05 | Pause clock, resume and restart through scene. | No simulation steps while paused; no catch-up burst violating policy; restart clears state and terminal latch. | EditMode + PlayMode |
| QA3-D06 | Full old 24-case EditMode suite plus all new DEV-003 cases. | Record **actual** Test Runner totals/results and failing method; never substitute source enumeration for execution. | Unity Test Runner |

## C. PlayMode, mobile UI and URP — HIGH/MEDIUM

| ID | Scenario | Assertions | Required mode |
|---|---|---|---|
| QA3-P01 | Battle.unity opens with exact Unity/package versions; original PlayMode tests. | No compilation errors, missing scripts or purple/magenta material; retain original smoke behavior. | Unity Editor + PlayMode |
| QA3-P02 | Victory, Defeat, simultaneous destruction, timeout, Restart. | Correct terminal overlays, no HUD overlap or unwanted repeated terminal event; button works with mouse/touch. | Unity PlayMode + device |
| QA3-P03 | 320x568, 360x640, 390x844, 480x853 portrait, with notch/safe-area emulation and real device where possible. | Text/button legibility, no clipping or occlusion; multiplier ×1/×2/×3 labels readable; compare against UI-002 sign-off criteria. | Screenshots + device |
| QA3-P04 | Android app background/foreground, orientation/resizing, notification interruption. | Pause policy honored, no time jump or duplicate input; restart resumes cleanly. | Android device |
| QA3-P05 | iOS app background/foreground and safe areas. | Same lifecycle/legibility expectations. Report NOT TESTED if iOS build/device unavailable. | iOS device |
| QA3-P06 | Clean checkout of exact PR commit on a fresh Unity project import. | URP renderer/pipeline assets **tracked and referenced**; Graphics/Quality settings resolve cleanly without a one-off Editor state; no magenta. | Clean Unity import |
| QA3-P07 | Device stability under repeated 60-second matches and restarts. | Log crashes, exceptions, FPS, stalls, GC allocations and memory/thermal observations; coordinate with Performance #8. | Android/iOS device |

## D. Repeat known QA-002 findings

- `BUG-01` pink stripe; `BUG-02` HUD scale; `BUG-03` VICTORY overlay.
- `QA-01` visual gates; `QA-02` URP; `QA-03` replay hash; `QA-04` immutable event history; `QA-05` swept contacts; `QA-06` stable MiniBot visuals; `QA-07` pause.
- `QA2-01` HIGH chronology; `QA2-02` MEDIUM text readability; `QA2-03` MEDIUM persisted URP.

For each report `FIX VERIFIED / STILL PRESENT / PARTIALLY FIXED / NOT TESTED`. A static source-level fix **never** silently converts to a runtime fix.

## Evidence contract / release gate

Capture and link:

1. Exact GitHub Issue, PR, base/head commit SHA, ARCH-003 version/SHA.
2. Changed-file diff and explicit in-scope/out-of-scope analysis.
3. Static checks (exit codes, Unity GUID and asmdef references, config changes).
4. Unity version, platform, build settings, Test Runner EditMode/PlayMode XML, error counts and Console traces.
5. For each reproduction: setup, input commands, step timeline, expected/actual event order, stable Unit IDs, snapshots, hashes and screenshots/logs.
6. Android/iOS device model, OS, resolution, orientation, APK/IPA build commit and pause/resume evidence where tested.
7. PASS/FAIL/NOT TESTED for every case, defect severity, exact file, owner and proposed fix.

**QA-003 final report statuses:** `PASS`, `BLOCKED`, or `STATIC_ONLY`. **VS-01 approval belongs only to Owner.** No self-merge, no VS-02, no changes to Developer/Architect code under this preparation task.

## Current execution record

- GitHub baseline PR #10: **MERGED**, `99cfcdc0b4982e9e441cf23a8669a487a7d75ea9` (verified by GitHub API).
- ARCH-003 #2: **IN_PROGRESS / not signed** at time of preparing this matrix.
- DEV-003 #4: **BLOCKED_BY_ARCH-003**.
- QA-003 runtime runs for DEV-003: **0**.
- This document: **test planning only**, not evidence of an executed test.

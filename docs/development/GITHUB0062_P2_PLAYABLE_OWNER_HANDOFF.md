# GITHUB-006.2 — P2 isolated playable trial candidate

**Status:** SOURCE CANDIDATE / UNITY VALIDATION REQUIRED. A local environment without Unity Editor or C# toolchain cannot certify a compiled or playable build. The Owner must run the newly added scene and tests before labeling P2 READY.

## Owner P2 fire-rate change (2026-11-15) — approval applied; balance decision required

- Owner approved **×1 = one MiniBot per second** and **active ×2 = two MiniBots per second**, preserving 30 simulation ticks per second, all movement/combat/wave timings, the first shot at tick 0, the 5HP ×2 box and 360-tick reward duration. Both authored P2 balance assets (`P2_HP2.asset`, `P2_HP3.asset`) and the P2-only fallback `V12P2Presets.Create()` now set `ShotPeriodTicks = 30`. This is a frequency change, **not** a change to simulation speed. The P1 three-box default (`ShotPeriodTicks = 10`) and VS-01 remain untouched.
- The existing `V12TrialSimulation.Fire()` multiplies the **number of bots per scheduled raw shot** by `boosts.ActiveMultiplier`. No change to its authoritative event clock, geometry, simultaneous combat, stable IDs, visual separation, HP2/HP3 mechanics, replay, or boost ledger was needed.
- EditMode regressions cover both HP variants, every raw shot through tick 90 at ×1, **12 boosted shot opportunities** (24 bots), 360-tick duration/expiry, transition back to ×1, fresh simulation restart and deterministic hashes, loss by base, loss by timeout, test-fixture victory, and the optimistic maximum-damage bound. Existing tests and scene checks are retained. Run static check: `node tools/validate-v12-p2-fire-rate.mjs`.
- **BALANCE BLOCKED — OWNER DECISION NEEDED:** Current P2 fortress has **65 HP** and the match is **1800 ticks / 60 seconds**. A bot needs at least **150 ticks** to reach the fortress (attack length 51000, speed 340); at most **55 scheduled shots** can arrive before timeout (first at 0, last eligible at 1620). The single ×2 reward can add at most **12 additional bots**, and at least **5 bots are sacrificed** to break its box. Therefore even assuming every remaining bot survives combat, **maximum fortress damage ≤ 55 + 12 − 5 = 62 < 65**. This is an optimistic upper bound; actual battle casualties reduce it further. **Canonical P2 victory is mathematically unreachable at this new cadence.** Do not silently lower Owner-approved fortress HP. Proposed experimental value: **FortressHP = 40** in both P2 assets and fallback preset, *only if Owner explicitly approves a balance change*. That would leave an optimistic margin of 22 bots for enemy combat; tuning still requires playtests. Alternatively Owner can authorize more match time, multiple reward boxes, or another damage mechanic. The old victory test with `FortressHP = 1` proves the terminal victory transition only, **not canonical balance**.
- The previous Owner Unity observation **EditMode 54/54 PASS, PlayMode 5/5 PASS, zero Console errors/warnings** was on the **earlier P2 HEAD before this fire-rate change**. Those results cannot certify the new HEAD. Reopen the updated ZIP in Unity 6000.3.28f1 and rerun all tests + real 60s gameplay for **both** HP2/HP3. Confirm one bot every second normally; exactly two together during ×2; no overlap (formation fix preserved); first shot at the start, boost onset after fifth box hit, expiration after 12 seconds, unchanged grouped enemies, base-defeat, timeout and restart. Record the canonical win balancing issue separately; do not mark P2 final READY until resolved.

## Owner P2 Safe Mode recovery (2026-11-15)

- Owner verified three `CS1503` errors on the previous P2 HEAD `ef1631189b645fb8c1fc69467a1812b8954687ef`: `V12TrialSimulation.cs` lines 603, 621, 622 passed `ulong` values to the `long`-only hash function.
- Added a `Hash(ref ulong, ulong)` overload retaining all eight canonical little-endian bytes, and directed the original `Hash(ref ulong, long)` through the same unchecked bit-preserving conversion. This changes no P2 gameplay decisions, random ordering, Unity packages, or scene assets.
- Added three EditMode regressions for a high-bit unsigned value, negative signed sentinel compatibility, and state/event/command hash integration; plus reproducible static guard `node tools/validate-v12-p2-hash-recovery.mjs`.
- **This source repair has not yet been compiled or run in Unity by the developer.** Owner should reopen the fresh exact-HEAD archive, confirm the 3 CS1503 errors are gone (and check for newly revealed errors), then run the complete EditMode and PlayMode suites and gameplay smoke before P2 acceptance.

## Owner P2 visual formation repair (2026-11-15)

- Owner has now independently run `V12_Trial.unity` in Unity 6000.3.28f1 and reported EditMode **50/50 PASS**, PlayMode **5/5 PASS**, no Safe Mode, a visibly broken ×2 box, `BOOST ×2 8s`, and a grouped wave. This evidence validates those observed parts of P2 only; do not extrapolate to all C1–C6/queues/devices.
- Owner found two green ×2-spawned MiniBots visually merged and same-lane red enemies overlapping. The cause was presentation-only X jitter of ±0.10 world units, smaller than the sphere/capsule diameters.
- `V12TrialView.ShowUnits()` now obtains an immutable-ID-derived formation offset from `V12FormationLayout.ForId()`: X ±0.52 and Z ±0.45 world units (four distinct positions; neighboring IDs separated at least 0.90). This is **only applied after reading the snapshot**. Same-tick ×2 copies and same-lane paired enemies retain exact original X/Z in the simulation, while appearing as separate models on screen.
- Added four Unity EditMode regression tests for adjacent-ID visibility, four-slot bounds, replay/frame order stability, and unchanged authoritative state/event/command hashes; plus standalone static guard `node tools/validate-v12-p2-visual-spacing.mjs`.
- **Required Owner retest:** open the new exact-HEAD ZIP, run `V12_Trial.unity` in 9:16, observe two visibly separate green bots per ×2 shot and two distinct red enemies per lane, compare screenshot before/after, rerun EditMode/PlayMode. Verify the narrow portrait arena, box and HUD remain visible. Screenshots/manual result are pending; no declaration that visual QA PASS already happened.

Canonical dependencies: [Owner #18](https://github.com/Gogenco/swarmforge-studio/issues/18), [Developer #4](https://github.com/Gogenco/swarmforge-studio/issues/4), [P1 #24](https://github.com/Gogenco/swarmforge-studio/pull/24), [P2 Draft #26](https://github.com/Gogenco/swarmforge-studio/pull/26).

## Unity Editor — opening P2

1. Use **Unity 6000.3.28f1**. Open the extracted ZIP project folder containing `Assets`, `Packages`, `ProjectSettings`; wait for asset import. Keep checked-in Unity/URP versions unchanged.
2. In **Project**, double-click `Assets/Swarmforge/Scenes/V12_Trial.unity` — **not** `Battle.unity`.
3. In Game view choose a **portrait 9:16** aspect (phone/tablet first). Press **Play**.
4. Hold the mouse button / drag a finger in the **bottom 28% of the visible arena**. Aim **left** at `×2 HP 5`. At normal speed the cannon produces **one MiniBot per second**. Five bots must strike and destroy the box. The HUD shows box HP and a pending ×2 reward; the **next simulation tick** starts ×2 autofire producing **two MiniBots per second for 12 seconds**. The two green robots should be visually separated.
5. Move left/right to defend against the group of four red enemy MiniBots, which spawns together after **6 seconds**. Shots are automatic; the cannon stays at the last aimed position. The enemy fortress is at the far end.
6. The two buttons `HP2` and `HP3` switch editable presets and start a fresh trial; `RESTART` starts a fresh trial with the selected HP profile. Victory is fortress HP zero; defeat is base HP zero or expiry at 60 seconds.

**Important:** old VS-01 is still available via `Assets/Swarmforge/Scenes/Battle.unity`; it remains the first scene in Build Settings. Opening it will **not** show P2 mechanics. P2 is the second scene in Build Settings.

## What is implemented only for P2

| Area | P2 trial |
| --- | --- |
| Unity scene | Separate `V12_Trial.unity` entrypoint, independent presentation and camera. |
| Reward box | Single left-lane `×2` box, **5 HP**, consumes a bot per hit. |
| Power | ×2 copies per raw shot, 12s = 360 ticks at 30Hz; starts tick after award, expires on an exclusive boundary. |
| Capacity | At most one active and one pending, with terminal reward reservation before dispatch; no capacity overflow. |
| Balance | Two Inspector-editable `P2_HP2.asset` and `P2_HP3.asset` differing in MiniBot HP 2/3; robot HP not claimed final. |
| Enemies | One group at tick180: two on left and two on right, same tick. |
| Combat | No physics authority; fixed coordinates, exact within-tick rational time, combat-first same-time order, pre-batch simultaneous damage, enemy single retaliation, stable IDs and cooldown. |
| End state | Fortress 0 → victory, base 0 → defeat, 1800 ticks → timeout, restart/reset and pause/resume. |
| Replay | Event/command/state 64-bit hashes, deterministic ordered commands. |

## QA verification that must be performed in Unity

1. Unity Test Runner → **EditMode** → Run All. Include `V12P2PlayableTests` and previously existing EditMode suite. Record pass/fail and Console errors.
2. Test Runner → **PlayMode** → Run All. Include `V12TrialSceneSmokeTests` (three scene-load/preset/pause tests) and existing VS-01 PlayMode suite.
3. Run new `V12_Trial.unity`: observe box HP 5→0, boost ×2 for exactly 360 ticks, wave four enemy instances simultaneously, both terminal states, restart, and `HP2`/`HP3` toggle. Observe Console for exceptions and the already known separate URP asset-import `Host type is not matching any asset type` issue.
4. Reopen `Battle.unity` to independently verify it still works, using previous owner baseline 35/35 (33 EditMode + 2 PlayMode) as reference, **not as a claim about this new branch**.
5. Any Unity compilation failure, assertion failure, unexpected outcome or broken rendering makes P2 **BLOCKED** until corrected. Attach raw Test Runner XML and Editor.log to [QA #9](https://github.com/Gogenco/swarmforge-studio/issues/9).

## Scope and safety

- No modifications to the existing `Battle.unity`, `BattleEntryPoint`, `MatchSimulation`, VS-01 gates or P1 three-box trial assets. P2 is opt-in.
- No changes to Unity/package versions, no `main` changes, no merge, no VS-02.
- The environment preparing these sources **did not have Unity Editor, the Unity Test Runner or a C# compiler**. Assertions are authored but are not attested as PASS before Owner/QA executes them.
- Tunable P2 profile parameters include fortress/base HP, MiniBot damage/HP and cooldown, shot interval, box position/radius/HP, scout HP/speed/cooldown and wave tick/count. Values are initial test hypotheses; HP2/HP3 and combat balancing may change after observed testing.

# SWARMFORGE — Stage P2 first isolated implementation slice

**Status: P2 DRAFT IN PROGRESS, NOT PLAYABLE / UNITY NOT RUN.** This is only the exact-rational event clock and canonical phase sorting prerequisite from Director GITHUB-006.1. The separate GITHUB-006.2 URP importer issue is **not fixed** by this slice. Do not merge, do not declare gameplay or P2 pass.

## Authorization rechecked (2026-11-14)

- Owner Director C1–C6 [Issue #18](https://github.com/Gogenco/swarmforge-studio/issues/18#issuecomment-6880765413).
- Designer GDD v1.2 Draft #19 exact `b70da4e478b7a4b6dc54a403b130a20facf4e62f` and Architect Draft #23 exact `95a900e15d1eaabd60f4fa75fe3a97cc076c2d2b`: Architect signed `ARCH_V12_EXACT_HEAD_ALIGNED`.
- Owner's real P1 Unity Test Runner XML: EditMode 33/33 PASS (9 P1 tests), PlayMode 2/2 PASS = 35/35, at Unity 6000.3.28f1. XML identifies P1 branch project name, not a cryptographically proven exact SHA.
- This branch is **stacked on** P1 Draft #24, so legacy main and P1 WIP remain immutable. No existing files changed.

## Exactly implemented

- `V12TickTime`: canonical integer tick + reduced rational sub-tick, exact cross-multiplied ordering with checked overflow. Strike at `100+1/4` plus 15 ticks ⇒ next eligibility **`115+1/4`** (not 116).
- `V12ContactEventKey`: exact `(time, phase, targetId, actorId)` sorting with combat before box at equal time. This is **NOT** an implementation of swept collision detection, HP exchange or box damage.
- Four additional EditMode tests for fractional cooldown, equal fractions, canonical same-time phase/ID order and invalid/overflow rejection; **not yet run in Unity**.

## Remaining P2 work in strictly approved order

1. Implement new opt-in 60s **one ×2 HP5 side box** trial config (keep existing P1 HP2/HP3 paired presets; no hard-coded HP). Extend replay config/fingerprint before gameplay use.
2. Create immutable 2D fixed-point bot/enemy trajectories and swept AABB TOI; validated integer bounds prevent rational cross-product overflow. Spawn one simultaneous enemy group with stable IDs, future spawns immobile until next tick.
3. Implement event-bucket combat C1/C2 (many allies → one enemy, **one** enemy retaliation per cooldown, pre-batch HP snapshot), exact positive repeat C3 and survivor route resumption next tick.
4. Implement C4 combat-first then surviving reserved box impact/consumption at equal TOI, dead-bot no-box hit. C5 pre-dispatch terminal entitlement, lock with zero-spend shot suppression, next-tick ×2 activation for 360 ticks, half-open expiry, no stack.
5. Implement base/fortress/timeout/restart; minimal separate opt-in arena/entry point, one-thumb automatic firing, HUD hooks, replay and deterministic event hashes.
6. Add exact Architect test oracle rows, same commands/list permutations/30-vs-60 render FPS/HP2-vs-HP3 comparative fixtures, legacy non-regression. Unity Editor, PlayMode, URP visuals, and actual build/performance must be tested before playable signoff.

**Safety:** current P2 work is only an additive deterministic primitive with no runtime scene connection. Six Owner URP package-import errors remain open. No ×3/×5 production rollout, bosses, 5-wave rollout, Forge, VS-02, package upgrade, main changes or merge.

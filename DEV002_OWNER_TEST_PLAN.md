# SWARMFORGE / DEV-002 — QA handoff and bug verification

**Task ID:** GAME-001/DEV-002  
**Status:** `DEV-002_READY_FOR_OWNER_TESTING` — source/static gate only; Editor acceptance pending.

## Important evidence boundaries

**Confirmed by Owner for original DEV-001 only:** Unity 6000.3.28f1 compilation PASS, EditMode 12/12 PASS, PlayMode 1/1 PASS, gameplay/fortress destruction/VICTORY/restart PASS.

**DEV-002 new version:** no Unity Editor or compiler was accessible to the developer. Never record a new green Unity test result without running this ZIP in Unity. No phone/device FPS benchmark was run.

## Reproduction and acceptance in Unity 6000.3.28f1

1. Open *inner* `SWARMFORGE` folder. Wait for package import. Select menu **SWARMFORGE → Configure VS-01 project (URP + portrait + battle scene)**. Inspect Graphics and Quality for URP pipeline. Open `Battle.unity`. If Console has red C# messages, export the exact first error and stop.
2. Press Play in portrait Game View. `BUG-01`: no large magenta surface/stripe; the three strips ×2/×1/×3 and all primitives must render normally. Capture screenshot.
3. `BUG-02`: inspect Game View 9:16, 19.5:9, 20:9; also toggle an Editor Game View with a compact viewport. HUD title, HP/time/shots and footer must occupy separate rows; no overlap or clipped text. Capture screenshots and aspect ratios.
4. `BUG-03`: wait for fortress destruction or adjust *copy* of test level's `FortressHP` to 1 to force victory. Screen must show one isolated result card (no background HUD) and RESTART. Use RESTART and confirm counters start fresh.
5. `QA-01`: drag lower screen left, then center, then right, starting a new match for each. Use tests `GateThresholds_AgreeWithBotLaneAndMultiplier` to verify exact -120/-119/119/120 thresholds. Bot geometry should start under the matching gate and visually converge toward attack route.
6. `QA-02`: verify `SWARMFORGE_Renderer.asset` and `SWARMFORGE_URP.asset` actually appeared under `Configs/Tuning` and pipeline is assigned. If magenta persists, capture Console errors and Graphics/Quality screenshots; do not claim shader bug fixed solely based on passing C# tests.
7. `QA-03`: run `ReplayHash_RecordsPerUnitSpawnXEvenWhenCannonReturnsToSamePosition` and existing `SameCommandLog_ThirtyAndSixtyRenderFPS_HashesMatch`.
8. `QA-04`: run `LastEventBatch_RemainsImmutableAfterLaterTicks_AndHistoryIsMatchScoped`.
9. `QA-05`: run `SweptContact_EliminatesFastCrossingInsteadOfAllowingTunneling`, then normal enemy contact test; ensure no duplicate kills/damage.
10. `QA-06`: run `VisualCap_120To121PreservesExistingSelectedIds`. Visually test high counts and inspect persistent meshes as cap is exceeded.
11. `QA-07`: during Play, switch focus away/from Editor or minimize a mobile build, return; the game should display **PAUSED**, remain at the same tick until **RESUME**, then continue. Run the added `PauseAndExplicitResume_DoNotAdvanceBackgroundSimulation` PlayMode test.
12. Re-run **all** EditMode and PlayMode tests; record totals/results. The previous **12/12 + 1/1** are *not* the expected new totals because DEV-002 adds tests. Verify terminal tie, last tick, 180 raw spawn,×2/×3, restart, and 30/60 FPS replay again.

## Potential remaining risks (not falsely marked fixed)

- **Unverified URP factory API in exact installed Unity Editor:** the `UniversalRenderPipelineAsset.Create(renderer)` Unity API must compile and produce working assets on the Owner machine.
- **Graphics smoke and mobile safe areas:** cannot be proven from static source without rendering screenshots and on-device checks.
- **Test geometry only:** exact 3D contacts/waypoints for future levels are a Game Designer + Game Architect dependency; VS-01 runs on current two enemy lanes and bypass.
- **Performance:** sticky selection avoids visual cap thrash, but sustained FPS and temperature on Android/iOS remain unmeasured.
- **Unity input mode:** this VS-01 uses legacy `Input`; set Active Input Handling appropriately if new Input System-only project configuration triggers a runtime error.

## Scope and file-level changes

| Area | Files |
| --- | --- |
| Pure logic / config | `LevelDefinition.cs`, `MatchState.cs`, `MatchSimulation.cs`, `MatchSnapshot.cs` |
| Presentation | `PrimitiveBattleView.cs`, `BattleEntryPoint.cs`, **new** `StableVisualSelection.cs` |
| Unity setup | `SwarmforgeProjectSetup.cs`, `Swarmforge.Editor.asmdef` |
| Regression tests | `Vs01SimulationTests.cs`, `Swarmforge.EditModeTests.asmdef`, `Vs01SceneSmokeTests.cs` |
| Static validation / docs | `tools/validate-project.mjs`, `README.md`, `DEV002_OWNER_TEST_PLAN.md`, generated `.meta` for new C# |

**Unchanged by design:** scene `Battle.unity`, serialized config values `VS01_Arena.asset`, clock frequency/tick contract, inventory/money/Forge/Mech/Titan, original DEV-001 source/ZIP.

**Next required action:** Owner unzips this deliverable, opens new project in Unity 6000.3.28f1, captures new EditMode/PlayMode totals and portrait/VICTORY screenshots, and returns any console errors to DEV/Architect. Only then QA can mark `VERIFIED`.

# SWARMFORGE P8 → Stage 2 3D integration (Game Developer)

This change is **presentation only**, on the existing `dev/v12-p2-rational-event-clock` Draft PR #26.
The Art Director's seven real OBJ models and Unity Editor importer remain intact.
The legacy VS-01 `Battle.unity` is unaffected. **UNITY NOT RUN by Developer.**

## One-time Unity Editor setup

1. Open the full GitHub branch archive in Unity 6000.3.28f1 (URP). Wait for compilation.
2. Select **SWARMFORGE > Stage 2 > Generate and Integrate P8 Art**. This calls the Art Director's OBJ → native Mesh/Material/Prefab generator, then creates/updates the `Assets/Swarmforge/Resources/SwarmforgeStage2ArtCatalog.asset` referencing all seven generated prefabs.
3. Confirm `Assets/Swarmforge/Art/Stage2/Generated/Prefabs/` has seven real prefabs. Confirm the Resources catalog exists. Generated files are created on the Owner's machine and are **not included as prebuilt native binary Unity assets** in the GitHub ZIP; the original authored polygonal OBJ geometry is included.
4. Open `Assets/Swarmforge/Scenes/V12_Trial.unity`, Game view **9:16**, Play. Native 3D should now appear (no new scene changes necessary because `V12TrialView` automatically resolves the Resources catalog at start).
5. Run complete EditMode + PlayMode suites; inspect Unity Console. P8 prior Owner target **86 EditMode + 8 PlayMode** is historical for the earlier version, NOT verified on this HEAD. Two new scene/catalog tests require the generation command first.
6. Test HP2/HP3, center lane red contacts, x2 chest off left, 1/s → 2/s sequential x2 (8 tick gap and same X) → 1/s, fortress 40 HP and victory/defeat/restart, portrait labels and old `Battle.unity`. Record screenshots and failed assertions if any.

## Binding and layout

Seven prefab roles: `S2_FriendlyMiniBot` (green models pooled at true simulation X/Z, foot Y=0.045 and scale .42), `S2_RedEnemyRobot` (red pooled models with stable ID-based visual-only offsets, scale .48), `S2_AutoCannon` (scale .65, barrel end aligned to authoritative Z=0), `S2_X2Chest` (single real chest at X=-950 simulation units), `S2_PlayerBase`, `S2_EnemyFortress` (HP0 flattened), `S2_Arena` (one floor + lanes + borders; old graybox floor removed while native art is present).

No colliders or rigidbodies added. P8 simulation coordinates and event order are unchanged. Asset materials are shared; pooling is still used for green/red. If the catalog has not yet been generated or is incomplete, original P8 graybox presentation remains as fallback; this is **not** proof that Stage 2 visual acceptance passed.

No GitHub Actions, Unity package updates, new PR, main, merge, Stage 3 or VS-02 changes.

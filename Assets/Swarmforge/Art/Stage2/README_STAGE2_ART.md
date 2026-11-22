# SWARMFORGE — Stage 2 P8 real 3D art handoff

**Owner approval:** [Art Issue #5 / comment 7085178678](https://github.com/Gogenco/swarmforge-studio/issues/5#issuecomment-7085178678)  
**Working branch / Draft PR:** [PR #26](https://github.com/Gogenco/swarmforge-studio/pull/26), `dev/v12-p2-rational-event-clock`  
**Scene:** `Assets/Swarmforge/Scenes/V12_Trial.unity`  
**Unity:** 6000.3.28f1 / portrait 9:16  
**Status:** REAL 3D MESH SOURCES DELIVERED; EDITOR PREFAB GENERATOR ADDED; **NOT YET INTEGRATED INTO P8 RUNTIME VIEW**. **UNITY NOT RUN** by Art Director.

## What is actually in GitHub

These are **real authored polygonal Wavefront OBJ files**, with 3D vertex coordinates, polygon faces and named color-material assignments, **not screenshots, 2D mockups or a docs-only concept**:

| File in `Models/` | Purpose | Native source geometry |
| --- | --- | --- |
| `S2_FriendlyMiniBot.obj` | Green robotic body, head, turquoise visor, antenna, arms and legs | 320 triangles |
| `S2_RedEnemyRobot.obj` | Red enemy with separate angular silhouette, shoulders, claws and amber visor | 252 triangles |
| `S2_AutoCannon.obj` | Teal/steel turret foundation, rounded neck, barrel and distinct muzzle | 432 triangles |
| `S2_X2Chest.obj` | Gold box with raised **3D X2 glyph** on the top, seams and latch | 324 triangles |
| `S2_PlayerBase.obj` | Cyan/blue base with pillar shapes and central shield core | 140 triangles |
| `S2_EnemyFortress.obj` | Red wall, towers, battlements and glowing central gate/core | 232 triangles |
| `S2_Arena.obj` | Dark 8.4-unit arena, service lanes, dashed lines, boundary posts and opposing thresholds | 900 triangles |

`Models/SWARMFORGE_P8.mtl` contains authored RGB material definitions for Blender/OBJ viewers. Models use **+Y up, +Z forward** and meters-like Unity units; there are no colliders or gameplay scripts.

The **Editor-only** tool `Editor/Stage2ArtBuilder.cs` reads actual OBJ faces, combines submeshes **by material** for reduced renderers, and creates **native Unity Mesh .asset**, URP **Material .mat**, and **Prefab .prefab** for each of the seven models under `Generated/`. It does not edit gameplay or the active scene, and it does not run during gameplay.

## Open in Unity / generate real native assets (Owner or Developer)

1. Download the **current PR #26 branch ZIP**, extract into a **new folder** and open that project in **Unity 6000.3.28f1**. Preserve the previously approved P8 trial folder unchanged.
2. Wait for Unity package import. Check Console; if there are Editor compilation or import errors, **STOP**, capture exact errors and report them. Do not call this a PASS.
3. In the Unity top menu choose **SWARMFORGE → Stage 2 → Generate Native Art Assets** (one deliberate Editor action).
4. In Project window browse `Assets/Swarmforge/Art/Stage2/Generated/Prefabs/`. There should be **seven native prefabs** and matching mesh/material assets under `Generated/Meshes/` and `Generated/Materials/`.
5. Open each prefab in Prefab Mode, inspect 3D geometry and materials. For a first visual review, drag each prefab into a temporary test scene or Preview pane; do not overwrite the approved P8 scene.
6. If all imports and prefab views work, **Game Developer integrates these prefabs into `V12TrialView` in the SAME PR #26 branch** in a separate sequential step. Developer must preserve exact simulation X/Z, 30Hz, HP2/HP3, left x2 chest, battle outcomes and restart, and real sequential ×2 MiniBots with unchanged captured muzzle X. Do not change `V12TrialSimulation` or 30Hz collision/contacts for art.
7. Only **after Developer integration**, Owner opens `V12_Trial.unity`, selects 9:16 Game view and performs **EditMode 86/86 + PlayMode 8/8 P8 regression target**, Console inspection, manual HP2/HP3, single-file x2, chest damage, Win/Lose/Restart. Report actual test counts; targets are **not** test results.

## Exact presentation integration guidance — Developer owns changes

Current `V12TrialView` creates primitives in its constructor/Rent. **Art has deliberately NOT modified that code**. New prefabs will NOT appear in Play mode until Developer wires them.

- **MiniBot:** replace **visual-only** green capsule prefab; keep the simulation true `WorldX(u.X)` and `WorldZ(u.Position)` **without** any fan-out. Model pivot is feet, total model height ~1.8; suggested visual **uniform scale around 0.35–0.4**, foot at world Y~0.07. Never use mesh collision for combat.
- **Enemy robot:** red unit prefab instead of sphere, scale about **0.4**, keep red ID-based formation offset that separates enemy groups while their **logical X remains center X0**. HP2/HP3 visual differentiation must not change logic.
- **Cannon:** align the **barrel muzzle** with the current **logical shot birth at Z=0**, while cannon follows authoritative aim X. Local muzzle is near **Z=+0.92**; choose parent placement/scale carefully so real births do not visually originate deep inside the model. Do not change spawn timing or captured aim.
- **Single ×2 chest:** world location remains **`WorldX(-950)`, `WorldZ(9000)`**, local pivot foot y=0; label and HP remain the existing authoritative HUD. **Do not add extra boxes.** Do not move chest into red central corridor.
- **Friendly base, enemy fortress:** retain original scripted victory/defeat and fortress HP40; on defeat, an animation-free flattened/damaged visual can be used, but no gameplay changes.
- **Arena:** keep current geometry and camera framing concept (X about ±4.2, Z roughly -2..20) and preserve visible left box and central combat lane in 9:16. The large floor prefab replaces overlapping graybox floor graphics, **not** the gameplay world coordinates. Avoid z-fighting, duplicate floor meshes or blocking side targets.
- All prefabs are **pure presentation with no colliders/Rigidbodies/gameplay components**. Pool and reset visual instances under the existing presentation lifecycle; preview meshes must never be treated as authoritative combat targets. Player one-finger controls, HUD, tests and physics must not be altered for Art.
- Avoid Stage 3 VFX/animation, Stage 4 UI, Stage 5 sound and VS-02.

## Actual verification, limitations and next handoff

**Done:** 7 original polygon sources, common MTL palette, manually-triggered Unity Editor mesh/material/prefab construction pipeline, source/project integration instructions.  
**Not done:** generating native .prefab/.mat/.asset files inside Unity, integrating new models into P8 scene view, Unity compile/Test Runner/Game view/mobile tests, FPS measurements. **UNITY NOT RUN** (no Unity Editor available to Art Director).  
**Handoff:** Developer is next owner. Developer must **run the menu command inside Unity or have Owner do it**, integrate these resources only in PR #26, and send a fresh Unity-testable build. Stage 2 remains **pending Owner Unity acceptance**. No main/merge/CI/Node, no VS-02.

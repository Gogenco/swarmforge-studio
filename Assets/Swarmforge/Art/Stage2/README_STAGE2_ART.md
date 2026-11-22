# SWARMFORGE — Stage 2 P9 Visual Rework / REAL 3D Sources

**Owner visual feedback:** [Art Issue #5, P9 rejection](https://github.com/Gogenco/swarmforge-studio/issues/5#issuecomment-7085773985).  
**Working branch:** `dev/v12-p2-rational-event-clock` — existing [Draft PR #26](https://github.com/Gogenco/swarmforge-studio/pull/26).  
**Unity Editor:** 6000.3.28f1, `Assets/Swarmforge/Scenes/V12_Trial.unity`, portrait 9:16.  
**Status:** `ART_SOURCE_READY_FOR_DEVELOPER_CAMERA_AND_SCALE_REWORK`. **UNITY NOT RUN** by Art Director.

## What changed after Owner rejected P9
This is a real replacement of all seven OBJ polygon sources (not concept images):
- **Friendly MiniBot:** large helmet/face visor, helmet crown, luminous head and back nodes, torso core, separate joints, bevelled armour, boot plates, strong cyan/green/charcoal value contrast. Source ~2,208 tris; pivot at feet; approximate height 2.14 local.
- **Enemy red robot:** hostile wider shoulder-plate silhouette, heavy gauntlets and claws, angular red helmet, high-contrast amber visor, armour and exposed reactor. ~2,232 tris, facing toward `-Z` for defense.
- **Auto cannon:** raised rotating gimbal, thick graphite barrel, bevelled housings and luminous muzzle. ~1,656 tris. **Local muzzle front is Z about +0.955** (front face +0.938, small inner light +0.953). Do NOT change authoritative spawn position.
- **Single ×2 chest:** physically raised `X2` top and front markings, metal reinforced frame, lid, emissive gold lock. ~1,384 tris. **Only one chest; keep gameplay position fixed**.
- **Player base** and **enemy fortress:** taller multi-layered structures, energy emitters, shield/gate cores, distinct teal/red profile. ~1,116 and ~2,084 tris respectively. Existing outcome/fortress health remains authoritative.
- **Arena:** multi-level track deck, subtle in-lane flush markings, modular raised service tracks, distinct top/bottom thresholds, edge guardrails, repeated pylons/tanks/industrial props and bright strips; extended decorative non-interactive side districts around `X=±5.18` to reduce empty 9:16 side space. Combat corridor and side chest are left open. Arena geometries are ***purely presentational***.
- **Shared materials:** editor generator updated to **URP/Lit**, one point-sampled 8×8 RGB palette texture `P9_Palette.asset`, max **two shared material/render groups per model** (Surface/Glow). Existing 36 named colors remain as source MTL swatches; glowing trim is static emission, **not Stage 3 VFX**.

The seven authored `Models/S2_*.obj` and `Models/SWARMFORGE_P8.mtl` remain the source of truth. Unity-generated `*.mesh/.asset`, `*.mat` and `*.prefab` files are created/updated with the existing Editor binder. No gameplay scripts, camera, scene, balances, replay, CI or main modified by Art.

## IMPORTANT: update existing local Unity project after new source import
1. Preserve a backup of accepted P8 trial folder and earlier P9 screenshot/build.
2. Download exact current branch ZIP via GitHub **after** Developer takes the handoff or replace `Assets/Swarmforge/Art/Stage2/Models/` + `Stage2ArtBuilder.cs` from PR #26 in a throwaway test project.
3. In Unity **6000.3.28f1**, **re-run** `SWARMFORGE → Stage 2 → Generate and Integrate P8 Art`. The old generated prefabs/materials in previous P9 local project will **NOT** magically upgrade just because new OBJ sources appear.
4. Inspect `Assets/Swarmforge/Art/Stage2/Generated/Prefabs/` and the new `Generated/Materials/P9_AtlasSurface.mat`, `P9_AtlasGlow.mat`, `P9_Palette.asset`. Confirm seven prefabs, 3D silhouettes, correct Lit material colors and no compilation errors. **Do not claim PASS until in Unity.**
5. Existing P9 runtime `V12TrialView` still uses 85° near top-down camera, ortho size 11.75 and green/red 0.42/0.48 scales; **Developer owns the next presentation-only changes**. Until developer updates this, **new meshes alone cannot solve tiny robot screen size**.

## DEV HANDOFF: precise presentation-only adjustments
**Suggested FIRST Unity experiment (not asserted as tested/approved):**
- Move camera from 85° top-down to **60–67° down-angle**, target arena Z near 9.0, and adjust ortho size around **11–12** / framing as required to show cannon, left side chest, both bases, full attack lane and outer decorations in 9:16. Do not crop playable route for a hero shot; no camera controller that changes aim mapping without Owner validation. Keep existing screenshot as before/after comparison.
- Increase MiniBot prefab visual root from **0.42** to approximately **0.75–0.85** and red enemy from **0.48** to approximately **0.67–0.78**, then tune actual sizes **in Game View**. New bot local footprint depth ~0.7: `0.8×0.7=0.56 < 0.84` world longitudinal spacing between x2 bots. **Confirm actual two-bot gap/aim in Unity** instead of assuming.
- Keep true green `WorldX(u.X)` and `WorldZ(u.Position)`, no sideways green offsets; both x2 bots stay on **same original X**, with actual 8-tick delayed follower. Red enemies keep their approved ID-stable visual grouping.
- Cannon: new front Z≈+.953 local, so adapt existing `Stage2MuzzleBackOffset = 0.953f * CannonModelScale` if changing `CannonModelScale` (e.g. .65→.85). Cannon root follows simulation-selected X unchanged; shot birth Z0 unchanged.
- Left x2 box must remain at `WorldX(-950)`, `WorldZ(9000)`, with HP5 and no new box or collision. Keep existing Art catalog references and regenerate its 7 prefabs. Keep `BoxLabelWorld`/readability in view; no HUD redesign here.
- Visual environment is outside combat path; do not introduce new colliders or damage from 3D meshes. If angle reveals wall/pylon obstruction, tune camera/visual-only art offsets **without** changing physics coordinates. Keep existing old `Battle.unity` and all legacy fallback fixtures intact.
- Use reasonable directional lighting/color. URP/Lit now makes side faces and bevels readable. **No Stage 3 animations/VFX, no Stage 4 UI redesign, no Stage 5 sound.**

## Owner Unity acceptance gate (actual results required, NOT claims)
In fresh **9:16 Unity Game View** after Developer integration, inspect two images (previous P9 and new live Unity) and check: MiniBot eyes/helmet/arms discernible, red enemy unmistakable, cannon muzzle, raised x2 glyph, fortresses, visible rich side environment without blocked box or path. **No synthetic image qualifies as Unity evidence.**

Use Unity EditMode and PlayMode full regression (Owner P8 baseline target 86+8, plus any current Stage 2 added checks; actual discovery counts may differ), Console errors=0, HP2/HP3, chest isolated, boost 12s, true x2 sequential same-line, red centered combat, Win/Lose/Restart, old Battle and mobile profiling where available.

**Boundary:** `UNITY NOT RUN` by Art Director. Native assets need Unity generation/re-import. Stage 2 **not Owner-approved** until live runtime review and regression. No merge/main/Actions tests, no Stage 3+.

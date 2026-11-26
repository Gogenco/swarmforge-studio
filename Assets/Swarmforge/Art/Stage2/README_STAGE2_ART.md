# SWARMFORGE — STAGE 2 / OWNER P12 MAJOR 3D & PBR ART REWORK

**Scope:** REAL 3D source assets, source-controlled PBR textures, editor-only native Unity prefab generation. **Art source readiness only: UNITY NOT RUN; no claim of 10/10 or visual approval.**

**Authority:** https://github.com/Gogenco/swarmforge-studio/issues/5#issuecomment-7103285574  
**Existing branch/PR only:** `dev/v12-p2-rational-event-clock` / https://github.com/Gogenco/swarmforge-studio/pull/26  
**Unity version:** 6000.3.28f1, URP 17.3.0, `Assets/Swarmforge/Scenes/V12_Trial.unity`, portrait 9:16.

## Major asset replacement, not minor embellishment

All of these files are actual playable Unity-importable **polygonal OBJ or PNG texture sources**, NOT screenshot cards or a background image. **Do not rely on old generated P9/P11 native prefabs:** the existing menu must regenerate them.

### Seven retained original prefab slots

| Active model | Source | P12 geometry budget and shape |
| --- | --- | --- |
| City arena | `Models/S2_Arena.obj` | **54,636 authored triangles**: entirely rebuilt boulevard, 8 tall buildings with real glass floors, mullions and rooflines, 9 background skyline towers, 18 multi-layer trees, street furniture, 72 flower clusters, paving, curbs, asphalt. |
| Friendly MiniBot | `Models/S2_FriendlyMiniBot.obj` | **3,972 triangles**: completely remodeled 3.1-unit-tall hero mech with oversized helmet/visor, articulated heavy boots, thighs, knee actuators, deep chest, lit core, bulky shoulder armour, manipulator hands, rear pack, side pistons. Green + cyan/steel. |
| Red enemy | `Models/S2_RedEnemyRobot.obj` | **4,060 triangles**: completely remodeled 3.1-unit hostile silhouette with claw/weapon pods, red armour plates, amber visor, articulated metal joints, exposed mechanisms, heavier shoulders and hands. Faces gameplay direction `-Z`. |
| Cannon | `Models/S2_AutoCannon.obj` | Existing authored model retained. Local muzzle ~Z+.9535, compensated visually only. |
| One real ×2 chest | `Models/S2_X2Chest.obj` | Existing authored one-left-side HP5 model retained; actual ×2 reward stays 12 seconds. No decorative duplicate boxes. |
| Friendly base | `Models/S2_PlayerBase.obj` | Existing source retained, teal lower end. |
| Enemy fortress | `Models/S2_EnemyFortress.obj` | Existing source retained, red upper end. |

Mech **real authored LOD** assets: `Models/S2_FriendlyMiniBot_LOD1.obj`, `...LOD2.obj`, `Models/S2_RedEnemyRobot_LOD1.obj`, `...LOD2.obj`: **460 and 228 triangles** respectively. Editor-generated prefab uses `LODGroup` for LOD0/1/2; all three are model geometry, not screenshots. Screen-relative level thresholds: **3.5% / 1.2% / .4%**; visual inspection needed on real mobile.

### Genuine physically based maps committed into repository

Every PBR material has **three real source-controlled 512×512 RGBA PNG files**, names:

- `Textures/P12_Asphalt_{Albedo,Normal,Mask}.png` — weathered road, coarse aggregate, subtle tire patches. Mask R=metallic, G=AO, A=smoothness (inverse roughness).
- `Textures/P12_Pavement_{Albedo,Normal,Mask}.png` — staggered limestone tile joints, normal relief and low-sheen stone.
- `Textures/P12_Grass_{Albedo,Normal,Mask}.png` — uneven green variation, finer clumps and low-sheen grass.
- `Textures/P12_CityGlass_{Albedo,Normal,Mask}.png` — vertically reflective blue-grey glass, warm window variation and high-smoothness window panels.

**No externally sourced or paid textures**; P12 map pixels are original, deterministically authored. Editor-only `Editor/Stage2ArtBuilder.cs` now loads the **committed PNGs** (rather than the previous 256px colour textures) with Unity `TextureImporter` (albedo sRGB, normals imported as NormalMap, masks linear, mipmaps/compression). Saves URP/Lit `P12_Asphalt.mat`, `P12_Pavement.mat`, `P12_Grass.mat`, `P12_CityGlass.mat` under `Generated/Materials/`; BumpMap + MetallicGlossMap assigned and normal/metallic keywords enabled. Meshes still support the old swatch palette for robot paint, trims, flowering branches, metalwork. These texture assets are NOT runtime-generated textures.

**Compatibility:** Original seven model names and prefab catalog remain unchanged. No runtime scene/gameplay source changed by Art, no new colliders/Rigidbodies. `S2_Arena.obj` includes `vn` normals and mesh faces; original OBJ models otherwise keep their authored normals. City road is at Y≈−0.1 (playable floor), above soil. Verify ground contact in Unity, don't move sim units to compensate.

## Exact boulevard geometry / presentation handoff to Game Developer

**Authoritative original simulation X/Z MUST NOT MOVE.** Geometry is aesthetic only.

- **Street playing region:** road centered X=0, width **about 8.23** world units (X±4.12); Z approximately **−2..20**. P8 combat and projectile authoritative positions remain unchanged.
- **Pedestrian path / plants:** curb X~±4.26, paving X~±4.94, trees X~±5.68, flowerbeds X~±6.10, building side X~±6.2 (street-facing facade); **no active colliders**.
- **Building scale:** eight main high-rises center around **X±7.5**, positioned near **Z=.8, 6.65, 12.5, 18.35**, tower heights around **Y13–22**; nine decorative distant towers near **Z=28–31**. Camera frustum must frame visible **actual side facades**, not only 2D narrow slivers.
- **MiniBot local feet:** Y0, helmet/antenna top about **Y3.2**, width ±1.1, forward +Z. **Enemy** same pivot/dimensions, faces -Z. Preserve same active sim coordinates; tune only *display* scale and camera so a nearest green is readable in real Game View without overlapping true x2 .84 world longitudinal gap.
- **Existing cannon model muzzle:** +Z≈.9535 local, keep cosmetic muzzle back-offset proportional to scale; never alter actual shot-origin or captured X.
- **Existing left x2:** logical X=−950, Z=9000 (sim units), HP5, reward12s. In current WorldX/WorldZ coordinates, P8 decides hit mechanics; new city no new boxes.

**P12 Owner rejected the former perspective camera** even though FOV55 at (0,16,−9) LookAt(0,.7,4.2) is already in source. Do **NOT** reapply that camera as the solution. Developer must test new intentional cinematic 9:16 composition: trial camera rear-lower **Y≈11–14** and Z≈−6..−9, target Y≈1–2, Z≈5–8, FOV ~43–50 (initial experiments only). Measure live projected subject size and ensure lower cyan base, full X2 chest, central combat and red upper fortress all remain visible. **More important than exact trial values:** a near green mech has enough on-screen pixels to read head, hands and feet; both near-building facades and their upper levels contribute visibly, not cropped edge slits; trees do not obscure real bots. Fine tune angles and camera height/target based on real Unity capture. Verify 9:16 and horizontal drag/aim rails each change. No yaw unless separately tested for aim control.

**Lighting target:** directional sun with selective soft shadow casting on hero robots/bases and dominant architecture, cool skylight fill, controlled neutral material exposure. Don't blindly cast all leaf or glass shadow geometry; check mobile drawcalls, FPS and memory with Unity Profiler. No fake PBR reflections or photoreal claims without real Game screenshot.

## Unity Owner verification and integration

1. Download exact-head PR #26 branch archive into **new Unity folder** (retain P8 rollback copy).
2. Open Unity 6000.3.28f1, let AssetDatabase import 12 PBR PNG files. Check Console for C# and import errors. If errors occur, **STOP** with first actual stack trace.
3. Run **`SWARMFORGE > Stage 2 > Generate and Integrate P8 Art`**. This re-saves seven native prefabs, actual bot `LODGroup`, material and mesh `.asset` resources. It may also need texture reimport; confirm in `Generated/Prefabs/`, `Generated/Materials/` and `Generated/Meshes/`.
4. In Prefab Mode confirm P12 city geometry (8 buildings, 18 trees, asphalt and glass), 3 levels of both robot prefabs, correct URP normal maps and 4 material sets. Do not expect old local generated prefabs to update automatically.
5. **Game Developer**, working sequentially in this *same PR*, adjusts only `V12TrialView` presentation camera, scales, directional lights and scenery casting flags after receiving this Art handoff. Art does NOT adjust gameplay/logic.
6. **Owner:** fresh 9:16 Game screenshots side-by-side with approved target, full actual EditMode + PlayMode tests and Console, aim left/center/right, side chest (five hits, x2), two **real sequential bots** with same captured X at t and t+8 ticks, no fanout; centre red hits, HP2 vs HP3, Fortress40 Base3, win/defeat/timeout/restart/replay, old Battle scene. Full results only when truly executed.
7. If visually inadequate, Owner reports which Game View objects fail, with screenshot. No **10/10 claim** or Stage2 approval until Owner personally accepts real Unity output.

**Status boundary:** actual 3D OBJ and PBR PNG source files exist in PR #26 once committed; native `.prefab`/`.mat`/`.asset` generated in Unity, **not pre-generated/verified by Art**. No Unity Editor available to Art; `UNITY NOT RUN`. Existing P8 sim 30Hz and all authoritative behavior protected; no main/merge/Actions/CI/Node, Stage3/4/5 or VS02.

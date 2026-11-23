# SWARMFORGE — Stage 2 FINAL CITY VISUAL ART (GITHUB-OWNER #7098706418)

**Status:** REAL 3D ART SOURCES COMMITTED; existing P8 Game Developer integration required; **UNITY NOT RUN by Art Director**. Stage 2 Owner approval NOT yet granted.  
**Owner directive:** https://github.com/Gogenco/swarmforge-studio/issues/5#issuecomment-7098706418  
**One active PR/branch:** https://github.com/Gogenco/swarmforge-studio/pull/26 / `dev/v12-p2-rational-event-clock`  
**Scene:** `Assets/Swarmforge/Scenes/V12_Trial.unity` — Unity 6000.3.28f1, 9:16 portrait.  
**Design:** premium lively urban-mech boulevard, faithfully tracking Owner's reference *components and composition*, not a flat screenshot. Exact pixel matching requires Owner's actual in-Unity comparison.

## Actual 3D deliverables

**Seven expected P8 prefab names remain unchanged.** The same `SWARMFORGE > Stage 2 > Generate and Integrate P8 Art` workflow re-generates real native Unity meshes, materials, textures and prefabs from OBJ sources in `Models/`.

**Main change: `Models/S2_Arena.obj` is now a REAL 3D CITY, not the old industrial arena.** Approximately 26,956 source triangles; includes a wide authentic-looking asphalt avenue, lane separators and crossings, beveled curbs, paving-tile sidewalks, deep-green park strips, 18 broadleaf/pink/yellow/red trees, flowerbeds, concrete planters, 12 lamp posts, 10 wooden benches, four cafe patios/umbrellas, glass-sided shopfronts, 8 high-rise towers with many authored 3D window panels and concrete facade bands, fortification banners, and a distant skyline behind the enemy fortress. All are true model faces and render in the live Unity world with perspective changes. Buildings/flowers/sidewalk furniture are outside the road's P8 logical combat lanes. **No additional functional or fake decorative chests** and no art colliders or combat mechanics. Exterior towers need framing/occlusion review by Developer.

**Legacy protection:** prior industrial P9 arena source is archived as `Models/Legacy/S2_Arena_P9_Industrial.obj.txt` (not imported as an active OBJ). Existing P8 simulation code and trial scene remain untouched by Art.

**Existing real 3D models upgraded, original names retained:**
- `S2_FriendlyMiniBot.obj`: green allied articulated armored mech, large visor/crown/head, hands/boots, reinforced joints and rivets, machine vents and glow accents. ~2,580 triangles source.
- `S2_RedEnemyRobot.obj`: distinct wide-shouldered hostile red mech, claws and plate segments, amber face visor, weapon-arm details and real bolts. ~2,456 triangles.
- `S2_AutoCannon.obj`: thick metallic turret/muzzle from P9. **Local muzzle remains about Z +0.953**, do not move gameplay shot birth or captured-X.
- `S2_X2Chest.obj`: **only one** side chest with physically raised top/front X2 marks and latch; original P8 HP5 is authoritative from sim. No fake loot/UI counters added.
- `S2_PlayerBase.obj`: taller cyan friendly bastion, new front teal crest banner/rivets/reinforcements; ~1,296 triangles.
- `S2_EnemyFortress.obj`: taller red hostile gate with roof battlements and red hanging banners; ~2,276 triangles.

**Art material/texture generation:** Art-owned `Editor/Stage2ArtBuilder.cs` now uses `URP/Lit` and a 16×16 palette texture for metals/paint/leaf/flowers; on manual Editor generation it also saves **four real tileable `Texture2D.asset` textures** (128×128 procedural asphalt grain, green grass variation, tiled limestone sidewalks and glossy glass). Color swatches, textures and lit shader are then used on native Unity meshes with world-projected UVs. Only procedural Editor-time texture generation; no gameplay RNG/code, no external PNG dependency. Mesh importer builds geometry into shared group renderers rather than thousands of GameObjects.

## IMPORTANT — Game Developer sequential handoff

**Art Director writes completed. Developer is next.** Do not mix concurrent PR #26 writes.

1. Save Owner-accepted P8 folder; download current branch ZIP into **fresh** Unity project folder. Unity 6000.3.28f1.
2. Open Unity and resolve any compiler/import errors FIRST. Execute **`SWARMFORGE > Stage 2 > Generate and Integrate P8 Art`**. This must regenerate previously made P9 assets; old locally generated prefabs are stale. The existing binder/catalog still expects the same seven prefab names; city art is embedded in `S2_Arena` to avoid changing Gameplay or Art Catalog contracts.
3. Inspect `Assets/Swarmforge/Art/Stage2/Generated/Prefabs/S2_Arena.prefab`: real road + street furniture + trees + glass towers. Check generated `P10_Asphalt_Texture.asset`, `P10_Grass_Texture.asset`, `P10_Pavement_Texture.asset` and `P10_CityGlass_Texture.asset` in `Generated/Materials`. Verify no missing shaders/materials/pink assets in Unity. Inspect green and red robots and both fortresses.
4. In `V12TrialView` PRESENTATION-ONLY: frame an elevated oblique 9:16 camera so the street appears wide, the top enemy red fortress and lower cyan base are simultaneously visible, the left X2 chest remains accessible and the modern towers/flowers/trees flank the avenue. Preserve large readable bots, street perspective, city backdrop and unobstructed aim. P9 source previously used tilt ~64°, green root scale .76, red .70, cannon .84; adjust based on **actual Unity Game view**, not a fixed numeric promise. Buildings extend roughly X±9.5 and beyond Z24; prioritize complete **playable road, chest and fortresses** over displaying every skyline prop. Tight crop only with Owner acceptance; do not shrink robots back into dots.
5. Preserve cannon **muzzle visual placement** with actual shot origin Z0 by cosmetic back-offset `~0.953 * cosmeticScale`; there is no new projectile type. Green units use authoritative true X/Z and follower at t+8 ticks, **never green sideways fanout**. Red group retains correct center sim collisions and visual formation offsets.
6. **Do not alter** `V12TrialSimulation`, 30Hz, 1-finger aim, Box x2 HP5 / boost 12s / queue, HP2/HP3, enemy combat C1–C6, Fortress HP40 / Base HP3, victory/loss/timeout/restart/replay, old `Battle.unity` or underlying AI/physics. All city meshes are visual-only, NO colliders/Rigidbody or interaction with sim.
7. Stage 3 effects, Stage 4 HUD and Stage 5 audio are **not authorized**. Keep accurate gameplay HUD counters even when the approved reference happens to show fake extra crates/Box x12. No Stage3 lasers or auto-loot.

## Acceptance gate — Owner ONLY in actual Unity

From fresh source ZIP: run the Editor generation, open `V12_Trial.unity`, portrait 9:16 **Game View**. Compare **real Unity screenshots** to approved reference. Confirm road width/map angle, rich towers, lush trees and mixed-color foliage, grass/flowers, glass facades, daylight and shadows, detailed visibly enlarged green/red robots, realistic cannon/single X2 chest/fortresses and empty area filled. Inspect Console for any real errors, mobile profiler if available (30/60 FPS are goals, **not verified**). If importer, atlas or 32-bit mesh indices fail, report exact Unity Console output.

Run *actual* Unity EditMode and PlayMode and P8 manual regression: controlled cannon aim, chest 5 real hits and X2 12s, same-X first/follower at +8 30Hz ticks (no sideways shot), center enemy combat, HP2/HP3, fortress/base outcomes, restart, queued boost/replay, legacy scene. Historic P8 Owner target was 86 EditMode + 8 PlayMode; the current P9+ art project may discover more tests and actual counts must be reported. A previous P8 green does not certify this new art build.

**ART LIMITATIONS:** No Unity Editor in Art environment: **UNITY NOT RUN**; no Unity-generated native prefab files committed, no actual in-engine screenshot/visual QA, FPS claim or test PASS. Source OBJ and editor importer are committed and form real meshes/materials/prefabs **when executed in Unity**. Developer integrates presentation in existing PR #26, Owner approves only following live Unity testing. No main, merge, force push/rebase, GitHub Actions/Node/CI, VS-02 or Stage 3.

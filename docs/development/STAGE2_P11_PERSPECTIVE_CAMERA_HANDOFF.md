# SWARMFORGE Stage 2 / P11 True Perspective City Camera Owner Handoff

Source: Art Director Issue #4 comment 7102117358, exact Art HEAD 05f5229124b42cdfcc45ce5f0775b4eee0bac202. Owner target/rejection in Art #5 comment 7100166059.

## Presentation-only changes

- `V12TrialView` uses TRUE perspective camera (orthographic=false), FOV 55 degrees, world location (0,16,-9), looking at (0,0.7,4.2). Camera yaw exactly 0. Foreground P8 green mechs receive natural perspective enlargement; high-rise facade sides, windows, foliage and city depth no longer render as a flat map. Some exterior towers may be cropped at the very edges; the P8 road, chest, cyan base and red fortress are more important.
- Keep native green scale .77 and red .72 to preserve physical 8-tick longitudinal gap visually; bigger screen sizes come from perspective, not false sideways fanout. Original t/t+8 sim spawns remain real and same-X.
- Lighting: main warmer daylight intensity 1.42 with low-resolution soft shadows strength .33; no-shadow cool fill intensity .33; daylight clear sky. Reworked P11 Art OBJ author tree geometry, eight highrises, authored vertex normals, four 256x256 urban textures are preserved untouched. Existing city Surface shadow caster remains; no new physics/Stage3 VFX.
- Reusable pure presentation helper `V12TrialAimProjection.ScreenToAim` reproduces original two projected cannon rail endpoints at world Y=.5 and Z0. Actual `V12TrialView.AimFromScreenX` uses it, keeping the P8 touch selection stable in perspective. It never touches P8 simulation commands, logical bot coordinates, swept contacts or wall-clock/boost.

## MANDATORY UNITY 6000.3.28f1 Owner test (UNITY NOT RUN here)

1. Keep a backup of accepted P8 scene/project. Download exact new HEAD source ZIP into a fresh folder; open project in Unity 6000.3.28f1.
2. In Unity top menu, run `SWARMFORGE > Stage 2 > Generate and Integrate P8 Art` AGAIN. This regenerates seven native model prefabs and four 256x256 city textures with P11 authored normals and reworked trees. Old locally generated assets are stale.
3. Open `Assets/Swarmforge/Scenes/V12_Trial.unity`, Game view 9:16, Play. Verify camera looks forward into a real street with readable mech armor, high-rise faces and tree branches/leaves. Compare genuine Unity screenshot with approved reference; source only cannot prove visual match.
4. Run full Unity EditMode and PlayMode; new P11 PlayMode test checks exact perspective cannon rail endpoints/midpoint mapping, green same-X muzzle and unchanged 8-tick followup; previous P9 and Final City camera tests now expect perspective. Test actual one-finger drag from left to right, including off-center mobile aspect ratios.
5. Recheck HP2/HP3, deliberate left X2 box HP5, 12-second boost queue, x1 1 bot/sec and x2 2 real bots/sec in ONE lane +8 ticks apart, four central red enemies/C1-C6 retaliation, fortress40/base3, win/defeat/timeout/restart, replay and legacy VS01 Battle scene. Inspect URP shadows/textures, yellow OBJ normal warnings, Console red errors, Game View 9:16 and device Profiler (FPS 30/60 are targets, not verified).

**Verification boundary:** No Unity Editor available to developer. UNITY NOT RUN, no external Node/CI/Actions validators, no claim of successful actual in-engine 3D visual match or FPS. Only P8 presentation, Unity acceptance test source and this documentation changed. No changes to `V12TrialSimulation`, configs, `main`, `.github/workflows`, Stage3/Stage4/VS02 or merge.

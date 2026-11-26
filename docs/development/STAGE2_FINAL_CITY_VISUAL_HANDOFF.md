# SWARMFORGE — Stage 2 Final City / Developer Presentation Integration

**Source:** Owner visual target in Issue #5 comment 7098706418; corrected art handoff in Issue #4 comment 7099136200 (Art SHA f084b4836f1b6543e1308fa1d04f76895a1dfbaf).

**Scope:** Presentation-only V12TrialView and Unity EditMode/PlayMode acceptance tests. The seven original native prefabs remain one catalog: MiniBot, red robot, cannon, X2 chest, player base, enemy fortress and **S2_Arena now contains the full city**, including road, sidewalks, grass, flowerbeds, 18 mixed-color trees, towers, cafe props, benches, lamps and skyline. The city OBJ authored geometry and four 128×128 generated city textures are unchanged from Art Director's corrected HEAD. No flat screenshot used.

## Camera and lighting authored for Unity Owner tuning
- City is placed once at transform origin and scale1, never on top of previous greybox floor; the +Y ground correction from Art is preserved.
- 9:16 portrait **orthographic** camera, elevated oblique 59 degrees, position (0,19.5,-2.7), half height max(12.8,7.6/aspect). This frames the complete P8 road and chest and much of the vegetation, deliberately cropping the farthest exterior towers instead of shrinking MiniBots into dots. The bright blue sky is a solid background.
- Green root scale .77 (true authoritative X/Z), red .72 (red stable-ID *visual* formation unchanged), cannon .84 with native muzzle +0.9535 backward compensation. The real x2 follower remains eight SIM ticks after first at captured aim X, not a cosmetic second shot.
- One URP-compatible soft daylight directional key, shadowStrength .28, Low shadow resolution, subtle no-shadow blue fill. Static city Surface, four red robots and major landmarks can cast; city road/glass/grass/glow and pooled green crowd do not cast, all non-glow receive. Only aesthetic Light/Renderer changes, no new physics.
- Old P8 simulation and old Battle scene untouched. No new crates, rewards, combat VFX, HUD restyling or animations.

## MUST RUN IN UNITY 6000.3.28f1
1. Preserve Owner-accepted P8 backup. Extract fresh full ZIP in another directory.
2. Let Unity compile; in Editor choose **SWARMFORGE > Stage 2 > Generate and Integrate P8 Art**. This regenerates seven native Mesh/Prefab assets, atlas and four city Texture2D assets using the corrected OBJ and binds the Resources catalog. **Old P9 generated assets are stale.**
3. Open **Assets/Swarmforge/Scenes/V12_Trial.unity**; Game View **9:16**; click Play. Verify real asphalt + sidewalks + grass + trees in mixed colors + flowerbeds + 8 glass highrises and distant buildings, models, shadows and sunny lighting. Compare in-engine screenshot with Owner reference; pixel-exact match cannot be claimed without actual image.
4. Run entire Unity EditMode and PlayMode. New EditMode test requires skyline mesh bounds and all four real textured city render layers; new PlayMode checks camera/road landmarks in frame, soft sun, true muzzle and unchanged HP40/box5. Existing earlier P9 camera test is adjusted rather than disabled.
5. Live manually test 1 bot/s, x2 two REAL bots in one lane t/t+8 and 12s, deliberate left X2 box 5 hits, central HP2/HP3 red damage/retaliation, fortress victory, base defeat, timeout/restart and old VS01 Battle. Check Unity Console zero red errors, original URP import concerns and actual Game View performance on target mobile devices.

**No Unity Editor available in Developer environment; code/test definitions are added but UNITY NOT RUN.** Sun shadows can only render if the Owner's URP/Quality asset supports shadows. FPS 30/60 are targets, never verified benchmarks. No Node/CI/GitHub Actions/test runner outside Unity; no main, merge, Stage3 or VS02.

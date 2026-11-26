# SWARMFORGE P9 — Stage 2 Visual Rework / Game Developer Unity handoff

Scope: changes are **Presentation only**, built on Art Director source commit `ee217cddd9d6455b56674258a33fbd011089c4a4`. No P8 simulation, balance, legacy Battle, packages or Actions changed.

1. Open the full exact-head GitHub project ZIP in **Unity 6000.3.28f1** as a fresh project; preserve P8 accepted backup.
2. Run **SWARMFORGE → Stage 2 → Generate and Integrate P8 Art** again. This is mandatory because old generated native prefabs use obsolete OBJ meshes/materials. Confirm updated `P9_AtlasSurface.mat`, `P9_AtlasGlow.mat`, seven generated prefabs and Resources art catalog.
3. Open `Assets/Swarmforge/Scenes/V12_Trial.unity`. Use **Game View 9:16**, press Play. Compare actual Unity screenshots before/after P9 (not synthetic screenshots).
4. Camera is now down-angle 64°, positioned (0,24,-2.7), with orthographic height auto-fit to 9:16 and narrower mobile screens. Visual-only green scale .76, red .70, cannon .84. Red visual four-slot spacing increased to +/- .70 on X/Z; **green true sim X/Z is untouched**. P9 cannon's muzzle local Z .9535 is aligned with logical spawn Z0. Lit key and cool fill both have no realtime shadows. The native arena already has raised props; no extra colliders.
5. Check actual 9:16 readability: green/red silhouettes & faces, cannon, raised X2 on side chest, arena pylons not cropped/overlapping chest, base and fortress in frame; P8 x2 pair fires **sequentially at t and t+8 on one exact X**, 1 bot/s ×1 and 2 bot/s ×2, HP2/HP3, 5HP x2 chest/12 seconds, central red combat C1–C6, FortressHP40, Victory/Defeat/Restart/Stop→Play and old Battle.
6. Run complete Unity **EditMode and PlayMode Test Runner**, inspect Console errors, capture actual counts and Unity screenshots, plus Profiler on target mobile hardware. This developer environment did **not** execute Unity Editor or its tests; changes are **not certified** by code submission.

Existing authored P9 OBJ art, shared atlas/material builder and Editor binder remain untouched. Greybox fallback remains available when native catalog isn't generated, but P9 acceptance strictly requires native catalog.

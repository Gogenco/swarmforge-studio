# SWARMFORGE STAGE 2 / P11 ART REWORK — approved realistic boulevard target

**Status:** AUTHORED REAL 3D ART SOURCES UPDATED; **UNITY NOT RUN**; VISUAL OWNER ACCEPTANCE PENDING. Use existing Draft PR #26 / `dev/v12-p2-rational-event-clock` only.

## Owner P11 visual rejection
- In-game robots too small in the portrait Game View.
- Orthographic 59° overhead view squashes city into a flat map and crops high-rise facades.
- Previous spherical tree crowns look like lollipops and surfaces are too uniform.
- Source OBJ had no authored vertex normals (Owner saw 44 yellow `ImportFBX` warnings about normals; not an error or proven broken gameplay).
- Real-world target is the approved 9:16 neon/modern glass avenue concept with cinematic depth. P8 must stay unchanged.

## Real updated art in the PR
- `Models/S2_Arena.obj`: removed drawn **sphere canopy faces** and rebuilt layered tree crowns with **2,772 individually modeled 3D foliage leaves**, **306 branched/tapered wooden limbs**, irregular deciduous and pink/yellow/red/green species, roadside hedges and grass border. These are geometric meshes, NOT flat screenshots. Tree roots/roads/collision paths remain fixed.
- Existing eight high-rises shift slightly toward the street **visually only**; added metallic mullions, side glass panels, additional story bands, reflective strips and rooftop volumes. No building collider; keep street/left chest clear.
- `Editor/Stage2ArtBuilder.cs`: regenerates four existing saved 256×256 tileable `Texture2D.asset` outputs (asphalt, grass, pavement, glass) with fine/medium/large grain and more restrained reflectance for roads/lawns, glossy glazed facades. Existing 128px locally generated assets are upgraded on menu rerun via `Reinitialize`. URP/Lit and source materials/prefab names remain stable.
- All seven active OBJ source models now include **authored per-face `vn` normals** (`v//vn` faces) to address the Owner's Unity OBJ `ImportFBX` no-normals warnings. The Editor-native generator still computes normals independently from those faces. Actual warning reduction is **unverified until the Owner reimports in Unity**. Seven OBJ source models and the single Resources art catalog remain compatible. No new functional boxes, no sim/physics edits.

## Developer sequential handoff — presentation change is necessary
A prettier OBJ alone cannot fix the P11 screenshot; **the existing camera is orthographic, at Euler pitch 59°**, with large half-height chosen to fit city width, making all robots and skyscrapers appear flat/tiny at 9:16. **Developer owns `V12TrialView.cs` next** after Art handoff, not simultaneous editing.

**Experiment in Unity Game View, do NOT commit arbitrary constants without observing live frame:**
1. Try a fixed elevated **PERSPECTIVE** camera (e.g. initial vertical FOV 45–55°, roughly 35–45° downward, camera rearward behind friendly base, target central corridor) for vanishing point, visible building side facades and larger foreground robot projections. Fit full P8 road, single left X2 HP5 chest, green spawn/cannon, friendly base near bottom, red fortress at top and active city towers; preserve one-finger touch aim mapping by projecting cannon rail endpoints each frame. **Exact FOV, camera Z/Y, clipping and safe-area cropping must be tuned in Unity, not claimed from source geometry.**
2. Keep MiniBot visual scale/bot screen prominence high without making the two real x2 bots (8 tick gap) overlap or hiding the playable road. Enlarge perceived size via perspective and camera proximity rather than arbitrarily multiplying prefab scale. Aim X/Z exact authoritative coordinates; no green fanout, no side shift/fake second bot.
3. Real shadows/soft sun key/fill, neutral tone mapping/URP exposure, glass glossy with restrained highlights, realistic road mottling. Remove not a single HUD/control (Stage4 only), but avoid hiding gameplay under old overlays. No realtime VFX/laser/music (later stages).
4. Re-import the newly updated mesh OBJ + art Editor script by running Unity **`SWARMFORGE > Stage 2 > Generate and Integrate P8 Art`** in a fresh extracted project. Do NOT use stale P11 generated prefabs or textures. Inspect actual prefabs, no missing geometry/materials, tree layering, roof lines and camera culling.

**Critical P8 gate:** sim 30Hz, single x2 box X−950 HP5 +12s, sequential two genuine bots at t and t+8 from captured aim X, center red group combat, HP2/HP3, fortress40/base3, win/lose/restart/replay, touch-to-aim, old Battle scene all unchanged. Developer and Owner run full EditMode+PlayMode in Unity, Console warnings/errors, Game View 9:16, real screenshots and mobile Profiler. No GitHub/Node/static tests. Historical P8 test counts do NOT prove current source PASS.

**Constraints:** no main, no new branch/PR, no merge, no GitHub Actions modifications, Stage3/4/5 or VS02. Art cannot claim identical look to approved image until Owner provides real Unity screenshots and approves result.

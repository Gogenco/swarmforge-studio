# GAME-001 / DEV-001 — VS-01 Handoff

**TASK ID:** GAME-001 / DEV-001  
**STATUS:** `BLOCKED` (Unity runtime verification)  
**SCOPE:** VS01-01..VS01-06 only; no VS-02/03/04.

## Completed source work

- Established separate Unity 6.3 LTS C# project with `Battle.unity`, versioned level asset, URP package and test assemblies.
- Implemented integer 30 Hz match simulation, stable UnitIDs, sorted/deduplicated commands, immutable snapshots, deterministic state and event hashes.
- Implemented raw shot schedule, horizontal cannon, ×2/×3 (one gate per bot), MiniBot/Enemy movement and contacts, fortress/base damage, three provisional fortress destruction stages, terminal tie-break, timer and fresh-match Restart.
- Implemented placeholder 3D visuals and one-thumb/mouse control; presentation does not determine damage.
- Added 11 EditMode test methods, with 2 parameterized gate cases, and one PlayMode smoke test. **These tests have not been executed.**

## Checks / evidence

- Node static checker `node tools/validate-project.mjs`: run it to verify structure, manifest, GUID references, assembly dependencies, and lexical C# delimiters.
- Unity C# compilation: **NOT RUN** (Editor/compiler absent).
- Unity EditMode/PlayMode: **NOT RUN** (Editor absent).
- Android / iOS builds, touch, FPS/thermal, gameplay video: **NOT RUN**.
- Balance and real lanes: **NOT VERIFIED**. VS-01 contains only a placeholder enemy on each lane.

## Issues and dependencies

1. **Critical environment blocker:** install/select Unity Editor 6000.3.28f1; verify Unity Hub, Package Manager, URP asset, Unity Test Framework and mobile build modules.
2. **Game Architect / Tech Lead:** inspect scene and core contracts; review fixed-point collision and event precedence on a running Editor.
3. **QA:** run actual EditMode/PlayMode, verify 30/60fps replay, last-tick win, repeat gate, restart and Base-loss. Provide failing logs/screenshots for correction.
4. **Game Designer:** final XYZ enemy lane waypoints, crossing regions and radii are intentionally deferred to VS-03.
5. **UI/UX / Performance:** touch safe-area and performance on physical Android/iOS are still pending; no claims of mobile readiness.

## Next step

Run in real Unity, fix any compile/test/runtime issues, then re-report with actual test counts, screenshots and an Editor-produced build. **Do not mark verified or complete yet.**

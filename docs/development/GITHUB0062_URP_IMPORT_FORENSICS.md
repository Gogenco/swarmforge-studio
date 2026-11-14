# SWARMFORGE — GITHUB-006.2 URP import evidence and safe next steps

**Status: URP ROOT CAUSE NOT YET PROVEN / NO ENGINE CODE FIX AUTHORIZED.** Documentation and optional local read-only diagnosis only. This Draft PR must **not** be described as a repair.

## Owner-confirmed evidence (2026-11-14)

1. Unity **6000.3.28f1**, Owner P1 EditMode **33/33** and PlayMode **2/2** tests PASS, **35/35** total. Nine P1 config/replay tests included; NUnit XML identifies the P1 branch project, but does not attest an exact commit SHA.
2. Six Editor Console messages after short-path clean extraction: `Host type is not matching any asset type` on Unity package assets `Packages/com.unity.render-pipelines.universal/Shaders/AutodeskInteractive/AutodeskInteractiveTransparent.shadergraph` and `Packages/com.unity.render-pipelines.core/Editor/Lighting/ProbeVolume/RenderingLayerMask/TraceRenderingLayerMask.urtshader`. They appear in asset postprocessing and GUI event frames; **no proven C# compiler/test failure**.
3. Owner checked URP/Core/Shader Graph **17.3.0**, URP Config **17.0.3**, and both default Graphics and Ultra Quality point to **SWARMFORGE_URP**. Shortening the path did not clear six host-type errors. Earlier 30 path/Burst errors arose during a separate forced reimport of a deeply nested copy; those are **not** the current six messages.
4. Git compare from stable DEV-002.2 `main` `99cfcdc0b4982e9e441cf23a8669a487a7d75ea9` to Developer P1 PR #24 `1281aed803a9f1bcdfb9de649623c6b783850762`: 15 **added** files, **none** under `Packages`, `ProjectSettings`, `Editor` or shader directories. Both have the **same exact** `Packages/manifest.json`, which directly pins `com.unity.render-pipelines.universal` at **17.3.0**. Neither tracked snapshot includes `Packages/packages-lock.json`, `ProjectSettings/GraphicsSettings.asset`, `ProjectSettings/QualitySettings.asset`, or a generated `SWARMFORGE_URP.asset`; Unity generates/maintains these in the Owner's local project. A git-only A/B therefore cannot prove whether the Editor importer error exists on baseline `main`.

**Interpretation:** The error message names a Unity package *asset host/type import mismatch*. The responsible internal importer failure, corrupt dependency/cache record, project-local setup interaction, platform-specific Unity bug, or package-state mismatch **cannot be distinguished without the originating Editor.log and an identical-environment baseline control**. A third-party claim that it is definitely one of these is not evidence. Unity forum threads independently report the **exact** `TraceRenderingLayerMask.urtshader` error in Unity 6.3, including failed reinstall/cache workarounds: https://discussions.unity.com/t/host-type-is-not-matching-any-asset-type/1694497 . They do **not** establish SWARMFORGE's specific root cause.

### Read-only evidence collection for Owner / QA

**Do not** reinstall/update editor or packages, clear the current Library, modify Unity vendor shaders, toggle render pipeline, or run `Reimport All` as an unverified remedy.

1. Close Unity **after** preserving the Console screenshot and failure; make a copy of the existing `Editor.log` to a separate user-owned diagnostics folder. Standard Windows Editor log location: `%LOCALAPPDATA%\Unity\Editor\Editor.log`. Note its last-write timestamp; Unity may rotate/update on launch.
2. In a checkout of this diagnostics Draft PR, run from project root, without requiring Unity: `node tools/diagnose-urp-import.mjs --editor-log "C:\\Users\\<user>\\AppData\\Local\\Unity\\Editor\\Editor.log" --json`. The script **only reads** and outputs package request/lock state, exact host-mismatch counts/paths/lines and known import-failure markers. It neither uploads logs nor modifies assets, caches or settings. Without a log, run `node tools/diagnose-urp-import.mjs --json` for static evidence only.
3. Share with QA: **redacted** first full occurrence plus ~20 lines before/after, the tool's JSON output, import timing (initial open / entering Play / build / GUI), `packages-lock.json` relevant resolved dependency versions if present, Windows active Graphics API (D3D11 vs D3D12), and whether shaders appear pink in Battle. Keep usernames, tokens and private paths out of public GitHub comments.
4. Separately reproduce in **two clean, short-path copies on the same machine** and **unchanged Unity 6000.3.28f1**: (A) stable `main` DEV-002.2 and (B) PR #24 P1. Record each import/Console outcome and Unity `Editor.log`, compare *resolved packages-lock*, graphics/quality assets and local generated URP assets. Run setup identically if needed; do **not** overwrite Owner's current P1.
5. Optional third control: create a new unmodified Universal 3D template in the **same Editor and compatible package versions**, capture whether the same package assets exhibit the mismatch. This differentiates editor/package-wide importer behavior from SWARMFORGE-specific setup but is **not** proof by itself.
6. Verify independent effects: Scene opens and renders without pink materials? Can a Windows Player build complete? Are shader imports red and reported as `DefaultAsset` instead of expected shader types? Report actual results. `35/35` NUnit PASS only proves those tests, **not** clean Console or build/visual PASS.

**Decision tree:** If (A) and (B) both fail identically, prioritize a Unity/URP/package/importer reproduction; do **not** patch gameplay. If only (B) fails under identical generated settings and lock, bisect P1 isolated additions and inspect importer activity before changing code. If empty URP control also fails, escalate with the exact Editor.log, version tuple and reproduction to Unity Issue Tracker/support. Only propose a version change or targeted asset fix after reproducible causal evidence and explicit Owner approval.

## Stage P2 dependency readiness, separate from URP Editor gate

- Architect `ARCH_V12_EXACT_HEAD_ALIGNED`: GDD #19 HEAD `b70da4e478b7a4b6dc54a403b130a20facf4e62f` ↔ ARCH #23 HEAD `95a900e15d1eaabd60f4fa75fe3a97cc076c2d2b`; Owner/Director C1–C6 is the single provisional trial oracle.
- Owner actual P1 EditMode/PlayMode **35/35 PASS**, including nine P1 tests; exact-SHA XML provenance not cryptographically proven.
- Thus the **GITHUB-006 design + P1 test gating evidence now exists**. Stage P2 design/branch planning may proceed independently under its existing narrow authorization; a clean P2 Unity Editor/visual/build gate remains **OPEN** pending URP classification. P2 must have a *different* Draft PR; no P3, VS-02 or merge.

## Checks for this branch

`node --test tools/diagnose-urp-import.test.mjs` (parser fixtures and manifest); `node tools/diagnose-urp-import.mjs --json` (read-only project snapshot). **No Unity runtime tests performed by this diagnostic branch.**

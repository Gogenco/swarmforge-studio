# ARCH-003 — QA2-03 URP persistence (separate architectural note)

**Task:** GAME-001 / ARCH-003. **Scope:** technical recommendation only; no asset or code changes in this PR.  
**Links:** [Architect #2](https://github.com/Gogenco/swarmforge-studio/issues/2) · [Developer #4](https://github.com/Gogenco/swarmforge-studio/issues/4) · [Master #1](https://github.com/Gogenco/swarmforge-studio/issues/1)

## Current source evidence

`Assets/Swarmforge/Scripts/Editor/SwarmforgeProjectSetup.cs` configures URP lazily via `[InitializeOnLoad]` and `EnsureUrp()`, creating these assets in a **local Unity Editor** when absent:

- `Assets/Swarmforge/Configs/Tuning/SWARMFORGE_Renderer.asset`
- `Assets/Swarmforge/Configs/Tuning/SWARMFORGE_URP.asset`

The DEV-002.2 baseline imported through PR #10 does not include those generated `.asset` and `.meta` files. It also lacks persisted rendering choices in full `ProjectSettings/GraphicsSettings.asset` and `ProjectSettings/QualitySettings.asset`. QA2-03 is a **source reproducibility gap**, not proof of a failing current Owner Unity session.

## Proposed recovery (requires Owner authorization)

1. In the Owner's **known-working Unity 6000.3.28f1** project, confirm the actual active URP Renderer, Pipeline and Project Settings assignments. Preserve them; **do not** replace working URP with a new pipeline.
2. Export the existing serialized renderer and pipeline `.asset` files **with their original `.meta` GUIDs**, plus the relevant GraphicsSettings and QualitySettings project settings.
3. Game Developer prepares a **separate scoped branch/PR** under QA2-03 authorization; no binary `Library/`, `Temp/`, editor cache, build outputs or credentials.
4. Verify every reference resolves to the checked-in GUID after a clean clone and Unity import; confirm correct URP render, no magenta materials, and no duplicate regenerated Pipeline/Renderer assets.
5. QA reports actual clean-import, PlayMode and device evidence separately. Static path inspection is insufficient to call the visual issue resolved.

**Architect recommendation:** keep Editor bootstrap only as an idempotent fallback, not the sole source of canonical URP assets. No action on QA2-03 in the QA2-01 code-fix PR; no VS-02.

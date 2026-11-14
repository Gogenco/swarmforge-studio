# SWARMFORGE — GAME-001 / DEV-002.2 / COMPILATION RECOVERY

> Studio coordination: [master GitHub issue #1](https://github.com/Gogenco/swarmforge-studio/issues/1),
> [team roles](docs/coordination/TEAM_ROLES.md),
> [VS-01 release gate](docs/release/VS01_GATE.md).
>
> This is the **DEV-002.2 source baseline**, not a finished game. Blocking
> QA2-01 chronological collision-vs-gate logic is awaiting ARCH-003; see
> [architect issue #2](https://github.com/Gogenco/swarmforge-studio/issues/2).
> The source is being introduced through a **draft PR**, pending Owner review.

**DEV-002.2 hotfix:** fixes the confirmed remaining Unity `CS1503` at
`Tests/EditMode/Vs01SimulationTests.cs:351`: `Does.Not.Contain(long)` was
being resolved against NUnit's **string** containment overload. The visual
selection test now calls the strongly typed `List<long>.Contains(long)` and
asserts its boolean result with `Is.True` / `Is.False`. Its verification
semantics (new ID 121 is present; deleted ID 1 is absent) are unchanged.
See **`DEV0022_OWNER_COMPILATION_RECOVERY.md`** for Unity re-test instructions.

**DEV-002.1 hotfix:** fixes Unity compiler errors `CS1503` at `MatchSnapshot.cs`
for `CommandHash` and `EventHash`, both `ulong`. The original signed `long`
hashing contract is retained; an unsigned overload hashes all 8 bytes without
truncation. One EditMode regression test checks the high-bit cases and method
signatures. No other gameplay code or balance values were changed relative
to DEV-002. **Unity compilation of this hotfix is pending Owner verification.**

See **`DEV0021_OWNER_COMPILATION_RECOVERY.md`** for the fastest Safe Mode
recovery procedure and a focused check list.

Unity **6000.3.28f1**, C#, URP **17.3.0**, offline/portrait Android+iOS target.
This archive is the **VS-01 bugfix source project**, not the subsequent VS-02/03/04.
No Forge, Mech, Titan, online services, shop or monetization have been added.

## Actual test evidence (keep DEV-001 separate from DEV-002)

- **DEV-001 / Owner's Unity 6000.3.28f1:** compilation PASS; EditMode **12/12 PASS**; PlayMode **1/1 PASS**; gameplay, fortress destruction, victory and restart PASS. This evidence was reported by the Owner **before** the DEV-002 code changes.
- **DEV-002 / authoring environment:** Unity Editor, Hub, `dotnet`, `csc`, Mono and mobile SDKs **not available**. **No DEV-002 Unity compilation, Editor tests, PlayMode tests, shader screenshot or mobile run was conducted here.**
- **DEV-002 / Owner's Unity 6000.3.28f1:** **compilation FAILED** with two confirmed `CS1503` errors (`ulong` cannot convert to `long`) in `MatchSnapshot.cs` lines 89–90.
- **DEV-002.1 / authoring environment:** the two call sites are supported by the new exact-type `ulong` overload; static verification only. Unity compilation, EditMode/PlayMode tests and gameplay **not run here**.
- **DEV-002.1 / Owner's Unity 6000.3.28f1:** prior `ulong` errors resolved, but compilation **FAILED** at one NUnit `Does.Not.Contain(1L)` assertion (CS1503: long to string).
- **DEV-002.2 / authoring environment:** NUnit assertion uses `List<long>.Contains(long)`; static verification only. Unity compilation and tests **not run here**.
- **DEV-002 / static:** `node tools/validate-project.mjs` verifies JSON, scene GUID references, assembly structure, lexical delimiter balance, spawn arithmetic and key regression markers. This is **not a substitute for Unity tests**.

## Start with the clean, corrected project

1. Extract ZIP with **Extract All** / **Извлечь всё**. In Unity Hub choose **Add project from disk** and select the *inner* `SWARMFORGE` folder containing `Assets/`, `Packages/`, `ProjectSettings/`. **Do not select the ZIP or the outer staging directory.** Use Unity **6000.3.28f1**.
2. Let Package Manager finish importing **Universal RP 17.3.0** and **Test Framework 1.6.0**. Open the Console, ensure no compile errors.
3. Run **SWARMFORGE → Configure VS-01 project (URP + portrait + battle scene)**. DEV-002 also attempts this URP initialization automatically when the Editor loads a project without a Graphics pipeline. The setup creates/loads:
   - `Assets/Swarmforge/Configs/Tuning/SWARMFORGE_Renderer.asset`
   - `Assets/Swarmforge/Configs/Tuning/SWARMFORGE_URP.asset`
   - an assignment in **Edit → Project Settings → Graphics → Default Render Pipeline**, and a matching override for the current Quality level.
   These two Unity-generated `.asset` files do **not** exist in the source archive until opened/configured with a real Editor. This is intentional. Save the Unity project after setup. **If the Editor warns about an existing non-URP pipeline, stop and ask the Tech Lead before replacing it.**
4. Select a **portrait** Game View aspect; open `Assets/Swarmforge/Scenes/Battle.unity`; press **Play**. Expected: three physically distinct strips **×2 | ×1 | ×3**, no magenta, compact nonoverlapping HUD, auto-fire, responsive touch/mouse lower-screen drag. Center ×1 produces no clones.
5. Destroy the fortress to show the isolated **VICTORY!** card (no HUD behind it) and use **RESTART**. In EditMode/PlayMode also verify a base-loss or timeout card. Switching out of the app pauses sim; return and press **RESUME** (no background catch-up).
6. Run **Window → General → Test Runner** → **EditMode → Run All** and **PlayMode → Run All**. Capture exact totals and results **for DEV-002**, not DEV-001; screenshot the Game View in a portrait layout. Then test Android/iPhone builds separately if device SDKs are installed.

If Unity reports missing/invalid `Input` support, open **Edit → Project Settings → Player → Active Input Handling** and select **Both** (or **Input Manager (Old)**), then restart the Editor if prompted. The current VS-01 bridge uses `UnityEngine.Input`.

## What DEV-002 changes

| ID | Correction | Unity acceptance |
| --- | --- | --- |
| BUG-01 | Pipeline-aware primitive shaders (`URP/Unlit` only if URP active, otherwise Built-in `Unlit/Color`); remove `TextMesh`; Editor creates/assigns URP asset. | No pink stripe in Game View; verify Graphics AND Quality settings. |
| BUG-02 | Scale by width **and** height, compact labels, separated rows and safe-area margins. | Text readable and not overlapping at common portrait ratios. |
| BUG-03 | Terminal overlay is an isolated full-screen UI state, replacing active HUD and controls. | VICTORY / loss / restart do not collide with HUD. |
| QA-01 | Exact x2/x1/x3 gate world zones match `GateForX`, `LaneForX` and the bot's recorded cannon X. | Visual strips and logical ×1/×2/×3 boundaries agree. |
| QA-02 | Idempotent URP setup via Unity Editor and Graphics/Quality assignments. | Confirm the generated `.asset` files and absence of incompatible shaders. |
| QA-03 | Include unit CannonX/BornTick/previous position, processed command hash and event hash in `StateHash`. | Replay hashes differ for divergent command histories; identical logs 30/60 FPS agree. |
| QA-04 | Immutable per-tick `GameEventBatch`, stable `LastEvents` plus match-scoped history. | Earlier event collections remain unchanged on later ticks. |
| QA-05 | Integer swept collision candidates with stable time/ID ordering. | Fast crossing destroys exactly one enemy and one bot. |
| QA-06 | Sticky visual selection by logical ID until unit leaves, capped at 120. | 120→121 logical bots does not halve visible meshes. |
| QA-07 | `OnApplicationPause` and `OnApplicationFocus` suspend the clock; explicit resume resets backlog. | No ticks run in background; foreground resumes cleanly. |

## Architecture retained

- `Assets/Swarmforge/Scripts/Simulation/Core/`: pure, Unity-independent 30-Hz integer simulation. First shot tick **0**, each 10 ticks through **1790** (180 originals on full-length match). Tick **1800** is final outcome/impact tick, no shot. Damage is single-application, base death wins the simultaneous tie (`LOSE`).
- `Scripts/Presentation/`: Unity-only 3D greybox, input, user interface, visual pooling/cap; no authoritative damage decisions.
- `Scripts/Infrastructure/`: versioned `LevelConfigAsset` bridge.
- `Scripts/Editor/`: Unity-only URP + portrait + battle-scene setup.
- `Configs/Levels/VS01_Arena.asset`: VS-01 test geometry (not final level design).
- `Tests/EditMode/` and `Tests/PlayMode/`: original tests **plus new regression cases**.

### Terminal priority

1. `BaseHP <= 0` → `LoseBase` even if fortress also falls in same tick.
2. Else `FortressHP <= 0` → `Win`, including the final tick.
3. Else tick 1800 → `LoseTimeout`.

### Limits and dependencies

The Unity-generated URP assets, actual visual rendering, runtime C# compilation, touch input, iOS/Android performance and new test counts **still require Owner/QA validation in Unity**. The 30 Hz contract remains unchanged. Level 3 geometry, Forge and economy are owned by other stages and intentionally untouched.

See `DEV002_OWNER_TEST_PLAN.md` for individual reproduction/verification steps and exact handoff status.

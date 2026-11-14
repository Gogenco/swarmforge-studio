# SWARMFORGE — GITHUB-006 / Developer Stage P1 Handoff

Status: **IMPLEMENTATION IN DRAFT / UNITY NOT TESTED**. Issue [#4](https://github.com/Gogenco/swarmforge-studio/issues/4), Owner [#18](https://github.com/Gogenco/swarmforge-studio/issues/18#issuecomment-6880563570). Baseline `main` = `99cfcdc0b4982e9e441cf23a8669a487a7d75ea9`.

## Exactly what P1 adds

- `V12BalanceDefinition`: version 2 pure-C# Inspector-serializable **trial-only** level, HP, enemy types, boxes and grouped waves. Validation rejects non-30Hz or invalid/mismatched references.
- `V12MatchConfig`: deep-copied, validated match configuration. Public accessors do not expose mutable internals. The stable FNV-1a `ConfigHash` covers every authored value in a fixed endian/field order, including nested arrays and their order.
- `V12ReplayIdentity`: v1.2 rule/schema/config compatibility guard. Legacy DEV-002.2 replay evidence is explicitly unversioned and cannot accidentally compare as v1.2 evidence. This **is not** a new simulation or complete replay executor.
- `V12BalanceConfigAsset`: optional ScriptableObject Inspector bridge. **Not linked to the Battle scene** or any existing `LevelConfigAsset`, so the DEV-002.2 runtime remains unchanged.
- `V12_HP2_Trial.asset`, `V12_HP3_Trial.asset`: editable, paired sample profiles differing only in MiniBot HP (and identifying name). **Neither robot HP value is final.**
- `V12BalanceConfigurationTests`: focused EditMode regressions for isolation, hashing, invalid config, profile parity and unchanged legacy hashes. They must be run in Unity by Owner/QA.
- `tools/validate-v12-p1.mjs`: dependency-free static sanity checks, **not** a C# compiler.

## Trial tuning and Owner decisions

In Unity Inspector select `Assets/Swarmforge/Configs/Trials/V12_HP2_Trial.asset` or `V12_HP3_Trial.asset`. Edit numeric fields and save the asset. For a future v1.2 trial, call `CreateMatchConfig()` to validate and freeze its values; do not mutate it after match start. Create a new config for subsequent tuned replays, never modify a running match. To compare HP2 and HP3 fairly, change **only** the `MiniBotMaxHP` and `ProfileId`; keep all wave groups, box data, inputs and seed equal.

Initial Owner-approved box settings are ×2/HP5/12s, ×3/HP10/10s, ×5/HP25/6s (`360/300/180` ticks at 30Hz). The sample 1-group wave, 100-HP test fortress, enemy HP/damage, 15-tick combat cooldown, X coordinates and hit radii are **hypotheses**, not approved runtime balance. P1 stores them as data only; there is **no gameplay behavior**.

## Explicit safety boundaries

1. Original `LevelDefinition`, `MatchState`, `MatchSnapshot`, `MatchSimulation`, `BattleEntryPoint`, `VS01_Arena.asset`, `Battle.unity` and existing tests **not edited**.
2. No gate removal, box combat, hit cadence, buff queue, collision chronology or enemy waves have been implemented; QA2-01 remains open.
3. No P2 until Architect posts `ARCH_V12_EXACT_HEAD_ALIGNED` against Designer PR #19 HEAD and Designer confirms matching trial contact rules; P1 tests must pass and Owner's GITHUB-006 gates remain effective.
4. No P3, VS-02, merge, or release declaration. Any P2 work requires a **separate Draft PR**.

## Verification to run

```sh
node tools/validate-project.mjs
node tools/validate-v12-p1.mjs
```

Then, in Unity **6000.3.28f1**, confirm `0` compile errors; run **all** EditMode and PlayMode suites (report real counts, not the earlier DEV-002.2 totals) and verify baseline Battle/Victory/Restart unchanged. Check both trial assets can be edited and produce different config hashes without affecting current Battle. Device performance and v1.2 playable mechanics have not been tested here.

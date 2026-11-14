# SWARMFORGE — DEV-002.1 / CRITICAL UNITY COMPILATION RECOVERY

**TASK ID:** `GAME-001/DEV-002.1`  
**STATUS:** `DEV-002.1_READY_FOR_OWNER_COMPILATION_TEST` (Unity runtime not available in developer environment).

## Exact confirmed root cause

In `Assets/Swarmforge/Scripts/Simulation/Core/MatchSnapshot.cs`, previous code exposed only:

```csharp
internal static void Hash(ref ulong hash, long number)
```

The same constructor calls `Hash(ref hash, s.CommandHash)` (line 89) and
`Hash(ref hash, s.EventHash)` (line 90). Both fields are **`ulong`**, and C#
deliberately has **no implicit conversion from `ulong` to `long`** (the unsigned
range exceeds `long.MaxValue`). Unity therefore stops with **two CS1503 errors**.

## Fix and why it is bit-exact

Added an overload `Hash(ref ulong hash, ulong number)` that processes all 8
bytes of the unsigned value directly using the existing FNV-1a routine.
The original `long` overload delegates to it through
`unchecked((ulong)number)`: for negative signed values, this intentionally
preserves the **same two's-complement 64-bit byte pattern** previously hashed
by DEV-002. It is an explicit width-preserving reinterpretation, not a
truncation. Thus existing signed inputs retain exactly the same replay hash.

The `ulong` overload is picked automatically for `CommandHash` and `EventHash`.
Every other call is `int` or `long` and still resolves to the existing `long`
overload; all call sites have been inspected. No change to tick order, 30 Hz,
damage, random state, level balance or gameplay was made.

## Exact modified files since DEV-002

1. `Assets/Swarmforge/Scripts/Simulation/Core/MatchSnapshot.cs` — overload + unchanged FNV hash bit-order.
2. `Assets/Swarmforge/Tests/EditMode/Vs01SimulationTests.cs` — one new test `HashOverloads_PreserveAllUnsignedBits_AndExistingSignedBitPatterns` checks overload signatures, `-1L`/`ulong.MaxValue`, `long.MinValue`/`0x8000000000000000UL`, and verifies the top bit affects hash.
3. `tools/validate-project.mjs` — static guard preventing accidental removal of either overload.
4. `README.md` — DEV-002.1 patch notes.
5. `DEV0021_OWNER_COMPILATION_RECOVERY.md` — this Owner handoff.

No original tests, mechanics or content have been removed. Existing `DEV002_OWNER_TEST_PLAN.md` remains applicable.

## Owner: repeat compilation in Unity 6000.3.28f1

1. Close the previous DEV-002 Unity Editor window or leave it untouched for comparison.
2. Extract **`SWARMFORGE_DEV0021_COMPILATION_RECOVERY.zip`** to a **new directory** using Windows **Extract All**.
3. In Unity Hub choose **Add project from disk** and select the *inner* `SWARMFORGE` directory with `Assets`, `Packages`, `ProjectSettings`.
4. Open with **6000.3.28f1**. Wait for package imports/compile to complete. If Unity offers Safe Mode again, choose Safe Mode, inspect the **first** red Unity Console error and share the exact script path and message. Do not assume other errors are fixed merely because these two are resolved.
5. Confirm **zero C# compilation errors**. Exit Safe Mode if applicable.
6. Run **SWARMFORGE → Configure VS-01 project (URP + portrait + battle scene)** (only after compilation). Open `Assets/Swarmforge/Scenes/Battle.unity`; press **Play**.
7. **Window → General → Test Runner:** run **EditMode** and **PlayMode** suites. Expected *discovered* test cases in this source: **24 EditMode / 2 PlayMode**; this is not a pass claim. Send the actual counts and any failures.
8. Check pink stripe, HUD, victory/restart, ×1/×2/×3 visuals and app pause per `DEV002_OWNER_TEST_PLAN.md`.

## Evidence and limitations

- Developer executed project structure/assembly/GUID/lexical C# validation, signature guard and ZIP integrity checks.
- **No Unity Editor/Hub/C# compiler is installed in the developer environment.** No Unity compilation or NUnit execution is claimed for DEV-002.1.
- Previous **DEV-001 12/12 + 1/1 PASS** evidence does not establish the health of DEV-002.1.
- The Owner's DEV-002 compile failure is confirmed by the exact two supplied Unity messages. Other potential compiler errors are **unverified**, not falsely ruled out.

**Next step:** Owner performs first compile in Unity 6000.3.28f1 and sends Console screenshots or test totals to the Game Developer / QA Engineer.

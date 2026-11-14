# SWARMFORGE — GAME-001 / DEV-002.2 — Compilation Recovery

**TASK ID:** `GAME-001/DEV-002.2`  
**STATUS:** `DEV-002.2_READY_FOR_OWNER_TESTING` — compilation still must run in Owner's Unity.

## Confirmed problem and exact cause

The Owner's Unity **6000.3.28f1** successfully moved past the DEV-002.1
`MatchSnapshot.cs` unsigned-hash errors, but found one new error:

`Assets/Swarmforge/Tests/EditMode/Vs01SimulationTests.cs(351,49)`
`CS1503: Argument 1 cannot convert from 'long' to 'string'`.

At the end of `VisualCap_120To121PreservesExistingSelectedIds`, the code
called `Assert.That(third, Does.Not.Contain(1L))`. NUnit's negated `Does.Not.Contain`
method exposes a **`string` substring** signature in this compiler context,
so passing a `long` fails to compile. This is a test-API overload mismatch;
**it is not a problem with simulation IDs or game damage logic**.

**Implemented exact-type solution:**

```csharp
Assert.That(third.Contains(121L), Is.True);
Assert.That(third.Contains(1L), Is.False);
```

`third` is `List<long>` returned by `StableVisualSelection.Choose(...)`;
`List<long>.Contains(long)` accepts exactly the element type and returns `bool`.
NUnit's boolean `Is.True`/`Is.False` constraints are unambiguous. The test still
checks **both** presence of new visual ID 121 and absence of removed ID 1.
No test was disabled or deleted.

## Other call-site compatibility checks

| Area | Check | Result |
| --- | --- | --- |
| EditMode NUnit | Scanned every `Assert.That`, `Does.*`, `Is.*` invocation and named signatures; no further `Does.Not.Contain(long)` or similar unchecked non-string `Does.Not.Contain` remains. | Static review only |
| Stable selection | `Choose(IReadOnlyList<UnitView>, IEnumerable<long>, int)` returns `List<long>`; `.Contains(121L)` and `.Contains(1L)` match the generic type. | Static review only |
| Hash | `Hash(ref ulong, long)` and `Hash(ref ulong, ulong)` cover all `int`, `long` and `ulong` arguments including EventHash/CommandHash. | Static review only; Owner reports earlier CS1503 errors gone |
| Reflection regression | `GetMethod(..., Type[] signature, ...)` resolves exact `(ref ulong, long)` and `(ref ulong, ulong)` overloads. | Static review only |
| EditMode asmdef | References `Swarmforge.Simulation`, `Swarmforge.Presentation`, NUnit test assembly. | JSON/assembly static PASS |
| PlayMode asmdef | References `Swarmforge.Presentation` and NUnit, retains 2 PlayMode scenarios. | JSON/assembly static PASS |
| Runtime/UI/URP | No changes to scene, graphics setup, input, game rules, 30 Hz clock or rendering from DEV-002.1. | Preserved; runtime remains unverified |

The Owner reported exactly **one** Unity Console error for DEV-002.1; that
error has been patched in source. The absence of additional errors can only
be confirmed when Unity recompiles DEV-002.2.

## Modified files since DEV-002.1

1. `Assets/Swarmforge/Tests/EditMode/Vs01SimulationTests.cs` — replace both collection `Does.Contain` assertions with strongly typed boolean membership checks.
2. `tools/validate-project.mjs` — static regression guard for incorrect NUnit overload usage.
3. `README.md` — new recovery notes.
4. `DEV0022_OWNER_COMPILATION_RECOVERY.md` — this handoff and compatibility audit.

**Unchanged:** all Simulation C# files, including the DEV-002.1 hash fix; all previous tests except those two assertion expressions; scene, balance, URP, UI, 30 Hz clock, visual selector, all DEV-001/DEV-002 archives.

## Owner test procedure in Unity 6000.3.28f1

1. Use Windows **Extract All** on `SWARMFORGE_DEV0022_COMPILATION_RECOVERY.zip` into a **new directory**; do not mix folders with DEV-002/DEV-002.1.
2. Unity Hub → **Add project from disk** → select the inner `SWARMFORGE` folder containing `Assets`, `Packages`, `ProjectSettings`.
3. Open in Unity **6000.3.28f1**, let Package Manager and script compilation complete. If it opens Safe Mode, examine the **first red Console error**. Send full path/line/message if present.
4. **Compilation gate:** Unity Console must report **0 C# compile errors**. Only then proceed to tests.
5. **Window → General → Test Runner** → EditMode **Run All**, then PlayMode **Run All**. There should be **24 EditMode / 2 PlayMode discovered cases** by source count; record actual discovered/passed/failed counts. This is **not a claim they pass**.
6. Open `Assets/Swarmforge/Scenes/Battle.unity` → **Play**. Check game launch, URP pink-stripe absence, HUD, ×1/×2/×3 boundaries, fortress destruction, Victory and Restart.
7. Send results and screenshots to Game Developer and QA Engineer before declaring DEV-002.2 `VERIFIED`.

## Verification completed locally

- Static project validator (`node tools/validate-project.mjs`).
- Static C# delimiter/assembly/GUID checks; manual signature and call-site review.
- Archive ZIP integrity test.
- **NOT RUN:** Unity Editor compilation, C# compiler, NUnit EditMode/PlayMode, gameplay on device. Unity Editor is absent from this developer environment.

**Next action:** Owner verifies DEV-002.2 in Unity. VS-02 remains prohibited.

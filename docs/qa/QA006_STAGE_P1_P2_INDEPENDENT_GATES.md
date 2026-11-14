# QA-006 — Independent Stage P1 / P2 acceptance gates (SWARMFORGE v1.2)

**Authority:** [Owner GITHUB-006 / Issue #18](https://github.com/Gogenco/swarmforge-studio/issues/18#issuecomment-6880563570) and [QA #9 GITHUB-006](https://github.com/Gogenco/swarmforge-studio/issues/9#issuecomment-6880587922).  
**Source baseline:** DEV-002.2 merged `main` `99cfcdc0b4982e9e441cf23a8669a487a7d75ea9` ([PR #10](https://github.com/Gogenco/swarmforge-studio/pull/10)).  
**Design reference exact HEAD:** [GDD Draft #19](https://github.com/Gogenco/swarmforge-studio/pull/19) `2089e646223c9f7ca2de001e00a09337251ccac6`.  
**Architecture reference exact HEAD:** [ARCH-v1.2 Draft #23](https://github.com/Gogenco/swarmforge-studio/pull/23) `33d45659ec072523b6a709677fe827c9f9ab606e`.  
**Existing QA plans:** [legacy QA-003 Draft #11](https://github.com/Gogenco/swarmforge-studio/pull/11) and [GDD12 QA-005 Draft #20](https://github.com/Gogenco/swarmforge-studio/pull/20).  

**Execution status:** `PREPARED / NO P1-P2 CODE OR TEST RESULTS REVIEWED`. This is a versioned QA gate checklist, **not** approval of code, balance, runtime, or release.

## 0. Owner's exact stage boundary

| Gate | Authorized implementation | QA stance |
|---|---|---|
| **P1 — NOW** | Isolated new Developer branch/ Draft PR: versioned editable balance config, HP2/HP3 equal-comparison presets, immutable per-match snapshot + config hash, opt-in v1.2 ruleset seam, replay/config compatibility harness, focused regression tests. | May conduct exact-HEAD independent code review and run available checks **when P1 PR exists**. No gameplay sign-off inferred. |
| **P2 — AFTER THREE DEPENDENCIES** | One temporary 60s arena, one-thumb auto-fire, friendly HP2/HP3, exactly one ×2 box with HP5, timed ×2 12s, one simultaneously spawning enemy group, fortress/base/restart and replay. | **BLOCKED** until Architect says `ARCH_V12_EXACT_HEAD_ALIGNED`, Designer signs the same deterministic trial combat contract, and P1 tests actually pass. |
| **P3 — NOT AUTHORIZED** | Full ×3/×5, multi-box queue/boss/five-wave rollout. | Do not claim these tests executed in P1/P2 or require them as P2 implementation scope; preserve future [QA-005](https://github.com/Gogenco/swarmforge-studio/pull/20) coverage. |
| **Release / merge** | Not authorized. Legacy baseline unchanged. | QA-003 / QA-006 cannot sign off VS-01 or VS-02; Owner controls merges/release. |

### Mandatory P2 STOP rules

1. Architecture exact-head reconciliation of GDD #19 `2089e646...` vs ARCH #23 `33d45659...` missing or inconsistent.
2. Designer and Architect do not explicitly agree on provisional contact damage/cooldown, survivor movement, same-tick enemy-versus-box priority, box reward/queue entitlement and precise timer boundaries.
3. P1 config/replay tests fail or were not actually run, or P1 scope modifies legacy gameplay.
4. Double damage loops, disappearing earned boost, replay divergence, unbounded contact loops or unbounded memory/CPU in a build.
5. Code deletes legacy gate/QA2-01 fixtures, overwrites original L3, modifies stable `main`, adds VS-02 or mixes P3 mechanics into P2.

Report exact `BLOCKED` reason and stop the affected progression without guessing missing rules.

## 1. Stage P1 — independent configuration/replay checklist (planned, NOT RUN)

| ID | Priority | Test procedure | Objective expected outcome / evidence |
|---|---|---|---|
| P1-Q01 | P0 | Verify branch ancestry against `main` `99cfcdc...` and complete changed-file list. | Isolated P1-only Draft PR, no direct main edit, no deletions of legacy scene/tests/gate code. |
| P1-Q02 | P0 | Load each schema version and inspect all tunable parameters. | Explicit `RulesetVersion`, balance schema/version, preset, 30 Hz, scene/level IDs, HP/damage/speed, box HP, durations, wave lists, identifiers; no hidden code constants for approved initial tuning. |
| P1-Q03 | P0 | Instantiate a match, then mutate source Inspector/config object and start another match. | First match retains frozen deep config and original hash; second match receives new version/hash only through explicit configuration selection. No shared array/object aliasing. |
| P1-Q04 | P0 | Compare HP2 and HP3 full canonical snapshots after stripping ONLY friendly maxHP field. | Every other authoritative value equal: waves/enemy, damage/cadence, box/timer, geometry, seed, inputs. Neither HP is silently assigned final status. |
| P1-Q05 | P0 | Reconstruct canonical config snapshot/hash twice from logically identical data with different dictionary or serialized insertion ordering. | Identical stable fingerprint; no `GetHashCode()`, Unity Object instance IDs, renderer frame time or unstable collection order used. |
| P1-Q06 | P0 | Change one authoritative field, re-run hash and replay registration. | Different config fingerprint; replay includes version/preset/hash, refuses silent cross-version interpretation. HP2 vs HP3 is **not expected** to have identical StateHash between presets. |
| P1-Q07 | P0 | Try invalid data: HP0/negative, damage0 where forbidden, cooldown0, duration negative/nonintegral, unsupported ruleset, missing archetype, duplicate IDs, enormous tick arithmetic. | Deterministic explicit validation errors; no silent coercion, overflow, partial match start or crash after mutation. Exact accepted bounds from Architect. |
| P1-Q08 | P0 | Start LegacyVs01V1 and opt-in BoxCombatV12 fixture independently. | Old 30-Hz DEV-002.2 behavior and replay namespace remains selectable; v1.2 rules do not auto-enable on old scene/save. |
| P1-Q09 | P0 | Replay identical input/tick commands at 30/60 render FPS, irregular delta and after process reload (if harness supports). | Within the same preset and ruleset: identical ordered events, stable IDs, StateHash/EventHash/CommandHash and terminal state. |
| P1-Q10 | P0 | Restart match and duplicate input replay. | Fresh match IDs and counters, no cross-match config cache leak, duplicate/stale inputs handled as baseline specifies. |
| P1-Q11 | P1 | Review C# asmdefs, GUID references and package/editor/runtime boundaries; validate structural script where available. | Compilable graph, no accidental UnityEditor code in runtime assembly, tracked .meta and serialized assets; structural validation PASS **does not equal** Unity compile. |
| P1-Q12 | P0 | Execute original + new EditMode/PlayMode tests on exact SHA with Unity 6000.3.28f1 if available. | Record actual test names/count/PASS/FAIL and XML/log; historical Owner 24/24+2/2 is **baseline only**, not new P1 verification. |
| P1-Q13 | P1 | Compare byte-for-byte source diff to approved P1 scope and test plan. | No combat/box/boost/runtime changes beyond explicitly necessary additive test seams; no unapproved P2/P3 code. |
| P1-Q14 | P1 | Inspect preset values after serialized round-trip and clean import. | Version, HP2/HP3 and numeric fields survive Unity serialization unchanged; asset .meta GUIDs unique. |

**P1 gate disposition:** `PASS` only if independently verifiable code/config tests and all mandatory assertions pass; otherwise `FAIL`, `BLOCKED` (missing code or approval), or `STATIC_ONLY` (source audit but no executable Unity). Owner runtime evidence and independent QA must be separate.

## 2. Stage P2 — first playable trial checklist (CONDITIONAL, NOT RUN)

**Entry gate:** Architect publishes `ARCH_V12_EXACT_HEAD_ALIGNED` for exact GDD/ARCH heads; Designer provides explicit signed trial combat/box timeline contract; P1 PR exact-HEAD tests pass. Only then QA may assess actual P2 gameplay implementation.

| ID | Priority | Scenario | Objective expected result / evidence |
|---|---|---|---|
| P2-Q01 | P0 | Open the opted-in 60s trial arena with each HP2 and HP3 preset. | No legacy gate auto-multiplier, old DEV-002.2 arenas untouched; game starts/runs 1800 logical ticks plus signed terminal boundary. |
| P2-Q02 | P0 | One-thumb steer left/right and continuously auto-fire through box/defense lanes. | Responsive on portrait touch + mouse; raw ATTACK command history stable and input cannot be blocked by wave UI. |
| P2-Q03 | P0 | Hit the single ×2 box five times with eligible MiniBots. | Box starts at HP5, each successful hit costs exactly one bot regardless of bot combat HP, decreases box HP by 1; no phantom/duplicate box damage. |
| P2-Q04 | P0 | Final box hit arrives exactly when another hit/enemy contact is possible. | One box destroyed, one reward granted, deterministic collision timeline and signed same-time tie; no duplicate reward or stranded reservation. |
| P2-Q05 | P0 | Activate ×2 and fire at ticks before/at/after the 12s boundary. | Time-only ×2 for the signed 360-tick interval, no quota, no retroactive in-flight multiplication; exact k/k+1 semantics per signed ARCH. |
| P2-Q06 | P0 | Simultaneous enemy group appears according to authored spawn tick. | More than one enemy in the scheduled group, stable IDs/positions, correct common spawn tick, no one-by-one waterfall. |
| P2-Q07 | P0 | Two identical enemy contacts with survivor HP2 and HP3 trials. | Each initial/repeated hit uses signed damage and strictly positive cooldown; survivor retains HP/ID; no render-frame repeated damage or accidental instant death. |
| P2-Q08 | P0 | Surviving combat MiniBot leaves engagement after enemy dies. | Follows signed progression/resume rule, can fight again; no same-tick infinite retarget/hit loop. |
| P2-Q09 | P0 | High-speed enemy/box interaction crosses within one tick. | Exact swept TOI; earlier physical event wins; equal-time as Architect/Designer signed; old QA2-01 remains separate legacy regression. |
| P2-Q10 | P0 | Fortress killed, base killed, simultaneous loss and timeout. | Correct single terminal outcome, unique event and frozen state; loss on simultaneous friendly-base/fortress death if retained in signed v1.2 contract. |
| P2-Q11 | P0 | Restart after win/loss and switch HP2↔HP3. | No stale queue/HP/events/config pointers, correct preset hash and fresh IDs; prior match unchanged. |
| P2-Q12 | P0 | Same scripted wave/input under HP2 and HP3, each at 30 and 60 render FPS. | Per-preset deterministic replay, HP2/HP3 differences traceable only to preset; collect base/fortress HP, time to kill, survivor counts, win reason. |
| P2-Q13 | P1 | Group-wave stress with peak logical MiniBot population and visible cap. | No logical bots deleted for visual budget; memory/GC/FPS/frametime recorded on named devices (Performance #8), no fabricated performance PASS. |
| P2-Q14 | P1 | Simulated background/resume, resize and Android/iOS device suspend. | No lost/extra ticks, no repeated reward/breach, no UI lockup; label unavailable device as NOT TESTED. |
| P2-Q15 | P2 | 320×568, 360×640 and 390×844 portrait UI with one box and group wave. | Readable HP, base/fortress/time, visible target and warnings; no overlap on safe areas or blocked input. |
| P2-Q16 | P0 | Clean checkout and import on exact SHA, EditMode+PlayMode runner and a real 60s game capture. | Record Console errors, TestResults XML/screenshot, Unity version, exact commit and build target; no PASS from a source-only assertion. |

**Not P2 exit criteria:** ×3/×5 boxes and timers, two-slot queue under a third reward box, bosses, full five waves, full audio/art/UI rollout. Those remain P3/out of authorized P2 scope and are covered by [QA-005](https://github.com/Gogenco/swarmforge-studio/pull/20) for later execution. This is **not** permission to silently delete those Owner-approved future requirements.

## 3. Runtime evidence ledger and QA decision

For each executed scenario:
`Test ID | exact PR HEAD SHA | GDD/ARCH sign-off SHA | ruleset/preset/config hash | tick/seed/input log | expected vs actual | unit/box/enemy IDs | state/event/command hashes | Unity XML/console/screenshot | device+OS+resolution | PASS/FAIL/NOT TESTED | severity and assigned owner`.

A static analysis or Node validator PASS is explicitly **not** a Unity compiler or PlayMode PASS. Screenshots and verbal Owner test outcomes are external evidence until independently reproduced by QA.

**Current verified facts (GITHUB-006 planning checkpoint):**
- Owner GITHUB-006 conditionally authorizes **Developer P1 only now**; P2 remains hard-gated.
- The last checked GitHub PR list showed GDD #19 and Architect #23 still Draft, with explicit unresolved exact-head alignment.
- No P1 Developer PR, build artifact or P1 Unity TestResults was available **at checklist preparation**; recheck live GitHub immediately before any later execution.
- QA2-01 in DEV-002.2 remains OPEN; old [QA-003 #11](https://github.com/Gogenco/swarmforge-studio/pull/11) maintained.

**Next QA action:** On first P1 Draft PR, pin exact SHA, review changed files and execute P1-Q01..14 where environment allows, report `QA006_P1_PASS / QA006_P1_FAIL / QA006_P1_STATIC_ONLY / QA006_P1_BLOCKED`; never trigger P2 on assumptions. After P2 entry gates, repeat against the distinct P2 exact HEAD. No merge or VS-02.

# SWARMFORGE — AUDIO-001 / VS-01 Event & Audio Design Specification

**Document status:** PROPOSED / READY_FOR_OWNER_REVIEW (design only)  
**Role owner:** 07 — Senior Sound Designer / Audio Engineer  
**Issue:** [#7 — AUDIO-001](https://github.com/Gogenco/swarmforge-studio/issues/7)  
**Master coordination:** [#1 — GAME-001](https://github.com/Gogenco/swarmforge-studio/issues/1)  
**Code baseline:** DEV-002.2, main merge commit \`99cfcdc0b4982e9e441cf23a8669a487a7d75ea9\`  
**Target runtime:** Unity 6000.3.28f1, 30 Hz deterministic simulation, portrait Android/iOS  
**Scope:** Audio contract, asset inventory, mix/playback budgets, QA design. **No runtime code, no sound assets, no procurement.**

## 1. Source-of-truth evidence and constraints

Reviewed baseline:
- \`Assets/Swarmforge/Scripts/Simulation/Core/MatchState.cs\`: \`GameEventKind\`, \`GameEvent\`, \`GameEventBatch\`, \`MatchOutcome\`.
- \`Assets/Swarmforge/Scripts/Simulation/Core/MatchSimulation.cs\`: authoritative \`AdvanceOneTick()\`, \`LastEventBatch\`, \`Publish()\`, \`LastEvents\`.
- \`Assets/Swarmforge/Scripts/Presentation/BattleEntryPoint.cs\`: \`RestartMatch()\`, \`Update()\`, \`Suspend()\`, \`ResumeFromSuspend()\`.
- \`Assets/Swarmforge/Scripts/Simulation/Core/SimulationClock.cs\`: multiple ticks can advance in a single rendered frame (currently up to eight).
- \`Assets/Swarmforge/Configs/Levels/VS01_Arena.asset\`: \`TickRate: 30\`, \`ShotPeriodTicks: 10\`, \`FirstShotTick: 0\`.
- [#2 — ARCH-003](https://github.com/Gogenco/swarmforge-studio/issues/2) and [#4 — DEV-003](https://github.com/Gogenco/swarmforge-studio/issues/4) define a **pending** chronological event-order fix (QA2-01 HIGH). The current source's gate/contact ordering is not accepted.

**Architectural invariant:** gameplay simulation is authoritative; audio is a best-effort, non-authoritative presentation consumer. Audio must never call \`Submit()\`, alter entities, reorder published events, affect RNG/state hashes or block \`AdvanceOneTick()\`. No audio code belongs in the Simulation assembly. Mixing/voice limits may drop audible effects but must not drop/alter simulation events.

**Implementation is NOT authorized by this document.** Sound acquisition, procedural generation, integration and any change to Developer-owned presentation code require a separate Owner decision. This spec may need a revision when ARCH-003 is signed and DEV-003 event contract is final.

## 2. Authoritative event-to-sound map

The payloads in this table reflect **current DEV-002.2 source**, not a new contract forced on DEV-003.

| Source event / UI transition | Observed event semantics | Proposed sound / action | Decision, precedence and notes |
| --- | --- | --- | --- |
| \`RawShot\` | \`SourceId = MiniBot Id\`, \`Value = 1\`; current test level fires each 10 ticks | \`sfx_autoshot\` | One per accepted event subject to voice cap; initial shot at tick 0 must be audible. |
| \`GateMultiplied\` | \`SourceId = original MiniBot Id\`, \`Value = gate multiplier\` | \`sfx_gate_x2\` or \`sfx_gate_x3\` | **No multiply sound for \`Value <= 1\`** (neutral ×1 currently emits an event). One cue per parent event, not per spawned clone. |
| \`EnemyDefeated\` | \`SourceId = MiniBot Id\`, \`TargetId = Enemy Id\` | \`sfx_contact\` | Primary cue for paired contact. The related \`MiniBotLost\` MUST NOT trigger a second collision cue. |
| \`MiniBotLost\` | Paired with \`EnemyDefeated\` for the same contact | None in VS-01 | Silent companion event. Do not double-fire. |
| \`FortressDamaged\` | \`SourceId = MiniBot Id\`, \`Value = remaining FortressHP\` | \`sfx_fortress_hit\` | Rate-limit/aggregate when many impacts occur in one tick. \`Value\` is **remaining HP**, not damage amount. |
| \`FortressStageChanged\` | \`Value = new stage\` | None in minimal VS-01 | Visual stage change already communicates progression; optional future variation must not duplicate hit sound. |
| \`BaseBreached\` | \`SourceId = Enemy Id\`, \`Value = remaining BaseHP\` | \`sfx_base_damage\` | Important warning. If this batch also ends in defeat, the terminal cue has priority. |
| \`MatchEnded\` | \`Value = (int)MatchOutcome\`: \`Win=1\`, \`LoseBase=2\`, \`LoseTimeout=3\` | \`stinger_victory\` or \`stinger_defeat\` | Exactly one terminal cue per match, higher priority than all transient SFX; treat both loss modes as defeat. |
| \`EnemySpawned\` | Each enemy spawn event is per unit | None in VS-01 | Silent to avoid wave-start spam. |
| \`CannonMoved\` | \`Value = CannonX\` | None in VS-01 | Do not emit sound for continuous pointer movement. |
| UI Restart successful | \`RestartMatch()\` creates new simulation and increments local \`matchSequence\` | \`ui_restart\` | UI-only event after accepted restart; not a new simulation GameEvent. Do not duplicate on scene initialization. |
| UI Resume successful | \`ResumeFromSuspend()\` called while suspended | \`ui_resume\` | UI-only accepted action; no sound on ordinary OS foreground auto-focus. |
| Explicit UI Pause (if later added to VS-01) | No manual Pause button today; current \`Suspend()\` is invoked on OS pause/focus loss | \`ui_pause\` (optional) | **Do not** play on OS backgrounding. Stop/fade audio there; only play if user deliberately taps an approved Pause control. |

**No background music is required for minimal VS-01.** Dedicated music, full 3D spatial audio and richer content are outside AUDIO-001. No Forge/Mech/Titan or VS-02 scope is being designed or implemented here.

## 3. Delivery contract (proposed integration; requires Developer/Architect review)

1. After **every** successful \`sim.AdvanceOneTick()\`, immediately capture \`sim.LastEventBatch\` and hand its immutable event list to a presentation-only audio adapter. This includes \`RestartMatch()\`'s initial **tick 0**.
2. Do not poll \`LastEventBatch\` only once at the end of \`SimulationClock.Advance(...)\`: multiple simulation ticks can occur in one rendered frame; only the final batch would survive. Processing each callback avoids lost events.
3. A batch with \`Tick < 0\` is initialization evidence, **not an audio event**. Ignore it.
4. Prefer the input enumeration index for event identity: \`(MatchId, Tick, index-in-GameEventBatch.Items)\`. \`CumulativeHash\` / \`EventHash\` is an integrity value, **not** a unique individual event key.
5. Consumer tracks the active \`MatchId\` and highest consumed tick, and handles each batch **at most once**. A repeated delivery of the same batch is silent. A new \`MatchId\` resets per-match event tracking, active voices and terminal latch. Do not leak sounds from the prior match after Restart.
6. Do not fabricate effects by comparing counts in \`MatchSnapshot\` or scanning rendered bots. Render throttling/visual cap must not alter the sound-event stream.
7. Coalesce/limit multiple sounds only **after** event consumption. A lower-priority audible cue may be suppressed without changing event counts, history, hashes or replay outcomes.
8. Until ARCH-003/DEV-003 finalizes chronology, take published \`GameEventBatch.Items\` order as baseline evidence only; do not assume gate-before-contact is correct. If sub-tick presentation timing is later required, request a versioned, read-only presentation timing field from Architect/Developer rather than infer or mutate the sim.
9. In a terminal batch, dispatch exactly one terminal stinger. Suppress or duck conflicting base/fortress impact tails so the outcome is clear. No win/lose stinger may be inferred from mere scene load.
10. If the application suspends or loses focus, pause/stop SFX and do not fast-forward missed audio on resume. \`clock.Reset()\` preserves simulation policy; audio must not trigger catch-up simulation.

**Important ownership:** \`BattleEntryPoint.cs\` is currently Developer-owned Presentation code. Only the Developer, after explicit Owner approval, should wire \`AdvanceOneTick()\` to a proposed Audio adapter. This document requests no code changes.

## 4. Mobile mixing and playback constraints (proposed budgets, NOT measured)

| Parameter | Proposed VS-01 baseline | Rationale / verification |
| --- | --- | --- |
| Master / SFX / UI controls | Master + SFX slider/mute; optional UI slider; default enabled | Persist user preference when implementation is approved; confirm accessibility UX with UI designer. |
| Global simultaneous voices | Soft cap **8**, including UI and terminal | Starting target for low-end phones; profile on real Android/iOS hardware. |
| Per-category voices | Raw shot 2, gate 2, contact 2, damage 2, UI 1, terminal 1; subject to global 8 | Prevents mass battles drowning the scene. |
| Voice admission | Terminal > base damage > fortress damage > contact > gate > UI > raw shot; accepted UI interaction may override combat bed | Priority voice-steal oldest lower-priority clip; never restart same terminal stinger. |
| Repetition/coalescing | One gate cue per approximately 2 sim ticks per gate class; contact and damage collisions grouped by tick/frame; no terminal suppression | Timing values are tuning proposals; event identity still consumed exactly once. |
| 2D/3D positioning | UI and terminal sounds non-spatial; battle SFX centered/very mild stereo pan | Avoid loud positional jumps through tiny phone speakers. Spatial sound can be evaluated later without affecting gameplay. |
| Streaming | No streaming for short SFX; preloaded compact clips | Limits I/O latency during combat. |
| Compression | Source master \`WAV PCM 16-bit / 44.1 kHz\`; Unity mobile import type/codec to be profiled | Proposed delivery convention only; choose platform codecs by audition, decode CPU and memory tests. |
| Peak / mix | No digital clipping; clear terminal and damage cues at low phone volume; avoid harsh high-frequency transients | Final LUFS/peak values determined by listening tests, not assumed from source. |
| Background/focus | Silence/fade on OS background; resume only when foregrounded and permitted by player setting | No ghost audio and no automatic replay of old one-shots. |

**Estimated impact:** around a dozen short mono clips and 8 voices is expected to be a small audio footprint, but **no assets exist and no RAM/CPU/FPS/latency numbers have been measured**. Raw 44.1kHz/16-bit mono PCM occupies about 88.2 kB per second *before import/compression and overhead*. Proposed acceptance budgets, to be validated on devices: audio clips plus buffers ≤ 2 MiB attributable memory for minimal SFX; no audible stutter or clipping; trigger-to-audible latency target ≤ 100 ms for interactive UI feedback under normal load. These are design targets, not current results.

## 5. Minimum sound asset inventory (NOT produced or acquired)

| Proposed logical ID | Source file proposal | Duration | Channels | Variants / intent |
| --- | --- | --- | --- | --- |
| \`sfx_autoshot\` | \`vs01_autoshot_01.wav\` | 0.08–0.16 s | mono | 2 subtle variants eventually; soft synthetic tick/laser |
| \`sfx_gate_x2\` | \`vs01_gate_x2_01.wav\` | 0.12–0.25 s | mono | brighter two-step pulse |
| \`sfx_gate_x3\` | \`vs01_gate_x3_01.wav\` | 0.15–0.30 s | mono | three-step elevated pulse |
| \`sfx_contact\` | \`vs01_contact_01.wav\` | 0.07–0.22 s | mono | 2 variations eventually; small metallic impact |
| \`sfx_fortress_hit\` | \`vs01_fortress_hit_01.wav\` | 0.12–0.30 s | mono | controlled armor thump |
| \`sfx_base_damage\` | \`vs01_base_damage_01.wav\` | 0.15–0.40 s | mono | readable danger alert, non-piercing |
| \`stinger_victory\` | \`vs01_victory_01.wav\` | 1.0–2.5 s | mono or stereo | distinct positive cadence |
| \`stinger_defeat\` | \`vs01_defeat_01.wav\` | 1.0–2.5 s | mono or stereo | brief subdued defeat |
| \`ui_restart\` | \`vs01_ui_restart_01.wav\` | 0.08–0.20 s | mono | clean confirmation |
| \`ui_resume\` | \`vs01_ui_resume_01.wav\` | 0.08–0.15 s | mono | unobtrusive transition |
| \`ui_pause\` | \`vs01_ui_pause_01.wav\` | 0.08–0.15 s | mono | optional; only after manual Pause is approved |

Proposed future asset location (after separate authorization): \`Assets/Swarmforge/Audio/VS01/\`. Do not commit source licenses without confirming redistribution rights; do not procure/download third-party assets under this issue.

## 6. Accessibility, mute and player expectations

- Sound is supplementary. Victory/Defeat, health, damage, gate multiplier and Pause must remain visually understandable when **all audio is muted**.
- Respect OS output route, hardware volume and user mute; never force-play audio when user disabled SFX.
- No mandatory headphones; check narrow-band phone speakers and quieter playback.
- Avoid startling full-scale peaks, piercing alarms and very high-frequency repeated shots; especially important when auto-fire occurs roughly **3 times per second** under the current test level.
- No audio from a hidden/background app; ensure suspension and UI transitions do not leave looping/queued playback.
- Audio labels are stable logical IDs, not localized spoken dialogue; localization not required for this minimal sound list.

## 7. Proposed QA / acceptance matrix (none executed)

| Case | Expected result | Required evidence / owner |
| --- | --- | --- |
| Startup first tick | Exactly one permitted \`RawShot\` cue at tick 0 | Developer instrumentation + QA Unity |
| Multi-tick frame (2–8 ticks) | All event batches consumed in order, no missing/duplicate effects | DEV integration unit/test hook |
| Same batch delivered twice | No second audible event | Audio adapter unit test |
| New match after Restart | Old voices and dedup state cleared, one UI restart, new tick-0 shot audible | QA PlayMode |
| ×1 neutral gate | No multiplier SFX despite \`GateMultiplied(Value=1)\` | Adapter unit test |
| ×2 / ×3 gates | Appropriate single cue per source event, not per clone | Adapter unit test |
| Enemy collision | \`EnemyDefeated + MiniBotLost\` produces one contact cue | Adapter unit test |
| Many impacts in one tick | Bounded voices, no clipping, unchanged game-event hashes | QA stress + Profiler |
| Base breach + terminal loss | One Defeat stinger, no duplicate terminal audio, damage cue is secondary | QA PlayMode |
| Win / timeout loss | Win and LoseTimeout resolve to correct single stingers | QA PlayMode |
| App pause, focus loss and resume | Background silenced; no queued catch-up audio; optional Resume UI feedback only on accepted button | QA Android/iOS |
| Mute / device volume / speakers | No mandatory audio, UI remains usable; no forced playback | QA accessibility/device matrix |
| ARCH-003 chronological cases | Audio ordering follows corrected authoritative ordered event batch; no sound-induced change to state/replay hash | QA after #2/#4 |
| Low-end device | Meet proposed voice, memory and perceived latency targets | Performance Engineer + QA, real devices |

Test evidence **currently unavailable** for every row. Static source inspection supports the design mapping only. The Owner-reported DEV-002.2 EditMode 24/24 and PlayMode 2/2 are **not audio tests**. No Unity Editor, Android, iOS or audio playback was exercised by this audio task.

## 8. Dependencies, open decisions and release gate

1. **Architect #2 / ARCH-003:** confirm source event order, stable ID/tie policy and whether published \`GameEventBatch\` schema changes. Audio consumes results only.
2. **Developer #4 / DEV-003:** implement chronological collision/gate semantics separately; on a future audio-implementation approval, wire the ordered batch handoff for **every** advanced tick, including tick 0, without rewriting simulation.
3. **UI/UX #6:** approve audio setting/mute interaction and whether VS-01 will expose a manual Pause button; current auto-suspend is not an intentional button action.
4. **Performance #8:** measure SFX voice cap, import/decode memory, CPU spikes and latency on target phones after actual assets exist.
5. **QA #9:** independently test mapping, dedup, OS lifecycle, mute and device behavior **after** authorized implementation.
6. **Product Owner:** approve/revise this specification, then separately approve asset production/acquisition and integration. **No self-merge.**

**Proposed spec acceptance:** Owner and Architect acknowledge event contract, Game Developer confirms integration seam, UI owner confirms mute/pause semantics and Performance/QA accept the test budgets. Until then status is \`READY_FOR_REVIEW (SPEC ONLY)\` with all runtime/audio asset work \`NOT_STARTED\`.

**Release remains blocked:** QA2-01 and the existing VS-01 release gates in [master #1](https://github.com/Gogenco/swarmforge-studio/issues/1) are unresolved. This document does **not** assert that VS-01 is ready, nor authorize VS-02.

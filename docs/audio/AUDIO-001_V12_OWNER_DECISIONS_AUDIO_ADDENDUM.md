# SWARMFORGE — AUDIO-001 / GDD v1.2 Owner-Decision Audio Addendum

**Status:** PROPOSED / READY_FOR_REVIEW — documentation and test planning only  
**Owner:** 07 Senior Sound Designer / Audio Engineer  
**Primary work item:** [#7 AUDIO-001](https://github.com/Gogenco/swarmforge-studio/issues/7)  
**Owner direction:** [#18 GDD v1.2 / GITHUB-005](https://github.com/Gogenco/swarmforge-studio/issues/18)  
**Cross-role GDD:** [Draft PR #19](https://github.com/Gogenco/swarmforge-studio/pull/19) (being revised; do not treat stale hypotheses as approved)  
**Baseline:** DEV-002.2 on main, `99cfcdc0b4982e9e441cf23a8669a487a7d75ea9`  
**Prior audio baseline:** [Draft PR #12](https://github.com/Gogenco/swarmforge-studio/pull/12), preserved unchanged  
**Scope:** Sound/haptic design, deterministic presentation event contract request, asset planning and test matrix. **No Unity code, no assets, no procurement, no merge, no VS-02.**

## 1. What the Owner has actually approved

These are the design decisions in #18 as of GITHUB-005. They define **audio trigger intent**, but are not an authorization to ship audio.

| Decision | Approved behavior | Audio consequence |
| --- | --- | --- |
| O12-01 | Every successful attack on a side upgrade box **consumes one MiniBot** and reduces box HP by 1; no new projectile mode | Box-hit cue is caused by **accepted damage**, not cannon proximity or aiming; each consumed bot must not generate an additional death cue for the same hit. |
| O12-05 | Newly authored v1.2 arenas replace always-on ×1/×2/×3 gates with breakable boxes | v1.2 sounds come from box-hit/box-break/reward events. Preserve legacy gate sounds/tests for DEV-002.2 until a separately approved migration. |
| O12-06 | Initial multiplier-box HP: ×2=**5**, ×3=**10**, ×5=**25** | 5/10/25 successful hits are a **balance parameter**, not a count of simultaneous full-volume sounds. Play a bounded, subtle hit texture plus one break cue. |
| O12-03 | Time-only boosts after **activation**: ×2=**12 s**, ×3=**10 s**, ×5=**6 s**; no shot quotas | Audible activation/expiry follows **authoritative transitions**, not a client-side timer or raw-shot count. At 30Hz nominal durations are 360/300/180 ticks; boundary policy remains Architect-owned. |
| O12-04 | Multipliers do not stack; **one active + one pending**, activated sequentially | Earned/queued vs activated/expired must be distinguishable. Do not play a second “activated” sound merely when a queued reward is earned. |
| O12-04 queue | When active and pending slots are full, other multiplier boxes are **locked/untargetable**; preserved box progress; no earned reward silently lost | Locked-box UI is predominantly visual. A quiet reject cue is optional only for an explicit invalid user interaction; **no audio every frame or when a bot passes a locked box**. |
| O12-02 | Friendly MiniBots survive some enemy contacts; successful box hit consumes MiniBot regardless of combat HP | Nonlethal hit, armor impact, death and box-consumption must be separate event meanings. Baseline “every contact kills both” assumptions are superseded for v1.2. |
| Wave grouping | Enemy drones spawn in **simultaneous groups** and escalate across waves; bosses may be accompanied | Group/wave cues trigger once per **group/wave transition**, not once per spawned enemy. No overlapping alarm storm for 10 enemies arriving together. |
| Wave UI | Nonblocking countdown/`WAVE n/N`, boss and base-under-attack warnings | Short distinct sounds; controls and visual alerts remain available. Optional low-intensity user-controlled haptics. |
| Prototype tuning | MiniBot combat HP is **not final** (compare HP=2 and HP=3 playtest presets). HP/damage, wave/group sizes, box HP and boost time must stay editable/versioned | Audio thresholds **must not hardcode** HP=2 or 3, fixed wave sizes or clip-count assumptions based on a chosen preset. Compare audio intelligibility across the same matched trials. |

**Not yet approved:** exact MiniBot/combat damage & repeat-hit cadence, enemy HP/archetypes, wave timings/group sizes, boss mechanics, exact intra-tick collision priority, boost time boundary/transition tie policy, shield/weapon/fire-rate queue interactions, spatial field/box geometry and first playable implementation scope. Proposed audio names below are **requests**, not existing API.

## 2. Event-to-audio contract requested from Architect and Developer

The current DEV-002.2 `GameEventBatch` contains `MatchId`, `Tick` and ordered `Items`; `GameEvent` contains `Kind`, `SourceId`, `TargetId`, `Value`. No v1.2 event kinds below currently exist on main. Architect must approve a versioned, typed/read-only event representation; do **not** stuff unrelated IDs/HP/timer data into an ambiguous `Value` without a contract.

| Provisional authoritative transition | Proposed sound / haptic | Exact semantic guard (needs Architect sign-off) |
| --- | --- | --- |
| `BoxHitApplied(boxId, miniId, boxHpAfter, hitId)` | soft `v12_box_hit` | One valid damage hit; applies HP reduction and consumes mini exactly once. **No `MiniBotKilled` overlay** for box consumption. Aggregate repeated hits for audible rate control. |
| `BoxDestroyed(boxId, rewardId)` | `v12_box_break` | Box alive→destroyed transition **once**, even if multiple MiniBots contact on same tick. |
| `MultiplierEarned(rewardInstanceId, type, boxId, queueState)` | `v12_reward_earned` (subtle for queued) | One sound when actually earned, never on every clone or visual sparkle. |
| `MultiplierActivated(instanceId, type, activationTick, plannedExpiryTick)` | `v12_boost_activate_x2/x3/x5` | Transition pending/earned→active once. If earned and immediately activated in same tick, use **one** positive accent in total; no duplicate “earned+active” pair. |
| `MultiplierExpired(instanceId, type)` | `v12_boost_expire` (quiet) | Once per actual active→expired transition. A new queued activation on same tick follows approved chronology; do not self-schedule with audio wall-clock timers. |
| `MultiplierBoxLockChanged(boxId, locked)` | Usually silent | UI lock is always legible; optional single UI feedback only for deliberate user action, never on periodic state polling. No MiniBot may be spent on locked box. |
| `FriendlyHitResolved(miniId, hitId, hpAfter)` | `v12_friendly_armor_hit` low priority | Nonlethal hit; **do not** play death sound while HP>0. Exact damage resolution left to GDD. |
| `EnemyHitResolved(enemyId, hitId, hpAfter)` | `v12_enemy_hit` | Nonlethal contact, including armored enemies; one event per accepted hit/attack cadence, regardless of collision callbacks. |
| `FriendlyDefeated(miniId)`, `EnemyDefeated(enemyId)` | `v12_unit_defeat` (or existing contact hit as appropriate) | Only terminal HP≤0 transition, once per unit. Unlike DEV-002.2, enemy defeat and MiniBot loss are **not necessarily paired**. |
| `EnemyGroupSpawned(groupId, waveId, count)` | optional short group arrival | Once per batch/group, not `EnemySpawned` one-by-one. May be silent if wave start or boss cue already covers it. |
| `WaveWarningStarted(waveId, expectedStartTick)` | `v12_wave_warning` | Once per wave, proposed ≈3s/90 ticks before start at 30Hz where feasible. First-wave boundary and timing authority require GDD/Architect. |
| `WaveStarted(waveId, index, total)` | `v12_wave_start` | Once per wave; `WAVE n/N` remains a UI state, not repeated sound through repaint. |
| `BossWarningStarted(bossId)`, `BossEntered(bossId)` | `v12_boss_warning`, `v12_boss_enter` | Once each real transition; boss priority overrides or coalesces overlapping general wave warning. |
| `BaseThreatEntered(threatId)`, `BaseThreatCleared(threatId)` | `v12_base_threat` | Enter danger state once, with stable threat ID, hysteresis and cooldown; *not* per-enemy proximity frame. `BaseBreached` remains actual base-damage event. |
| `MatchEnded` / accepted Restart or Resume UI | Existing victory/defeat and UI cues from PR #12 | Preserve clear terminal sound, silence background transitions, no duplicate “restart” on initial scene creation. |

**Audio-facing dedup policy:** transport dedup per `(MatchId, Tick, orderedEventIndex)` after consuming **every** advanced tick, including tick 0 and 2–8 ticks processed within one render frame. Semantic dedup additionally uses `boxId`, `hitId`, `rewardInstanceId`, `waveId`, `groupId`, `bossId` and genuine state-transition IDs. `EventHash` is **not** an event ID. Do not derive attacks, expiry, wave start or base threat from rendered visuals or local timers. No Unity audio callback may mutate sim state, input queue, RNG, HP, rewards, gameplay event order or replay hashes.

### Simultaneous-event audio precedence

1. One terminal Victory/Defeat stinger is supreme; suppress/duck recent impacts and warning tail.
2. Actual base breach > imminent base threat/boss warning > wave start/warning > confirmed reward or box break > enemy/friendly hits > routine auto-shot.
3. If boss warning and general wave warning are simultaneous, use **one** distinct boss warning (pending owner/UI approval).
4. If an upgrade expires and queued reward activates on the same tick, consume both deterministic events but play at most one clear activation-focused accent; UI timer/order must follow simulation.
5. If several MiniBots hit one box on the same tick, emit the audible break cue **only** if an authoritative `BoxDestroyed` exists, once. Coalesce regular hit texture; do not fake damage or consume extra bots.
6. The audio listener may **drop low-priority playback**, never drop/reorder authoritative simulation events or change their outcome.

## 3. Sound identity and minimal asset-preparation list (NOT assets)

| Logical ID | Proposed source master | Target duration | Priority | Notes |
| --- | --- | --- | --- | --- |
| `v12_box_hit` | `v12_box_hit_01.wav` | 0.08–0.16s | LOW | muted, rhythmic; withstand up to 25 hits without harsh repetition |
| `v12_box_break` | `v12_box_break_01.wav` | 0.18–0.35s | MEDIUM | one stronger release, audible distinction from nonlethal hits |
| `v12_reward_earned` | `v12_reward_earned_01.wav` | 0.12–0.24s | MEDIUM | soft acquisition/queued feedback |
| `v12_boost_activate_x2/x3/x5` | `v12_boost_activate_xN_01.wav` | 0.18–0.32s | MEDIUM | common timbre, increasing character for bigger multiplicity |
| `v12_boost_expire` | `v12_boost_expire_01.wav` | 0.12–0.25s | LOW | distinct yet quiet; no misleading “reward lost” alarm |
| `v12_enemy_hit` | `v12_enemy_armor_hit_01.wav` | 0.08–0.18s | LOW | distinct from defeat |
| `v12_friendly_armor_hit` | `v12_friendly_hit_01.wav` | 0.08–0.17s | LOW | low intensity; avoid fatigue with survivor bots |
| `v12_unit_defeat` | `v12_unit_defeat_01.wav` | 0.10–0.25s | MEDIUM | only true death transitions |
| `v12_wave_warning` | `v12_wave_warning_01.wav` | 0.40–0.70s | HIGH | advance warning; no blocking |
| `v12_wave_start` | `v12_wave_start_01.wav` | 0.40–0.80s | HIGH | simple callout accent, no mandatory voiceover |
| `v12_boss_warning` | `v12_boss_warning_01.wav` | 0.50–0.90s | HIGH | distinct from regular warning |
| `v12_boss_enter` | `v12_boss_enter_01.wav` | 0.50–1.00s | HIGH | does not drown base danger/one-thumb feedback |
| `v12_base_threat` | `v12_base_threat_01.wav` | 0.30–0.60s | CRITICAL | limited repeats; not confused with actual breach |

**Proposed assets format:** source WAV PCM mono 44.1kHz/16-bit for short cues; Unity compression/import settings and stereo/spatial policy require actual asset/device testing. Do not copy third-party game sounds or trade dress. Earlier PR #12 legacy SFX inventory remains historically valid for DEV-002.2. No files listed above have been generated, purchased, downloaded or imported.

## 4. Mobile audio / haptic constraints

- Keep **8 total simultaneous audio voices** as a *provisional starting cap*, not a proven budget. Coalesce box impacts and group-enemy impacts per rendered frame or short cooldown; speech/wave/critical cues can steal older low-priority voices. Run real Android/iOS profiling before accepting a numeric cap.
- Suggested starting per-class caps: box impacts 2; combat impacts 2; ordinary shots 2; UI/reward 1; high-priority alert 1 (global 8 still wins). Terminal sound overrides. These caps require QA validation under ×5 six-second bursts.
- Wave announcements are **nonblocking** and must not stop aiming, MiniBot attacks, box UI or performance; use short sound cues and optionally 3–6 dB temporary ducking of low-priority effects (proposed, not measured).
- Low-intensity haptics are **optional**, under a separate user-controlled setting. Suggest one short pulse at wave/boss/base-critical transition, not for every bot hit, shot or box hit; respect platform, accessibility and OS restrictions. Haptic pulse length and frequency are QA tuning, not approved gameplay constants.
- Master/SFX/mute setting must not hide gameplay information. Visible `WAVE n/N`, boss warning, lock reason, queue state and base threat remain readable in total silence and when haptics are disabled.
- Sound and haptics must not run in app background, and no queued burst playback is allowed after resume. No audio logic must infer boost expiry from wall-clock elapsed while simulation is paused.
- No automatic variation based on MiniBot HP=2 versus HP=3; all audio triggers rely on actual accepted hit/death transitions. Same per-event mix across matched presets, so Owner can assess gameplay balance fairly.

**Estimates, not benchmarks:** short SFX clips should have low memory cost, but true decoded clip footprint, voice count, CPU spikes, clipping, phone speaker intelligibility, latency, OS suspend/resume and haptic behavior are **NOT TESTED**. Candidate acceptance targets: ≤8 simultaneous voices, clear alerts without clipping and no audible disruption of one-thumb input. Performance/QA must validate on devices after authorized implementation and real assets.

## 5. Proposed cross-role QA and playtest matrix (NOT executed)

| ID | Scenario | Expected AUDIO/HAPTIC observation | Required dependency |
| --- | --- | --- | --- |
| V12-A01 | ×2 box HP=5; five successful bot hits | Up to five **bounded** hit textures, one break, exactly one reward; no duplicate bot-death cue | GDD/Arch authoritative box hit |
| V12-A02 | ×3 box HP=10; ten successful hits | Same event semantics, without ten simultaneous deafening effects | Same |
| V12-A03 | ×5 box HP=25 with dense bots | Contact/audio rate-limiting never changes consumed MiniBots or accepted hits | Performance stress |
| V12-A04 | Box already destroyed + extra same-tick MiniBots | Exactly one box-break event and sound | Arch TOI ordering |
| V12-A05 | Active ×2, pending ×3, third multiplier box targeted | Third box locked **before any MiniBot cost**; no hit/reward sounds; clear visual lock | GDD/Arch queue lock boundary |
| V12-A06 | Active expires; pending activates on same tick | One clear activation cue, no lost reward and no accidental overlap/double sound | Arch activation/expiry phase |
| V12-A07 | ×2=12s, ×3=10s, ×5=6s under same 30Hz log | Audible activate/expire only on authoritative state transitions; no client wall-time inference | Versioned timing config |
| V12-A08 | Enemy hits MiniBot but MiniBot survives | Nonlethal impact cue only; **no** MiniBot-death sound | GDD proposed combat cadence |
| V12-A09 | Enemy dies while MiniBot remains alive | One enemy-defeat cue without mandatory MiniBot-loss pair | Multi-HP event semantics |
| V12-A10 | Successful box hit by survivor MiniBot | MiniBot consumed **once** for box hit; box cue, no duplicate death | O12-01 vs O12-02 |
| V12-A11 | Group of enemies spawns simultaneously | One group/wave cue, not per-enemy alarm spam | Wave group IDs |
| V12-A12 | ~3s wave warning and WAVE n/N callout | One short warning + one start accent; controls and boxes remain usable | GDD authored timings/UI |
| V12-A13 | Boss with accompanying group | Boss signature wins over duplicate generic wave stinger | Arch event priority |
| V12-A14 | Base threat enters, persists and clears | Alert once on entry; no per-frame retrigger; clear/re-entry policy honored | GDD hysteresis/cooldown |
| V12-A15 | Mute ON; haptics optionally OFF | All visual info still present; no forced sound/vibration | UI accessibility |
| V12-A16 | App pause, focus loss, resume and Restart | No background/catch-up audio; event dedup resets per new match | Presentation adapter |
| V12-A17 | Two **identical** scripted trials except MiniBot HP=2 vs HP=3 | Actual audio frequency may differ only because authoritative hit/death events differ; no hardcoded HP threshold; capture voice peak, alert audibility, clipped samples | QA/Performance presets |
| V12-A18 | Same tick box hit vs enemy hit vs boost change | Audio order follows eventual signed Arch v1.2 chronology; sim/replay hashes unaffected by SFX voice-stealing | ARCH v1.2 & QA2-01 |

**For matched preset tests** keep level seed, box choices, enemy groups/timing, device and command stream identical; vary only the intended versioned HP preset. Log: actual event counts, audible plays versus suppressed plays, peak simultaneous voices, clipping incidents, event latency and OS audio interruptions. Record **PASS / FAIL / NOT TESTED** only after execution. This document provides expected outcomes, **not** proof of tests.

## 6. Dependencies / decisions to resolve before implementing audio

1. **Game Designer / Owner #18 / revised Draft PR #19:** authoritative wave timestamps/count/group IDs, boss emergence, box targeting/lock behavior, enemy combat hit cadence, precise threat entry/exit, plus matched HP=2/3 prototypes. Preserve all approved constants as initial configurable presets (not hardcoded audio rules).
2. **Architect #2 / v1.2 technical spec:** stable semantic IDs for box, hit, boost and wave; deterministic time-of-impact and ordering, exact boost expiry/activation on same tick; audio-compatible read-only event batch and versioning; QA2-01 legacy gate migration without deleting history.
3. **Developer #4:** only after separate implementation authorization, bridge every produced tick's ordered batch to a presentation-only audio listener; UI-only accepted Resume/Restart signals, no gameplay changes from SFX.
4. **UI #6 / Art #5:** visible multiplier queue, locked box reason, survival-hit distinction, group/boss warning, audio/haptic toggles and silent accessibility acceptance.
5. **Performance #8:** eight-voice working hypothesis on low-end devices and ×5 bursts; no FPS, memory or latency claims before measurements.
6. **QA #9:** run exact matched HP presets and events/OS/haptics test matrix after authorized asset/integration work.
7. **Owner:** review/revise this draft addendum, later choose sound asset sourcing/creation and code implementation scope; **do not self-merge**.

## 7. What is completed vs pending

- **Completed:** GitHub read of #18 decisions, #7 role instruction, current PR #19 and DEV-002.2 audio spec; proposed v1.2 event/SFX/haptic contract; mobile budget and 18 test cases.
- **Not completed:** GDD v1.2 final approval, ARCH v1.2 contract, audio assets, integration, Unity/mobile testing, device profiling, gameplay release sign-off.
- **Modified files:** only this new Markdown document, on a dedicated docs branch and Draft PR. Earlier PR #12 remains unchanged.
- **Release restriction:** QA2-01 remains open; DEV-002.2 historical code/tests must remain; no VS-02.

**Review target:** `AUDIO_V12_OWNER_DECISIONS_SPEC_READY_FOR_REVIEW`.

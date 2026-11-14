# SWARMFORGE — GDD v1.2 / GD-006

**Status:** DESIGN PROPOSAL — NOT APPROVED FOR IMPLEMENTATION  
**Owner directive:** [#18](https://github.com/Gogenco/swarmforge-studio/issues/18); role thread [#3](https://github.com/Gogenco/swarmforge-studio/issues/3)  
**Prepared by:** 03 — Game Designer / Level Designer  
**Date:** 2026-11-14  
**Source baseline:** `main` DEV-002.2, merge `99cfcdc0b4982e9e441cf23a8669a487a7d75ea9` / [PR #10](https://github.com/Gogenco/swarmforge-studio/pull/10)  
**Related, not superseded:** [GD-005 Draft PR #15](https://github.com/Gogenco/swarmforge-studio/pull/15); [ARCH-003 Draft PR #14](https://github.com/Gogenco/swarmforge-studio/pull/14)

> **Authority legend:** **APPROVED INITIAL DESIGN** = specific Owner decisions consolidated in [Issue #18 GITHUB-005](https://github.com/Gogenco/swarmforge-studio/issues/18#issuecomment-6880330660), still tunable after playtesting. **PROPOSED** = Game Designer recommendation awaiting Owner/Architect sign-off. **PLAYTEST PRESET** = comparable, unapproved numbers. Fixed gates stay in DEV-002.2 code/tests for migration, but are **replaced by breakable side boxes in new v1.2 gameplay**. Friendly MiniBot HP **is intentionally undecided** (test 2 vs 3), and Unity source/config changes, PR merge and VS-02 are forbidden. Older v1.2 draft paragraphs using a per-shot quota or MiniBot combat death on every hit have been replaced.

## 1. Player fantasy and Owner-locked design pillars
*Fire a stream of charming, survival-capable robots; choose whether to defend the home base, crack a tempting side upgrade box, assault the fortress or build a Forge machine; enjoy a meaningful timed power spike before stronger grouped enemy waves arrive.*

1. **One-thumb, automatic fire.** The player aims/steers, while every new raw MiniBot is assigned a target at spawn. Defensive timing matters more than mechanical input speed.
2. **Box tradeoff [APPROVED].** A successful box damage hit **consumes one MiniBot** regardless of that MiniBot's remaining combat HP. The rewards replace fixed multiplication gates in **new v1.2 arenas**, not in DEV-002.2 legacy runtime yet. Box initial durability **×2=5, ×3=10, ×5=25 HP**.
3. **Timed, sequential earned power [APPROVED].** ×2 **12 s**, ×3 **10 s**, ×5 **6 s**, no shot-count quota and no stacking. Exactly **one active + one pending multiplier**; earned pending boost starts its full timer automatically after the preceding one expires. Other multiplier boxes are visibly locked while both slots are full.
4. **Robots can survive combat [APPROVED DIRECTION, HP NOT SET].** MiniBots may exchange multiple hits with enemies and remain on the field; only box-hitting MiniBots are immediately consumed. The exact MiniBot HP, damage, attack interval and survival behavior require an Architect contract and **comparative HP=2 vs HP=3 playtests**.
5. **Enemy groups and escalation [APPROVED DIRECTION].** Multiple enemies enter simultaneously as visible groups. Their type and durability increase across pre-authored waves, without instantly counter-scaling whenever an upgrade is earned.
6. **Siege/Forge identity retained.** Fortress destruction remains the objective; friendly base must survive. ATTACK/FORGE, blueprint choice and future Mech/Titan remain part of SWARMFORGE, subject to a signed-off migration plan. No visual copying of reference games.

**Scope note:** Values like wave timestamps, box positions, enemy stats, player engagement radius, boss HP, fortress HP in the sample and UI hysteresis below are **PROPOSED**, not approved. A configuration-driven playtest is required before choosing a MiniBot HP or making balance claims.
## 2. OLD DEV-002.2 → NEW APPROVED DIRECTION / PROPOSED CONTRACT

| Area | DEV-002.2 in merged `main` | Owner-approved target design / unresolved implementation |
|---|---|---|
| Cannon | Automatic 3 raw MiniBots/s, deterministic 30Hz | Retain as **PLAYTEST PRESET** initially; cadence must be editable in versioned balance config |
| Gates | Permanent ×1/×2/×3 fixed gates; old QA2-01 collision issue | **APPROVED:** v1.2 arenas use destructible side boxes instead. **Do not delete old gate code or tests** before explicit Architect/Owner migration approval |
| Box hits | No box HP/reward subsystem | **APPROVED:** one successful hit reduces box HP by1 and consumes the MiniBot. Approved HP=5/10/25 for ×2/×3/×5 |
| Multiplier | Fixed gate creates clones with no duration | **APPROVED:** ×2=12s, ×3=10s, ×5=6s, time-only; no shot quota/stacking; active1+pending1. Implementation tick boundaries unresolved |
| Combat | Same-lane one-MiniBot/one-Drone mutual destruction | **APPROVED DIRECTION:** MiniBots can survive some combat contacts; multiHP enemies; **NOT APPROVED** friend HP2 vs HP3, damage/cooldown/engagement |
| Enemy spawning | VS-01 smoke: small number of discrete enemy spawns | **APPROVED DIRECTION:** simultaneous grouped waves, escalating HP/types/counts and nonblocking warning; exact schedule is PROPOSED |
| HUD | Base/fortress/time, limited old information | New WAVE n/N, boss/base alerts, box HP, time-only boost countdown and pending queue |
| Routes | Scalar lanes; 2D choke not verified | Side boxes require real target geometry and chronological contacts; routing once at spawn |
| Forge/Mech/Titan | GDD v1.1 design, not implemented in VS01 smoke | Preserved; multiplier effect on FORGE shot and full-level balance need Owner/Architect sign-off; proposed ATTACK-only |
| L1–L3 | Existing GDD v1.1 values; VS-01 is only a smoke fixture | **No actual balance change now.** This document offers a separate 60s sandbox and level *intent*, not replacement files |
| Build gate | QA2-01 open; ARCH-003 Draft #14 unapproved | Document/architecture/test planning only. No Unity edits, no merges, no VS-02 |

**Design source control:** this update stays on existing [Draft PR #19](https://github.com/Gogenco/swarmforge-studio/pull/19); [GD-005 #15](https://github.com/Gogenco/swarmforge-studio/pull/15), [ARCH-003 #14](https://github.com/Gogenco/swarmforge-studio/pull/14), code and WIP are preserved unchanged.
## 3. Core loop, one-finger control and route contracts

1. A wave warning and box rewards are visible. Auto-fire produces **raw MiniBot shot opportunities** at the configured rate (initial playtest:3/s).
2. The player drags the cannon's aim left/right. In ATTACK mode the target sector is one of `BOX LEFT | DEFEND LEFT | FORTRESS | DEFEND RIGHT | BOX RIGHT`. A shot's target is locked **at spawn**, never changed midflight. Box and enemy aiming must have clear world arrows and target outlines.
3. A box-targeted MiniBot is never multiplied; once it **successfully deals one box HP**, it is **consumed**, whether it had HP2 or HP3. If killed by an enemy before arriving, its reservation releases with no box damage.
4. On the last needed box hit, reward enters active slot if empty; otherwise queued slot if empty. **No shot count** is spent to maintain the multiplier. During its active **12/10/6 s gameplay timer**, each raw ATTACK shot generates ×N independent MiniBots; box/Forge raw shots do not multiply under the **PROPOSED ATTACK-only** boundary.
5. Enemy contact is not a one-off deletion. **PROPOSED:** MiniBot and enemy enter a short deterministic engagement; after each attack interval they exchange integer HP damage and surviving robots fight again/continue toward the fortress. Exact repeat-hit semantics await Architect and test presets in §7.
6. Enemies appear in **simultaneous wave groups** and keep threatening the base while upgrades/Forge compete for player shots. Defeat three times at base HP3 (unless a separate approved shield changes it) or fail to destroy fortress within timer → loss; same-tick fortress/base destruction → loss.

**One thumb and Forge:** tapping `ATTACK / FORGE` alternates modes (same finger, no simultaneous multi-touch). In FORGE mode center shots contribute to selected blueprint, and side boxes are visibly unavailable; player must return to ATTACK to break them. Old GDD v1.1 blueprint selection and station-slot reservations are retained, not silently redesigned here. Aim reticle, auto-fire lane and destination preview are never hidden by wave banners.

**Box saturation UX:** once missing HP are covered by reserved in-flight hits, show `PENDING`; raw firing pauses **only while aim remains on that saturated box**, without consuming shots. Drag back to a defensive lane to fire unboosted MiniBots while previously dispatched box-bound MiniBots travel. No spawned MiniBot is automatically rerouted. If a reservation is lost, box reopens for its remaining HP.

**Boost queue UX [APPROVED]:** when `ACTIVE ×2 08s | NEXT ×3` both slots are filled, other **multiplier** boxes are `LOCKED · BOOST QUEUE FULL` *before* any new units are spent on them. Their existing HP progress is retained. When ACTIVE expires, NEXT becomes ACTIVE with its **full** duration, and the other boxes unlock. Preexisting terminal-hit reservations reserve entitlement capacity ahead of arrival; **active + pending + promises ≤2**. This avoids reward overflow and removes the old box-side escrow proposal. Await Architect's exact-head acceptance (§14).
## 4. Proposed field layout, dimensions and routing

These are **hypothetical world-space geometry values** for a 60 s test arena, **not a replacement** for approved L1–L3 authored coordinates or verified GD-004 geometry. Let X be screen horizontal and Z point upfield. Cannon neutral muzzle `(0, 0.6)`; friendly breach plane `Z=0.6`; front of enemy fortress `Z=17.6`; fortress visual center approximately `(0,18.8)`; enemy spawn plane `Z=17.6`; attack lanes near `X=-1.2, 0, +1.2`; left/right box centers `(-2.5,5.0)` and `(+2.5,5.0)` with approximate `0.55` **visual** radius; logical combat/box colliders are **proposed 2D fixed-point swept axis-aligned rectangles** with independently editable half-width/half-height, not world-render circles; their final dimensions and camera framing require Architect/Designer tests.

| Object | Initial proposal | Design purpose / concern |
|---|---|---|
| Cannon, base | `(0,0.6)`, movement/aim constrained to bottom band | Never occlude base HP, aim sector or ATTACK/FORGE |
| LEFT upgrade slot | `(-2.5,5.0)`: ×3 / HP10 | Faster tactical boost; enemy pressure opportunity cost |
| RIGHT upgrade slot | `(+2.5,5.0)`: ×5 / HP25 | Higher-risk burst; long uninterrupted diversion unsafe |
| ×2 box template | HP5, available in L1 or an alternate authored loadout | Introduce concept safely; **not a third simultaneous UI-critical slot** |
| Enemy LEFT lane | from `(-1.2,17.6)` toward `(-1.2,0.6)` | Must be interceptable by corresponding DEFEND LEFT sector |
| Enemy RIGHT lane | from `(+1.2,17.6)` toward `(+1.2,0.6)` | Likewise right; no assumed cross-lane collision |
| Enemy CENTER / boss | from `(0,17.6)` toward `(0,0.6)` | Telegraph large boss without hiding box controls |
| Fortress hit plane | front face `Z=17.6`, proposed full-width target | Surviving MiniBots reaching it damage fortress |

At neutral aim, box path length is `sqrt(2.5² + 4.4²) = 5.060 u`, travel at **hypothetical** `3.4 u/s` is `1.488 s`. Direct assault reference distance is `17.0 u`, travel `5.000 s`; extreme ±X aiming makes physical distances marginally longer and must be measured in implementation. Enemy breach from `Z=17.6` at speed `2.3 u/s` is `17/2.3 = 7.391 s` after enemy spawning. The old ~7.4 s breach risk therefore remains relevant, but no box/contact win is proven.

**Geometry must be reviewed before implementation:** side boxes must not intercept ATTACK rays unintentionally; box hit radii and enemy hit radii must be distinct; a MiniBot may collide with an enemy on its path to a box and die, releasing its reservation; narrow-screen camera must not hide rightmost `×5` badge; two side enemy lanes cannot magically merge under the old same-lane scalar contact model. Whether to use genuine 2D paths, dedicated authored lane crossing, or a separate validated approximation belongs to Architect.

## 5. Box reservations, timed rewards and queue — approved versus proposed

**APPROVED reward mechanics:** ×2=5HP box→12s ×2; ×3=10HP→10s ×3; ×5=25HP→6s ×5. **No shot quota.** One successful box impact destroys **the hitting MiniBot**, reducing box HP by exactly one. Multipliers never stack. One active and one queued multiplier maximum. Active expires based on authoritative match time, queued boost then starts a **fresh full duration**. When queue full, other multiplier boxes are visibly locked before any *new* MiniBots are spent on them; box HP progress never resets.

**PROPOSED DETERMINISTIC ARCH-v1.2 #23 AGREEMENT:** `AVAILABLE → RESERVED_IN_FLIGHT → (TERMINAL_AWARD_PROMISED) → DAMAGED → DESTROYED → REWARD_GRANTED_ONCE → (ACTIVE | PENDING)`. Each box has BoxId, MaxHP, RemainingHP, ReservedShotIds, TerminalAwardPromise, RewardGranted and EntityVersion. Each MiniBot has UnitId, immutable TargetKind/TargetId, CurrentHP and engagement. No side escrow or third bonus slot is created; authoritative box damage is not VFX.

- **Reservation and entitlement:** on spawn, reserve one intended box damage only when RemainingHP−liveReservations>0. **Before dispatching a shot that would fully cover a box's remaining HP, reserve a terminal award promise** from the two-slot entitlement capacity. Required invariant: **ActiveCount + PendingCount + TerminalAwardPromises ≤ 2**. Existing in-flight reservations do not subtract actual HP. The shot never changes its target after launch.
- **Enemy interception:** if a box-bound MiniBot dies fighting or in any other contact before reaching its box, release its reservation. No HP loss and no reward. A surviving box-bound MiniBot reaching box loses **all remaining HP** and is removed on successful 1HP damage.
- **Filled reservation:** target shows `PENDING`. If player remains in that box aim sector, cannon does not waste raw shots; drag to attack immediately while hits travel, without retroactive multiplication of those attack shots.
- **Exactly-once award:** damage decrements HP to zero once. A unique `BoxBroken` produces a unique `RewardClaimed` event. Later/duplicate contacts do not grant additional timed boosts.
- **Queue/promise full (ALIGNMENT PROPOSAL):** while active + pending + terminal award promises = 2, every **other multiplier box is visibly target-locked before any new box shot**, including non-final damage shots; its accumulated HP persists. If a terminal-promise shot dies before arrival, release the claim and recompute eligibility. On an authorized terminal hit, atomically convert its promise into one available active/pending entitlement, **never a third reward**. Existing legal in-flight shots may land, but entitlement reservations prevent overflow rather than risking box-side escrow. A locked-target shot opportunity emits ShotSuppressed (no MiniBot spawned, no hidden reroute, no spent raw unit).
- **Activation boundary (ARCH #23 trial proposal accepted by Designer):** an award at within-tick (k,τ) is never retroactive; if Active slot free, activation starts **at the following tick k+1 boundary**. If active exists, reward holds pending FIFO and starts at the instant the active boost expires **at the start of expiry tick**. Duration windows are **[StartTick,EndExclusiveTick)**, ×2=360, ×3=300, ×5=180 ticks at30Hz. Expire/promote **before** firing a raw shot due on that boundary. Snapshot applied boost on born ATTACK MiniBots; no shot quotas.
- **Timer:** **PROPOSED** use gameplay fixed-tick clock, uninterrupted during ATTACK/FORGE targeting, box shooting, wave warnings or a temporary pause of box-sector firing. Timer starts on activation (not award while queued); expiry is not delayed by holding a finger elsewhere.
- **End of battle:** freeze box reservations, timers, queued rewards and event emissions on the authoritative terminal tick. No late rewards after victory, loss or timeout. Restart creates clean state.
- **Box availability:** two side slots visible at a time is a **layout proposal**. A later wave can place a new box in an EMPTY slot only; no silent displacement, random reward or HP reset.

**Other reward classes (unapproved for first slice):** shield, Pulse, fire-rate require a separately approved reward category, stacking/queue semantics and event contracts. They must **not** steal the active/pending *multiplier* slots without Owner decision. Design priority is first playable multipliers, grouped combat and warnings.

**World-space feedback:** HP bar `8/10`, icon `×3`, `IN FLIGHT`, `LOCKED · 2 BUFFS HELD`; UI persistent `ACTIVE ×2 07s | NEXT ×3 (10s)`. Accessible symbols accompany color. Box hit/break, queued/activated/expired are distinct events for Art/Sound/UI.
## 6. Corrected time-only ×2/×3/×5 acquisition economics and defense risk

**APPROVED base cost:** each successful box hit consumes one raw MiniBot; HP values5/10/25. **APPROVED reward durations:**12/10/6s, no charges, nonstacking, active1+pending1. **PLAYTEST assumption:** 3 raw shots/s; MiniBot travel3.4u/s; neutral cannon→box distance5.060u = **1.488s**. These physical times are not approved or simulated.

The Nth shot to a box leaves at `(N−1)/3`; ideal award at `(N−1)/3+1.488`:

| Reward | Required raw shots | Last shot dispatched | Last box hit / award within tick | First boosted ATTACK shot | **PROPOSED active tick window [start,end)** |
|---|---:|---:|---:|---:|---|
| ×2 (HP5) | 5 | 1.333s | **2.821s** | 3.000s | **2.833–14.833s** (ticks85–445) |
| ×3 (HP10) | 10 | 3.000s | **4.488s** | 4.667s | **4.500–14.500s** (ticks135–435) |
| ×5 (HP25) | 25 | 8.000s | **9.488s** | 9.667s | **9.500–15.500s** (ticks285–465) |

**Ideal full-active-window output (no box/Forge diversion while active):**

| Reward | Duration [APPROVED] | 3 raw/s during boost | MiniBots generated during boost | Extra MiniBots vs ×1 | Box cost | **Net extra vs ignoring box** | Peak spawn rate |
|---|---:|---:|---:|---:|---:|---:|---:|
| ×2 | 12s | 36 | 72 | +36 | −5 | **+31** | 6 MiniBots/s |
| ×3 | 10s | 30 | 90 | +60 | −10 | **+50** | 9 MiniBots/s |
| ×5 | 6s | 18 | 90 | +72 | −25 | **+47** | 15 MiniBots/s |

All totals are **upper ideal arithmetic**: only shots fired in ATTACK while multiplier active receive benefit; missed aim, spent Forge/box time, cooldown phase, early defeat, enemy interception and other effects reduce realized value. **×3 now yields +50 net theoretical units, ×5 +47; ×5 instead concentrates production into a faster six-second burst.** The earlier Draft PR estimate ×3+20/×5+23 based on per-shot charges is obsolete and must not be used.

**W1 safety calculation:** illustrative first Scout group spawns at t6, from17u, speed2.3u/s; earliest breach `t=6+17/2.3=13.391s`. Aligned opposing ATTACK MiniBot speed3.4u/s, earliest *boosted* contact:
`t_contact = (17 + 3.4·t_attack_spawn + 2.3·6)/(3.4+2.3)`.

- ×2: first post-award shot t3.000 → contact **~7.19s**, ~6.20s before first unopposed breach.
- ×3: shot t4.667 → **~8.19s**, ~5.20s before breach.
- ×5: shot t9.667 → **~11.17s**, ~2.22s before breach.

**This does not prove W1 interception**, especially with two flank groups, engagement cooldown, survivor HP2/HP3 and a finite cannon aim time. ×5 can be tactically dangerous despite its stronger rate. A skilled player may stop aiming at a box once enough shots are *reserved*: first **unboosted** defense shot could leave at1.667/3.333/8.333s for ×2/×3/×5; returning to attack preserves box-bound target assignments but does not grant bonus retroactively.

**Sequential example:** box×2 final hit at2.821s triggers activation **at tick85=2.833s**. A second box×3 final hit around6.155s is queued while ×2 runs; it **does not overlap** or become ×6. ×2 expires **at tick445=14.833s**, and queued ×3 becomes active on that same boundary with its full **300 ticks**, until **tick745=24.833s**. No gap, no overlap, no per-shot quota. Timed exactness is a Designer-accepted **ARCH #23 prototype proposal**, not Owner-final gameplay tuning.

**Wave spike fairness:** upgrades do not trigger enemy instant-level scaling; next authored wave retains its scheduled spawn and HP. Recommended telemetry: time from reward activation to first serious base threat, damage handled during window, buff seconds spent ATTACK, base breaches before/after reward. These are not playtest findings.
## 7. Survival-capable MiniBots, multi-hit enemies and comparable presets

**APPROVED DIRECTION:** MiniBots have more than one HP and can survive combat contacts. Enemies, including armored types, also have HP and arrive in groups. **NOT APPROVED:** MiniBot max HP2 versus HP3; raw damage, combat attack interval, enemy damage, stops/movement while engaged, exact target selection and boss HP. A box hit **always consumes** the MiniBot on successful damage regardless of friendly combat HP.

### Proposed minimal deterministic combat model (for Architect review only)

1. Every MiniBot and enemy has max/current integer HP and attackDamage. **PLAYTEST defaults:** friendly damage1, enemy damage1, configurable **AttackCooldownTicks=15 (>0)**. At first swept contact within tick k, the two eligible actors exchange immediate **simultaneous** damage using a pre-exchange alive snapshot. First repeat is eligible at **tick k+1+AttackCooldownTicks** per ARCH #23; **trial design recommendation** for subsequent repeats is `repeatTick + AttackCooldownTicks` with positive interval. The cooldown is measured by deterministic 30Hz ticks, never render frames.
2. **PROPOSED BOUNDED PAIR CONTRACT:** one MiniBot may engage at most one enemy and vice versa. Paired actors pause route motion and repeat combat only against that partner; **no third actor joins an occupied engagement** in Stage P2. At equal time, choose unengaged pairs by **EnemyId ASC then MiniBotId ASC** and revalidate eligibility; other units continue their routes or form another eligible pair. A contact does **not** automatically destroy a MiniBot. When one partner dies, surviving partner resumes its originally assigned path **next tick**, not by instant same-tick retarget loop.
3. **Only the two members of each eligible pair** exchange damage simultaneously from their pre-exchange alive snapshot. If both die on that strike, both legitimate damages count and neither resumes. The next strike is forbidden until the positive repeat cooldown, even if rectangle hitboxes still overlap. Damage from **different** pairs at equal time is deterministic via stable IDs. No first-kill advantage, no multi-attacker focus fire on one occupied enemy in Stage P2.
4. Damage and cooldown checks belong to simulation, never animation or collision callbacks alone. UnitId/EnemyId, target lock, lastHitTick, HP and encounter phase must be replayable and deterministic.
5. To keep the siege economy readable, **PROPOSED:** any MiniBot that reaches the *enemy fortress* deals **one fortress damage and is consumed**, even if its combat HP remained >0; no repeated fortress hits per one survivor without Owner approval.
6. Three home-base breaches at baseline3HP still cause defeat; **PROPOSED** every enemy, boss included, deals1 breach HP on crossing. This prevents one untested boss arrival causing instant three-HP defeat.
7. This **single-pair stationary engagement** is simpler than the previous GDD's multi-attacker gang combat and is a **provisional trial contract**, not the final fun/balance rule. Dense groups still pressure through several **parallel pairs and unengaged runners**. Architect must confirm exact contact candidate revalidation and cooldown timing at GDD HEAD; Owner may request alternate group combat after P2 playtests.

### Two deliberately **unapproved** presets with identical scripted inputs

| Editable key | HP2 preset P2 | HP3 preset P3 | Owner approval |
|---|---:|---:|---|
| friendly.miniBot.maxHp | **2** | **3** | **DEFERRED until playtest** |
| friendly.miniBot.damage | 1 | 1 | PROPOSED common control |
| combat.cooldownTicks | 15 | 15 | PROPOSED common control (30Hz) |
| enemy.defaultDamage | 1 | 1 | PROPOSED common control |
| combat.engagementRadius | 0.6u | 0.6u | PROPOSED common control |
| cannon.rawRatePerSecond | 3 | 3 | PROPOSED common control |
| box.hp.x2/x3/x5 | 5/10/25 | 5/10/25 | **APPROVED INITIAL**, tunable later |
| boost.seconds.x2/x3/x5 | 12/10/6 | 12/10/6 | **APPROVED INITIAL**, tunable later |
| wave composition, boss, fortress, scripted input seed | identical | identical | PROPOSED comparison control |

**Serial 1-v-1 examples under the proposed simultaneous-exchange model** (each combatant deals exactly1 per0.5s round, and participants exchange even if dying in that round):

| Duel / sequence | MiniBot HP2 | MiniBot HP3 | Meaning / caveat |
|---|---|---|---|
| One Scout (enemy HP1) | MiniBot survives at HP1 | Survives at HP2 | No one-hit mutual disappearance |
| Two Scouts **in succession** | Dies on second while defeating it | Survives second at HP1 | Surviving robots can fight again |
| One Runner (HP2) | Kills Runner, dies in second exchange | Kills Runner, survives at HP1 | Remaining army differs |
| One Armored (HP3) | Dies after dealing2; enemy remains HP1 | Kills Armored in third exchange, dies | HP3 can save a reinforcing robot |
| One Boss (HP18) alone | At most2 enemy damage before dying | At most3 | **Pair-only trial:** other allies cannot simultaneously focus the occupied boss; group mechanics may be reconsidered after evidence |

**Enemy archetype PLAYTEST data (identical P2/P3):**

| Enemy | HP | Speed u/s | Behavior | Visual telegraph |
|---|---:|---:|---|---|
| Scout | 1 | 2.3 | Basic grouped opener | Bright small drone |
| Runner | 2 | 2.7 | Fast flank threat | Glowing frame, 2 HP pips |
| Armored | 3 | 1.8 | Survives multiple hits | Plated shell, 3 HP pips |
| Bulwark (later) | 5 | 1.4 | Deferred tougher unit | Large shield silhouette |
| Siege Behemoth boss | 18 | 1.1 | Group-spawn boss, 1 breach HP proposed | Segmented HP meter and original silhouette |

**Matched-test evidence needed:** baseHP after W1/W3/W5, same-tick exchange ordering, time in engagements, surviving MiniBots, armor/boss time-to-kill, fortress damage and actual victory rate, peak live units, visibility of HP changes. **Do not select either preset based on arithmetic alone.** If P3 trivializes defense or causes device pressure, tune after evidence; P2 may be too lethal when several groups simultaneously move toward the base before engagement can finish.
## 8. Simultaneous grouped waves and one complete 60-second sample

**Boundary:** a **PROPOSED** standalone 60s pacing sandbox, **NOT** a change to GDD v1.1 L3 or DEV-002.2. For exploratory feasibility, use hypothetical fortress **100 HP**, friendly base3HP. Old L3 fortress270HP and other baseline numbers are unchanged. Enemies spawn **as simultaneous groups on exactly one authored tick each**, not drip-fed one at a time; all start on Z=17.6 with authored side offsets to avoid overlaps. Geometry and actual collision radii need Architect review.

### Proposed five-wave schedule (group units all arrive at the same spawn tick)

| Wave | Warning | Group spawn | Simultaneous composition | Count | Total enemy HP | Earliest unopposed breach |
|---|---:|---:|---|---:|---:|---|
| W1 | 3s | **6s** | LEFT 2 Scouts, RIGHT 2 Scouts | **4** | 4 | Scout13.39s |
| W2 | 13s | **16s** | EACH SIDE 1 Scout +1 Runner +1 Armored | **6** | 12 | Runner22.30s |
| W3 | 23s | **26s** | EACH SIDE 1 Runner +2 Armored; RIGHT +1 Scout | **7** | 17 | Runner32.30s |
| W4 | 32s | **35s** | EACH SIDE 1 Runner +3 Armored | **8** | 22 | Runner41.30s |
| W5 **BOSS** | 40s | **43s** | CENTER boss; EACH SIDE 2 Runner +2 Armored | **9** | 38 | Runner49.30s; boss58.45s |
| **TOTAL** | — | — | Five simultaneous groups | **34** | **93** | — |

Enemy HP cross-check: W1=4·1=4; W2=2·(1+2+3)=12; W3=2·2+4·3+1=17; W4=2·2+6·3=22; W5=18+4·2+4·3=38, sum **93**. Group sizes **4→6→7→8→9**. All waves run on a preauthored schedule **independent of player upgrade pickup**. Warnings are three seconds ahead and wave callouts run1.25s without pausing. Boss appears **with eight escorts** (never as a solitary trivial one-by-one spawn).

**Spawn geometry handoff:** at wave.spawnTick generate entire spawnGroup[] with stable WaveId, GroupIndex and SpawnOrder. Distinct same-tick X offsets and hit radii must avoid overlap while retaining equal starting route length. If fan-out does not fit a narrow portrait screen, adjust positions and recompute ETAs; **do not pretend sequential trickle spawns are an approved substitute**.

### 60-second event-by-event player scenario (not a claimed successful run)

| Time | World event and example choice | Gameplay constraint |
|---|---|---|
| **0–3s** | Aim LEFT ×3 box; dispatch ten raw MiniBots | HP10 costs ten raw shot opportunities; last impact due4.488s on neutral ideal path |
| **3s** | WAVE 1/5 IN 3… overlay | Can retarget DEFEND while reserved box shots fly; no forced wait/multi-touch |
| **4.49→4.50s** | ×3 box breaks, then ×3 activates at **next 30Hz tick** for ten full seconds | In ATTACK, up to30 raw shots generate90 MiniBots; **no quota**, HP2/HP3 affects battle survival |
| **6s** | **Four** Scouts spawn simultaneously, two each side | Banner1.25s; unopposed breach at13.39s |
| **9–13s** | Defend flanks, optionally aim toward another reward | Any time spent shooting box/Forge during active multiplier sacrifices possible bonus production |
| **13s** | W2 warning | ×3 remains active until14.500s, so advantage is not instantly cancelled |
| **14.50s** | ×3 expires **before** any raw shot at tick435; queued boost activates on same boundary if present | HUD shows timer end / queued activation |
| **16s** | **Six** mixed enemies appear together | GroupHP12; survival-capable MiniBots exchange several blows |
| **18–23s** | Attack fortress, defend flank, or pay for ×5 | ×5 requires25 raw box shots and ~9.49s to unlock from fresh start |
| **23s/26s** | W3 warning, then **seven-enemy** armored group | HP17 group; no reactive instantaneous HP adjustment |
| **27–32s** | Vary attack lanes; optionally FORGE in full approved-level scope | Engaged MiniBots may stall enemies under proposed model, not proven |
| **32s/35s** | W4 warning, then **eight-enemy** group | HP22 group; multiple enemy hits, little unearned downtime |
| **40s** | Distinct BOSS INCOMING · 3… | Priority over generic warning; no interruption of aim |
| **43s** | **Nine-enemy** simultaneous group: boss plus eight escorts | Runners may breach49.30s, boss unopposed58.45s |
| **44–58s** | Defend base, focus boss with survivors; ×5 diversion only in a safe window | ×5 peak15 MiniBots/s for6s, but spending8.33s of cannon capacity can be fatal |
| **58–60s** | Fortress assault / last interception / terminal resolution | Timeout without fortress kill = loss; same-tick fortress/base destruction = **LOSS** |

### Corrected production bounds: MiniBot quantity is **not** damage or victory

At3 raw shots/s, a shot must leave by approximately t55s to reach fortress after an unopposed 17u / 3.4u/s=5s flight before t60; about **166 raw opportunities** (including boundary; exact last tick determined by Architect). Assuming one boost earned early, fully active while player attacks, and no other optional spending:

| Choice | Ideal attack MiniBots launched by t≈55s | Peak rate | Defending before first breach |
|---|---:|---:|---|
| No upgrade | **166** | 3/s | No spike |
| ×2 | 166−5+36 = **197** | 6/s | Award ~2.82s |
| ×3 | 166−10+60 = **216** | 9/s | Award ~4.49s |
| ×5 | 166−25+72 = **213** | **15/s**, only6s | Award ~9.49s; W1 already marches |

These are **emitted-unit upper budgets**, not a count of surviving robots or expected fortress impacts. The obsolete formula (MiniBots launched − enemy HP = fortress damage) is **invalid** after Owner approved reusable combat MiniBots. An HP2/HP3 survivor can fight repeatedly, but grouped attackers can gang up on it; engagements also consume time, and targeting matters. No sample strategy or L3 victory is proven by this table. The 100HP sandbox is **only a playtest candidate**, not an alteration to existing 270HP L3.

**Earlier GD-004 first-wave 4-of-6 interception:** this proposed sample W1 is four Scouts, while original L3 six drones remain a distinct required **regression stress scenario**. Actual interception must be replay-tested with agreed 2D paths, combat cooldown and P2/P3 HP.
## 9. Wave, box, timed-boost and accessibility UI

**APPROVED DIRECTION:** 3-second prewave warning, persistent WAVE n/N, 1–1.5s nonblocking wave-name callout, distinct boss warning/arrival, BASE UNDER ATTACK warning. Game simulation and one-thumb aim run uninterrupted by UI. **PROPOSED exact durations/trigger thresholds** follow.

| Display state | Event trigger / lifetime | UI priority | Pointer/feedback contract |
|---|---|---|---|
| Persistent HUD | Entire match | Always BASE/FORT/TIME/WAVE plus ACTIVE timer and NEXT boost | Safe-area; never intercept world aiming |
| PRE-WAVE | Warning event at startTick−90 (30Hz), 3s countdown | Regular | WAVE n/N IN 3…2…1, nonblocking |
| WAVE START | Entire group spawns same tick, banner1.25s | Regular | WAVE n/N — group name; no pause and no spawn-by-animation |
| BOSS WARN | 3s before boss spawn | Boss above ordinary wave notice | BOSS INCOMING, optional one haptic |
| BOSS ARRIVE | Boss and escorts spawn together | Boss below base danger | Original boss title, HP meter; no fullscreen takeover |
| BASE DANGER | Proposed authoritative ETA ≤2s, clear only at ETA≥2.5s | Highest transient | BASE UNDER ATTACK!; enter/clear once, not every tick |
| BOX | Spawn/impact/reservation | World status | HP 9/10, READY, PENDING, LOCKED (queue full); box HP readable on narrow phone |
| ACTIVE BOOST | Authoritative activation/expiry, full Owner timer | Persistent | ACTIVE ×5 05.8s (time not remaining shots), clear NEXT queue |
| NEXT BOOST | Queued reward from earned box | Persistent | NEXT ×3 (10s on activation), never silently discarded |
| TERMINAL | Victory/loss/timer and priority tick | Overrides all | Result alone can stop gameplay; simultaneous base+fortress death = LOSS |

**Priority order:** terminal > BASE DANGER > BOSS > WAVE > BOX/REWARD (transient); *persistent* counters remain visible. If multiple transient banners overlap, suppress lower-priority animation rather than scheduling misleading stale banners. Sound/haptic issue separate unique transition events for BoxHit/Break, BoostQueued/Active/Expired, EnemyDamaged/Dead, WaveWarning/Start, BossWarning/Arrival and BaseThreatEntered/Cleared. Low-intensity haptics are optional and independently disabled.

**320×568 and notch checks:** ≥44–48px tappable mode control and clearly snappable aim sectors; no banner hides box reward/HP, base HP, target arrows or thumb. Use icons, digits and damage-stage silhouettes in addition to colors; test high contrast, reduced motion, no mandatory audio; limit fullscreen flashes, box burst overdraw and combat health-pop spam. The palette disagreement between existing Art Draft #13 and UI Draft #17 requires coordinated art/UI Owner decision. No assets/UI code are approved by this design write-up.
## 10. ARCHITECT HANDOFF: 30Hz events, survival combat and editable versioned balance

| Mechanic/state | Legacy DEV-002.2 | v1.2 canonical target / precise **Architect questions** |
|---|---|---|
| Shot spawn/target | Auto raw shots, lane only | ShotSpawned(UnitId,TargetKind,TargetId,Tick); target immutable; attach initial HP from selected P2/P3 config |
| Gate collision | Fixed ApplyGates before ResolveContacts; QA2-01 open | **New authored arenas have NO fixed gates.** ARCH-003 #14 migration must preserve regression tests and old code until approved |
| Side boxes | No BoxState | BoxId,RemainingHp,ReservedShotIds,BoxBrokenEventId; box hit consumes surviving shooter; exactly once grant |
| Buff timers | Fixed gate multiplication | ActiveMultiplier Type/ActivatedTick/ExpiresExclusiveTick; PendingMultiplier full duration; NO shot quota; active1+pending1 |
| Queue/locks | Not present | **TerminalAwardPromises** reserve third-box final-hit entitlement before shot spawn; active+pending+promises≤2; suppressed box shot on full capacity |
| MiniBot HP | One contact kills | MiniBot MaxHP/CurrentHP, CombatTargetId, LastAttackTick, EngagedState, deterministic kill/survivor replay |
| Enemy HP | Mutual one-touch destruction | Enemy MaxHP/CurrentHP/Damage/AttackInterval, group index, target-lock/aggro, EnemyDamaged and EnemyDead |
| Simultaneous combat | Sequential pair contact | Exact TOI, same-time participant snapshot and batched HP exchange; stable IDs; kill+counterattack tie |
| Wave schedule | Minimal spawn fixture | WaveId,WarningTick,SpawnTick,SpawnGroup[] all **same tick**, stable GroupIndex/SpawnOrder; warning UI is read-only |
| Boss/base alert | Baseline breach event | Boss arrives with escorts; danger ETA derived from authoritative state, enter/clear hysteresis; boss 1HP breach only proposed |
| Fortress & terminal | Fortress hit/defeat logic | Define whether multiHP robot is consumed on fortress hit (recommended yes); terminal/late rewards deterministic |
| Replay/hash | 30Hz, ARCH-003 gate event log | Versioned gameplay ruleset/config ID included in StateHash/EventHash, stable cloned IDs, repeat-hit/cooldown and queue/timer events |
| World geometry | Scalar same-lane intersections | Real 2D box vs enemy segments/hit radii or precisely validated alternate; maintain immutable shot destination |
| Performance | Render pool capped separately from simulation | Preserve logical damage on visual culling; concurrency with 6s ×5 bursts and surviving MiniBots must be profiled |

**Equal-TOI Designer/Architect #23 TRIAL ALIGNMENT (not Owner-final):** sort chronological exact rational swept TOI first. If a box-bound MiniBot encounters an enemy at the *same* TOI as its box, **COMBAT_CONTACT first**; the MiniBot becomes engaged and **does not deal box damage at that instant, even if it survives the exchange**. It later resumes its immutable box route only on the next tick after surviving its pair engagement. An already-dead bot cannot earn a box hit. At equal-time several robots meeting one enemy, select only **one eligible 1:1 pair** by (EnemyId ASC,MiniBotId ASC), invalidating the other engagement candidates. Legacy CONTACT→GATE precedence from #14 is a separate test rule. Boost award within (k,τ) schedules activation only at tick k+1; expire/promote on exclusive end tick before spawning a new ATTACK shot. Terminal result after physical tick: base death beats fortress victory; prevent any post-terminal award. Full sign-off remains Architect exact HEAD + Owner gate.

### Editable versioned balance data (DESIGN CONTRACT, NOT C#)

Owner wants to change balance after actually playing. **Do not hardcode combat HP, damage, grouped wave sizes, box HP or boost seconds throughout gameplay logic.** Proposed Inspector/data-config layout for Architect to choose:

| Config group | Minimum editable versioned fields | Initial comparison / authoritative status |
|---|---|---|
| Ruleset | schemaVersion, balancePresetId, gameRulesVersion, seed, runtimeBuildHash | Different P2/P3 IDs required for replay comparisons |
| Friendly | miniBotMaxHp, miniBotAttackDamage, attackCooldownTicks, cannonRawSpawnIntervalTicks, speed, engagementRadius | P2=2HP, P3=3HP; remainder identical hypotheses |
| Enemy archetypes | perTypeMaxHp, perTypeAttackDamage, attackCooldownTicks, speed, colliderRadius, breachDamage | Scout1/Runner2/Armored3/Boss18 **hypotheses** |
| Upgrade boxes | boxType, maxHp, hitDamage, targetRadius, routePosition, reservedHitPolicy | **APPROVED INITIAL** maxHp5/10/25, hit1 consumed |
| Timed boosts | boostKind, durationTicks, targetFilter, maxActive, maxPending, stackPolicy | **APPROVED INITIAL:**360/300/180 ticks at30Hz, active1/pending1, nonstack |
| Waves | WaveId, warningLeadTicks, spawnTick, GroupMembers[]{type,count,side,positions}, bannerTicks | **APPROVED grouped direction**, schedule and mixes hypotheses |
| Objectives | fortressMaxHp, friendlyBaseHp, matchLengthTicks, lossPrecedence, fortressHitBehavior | Original level config untouched; sandbox fortress100 only |
| UI tuning | dangerEtaTicks, clearEtaTicks, bannerTicks, safeAreaScale, cueCooldownTicks | Warning3s approved direction, exact trigger thresholds hypotheses |

**Config hygiene:** immutable snapshot of selected preset at battle start; reject invalid negative HP/durations, duplicate WaveId/BoxId or group spawn outside match, impossible zero-damage loops and queue-overflow errors; log validation failures. Do not allow mid-match Inspector edit to retroactively change HP/timers. Each run prints preset ID/hash and replay seed. For fair P2/P3 test runs, **only friendly HP differs**; all enemies, input scripts, scheduled groups, box positions, boosts, random seed and graphics tier stay equal. Architecture/Developer select exact file format; no in-game balance editor requested.

**Performance upper-load sanity:** under 3 raw/s, a continuous ×5 six-second window emits **18 raw×5=90 MiniBots**, peak **15 logical bots/s** (versus baseline3/s); some survive and remain alive longer. At a hypothetical5s uninterrupted corridor, that burst can place ~75 boosted bots in transit simultaneously, **before** surviving combatants are added. A single full-duration ×5 box in60s emits at most 180−25+72=**227** field-bound logical units under ideal ATTACK use; ×3 alone 180−10+60=**230**; ×2 alone 180−5+36=**211**. Three sequential fully used ×2/×3/×5 rewards with box costs5+10+25 can emit at most 180−40+36+60+72=**308** in60s (assumes attainable acquisition timing and constant ATTACK during rewards). **These are totals spawned, not simultaneously alive**; actual load depends on engagement lifetime, routing and enemy group clustering. The 120 visible pool cap is **not** a logical simulation cap. No deletion of simulated damage/health for FPS. Performance/QA must benchmark on actual selected Android/iOS device classes.
## 11. Gameplay, balance, replay, UI and mobile verification matrix — ALL UNRUN

**Matched-playtest rule:** execute every comparative scenario twice as P2 (MiniBot HP2) and P3 (HP3), with **identical scripted thumb inputs, enemy group seed, level geometry, attack damage, cooldown, box HP, boost timers and graphics configuration**. Record at least: result/baseHP, fortressHP, enemy kills, time to defeat each group, MiniBot health on survival, active buff uptime, units spent on boxes vs combat, boxes claimed/queued, breach ETA, input errors, live logical/visible units, fixed-tick replay hash, CPU/GC/FPS/thermal. Presets are **not Owner selections**.

| Test ID | Exact focus | Expected pass/fail evidence |
|---|---|---|
| GD12-Q01 | P2 HP2 vs P3 HP3 in W1 four simultaneous Scouts | Show actual hits surviving and second engagements; compare base damage, do not infer win |
| GD12-Q02 | W2 6 mixed enemies spawned same tick | All GroupMember IDs spawn one tick; none dripped one-by-one; no overlap exploits |
| GD12-Q03 | W3 7 / W4 8 / boss group9 | Monotonic authored group counts4→6→7→8→9, visible spawn with readable silhouettes |
| GD12-Q04 | 1 MiniBot vs Scout, Runner, Armored in P2/P3 | HP decrements each exchange; survival matches §7 serial duel examples |
| GD12-Q05 | 2+ allies meet one Armored at same TOI | Only one 1:1 engagement chosen by EnemyId/MiniBotId, other candidates invalidated/recomputed; no duplicated strike |
| GD12-Q06 | MiniBot falls to0HP same instant enemy falls to0 | Simultaneous eligible attacks apply; no one-sided sort advantage |
| GD12-Q07 | MiniBot survives contact, disengages and hits a second enemy | Combat HP persists; cooldown prevents repeated hits every render frame |
| GD12-Q08 | MiniBot HP2/HP3 successfully hits a box | Regardless remaining combat HP, box takes1 damage and MiniBot is consumed |
| GD12-Q09 | Reserved box-bound MiniBot killed by enemy before arrival | Reservation released, no box HP lost, shot not rerouted, no reward |
| GD12-Q10 | 2+ shots reach HP1 box same tick | Exactly one BoxBroken/RewardClaimed; no negative box HP or duplicate multiplier |
| GD12-Q11 | Exact enemy-vs-box TOI tie for same box-bound MiniBot | Enemy engages first; box HP unchanged that instant even if bot survives; resume original box route only after engagement |
| GD12-Q12 | ×2 box5HP, ATTACK uninterrupted12s | ×2 for timer only; approximately36 boosted raw shots at3/s; **no charge expiration** |
| GD12-Q13 | ×3 box10HP, ATTACK uninterrupted10s | Approximately30 boosted raw shots/90 MiniBots; expiry on exact signed tick |
| GD12-Q14 | ×5 box25HP, ATTACK uninterrupted6s | Approximately18 boosted raw shots/90 MiniBots; logical count, CPU/GC measured |
| GD12-Q15 | ×2 active; ×3 claimed and queued | No ×6 stack; ×3 starts full10s after ×2 full12s expiry |
| GD12-Q16 | active1/pending1 full; target third multiplier box | Lock before spending; preserve boxHP; unlock on slot availability |
| GD12-Q17 | Active + pending + promised terminal awards reaches2 | New third-box shot suppressed before any expenditure; terminal promise atomic; no third earned reward lost |
| GD12-Q18 | Buff active; aim FORGE/box, then ATTACK | Time continues in both modes (proposal), shots assigned at spawn; no retroactive ×N |
| GD12-Q19 | ×3 vs ×5 for W1 at identical fixed inputs | Record actual breach count and value of first boosted contact; show ×5 can be risky, not automatic winner |
| GD12-Q20 | Group W5 boss with escorts, P2/P3 | Boss/escort HP correct, no trivial solo spawn, assess 1HP breach rule and boss time-to-kill |
| GD12-Q21 | Warnings3/13/23/32/40, spawns6/16/26/35/43 | Exactly3s lead, WAVE n/N and BOSS priority, no pause or input blockers |
| GD12-Q22 | BASE DANGER, BOSS and queued boost alerts overlap | Base danger highest transient; active timer/NEXT and aim always readable, no haptic spam |
| GD12-Q23 | Same-tick fortress/base destruction or timer60 | Loss precedence preserved; post-terminal box/buff events ignored and Restart clean |
| GD12-Q24 | 30Hz sim with 30/60/variable render FPS, same preset/seed | Same EventHash/StateHash, entity IDs, engagement HP and timer outcomes |
| GD12-Q25 | Old DEV-002.2 QA2-01 Gate-vs-Contact regression | Preserve original gate tests/fixtures until migration authorized; no silent deletion |
| GD12-Q26 | GD-004 original first wave 6 drones and base3HP | Confirm ≥4 intercepted in actual L3 2D/combat scenario if Owner retains criterion; no diagram-only PASS |
| GD12-Q27 | Entry portrait 320×568 with ×5 burst + 9-enemy boss group | Target, HP pips, overlays, 30Hz logic, device FPS/GC/thermal measured on Android/iOS |
| GD12-Q28 | Edit P2→P3 config between independent runs | Only documented config values change; run stores preset version/hash, old replay remains stable |

**Playtest acceptance goals** (all **targets**, not evidence): no box consumes more than required HP hits; 0 duplicate reward grants; 0 same-tick nondeterminism across replay frame rates; no silent boost loss; no one-by-one wave drip; 100% tested thumb interactions accessible under alerts; multiHP damage visible and unambiguous; neither ×3 nor ×5 universally dominant across varied scenarios; P2/P3 contrast reveals useful, observable differences before Owner chooses HP. Track first-time comprehension and session enjoyment in real player interviews.

**Critical caution:** an optimistic attack-production count does not establish a wave's intercept count, a robot's survival, a boss kill or fortress victory. Independent QA and Architect must prove timing/contact correctness from an actual runnable build and replay logs; Game Designer has run **zero** Unity or device tests.
## 12. Owner decision register — CONFIRMED versus OPEN

Owner approvals are initial design constants and directional rules, **not** runtime-test results, merge permission or final parameter locks. Remaining OPEN subchoices are explicitly separated.

| ID | Subject | CURRENT OWNER STATUS | What remains for design/Architect/playtest |
|---|---|---|---|
| O12-01 | Box hit cost | **APPROVED:** successful 1HP box hit consumes one MiniBot; no special projectile | Deterministic same-TOI enemy/box order; lost reservation handling |
| O12-02 | Combat durability | **APPROVED DIRECTION:** MiniBots survive enemy contacts; enemy HP multi-hit; **MiniBot HP 2/3 DEFERRED** | Decide via matched P2/P3 playtest; attack damage/interval/targeting/engagement also OPEN |
| O12-03 | Boost duration | **APPROVED:** time-only ×2=12s, ×3=10s, ×5=6s; **no shot quota** | Start/expire fixed-tick boundary, FORGE timer behavior, balance after playtest |
| O12-04 | Boost stacking/queue | **APPROVED:** nonstack active1+pending1, full duration on queued activation; lock other boxes | **PROVISIONAL #23:** terminal entitlement promises, active+pending+promises≤2; no escrow; Architect exact-HEAD confirmation still required |
| O12-05 | Fixed gates | **APPROVED NEW DESIGN:** replace fixed gates with boxes in authored v1.2 arenas | Keep old gate code/assets/tests, Architect+Owner migration plan before deletion |
| O12-06 | Box durability | **APPROVED INITIAL:** HP5/10/25 for ×2/×3/×5 | Configurable balance and later tests; other reward boxes OPEN |
| O12-07 | Side-box placement | **OPEN:** two visual slots near (±2.5,5), radius≈0.55 visual only | Authoritative fixed-point swept AABB dimensions and narrow-screen safe layout remain to be approved |
| O12-08 | Group wave schedule | **APPROVED DIRECTION:** simultaneous groups, stronger count/HP/types; **OPEN** 5-wave example | Exact groups4/6/7/8/9 at6/16/26/35/43 must be simulated/Owner reviewed |
| O12-09 | Boss rules | **APPROVED DIRECTION:** distinct boss with escorts and warning; numeric **OPEN** | Recommend bossHP18 at1.1u/s, 1 base breachHP; do not hardcode |
| O12-10 | L1–L3 authored progression | **OPEN:** propose L1 ×2 boxes, L2 ×2/×3, L3 ×3/×5 and grouped armor | Existing level balance/old 270HP unchanged pending approval |
| O12-11 | First implementation slice | **OPEN:** only docs/architecture/test plan approved now | Architect resolves chronology/QA2-01 migration; Owner separately authorizes DEV code |
| O12-12 | Contact tie & replay | **TRIAL DESIGN SIGNOFF (PROVISIONAL):** first-hit simultaneous pair exchange, enemy before box on equal TOI; stable IDs | Architect #23 exact-HEAD reconciliation and Owner feedback needed; no old gate QA2-01 migration implied |
| O12-13 | Shield/weapon/fire-rate | **OPEN:** future candidate rewards, not first playable default | Distinct slot semantics, stacking, target and VFX contracts |
| O12-14 | Alerts | **APPROVED DIRECTION:** 3s warning, WAVE n/N, 1–1.5s callout, boss/base warning | PROPOSED base danger ETA2s, clear hysteresis2.5s and 1.25s banner |
| O12-15 | Performance target | **OPEN:** no device class or FPS certified | Benchmark 15 logical MiniBots/s ×5 burst + surviving combats on representative devices |
| O12-16 | Fortress balance | **UNCHANGED:** old L3 270HP; 100HP is test sandbox **only** | Full simulation-backed retune before authored L3 migration |
| O12-17 | Fortress impact by survivor | **OPEN:** recommend one damage then MiniBot consumed | Avoid repeated invulnerable fortress DPS, confirm with Owner |
| O12-18 | Combat engagement & damage | **OPEN:** PROPOSE simultaneous first exchange, repeated15-tick cooldown, paused pairs | Evaluate whether boss gets overwhelmed; tune target aggro and movement |
| O12-19 | Editable balance storage | **APPROVED REQUIREMENT:** versioned, editable test data, no locked HP choice | Architect chooses config schema/validation, preset provenance; no in-game balance editor requested |

**Next Owner review priority:** (1) Approve/revise the **provisional pair-only** combat model for a bounded P2; HP2 and HP3 remain test presets; (2) review terminal-hit promises, 2D swept rectangles, next-tick timer activation and locked-shot suppression from Architect #23; (3) authorize P2 **only** after exact-HEAD Architect sign-off and P1 checks under GITHUB-006. Legacy QA2-01 evidence must be preserved. No GDD merge.
## 13. Change log, role dependencies, risks and Owner-gated next steps

### GD-006 original draft → GITHUB-005 revised draft

| Original Draft PR #19 assumption | Owner-confirmed replacement / new test obligation |
|---|---|
| Box MiniBot consumption was merely recommended | **APPROVED:** successful box hit consumes MiniBot; durability5/10/25 initially |
| Multiplier lasted next18/15/12 ATTACK shots with max timer | **SUPERSEDED:** ×2 12s, ×3 10s, ×5 6s time-only, no per-shot quota |
| active1+pending1 was unapproved suggestion | **APPROVED:** nonstacking sequential active1+pending1; visibly lock further multiplier boxes; never lose reward |
| Fixed gates removal was a design option | **APPROVED TARGET:** destructible boxes replace gates in new v1.2 arenas; keep old gate code/tests until approved migration |
| Friendly MiniBot HP1 and consumed on every enemy hit | **SUPERSEDED:** survive enemy fights; **HP2 vs HP3 explicitly deferred** and compared as editable presets |
| Enemy one-hit mutual-consumption and small isolated spawn encounters | **SUPERSEDED:** enemy multiHP, same-tick grouped waves with increasing count, mixed types and boss escorts |
| Old 60s enemyHP70, one-hit-to-kill budget, −enemyHP fortress calculation | **REPLACED:** group counts4/6/7/8/9 =34 enemies/93HP *hypothesis*; survival combat invalidates naive subtraction |
| Old 18 QA cases and limited contract fields | **UPDATED:** 28 unrun tests, timed-buff and survival events, config schema/preset replay requirements |

### Specialist handoffs / blockers

| Responsible role | Needed from this GDD before work | Scope/hold |
|---|---|---|
| 01 Game Director / Owner | Review §12 decisions, sign off design and separately authorize any implementation or merge | No implicit approval from GITHUB-005 |
| 02 Architect [#2](https://github.com/Gogenco/swarmforge-studio/issues/2) | Verify this GDD's new exact HEAD against ARCH #23; formalize pair-only 30Hz contract, terminal-hit promises, swept AABB, same-time enemy-before-box tie, next-tick boosts and QA2-01 separation | Must post ARCH_V12_EXACT_HEAD_ALIGNED before Developer P2 |
| 04 Developer [#4](https://github.com/Gogenco/swarmforge-studio/issues/4) | Narrow staged first playable prototype implementation **plan** with editable preset configs, no code until Owner+Architect sign-off | Do not alter merged DEV-002.2 or start VS-02 |
| 09 QA [#9](https://github.com/Gogenco/swarmforge-studio/issues/9) | Prepare §11 matched P2/P3 scripts plus original six-drone defense and replay/hash cases | No assumed or fabricated PASS |
| 08 Performance [#8](https://github.com/Gogenco/swarmforge-studio/issues/8) | Rebudget 15 logical bots/s ×5 six-second burst, survivors extending lifetime, 9-enemy boss group, low-end device profiler scenarios | Render cap not logical count |
| 06 UI [#6](https://github.com/Gogenco/swarmforge-studio/issues/6) | Time countdown instead of charges; active/NEXT and LOCKED states, multiHP pips, group warnings, base alert priority | Check 320×568 and existing palette conflict |
| 05 Art [#5](https://github.com/Gogenco/swarmforge-studio/issues/5) | Damage-stage MiniBot survival and enemy armor pips, group silhouettes, box damage and acquisition cues | No new full assets without implementation scope |
| 07 Sound [#7](https://github.com/Gogenco/swarmforge-studio/issues/7) | Distinguish combat nonlethal hit vs death, box hit vs break, boost queued/activated/expired, grouped wave/boss and threat enter/clear | Sound/haptic from deterministic events only |

### Risk register (NONE verified in runtime)

1. **R12-01 / CRITICAL — QA2-01 unresolved.** Old fixed-gate chronology and new box/engagement chronology can diverge. Preserve evidence, migration tests and Architect contract before Developer changes.
2. **R12-02 / HIGH — HP3 survivor snowball.** One MiniBot can defeat multiple weak enemies and still hit the fortress; P3 might trivialize group defenses. Compare matched P2/P3 before choosing, tune damage/cooldown separately.
3. **R12-03 / HIGH — grouped-wave congestion and pair locking.** Multiple enemies in one spawn can occupy many simultaneous 1:1 engagements or run past unengaged defenders. Test candidate revalidation, path stoppage and available target selection; there is NO multi-attacker same-enemy focus fire in Stage P2.
4. **R12-04 / HIGH — ×5 traps early defense.** 25 raw box hits and9.49s acquisition delay leave little before earliest W1 breach; high peak burst does not mean global best ROI. No guaranteed victory until playtest.
5. **R12-05 / HIGH — original L3 math no longer valid.** Prior 270HP versus sample100HP and new HP2/3 repeat hits cannot be reconciled by old unit subtraction. Retune only after Owner approval.
6. **R12-06 / HIGH — logical entity concurrency.** Active ×5 generates90 units/6s at3 raw/s; survivors linger, broadphase/contact/GC can exceed old 120 visible cap assumptions. Device profile required.
7. **R12-07 / HIGH — terminal entitlement race.** Before the final box shot is dispatched, reserve its potential reward; maintain active+pending+terminalPromises≤2. If terminal bot dies, release promise and safely unlock targets. Without this invariant Stage P2 should STOP.
8. **R12-08 / HIGH — narrow mobile screen.** Five thumb sectors, 2 side boxes, 9-enemy fan, base HUD and boss warning could obscure each other. UI 320×568 and actual hands-on tests needed.
9. **R12-09 / MEDIUM — wave announcement fatigue.** Haptics and banners can cover target feedback if duplicated per tick. Unique logical events, cooldown and reduce-motion settings needed.
10. **R12-10 / MEDIUM — future Forge loss of identity.** Timed upgrades currently proposed ATTACK-only; ensure original blueprint selection/reservations/Mech/Titan survive design-to-implementation migration without silent removal.
11. **R12-11 / MEDIUM — replay invalidated by balance edits.** Store immutable preset ID/schema/version/seed and output hash per run; do not mutate config mid-match.

### Required order of work

1. **This same Draft PR #19:** record all approved Owner design choices, show P2/P3 side-by-side, keep original PR history as additive commit. No merge.
2. **Architect exact-head specification:** reconcile current Designer PR #19 with Architect PR #23, issue ARCH_V12_EXACT_HEAD_ALIGNED or exact BLOCKED; preserve legacy QA2-01 separate.
3. **Developer Stage P1 NOW authorized by GITHUB-006:** isolated branch/Draft PR with versioned editable config, P2/P3 presets, replay/config test seam, legacy untouched. Run relevant checks and report exact HEAD; NO merge.
4. **Developer Stage P2 ONLY AFTER HARD GATES:** ARCH_V12_EXACT_HEAD_ALIGNED, Designer identical trial contract and passing P1 checks. Separate Draft PR for **one** 60s opt-in arena with one ×2HP5 box / ×2 12s, one grouped wave, HP2/P3 combat, base/fortress and restart. Test actual Unity behavior and replay; NOT the future ×3/×5/boss/five-wave suite.
5. **Owner selection after playtest:** choose/retune MiniBot maxHP, enemy HP/damage/cooldown, wave sizes/times, fortress HP and reward timers based on evidence. This documentation **does not** preselect HP2 or HP3.

**Checks actually performed:** read Owner GITHUB-005 Issue #18 decision chain and latest role Issue #3 comment, reconciled arithmetic for timed boosts, 34 grouped units/93 enemy HP and both survival presets conceptually, and kept the PR scope documentation-only. **Not performed:** Unity compilation, actual EditMode/PlayMode execution, battle simulation, replay hash verification, actual winning/losing runs, player research or mobile measurements.

**Historical GITHUB-005 milestone:** `GDD_V12_OWNER_DECISIONS_INTEGRATED`. **Superseded for current task by §14 GITHUB-006 provisional trial-contract sign-off.** Draft PR #19 remains unmerged; no Designer Unity changes, no VS-02 and no claim of final balanced or playable build.


## 14. GITHUB-006 — STAGE P1/P2 DESIGNER ↔ ARCHITECT TRIAL CONTRACT (2026-11-14)

**Governing directive:** [Owner Issue #18 / GITHUB-006](https://github.com/Gogenco/swarmforge-studio/issues/18#issuecomment-6880563570). This section is a **DESIGNER SIGN-OFF ON ONE PROVISIONAL P2 TRIAL CONTRACT**, reconciled conceptually with [Architect Draft #23](https://github.com/Gogenco/swarmforge-studio/pull/23) at **HEAD 33d45659ec072523b6a709677fe827c9f9ab606e**, not a claim that Architect has yet checked this later GDD exact HEAD. **Architect must independently post ARCH_V12_EXACT_HEAD_ALIGNED or BLOCKED.** Owner-approved gameplay principles are distinguished below from experimental algorithm choices. No GDD or architecture merge.

### 14.1 No-critical-risk assessment and Stage boundaries

- **Stage P1: NOW AUTHORIZED FOR GAME DEVELOPER ONLY** to implement opt-in, immutable, versioned and validated balance snapshots; HP2/HP3 test presets, replay/config compatibility tests and legacy isolation on a separate Draft PR. It is not an authorization for the Game Designer to edit C#.
- **Stage P2: HARD-GATED.** Developer may start the separate FIRST PLAYABLE trial Draft PR only after (1) Architect publishes exact-HEAD alignment, (2) this Designer/Architect trial contract is confirmed identical, **and** (3) P1 tests/checks pass. If any gate fails, no P2 code.
- **P2 maximum gameplay scope:** one temporary 60s opt-in arena; one automatic one-thumb cannon; one **×2 box with HP5** consuming one MiniBot on each successful hit; ×2 multiplier for **12s** (360 ticks); MiniBots survive enemy combat using P2 or P3 editable HP; **one simultaneously spawning enemy group**; friendly base/enemy fortress victory/defeat, restart and real replay/Unity tests. That is **not** L1–L3 migration, full Forge, ×3/×5, boss, five waves, art-heavy UI/audio or VS-02.
- **Future P3 (NOT AUTHORIZED):** full ×3/×5, queue overflow in a live 3-box encounter, multi-wave progression, boss, Forge interactions and full production balancing. Their *rules* may be specified/tested in core math now, not implemented as gameplay P2 without new Owner decision.
- **Critical risk verdict:** no unmitigated critical obstacle to isolated **P1 config/replay** preparation. **P2 remains gated** on exact-head alignment and tests, particularly 1:1 combat/box same-time, reward promises and replay correctness; STOP if any causes damage loops, dropped rewards or incompatible IDs/hashes.

### 14.2 Single provisional deterministic combat/impact contract — DESIGNER ACCEPTANCE

| Contract field | Designer-approved **trial hypothesis** aligned with ARCH #23 | Required P2 oracle |
|---|---|---|
| Health presets | P2 MiniBotMaxHP=2 / P3=3; **neither final**; equal damage1, 30Hz, same enemy/group/commands | Both snapshots immutable, distinct config hashes, no live Inspector retroactive mutation |
| Combat pairing | **One MiniBot ↔ one enemy** active engagement per actor; pair stationary; occupied actors unavailable to third attacker | Two allies encountering one enemy same TOI start only one stable pair |
| First strike | At actual chronological swept contact time **(k,τ)**, both eligible combatants exchange simultaneous integer damage from pre-strike alive snapshot | Lethal mutual attack applies both damages; no ordering advantage |
| Positive repeat cooldown | Configurable positive integer (trial15); first repeat **tick k+1+15** after first-hit tick k; subsequent proposed exact ticks **r+15** | No render-FPS damage, no same-tick repeats; Architect must confirm recurring phase |
| Survival and continuation | At HP>0 after a partner dies, survivor resumes original immutable route **the next tick**; 0HP unit never acts again | HP2/HP3 serial Scout/Runner/Armored cases and another later target |
| 2D contacts | Proposed fixed-point **swept 2D axis-aligned rectangles**, exact rational time-of-impact, stable X/Z waypoints; logical hitbox shape editable in config | Side box hit only by box-targeted units; no invisible same-lane/cross-lane contact |
| Enemy vs box equal-time | **COMBAT_CONTACT precedes BOX_HIT** on exact TOI tie: box-bound MiniBot pairs with enemy, **no box damage at that instant even if bot survives** | Box HP invariant, no dual contact consumption; resume later on original route if still alive |
| Box impact | One *successful* box hit costs exactly one living MiniBot, always deals1 HP regardless of its HP2/HP3; on-route death releases reservation | Five successful hits to destroy boxHP5; no free hits, no duplicate grant |
| Queue safety | Reserve terminal award entitlement before final covering shot: **active+pending+promised≤2**; while full, all other multiplier boxes target-locked; suppress raw shot without expenditure or reroute | No third reward loss or phantom bot; promise freed if terminal shot dies |
| Activation/expiry | Box award during tick k activates **start k+1** if slot free; active duration exclusive **[a,a+360)** for ×2; expiry and queued promotion precede shot creation on expiry tick | Shot before activation remains ×1; boundary-shot uses correct boost, no overlap/gap |
| Raw shot and enemy IDs | Original raw unit ID before boost children ordered clone1…N−1; enemy group IDs ordered Wave→Group→Lane→Formation; spawned units first move next tick | Same ID/EventHash/StateHash on deterministic replay at different render FPS |
| Wave group | **All members born on same spawn tick**, stable group/formation ordering; UI warning not a spawn scheduler | P2 one group of 4 Scouts in same tick, not individual drips |
| Terminal | Process physical contacts (including earlier intra-tick legal rewards) then compare result: friendly baseHP≤0 ⇒ LOSS, else fortressHP≤0 ⇒ WIN, else 60s expiry ⇒ LOSS; freeze all later events | Base+fortress die same logical tick ⇒ LOSS; no postterminal grant/mutation; restart clean |
| Legacy | v1.2 opt-in ruleset separate from unchanged DEV-002.2 and old ARCH-003 QA2-01 fixture | No old C# assets/config deleted; old deterministic regression still runs |

**Exact repeat tick note:** ARCH #23 suggests first repeat no earlier than k+1+CooldownTicks after first collision; **P2 proposes that as the exact first repeat**, then repeating every CooldownTicks thereafter. Architect must confirm this recurrence, fractional first contact treatment and the original **spawn raw shot before group wave, and new units first move next tick** deterministic phases; if different, publish the exact difference before P2.

**P2 example authored data (ALL TEST CANDIDATES, NOT NEW OWNER-APPROVED LEVEL BALANCE):** 60s/1800 ticks; friendly base3HP; fortress trial90HP **editable**; raw cannon3/s; box LEFT ×2 HP5 at sample (-2.5,5.0) visual location; ×2 for360 ticks; one wave warning at t3/tick90 and **four Scouts in one group at t6/tick180**, two on each side; Scout HP1/speed2.3u/s/damage1; P2/P3 only differs in MiniBot maxHP. Real hitbox half-extents, formation slots, 2D waypoints, box target snapping, cooldown and breach check belong to Architect's versioned config schema and must be validated by reproducible P1/P2 QA. The trial fortress90HP is **not** old L3=270HP and does not modify the stable VS-01 scene.

**P2 test gate (all UNRUN):** exact-P2/P3 pair exchange, repeated15-tick cooldown, surviving bot reengagement, box consumes even HP3 MiniBot, successful ×2 one-time award, active next-tick/time exclusive, same-time enemy/box priority, group count4 spawned atomically, base+fortress precedence, restart, 30/60 FPS replay equivalence, config-hash isolation, original QA2-01 regression preservation and one-finger mobile portrait input. Log any missing Unity/device verification honestly.

### 14.3 Outstanding Owner/Architect decisions — not blockers for P1 config work

1. **Trial mechanic sign-off:** Architect must check this exact GDD commit against Architect Draft #23 and confirm/revise pair-only engagement, first/repeat attack timing, same-time enemy-before-box rule, terminal entitlement promises, locked-shot suppression and timer boundary. This is the **mandatory P2 gate**.
2. **HP2/HP3 remain open by Owner decision.** Neither is a final winner; Developer must expose editable balanced presets and QA must compare them.
3. **Full 2D geometry/aim:** proposed swept rectangles need bounds/waypoints that allow P2 one group and box interception without hidden cross-lane damage; Inspector data and numerical tests, not a diagram assertion.
4. **Later gameplay remains open:** optimal ×3/×5 dynamics, boss and five-group 60s sample, Forge, shield/weapon/firerate queue interaction, full L1–L3 balance, old270HP authored level retune and rendering budgets. None is authorized in P2.
5. **Any inconsistency with ARCH #23 or game loop:** stop before P2, publish exact contradictory clauses and seek Owner clarification. No silent guess or code workaround.

**Designer trial-contract status:** `GDD_V12_GITHUB006_DESIGNER_TRIAL_SIGNOFF` — provisional algorithm accepted for a bounded trial, **ARCH_V12_EXACT_HEAD_ALIGNED pending**; P1 authorized separately for Developer, P2 conditional, no code/merge/VS-02 by Game Designer.

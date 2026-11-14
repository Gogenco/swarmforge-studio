# ART-002 — VS-01 Art Direction & Readability Mini-Spec

**Status:** DRAFT / READY FOR OWNER REVIEW (specification only; not an implementation sign-off)  
**Owner:** 05 — Art Director  
**Issue:** [#5](https://github.com/Gogenco/swarmforge-studio/issues/5)  
**Baseline:** SWARMFORGE DEV-002.2, `main` merge commit [`99cfcdc0`](https://github.com/Gogenco/swarmforge-studio/commit/99cfcdc0b4982e9e441cf23a8669a487a7d75ea9)  
**Related:** [master #1](https://github.com/Gogenco/swarmforge-studio/issues/1), [UI-002 #6](https://github.com/Gogenco/swarmforge-studio/issues/6), [PERF-002 #8](https://github.com/Gogenco/swarmforge-studio/issues/8), [QA-003 #9](https://github.com/Gogenco/swarmforge-studio/issues/9)  
**Engine:** Unity 6000.3.28f1, URP 17.3.0; deterministic simulation 30 Hz  
**Version:** ART-002 v1.0 proposal, 2026-11-14

## 1. Intent, boundaries and evidence level

Create a cohesive and readable **portrait mobile VS-01 graybox**. A player must distinguish friendly MiniBots, enemy drones, cannon/base, fortress state and the three gate effects within one glance, including in dense scenes and on small screens.

**In scope:** proposed semantic color tokens, shapes/patterns, world/UI separation, contrast targets, art review matrix, minimal asset/reference inventory and risk register.

**Out of scope:** Unity code or shader modifications, materials/assets implementation, changing level geometry or balance, modifying the simulation, changing HUD layout/typography (owned by UI-002), new gameplay mechanics, Forge, Mech, Titan or VS-02 content. All future implementation requires separate Owner approval. No change to the visual cap may affect logic.

**Evidence:** Source inspected in `Assets/Swarmforge/Scripts/Presentation/PrimitiveBattleView.cs`, `BattleEntryPoint.cs`, `Configs/Levels/VS01_Arena.asset`, and `docs/qa/QA002_SOURCE_FINDINGS.md`. No Unity Editor, mobile device, runtime screenshot, contrast capture, profiler or frame-time benchmark was performed for ART-002. Values below are **proposed design targets, not measured game results**. QA2-02 remains open.

## 2. Current graybox: observed in source

| Element | Actual DEV-002.2 presentation | Readability concern / interpretation |
| --- | --- | --- |
| Floor and lanes | Dark blue-gray cube planes (`Color(.11,.16,.24)`, `Color(.19,.29,.41)`) | Visually similar planar layers; check dark-device contrast. |
| Friendly MiniBot | Bright mint-green **capsule**, `Color(.22,.95,.70)` | Strong hue but can be tiny in a 120-visual crowd. |
| Enemy Drone | Red **sphere**, `Color(1,.26,.32)` | Color and silhouette differ from bot; verify at small projected size and for color-vision deficiency. |
| Cannon | Green **cube**, shares MiniBot material | Player-controlled anchor can be mistaken for an enlarged bot. |
| Player base | Cyan block, shares ×2 gate material | Potential false affordance: base may look like an ×2 gate. |
| Gate ×2 / ×1 / ×3 | Cyan / neutral gray / yellow blocks | ×1 neutral strip is narrow between left/right threshold bands; must not imply a multiplier. |
| Fortress | Gray → warm amber/brown → red → very dark; stage also changes height | Need unmistakable damage hierarchy even in grayscale. |
| Camera | Orthographic, `orthographicSize = 12.5f`, portrait | Actual projected pixel size varies with screen, aspect and Unity rendering. |
| HUD/terminal | IMGUI text, boxes, world-projected gate labels; reference scale based on 480×850 | QA2-02 notes phone readability; exact sizing/anchors belong to UI-002. |

**Authoritative spatial semantics:** `GateLeftMaxX=-120`, `GateRightMinX=120`; left `×2`, center `×1` bypass, right `×3`. The visual design must never relabel or widen the underlying interaction zones without a Game Designer/Architect-approved gameplay change. The presentation currently caps displayed MiniBots at `MaxVisibleMiniBots=120` without capping simulation.

## 3. Proposed visual language

**Art style:** compact, clean **tactical arcade sci-fi**. Readable forms and strong semantic layers take precedence over surface detail. No bloom dependence, photoreal textures, expensive transparent particles, rim-light shaders or detailed 3D redesign in VS-01.

### 3.1 Proposed semantic palette (hex, display-sRGB references)

| Role | Proposed token | Meaning and use |
| --- | --- | --- |
| Background | `#11192D` | Darkest world field; not interchangeable with HUD surface. |
| HUD / terminal panel | `#101C2C` | Opaque or near-opaque dark surface behind text. |
| Primary text | `#F7FAFF` | Normal readable interface text. |
| Secondary text | `#C4D3E5` | Hints and less prominent statistics. |
| Friendly MiniBot | `#37E4B0` | Friendly unit body; capsule silhouette retained. |
| Enemy Drone | `#FF6472` | Hostile unit body; sphere silhouette retained. |
| Gate ×2 | `#49B7F2` | Positive ×2, **two-bar/chevron** secondary cue. |
| Gate ×1 | `#93A1B0` | Neutral bypass, **single dash** cue; no success glow. |
| Gate ×3 | `#FFC74F` | Positive ×3, **three-bar/chevron** secondary cue. |
| Dark text on bright gate chips | `#091322` | Gate glyphs/badges where light background is used. |

These are **design tokens**, not exact Unity `Color`/URP conversions. Unity color-space settings, tonemapping, material lighting, transparency, display calibration and text antialiasing can change rendered contrast. Any implementation must check captured pixel values from target builds.

Calculated contrast of the **flat, opaque sRGB tokens** only (WCAG luminance model, not a Unity measurement): primary text on panel **16.39:1**; secondary text on panel **11.27:1**; dark gate text on ×2 **8.28:1**, ×1 **7.06:1**, ×3 **12.00:1**. World-color-to-background tokens: MiniBot **10.73:1**, Enemy **6.09:1**, ×2 **7.78:1**, ×1 **6.63:1**, ×3 **11.27:1**. These numbers do **not** validate projected small-unit detection or final contrast in scene lighting.

**Acceptance thresholds for the eventual rendered implementation:** text `>=4.5:1` for normal size and `>=3:1` for large text; meaningful UI symbols and object boundaries `>=3:1` against immediate surroundings where applicable. Treat these as engineering review targets inspired by WCAG, not a claim that a real-time game is formally WCAG certified. Avoid relying on hue alone.

### 3.2 Object form and value hierarchy

1. **MiniBot:** preserve a narrow, upright capsule/profile. Distinct mint hue and a bright central highlight at the scale where the model remains legible; do not introduce bones/Animator or alter movement. In clusters, prioritize the leading edge and group silhouette over decorating every unit.
2. **Enemy Drone:** retain a round/spherical base profile and coral-red hostile hue. A dark core or equatorial band is a *future optional art detail* only if it survives mobile pixel size and performance review. Never substitute the friendly silhouette.
3. **Cannon:** must be recognizable as a fixed **turret anchor**, not another MiniBot. Proposal: darker base + clearly oriented barrel/wedge outline. Current game logic and touch drag region remain unchanged. Any mesh replacement needs separate approval.
4. **Player base:** neutral structural body with its own outline/striping; do not reuse ×2 “multiplier” visual semantics. Cyan may remain an accent, not the primary fill.
5. **Fortress:** preserve the four current gameplay states and the current transform changes. Suggested art-facing severity ladder: intact = cool neutral, damaged = amber warning, critical = red and broken/sparse border, destroyed = dark remnant. Each state needs a non-color cue (visible shape/height/mark); do not alter hit points.
6. **World:** floor stays visually behind units; lanes should guide orientation without introducing saturated competing stripes. Gate and base silhouettes remain separated in perspective.

### 3.3 Three-gate visual contract

| Zone | Existing gameplay condition | Proposed visuals | Must never imply |
| --- | --- | --- | --- |
| LEFT ×2 | `x <= -120` | Cyan bar, **×2** text and two repeated notch/chevron cues | ×3 or guaranteed survival |
| CENTER ×1 | `-120 < x < 120` | Desaturated gray gap, **×1** text and one horizontal dash | Multiplication/bonus |
| RIGHT ×3 | `x >= 120` | Amber bar, **×3** text and three notch/chevron cues | ×2 or neutral |

Keep labels readable when seen **without** the color backgrounds. Avoid text directly over similarly colored units. Prefer solid text badges with dark label text and a defined border; UI-002 owns final geometry, sizes and safe-area behavior. No change to gate threshold coordinates or current `GateLabelWorld()` projection under ART-002.

### 3.4 HUD and terminal composition handoff to UI-002

- **Art-owned:** palette tokens, image/text contrast targets, visual separation of foreground/background, icon/gate semantics and consistency of state colors.
- **UI-002-owned:** reference canvas and scaling formula, typography, button dimensions, screen anchors, four-sided `Screen.safeArea`, wrapping, small-screen constraints, notched-device fallbacks and final 320×568 / 360×640 / 390×844 / tall-aspect mockups.
- During normal play keep HUD panels darker and visually quieter than units/gates; no animated decoration behind primary statistics. Distinguish `BASE`, `FORT` and `TIME` by hierarchy/spacing, not additional competing neon colors.
- Terminal **VICTORY**, **BASE DESTROYED**, **TIME UP**, and **PAUSED** must retain readable text and a visible `RESTART`/`RESUME` action over a stable dimmed backdrop; no outcome should depend solely on red/green color.
- Prevent top HUD, world-projected ×1/×2/×3 labels and bottom drag guidance from colliding at narrow widths. Request exact collision-free layouts and tap sizes from UI-002, not undocumented ad-hoc art changes.

## 4. Minimal assets / references needed (no asset procurement authorized)

| Deliverable | Required now? | Purpose / implementation note |
| --- | --- | --- |
| This mini-spec + palette swatches | **Yes, documentation only** | Owner reviews semantic colors before any Unity changes. |
| One 9:16 current-state screenshot with HUD + all three gates | **Needed for visual validation** | Owner/QA capture in Unity; no simulated screenshots are treated as runtime evidence. |
| One dense-bot (up to 120 displayed) and one hostile-contact screenshot | **Needed for validation** | Evaluate clutter and hostile/friendly identification; do not change visual cap. |
| Win / base-loss / timeout / pause screenshots | **Needed for validation** | Verify overlay legibility; can be supplied in UI-002 mockup and Owner/QA runtime pack. |
| Optional simple turret outline and three gate notch patterns | **Deferred, only with separate implementation approval** | Prefer primitive geometry/low-cost UI strokes; no new expensive model pipeline. |
| High-poly characters, textures, cinematic VFX, custom shader library | **Not required; deferred outside VS-01** | No Forge/Mech/Titan or VS-02 work. |

No external fonts, stock models, plug-ins or purchases are proposed. If an asset is later authorized, use explicit source/license metadata and keep the project public-repository safe.

## 5. Visual risk register

| ID | Severity | Evidence status | Risk | Proposed mitigation / owner |
| --- | --- | --- | --- | --- |
| ART-R01 | MEDIUM | **Source-confirmed** shared material | Cannon and MiniBot both green; weak hierarchy | Propose turret-specific outline/body; Art → Owner → Developer. |
| ART-R02 | MEDIUM | **Source-confirmed** shared material | Player base and ×2 share cyan “bonus” meaning | Make base neutral with distinct edge treatment; Art → Owner. |
| ART-R03 | MEDIUM | **Source-confirmed** center narrower than adjacent gates | ×1 can be mistaken for empty/bonus strip on phone | Persistent ×1 badge + neutral non-color cue; UI-002. |
| ART-R04 | MEDIUM | **QA2-02 static finding** | HUD and terminal body text become small under current scale | UI-002 sizing and device evidence; do not declare fixed via this spec. |
| ART-R05 | MEDIUM | **Untested** | Dense bot/enemy overlap hides faction silhouette | Capture real 120-visual scenes; adjust only presentation if approved; QA/PERF. |
| ART-R06 | MEDIUM | **Untested** | Rendered contrast differs from hex calculations | Compare actual captures with acceptance targets; UI/QA. |
| ART-R07 | LOW | **Source-confirmed** state mapping; appearance untested | Fortress stages can be ambiguous without distinct contours | Keep stage silhouette transitions clear; Art/QA. |
| ART-R08 | MEDIUM | **Dependency open** | QA2-03 URP assets not persisted, potentially altering shader/color output | Architect/Developer must persist config; QA retest on clean import. |
| ART-R09 | HIGH for release, **outside art scope** | **QA2-01 static confirmed** | Collision/gate chronology blocks VS-01 regardless of art | ARCH-003 → DEV-003 → QA-003; no art changes to simulation. |

## 6. Acceptance and reproducible review matrix

**Documentation gate (this PR):**
- [ ] All gameplay semantics match `VS01_Arena.asset` and source; ×1 is explicitly neutral.
- [ ] Palette includes hex tokens, contrast method, fallbacks for color-vision deficiency and no color-only gameplay cues.
- [ ] Art/UI responsibilities are separated; #6 linked and coordinated.
- [ ] Every risk has evidence status and an assigned next owner.
- [ ] No `Assets/`, `Packages/`, `ProjectSettings/`, shaders, C#, gameplay, or performance-critical files modified.
- [ ] Owner review requested; no merge without explicit approval.

**Future visual validation gate (NOT RUN):**
1. Capture actual Unity Game View and physical device screens at **320×568, 360×640, 390×844** plus one notched/tall portrait aspect. Record OS, device, build, render pipeline, orientation and screenshots.
2. On each: normal HUD, ×2/×1/×3 visibly distinct with grayscale and color-vision deficiency simulation, drag hint intact, positive and hostile shapes distinct.
3. Check projected labels do not overlap HUD or each other; verify fully visible controls in all four safe-area insets. Accept exact bounds only after UI-002 sign-off.
4. Test terminal Victory, base defeat, time-up and paused/suspended; screenshots show all essential text and action controls.
5. Capture ordinary and dense scenes (up to **120 rendered** bots) with enemies; validate distinguishability and check clipping and z-fighting. 120 is the *existing visual cap*, **not** a new recommended count.
6. Evaluate screenshots in grayscale and at minimum intended screen brightness. Measure contrast on the **rendered pixels** where meaningful; report PASS/FAIL/NOT TESTED, not projected success.
7. PERF-002 profiles any subsequently approved art implementation on Android/iOS before it can claim no regression. No FPS/memory targets or results are asserted here.

**Release independence:** The ART-002 spec cannot close QA2-01, QA2-02, QA2-03 or VS-01; Owner release sign-off requires ARCH-003 → DEV-003 → QA-003 and actual mobile/Unity evidence.

## 7. Handoff / decisions requested

- **Owner:** approve or revise the proposed palette/silhouette strategy; authorize any later art implementation separately. Review this documentation PR only.
- **UI/UX #6:** use palette and contrast targets as input, return exact HUD bounds/font sizes/safe-area mockups; confirm contrast from rendered imagery.
- **Architect #2 + Developer #4:** do not change 30 Hz simulation, gate thresholds, event/replay semantics or other code based on this document. Future art modifications must be presentation-only.
- **Performance #8:** advise draw-call/material/overdraw budgets before asset implementation; confirm device profiling plan.
- **QA #9:** treat all unperformed visual/device checks as NOT TESTED and retain QA2-01 blocking state.

**Final ART-002 document state:** ready for review as a proposal. **VS-01 is not release-ready by this document.**

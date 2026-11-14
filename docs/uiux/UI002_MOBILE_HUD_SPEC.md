# SWARMFORGE — UI-002 Mobile HUD Specification

**Version:** UI-002 v0.1 — FOR OWNER / ART REVIEW (not approved for runtime implementation)  
**Role:** 06 Senior UI/UX Game Designer  
**Scope:** GAME-001 / VS-01 only; QA2-02 mobile HUD readability  
**Baseline:** `main` at merge commit [`99cfcdc0b4982e9e441cf23a8669a487a7d75ea9`](https://github.com/Gogenco/swarmforge-studio/commit/99cfcdc0b4982e9e441cf23a8669a487a7d75ea9)  
**Tracking:** [Owner #1](https://github.com/Gogenco/swarmforge-studio/issues/1) · [UI #6](https://github.com/Gogenco/swarmforge-studio/issues/6) · [Art #5](https://github.com/Gogenco/swarmforge-studio/issues/5) · [Developer #4](https://github.com/Gogenco/swarmforge-studio/issues/4) · [QA #9](https://github.com/Gogenco/swarmforge-studio/issues/9)

> **Document status:** Proposed geometry, type, colors and visual references, **not** evidence of a working Unity implementation or physical-device accessibility. ART-002 colors are subject to Art Director approval. This PR must contain documentation/mockups only. Neither ARCH-003/DEV-003 nor VS-02 is changed.

## 1. Existing source and confirmed observations

The DEV-002.2 HUD is implemented in `Assets/Swarmforge/Scripts/Presentation/BattleEntryPoint.cs`, `OnGUI()`; projected gate labels use `PrimitiveBattleView.GateLabelWorld(int)`. The match and outcome data originate from `MatchSnapshot` and are authoritative; the HUD is presentation only.

| Existing code behavior | Consequence / design requirement |
| --- | --- |
| IMGUI scale `max(.35, min(Screen.width/480, Screen.height/850))` | Text and touch targets become very small on narrow portrait screens. For 320×568, scale ≈ 0.667; nominal text 15/17/20 renders at ≈ 10/11.3/13.3 screen pixels. |
| `titleStyle.fontSize = 17`, `lineStyle.fontSize = 15`; terminal `buttonStyle.fontSize = 20` | Separate critical stats from tertiary instructions; increase reference typography and avoid shrinking it to fit. |
| Terminal button height = 58 pre-scale GUI units | At 320×568 it renders ≈ 38.7 pixels high. The physical touch target in points/dp is **not** established by this calculation. |
| `safeTop` / `safeBottom` used but no lateral safe-area bounds | Anchor content to all **four** insets, not `Screen.width` minus fixed padding alone. |
| HUD, gates, control hint all drawn by IMGUI | Keep the fix in presentation; no simulation/event changes. Projection must stay aligned with three actual gate positions. |
| `PollPointer()` accepts input only in the bottom quarter of `Screen.height` | Bottom tutorial is a passive overlay; no new button/gesture/cannon-zone changes in this specification. |
| On suspend, `PAUSED` with `RESUME`; on terminal outcome, `VICTORY!`, `BASE DESTROYED`, `TIME UP` with `RESTART` | Keep exactly these states and transitions. There is no approved new in-game Pause button. |

**Severity:** QA2-02 MEDIUM from QA-002 **STATIC_ONLY** review. A low text size is confirmed by code arithmetic; user-perceived legibility, physical device density, notch behavior and in-game overlap remain **NOT TESTED**.

## 2. Intended UX and information priority

**Portrait-only VS-01**. The player must see the cannon, gate options and approaching enemies without a permanently obstructive UI.

1. **Critical / always visible:** BASE HP, FORT HP, TIME; 3 distinct world-aligned multiplier badges **×2, ×1, ×3**.
2. **Supporting / always visible:** SHOTS, BOTS (produced total), KILLS; align in a separate compact stat row.
3. **Guidance:** `DRAG TO AIM` and `AUTO-FIRE · ATTACK` in the bottom thumb zone; keep the essential command in the first line. No redundant long left/center/right tutorial during active play.
4. **Terminal / pause:** one unobstructed full-screen scrim and single centered state card; all gameplay HUD and gate labels hidden behind it. Only `RESUME` on suspend, only `RESTART` on victory/defeat.

The proposed hierarchy changes presentation only. Stat names, underlying values, multipliers, terminal priority and input behavior are **unchanged**.

## 3. Coordinate and scaling contract

### 3.1 Reference and platform units

- **Reference artboard:** 390×844 **design units**, portrait. These coordinates are for visual handoff; they are **not equivalent to iOS pt or Android dp**.
- **Reference scale for current IMGUI implementation:** `s = min(1.10, Screen.safeArea.width / 390.0f)` for supported safe-area widths ≥ 320 view pixels. This is a **proposed implementation equation**, not yet checked on device.
- All **unscaled reference** dimensions below multiply by `s`. Place and clip UI relative to `Screen.safeArea` in actual Unity screen pixels, converting to IMGUI coordinates if `GUI.matrix` is used.
- Do **not** downscale UI further to compensate for a short screen; reduce secondary content and vertical gaps before ever reducing critical text. For safe-area width < 320 or safe-area height < 500, trigger a compact layout and record a QA exception instead of silently clipping.
- `Screen.safeArea` is a pixel rect with origin at **bottom-left**. For IMGUI drawing (origin at top-left), derive `safeLeft = safe.xMin / s`, `safeRight = (Screen.width - safe.xMax) / s`, `safeTop = (Screen.height - safe.yMax) / s`, `safeBottom = safe.yMin / s` after setting the scale. Draw within `(safeLeft, safeTop, safe.width/s, safe.height/s)`.
- For future uGUI implementation, equivalent normalized safe-area anchors are `(safe.xMin/Screen.width, safe.yMin/Screen.height)` through `(safe.xMax/Screen.width, safe.yMax/Screen.height)`. **Do not introduce uGUI/TMP or new packages without Architect/Owner approval**: current `Packages/manifest.json` contains IMGUI/UI modules but not an explicit `com.unity.ugui`/TMP dependency.

### 3.2 Safe layout geometry at reference 390×844

All measurements in **reference design units**; `S` is the safe rectangle. Content must never escape `S`.

| Element | Anchor / coordinate | Size | Notes |
| --- | --- | --- | --- |
| HUD | `S.top-left + (16, 12)` | `S.width − 32` × **150** | Rounded surface; text rows and 3-column stats grid. |
| HUD brand row | HUD top + 10 | height **22** | SWARMFORGE at left; VS-01 badge at right. Least important row. |
| Critical stats (BASE, FORT, TIME) | HUD top + **33** | height **50** | Three equal columns, gap 6, side inset 9. |
| Supporting stats (SHOTS, BOTS, KILLS) | HUD top + **90** | height **50** | Same 3-column grid and ordering. |
| Gate badges (×2, ×1, ×3) | center above corresponding projected gate | **60×40** each | Clamp within `S` and between top HUD/bottom control regions; never lie about actual lane positions. |
| Control hint | `S.bottom-left + (16, −12 − 76)` | `S.width − 32` × **76** | Passive, no raycast interception; within existing bottom-quarter gesture zone. |
| Terminal scrim | full screen | full viewport | Alpha 0.85+ over scene; gameplay HUD hidden. |
| Terminal card | centered inside `S` | `min(350, S.width − 48)` × **325** | Fixed reading order; becomes scrollable/compact only if safe height cannot fit. |
| Terminal main action | card top + **249** | card width − **56** × **64** | `RESUME` or `RESTART`, one action only; visible pressed state. |

**Horizontal:** all insets are from **safe** edges. Gate badges are individually constrained to avoid left/right cutouts; if the projected labels would overlap, reserve three equal-width legend cells aligned to the actual gate strips and place a short connector cue to the correct strip. Do not swap their semantic order.

**Vertical:** when top HUD/bottom hint are occupied, the remaining `S` between them is the world-view corridor. Reduce HUD brand height and/or hide the second guidance line at short safe heights. Do **not** cover the cannon or gates with permanent UI. A source-world projection collision with HUD is a visual failure requiring a different badge position, not a gameplay geometry modification.

## 4. Typography, targets and fallback

**Text sizes are reference design units at 390-pixel safe width**. Min rendered values below are mathematical approximations at 320-pixel safe width with `s≈0.821`, not physical pt/dp measurements.

| Role / element | Ref size | Approx. @320 | Style / rules |
| --- | ---: | ---: | --- |
| HUD brand | 19 | 15.6 | Semibold, single line |
| Statistic label (`BASE`, etc.) | **18** | **14.8** | Medium/semibold, uppercase |
| Statistic value | **22** | **18.1** | Bold, tabular numerals if font supports |
| Multiplier (×1/×2/×3) | **22** | **18.1** | Bold, centered; color + numeral (never color only) |
| Primary aim hint | **21** | **17.2** | Semibold |
| Secondary aim hint | **18** | **14.8** | Medium; hide when height constrained |
| Terminal heading | **28** | **23.0** | Bold; max 2 lines, no ellipsis |
| Terminal stat label | **18** | **14.8** | Medium |
| Terminal stat value | **22** | **18.1** | Bold |
| Terminal button label | **22** | **18.1** | Bold, at least 64 reference-height action |

**Touch target requirement:** A visual button of 64 reference units will be ≈52.5 view pixels tall at width 320; the actual **iOS 44 pt** / **Android 48 dp** size must be independently measured in physical platform units after build. Avoid confusing rendered resolution pixels, points and dp; if the platform hit region is below its target, expand the invisible hit region without overlapping other controls. Gate multiplier badges are **informational, not touch targets**.

**Long values / localization contingency (without adding localization):**

- BASE, FORT and TIME are never truncated; TIME uses `mm:ss` only if required to keep a readable width (review behavior before changing existing `30s` string format). Negative values must not display.
- For supporting counters, shorten only the **display** (e.g., `1.2K`) if text overflows a column; keep exact simulation values intact. Format deterministically and test 0, 9, 99, 999, 1000, 99999.
- Reserve two lines for `BASE DESTROYED`; reduce heading **only** if it still cannot fit, never under 24 reference units. `TIME UP`, `VICTORY!` and `PAUSED` remain one line.
- Avoid clipped text: allow line breaks in terminal stats, decrease optional padding, and as last resort hide the nonessential HUD brand or secondary tutorial. Never overlap critical text.
- UI strings are English for VS-01; potential translated strings must be manually checked before localization is approved. No runtime translation dependency.

## 5. Provisional design tokens — pending Art Director #5

These tokens are designed for a readable **greybox** UI. The Art Director owns final color language and world visuals; changing these values requires UI-002 review if legibility is affected.

| Token | Hex | Purpose | Static text contrast against fill |
| --- | --- | --- | --- |
| `ui.surface` | `#101E34` | HUD/card opaque fill | — |
| `ui.text.primary` | `#F9FBFF` | Critical text on `ui.surface` | ≈16.1:1 |
| `ui.text.secondary` | `#D5E3F5` | Supporting text on `ui.surface` | ≈12.8:1 |
| `ui.gate.double` | `#076079` | ×2 badge teal | white ≈7.1:1 |
| `ui.gate.neutral` | `#3D465A` | ×1 badge slate | white ≈9.45:1 |
| `ui.gate.triple` | `#865400` | ×3 badge deep amber | white ≈6.4:1 |
| `ui.action` | `#F7BE35` | Primary action | `#101E34` ≈9.8:1 |
| `ui.scrim` | `#050B17` at 88% alpha | Terminal isolation | Test composited contrast on worst scene |

Contrast values were calculated from opaque RGB color pairs (sRGB relative luminance); rendered scene compositing, custom fonts and physical readability still need Unity/device checks. Target at least **4.5:1 text contrast** and **3:1 distinguishability** for UI boundaries where applicable. Shape/numeral labels remain mandatory even when colors are used.

## 6. Static visual acceptance mockups

Reproducible SVGs are generated by [`mockups/render-ui002.mjs`](mockups/render-ui002.mjs). They are **representative greybox diagrams**, not runtime screenshots; world geometry and actors are illustrative. Native SVG dimensions match the declared Game View viewport, with notches simulated via specified insets.

### Gameplay

| File | Viewport | Simulated safe insets L/T/R/B | Purpose |
| --- | --- | --- | --- |
| [`UI002_GAME_320x568.svg`](mockups/UI002_GAME_320x568.svg) | 320×568 | 0 / 0 / 0 / 0 | Narrowest baseline; no cropped text |
| [`UI002_GAME_360x640.svg`](mockups/UI002_GAME_360x640.svg) | 360×640 | 0 / 0 / 0 / 0 | Android portrait reference |
| [`UI002_GAME_390x844_NOTCH.svg`](mockups/UI002_GAME_390x844_NOTCH.svg) | 390×844 | 0 / 54 / 0 / 26 | iOS-like notch simulation; **not a named device** |
| [`UI002_GAME_412x915_NOTCH.svg`](mockups/UI002_GAME_412x915_NOTCH.svg) | 412×915 | 0 / 56 / 0 / 34 | Tall portrait / notch simulation |

### Terminal and pause (390×844, safe 0/54/0/26)

- [`UI002_TERMINAL_VICTORY.svg`](mockups/UI002_TERMINAL_VICTORY.svg)
- [`UI002_TERMINAL_BASE_DESTROYED.svg`](mockups/UI002_TERMINAL_BASE_DESTROYED.svg)
- [`UI002_TERMINAL_TIME_UP.svg`](mockups/UI002_TERMINAL_TIME_UP.svg)
- [`UI002_TERMINAL_PAUSED.svg`](mockups/UI002_TERMINAL_PAUSED.svg)

The values shown are **placeholder sample snapshot data**, not changes to actual match outcomes or economy. All typography in SVG is illustrative and must be compared to the final Unity font rendering.

## 7. Implementer handoff: exact responsibilities and nonchanges

**Only after Owner design sign-off:** Developer #4 integrates the approved UI spec into the existing `BattleEntryPoint.cs` presentation layer (or proposes a narrowly scoped UI component approved by the Architect). IMGUI is the minimal no-new-package option. Use this order:

1. Introduce one safe-area-aware layout calculation used by HUD, gate labels, hint and terminal card; update on `Screen.width/height` and `Screen.safeArea` change, including resume/orientation/device cutout updates.
2. Replace old scale/layout constants with reference unit sizes and measured target bounding boxes. Keep actual `MatchSnapshot` values and `MatchOutcome` transitions; no simulation mutations, no new actions.
3. Keep gate badge positions tied to `GateLabelWorld(int)` via `Camera.WorldToScreenPoint` and only alter **presentation coordinates**; do not remap thresholds or lane identity.
4. Enforce exclusive `Playing` vs `Suspended/Outcome` UI: one overlay, no touch-through, no duplicate restart/resume.
5. Ensure UI hit tests do not accidentally consume the bottom-quarter drag during gameplay and do not pass terminal button taps into aiming.
6. Record UI tests and screenshots per device/resolution. All implementation commits must live in a separate Developer-owned branch/PR or an expressly Owner-approved UI branch; **this documentation PR must not modify C#**.

**No changes requested to:** `MatchSimulation.cs`, `MatchSnapshot.cs`, game progression, gate multipliers, input command sequencing, 30 Hz, replay hash, URP assets, level coordinates, Android/iOS SDK configuration, scenes/prefabs or VS-02 features.

## 8. QA acceptance criteria and device matrix

| ID | Check | Required evidence | Result now |
| --- | --- | --- | --- |
| UI2-A01 | 320×568, 360×640, 390×844 with supplied safe-area examples: full HUD/labels fit; no overlaps | Unity Game View unscaled screenshots with resolution + safe-area debug overlay | **NOT TESTED** |
| UI2-A02 | Additional 412×915 tall portrait and actual notch/cutout phones; four-sided padding correct | Device screenshots identifying model, OS, resolution and safe-area coordinates | **NOT TESTED** |
| UI2-A03 | Text BASE/FORT/TIME and result stats legible at normal handheld distance | On-device UX review with device pixel-density/scale, pass/fail notes | **NOT TESTED** |
| UI2-A04 | ×2/×1/×3 at correct physical strips; badges never clip/intersect each other or HUD | Three screenshots + manual lane/visual check | **NOT TESTED** |
| UI2-A05 | Aiming works across bottom quarter; pointer doesn't get stuck on hint | Touch drag video on Android and iOS; optional input logs | **NOT TESTED** |
| UI2-A06 | PAUSED→RESUME, VICTORY/BASE DESTROYED/TIME UP→RESTART; overlay hides HUD, touches don't leak | Four state screenshots and manual test notes | **NOT TESTED** |
| UI2-A07 | Button hit area meets ~44 pt iOS / ~48 dp Android or is enlarged; feedback visible | Pixel/point/dp measurement on built app, taps at corner/edge | **NOT TESTED** |
| UI2-A08 | Strings 0, 999, 1000, 99999; long terminal title; no clipping/ellipsis of critical text | Debug/synthetic presentation snapshots and screenshots | **NOT TESTED** |
| UI2-A09 | UI tests do not break DEV-002.2 simulation or ARCH-003/DEV-003 determinism | Unity EditMode/PlayMode + DEV-003 regressions + QA-003 audit | **NOT TESTED** |

**Minimum physical coverage requested:** one compact Android phone, one modern tall Android with display cutout, one small iPhone, one notched iPhone; record exact models/builds or explicitly report unavailable. Screenshot-only Game View evidence is **not** an acceptable substitute for on-device text/touch verification.

## 9. Decisions and dependencies

- **Art Director #5:** approve or replace token palette, style of badges, text foreground and silhouette separation. A color change must preserve measurable contrast and ×1/×2/×3 semantic distinction.
- **Architect #2 / Developer #4:** determine whether a future implementation stays IMGUI or introduces uGUI/TMP (requires package/architecture review); fix UI only after Owner approval and without conflating QA2-01 / QA2-03.
- **QA #9:** verify on actual devices after implementation, not against SVG alone.
- **Owner #1:** approve the specification and visuals, then authorize Developer implementation and eventually sign off VS-01 after all release gates. **No self-merge**.

### External primary references

- [Unity `Screen.safeArea` scripting API](https://docs.unity3d.com/ScriptReference/Screen-safeArea.html): safe rectangle is in screen pixels with bottom-left origin; Player window safe-area handling can vary by setting.
- [Apple Human Interface Guidelines — Buttons](https://developer.apple.com/design/human-interface-guidelines/buttons): general hit region **44×44 pt**; points are not Unity pixels.
- [Android Developers — Make apps more accessible](https://developer.android.com/guide/topics/ui/accessibility/apps): recommended touch target **48×48 dp**; dp is not Unity pixels.

**Completion gate:** Docs + SVGs can be `READY_FOR_REVIEW` after static validation; QA2-02 itself cannot be `VERIFIED` or `COMPLETED` until an approved Unity implementation and actual device checks pass.

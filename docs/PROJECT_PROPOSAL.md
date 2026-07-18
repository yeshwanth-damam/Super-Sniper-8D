# SUPER SNIPER 8D
## Project Proposal — Premium Mobile Sniper Game with High-Fidelity ("4K-class") Graphics

Version 1.0 · July 2026
Companion documents: `SuperSniper8D_Design_Monetization.md` (design & economy), top-level `README.md` (build/run guide)

---

## 1. Executive Summary

**Super Sniper 8D** is a premium-feel, mission-based mobile sniper game for Android (iOS later), competing in the genre defined by Sniper 3D (Wildlife Studios, 500M+ downloads) — but positioned against its weaknesses: ad spam, energy timers, and casual cartoon presentation. Super Sniper 8D offers the same proven 60-second contract loop wrapped in cinematic, console-style presentation ("the Sniper 3D loop, rebuilt with Hitman's atmosphere") with player-respecting monetization.

**The project is not starting from zero.** A complete playable build already exists in this repository:

- Full core loop: bolt-action rifle, 8× scope, breath-hold, recoil, tracer, reload
- The signature **bullet cam** (slow-mo chase camera on final kills / long headshots)
- 12 authored contracts across 2 regions, home/region map, mission-select case files
- Persistent credits economy, 4-track weapon upgrade tree, JSON save
- Procedural 3D positional audio (the "8D" hook), filmic post grade, pause/fail/results flow
- Mobile touch controls; runs on URP or Built-in pipeline with zero external assets

This proposal covers taking that verified foundation to a **market-ready Google Play release**: production art at high fidelity, expanded content, monetization implementation, backend services, soft launch, and live operations.

**One-line pitch:** *Quick sniper contracts that look, sound, and feel like a console game — no energy bars, no forced ads.*

---

## 2. Current Status (Foundation Already Built)

| System | Status | Where |
|---|---|---|
| Shooting / scope / breath mechanics | ✅ Working | `Player/WeaponController.cs`, `Player/MouseLook.cs` |
| Bullet cam (signature feature) | ✅ Working | `Player/BulletCam.cs` |
| Campaign: 2 regions × 6 contracts | ✅ Working | `Core/Campaign.cs` |
| Meta screens: home, mission select, safehouse | ✅ Working | `UI/UIManager.cs` |
| Economy: credits, upgrades, JSON save | ✅ Working | `Core/SaveSystem.cs`, `Core/Upgrades.cs` |
| Procedural audio (all synthesised) | ✅ Working | `Core/ProceduralAudio.cs` |
| Cinematic grade (vignette/grain/tint, HDR) | ✅ Working | `UI/UIManager.cs`, `Bootstrap/GameBootstrap.cs` |
| Production 3D art | ❌ Placeholder primitives | — |
| Animated human characters | ❌ Placeholder primitives | — |
| Monetization (ads/IAP) | ❌ Not started (designed) | Design doc Part 4 |
| Backend / accounts / cloud save | ❌ Not started | — |
| PvP multiplayer | ❌ Deliberately out of scope v1 | Design doc P4 |

This foundation de-risks the two most common failure modes of game projects: "the core loop was never fun" and "the architecture couldn't support the meta game." Both already exist and hold together end-to-end.

---

## 3. Vision & Market Positioning

### 3.1 Target market
- Players of Sniper 3D, Hitman Sniper, Sniper Strike — a proven, massive genre audience
- Specifically: the segment exhausted by interstitial ads, energy systems, and pay-walls (consistently the top complaints in genre reviews)
- Headphone-wearing players who respond to audio-led immersion (the "8D audio" hook is unique in the genre)

### 3.2 Differentiators
1. **Cinematic presentation** — golden/blue-hour lighting, letterboxed bullet cams, dossier mission briefs, restrained "quiet luxury" UI
2. **8D binaural audio** — positional soundscape, scope muffle, rising heartbeat; marketed as *"wear headphones, hear the city breathe"*
3. **Player-respecting monetization** — no interstitials, no energy system, unlimited play; season pass + cosmetics + opt-in rewarded "Intel Drops"
4. **The bullet cam as a content engine** — every kill cam is a shareable clip and a showcase for cosmetic skins/trail effects

### 3.3 What we deliberately do NOT build in v1
- PvP multiplayer, clans, squad wars (the design doc marks this a solo-team trap; revisit post-revenue)
- 180+ weapons (we ship ~8–12 excellent rifles instead of a wall of stat sticks)
- Hundreds of missions (we ship 40–60 hand-authored contracts that are actually good)

---

## 4. "4K Graphics" on Mobile — Honest Technical Strategy

"4K" in mobile marketing means **high-fidelity rendering**, not literal 3840×2160 output. Almost no phone has a 4K panel (flagships are 1080p–1440p), and rendering at native 4K would destroy battery and thermals for zero visible benefit. The correct engineering interpretation — and what we commit to — is:

### 4.1 Rendering pipeline (Unity 6, URP)
- **URP with HDR** rendering and hardware MSAA where budget allows
- **Full post-processing volume stack**: filmic tonemapping (ACES), bloom, color-grade LUTs per region, vignette, depth of field in scope view and kill cams, motion blur in bullet cams
- **Dynamic resolution scaling**: render scale adapts per-device (flagships ~1.0 at native, mid-range 0.7–0.85 upscaled) to hold frame rate
- **Target frame rates**: 60 fps on flagship/upper-mid devices, 30 fps floor on min-spec
- **4K-resolution source textures** (2048–4096 texture sets) on hero assets — the rifle, hands, primary targets — so close-ups and kill cams genuinely read as "4K-class"

### 4.2 Asset quality tiers
| Tier | Assets | Budget |
|---|---|---|
| Hero (always on screen / kill cam) | Rifle, hands, scope, bullet | 20–60k tris, 4K PBR texture sets |
| Primary (aim targets) | Enemy characters | 15–30k tris, 2K textures, full rigs |
| Environment | Buildings, props, vehicles | Aggressive LODs, texture atlases, GPU instancing |
| Distant | Skyline, background city | Baked imposters / low-poly silhouettes in fog |

### 4.3 Lighting & atmosphere
- Baked global illumination + light probes for environments; one real-time directional (sun) with soft shadows
- Volumetric-style fog (URP height fog + gradient sky) for the golden-hour/blue-hour signature look
- Reflection probes for wet-street/neon scenes (Region 2: Neon District)

### 4.4 Device support matrix
| Class | Example | Experience |
|---|---|---|
| Flagship (Snapdragon 8-series, A16+) | Galaxy S23+, iPhone 15 | Native res, 60 fps, all post FX |
| Mid (Snapdragon 7-series, Dimensity 8000) | Redmi Note 13 Pro | 0.8 render scale, 60 fps, most FX |
| Min-spec (4GB RAM, Adreno 610-class) | Redmi 9 class | 0.7 scale, 30 fps, reduced FX, no DoF |

Quality tiers auto-detect with a manual override in settings. Min Android API 26 (Android 8.0), 64-bit only, Vulkan with GLES3 fallback.

---

## 5. Technical Architecture

### 5.1 Client (exists, evolves)
```
Unity 6 LTS (C#) — URP
├── Bootstrap (scene assembly; migrates to authored scenes + addressables)
├── Player (WeaponController, MouseLook, BulletCam)
├── Targets (AI: patrol, idle, alert states; Mixamo/custom rigs replace primitives)
├── Core (GameManager state machine, Campaign data, Upgrades, SaveSystem)
├── UI (UIManager; migrates to UI Toolkit or prefab-based uGUI screens)
└── Services (NEW: Ads, IAP, Analytics, RemoteConfig, CloudSave adapters)
```
Existing code carries forward; the primary refactors for production are (a) authored scenes with baked lighting replacing fully-procedural scene generation, (b) Addressables for asset streaming, (c) a thin service layer isolating third-party SDKs.

### 5.2 Backend (new, phased)
v1 ships **client-authoritative with cloud save** — no custom game server needed until PvP.

| Need | v1 Solution | Later |
|---|---|---|
| Accounts / auth | Google Play Games Services + Unity Authentication | Own auth service |
| Cloud save | Unity Cloud Save (or Play Games saved games) | Own service (Spring Boot) |
| Remote config / economy tuning | Unity Remote Config / Firebase RC | Own config service |
| Analytics | Firebase Analytics + Unity Analytics | Data warehouse |
| Leaderboards (async, not PvP) | Play Games leaderboards | Own service |
| Receipt validation | Server-side validation via Play Developer API (small Spring Boot service on AWS) | Expanded backend |

Given the owner's Java/Spring Boot/AWS background, the one custom backend piece in v1 — **IAP receipt validation + player-economy audit log** — is a deliberately small Spring Boot service (Elastic Beanstalk / ECS Fargate, PostgreSQL RDS). Everything else uses managed services to keep v1 ops near-zero.

### 5.3 Data & save
- Local JSON save (exists) becomes the offline cache; cloud save syncs on login/mission-complete with last-write-wins + version numbers
- Economy balance lives in remote config so tuning requires no app update

### 5.4 Tooling & pipeline
| Area | Tool |
|---|---|
| Engine / language | Unity 6 LTS, C# |
| IDE | Visual Studio / Rider |
| 3D modeling | Blender |
| Characters/animation | Mixamo (early), custom rigs (later) |
| Textures | Substance Painter (or Quixel Mixer, free) |
| Audio | Reaper/Audacity + recorded foley; Steam Audio plugin for HRTF binaural |
| Version control | GitHub (this repo) + Git LFS for binaries |
| CI | GitHub Actions + GameCI (automated Android builds, PR checks) |
| Crash reporting | Firebase Crashlytics |

---

## 6. Scope of Work — Feature Breakdown

### 6.1 Content (v1 release target)
- **4 regions** (Old Harbour, Neon District + 2 new: Mountain Pass, Marina at Night), 10–15 contracts each → **40–60 missions**
- Mission variety: eliminate, timed, VIP-protect (don't hit the escort), moving convoy, multi-target chains, no-collateral (civilians present)
- **8–12 rifles** with distinct handling (zoom, sway, bolt time, damage class) replacing the single-rifle-with-upgrades model; per-rifle upgrade tracks (system exists)
- Boss contracts capping each region (unique target, scripted moment)

### 6.2 Presentation upgrade
- Production environment art per region (modular building kits)
- Rigged, animated human targets (idle/walk/alert/ragdoll death replacing physics-tip)
- Hero rifle models with animated bolt/hands in first person
- Full post-processing volume stack per Section 4
- Recorded/licensed audio replacing procedural synthesis where it matters (gunshots, foley); procedural systems retained for wind/heartbeat; Steam Audio HRTF integration
- Music: sparse tension pads + region themes (licensed or commissioned)

### 6.3 Monetization (per design doc Part 4 — already fully specified)
- **Season pass** ($4.99/~6 weeks): free + premium tracks, cosmetics-weighted
- **Cosmetics**: weapon skins, reticle styles, bullet-cam trail effects, kill-card themes
- **Intel Drops**: diegetic opt-in rewarded video (+credits); 2× mission rewards option on results screen
- **Operator Pack** ($6.99 one-time): ad-free flag + starter cosmetics
- Hard commitments: **no interstitials, no energy system, no popup offers**
- SDKs: Google Play Billing (via Unity IAP), Unity LevelPlay or AdMob (rewarded only)

### 6.4 Retention & live ops
- Daily challenge contract (seeded variation of existing missions)
- Weekly featured mission with score leaderboard (Play Games)
- Achievements, push notifications (respectful defaults), seasonal pass rotation

### 6.5 Quality / release engineering
- Device-tier performance profiles + in-game settings (graphics tier, fps cap, sensitivity, audio)
- Localization-ready string tables (English at launch; hi-IN, pt-BR, es, ru as fast follows — genre's biggest markets)
- Google Play: closed testing → open testing → production, staged rollout, pre-launch report, Data Safety form, IARC rating

---

## 7. Phased Delivery Plan

Each phase has an exit test; the next phase does not begin until it passes (rule inherited from the design doc).

**Phase A — Production foundation.**
Authored scenes + baked lighting, Addressables, service-layer refactor, CI pipeline with automated Android builds, device-tier system. *Exit: current game runs at 60 fps on a mid-tier phone with the full post stack.*

**Phase B — Art fidelity.**
Region 1 rebuilt with production art; rigged enemies with animation states; hero rifle + first-person hands; recorded weapon audio + Steam Audio. *Exit: a 30-second clip that a stranger mistakes for a console game.*

**Phase C — Content expansion.**
Regions 2–4 to production quality; 40–60 contracts; 8–12 rifles; boss contracts; mission-type variety. *Exit: a new player plays 45+ minutes unprompted; day-1 retention in friends-and-family test > 35%.*

**Phase D — Monetization + backend.**
Unity IAP + Play Billing, receipt-validation service (Spring Boot/AWS), LevelPlay rewarded integration, season pass v1 (~30 items), cloud save, remote config, analytics events. *Exit: a stranger completes a purchase and an Intel Drop without confusion; economy dashboards live.*

**Phase E — Soft launch.**
Closed → open testing on Play (1–2 mid-size markets, e.g. Philippines/Brazil convention), Crashlytics triage, economy tuning via remote config, store listing A/B tests. *Exit: crash-free sessions ≥ 99.5%, D1 ≥ 35%, D7 ≥ 12%, rewarded-ad engagement ≥ 25% of DAU.*

**Phase F — Global launch + live ops.**
Staged production rollout, launch trailer built from bullet-cam footage, seasonal cadence begins. *Ongoing: monthly content drops, seasonal passes.*

**Indicative durations** (industry-typical, dependent on staffing below): Phase A ~1 month; B ~2–3 months; C ~3–4 months; D ~1.5–2 months; E ~1–2 months. Solo development stretches these roughly 2–3×; the existing codebase is what makes even the solo path viable.

---

## 8. Team & Budget Options

### Option 1 — Solo developer (owner) + contractors
| Role | Arrangement |
|---|---|
| Programming (client + backend) | Owner (existing code, Java/Spring/AWS background) |
| 3D environment art | Contract / asset-store hybrid |
| Character art + animation | Mixamo + one contractor pass |
| Audio | Licensed packs + one freelance sound designer pass |
| **Indicative cost** | **₹3–8 lakh** (contract art/audio, devices, tools, store fees) |

### Option 2 — Small indie team (recommended for the 4K-class art bar)
| Role | Count |
|---|---|
| Unity developer | 1 (+ owner) |
| 3D artist (environment + props) | 1–2 |
| Character artist/animator | 1 |
| UI/UX designer | 0.5 (contract) |
| Sound designer | 0.5 (contract) |
| QA | 0.5 (+ community testing) |
| **Indicative cost** | **₹25–60 lakh** to global launch |

Fixed costs either way: Google Play ($25 one-time), Apple ($99/yr when iOS ships), AWS for the receipt service (≈$30–80/mo at launch scale), Unity (free below revenue threshold), test devices (3–4 across tiers, ₹60–100k).

Note: the art bar is the budget driver. The code is largely paid for (it exists); "4K-class" visuals are where money goes.

---

## 9. KPIs & Success Criteria

| Metric | Soft-launch gate | 6-month goal |
|---|---|---|
| Crash-free sessions | ≥ 99.5% | ≥ 99.7% |
| D1 / D7 / D30 retention | 35% / 12% / 4% | 40% / 15% / 6% |
| Avg session length | ≥ 12 min | ≥ 15 min |
| Rewarded-ad engagement | ≥ 25% DAU | ≥ 35% DAU |
| Payer conversion | ≥ 1.5% | ≥ 2.5% |
| ARPDAU | ≥ $0.03 | ≥ $0.06 |
| Play Store rating | ≥ 4.4 | ≥ 4.6 (the premium-feel bet made visible) |

---

## 10. Risk Register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Art quality misses the "console" bar | Medium | High | Phase B exit test is a hard gate; contract senior art help before expanding content |
| Thermal/perf on mid devices | Medium | High | Dynamic resolution + tier system from Phase A; profile on real min-spec early |
| Solo bandwidth vs 4 other active projects | High | High | Explicit decision (per design doc): commit staffing Option 1 vs 2 before Phase B |
| Economy tuning wrong at launch | Medium | Medium | Remote config from day one; soft-launch gate on economy metrics |
| UA cost (can't outspend Wildlife) | High | Medium | Strategy is organic: ratings, shareable bullet-cam clips, premium word-of-mouth |
| Store policy churn (Data Safety, ads SDKs) | Low | Medium | Rewarded-only ads simplify compliance; annual policy review |
| Scope creep toward PvP | Medium | High | Contractually out of v1; revisit only post-revenue |

---

## 11. Why This Proposal Is Credible

Most proposals in this genre begin with 6 months of engine work before anything is playable. This one begins with a **working game in the repository**: the loop is proven in code, the meta systems (economy, save, campaign, screens) hold together end-to-end, an external code review has already been absorbed, and the design/monetization thinking is documented. The remaining work is *production* — art, content, integration, and launch discipline — which is more predictable and more parallelizable than invention.

**Recommended immediate next steps:**
1. Decide staffing (Option 1 vs 2) — this sets the realistic art bar
2. Phase A: authored-scene refactor + CI + device-tier system
3. Commission/acquire the Region 1 art kit and one hero rifle to validate the Phase B bar early
4. Implement the Intel Drop + IAP service layer behind feature flags (code-ready before art lands)

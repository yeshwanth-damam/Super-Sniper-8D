# SUPER SNIPER 8D
## Game Design & Monetization Document — Premium Cinematic Edition

Version 1.0 · July 2026

**Positioning in one line:** *The Sniper 3D loop, rebuilt with Hitman's atmosphere.*

The target player loves 60-second sniper missions but is exhausted by ad spam, cartoon menus, and energy timers. The promise: quick missions that look, sound, and feel like a console game. You cannot out-spend Wildlife Studios on user acquisition — but a solo developer absolutely can out-craft them, and premium feel is the one battlefield where they can't follow without breaking their own revenue model.

---

## Part 1 — Gap Analysis: Current Build vs Sniper 3D

The current build is a verified engine: shooting, scoping, scoring, levels, win/lose, mobile input, spatial audio. What Sniper 3D has on top of that engine is seven years of content, art, economy, and live operations built by a full studio. The honest picture:

| Area | Super Sniper 8D today | Sniper 3D | Priority |
|---|---|---|---|
| Kill feedback | Tracer + physics knockback | Slow-mo bullet cam, ragdolls | **P1 — signature** |
| Enemies | Primitive capsule dummies | Animated humans, guards, civilians, vehicles | **P1** |
| Presentation | Plain HUD, no briefing | Mission briefs, polished menus, VFX | **P1** |
| Content | 3 procedural levels | 850+ authored missions, 20+ regions | P2 |
| Weapons | 1 rifle, fixed stats | 150+ guns with 5-stat upgrade trees | P2 |
| Economy | None | Dual currency (coins + diamonds) | P2 |
| Meta screens | None | Home, region map, garage, shop, events | P2 |
| Save system | None | Cloud save + local | P2 |
| Retention systems | None | Dailies, achievements, push notifications | P3 |
| Monetization | None | Ads + IAP + VIP subscription | P3 |
| Multiplayer | None | PvP arena, squad wars | P4 — optional, skip |

The sequencing rule: close P1 gaps first, because they determine whether anyone *wants* to play. P2 gives the game a body. P3 makes money. P4 is a trap for a solo developer and should be ignored until the game has revenue.

---

## Part 2 — How Sniper 3D Actually Earns Money

Sniper 3D's design is a funnel, and every system exists to feed it. Missions run 30–60 seconds, so the reward loop fires dozens of times per session. Mission rewards pay out in coins, but weapon upgrade costs are tuned to outpace coin income somewhere around the second or third region. That manufactured shortfall — the *currency gap* — is the entire business. Once a player hits a mission that says "requires upgraded rifle," they have three options: grind old missions, watch ads, or pay. Every option is a win for the developer.

**The ad layer.** Interstitial ads fire after roughly every second or third mission for free players — high volume, low goodwill. Rewarded video is the real workhorse: double mission rewards, free diamonds, energy refills, and lucky-wheel spins are all gated behind a 30-second opt-in ad. Rewarded placements earn higher eCPMs, players choose them voluntarily, and they don't tank store ratings the way interstitials do.

**The IAP layer.** Currency packs run from about $1.99 to $99.99, alongside starter bundles, weapon bundles, an ad-removal purchase, and a VIP subscription that grants daily diamonds, exclusive weapons, and no ads. The whale curve is standard free-to-play: most revenue from a small fraction of payers.

**The energy system.** Play consumes energy; energy runs out; the session ends on the house's terms. The player either pays, watches an ad, or comes back tomorrow — and all three outcomes are monetization or retention. It is also the single most resented mechanic in the genre.

**The scale caveat.** This model works because Sniper 3D has hundreds of millions of installs and a UA machine feeding it casual players. Ad revenue carries the broad free base; IAP adds depth. A solo developer copying this playbook gets the resentment without the scale. That is why Super Sniper 8D takes the loop and rejects the pressure tactics — covered in Part 4.

*(Note: figures and placement details reflect the game as of early 2026; verify current behavior by playing a few sessions before finalizing your own economy numbers.)*

---

## Part 3 — The Premium Cinematic Vision

### Art direction

Sniper 3D is bright, saturated, and casual. Super Sniper 8D goes the opposite way: near-black shadows, desaturated urban blues and greys, and a single amber-gold accent reserved exclusively for interactive elements, muzzle light, and tracer glow. Lighting is always golden hour or blue hour — never flat noon. Volumetric fog gives depth to every skyline. The URP post-processing stack carries the look: low bloom, gentle vignette, a cinematic color-grade LUT, and film grain at low opacity. During kill cams the screen letterboxes to 21:9 with black bars. Restraint is the aesthetic; nothing glows unless it matters.

### The Bullet Cam — the signature moment

This is the one feature that makes clips shareable and the game memorable, and it deserves more polish than anything else. It triggers on the final target of every mission and on any headshot beyond 200 meters. The sequence: time collapses to 0.15×, the camera detaches and chases the bullet with slight lag and a slow roll, an air-distortion trail ripples behind the round, and the soundscape cuts to almost nothing — a low whoosh and a heartbeat. Impact lands as a single white frame, then sound crashes back in with the body fall. Three to four seconds, skippable after the first viewing. Implementation is a second camera following the raycast path on a spline, with `timeScale` and `fixedDeltaTime` scaled together.

### 8D audio design

Audio is the differentiator the name promises, so it gets designed, not defaulted. When scoped, the outside world runs through a low-pass filter and the player's own heartbeat fades in, climbing from roughly 55 to 90 BPM the longer breath is held — release too late and the sway punishes greed. Bolt-action foley gets real mechanical detail. The wind beds already built stay positional, joined by a distant city rumble behind the player. Steam Audio's HRTF spatializer delivers true binaural rendering in headphones. Music stays sparse: a tension pad under gameplay, a single cello motif when one target remains, and deliberate silence as a tool. The pitch to players: *wear headphones, hear the city breathe.*

### UI design language

One word governs every screen: restraint. Dark translucent panels with one-pixel hairline borders, generous spacing, a single typeface family (a condensed grotesque like Barlow Condensed for numerals, a clean sans for body), and the amber accent appearing only on elements the player can touch. Micro-animations run 150–250ms with ease-out curves. There are no red notification badges, no pulsing SALE buttons, no screen-shaking menus. Sniper 3D's interface shouts; this one whispers, and the quiet is what reads as expensive.

The home screen is a full-bleed cinematic shot of the current region with a single mission card in the lower third and at most three icons. Mission select presents contracts as case files: a target photo, a name, one line of intel, the reward, and a CLASSIFIED stamp, with text revealed typewriter-style. The garage shows the rifle on a dark turntable with stats as thin bars and upgrades that land with a mechanical click. The results screen counts the score up slowly, then shows accuracy, headshot percentage, and best-shot distance with one call to action.

### Mission presentation

Every mission opens with a five-second, skippable dossier: the file slides in, typewriter text sets the stakes — *TARGET: arms courier. WINDOW: 60 seconds. COLLATERAL: zero.* — and then a hard cut to scope-up. This single screen does more for perceived quality than any graphics upgrade, because it turns "Level 12" into a contract.

---

## Part 4 — Monetization That Doesn't Break Premium

The strategy keeps Sniper 3D's proven economy skeleton and removes every mechanic that generates resentment. Kept: rewarded video, an upgrade economy, mission rewards, and bundles. Dropped: forced interstitials (the fastest way to feel cheap), the energy system (unlimited play, full stop), popup offers, and fake discounts. Added: the revenue mechanics of modern premium free-to-play — a season pass and a deep cosmetic line.

| Placement | Sniper 3D | Super Sniper 8D |
|---|---|---|
| Interstitial after missions | Every 2–3 missions | **Never** |
| Rewarded video | 2× rewards, diamonds, energy, spins | 2× rewards + "Intel Drops," always opt-in |
| Energy paywall | Yes | **No — unlimited play** |
| Currency IAP packs | Heavily pushed | Available, never pushed |
| Season pass | Present but secondary | **Core revenue driver** ($4.99/season) |
| Cosmetics | Minor | Major: weapon skins, reticle styles, bullet-cam trail effects, kill-card themes |
| Subscription / VIP | Yes | Later, optional |
| Ad removal | Paid unlock | Included in one-time "Operator Pack" ($6.99) |

Rewarded ads get diegetic framing so they belong in the world: an encrypted crate labeled **Intel Drop** sits in the safehouse, and the copy reads *"Decrypt intel (30s) → +200 credits."* No popups, ever. The results screen offers 2× rewards as a quiet button, not a modal.

The season pass is the engine: a free track and a $4.99 premium track over roughly six weeks, weighted toward cosmetics (skins, reticles, cam effects) with modest currency, never gameplay power. Cosmetics work in this genre because the bullet cam is a built-in showcase — players literally watch their own skin and tracer effect in slow motion after every mission.

**The honest economics.** This model earns meaningfully less per install than Sniper 3D's — no interstitial volume, no energy pressure means lower short-term ARPDAU. What it buys instead is retention, ratings, and word-of-mouth, which are the only acquisition channels a solo developer actually has. The bet is explicit: trade squeezed pennies for a 4.7-star rating and players who stay.

**Tech stack:** Unity LevelPlay (or AdMob) for rewarded video, Google Play Billing for IAP, and remote config for economy tuning once live.

---

## Part 5 — Phased Build Plan

**Phase 1 — Core slice.** Done: shooting, 8× scope, levels, scoring, mobile input, spatial audio foundation.

**Phase 2 — Feel (2–4 weeks).** Bullet cam, humanoid targets using free Mixamo models and animations, scope audio filter plus heartbeat, URP post-processing stack, letterboxing, and the mission-brief dossier screen. Exit test: a 30-second gameplay clip that makes a stranger ask *"what game is that?"* Do not leave this phase until the clip passes.

**Phase 3 — Structure (3–5 weeks).** Twenty hand-authored missions across two regions, a credits currency, one rifle upgrade tree (damage, stability, zoom, clip), JSON save via persistent data path, and the full results screen. Exit test: a friend plays ten missions unprompted.

**Phase 4 — Money (1–2 weeks).** Intel Drop rewarded placements, 2× results option, a manually-authored season pass v1 of about 30 items, and the Operator Pack IAP. Exit test: a stranger completes a purchase flow without confusion.

**Phase 5 — Live (ongoing).** Daily challenge contract, weekly featured mission, push notifications, and seasonal pass rotations.

The governing rule: no phase begins until the previous phase's exit test passes. The graveyard of solo game projects is full of economies built for games nobody wanted to play; the bullet cam comes before the shop.

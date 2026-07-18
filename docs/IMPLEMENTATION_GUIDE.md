# SUPER SNIPER 8D — Step-by-Step Implementation Guide
### From a fresh machine to a launched game, in order, with checkpoints

Version 1.0 · July 2026
Companions: `PROJECT_PROPOSAL.md` (the plan), `SuperSniper8D_Design_Monetization.md` (the design), `README.md` (quick reference)

**How to use this guide:** work strictly in order. Every stage ends with a
**CHECKPOINT** — do not continue until it passes. Skipping checkpoints is how
projects end up with ten broken half-features instead of one working game.

---

## STAGE 0 — Accounts & installs (all free except one)

1. Create/verify a **GitHub** account. This repo is your project home.
2. Start the **Google Play Console** registration ($25 one-time — the project's
   only cost) at https://play.google.com/console — identity verification can
   take days, so start it now even though you won't need it for months.
3. Install **Unity Hub** from unity.com/download.
4. In Unity Hub → *Installs* → *Install Editor* → newest **Unity 6 LTS**.
   Tick these modules:
   - **Android Build Support** (+ OpenJDK + Android SDK/NDK — accept all three)
   - Nothing else needed.
5. Install **Visual Studio Community** (free) or **VS Code** with the C# extension.
6. Install **Git** (git-scm.com) and **Git LFS** (git-lfs.com).
7. Later stages also use (install when reached, all free): **Blender**,
   **GIMP or Krita**, **Audacity**, **Upscayl**, and a **Mixamo** (Adobe) account.

**CHECKPOINT 0:** Unity Hub shows Unity 6 LTS with Android module; `git lfs version` works in a terminal; Play Console registration submitted.

---

## STAGE 1 — Create the Unity project and make it compile

1. Unity Hub → **New project** → template **Universal 3D** (URP) → name
   `SuperSniper8D` → Create. First open takes a few minutes.
2. Install uGUI (the project's UI framework):
   *Window → Package Manager → + (top-left) → Install package by name…* →
   `com.unity.ugui` → Install.
   *(Skipping this causes the CS0234 `UnityEngine.UI` Safe-Mode errors.)*
3. Set input handling: *Edit → Project Settings → Player → Other Settings →
   Active Input Handling* → **Both**. Restart the editor when prompted.
4. Clone this repo next to (not inside) the Unity project, then copy its
   `Assets/Scripts` folder into the Unity project's `Assets/` folder.
5. Wait for the compile spinner (bottom-right). Open the **Console**
   (*Window → General → Console*): there must be **zero red errors**
   (warnings are fine).

**CHECKPOINT 1:** Console shows 0 errors after import.

---

## STAGE 2 — First run of the actual game

1. In the empty scene (*File → New Scene* if unsure):
   **GameObject → Create Empty**, rename it `Bootstrap`.
2. With `Bootstrap` selected: **Add Component → Game Bootstrap**.
3. Press **Play**. You should get: home screen ("SUPER SNIPER 8D", region
   cards) → click **OLD HARBOUR** → contract list → click contract #01 →
   dossier types itself out (click to skip) → you're in the mission.
4. Test everything against this list:
   - [ ] Mouse look works; **right-click** scopes (8× zoom + reticle)
   - [ ] **Left-click** fires; tracer + muzzle flash visible; bolt sound after shot
   - [ ] Hitting the **amber head sphere** scores 150; body 50
   - [ ] **Shift** while scoped steadies aim; heartbeat gets louder *and faster*;
         holding too long makes sway spike; world sounds muffled while scoped
   - [ ] Killing the **last target** triggers the slow-mo **bullet cam** → white
         flash → results screen counts up → credits shown
   - [ ] **CONTINUE** → Safehouse (buy an upgrade if affordable) → **DEPLOY** →
         contract board shows #01 CLEARED, #02 unlocked
   - [ ] **Esc** pauses (Resume/Restart/Quit); letting the timer run out shows
         MISSION FAILED with Retry/Abort
5. Stop Play mode. Save the scene as `Assets/Scenes/Main.unity`.

**CHECKPOINT 2:** every box above ticks. If something fails, fix it now — this
is your known-good baseline for everything that follows.

---

## STAGE 3 — Put the whole Unity project under version control

The repo currently holds scripts + docs. From here on, track the full project.

1. Copy the repo's `.gitignore` into the Unity project root (it already
   ignores `Library/`, `Temp/`, builds, etc.).
2. Move the repo's `docs/` and `README.md` into the Unity project root, then
   make the Unity project folder the repo working tree (or re-clone and copy
   the Unity project in — either way, one folder = one repo).
3. Enable meta files + text serialization (usually already default):
   *Project Settings → Version Control → Mode: Visible Meta Files* and
   *Editor → Asset Serialization: Force Text*.
4. Set up Git LFS for binaries before adding any art:
   `git lfs track "*.png" "*.jpg" "*.tga" "*.psd" "*.fbx" "*.wav" "*.mp3" "*.ogg" "*.blend"`
   then commit `.gitattributes`.
5. Commit and push: scenes, scripts, settings, packages manifest.

**CHECKPOINT 3:** a fresh `git clone` + open in Unity Hub reproduces
Checkpoint 2 on another machine (or after deleting `Library/`).

---

## STAGE 4 — Tune the feel (your first real game-dev work)

All numbers live in the scripts — change, Play, feel, repeat. Keep each change
small and commit when it feels better.

| What | Where | Field(s) |
|---|---|---|
| Sway strength | `WeaponController` | `hipSway`, `scopedSway` |
| Breath window | `WeaponController` | `breathHoldMax` |
| Bolt/reload pace | `WeaponController` | `boltTime`, `reloadTime` |
| Zoom snappiness | `WeaponController` | `aimSpeed` |
| Recoil kick | `WeaponController` | `recoilPitch`, `recoilYaw` |
| Bullet-cam length/feel | `BulletCam` | `slowMoScale`, `travelSeconds`, `longShotThreshold` |
| Mission difficulty | `Campaign.cs` | targets/movers/speed/time per contract |
| Economy pace | `GameManager.CompleteMissionRoutine` (credits formula), `Upgrades.BaseCost` |

**CHECKPOINT 4:** you can clear Region 1 and it *feels* fair — a friend can
clear contracts #01–#03 without instructions and wants to keep playing.

---

## STAGE 5 — First Android build on your own phone

1. On the phone: enable Developer Options (tap Build Number 7×) → enable
   **USB debugging**.
2. Unity: *File → Build Settings (Build Profiles on Unity 6) → Android →
   Switch Platform* (one-time reimport wait).
3. *Player Settings*:
   - Company/Product name; **Default Orientation: Landscape Left**
   - *Other Settings*: Scripting Backend **IL2CPP**, Target Architectures
     **ARM64** only, Minimum API **26**
4. Add your saved scene to *Scenes in Build*.
5. Connect the phone → **Build and Run**. First build is slow (IL2CPP).
6. On device, re-run the Checkpoint 2 list — plus:
   - [ ] Drag-to-look works on the left/centre of the screen
   - [ ] FIRE / SCOPE / HOLD buttons work and **disappear when menus are open**
   - [ ] Frame rate feels smooth (we formalize fps later); no overheating in
         a 10-minute session
   - [ ] Progress survives killing and reopening the app (save file works)

**CHECKPOINT 5:** the game runs on your physical phone and saves progress.
*You now have a complete, working mobile game. Everything after this is making
it bigger and prettier — from a position of strength.*

---

## STAGE 6 — Production foundation (Proposal Phase A)

Goal: turn the tech base from "generated demo" into "production app" while
changing nothing the player feels.

1. **Quality tiers.** Add a small `QualityManager` script: detect device memory
   / GPU via `SystemInfo`, set `ScalableBufferManager`/render scale +
   `Application.targetFrameRate` (60 flagship / 30 low-end), and expose a
   manual override in a simple settings panel. Keep the 4K path alive: when
   `Display.main.systemWidth >= 3840`, allow native resolution.
2. **Authored scene migration.** The bootstrap currently builds the city at
   runtime. In the editor, run Play once, then use *GameObject → Save as
   prefab* on generated environment pieces (or rebuild the rooftop + skyline
   by hand in the scene) so the environment becomes an editable, lightable
   scene asset. Keep `GameBootstrap` for managers/wiring only.
3. **Baked lighting.** With static environment geometry: mark it *Static*,
   *Window → Rendering → Lighting* → generate lightmaps + light probes.
   Golden-hour directional sun, fog as already configured.
4. **CI builds.** Add GameCI GitHub Actions (game.ci docs): every push builds
   an Android APK. Store a Unity license secret per GameCI instructions.
5. **Crash + analytics hooks.** Add Firebase (free): Crashlytics + Analytics
   via the Firebase Unity SDK. Log 4–5 events only for now: mission_start,
   mission_complete, mission_fail, upgrade_bought, session_start.

**CHECKPOINT 6:** CI produces an installable APK on every push; the authored
scene plays identically to Checkpoint 5; Crashlytics shows a test crash.

---

## STAGE 7 — Art fidelity pass (Proposal Phase B) — the big visible jump

Work order matters: character → rifle → environment → post → audio.

1. **Real enemies (Mixamo).**
   - mixamo.com → pick a character (e.g. a soldier/civilian) → download as FBX
     with a few animations: Idle, Walk, Alert, and a death/fall.
   - Import to Unity; set rig to **Humanoid**. Build one `Enemy` prefab with an
     `Animator` (Idle/Walk states driven by a `bool isWalking`).
   - In `TargetSpawner.BuildDummy`, replace the primitive stack with
     instantiating this prefab; keep the head collider + `TargetHead` marker on
     the head bone; on `Target.TakeHit`, disable the Animator and enable
     ragdoll (add ragdoll via *GameObject → 3D Object → Ragdoll…* wizard) —
     this replaces the rigid-tip with a proper collapse.
2. **Hero rifle.** Find one CC0/free rifle (Sketchfab CC0, Kenney, or model a
   simple one in Blender). Replace the primitive rifle in
   `WeaponController.BuildRifle` with the model (same parent/hide-on-scope
   logic). Texture at 4K using Materialize/Upscayl from CC0 sources.
3. **Environment kit.** PolyHaven/ambientCG materials + Kenney or CC0 building
   meshes for Region 1's rooftop + street. Aggressively reuse: 6–8 building
   shells with material variation reads as a full city in fog.
4. **URP post stack.** Now that you're URP-committed: add a global Volume —
   ACES tonemapping, bloom (low), color-grade LUT (teal-shadow/amber-light),
   vignette, film grain, DoF in scope view and bullet cam. Retire the UI-based
   grain/vignette overlays (keep the letterbox).
5. **Audio pass.** freesound.org CC0: real rifle crack, bolt, body fall +
   phone-recorded foley. Swap into `ProceduralAudio` (keep procedural wind +
   heartbeat). Install **Steam Audio** (free) → *Project Settings → Audio →
   Spatializer Plugin: Steam Audio* for true binaural HRTF.

**CHECKPOINT 7 (the Phase B gate):** record 30 seconds of a mission ending in a
bullet cam. Show it to a stranger. If they ask "what game is that?" — pass.
If not, iterate on lighting/grading (cheapest, highest-impact) before touching
more assets.

---

## STAGE 8 — Content expansion (Proposal Phase C)

1. Extend `Campaign.cs`: Region 1 and 2 to 10–15 contracts each, then add
   **Mountain Pass** and **Marina at Night** the same way (the home screen,
   select board and unlock gating pick up new data automatically).
2. New mission logic in `GameManager`/`TargetSpawner` (one at a time, each
   fully finished before the next):
   - **VIP protect:** one target is friendly — hitting it fails the mission
   - **Convoy:** movers follow a path (waypoints array on `TargetMover`)
   - **No-collateral:** civilians wander; hitting one fails
   - **Chain:** targets must die in marked order
   - **Region boss:** unique named target, longer intel, scripted bullet cam
3. **Rifles 2–12:** turn the rifle into data (`RifleDef`: zoom, sway, bolt
   time, clip, damage class, model ref) + a garage picker. Reuse the existing
   upgrade tracks per rifle.
4. Per-region environment variation: skybox/fog/LUT swap + one landmark each.

**CHECKPOINT 8:** a new player plays 45+ minutes unprompted; 40+ contracts;
every mission type completable and failable correctly.

---

## STAGE 9 — Monetization + services (Proposal Phase D, all free-tier)

Order: services → IAP → ads → pass. Everything behind a feature flag.

1. **Play Games Services:** plugin from Google's GitHub → sign-in, cloud save
   (sync the existing JSON on login/mission-complete, last-write-wins with
   version numbers), achievements, weekly-mission leaderboard.
2. **Remote Config (Firebase):** move `Upgrades.BaseCost`, credit formula
   constants, and season timing into remote config with hardcoded fallbacks.
3. **IAP:** *Window → Package Manager → Unity IAP*; products: `operator_pack`
   (non-consumable, $6.99), `season_pass_s1` (non-consumable, $4.99), 2–3
   credit packs (consumable). Add **Play Integrity** checks. Test with Play
   Console **license testers** (needs Stage 0's account approved).
4. **Rewarded ads:** Unity LevelPlay (or AdMob) — **rewarded placements
   only**: Intel Drop crate in the Safehouse (+credits) and an optional 2×
   on the results screen. No interstitials, ever (this is the brand).
5. **Season pass v1:** ~30 items (weapon skins, reticle styles, bullet-cam
   trail colors, kill-card themes) using a simple material/color system; free
   + premium tracks; XP = mission score.

**CHECKPOINT 9:** on a test build — a purchase completes and restores; an Intel
Drop pays out once per watch; economy numbers change from remote config without
a rebuild; cloud save survives reinstall.

---

## STAGE 10 — Release (Proposal Phases E–F)

1. **Play Console setup:** app entry, store listing (name, descriptions,
   4K-captured screenshots, feature graphic), content rating questionnaire
   (IARC), **Data Safety form** (declare Firebase/ads data), privacy policy
   (generate one, host on GitHub Pages, free).
2. **Closed testing:** upload an AAB (*Build App Bundle*, signed with an
   upload key you back up twice). Invite 10–20 testers. Fix the crash list.
   Google requires a closed-test period for new personal accounts — plan for it.
3. **Open testing (soft launch):** promote the track; watch Crashlytics +
   Analytics against the proposal's gates: crash-free ≥ 99.5%, D1 ≥ 35%,
   rewarded engagement ≥ 25% of DAU. Tune economy via Remote Config only.
4. **Production:** staged rollout 10% → 50% → 100%. Launch trailer = your best
   bullet-cam clips (4K capture). Post to genre communities; respond to every
   review in the first weeks.
5. **Live ops cadence:** daily challenge (seeded variation — small code task),
   weekly featured mission + leaderboard, ~6-weekly season rotation, monthly
   contract drops. Add the post-revenue backend (receipt validation service)
   when revenue exists.

**CHECKPOINT 10:** the game is live on Google Play at 100% rollout with
crash-free ≥ 99.5% and a working live-ops loop.

---

## The rules that keep this on track

1. **Never break Checkpoint 2.** After every stage, the baseline test still
   passes. Keep a green build always.
2. **One thing at a time.** One mission type, one rifle, one SDK — finished,
   tested, committed — before the next.
3. **Commit small, push always.** Your repo is your safety net.
4. **The gates are the schedule.** Solo + zero budget means calendar estimates
   are noise; passing checkpoints is the only progress that counts.
5. **When stuck for over an hour:** cut scope, not quality. A smaller game
   that ships beats a bigger one that doesn't.

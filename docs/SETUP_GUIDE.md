# Super Sniper 8D — Setup Guide (manual wiring, alternative path)

> **Note:** The recommended way to run this project is the code-generated
> bootstrap described in the top-level `README.md` — add one `GameBootstrap`
> component to an empty object and press Play, with zero Inspector wiring.
>
> This guide is kept as a reference for the older **manual** approach, where you
> assemble the scene and wire references by hand (useful if you want to swap in
> your own art, player rig, or Canvas). It predates the full script set and does
> not cover the bullet cam, dossier, or results screens.

A step-by-step to wire up the scripts into a playable vertical slice.

## Before you start
- Unity 6 LTS, 3D (URP) project
- Free Asset Store packages: **Starter Assets – FirstPerson**, a rifle model, an environment/rooftop
- Install **TextMeshPro** when prompted (Window > TextMeshPro > Import TMP Essentials)

## 1. Layers & Tags
1. Create a Layer called **Target**. Put all your target objects on it.
2. Create a Tag called **Head**. Apply it to the head collider of each target.

## 2. Player & Weapon
1. Drop the FirstPerson player rig into the scene.
2. Parent the rifle model under the Main Camera (bottom-right of view).
3. Add `WeaponController.cs` to the Main Camera.
4. In the Inspector, set:
   - **Cam** = Main Camera
   - **Hip FOV** = 60, **Scope Multiplier** = 8
   - **Hittable** = the Target layer
   - **Scope Overlay** / **Crosshair** = your UI images
   - **Shot Sound** = a 3D AudioSource with a rifle clip

## 3. Targets
For each target:
1. Add a **Rigidbody** (tick *Is Kinematic* so it stands still until hit).
2. Add a **Collider** on the body, plus a child collider tagged **Head**.
3. Add `Target.cs`. Optionally assign a hit sound and particle effect.

## 4. UI (Canvas)
Create a Canvas with:
- **Score** text (TMP)
- **Mission** text (TMP)
- **Crosshair** image (center, small)
- **Scope Overlay** image (full-screen black ring + crosshair, disabled by default)
- **Win Panel** (disabled) with a "Mission Complete" label + Restart button

## 5. GameManager
1. Create an empty GameObject called **GameManager**.
2. Add `GameManager.cs`. Assign Score text, Mission text, Win Panel.
3. On `WeaponController`, set **Game Manager** = this object.
4. On the Restart button's OnClick, call `GameManager.Restart`.

## 6. 8D Audio
- On every AudioSource (shot, footsteps, ambient), set **Spatial Blend = 1.0 (3D)**.
- For true binaural "8D" in headphones, install the free **Steam Audio** plugin
  and enable HRTF spatialization in Project Settings > Audio.

## 7. Test
Press Play:
- Left click = shoot
- Hold right click = 8x scope
- Hit 3 targets = Win panel appears

## Next steps (after the slice works)
- Bullet-cam / slow-mo on final kill
- Multiple weapons + upgrade screen
- Mobile touch controls (on-screen fire + aim buttons)
- Level/mission select
- Ads + IAP (only once the loop is fun)

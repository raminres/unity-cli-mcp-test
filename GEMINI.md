# BlockBreaker: System Instructions, Workspace Map & Knowledge Items (Source of Truth)

This file provides foundational mandates, architectural maps, key mathematical constraints, subsystem knowledge items, and workflows for developer agents. It is automatically pre-loaded at the start of every session to serve as the single source of truth, minimizing token usage and research turns.

---

## 1. Project Specs & Key Paths
- **Product Name / Bundle ID**: `BlockBreaker` / `com.RaminRasulzade.BlockBreaker`
- **Unity Version**: Unity 6 (`6000.6.0f1`), Universal Render Pipeline (`URP 17.6.0`)
- **Active Branch**: `feature/ui-touch-visual-improvements` (Git LFS enabled)
- **Main Scenes**:
  1. `Assets/Scenes/LV_BlockBreaker_MainMenu.unity` (Build Index 0, Main Menu & Start Scene configured via `PlayModeSceneSetup.cs`)
  2. `Assets/Scenes/LV_BlockBreaker.unity` (Build Index 1, Primary Gameplay Arena)
- **Key Directories**:
  - Scripts: `Assets/Scripts/` (Assembly: `Arcade.Gameplay.asmdef`)
  - Editor Tools: `Assets/Editor/` (Assembly: `Arcade.Editor.asmdef`)
  - Prefabs: `Assets/Prefabs/` (100% prefab-driven suite: Arena, Paddle, Balls, Blocks, Powerups)
  - Materials: `Assets/Materials/BlockBreaker/` (`MT_Master_PBR_URP.mat` master, `MI_*` instances)
  - Settings: `Assets/Settings/` (Levels `SO_Level_01` to `SO_Level_15`, `SO_PowerupIcons.asset`, URP Profiles)
  - Localization: `Assets/Localization/` (`Locales/`, `Tables/`, `LocalizationSettings.asset`)
  - UI Assets: `Assets/UI/` (UXML, USS, Fonts, Icons, procedural gradients under `Textures/Gradients/`)
  - Tests: `Assets/Tests/` (Assembly: `Arcade.Tests.asmdef`, 270 EditMode NUnit unit & integration tests)

---

## 2. Core Rules & Conventions
- **Zero Runtime Primitives**: DO NOT use `GameObject.CreatePrimitive` at runtime or in scene setups. All objects instantiate directly from `Assets/Prefabs/`.
- **Decoupled Physics & Visuals**: Physical colliders are placed exclusively on root container objects; 3D visual models reside in child GameObjects with all colliders removed to prevent double-collision glitches and phantom catches.
- **Naming Conventions**:
  - Materials: `MT_*` (Master), `MI_*` (Instance/Variant under `Assets/Materials/BlockBreaker/`)
  - Textures: `TX_*` (`TX_*_BaseColor`, `TX_*_Normal`, `TX_*_Emissive`)
  - Audio: `AU_*` (e.g. `AU_Pop`, `AU_Break`, `AU_Powerup`, `AU_Shield_Impact`, `AU_Game_Over`)
  - ScriptableObjects: `SO_*` (e.g. `SO_Level_*.asset`, `SO_PowerupIcons.asset`)
  - Localization: `Assets/Localization/Tables/ArcadeTable.asset` (`ArcadeTable_en.asset`, `ArcadeTable_tr.asset`)
  - Prefabs: `PF_*` under `Assets/Prefabs/`
  - Presets: `PR_*` under `Assets/Presets/`
- **Warning Suppression**: NEVER suppress/disable compiler warnings, use hacks, or bypass type systems.

---

## 3. Modular Prefab Suite (`Assets/Prefabs/`)

| Category | Prefab Name | Hierarchy & Sockets | Components & Physics |
| :--- | :--- | :--- | :--- |
| **Arena** | `PF_Walls.prefab` | `Side_Left` ($X = -10.25, H = 30.2$), `Side_Right` ($X = +10.25, H = 30.2$), `Top_Wall` ($Y = 24.25, W = 17.4$), `Chamfer_Left` & `Chamfer_Right` (45° wedges at $\pm 9.40, 23.40$), `KillZone` ($Y = -9.0$, Tag `"KillZone"`). | `ArenaWalls.cs`, decoupled BoxColliders on each boundary with `PM_ArcadeBounce.physicMaterial`. |
| **Arena** | `PF_Background.prefab` | Root container + child `plane` ($40 \times 80$ quad). | `LevelBackgroundController.cs`, `URP/Unlit` double-sided material (`MI_Background_Gradient.mat`), 4 cosmic textures (`TX_Background_Gradient_A` to `D`). |
| **Arena** | `PF_ShieldWall.prefab` | Root + Child 1 `model` (`MI_ShieldWall.mat`, zero colliders) + Child 2 `vfx` (`ParticleSystem` cyan energy barrier). | `ShieldWall.cs`, root `BoxCollider` (`size = (1, 1, 1)` with `PM_ArcadeBounce.physicMaterial`). Emits 12 particles on ball impact via `shieldVfx.Emit(12)`. |
| **Paddle** | `PF_Paddle.prefab` | Root + sockets: `visuals` (`deck`, `chassis`, `keel`), `guns` (`Muzzle_Left`, `Muzzle_Right`), `laser_gun` (`VFX_Railgun_HyperBeam`), `frost` (freeze shell), `dock_point` (`(0, 0.88f, 0)`), `vfx` (sparks). | `PaddleController.cs`, `PaddleLaserController.cs`. Root `BoxCollider` ($W=5.0, H=0.24, Z=1.0$ at center $Y=+0.38$) with `PM_ArcadeBounce.physicMaterial`. |
| **Ball** | `PF_Ball_Standard.prefab` | Root + child visual sphere mesh + `TrailRenderer`. | `BallController.cs`, root `SphereCollider` (radius 0.4, $D = 0.8\text{u}$), `Rigidbody` (interpolate, continuous dynamic, velocity-driven). |
| **Blocks** | `PF_Block_Base.prefab`<br>`PF_Block_Red.prefab`<br>`PF_Block_Green.prefab`<br>`PF_Block_Blue.prefab`<br>`PF_Block_Bomb.prefab`<br>`PF_Block_Glass.prefab` | 4 standardized child sockets:<br>1. `brick`: PBR visual mesh (zero colliders).<br>2. `brick frost`: Ice encasement shell.<br>3. `brick special`: UI Toolkit badge element (`BlockBadge`).<br>4. `brick vfx`: Burst anchor. | `Block.cs`, root `BoxCollider` ($W = 1.15, H = 0.55, Z = 1.0$), `MaterialPropertyBlock` zero-allocation tinting. |
| **Drops** | `PF_Drop_Powerup.prefab`<br>`PF_Drop_Hazard.prefab` | Root container + Child 1 visual mesh (Capsule scale 0.85 / Diamond Cube scale 0.72) + Child 2 `Icon_Billboard` ($Z = -0.6\text{f}$). | `PowerupCapsule.cs`, trigger `BoxCollider`, tumbling rotation ($120^\circ/\text{s}$), fall speed $4.5\text{ u/s}$ at $Z = -1.0\text{f}$. |

---

## 4. Physics, Deflection Math & Safeguards
- **3-Tier Inverted Stepped Pyramid Paddle**:
  - **Tier 1 (Strike Deck)**: $W = 5.0, H = 0.24, Z = 1.0$ at $Y = -6.0$ with `MI_Paddle_Deck.mat`. Holds the main `BoxCollider` ($H = 0.24$, center $Y = +0.38$).
  - **Tier 2 (Mid Chassis)**: $W = 3.6, H = 0.20, Z = 0.88$ with `MI_Paddle.mat`.
  - **Tier 3 (Keel / Thrusters)**: $W = 2.2, H = 0.16, Z = 0.72$ with `MI_Paddle_Core.mat`.
  - Low vertical profile ($0.60$ total) and inward stepping ($\le 1.4\text{u}$/side) eliminate side catches.
  - Normal Validation: `BallController.IsValidPaddleBounceNormal(normal)` requires `normal.y >= 0.25f`. Brushing balls fall to the kill zone.
- **Deflection & Active Kinetic Strike**:
  - Formula: `Exit Angle = Ray Angle + Hit Offset Steer (-32° to +32°) + Paddle Velocity Steer (-45° to +45°)`, clamped to $[25^\circ, 155^\circ]$.
  - Vertical deadzone: Trajectory strictly excludes $[85^\circ, 95^\circ]$.
  - **Active Kinetic Strike**: When moving paddle $|V_x| \ge 3.5\text{ u/s}$, strike gives $+8\%$ speed pop (capped at `maxSpeed = 22f`), higher-pitch audio (`1.22f`), $18\%$ squash recoil, and cyan spark burst. Stationary paddle cushions ball and preserves speed.
- **Anti-Trap Trajectory Safeguards**:
  - **Vertical Floor**: $|v_y| \ge \text{currentSpeed} \cdot \sin(20^\circ)$ prevents horizontal trapping.
  - **Wall Steepener**: $\ge 2$ consecutive wall bounces steepens trajectory to $\ge 35^\circ$.
  - **Continuous X Movement**: Enforces $|v_x| \ge \text{currentSpeed} \cdot \sin(5^\circ)$.
  - **45° Corner Chamfers**: Angled wedges at $(\pm 9.40, 23.40)$ join top and side walls, preventing top corner wedging.

---

## 5. Tactical Brick Interactions (Model A)
- 🔴 **Red (Tier 1, 10 pts) — Kinetic Dampener**: Reduces ball speed by $-1.2\text{ u/s}$ (floored at `baseSpeed = 14f`). Pitch $0.90\times$.
- 🟢 **Green (Tier 2, 20 pts) — Kinetic Turbo**: Boosts ball speed $+10\%$ (capped at `maxSpeed = 22f`). Pitch $1.10\times$.
- 🔵 **Blue (Tier 3, 30 pts) — Optical Scatter**: Rotates exit trajectory by $\pm 18^\circ$ to $\pm 28^\circ$ while respecting anti-trap constraints. Pitch $1.25\times$.
- **Glass Bricks**: 2-hit reinforced blocks (Hit 1: shatters glass shell; Hit 2: breaks brick for $2\times$ score).
- **Bomb Bricks**: Explosive trigger, cascades destruction across blocks in a $2.5\text{u}$ radius with compound $1.5^{\text{chainIndex}}$ multiplier.

---

## 6. Pickups, Hazards & Defense Architecture
- **Drops Speed**: Fall velocity $4.5\text{ u/s}$ at $Z = -1.0\text{f}$. Destroyed on life loss, shield save, level clear, or game over via `ClearAllFallingCapsules()`.
- **Powerups (Cyan `#00f2fe`, Capsule, scale 0.85)**:
  - **Expander**: Widens paddle $+10\%$ compounding (max $12.0\text{u}$, spring overshoot animation, 10s).
  - **Score Multipliers**: Multiplies break score by $2\times, 3\times, 4\times,$ or $5\times$ (10s).
  - **Multi-Ball**: Spawns 2 extra balls at $\pm 35^\circ$; scales all scored points ($2\times$ or $3\times$).
  - **Extra Heart**: Grants $+1$ life (up to 5 max).
  - **Laser Blaster**: Twin cannons firing ruby bolts ($34\text{ u/s}$) at $0.32\text{s}$ interval (10s).
- **Powerdowns / Hazards (Crimson `#ff1744`, Diamond, scale 0.72)**:
  - **Paddle Shortener**: Shrinks paddle $-18\%$ (min $2.4\text{u}$, 10s).
  - **Paddle Slower**: Adds input drag, reducing speed by $-50\%$ (8s).
  - **Brick Freezer**: Glaciates 5 bricks; each absorbs 1 defrost hit before shattering.
  - **Ball Shrinker**: Shrinks ball radius to $60\%$ ($0.8\text{u} \to 0.48\text{u}$, 10s).
  - **Ball Slower**: Slows ball velocity to $9.5\text{ u/s}$ (8s).
  - **Paddle Freezer**: Immobilizes paddle for $1.2\text{s}$; input triggers visible $45\text{Hz}$ tremor (`Mathf.Sin(Time.time * 45f) * 0.07f`).
- **Physical Shield Wall (`PF_ShieldWall.prefab`)**:
  - Deployed at $Y = -7.6\text{f}$ for 10s on shield pickup with smooth tween overshoot ($0 \to 1.08 \to 1.0$).
  - Upward anti-trap one-way passthrough allows balls below to rise through collider without docking or life penalty. Bounces downward balls back upward at $\ge 35^\circ$.
  - Particle energy barrier emits 12-particle kinetic bursts on ball impacts.

---

## 7. Camera, Lighting & UI Toolkit
- **Perspective Camera**: $38^\circ$ FOV at $Z = -32\text{f}$, centered at $Y = 8.5\text{f}$. `ResponsiveCameraController.cs` dynamically adjusts distance to keep boundaries 100% visible on 16:9, 9:16, and 19.5:9 aspect ratios.
- **Main Menu Scene (`LV_BlockBreaker_MainMenu.unity`)**:
  - `UI_MainMenu` has transparent `.root-container` (no solid background or `.bg-glow`), revealing the 3D scene's `PF_Background` quad.
  - Camera position ($Z = -32\text{f}$), FOV ($38^\circ$), Global Volume Bloom, and `ResponsiveCameraController` matched 1:1 with gameplay.
- **UI Toolkit & Screen Harmonization**:
  - Unity 6 `PanelRenderer` on `UI_HUD` and `UI_MainMenu` with iPhone portrait reference resolution (`1170x2532`).
  - **Mobile Safe Area & Dynamic Island Adaptation**: `SafeAreaController.cs` calculates Yoga width-relative insets via `CalculateYogaInsets`, ensuring complete clearance from Dynamic Island and device notches on iPhone 15/15 Pro in physical builds and Unity Device Simulator. `#safe-area-content` wraps top bar dashboard pods and powerup rows, while modals maintain full-bleed coverage.
  - **Touch Ergonomics & System Gesture Deferral**: `PlayerSettings.iOS.deferSystemGesturesMode = UnityEngine.iOS.SystemGestureDeferMode.All` configures iOS to require a deliberate double-swipe for Home bar navigation, preventing edge gesture drops. Input position clamps to $Y \ge 4\text{px}$ and a minimal, transparent touch guideline (`#touch-guideline`, `opacity: 0.16`, `pickingMode: Ignore`) sits comfortably above the iOS Home bar indicator.
  - **Tactile 3D Extruded Gradient Design System**:
    - Discarded transparent/liquid glass in favor of solid opaque obsidian navy backgrounds (`rgb(18, 24, 40)`) with 4 procedural vertical gradient sprites in `Assets/UI/Textures/Gradients/` (`TX_Grad_Card_Bg`, `TX_Grad_Ruby_Btn`, `TX_Grad_Emerald_Btn`, `TX_Grad_Titanium_Btn`).
    - Mechanical 3D extruded button geometry: resting 7px bottom shelf (`border-bottom-width: 7px;`), 2px top/side bevels, and active physical depression (`translate: 0 5px; border-bottom-width: 2px; border-top-width: 4px;`).
    - Propagated across all modal cards (`.modal-card`, `.level-modal-card`, `.scorecard-card`) and button styles (`.arcade-btn`, `.arcade-button`, `.btn-primary`, `.btn-secondary`, `.btn-default`, `.warning-btn`) across both Main Menu and Gameplay HUD.
  - **Menu Parity**: Pause Menu sub-screens (How to Play, Credits, Level Select, and Options) match Main Menu in layout structure, fonts, button styling, and responsive proportions.
  - **Custom Animated Mute Toggles**: Both Main Menu and In-Game Pause Options panels feature custom checkmark toggles for SFX and Music sliders (`68px × 68px`). Toggles dynamically switch between speaker/mute icons with glowing cyan (`#21d4fd`) and crimson (`#ff3b56`) tints.
  - Safe area insets handled by `SafeAreaController.cs`. Sprites managed via `SO_PowerupIcons.asset`.
- **Unity Localization Tables (`com.unity.localization` 1.5.13)**:
  - Multi-language StringTableCollection `ArcadeTable.asset` under `Assets/Localization/Tables/` containing English (`en`) and Turkish (`tr`) locales.
  - Generable via `Assets/Editor/SetupLocalizationTables.cs` (`Tools > Arcade > Setup Localization Tables`).
  - `LocalizationManager.cs` retrieves translations directly from active `StringTable` with zero-overhead dictionary fallback.

---

## 8. End-Game, Hyper-Beam & Campaign
- **Lone Block Countdown**: Triggered when exactly 1 brick remains. Starts a 12s countdown with score multiplier decaying from $10\times \to 1\times$.
- **Railgun Hyper-Beam**: On countdown expiration, fires vertical railgun beam ($W \approx 3.2$, surge duration $0.65\text{s}$), destroying remaining bricks.
- **Victory Freeze**: When level clears, freeze all ball velocities, lock paddle input, freeze falling capsules, wait $1.0\text{s}$ (or $1.5\text{s}$ for Hyper-Beam) before displaying scorecard.
- **15-Level Campaign**: High-density grids (11–14 columns, $13.75\text{u}–17.5\text{u}$ span) with outer flank bumper blocks. Ball speed scales from $0.92\times$ to $1.48\times$, paddle narrows from $5.5\text{u}$ to $4.5\text{u}$, and hazards escalate from 0 to 13.
- **Onboarding Block Scale (+20% on Levels 1–3)**: Levels 1, 2, and 3 (`SO_Level_01`, `SO_Level_02`, `SO_Level_03`) feature enlarged blocks (`blockSize = 1.20f`) with adjusted horizontal spacing ($1.50\text{u}, 1.45\text{u}, 1.40\text{u}$) to maximize readability and touch targeting comfort while strictly preserving $> 1.0\text{u}$ clearance to arena walls ($X = \pm 10.25$). Levels 4–15 retain standard $1.00\text{u}$ scale. Runtime setters: `SetBlockSize`, `SetHorizontalSpacing`, `SetVerticalSpacing` in `LevelConfiguration.cs`.

---

## 9. Knowledge Items & Subsystem Map (Quick Reference)

Use this architectural lookup table to locate subsystems, classes, and responsibilities instantly without exploratory searching:

| Subsystem | Primary Script(s) | Primary Prefab / Asset | Key Responsibility |
| :--- | :--- | :--- | :--- |
| **Ball Physics** | `Assets/Scripts/BlockBreaker/BallController.cs` | `Assets/Prefabs/Balls/PF_Ball_Standard.prefab` | Dynamic deflection math, anti-trap floor, wall steepener, speed capping, active strike recoil, kinetic color interactions. |
| **Paddle Control** | `Assets/Scripts/BlockBreaker/PaddleController.cs`<br>`Assets/Scripts/BlockBreaker/PaddleLaserController.cs` | `Assets/Prefabs/Paddle/PF_Paddle.prefab` | Mouse/touch 1:1 tracking, keyboard smoothing, blaster twin ruby bolts, hyper-beam aperture, freeze tremor, dock point. |
| **Arena Boundaries** | `Assets/Scripts/BlockBreaker/ArenaWalls.cs`<br>`Assets/Scripts/BlockBreaker/KillZone.cs` | `Assets/Prefabs/Arena/PF_Walls.prefab` | Side/top boundary colliders, 45° corner chamfer traps prevention, kill zone trigger at $Y = -9.0$. |
| **Shield Defense** | `Assets/Scripts/BlockBreaker/ShieldWall.cs` | `Assets/Prefabs/Arena/PF_ShieldWall.prefab` | Defensive energy wall, tween scale overshoot, anti-trap one-way upward passthrough, particle impact emission. |
| **Level Generation** | `Assets/Scripts/BlockBreaker/LevelGenerator.cs`<br>`Assets/Scripts/BlockBreaker/Block.cs` | `Assets/Prefabs/Blocks/PF_Block_*.prefab` | Spawns modular block prefabs from ScriptableObjects, configures special badges/frost shells, bomb cascade triggers. |
| **Background Plane** | `Assets/Scripts/BlockBreaker/LevelBackgroundController.cs` | `Assets/Prefabs/Arena/PF_Background.prefab` | Cosmic gradient quad at $Z = 6.0\text{f}$ ($40 \times 80$), randomizes textures on level transitions via MaterialPropertyBlock. |
| **Drops & Buffs** | `Assets/Scripts/BlockBreaker/PowerupCapsule.cs` | `Assets/Prefabs/Powerups/PF_Drop_*.prefab` | Tumbling capsule/diamond falls, pickup triggers, duration timers, HUD badge updates. |
| **Game Coordinator** | `Assets/Scripts/Core/ArcadeGameManager.cs` | Scene Coordinator GameObject | Lives, score multiplier combos, clutch countdown, hyper-beam railgun sequence, victory freeze, scorecard popup. |
| **Responsive Camera** | `Assets/Scripts/Core/ResponsiveCameraController.cs` | Main Camera in both scenes | Calculates distance required to fit $23 \times 32.5$ arena across any device aspect ratio (16:9, 9:16, 19.5:9). |
| **Audio Engine** | `Assets/Scripts/Audio/ArcadeAudioManager.cs` | `AudioManager` persistent GameObject | Plays custom SFX (`AU_*`) with algorithmic fallback synthesis (pops, cracks, chimes, sirens). |
| **Localization** | `Assets/Scripts/Core/LocalizationManager.cs` | `Assets/Localization/Tables/ArcadeTable.asset` | Unity Localization Tables (`com.unity.localization`), multi-locale table queries (`en`/`tr`), fallback dictionary. |
| **Localization Tool** | `Assets/Editor/SetupLocalizationTables.cs` | Menu: `Tools/Arcade/Setup Localization Tables` | Generates and populates StringTableCollection, Locales, and setting assets. |
| **UI HUD & Menu** | `Assets/Scripts/UI/BlockBreakerHUD.cs`<br>`Assets/Scripts/UI/MainMenuUIManager.cs`<br>`Assets/Scripts/UI/ArcadeUIManager.cs`<br>`Assets/Scripts/UI/SafeAreaController.cs` | `Assets/UI/BlockBreakerHUD.uxml`<br>`Assets/UI/MainMenuUI.uxml` | Unity 6 PanelRenderer HUD dashboard pods, transparent main menu overlay, menu options parity, custom mute toggles, safe-area insets. |
| **Scene Generator** | `Assets/Editor/SetupBlockBreakerScenes.cs` | Menu: `Tools/Arcade/Setup All Block Breaker Scenes` | Completely generates and saves `LV_BlockBreaker_MainMenu.unity` and `LV_BlockBreaker.unity` from prefabs. |

---

## 10. Testing & Verification Command
All logic, boundaries, prefabs, hazards, scene setups, localization tables, touch ergonomics, and level scaling are strictly verified by **270 automated EditMode NUnit tests**. To execute:
```bash
unity cmd run_tests --mode editor --timeout 90
```
Or via the Unity Editor Test Runner window (EditMode tab).

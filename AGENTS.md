# Project Context & Architecture: BlockBreaker

High-density technical context and architectural rules for BlockBreaker.

---

## 1. Project Overview & Identity
- **Product / Bundle ID**: `BlockBreaker` / `com.RaminRasulzade.BlockBreaker`
- **Engine / Pipeline**: Unity 6 (`6000.6.0f1`), Universal Render Pipeline (`URP 17.6.0`)
- **Scenes**:
  1. `Assets/Scenes/LV_BlockBreaker_MainMenu.unity` (Build Index 0, Start Scene configured via [PlayModeSceneSetup.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Editor/PlayModeSceneSetup.cs))
  2. `Assets/Scenes/LV_BlockBreaker.unity` (Build Index 1, Primary Gameplay)
- **Repository**: `https://github.com/raminres/unity-cli-mcp-test.git` (Active Branch: `feature/ui-touch-visual-improvements`, Git LFS enabled)

---

## 2. Naming Conventions & Asset Presets
- **Materials**: `MT_Master_PBR_URP.mat` (Master), `MI_*` (Instances in `Assets/Materials/BlockBreaker/`).
- **Textures / Icons**: `TX_` prefix with semantic suffixes (`_BaseColor`, `_Normal`, `_Emissive`). UI icons in `Assets/UI/Icons/`.
- **Audio**: `AU_*` (e.g. `AU_Pop`, `AU_Break`, `AU_Powerup`, `AU_Powerup_Laser`, `AU_Powerup_Shield`, `AU_Life_Lost`, `AU_Level_Success`, `AU_Game_Over`, `AU_Bomb_Explosion`, `AU_Glass_Break`).
- **ScriptableObjects**: `SO_*` (`Assets/Settings/Levels/SO_Level_*.asset`, `Assets/Settings/SO_PowerupIcons.asset`).
- **Presets**: `PR_*` in `Assets/Presets/` managed by `PresetManager.asset` and [SetupAssetPresets.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Editor/SetupAssetPresets.cs).
- **Localization**: Unity Localization Tables under `Assets/Localization/` (`Locales/en.asset`, `Locales/tr.asset`, `Tables/ArcadeTable.asset`).

---

## 3. Core Gameplay Architecture & Physics
- **3-Tier Inverted Stepped Pyramid Paddle**:
  - **Tier 1 (Strike Deck)**: $100\%$ width ($W = 5.0$), ultra-thin ($H = 0.24, Z = 1.0$), glowing neon cyan rim ([MI_Paddle_Deck.mat](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Materials/BlockBreaker/MI_Paddle_Deck.mat)). Top surface at $Y = -6.0$ for ball docking.
  - **Tier 2 (Mid Chassis)**: $72\%$ width ($W = 3.6, H = 0.20, Z = 0.88$) in dark brushed titanium ([MI_Paddle.mat](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Materials/BlockBreaker/MI_Paddle.mat)).
  - **Tier 3 (Keel / Thrusters)**: $44\%$ width ($W = 2.2, H = 0.16, Z = 0.72$) with engine vent glow ([MI_Paddle_Core.mat](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Materials/BlockBreaker/MI_Paddle_Core.mat)).
  - Total vertical profile: $0.60$ (lower sides stepped inward $\le 1.4\text{u}$/side to eliminate phantom side catches).
  - **Collider & Normal Guard**: Primary `BoxCollider` fitted strictly to Tier 1 ($H = 0.24$, center $Y = +0.38$). Below $Y = -6.24$, zero collision volume exists. `BallController.IsValidPaddleBounceNormal(normal)` (`normal.y >= 0.25f`) guarantees brushing balls fall cleanly into killzone.
- **Deflection Math, Friction & Active Kinetic Strike**:
  - `BallController.CalculatePaddleDeflection(inVelocity, hitOffset, steerStrength = 32f, minAngleDeg = 25f, maxAngleDeg = 155f, paddleVelocityX, velocityInfluence = 1.5f, verticalDeadzoneAngleDeg = 5f, maxVelocitySteerDeg = 45f)`:
    - Base angle: `Mathf.Atan2(|inVelocity.y|, inVelocity.x) * Rad2Deg`
    - Steer: `steer = -hitOffset * 32f`
    - Paddle velocity steer: `clamp(-paddleVelocityX * 1.5f, -45°, +45°)`. Deliberate counter-swipe reverses horizontal momentum (reversal cut/slice).
    - Clamped to $[25^\circ, 155^\circ]$, strictly excluding vertical deadzone $[85^\circ, 95^\circ]$.
  - **Kinetic Speed Pop ("Active Strike")**: Moving paddle strike ($|V_x| \ge 3.5\text{ u/s}$) gives $+8\%$ speed pop (capped at `maxSpeed = 22f`), higher-pitch audio (`1.22f`), $18\%$ squash recoil, and cyan spark burst (`BlockVFXManager.PlayPaddleHitSpark`). Stationary paddle preserves speed for cushion control.
- **Anti-Trap Ball Physics**:
  - **Vertical Floor**: $|v_y| \ge \text{currentSpeed} \cdot \sin(20^\circ)$ prevents shallow horizontal traps.
  - **Wall Steepener**: $\ge 2$ consecutive wall bounces steepens trajectory to $\ge 35^\circ$.
  - **Vertical Deadzone**: Trajectory excludes $[85^\circ, 95^\circ]$; continuous play enforces $|v_x| \ge \text{currentSpeed} \cdot \sin(5^\circ)$.
  - **45° Corner Chamfers**: Top corners $(\pm 9.40, 23.40)$ have $45^\circ$ wedges ($2.5 \times 0.5 \times 2.0$) joining top wall ($Y = 24.25, W = 17.4$) and side walls ($X = \pm 10.25, H = 30.2$). Guaranteed via `LevelGenerator.EnsureCornerChamfers()`.
- **Arena & Camera**:
  - Kill Zone: $Y = -9.0$ ([KillZone.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/KillZone.cs)).
  - Camera: $38^\circ$ FOV with [ResponsiveCameraController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/Core/ResponsiveCameraController.cs) dynamically adjusting $Z$-distance for 100% boundary visibility on 16:9, 9:16, 9:19.5.
- **Brick Color Physical Interactions (Model A)**:
  - 🔴 **Red (Tier 1, 10 pts)**: Kinetic Dampener. Reduces speed by $-1.2\text{ u/s}$ (floored at `baseSpeed = 14f`). Audio pitch $0.90\times$.
  - 🟢 **Green (Tier 2, 20 pts)**: Kinetic Turbo. Boosts speed $+10\%$ (capped at `maxSpeed = 22f`). Audio pitch $1.10\times$.
  - 🔵 **Blue (Tier 3, 30 pts)**: Optical Prism Scatter. Rotates exit angle by $\pm 18^\circ$ to $\pm 28^\circ$ while enforcing anti-trap constraints. Audio pitch $1.25\times$.
- **Scoring & Multipliers**:
  - Lives: 3 starting (max 5).
  - Volley Combo: Unreturned rallies scale multiplier: Hits 1–2 ($1\times$), 3–4 ($2\times$), 5–7 ($3\times$), 8–10 ($4\times$), 11+ ($5\times$ MAX). Banks on paddle hit; resets on killzone. SFX pitches up $+1$ semitone per hit.
  - Synergies: Bomb chains ($\text{base} \times 1.5^{\text{chainIndex}}$), Multi-Ball ($2\times/3\times$ points during multi-ball).
  - Floating Popups: Spawns at $Z = -0.8\text{f}$, drifts $+1.2\text{u}$ over $0.65\text{s}$ ([FloatingScoreManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/FloatingScoreManager.cs)).
- **Clutch Hyper-Beam & Level Clear**:
  - Lone Block Countdown: Exactly 1 brick remaining activates 12s countdown ($10\times \to 1\times$ decaying multiplier).
  - Railgun Hyper-Beam: On timer expiry, fires vertical hyper-beam ($W \approx 3.2, H \approx 31$, surge duration $0.65\text{s}$, shader `Arcade/VFX_LaserHyperBeam`).
  - Clear Delay & Freeze: $1.0\text{s}$ standard clear, $1.5\text{s}$ hyper-beam clear before scorecard modal. All balls freeze velocity (`BallController.FreezeBall()`), paddle locks, falling capsules freeze.

---

## 4. Powerups, Hazards & Defense Architecture
- **Collectible Drops ([PowerupCapsule.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/PowerupCapsule.cs))**:
  - Fall speed $4.5\text{ u/s}$, $Z = -1.0\text{f}$. Root container (scale $1.0$), Child 1 visual mesh, Child 2 `Icon_Billboard` ($Z = -0.6\text{f}$). Destroyed via `ClearAllFallingCapsules()` on life loss, shield save, clear, or game over.
  - **Powerups (Cyan `#00f2fe`)**: Rounded 3D capsule (`PrimitiveType.Capsule`, scale $0.85$).
    - Expander ($+10\%$ compounding, max $12.0\text{u}$, spring overshoot), Extra Heart ($+1$ life), Shield (physical wall), Multi-Ball (+2 balls at $\pm 35^\circ$), Multipliers (2X–5X), Laser Blaster (twin cannons, $34\text{ u/s}$, 10s).
  - **Powerdowns / Hazards (Crimson `#ff1744`)**: Faceted 3D diamond (`PrimitiveType.Cube` rotated $45^\circ$, scale $0.72$). Top HUD shows crimson badge and countdown timer.
    - Paddle Shortener ($-18\%$, min $2.4\text{u}$, 10s), Paddle Slower (input drag, 8s), Brick Freezer (encases up to 5 bricks; absorbs 1 defrost hit before break), Ball Shrinker ($60\%$ radius, 10s), Ball Slower ($9.5\text{ u/s}$, 8s), Paddle Freezer ($1.2\text{s}$ immobilization with sinusoidal tremor `Mathf.Sin(Time.time * 45f) * 0.07f`).
- **Physical Shield Wall ([ShieldWall.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/ShieldWall.cs), `PF_ShieldWall.prefab`)**:
  - Modular prefab: Root `BoxCollider` (`size = (1,1,1)`, `center = (0,0,0)`), Child 1 `model` (`MI_ShieldWall.mat`, zero collider), Child 2 `vfx` (`ParticleSystem` cyan energy barrier).
  - Deployed at $Y = -7.6\text{f}$ for 10s on shield pickup with smooth tween scale overshoot ($0 \to 1.08 \to 1.0$).
  - Physical collision reflection $\ge 35^\circ$ keeps balls live without life loss; impacts trigger `shieldVfx.Emit(12)` particle pulse.
  - Anti-trap one-way upward passthrough: balls moving upward beneath paddle/shield pass freely through collider.
- **Environmental**: Bomb Bricks ($2.5\text{u}$ radius cascade), Glass Shells (2 hits: shatter shell, break brick for $2\times$).

---

## 5. Level Campaign Arc & Pacing
- **Progression Overview**: 15 ScriptableObject levels (`SO_Level_01` to `SO_Level_15` via [SetupBlockBreakerScenes.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Editor/SetupBlockBreakerScenes.cs)).
  - Grids: High-density 11–14 columns ($13.75\text{u}–17.5\text{u}$ span, within $1.6\text{u}$ of walls). Outer side flank bumper blocks at $c = 0$ and $c = \text{cols} - 1$ prevent wall bypass lanes.
  - Archetypes: `Pyramid`, `Diamond`, `Pillars`, `Shield`, `HollowBox`, `Crown`, `Heart`, `Invader`, `Cross`, `Hourglass`, `Chevron`, `Castle`, `CheckerboardEmpty`, `Stripes`, `Custom`.
  - Speed scaling: $0.92\times$ (Level 1) $\to 1.48\times$ (Level 15). Volley pacing adds $+8\%$ per 10s continuous rally.
  - Paddle width: $5.5\text{u} \to 4.5\text{u}$. Hazard saturation: 0 (Level 1) $\to$ 13 (Level 15).
  - **Onboarding Block Scale (+20% on Levels 1–3)**: Levels 1, 2, and 3 (`SO_Level_01`, `SO_Level_02`, `SO_Level_03`) feature enlarged blocks (`blockSize = 1.20f`) with adjusted horizontal spacing ($1.50\text{u}, 1.45\text{u}, 1.40\text{u}$) to maximize readability and touch targeting comfort while strictly preserving $> 1.0\text{u}$ clearance to arena walls ($X = \pm 10.25$). Levels 4–15 retain standard $1.00\text{u}$ scale. Runtime setters: `SetBlockSize`, `SetHorizontalSpacing`, `SetVerticalSpacing` in [LevelConfiguration.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/LevelConfiguration.cs).

---

## 6. Graphics, UI & Systems Architecture
- **VFX System ([BlockVFXManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/BlockVFXManager.cs))**: Pooled bursts under `_Pool_VFX` ($Y = -500\text{f}$). Zero-allocation `MaterialPropertyBlock` tinting. Frame-0 prewarming in `Start()` compiles PSOs upfront on Metal/Vulkan/DX12/WebGPU.
- **Background**: Quad at $Z = 6.0\text{f}$ ($40 \times 80$) with `Universal Render Pipeline/Unlit` cosmic gradients ([LevelBackgroundController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/LevelBackgroundController.cs)).
- **UI Toolkit & Screen Harmonization**:
  - Unity 6 `PanelRenderer` on `UI_HUD` and `UI_MainMenu` scaled for iPhone portrait reference resolution (`1170x2532`).
  - **Mobile Safe Area & Dynamic Island Adaptation**: [SafeAreaController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/UI/SafeAreaController.cs) calculates Yoga width-relative insets (`CalculateYogaInsets`), preventing Dynamic Island and notch occlusion on iPhone 15/15 Pro in physical builds and Unity Device Simulator. `#safe-area-content` wraps top bar dashboard pods and powerup rows, while modals maintain full-bleed coverage.
  - **Touch Ergonomics & System Gesture Deferral**: `PlayerSettings.iOS.deferSystemGesturesMode = UnityEngine.iOS.SystemGestureDeferMode.All` configures iOS to require a deliberate double-swipe for Home bar navigation, preventing edge gesture drops. Input position clamps to $Y \ge 4\text{px}$ and a minimal, transparent touch guideline (`#touch-guideline`, `opacity: 0.16`, `pickingMode: Ignore`) sits comfortably above the iOS Home bar indicator.
  - **Tactile 3D Extruded Gradient Design System**:
    - Discarded transparent/liquid glass in favor of solid opaque obsidian navy backgrounds (`rgb(18, 24, 40)`) with 4 procedural vertical gradient sprites in `Assets/UI/Textures/Gradients/` (`TX_Grad_Card_Bg`, `TX_Grad_Ruby_Btn`, `TX_Grad_Emerald_Btn`, `TX_Grad_Titanium_Btn`).
    - Mechanical 3D extruded button geometry: resting 7px bottom shelf (`border-bottom-width: 7px;`), 2px top/side bevels, and active physical depression (`translate: 0 5px; border-bottom-width: 2px; border-top-width: 4px;`).
    - Propagated across all modal cards (`.modal-card`, `.level-modal-card`, `.scorecard-card`) and button styles (`.arcade-btn`, `.arcade-button`, `.btn-primary`, `.btn-secondary`, `.btn-default`, `.warning-btn`) across both Main Menu and Gameplay HUD.
  - **Menu Parity**: Pause Menu sub-screens (How to Play, Credits, Level Select, and Options) share visual design, responsive layouts, and button hierarchies with Main Menu.
  - **Custom Animated Mute Toggles**: Both Main Menu and In-Game Pause Options screens feature custom checkmark toggles for SFX and Music (`68px × 68px`). Toggles dynamically swap between speaker and mute icons, transitioning between cyan (`#21d4fd`) and crimson (`#ff3b56`) tints.
  - `UI_MainMenu`: Transparent `.root-container` reveals the 3D scene's `PF_Background` plane. Camera matched to $38^\circ$ FOV at $Z = -32\text{f}$ with `ResponsiveCameraController` and Global Volume Bloom.
  - Safe area insets handled by [SafeAreaController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/UI/SafeAreaController.cs). Sprites managed via `SO_PowerupIcons.asset`.
- **Unity Localization Tables System**:
  - Driven by `com.unity.localization` (`1.5.13`) under `Assets/Localization/`:
    - Locales: English (`en`) and Turkish (`tr`).
    - String Table Collection: `Tables/ArcadeTable.asset` (`ArcadeTable_en.asset`, `ArcadeTable_tr.asset`, `ArcadeTable Shared Data.asset`).
    - Configured via [SetupLocalizationTables.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Editor/SetupLocalizationTables.cs) (`Tools > Arcade > Setup Localization Tables`).
  - [LocalizationManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/Core/LocalizationManager.cs) queries active `StringTable` dynamically with instant fallback dictionary, allowing designers to edit translations directly in Unity's Localization Tables window without touching code.
- **Audio Engine ([ArcadeAudioManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/Audio/ArcadeAudioManager.cs))**: Custom clips with procedural audio synthesis fallback.

---

## 7. Modular Prefab Suite & Runtime Architecture
- **Complete Prefab Suite (`Assets/Prefabs/`)**:
  - `Arena/`: `PF_Walls` (left/right/top walls, 45° chamfers, kill zone), `PF_Background` (quad mesh + `LevelBackgroundController`), `PF_ShieldWall` (decoupled physics, visual model, electric energy barrier).
  - `Paddle/`: `PF_Paddle` (deck, chassis, keel, blaster cannons, laser aperture, frost shell, hit sparks).
  - `Balls/`: `PF_Ball_Standard` (decoupled sphere mesh, TrailRenderer, BallController).
  - `Blocks/`: `PF_Block_Base`, `PF_Block_Red`, `PF_Block_Green`, `PF_Block_Blue`, `PF_Block_Bomb`, `PF_Block_Glass` (4 child sockets: `brick`, `brick frost`, `brick special`, `brick vfx`).
  - `Powerups/`: `PF_Drop_Powerup` (cyan capsule), `PF_Drop_Hazard` (crimson diamond).
- **Zero Runtime Primitives**: All legacy procedural cube/quad fallbacks (`GameObject.CreatePrimitive`) removed from runtime and scene setups.
- **Automated Tests**: 270 EditMode tests passing across core physics, prefabs, boundaries, powerups, hazards, UI harmonization, localization tables, touch ergonomics, and level scaling:
  ```bash
  unity cmd run_tests --mode editor
  ```

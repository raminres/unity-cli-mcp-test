# Project Context & Architecture: BlockBreaker

This file provides persistent, high-density project context across agent sessions for this Unity project.

---

## 1. Project Overview & Identity
- **Product Name**: `BlockBreaker`
- **Company Name**: `RaminRasulzade`
- **Application / Bundle Identifier**: `com.RaminRasulzade.BlockBreaker` (iOS, Standalone, Android)
- **Engine Version**: Unity 6 (6000.6.0f1)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Play Mode Start Scene**: `Assets/Scenes/LV_BlockBreaker_MainMenu.unity` (configured via [PlayModeSceneSetup.cs](Assets/Editor/PlayModeSceneSetup.cs))
- **Remote Repository**: `https://github.com/raminres/unity-cli-mcp-test.git`
- **Active Branch**: `develop` (Git LFS enabled)

### Active Scenes & Build Index
1. `Assets/Scenes/LV_BlockBreaker_MainMenu.unity` (Build Index 0)
2. `Assets/Scenes/LV_BlockBreaker.unity` (Build Index 1)

---

## 2. Naming Conventions & Asset Presets
- **Materials**: `MT_` prefix for Master Materials (`Assets/Materials/MT_Master_PBR_URP.mat`), `MI_` prefix for Material Instances (`Assets/Materials/BlockBreaker/MI_*`).
- **Textures / Icons**: `TX_` prefix with semantic suffixes (`_BaseColor`, `_Normal`, `_MetallicSmoothness`, `_AO`, `_Emissive`). UI icons in `Assets/UI/Icons/`.
- **Audio**: `AU_` prefix (e.g. `AU_Pop.mp3`, `AU_Break.mp3`, `AU_Powerup.mp3`, `AU_Button_Press.mp3`, `AU_Level_Success.mp3`, `AU_Game_Over.mp3`, `AU_Life_Lost.mp3`, `AU_Powerup_Shield.mp3`).
- **ScriptableObjects**: `SO_` prefix (`Assets/Settings/Levels/SO_Level_*.asset`).
- **Presets**: `PR_` prefix in `Assets/Presets/` managed by `ProjectSettings/PresetManager.asset` and [SetupAssetPresets.cs](Assets/Editor/SetupAssetPresets.cs).

---

## 3. Core Gameplay Architecture
- **Paddle Math & Dynamic Deflection**:
  - Size: 5:1 aspect ($5.0 \times 1.0 \times 1.0$), initial position $Y = -6.5$.
  - Deflection formula: bounce angle $\theta = 90^\circ - (\text{normalizedOffset} \times 60^\circ)$.
  - Compounding expansion: $+10\%$ per expander ($W_n = W_{prev} \times 1.10$, clamped to max $12.0$, adaptive bounds $[-10 + \frac{W}{2}, 10 - \frac{W}{2}]$).
- **Arena Dimensions**:
  - Top Wall: $Y = 24.25$, Left/Right Walls: $X = \pm 10.5$ (height $32.0$), Kill Zone: $Y = -9.0$ ([KillZone.cs](Assets/Scripts/BlockBreaker/KillZone.cs)).
  - Camera: Perspective $38^\circ$ FOV with [ResponsiveCameraController.cs](Assets/Scripts/Core/ResponsiveCameraController.cs) dynamically adjusting $Z$-distance to guarantee 100% visible arena boundaries across any aspect ratio (16:9, 9:16, 9:19.5, etc.).
- **Lives & Scoring**:
  - Starting lives: 3 (expandable up to max 5 via Extra Heart powerup).
  - Tiers: Red = 10 pts (bottom), Green = 20 pts (middle), Blue = 30 pts (top).

---

## 4. Progressive 7-Level Campaign Arc
Levels scale smoothly in block count, speed, and mechanic introduction, looping back endlessly ($7 \to 1$) while preserving cumulative score:

| Level | Name | Theme & Star Mechanic | Cols $\times$ Rows | Blocks | Speed | Modifiers Breakdown |
| :---: | :--- | :--- | :---: | :---: | :---: | :--- |
| **1** | **First Flight** | **Warmup & Deflection Mastery** | $5 \times 3$ | **15** | `0.85x` (Paddle 5.5) | • 1x Paddle Expander<br>• *0x Hazards / Armored Bricks* |
| **2** | **Glass & Gold** | **Durability & High Scores** | $6 \times 3$ | **18** | `0.95x` (Paddle 5.0) | • 1x 2X Multiplier, 2x Glass-Enclosed, 1x Expander |
| **3** | **Chain Reaction** | **Explosive Cascades** | $7 \times 6$ | **42** | `1.05x` (Paddle 5.0) | • 2x Bombs, 2x 2X Multipliers, 1x Expander (`Checkerboard`) |
| **4** | **Kinetic Aegis** | **Speed Surge & Protective Net** | $8 \times 6$ | **48** | `1.15x` (Paddle 5.0) | • 1x Shield, 1x Extra Heart, 1x Bomb, 2x Glass, 1x 2X |
| **5** | **Multi-Ball Mayhem** | **Ball Juggling Rush** | $8 \times 6$ | **48** | `1.20x` (Paddle 5.0) | • 2x Multi-Ball, 1x Shield, 1x Bomb, 2x Glass, 1x 2X (`Checkerboard`) |
| **6** | **The High Roller** | **High Stakes & 4X Multiplier** | $9 \times 6$ | **54** | `1.28x` (Paddle 5.0) | • 1x 4X, 2x 3X, 1x 2X, 1x Heart, 1x Shield, 1x Multi-Ball, 2x Bombs, 3x Glass, 1x Expander |
| **7** | **Chaos Gauntlet** | **The Grand Climax & 5X Multiplier** | $10 \times 9$ | **90** | `1.38x` (Paddle 5.0) | • 1x 5X, 2x 4X, 2x 3X, 2x 2X, 2x Expanders, 3x Bombs, 4x Glass, 1x Heart, 2x Shields, 2x Multi-Balls (`Randomized`) |

- **Asset Storage**: `Assets/Settings/Levels/SO_Level_01.asset` through `SO_Level_07.asset`.
- **Automation**: [SetupBlockBreakerScenes.cs](Assets/Editor/SetupBlockBreakerScenes.cs) configures and wires all 7 presets into both scenes.

---

## 5. Powerups & Special Brick Archetypes
- **Paddle Expander (`PaddleExpander`)**: 10-second timed buff widening paddle by $+10\%$ compounding (clamped to max $12.0$), displays live HUD timer countdown, and reverts to base width on expiration. Plays break + powerup audio, cyan particle burst.
- **Timed Combo Score Multipliers (`ScoreMultiplier2x`, `ScoreMultiplier3x`, `ScoreMultiplier4x`, `ScoreMultiplier5x`)**: 10-second global combo window that multiplies points for ALL destroyed blocks (2x, 3x, 4x, 5x). Collecting higher tiers upgrades tier and refreshes duration; collecting equal/lower tiers refreshes duration. Displays live HUD countdown with tier badge styling.
- **Glass-Enclosed Bricks (`GlassEnclosed`)**: Encased in a $1.18\times$ glass shell ([MI_Block_Glass.mat](Assets/Materials/BlockBreaker/MI_Block_Glass.mat)). Requires 2 hits (Hit 1: shatters glass shell with crystal debris; Hit 2: breaks brick for $2\times$ points).
- **Bomb Bricks (`Bomb`)**: Explosive radius detonation ($2.5$ units) detonating surrounding bricks with outward impulses. Protected by `isDestroyed` flag against recursive loops.
- **Shield Powerup (`Shield`)**: 10-second defensive barrier. Falling balls intercept safely into `ReadyToLaunch` docked on paddle without losing lives. Displays live countdown timer on HUD.
- **Multi-Ball Powerup (`MultiBall`)**: Spawns 2 extra balls at $\pm 35^\circ$ diverging angles (3 balls active). Extra balls falling do NOT lose lives; only the final remaining ball causes life loss.
- **Extra Heart Powerup (`ExtraHeart`)**: Grants $+1$ life (up to 5 max) with flying heart HUD parabolic animation.
- **World Space Badges ([BlockBadge.cs](Assets/Scripts/BlockBreaker/BlockBadge.cs))**: Rendered via Unity 6 `PanelRenderer` in `WorldSpace` mode (`80px` fixed dimension, 100 PPU, clamped margins). Supports plates and text for `x2`, `x3`, `x4`, and `x5`.

---

## 6. Graphics, VFX & Performance Architecture
- **VFX System**: `Assets/VFX/VFX_BlockShatter.vfx` + physical 3D debris fragments.
- **Cross-Platform Frame-0 Prewarming ([BlockVFXManager.cs](Assets/Scripts/BlockBreaker/BlockVFXManager.cs))**:
  - Primes shaders and dispatches off-camera VFX simulation during `Start()` while in `ReadyToLaunch`. Forces Apple Metal, DX12, Vulkan, and WebGPU drivers to compile compute/raster PSOs upfront, eliminating first-hit hitching.
  - Zero-allocation `MaterialPropertyBlock` tinting: preserves 100% SRP Batcher compatibility without material cloning.
  - Struct-based simulation lists (`ActiveDebris`, `ActiveVFX`) replacing per-brick coroutines.
- **iOS Debris Material**: Custom Universal Render Pipeline shader `Assets/Shaders/VFX_BlockDebris.shader` (`Arcade/VFX_BlockDebris`) preventing uncompiled pink shaders on Apple Metal.

---

## 7. UI Toolkit & Audio Systems
- **PanelRenderer Migration**: Native Unity 6 `PanelRenderer` on `UI_HUD` and `UI_MainMenu` with `RegisterUIReloadCallback` lifecycle binding.
- **Safe Area Controller ([SafeAreaController.cs](Assets/Scripts/UI/SafeAreaController.cs))**: Dynamically resolves device safe area insets directly to child roots (`hud-root`, `root-container`), ensuring the top HUD bar clears notches and Dynamic Island.
- **HUD & Modal Controls**:
  - Heart icon life indicator (`TX_Heart_Fill.png` / `TX_Heart_Empty.png`).
  - Active Power-Up Badges: Dedicated top status pills for Shield (`SHIELD 10s`), Multi-Ball (`3 BALLS`), Wide Paddle (`10s` outward arrows), and Score Multipliers (`2X`–`5X` `10s` star icon) with live countdowns and tint themes.
  - Quick action bar: Pause/Play dynamic icon swap, Settings 360° compounding mechanical spin, Volume/Mute toggle with custom icons.
  - 2-row responsive wrapped tabs for all 7 levels (`LVL 1`–`LVL 4` top, `LVL 5`–`LVL 7` bottom) with $>80\text{px}$ touch targets.
  - Live interactive tuning sliders in Level Settings modal.
- **Audio System ([ArcadeAudioManager.cs](Assets/Scripts/Audio/ArcadeAudioManager.cs))**:
  - Persistent singleton wired with dedicated audio clips and fallback procedural synthesis for full offline safety.

---

## 8. Cross-Platform Build Configurations
- **iOS & macOS Xcode**:
  - Swift Xcode Project: `PlayerSettings.xcodeProjectType = XcodeProjectType.Swift`, targeting modern Swift lifecycle (`MainApp.swift`).
  - Build Profile: `Assets/Settings/Build Profiles/iOS.asset`.
  - Remote branch `develop`. macOS environment used for iOS device compilation.
- **WebGPU / WebGL**: Configured in `PlayerSettings` as primary graphics API.

---

## 9. Automated Test Suite
- **Location**: `Assets/Tests/BlockBreakerCoreTests.cs`
- **Total Tests**: **86 passing tests (100%)**, executing in ~130ms.
- **Coverage**: Global combo scoring multipliers (2x-5x), dynamic paddle deflection math, boundary clamping, life tracking, heart UI transitions, safe area insets, aspect-ratio frustum framing, compounding timed paddle widening & reversion, audio persistence, debris shader resolution, pause lifecycle, direct touch controls, bomb radius blast, glass 2-hit durability, shield countdown & killzone intercept, multi-ball death tolerance, 7-level campaign existence, speed escalation, cyclic advancement, HUD status badges, and 4x/5x block metadata.

# Project Context & Architecture: BlockBreaker

This file provides persistent, high-density project context across agent sessions for this Unity project.

---

## 1. Project Overview & Identity
- **Product Name**: `BlockBreaker`
- **Company Name**: `RaminRasulzade`
- **Application / Bundle Identifier**: `com.RaminRasulzade.BlockBreaker` (iOS, Standalone, Android)
- **Engine Version**: Unity 6 (6000.6.0f1)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Play Mode Start Scene**: `Assets/Scenes/LV_BlockBreaker_MainMenu.unity` (configured via [PlayModeSceneSetup.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Editor/PlayModeSceneSetup.cs))
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
- **Presets**: `PR_` prefix in `Assets/Presets/` managed by `ProjectSettings/PresetManager.asset` and [SetupAssetPresets.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Editor/SetupAssetPresets.cs).

---

## 3. Core Gameplay Architecture
- **Inverted Stepped Pyramid Paddle & Dynamic Deflection**:
  - **3-Tier Geometry**:
    - Tier 1 (Top Strike Deck): $100\%$ width ($W = 5.0$), ultra-thin profile ($H = 0.24$, $Z = 1.0$) with glowing neon cyan rim ([MI_Paddle_Deck.mat](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Materials/BlockBreaker/MI_Paddle_Deck.mat)). Top surface preserved exactly at $Y = -6.0$ for consistent ball docking.
    - Tier 2 (Mid Chassis): Stepped inward to $72\%$ width ($W = 3.6$), height $H = 0.20$, $Z = 0.88$ in dark brushed titanium ([MI_Paddle.mat](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Materials/BlockBreaker/MI_Paddle.mat)).
    - Tier 3 (Bottom Keel / Thrusters): Stepped inward to $44\%$ width ($W = 2.2$), height $H = 0.16$, $Z = 0.72$ with engine vent glow ([MI_Paddle_Core.mat](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Materials/BlockBreaker/MI_Paddle_Core.mat)).
    - Total combined vertical profile: $0.60$ (reduced from legacy $1.0$ cube), with lower sides stepped inward by up to $1.4$ units on each side to eliminate phantom side catches.
  - **Physical Strike Collider & Contact Normal Guard**:
    - Primary `BoxCollider` is strictly fitted to Tier 1 ($H = 0.24$, center $Y = +0.38$). Below $Y = -6.24$, there is zero collision volume.
    - Upward Normal Threshold: `BallController.IsValidPaddleBounceNormal(normal)` (`normal.y >= 0.25f`) guarantees that balls brushing the side wall or hitting from below are NOT deflected upward, falling cleanly into the killzone.
  - **Deflection Formula**: bounce angle $\theta = 90^\circ - (\text{normalizedOffset} \times 60^\circ)$.
  - **Compounding Expansion with Spring Overshoot**:
    - $+10\%$ per expander ($W_n = W_{prev} \times 1.10$, clamped to max $12.0$, adaptive bounds $[-10 + \frac{W}{2}, 10 - \frac{W}{2}]$).
    - Features spring-damper overshoot animation ($\approx +16\%$ overshoot with $Y$-axis squash-and-stretch settling over $0.35\text{s}$) for tactile arcade feedback.
- **Arena Dimensions**:
  - Top Wall: $Y = 24.25$, Left/Right Walls: $X = \pm 10.5$ (height $32.0$), Kill Zone: $Y = -9.0$ ([KillZone.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/KillZone.cs)).
  - Camera: Perspective $38^\circ$ FOV with [ResponsiveCameraController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/Core/ResponsiveCameraController.cs) dynamically adjusting $Z$-distance to guarantee 100% visible arena boundaries across any aspect ratio (16:9, 9:16, 9:19.5, etc.).
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
| **6** | **The High Roller** | **High Stakes & 3X Multiplier** | $9 \times 6$ | **54** | `1.28x` (Paddle 5.0) | • 1x 3X (90 pts on Blue!), 2x 2X, 1x Heart, 1x Shield, 1x Multi-Ball, 2x Bombs, 3x Glass |
| **7** | **Chaos Gauntlet** | **The Grand Climax** | $10 \times 9$ | **90** | `1.38x` (Paddle 5.0) | • 2x 3X, 2x 2X, 2x Expanders, 3x Bombs, 4x Glass, 1x Heart, 2x Shields, 2x Multi-Balls (`Randomized`) |

- **Asset Storage**: `Assets/Settings/Levels/SO_Level_01.asset` through `SO_Level_07.asset`.
- **Automation**: [SetupBlockBreakerScenes.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Editor/SetupBlockBreakerScenes.cs) configures and wires all 7 presets into both scenes.

---

## 5. Powerups & Special Brick Archetypes
- **Collectible Powerup Drops ([PowerupCapsule.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/PowerupCapsule.cs))**:
  - Tactical powerups drop tumbling 3D collectible capsules ([MI_Powerup_Capsule.mat](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Materials/BlockBreaker/MI_Powerup_Capsule.mat)) that fall at $4.5\text{ units/s}$ with 3D rotational spin and vibrant neon emissive tint. Must be intercepted by the paddle to claim:
    - **Paddle Expander (`PaddleExpander`)**: Drops neon cyan capsule; catching triggers spring overshoot expansion ($+10\%$).
    - **Extra Heart (`ExtraHeart`)**: Drops radiant neon pink capsule; catching grants $+1$ life (up to 5 max) with HUD parabolic flight animation.
    - **Shield (`Shield`)**: Drops electric blue capsule; catching activates 10-second defensive barrier with HUD countdown.
    - **Multi-Ball (`MultiBall`)**: Drops neon magenta capsule; catching spawns 2 extra balls at $\pm 35^\circ$ diverging angles with distinct trail colors.
- **Immediate Environmental / Scoring Modifiers**:
  - **Bomb Bricks (`Bomb`)**: Explosive radius detonation ($2.5$ units) immediately detonating surrounding bricks with outward impulses. Protected by `isDestroyed` flag against recursive loops.
  - **Score Multipliers (`ScoreMultiplier2x`, `ScoreMultiplier3x`)**: Immediately multiplies brick point value (e.g. 3x Blue = 90 pts).
  - **Glass-Enclosed Bricks (`GlassEnclosed`)**: Encased in a $1.18\times$ glass shell ([MI_Block_Glass.mat](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Materials/BlockBreaker/MI_Block_Glass.mat)). Requires 2 hits (Hit 1: shatters glass shell with crystal debris; Hit 2: breaks brick for $2\times$ points).
- **World Space Badges ([BlockBadge.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/BlockBadge.cs))**: Rendered via Unity 6 `PanelRenderer` in `WorldSpace` mode (`80px` fixed dimension, 100 PPU, clamped margins).

---

## 6. Graphics, VFX & Performance Architecture
- **VFX System**: `Assets/VFX/VFX_BlockShatter.vfx` + physical 3D debris fragments.
- **Cross-Platform Frame-0 Prewarming ([BlockVFXManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/BlockVFXManager.cs))**:
  - Primes shaders and dispatches off-camera VFX simulation during `Start()` while in `ReadyToLaunch`. Forces Apple Metal, DX12, Vulkan, and WebGPU drivers to compile compute/raster PSOs upfront, eliminating first-hit hitching.
  - Zero-allocation `MaterialPropertyBlock` tinting: preserves 100% SRP Batcher compatibility without material cloning.
  - Struct-based simulation lists (`ActiveDebris`, `ActiveVFX`) replacing per-brick coroutines.
- **iOS Debris Material**: Custom Universal Render Pipeline shader `Assets/Shaders/VFX_BlockDebris.shader` (`Arcade/VFX_BlockDebris`) preventing uncompiled pink shaders on Apple Metal.

---

## 7. UI Toolkit & Audio Systems
- **PanelRenderer Migration & Runtime Binding**:
  - Native Unity 6 `PanelRenderer` on `UI_HUD` and `UI_MainMenu`.
  - Due to `RegisterUIReloadCallback` firing only during editor asset reloads and not standard runtime startup, `ArcadeUIManager`, `MainMenuUIManager`, and `SafeAreaController` implement `EnsureInitialized()` with fallback reflection (`rootVisualElement` / `containerPanel.visualTree`), guaranteeing 100% reliable frame-0 UI binding.
  - Script Execution Order: `[DefaultExecutionOrder(-100)]` on `ArcadeGameManager` and `[DefaultExecutionOrder(-90)]` on `ArcadeInputHandler` guarantees manager singletons and event broadcasters are initialized before scene entities (`BallController`, `PaddleController`).
  - Pointer Event Pass-Through: Floating HUD overlays (e.g. `launch-banner`) use `picking-mode="Ignore"` and `pointer-events: none` to prevent blocking touch and click events to underlying controls.
- **Safe Area Controller ([SafeAreaController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/UI/SafeAreaController.cs))**: Dynamically resolves device safe area insets directly to child roots (`hud-root`, `root-container`), ensuring the top HUD bar clears notches and Dynamic Island.
- **HUD & Modal Controls**:
  - Detached floating frosted glass pods: Hearts/Lives left-aligned, Current Score centered (single readout, clean typography), Minimal Quick Action pod right-aligned (Mute, Settings, Pause).
  - High Scores Leaderboard: Dedicated top 10 scores modal with rank 1 (Gold), rank 2 (Silver), and rank 3 (Bronze) medal icons, formatted dates, and full reset capability in both Main Menu and Pause Menu ([HighScoreManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/Core/HighScoreManager.cs)).
  - How to Play: Quick-reference card-based gameplay guide in Main Menu and Pause Menu covering controls, dynamic deflection, block tier scoring, glass 2-hit durability, and all powerups.
  - Interactive Credits: Dedicated modal citing Unity MCP, Gemini, Antigravity, and Ramin Rasulzade, with clickable URL buttons routing to Email (`mailto:`), personal Website, LinkedIn, and GitHub via `Application.OpenURL`.
  - Scaled UI & Ergonomic Touch Targets: +10%–15% size boost across all HUD pods, badges, and modals; touch targets $\ge 54\text{px}$–$64\text{px}$ with generous spacing tailored for high-density iPhone displays ($2778 \times 1284$).
  - Typography: `FT_Montserrat` for titles, headers, and score readout; `FT_Inter` for body copy, badges, and buttons.
  - Launch Suppression: Touch release / click suppression on modal dismissal and unpause prevents balls docked in `ReadyToLaunch` from auto-launching.
  - Heart icon life indicator (`TX_Heart_Fill.png` / `TX_Heart_Empty.png`).
  - Quick action bar: Pause/Play dynamic icon swap, Settings 360° compounding mechanical spin, Volume/Mute toggle with custom icons.
  - 2-row responsive wrapped tabs for all 7 levels (`LVL 1`–`LVL 4` top, `LVL 5`–`LVL 7` bottom) with $>80\text{px}$ touch targets.
  - Live interactive tuning sliders in Level Settings modal.
- **Audio System ([ArcadeAudioManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/Audio/ArcadeAudioManager.cs))**:
  - Persistent singleton wired with dedicated audio clips and fallback procedural synthesis for full offline safety.

---

## 8. Cross-Platform Build Configurations
- **iOS & macOS Xcode**:
  - Swift Xcode Project: `PlayerSettings.xcodeProjectType = XcodeProjectType.Swift`, targeting modern Swift lifecycle (`MainApp.swift`).
  - Build Profile: `Assets/Settings/Build Profiles/iOS.asset`.
  - Remote branch `develop`. macOS environment used for iOS device compilation.
- **WebGPU / WebGL**: Configured in `PlayerSettings` as primary graphics API.

---

## 9. Ball Trail & Visual Effects Architecture
- **Dual-Layer Stacked Trail ([BallTrail.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/BallTrail.cs))**:
  - Layer 1 (Outer Halo): Wider tapering ribbon ($0.55$ start width, $0.22\text{s}$ lifetime, $40\%$ alpha) in rich base hue.
  - Layer 2 (Inner Core): Narrower, high-opacity hot core ($0.25$ start width, $0.16\text{s}$ lifetime, $85\%$ alpha) with $+65\%$ white blend for intense luminous depth.
  - Custom quadratic decay curves smoothly taper width to zero without harsh terminal truncation.
  - Zero-allocation `MaterialPropertyBlock` syncs the ball mesh base color and emissive glow to the trail hue.
  - Multi-Ball Differentiation: Primary ball emits Electric Cyan (`#00F2FF`); secondary multi-balls receive distinct high-contrast neon tints (Magenta `#FF0099`, Gold `#FFCC00`, Neon Lime `#00FF66`).
  - Emitting state is tightly coupled to ball lifecycle: clears and silences when docked in `ReadyToLaunch` or paused, emits during play.

---

## 10. Automated Test Suite
- **Location**: `Assets/Tests/BlockBreakerCoreTests.cs`
- **Total Tests**: **105 passing tests (100%)**, executing in ~160ms.
- **Coverage**: Scoring multipliers, dynamic paddle deflection math, boundary clamping, life tracking, heart UI transitions, safe area insets, aspect-ratio frustum framing, compounding paddle widening, stepped pyramid geometry & tier ratios, spring overshoot expansion animation, powerup capsule collection (PaddleExpander, ExtraHeart, Shield), contact normal validation (`IsValidPaddleBounceNormal`), audio persistence, debris shader resolution, pause lifecycle, launch suppression window, direct touch controls, bomb radius blast, glass 2-hit durability, shield countdown & killzone intercept, multi-ball death tolerance, multi-ball distinct trail color assignment, dual-layer trail creation and curve decay, 7-level campaign existence, speed escalation, cyclic advancement, HighScoreManager sorting/clamping/resetting, and in-game/menu modal visibility states.

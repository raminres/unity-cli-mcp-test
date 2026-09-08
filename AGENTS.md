# Project Context & Memory: unity-cli-mcp-test

This file provides persistent context across agent sessions for this Unity project.

---

## Project Overview
- **Product Name**: `BlockBreaker`
- **Company Name**: `RaminRasulzade`
- **Application / Bundle Identifier**: `com.RaminRasulzade.BlockBreaker` (iOS, Standalone, Android)
- **Engine Version**: Unity 6 (6000.6.0f1)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Active Scene**: `Assets/Scenes/LV_BlockBreaker_MainMenu.unity` (Play Mode Start Scene)
- **Remote Repository**: `https://github.com/raminres/unity-cli-mcp-test.git`
- **Active Branch**: `feature/gameplay-improvements` (Git LFS enabled)

---

## Session History & Changes Made

### 1. Scene Object: `Moving_Object_01`
- **Type**: Cube primitive located at the root of the hierarchy.
- **Initial Transform**: Position `(10, 0, 0)`, Rotation `(45, 0, 0)`, Scale `(1, 1, 1)`.
- **Components**:
  - `Transform`
  - `MeshFilter` (Cube)
  - `BoxCollider`
  - `MeshRenderer` (Material: `MI_Moving_Object_01`)
  - `Animator` (Controller: `Moving_Object_01_Controller`)

### 2. Animation System
- **Animation Clip**: `Assets/Animations/Moving_Object_01_Anim.anim`
  - **Length**: 2.0 seconds, 60 FPS, looping enabled (`loop = true`).
  - **Position X Oscillation**:
    - $t = 0.0s$: `10.0`
    - $t = 1.0s$: `-10.0`
    - $t = 2.0s$: `10.0`
    - Flat tangents ($0.0$) at turnaround points for harmonic ease-in / ease-out.
  - **Rotation X Oscillation**:
    - $t = 0.0s$: `45.0°`
    - $t = 1.0s$: `-45.0°`
    - $t = 2.0s$: `45.0°`
    - Flat tangents ($0.0$) at turnaround points.
  - Channels $Y$ and $Z$ locked at $0$.
- **Animator Controller**: `Assets/Animations/Moving_Object_01_Controller.controller`
  - State: `MoveAndRotate` assigned to `Moving_Object_01_Anim.anim` as default state.
  - Assigned to `Moving_Object_01`'s `Animator` component.

### 3. Material System & Naming Convention
- **Conventions**:
  - `MT_` prefix for Master Materials.
  - `MI_` prefix for Material Instances / Variants.
- **Master Material**: `Assets/Materials/MT_Master_PBR_URP.mat`
  - Shader: `Universal Render Pipeline/Lit`
- **Material Instance (Variant)**: `Assets/Materials/MI_Moving_Object_01.mat`
  - Parent: `MT_Master_PBR_URP`
  - Base Color (Albedo): Dark Green `RGBA(0.05, 0.35, 0.08, 1.0)`
  - Smoothness: `0.5`
  - Metallic: `0.0`
  - Assigned to `Moving_Object_01` (`MeshRenderer.materials[0]`).

### 4. Studio Lighting & Environment
- **Original Directional Light**: Deleted.
- **Key Light**: Directional Light (`Key Light`)
  - Color: Warm Yellow `RGBA(1.0, 0.88, 0.45, 1.0)`
  - Intensity: `1.6`
  - Rotation: `(45°, -35°, 0°)`
  - Shadows: Soft Shadows (`LightShadows.Soft`)
- **Fill Light**: Directional Light (`Fill Light`)
  - Color: Cool Blue `RGBA(0.35, 0.65, 1.0, 1.0)`
  - Intensity: `0.8`
  - Rotation: `(30°, 140°, 0°)`
  - Shadows: None
- **Skybox & Background**:
  - Skybox removed (`RenderSettings.skybox = null`).
  - Ambient mode set to Flat with dark ambient light `RGBA(0.04, 0.04, 0.06, 1.0)`.
  - `Main Camera` background cleared with Solid Color `RGBA(0.08, 0.08, 0.12, 1.0)`.
- **Fog**:
  - `RenderSettings.fog = true`, Exponential mode.
  - Color: `RGBA(0.08, 0.08, 0.12, 1.0)`, Density: `0.035`.
- **Post-Processing (Bloom)**:
  - Tuned on `Assets/Settings/SampleSceneProfile.asset`:
    - Intensity: `1.2`
    - Threshold: `0.85`
    - Scatter: `0.7`

### 5. Cinemachine Camera Setup
- **Package Installed**: `com.unity.cinemachine` (v6.6.0 / Cinemachine 3.x).
- **Brain**: `CinemachineBrain` component added to `Main Camera`.
- **Virtual Camera**: `CinemachineCamera` GameObject:
  - Position: `(0, 1, -10)`
  - Component: `CinemachineCamera` with `LookAt = Moving_Object_01.transform`.
  - Component: `CinemachineRotationComposer` with damping `(0.5, 0.5, 0.5)` for smooth dynamic tracking of the moving cube.

### 6. Technical Art Asset Conventions & Preset System
- **Folder Structure**:
  - `Assets/Textures/`: Project textures adhering to `TX_` conventions.
  - `Assets/Models/`: 3D meshes adhering to `SM_` and `SK_` conventions.
  - `Assets/Presets/`: Reusable Unity `.preset` assets using the `PR_` prefix (`PR_BaseColor.preset`, `PR_Normal.preset`, `PR_MetallicSmoothness.preset`, `PR_AO.preset`, `PR_Emissive.preset`, `PR_StaticMesh.preset`, `PR_SkeletalMesh.preset`, `PR_Audio.preset`) with PC (`Standalone`) and iOS (`iPhone`) platform overrides.
- **Naming Conventions**:
  - **Presets**: `PR_` prefix for all `.preset` files regardless of asset target.
  - **Audio**: `AU_` prefix (e.g. `AU_Explosion_01.wav`).
  - **Static Meshes**: `SM_` prefix (e.g. `SM_Rock_01.fbx`).
  - **Skeletal Meshes**: `SK_` prefix (e.g. `SK_Character_01.fbx`).
  - **Textures**: `TX_` prefix with mandatory semantic suffixes:
    - `_BaseColor`: sRGB color map (BC7 on PC, ASTC 6x6 on iOS).
    - `_MetallicSmoothness`: Linear mask map (BC7 on PC, ASTC 6x6 on iOS).
    - `_Normal`: Normal map (BC5 on PC, ASTC 6x6 on iOS).
    - `_AO`: Linear ambient occlusion map (BC7 on PC, ASTC 6x6 on iOS).
    - `_Emissive`: sRGB emissive color map (BC7 on PC, ASTC 6x6 on iOS).
- **Preset Manager Automation**:
  - Configured in `ProjectSettings/PresetManager.asset` with glob patterns:
    - `TextureImporter`:
      - `glob:"*_Normal*"` -> `PR_Normal.preset`
      - `glob:"*_MetallicSmoothness*"` -> `PR_MetallicSmoothness.preset`
      - `glob:"*_AO*"` -> `PR_AO.preset`
      - `glob:"*_Emissive*"` -> `PR_Emissive.preset`
      - `glob:"*BaseColor*"` -> `PR_BaseColor.preset`
      - `glob:"*TX_*"` -> `PR_BaseColor.preset`
    - `ModelImporter`:
      - `glob:"*SM_*"` -> `PR_StaticMesh.preset`
      - `glob:"*SK_*"` -> `PR_SkeletalMesh.preset`
    - `AudioImporter`:
      - `glob:"*AU_*"` -> `PR_Audio.preset`
  - Automation tool: `Assets/Editor/SetupAssetPresets.cs` (Menu item: `Tools/TechArt/Generate Asset Presets`).

### 7. Engine Packages Installed
- `com.unity.performance.profile-analyzer` (1.4.0) - Profiling analysis and frame comparison.
- `com.unity.project-auditor` (2.0.0) - Static analysis and project auditing.
- `com.unity.2d.sprite` (1.0.0) - 2D Sprite management and atlasing.
- `com.unity.memoryprofiler` (1.1.12) - Deep memory snapshot inspection.
- `com.unity.localization` (1.5.13) - Multi-language and asset localization.
- `com.unity.visualeffectgraph` (17.6.0) - GPU particle simulation and VFX.

---

### 8. Block Breaker Game & Arcade Testbed Architecture
- **Scenes (`LV_` Prefix)**:
  - `Assets/Scenes/LV_BlockBreaker_MainMenu.unity`: Fast-loading start scene with New Game, Continue, Settings, and Credits modals.
  - `Assets/Scenes/LV_BlockBreaker.unity`: Primary 3D arcade gameplay scene.
  - Both scenes registered in `EditorBuildSettings.scenes`.
- **Camera Perspective & Responsive Framing**:
  - Front-facing perspective camera ($38^\circ$ FOV) with [ResponsiveCameraController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/Core/ResponsiveCameraController.cs) dynamically adjusting $Z$-distance based on device aspect ratio (e.g. $Z \approx -36.3$ for 16:9 widescreen, $Z \approx -77.1$ for narrow 9:19.5 iPhone portrait), guaranteeing all boundaries and the ball remain 100% visible on screen without edge clipping.
- **Platform Targets**:
  - PC (Keyboard: Left/Right/A/D to move, Up/Space to launch, Esc to pause).
  - iOS / Mobile Touch (Horizontal drag to slide, tap/swipe up to launch, [SafeAreaController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/UI/SafeAreaController.cs) injecting dynamic breathing padding for notches/Dynamic Island).
  - WebGPU (Configured in `PlayerSettings` as primary graphics API for WebGL platform).
- **Gameplay Math & Scoring**:
  - Platform: 5:1 ratio ($5.0 \times 1.0 \times 1.0$), initial position $Y = -6.5$.
  - Taller Vertical Arena: Top Wall at $Y = 24.25$, Left/Right Walls height $32.0$ ($Y \in [-7.5, 24.5]$), Kill Zone at $Y = -9.0$.
  - Blocks: 1:1 ratio ($1.0 \times 1.0 \times 1.0$ cubes), 6 rows $\times$ 8 cols = 48 blocks with `startCenterY = 15.5`.
  - Point Multipliers: Red = 10 pts, Green = 20 pts, Blue = 30 pts.
  - Paddle Deflection: Dynamic bounce angle based on normalized impact offset $\theta = 90^\circ - (\text{offset} \times 60^\circ)$.
  - Lives: 3 starting lives, trigger volume using dedicated [KillZone.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/KillZone.cs) component to eliminate `CompareTag` dependency.
- **Editor Play Mode Routing**:
  - [PlayModeSceneSetup.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Editor/PlayModeSceneSetup.cs) binds `EditorSceneManager.playModeStartScene` to `LV_BlockBreaker_MainMenu.unity`, ensuring pressing Play in the Unity Editor always launches from the Main Menu.
- **VFX & Materials**:
  - `Assets/VFX/VFX_BlockShatter.vfx`: Visual Effect Graph sub-box burst with dynamic HDR color and collision normal bias.
  - Dual-layer burst: GPU particle burst + 8 physical 3D mini-cube fragments ($2 \times 2 \times 2$ sub-box explosion) with gravity damping and rotation.
  - Material variants in `Assets/Materials/BlockBreaker/`: `MI_Paddle`, `MI_Ball`, `MI_Block_Red`, `MI_Block_Green`, `MI_Block_Blue`, `MI_Playfield_Border` (all deriving from `MT_Master_PBR_URP.mat`).
- **Audio System (`AU_`)**:
  - `ArcadeAudioManager.cs`: Persistent singleton with procedural wave synthesis for immediate feedback (`AU_PaddleBounce`, `AU_WallBounce`, `AU_BlockHit_Red/Green/Blue`, `AU_LifeLost`, `AU_LevelClear`, `AU_GameOver`) with volume and mute persistence.
- **UI Toolkit & Unity 6 PanelRenderer Migration**:
  - Migrated from deprecated `UIDocument` to native Unity 6 `PanelRenderer` on `UI_HUD` ([LV_BlockBreaker.unity](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scenes/LV_BlockBreaker.unity)) and `UI_MainMenu` ([LV_BlockBreaker_MainMenu.unity](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scenes/LV_BlockBreaker_MainMenu.unity)).
  - [ArcadeUIManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/UI/ArcadeUIManager.cs), [MainMenuUIManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/UI/MainMenuUIManager.cs), and [SafeAreaController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/UI/SafeAreaController.cs) adopt `[RequireComponent(typeof(PanelRenderer))]` with version-resilient `RegisterUIReloadCallback` lifecycle binding.
  - [SetupBlockBreakerScenes.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Editor/SetupBlockBreakerScenes.cs) updated to generate `PanelRenderer` components.
  - **Safe Area Inset Fix**: [SafeAreaController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/UI/SafeAreaController.cs) automatically resolves and applies insets directly to child content roots (`hud-root`, `root-container`) with `extraTopPercent = 3.5%` and simulated 160px top inset, ensuring the in-game top bar stays completely outside and below the hardware notch/Dynamic Island.
  - **Typography & Bouncy Animations**:
    - Button fonts enlarged across Main Menu (`19px`) and HUD modals (`18px`), with quick control buttons increased to `20px` (46x46px touch target).
    - Added juicy `ease-out-back` hover pop overshoot (`scale: 1.05` to `1.15`, `translate: 0 -2px`) and tactile active compression (`scale: 0.86` to `0.92`, `translate: 0 2-3px`) paired with procedural audio clicks.
  - `ArcadePanelSettings.asset`: Reference resolution 1920x1080, Scale with Screen Size.
- **Automated Test Suite**:
  - `Assets/Tests/BlockBreakerCoreTests.cs`: 32 automated unit/integration tests validating score multipliers (2x, 3x), paddle deflection math, compounding paddle widening, boundary clamping, life tracking, game state transitions, safe area insets, multi-aspect ratio frustum framing, inverted row ordering, checkerboard alternating colors, randomized block dispersion, level advancement across configurations, and LevelConfiguration runtime cloning/clamping.

---

### 9. ScriptableObject Level Architecture & Gameplay Modifiers
- **ScriptableObject Data Models (`SO_` Prefix)**:
  - [LevelConfiguration.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/LevelConfiguration.cs): Defines grid layout (columns, rowsPerTier, horizontal/vertical spacing, startCenterY), color patterns (`BlockColorPattern`), gameplay balance (ball speed multiplier, initial paddle width), and special modifier counts (2x, 3x score multipliers, paddle expanders). Supports deep runtime cloning and boundary-clamped runtime tuning.
  - Assets in `Assets/Settings/Levels/`:
    - `SO_Level_01.asset`: "Level 1: Classic Inverted" (8 cols, 2 rows/tier = 6 rows, 1.0x speed, 1x 2X block, 1x expander block, `InvertedTiered`).
    - `SO_Level_02.asset`: "Level 2: Wide Checkerboard" (9 cols, 2 rows/tier = 6 rows, 1.2x speed, 2x 2X blocks, 1x 3X block, 1x expander block, `Checkerboard`).
    - `SO_Level_03.asset`: "Level 3: Chaos Gauntlet" (10 cols, 3 rows/tier = 9 rows, 1.35x speed, 2x 2X blocks, 2x 3X blocks, 2x expander blocks, `Randomized`).
- **Inverted Block Color Rows**:
  - [LevelGenerator.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/LevelGenerator.cs) inverts the block tier layout:
    - Top rows ($r < rowsPerTier$): Blue blocks (Tier 3, 30 pts, `matBlueBlock`).
    - Middle rows ($r < rowsPerTier \times 2$): Green blocks (Tier 2, 20 pts, `matGreenBlock`).
    - Bottom rows ($r \ge rowsPerTier \times 2$): Red blocks (Tier 1, 10 pts, `matRedBlock`).
- **Block Modifiers & World Space UI Toolkit**:
  - [BlockModifier.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/BlockModifier.cs): Defines `BlockSpecialType` (`Normal`, `ScoreMultiplier2x`, `ScoreMultiplier3x`, `PaddleExpander`).
  - [Block.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/Block.cs): Multiplies awarded score points on destroy (e.g. 2x: Blue 60, Green 40, Red 20; 3x: Blue 90, Green 60, Red 30).
  - [PaddleController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/PaddleController.cs): Implements compounding expansion ($W_n = W_{prev} \times 1.10$) with `expansionCount` tracking, maximum cap at 12.0f, and adaptive collision boundary clamping ($minX = -10.0 + \frac{W}{2}$, $maxX = 10.0 - \frac{W}{2}$).
  - [BlockBadge.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/BlockBadge.cs): Attached to special block faces using Unity 6 `PanelRenderer` in `PanelRenderMode.WorldSpace` mode (`Assets/UI/BlockWorldPanelSettings.asset`, `Assets/UI/BlockBadgeUI.uxml`, `Assets/UI/BlockBadgeUI.uss`) rendering glowing badges: "x2" in gold, "x3" in fiery neon red/magenta, and "+10%" in cyan.
- **Editable Level Settings Modal & Main Menu Level Select**:
  - `BlockBreakerHUD.uxml` & `BlockBreakerHUD.uss`: Adds wide level modal with Level 1/2/3 preset tabs, level descriptions, live sliders for Columns (4-14), Rows/Tier (1-4), Ball Speed (0.6-2.5x), 2X blocks (0-8), 3X blocks (0-8), Paddle Expanders (0-5), and an "APPLY & RESTART" button.
  - `MainMenuUI.uxml` & `MainMenuUI.uss`: Adds "SELECT LEVEL" button and level selection modal to directly launch into any configured level.
  - Decoupled state synchronization via [ArcadeUIManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/UI/ArcadeUIManager.cs) and [MainMenuUIManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/UI/MainMenuUIManager.cs).

---

### 10. Level Progression & Variations
- **Level Advancing Loop**:
  - [ArcadeGameManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/Core/ArcadeGameManager.cs) `AdvanceToNextLevel()` preserves cumulative score and remaining lives while setting game state to `ReadyToLaunch`.
  - [LevelGenerator.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/LevelGenerator.cs) `AdvanceToNextLevel()` selects and loads the next level configuration (Level 1 $\to$ 2 $\to$ 3 $\to$ 1 loop), resets the ball onto the paddle, and resets the paddle to the level's default width.
  - [ArcadeUIManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/UI/ArcadeUIManager.cs) binds the "NEXT LEVEL" button in the Victory / Level Cleared modal to advance to the next level seamlessly without reloading the entire scene.
- **Color Distribution Patterns**:
  - `BlockColorPattern.InvertedTiered`: Classic Blue top, Green middle, Red bottom.
  - `BlockColorPattern.Checkerboard`: Alternating formula $(r + c) \pmod 3$ generating diagonal geometric color waves.
  - `BlockColorPattern.Randomized`: Uniform random tier distribution across the grid.

---

### 11. Cross-Platform & macOS Xcode Build Configuration
- **Project Identity & Code Signing**:
  - **Product Name**: `BlockBreaker`
  - **Company Name**: `RaminRasulzade`
  - **Application / Bundle Identifier**: `com.RaminRasulzade.BlockBreaker` (Configured across iOS, Standalone, and Android in `ProjectSettings/ProjectSettings.asset`).
  - **Xcode Project Type**: `Swift` (`UnityEditor.XcodeProjectType.Swift`, `xcodeProjectType: 1` in Unity 6000.6.0f1), generating a modern Swift-based Xcode project structure instead of legacy Objective-C.
- **Multi-Machine Workflow (Windows PC $\leftrightarrow$ macOS)**:
  - Remote repository branch: `feature/gameplay-improvements`.
  - The macOS machine is used for iOS device test builds and Xcode compilation.
  - The macOS environment has a local stash containing Xcode build profile and test build customizations.
  - Both machines maintain `BlockBreaker`, `RaminRasulzade`, and Swift Xcode project type in version-controlled `PlayerSettings.asset`, ensuring clean pulls without stash conflicts.
  - When testing iOS builds on macOS, apply the stashed changes (`git stash apply`).

---

### 12. iOS Build Profile & Swift Xcode Project Export
- **Unity 6 Build Profile**:
  - Asset: `Assets/Settings/Build Profiles/iOS.asset`
  - Platform: `iOS` (`ad48d16a66894befa4d8181998c3cb09`)
  - Active: Set as active build profile via `EditorUserBuildSettings.activeBuildProfile`.
  - Override Global Scenes: `true`
  - Scenes Included:
    - `Assets/Scenes/LV_BlockBreaker_MainMenu.unity` (Build Index 0, enabled)
    - `Assets/Scenes/LV_BlockBreaker.unity` (Build Index 1, enabled)
- **Player Settings & Swift Xcode Project**:
  - Company Name: `RaminRasulzade`
  - Product Name: `BlockBreaker`
  - iOS Application Identifier (Bundle ID): `com.RaminRasulzade.BlockBreaker`
  - `PlayerSettings.xcodeProjectType = XcodeProjectType.Swift` (`xcodeProjectType: 1` in `ProjectSettings.asset`).
  - Target Minimum iOS Version: `26.0`.
  - Modern entry point generated in Swift (`MainApp/MainApp.swift` with SwiftUI lifecycle `@main struct MainApp: App`).
  - `UnityAPI` Swift module providing `UnityPlayer.swift`, `CrashReporter.swift`, `UnityEngineLoadState.swift`, and SwiftUI `UnityView`.
- **Exported Build Artifacts**:
  - Output Path: `/Users/raminrasulzade/Documents/UnityProjects/Builds/BlockBreakerBuilds`
  - Xcode Project: `BlockBreaker.xcodeproj`

---

## Active Scenes & Build Index
1. `Assets/Scenes/LV_BlockBreaker_MainMenu.unity` (Build Index 0)
2. `Assets/Scenes/LV_BlockBreaker.unity` (Build Index 1)
3. `Assets/Scenes/SampleScene.unity` (Disabled baseline)


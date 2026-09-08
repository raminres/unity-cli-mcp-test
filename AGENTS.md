# Project Context & Memory: unity-cli-mcp-test

This file provides persistent context across agent sessions for this Unity project.

---

## Project Overview
- **Engine Version**: Unity 6 (6000.6.0f1)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Active Scene**: `Assets/Scenes/SampleScene.unity`
- **Remote Repository**: `https://github.com/raminres/unity-cli-mcp-test.git`
- **Active Branch**: `tech-art/project-settings-01` (Git LFS enabled)

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
- **UI Toolkit**:
  - `MainMenuUI.uxml` / `.uss`: Glassmorphic cards, candy neon gradients, settings sliders, target FPS toggle.
  - `BlockBreakerHUD.uxml` / `.uss`: Floating top bar (Score, High Score, 3 Glowing Heart Pips, subtle top-right buttons for Mute, Options, Pause), Center Launch Banner, and Level Clear / Game Over modals.
  - `ArcadePanelSettings.asset`: Reference resolution 1920x1080, Scale with Screen Size.
- **Automated Test Suite**:
  - `Assets/Tests/BlockBreakerCoreTests.cs`: 17 automated unit/integration tests validating score multipliers, paddle deflection math, boundary clamping, life tracking, game state transitions, safe area insets, and multi-aspect ratio frustum framing.

---

## Active Scenes & Build Index
1. `Assets/Scenes/LV_BlockBreaker_MainMenu.unity` (Build Index 0)
2. `Assets/Scenes/LV_BlockBreaker.unity` (Build Index 1)
3. `Assets/Scenes/SampleScene.unity` (Disabled baseline)


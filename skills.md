# Agent Skills & Playbook: Unity URP & Cinemachine Automation

This document outlines the workflows, MCP tool patterns, conventions, and C# editor techniques established for this repository.

---

## 1. Project Specifications & Environment
- **Unity Version**: Unity 6 (6000.6.0f1)
- **Pipeline**: Universal Render Pipeline (URP)
- **Cinemachine Version**: 3.x (`com.unity.cinemachine@6.6.0`)
- **Key Assemblies**: `Unity.Cinemachine`, `UnityEngine.Rendering.Universal`, `UnityEngine.Rendering`

---

## 2. Material System Workflow
Follow strict studio naming conventions:
- **Master Materials**: Prefix `MT_` (e.g., `MT_Master_PBR_URP.mat`).
  - Created via `create_asset` or C# `new Material(Shader.Find("Universal Render Pipeline/Lit"))`.
- **Material Instances (Variants)**: Prefix `MI_` (e.g., `MI_Moving_Object_01.mat`).
  - In Unity 6 / URP, Material Variants are created by setting `materialInstance.parent = masterMaterial;`.
  - Property overrides (e.g., `_BaseColor`, `_Smoothness`, `_Metallic`) are applied to the instance while preserving inheritance from the master.
- **Assigning to MeshRenderer**:
  - Property path: `m_Materials.Array.data[0]` via `set_serialized_field`, or `meshRenderer.sharedMaterial = ...` via `eval`.

---

## 3. Animation & Animator Workflow
When generating procedural or keyframed animations:
1. **Clip Creation**:
   - Tool: `create_animation_clip` with `path`, `frameRate: 60`, `loop: true`.
2. **Curve Generation**:
   - Tool: `set_animation_curve`.
   - Property paths:
     - Position: `m_LocalPosition.x`, `m_LocalPosition.y`, `m_LocalPosition.z` (Component: `Transform`).
     - Rotation: `localEulerAnglesRaw.x`, `localEulerAnglesRaw.y`, `localEulerAnglesRaw.z` (Component: `Transform`).
   - **Looping & Tangents**:
     - Keyframe format: `[{"time": t, "value": v, "inTangent": 0, "outTangent": 0}]`.
     - Leaving tangents at `0` creates flat, smooth ease-in / ease-out transitions at extreme endpoints without velocity discontinuities.
3. **Controller & State Setup**:
   - Tool: `create_animator_controller` at `Assets/Animations/...`.
   - Tool: `add_animator_state` with `motion` referencing the `.anim` clip and `isDefault: true`.
   - Bind to GameObject's `Animator` component field `m_Controller`.

---

## 4. Lighting & Environment Configuration
1. **Three-Point / Studio Lighting**:
   - **Key Light**: Primary illumination angled ~45° from the side, warm tint (e.g. yellow `RGBA(1.0, 0.88, 0.45, 1.0)`), intensity `1.5 - 2.0`, soft shadows.
   - **Fill Light**: Soft shadow-filling light angled from the opposite side, cool tint (e.g. blue `RGBA(0.35, 0.65, 1.0, 1.0)`), intensity `0.6 - 0.8`, no shadows.
   - **Back/Rim Light** (optional): Angled from behind the subject to create silhouette separation.
2. **Atmosphere & Skybox Removal**:
   - `RenderSettings.skybox = null;`
   - `RenderSettings.ambientMode = AmbientMode.Flat;` with low ambient intensity.
   - Camera `clearFlags = CameraClearFlags.SolidColor` with dark backdrop (`#14141F`).
3. **Fog Settings**:
   - `RenderSettings.fog = true;`
   - `RenderSettings.fogMode = FogMode.Exponential;`
   - Matching fog color to the camera solid background color.
4. **URP Post-Processing (Volume)**:
   - Volume profile at `Assets/Settings/SampleSceneProfile.asset`.
   - Access overrides (`Bloom`, `Tonemapping`, `Vignette`) via `profile.TryGet<T>(out var component)`.
   - Always set `overrideState = true` on fields when modifying values.

---

## 5. Cinemachine 3.x Dynamic Camera Tracking
In Unity 6 / Cinemachine 3.x:
1. **Main Camera**:
   - Must have `Unity.Cinemachine.CinemachineBrain`.
2. **Virtual Camera GameObject**:
   - Component: `Unity.Cinemachine.CinemachineCamera`.
   - Set `vcam.LookAt = targetTransform;` to designate tracking target.
   - Set `vcam.Follow = targetTransform;` if following target position.
3. **Aim Behaviors**:
   - `Unity.Cinemachine.CinemachineRotationComposer`: Smooth damped rotation that keeps target centered.
     - Damping: `Vector3(x, y, z)` (e.g. `(0.5, 0.5, 0.5)` for cinematic smoothness).
   - `Unity.Cinemachine.CinemachineHardLookAt`: Rigid instantaneous look-at without damping.

---

## 6. MCP & Roslyn `eval` Best Practices
- **Standard MCP Tools**: Use `get_scene_hierarchy`, `create_gameobject`, `set_transform`, `set_serialized_field`, etc. for common CRUD operations.
- **Roslyn `eval`**: Use for complex operations, multi-step transactions, or editor APIs that lack direct MCP bindings (e.g. Material Variants, RenderSettings, Cinemachine target references).
- **Rules for `eval`**:
  - Always write valid C# statements terminated with semicolons.
  - Return a serializable result (e.g., `return "Success";` or `return someValue;`).
  - Call `Undo.RegisterCreatedObjectUndo(...)` or `Undo.RecordObject(...)` for undoability.
  - Call `EditorSceneManager.MarkSceneDirty(...)` and `EditorSceneManager.SaveOpenScenes()` or `AssetDatabase.SaveAssets()` to ensure changes persist to disk.

---

## 7. Asset Pipeline Conventions & Preset Manager Automation
1. **Asset Naming Conventions**:
   - **Audio**: `AU_` prefix (e.g., `AU_Laser_01.wav`).
   - **Static Meshes**: `SM_` prefix (e.g., `SM_Crate_01.fbx`).
   - **Skeletal Meshes**: `SK_` prefix (e.g., `SK_Boss_01.fbx`).
   - **Textures**: `TX_` prefix with semantic suffixes:
     - `_BaseColor`: sRGB color map.
     - `_MetallicSmoothness`: Linear mask map.
     - `_Normal`: Normal map.
     - `_AO`: Ambient occlusion mask.
     - `_Emissive`: Emissive color map.
2. **Platform Overrides Matrix**:
   - **PC (`Standalone`)**:
     - Textures: BC7 format (BC5 for Normal maps), Max Size 2048.
     - Audio: Vorbis compression, quality 0.7, compressed in memory.
   - **iOS (`iPhone`)**:
     - Textures: ASTC 6x6 compression, Max Size 2048.
     - Audio: AAC compression, quality 0.7, compressed in memory.
3. **Preset Manager Glob Patterns & Presets (`PR_` prefix)**:
   - All Unity Preset assets reside in `Assets/Presets/` and use the uniform `PR_` prefix.
   - `TextureImporter`:
     - `glob:"*_Normal*"` -> `PR_Normal.preset`
     - `glob:"*_MetallicSmoothness*"` -> `PR_MetallicSmoothness.preset`
     - `glob:"*_AO*"` -> `PR_AO.preset`
     - `glob:"*_Emissive*"` -> `PR_Emissive.preset`
     - `glob:"*BaseColor*"` -> `PR_BaseColor.preset`
     - `glob:"*UI/Icons/*"` -> `PR_Icon.preset`
     - `glob:"*Icons/*"` -> `PR_Icon.preset`
     - `glob:"*TX_*"` -> `PR_BaseColor.preset`
   - `ModelImporter`:
     - `glob:"*SM_*"` -> `PR_StaticMesh.preset`
     - `glob:"*SK_*"` -> `PR_SkeletalMesh.preset`
   - `AudioImporter`:
     - `glob:"*AU_*"` -> `PR_Audio.preset`

---

## 8. Block Breaker & Mobile Arcade Workflow
1. **Scene Conventions (`LV_` prefix)**:
   - `LV_BlockBreaker_MainMenu.unity`: Fast-loading start scene with New Game, Continue, Settings, and Credits.
   - `LV_BlockBreaker.unity`: Primary 3D arcade gameplay scene.
   - Registered in `EditorBuildSettings.scenes` with indices 0 and 1.
2. **Editor Play Mode Start Scene**:
   - `EditorSceneManager.playModeStartScene` bound to `LV_BlockBreaker_MainMenu.unity` via `Assets/Editor/PlayModeSceneSetup.cs`, ensuring clicking Play in the Editor always starts from the Main Menu.
3. **Mobile Safe Area & UI Toolkit**:
   - `Assets/Scripts/UI/SafeAreaController.cs`: Dynamically injects hardware safe area percentage padding + extra breathing room margins into `UIDocument` root visual elements.
   - Prevents HUD elements (score, lives, quick buttons) from clipping behind notches, Dynamic Island, or the iOS home indicator bar.
4. **Responsive Camera Framing**:
   - `Assets/Scripts/Core/ResponsiveCameraController.cs`: Dynamically recalculates 3D camera distance $Z$ to enclose both width and height within the frustum across all aspect ratios (e.g. 16:9 widescreen, 4:3 iPad, 9:19.5 iPhone portrait).
   - Eliminates off-screen ball bounces and edge boundary clipping.
5. **Gameplay Physics & Collision Rules**:
   - Platform: 5:1 ratio ($5.0 \times 1.0 \times 1.0$).
   - Blocks: 1:1 ratio ($1.0 \times 1.0 \times 1.0$ cubes).
   - Deflection formula: $\theta = 90^\circ - (\text{hitOffset} \times 60^\circ)$.
   - Kill Zone: Uses dedicated `KillZone.cs` component on trigger volume to eliminate `CompareTag` errors.
6. **Automated Testing Suite**:
   - Tests reside in `Assets/Tests/` under `Arcade.Tests.asmdef`.
   - Run via `Assets/Editor/RunBlockBreakerTests.cs` (Menu item: `Tools/Arcade/Run Block Breaker Tests`) or `unity test . --mode EditMode`.

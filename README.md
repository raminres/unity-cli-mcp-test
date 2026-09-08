# Unity CLI & Antigravity MCP Testbed (`unity-cli-mcp-test`)

[![Unity Version](https://img.shields.io/badge/Unity-6%20(6000.6.0f1)-black.svg?style=flat&logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP-blue.svg)](https://unity.com/srp/universal-render-pipeline)
[![Git LFS](https://img.shields.io/badge/Git-LFS%20Enabled-orange.svg)](https://git-lfs.github.com/)
[![AI Integration](https://img.shields.io/badge/AI%20Assistant-Google%20Antigravity-green.svg)]()

A research and testbed project exploring the synergy between **Google Antigravity**, **Unity MCP (Model Context Protocol)**, and the **Unity CLI**. 

The goal is to evaluate autonomous and pair-programming workflows for Unity development—including programmatic scene authoring, procedural animation, material instance hierarchies, studio lighting, post-processing calibration, and dynamic camera tracking through conversational AI.

---

## 🎯 Purpose & Scope

This project serves as a testing ground for:
- **Agentic Unity Editing**: Direct manipulation of Unity scenes, GameObjects, components, and serialized fields via Unity MCP tools and Roslyn `eval`.
- **Procedural Asset Creation**: Automating the creation of animation clips, animator controllers, and PBR materials from natural language instructions.
- **Persistent AI Context**: Utilizing Antigravity's customization system (`AGENTS.md`, `skills.md`, and `.agents/skills/`) to maintain seamless memory across agent sessions.
- **Production Asset Standards**: Enforcing studio naming conventions, Git LFS asset management, and modular rendering pipelines.

---

## 📁 Repository Structure

```text
 unity-cli-mcp-test/
 ├── .agents/
 │   └── skills/
 │       └── unity-scene-workflow/   # On-demand Antigravity skill definition
 │           └── SKILL.md
 ├── Assets/
 │   ├── Animations/                 # Generated Animation Clips & Animator Controllers
 │   │   ├── Moving_Object_01_Anim.anim
 │   │   └── Moving_Object_01_Controller.controller
 │   ├── Editor/                     # Editor automation tools & utilities
 │   │   └── SetupAssetPresets.cs
 │   ├── Materials/                  # Master Materials & Material Instances
 │   │   ├── MT_Master_PBR_URP.mat
 │   │   └── MI_Moving_Object_01.mat
 │   ├── Models/                     # 3D models adhering to SM_ and SK_ conventions
 │   ├── Presets/                    # Unity .preset assets (PR_ prefix) with PC/iOS overrides
 │   ├── Scenes/
 │   │   └── SampleScene.unity       # Active demonstration scene
 │   ├── Screenshots/                # Editor & in-game captures (tracked by Git LFS)
 │   ├── Settings/                   # URP configuration & Global Volume profiles
 │   └── Textures/                   # Textures adhering to TX_ conventions
 ├── Packages/                       # Package manifest & lockfiles
 ├── ProjectSettings/                # Project engine configuration & PresetManager
 ├── .gitattributes                  # Git LFS rules for Unity binary assets
 ├── .gitignore                      # Standard Unity gitignore (ignores Library, Temp, etc.)
 ├── AGENTS.md                       # Persistent workspace memory for AI agent sessions
 ├── README.md                       # Project overview & architectural guide
 └── skills.md                       # Agent playbook, MCP recipes, and conventions
 ```
 
 ---
 
 ## 📐 Project Rules & Conventions
 
 ### 1. Material System Naming & Hierarchy
 - **Master Materials (`MT_`)**: Base materials defining the shader and default parameter baseline (e.g., `MT_Master_PBR_URP.mat` using `Universal Render Pipeline/Lit`).
 - **Material Instances (`MI_`)**: Material Variants inheriting from a master material (`materialInstance.parent = masterMaterial`). Parameter overrides (such as base color, smoothness, and metallic) are applied while preserving the parent link.
 
 ### 2. Animation Guidelines
 - All procedural animation clips use a base sampling frame rate of **60 FPS** with `loop = true`.
 - Oscillation turnarounds at extrema ($t = 0.0s, 1.0s, 2.0s$) must use **flat tangents ($0.0$)** to guarantee continuous harmonic ease-in / ease-out motion without velocity jerking.
 
 ### 3. Technical Art Asset Conventions & Presets (`PR_`)
 - **Preset Assets (`PR_`)**: Presets in `Assets/Presets/` use the `PR_` prefix (`PR_BaseColor`, `PR_Normal`, `PR_MetallicSmoothness`, `PR_AO`, `PR_Emissive`, `PR_StaticMesh`, `PR_SkeletalMesh`, `PR_Audio`) and configure PC (`Standalone`) and iOS (`iPhone`) platform overrides.
 - **Asset Naming Conventions**:
   - Audio: `AU_` prefix (e.g. `AU_Laser_01.wav`).
   - Static Meshes: `SM_` prefix (e.g. `SM_Crate_01.fbx`).
   - Skeletal Meshes: `SK_` prefix (e.g. `SK_Character_01.fbx`).
   - Textures: `TX_` prefix with suffixes `_BaseColor`, `_MetallicSmoothness`, `_Normal`, `_AO`, `_Emissive`.
 - **Preset Manager Automation**: Configured in `ProjectSettings/PresetManager.asset` with glob patterns to automatically assign presets upon asset import.
 
 ### 4. Git & Asset Management
 - **Primary Branches**: `develop`, feature branches (e.g. `tech-art/project-settings-01`).
 - **Git LFS**: Track all binary assets (3D models, textures, audio, video, archives, native plugins) via `.gitattributes`.
 - **Unity Meta Files**: Every asset and folder meta file must be committed alongside its corresponding asset.
 - **Excluded Directories**: `Library/`, `Temp/`, `Logs/`, `UserSettings/`, and IDE-generated solutions (`*.csproj`, `*.sln`, `.vs/`) are strictly ignored.
 
 ### 5. Agent Customization Files
 - **`AGENTS.md`**: Automatically loaded by Antigravity upon entering this workspace. Acts as persistent memory for scene changes and configuration.
 - **`skills.md`**: Playbook detailing MCP tool workflows, serialized property paths, and Roslyn C# code patterns.
 - **`.agents/skills/unity-scene-workflow/SKILL.md`**: Official skill registered in Antigravity's progressive disclosure system.

---

## 🎬 Current Scene Demo (`SampleScene.unity`)

The active scene showcases an end-to-end procedural setup:

1. **`Moving_Object_01`**:
   - Cube primitive oscillating along the X axis between `+10` and `-10` while rotating on X between `+45°` and `-45°` over a seamless 2-second loop.
   - Textured with dark green PBR Material Variant `MI_Moving_Object_01` (Albedo `(0.05, 0.35, 0.08)`, Smoothness `0.5`, Metallic `0.0`).
2. **Studio Lighting**:
   - **Key Light**: Directional light with soft shadows, warm yellow tint (`RGB: 1.0, 0.88, 0.45`), intensity `1.6`.
   - **Fill Light**: Directional light, cool blue tint (`RGB: 0.35, 0.65, 1.0`), intensity `0.8`.
3. **Atmosphere & Post-Processing**:
   - Skybox removed; camera cleared to solid studio dark (`#14141F`).
   - Exponential atmospheric fog matching ambient background color.
   - Bloom enabled on `SampleSceneProfile.asset` (intensity `1.2`, threshold `0.85`, scatter `0.7`).
4. **Cinemachine Dynamic Tracking**:
   - `Main Camera` powered by `CinemachineBrain`.
   - `CinemachineCamera` with `CinemachineRotationComposer` actively tracking `Moving_Object_01` via `LookAt` with rotation damping (`0.5`).

---

## 🛠️ Requirements & Environment

- **Unity**: `6000.6.0f1` (Unity 6)
- **Render Pipeline**: Universal Render Pipeline (URP 17.6.0)
- **Cinemachine**: `com.unity.cinemachine` (6.6.0 / Cinemachine 3.x)
- **Git**: Git 2.50+ with **Git LFS** 3.x+
- **AI Agent**: Google Antigravity with Unity MCP Server

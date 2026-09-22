# BlockBreaker 3D

[![Unity Version](https://img.shields.io/badge/Unity-6%20(6000.6.0f1)-black.svg?style=flat&logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP-blue.svg)](https://unity.com/srp/universal-render-pipeline)
[![Automated Tests](https://img.shields.io/badge/Tests-263%2F263%20Passing%20(100%25)-brightgreen.svg)]()
[![Platforms](https://img.shields.io/badge/Platforms-iOS%20(Swift)%20%7C%20WebGPU%20%7C%20PC-purple.svg)]()
[![Git LFS](https://img.shields.io/badge/Git-LFS%20Enabled-orange.svg)](https://git-lfs.github.com/)
[![AI Integration](https://img.shields.io/badge/AI%20Assistant-Google%20Antigravity-green.svg)]()

**BlockBreaker 3D** is a physics-driven arcade brick breaker built with Unity 6 (`6000.6.0f1`) and the Universal Render Pipeline (URP). Featuring 3-tier stepped pyramid paddle geometry with kinetic active strikes, Model A tactile brick physicals (dampen, boost, scatter), anti-trap trajectory guards, a physical shield wall with anti-trap passthrough, cascading bomb chains, and an escalating 15-level campaign with powerups and tumbling hazard debuffs.

Acts as a production testbed for **Google Antigravity**, **Unity MCP (Model Context Protocol)**, and **Unity CLI** agentic workflows.

---

## 🎮 Highlights & Mechanics

- **Inverted Stepped Pyramid Paddle**: 3-tier structure ($0.60$ total vertical profile) with strike deck ($W = 5.0$), mid chassis ($W = 3.6$), and keel thrusters ($W = 2.2$). Low profile and inward stepping eliminate side clipping.
- **Dynamic Deflection & Kinetic Strikes**: Paddle velocity imparts tangential steer ($\pm 45^\circ$), allowing counter-swipes (reversal cuts). Moving paddle strikes ($|V_x| \ge 3.5\text{ u/s}$) trigger a $+8\%$ kinetic speed pop, high-pitch audio ($1.22\times$), and cyan spark bursts.
- **Anti-Trap Trajectory Guards**: Mathematical vertical floor ($|v_y| \ge v \cdot \sin(20^\circ)$), consecutive wall steepening ($\ge 35^\circ$), vertical deadzone exclusion ($[85^\circ, 95^\circ]$), and $45^\circ$ top corner chamfers eliminate trajectory loops.
- **Model A Tactile Brick Behaviors**:
  - 🔴 **Red**: Kinetic dampener ($-1.2\text{ u/s}$, floored at $14\text{ u/s}$).
  - 🟢 **Green**: Kinetic turbo ($+10\%$ speed impulse, capped at $22\text{ u/s}$).
  - 🔵 **Blue**: Optical scatter ($\pm 18^\circ$ to $\pm 28^\circ$ exit refraction).
- **Physical Shield Wall**: Deployed at arena bottom ($Y = -7.6\text{f}$) with tween scale overshoot. Bounces balls upward ($\ge 35^\circ$) and features anti-trap one-way upward passthrough.
- **Clutch Hyper-Beam Railgun**: When 1 brick remains, a 12s countdown ($10\times \to 1\times$) begins. If time expires, an emergency vertical hyper-beam surges from paddle to ceiling, clearing the stage.
- **Hybrid Scoring**: Unreturned volley combo multipliers ($1\times \to 5\times$), multi-ball score multipliers ($2\times/3\times$), compound bomb cascades, and speed/flawless scorecard bonuses.
- **Bilingual Localization & Options Parity**: Unity Localization Tables (`com.unity.localization`) support English and Turkish with instant runtime switching. Full UI parity between Main Menu and In-Game Pause options, featuring custom animated checkmark mute toggles with cyan/crimson audio feedback.

---

## ⚡ Pickups & Hazards

| Type | Appearance | Effects |
| :--- | :--- | :--- |
| **Expander** | Cyan Capsule | Widens paddle $+10\%$ compounding (max $12.0\text{u}$, 10s). |
| **Score Multiplier** | Cyan Capsule | Multiplies block breaks by $2\times, 3\times, 4\times,$ or $5\times$ (10s). |
| **Physical Shield** | Cyan Capsule | Deploys defensive energy barrier with anti-trap passthrough (10s). |
| **Multi-Ball** | Cyan Capsule | Spawns 2 extra balls at $\pm 35^\circ$; scales all scored points. |
| **Extra Heart** | Cyan Capsule | Awards $+1$ life (up to 5 maximum). |
| **Laser Blaster** | Cyan Capsule | Mounts twin cannons firing ruby bolts ($34\text{ u/s}$) for 10s. |
| **Paddle Shortener** | Crimson Diamond | Shrinks paddle width $-18\%$ (min $2.4\text{u}$, 10s). |
| **Paddle Slower** | Crimson Diamond | Induces input inertia and reduces keyboard speed by $-50\%$ (8s). |
| **Brick Freezer** | Crimson Diamond | Freezes up to 5 bricks; each absorbs 1 defrost hit before shattering. |
| **Ball Shrinker** | Crimson Diamond | Reduces ball radius to $60\%$ ($0.8\text{u} \to 0.48\text{u}$, 10s). |
| **Ball Slower** | Crimson Diamond | Lowers ball velocity to $9.5\text{ u/s}$ (8s). |
| **Paddle Freezer** | Crimson Diamond | Freezes paddle for $1.2\text{s}$; input triggers a visual struggle tremor. |
| **Environmental** | Grid Bricks | Armored 2-hit Glass Bricks and explosive Bomb Bricks ($2.5\text{u}$ radius). |

---

## 🕹️ 15-Level Campaign

The campaign features 15 ScriptableObject levels (`SO_Level_01` through `SO_Level_15`) with high-density grids (11–14 columns, up to $17.5\text{u}$ span) and outer bumper flanks to eliminate empty side highways. Ball speed scales from $0.92\times$ to $1.48\times$, paddle width narrows from $5.5\text{u}$ to $4.5\text{u}$, and hazards escalate progressively from 0 up to 13.

*For full level breakdowns, archetypes, and par times, refer to [GDD.md](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/GDD.md).*

---

## 🎯 Controls

| Platform | Action | Input |
| :--- | :--- | :--- |
| **Desktop** | Move Paddle | Mouse Movement (1:1 tracking) or `A` / `D` / Arrow Keys |
| | Launch Ball | Left Mouse Button or `Space` |
| | Pause / Options | `Esc` or UI Action Bar |
| **Mobile / Touch** | Move Paddle | Horizontal Finger Drag anywhere on screen |
| | Launch Ball | Tap screen when docked |
| | UI Interaction | Touch buttons (ergonomic touch targets $\ge 54\text{px}$) |

---

## 🏗️ Technical Architecture & Environment

- **Unity**: `6000.6.0f1` (Universal Render Pipeline `URP 17.6.0`)
- **Start Scene**: `Assets/Scenes/LV_BlockBreaker_MainMenu.unity` (Build Index 0, transparent UI Toolkit overlay over 3D cosmic background, matched $38^\circ$ FOV and camera distance)
- **Gameplay Scene**: `Assets/Scenes/LV_BlockBreaker.unity` (Build Index 1)
- **Modular Prefab Architecture**: 100% prefab-driven (`Assets/Prefabs/`), eliminating all runtime procedural primitives (`GameObject.CreatePrimitive`):
  - `Arena/`: `PF_Walls` (side/top walls, 45° chamfers, kill zone), `PF_Background` (cosmic gradient quad + `LevelBackgroundController`), `PF_ShieldWall` (decoupled physics, visual model, electric cyan particle barrier).
  - `Paddle/`: `PF_Paddle` (3-tier stepped hierarchy, twin blaster cannons, laser railgun aperture, frost shell, hit sparks).
  - `Balls/`: `PF_Ball_Standard` (decoupled visual sphere, TrailRenderer, and BallController).
  - `Blocks/`: `PF_Block_Base`, `PF_Block_Red`, `PF_Block_Green`, `PF_Block_Blue`, `PF_Block_Bomb`, `PF_Block_Glass` (4 modular child sockets: `brick`, `brick frost`, `brick special`, `brick vfx`).
  - `Powerups/`: `PF_Drop_Powerup` (cyan capsule), `PF_Drop_Hazard` (crimson diamond).
- **UI Toolkit & Screen Harmonization**: Native Unity 6 `PanelRenderer` scaled for iPhone portrait reference resolution (`1170x2532`). Features 1:1 design parity across Main Menu and Pause Menu sub-screens, custom animated checkmark mute toggles for SFX and Music (`68px × 68px`) with cyan/crimson audio feedback, and `SafeAreaController` handling mobile device insets.
- **Unity Localization Tables (`com.unity.localization` 1.5.13)**: Multi-language `StringTableCollection` under `Assets/Localization/Tables/` supporting English (`en`) and Turkish (`tr`). Localized strings can be modified dynamically via Unity Editor table windows without code changes, backed by an instant-access fallback dictionary.
- **Rendering & VFX**: Zero-allocation `MaterialPropertyBlock` pooling under `_Pool_VFX` with frame-0 shader prewarming for stutter-free execution on Apple Metal, DX12, Vulkan, and WebGPU.
- **Audio Engine**: `ArcadeAudioManager.cs` with custom SFX and automatic procedural synthesizer fallback.

---

## 🧪 Automated Testing

The project includes **263 automated NUnit EditMode tests** covering physics deflection math, boundary clamps, powerup & hazard lifecycles, diamond falling geometries, shield wall mechanics, modular prefab hierarchies, anti-trap passthrough, UI menu parity, custom mute toggles, and localization tables:

```bash
unity cmd run_tests --mode editor
```

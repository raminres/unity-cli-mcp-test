# BlockBreaker 3D

[![Unity Version](https://img.shields.io/badge/Unity-6%20(6000.6.0f1)-black.svg?style=flat&logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP-blue.svg)](https://unity.com/srp/universal-render-pipeline)
[![Automated Tests](https://img.shields.io/badge/Tests-87%2F87%20Passing%20(100%25)-brightgreen.svg)]()
[![Platforms](https://img.shields.io/badge/Platforms-iOS%20(Swift)%20%7C%20WebGPU%20%7C%20PC-purple.svg)]()
[![Git LFS](https://img.shields.io/badge/Git-LFS%20Enabled-orange.svg)](https://git-lfs.github.com/)
[![AI Integration](https://img.shields.io/badge/AI%20Assistant-Google%20Antigravity-green.svg)]()

**BlockBreaker 3D** is a modern, physics-driven arcade brick breaker built with Unity 6 (6000.6.0f1) and the Universal Render Pipeline (URP). Featuring dynamic paddle deflection physics, explosive chain reactions, armored multi-hit bricks, timed combo multipliers up to 5X, an escalating 7-level campaign arc, mobile safe-area touch controls, frame-0 shader prewarming, and an automated NUnit test suite.

The project also acts as a production testbed for **Google Antigravity**, **Unity MCP (Model Context Protocol)**, and **Unity CLI** agentic workflows.

---

## 🎮 Game Overview & Key Features

- **Dynamic Angle Deflection**: Bounce angles are calculated in real time based on paddle contact offset ($\theta = 90^\circ - \text{offset} \times 60^\circ$), giving players complete precision over shot placement while eliminating horizontal trapping.
- **7-Level Progressive Campaign Arc**: A smoothly escalating campaign that introduces mechanics incrementally—from deflection warmups to explosive chain reactions, multi-ball chaos, and high-tier 4X and 5X combo multipliers. Loops endlessly ($7 \to 1$) preserving cumulative high scores.
- **10 Dynamic Modifiers & Brick Archetypes**:
  - 🌟 **Paddle Expander**: 10-second timed buff widening paddle by $+10\%$ compounding (clamped to max $12.0$) with live HUD countdown timer, reverting cleanly upon expiration.
  - ✖️ **Timed Combo Multipliers (2X, 3X, 4X, 5X)**: 10-second global combo window that multiplies points for all destroyed blocks across the arena. Collecting higher tiers upgrades tier and refreshes duration; collecting equal/lower tiers refreshes duration.
  - 💥 **Bomb Bricks**: Detonates adjacent bricks in a $2.5$-unit radius with outward physical debris impulses.
  - 🛡️ **Shield Powerup**: 10-second defensive safety net. Intercepts falling balls back into docked `ReadyToLaunch` without life loss, accompanied by a live HUD countdown.
  - ⚡ **Multi-Ball Powerup**: Spawns 2 extra balls at diverging angles ($\pm 35^\circ$). Extra balls fall harmlessly; only the final remaining ball causes a life penalty.
  - 💎 **Glass-Enclosed Bricks**: Encased in a translucent 3D crystal shell requiring 2 hits (Hit 1: crystal shatter; Hit 2: brick destroyed for $2\times$ points).
  - 💖 **Extra Heart**: Grants $+1$ life (up to 5 max) with a celebratory flying heart HUD animation.
  - 🏷️ **World-Space Badges**: Crisp, resolution-independent badges rendered via Unity 6 UI Toolkit `PanelRenderer` in `WorldSpace` mode (`x2`, `x3`, `x4`, `x5`, `SHIELD`, `3-BALL`, `+1 HP`, `BOMB`).
- **Real-Time Top HUD Status Indicators**: Dedicated visual status pills with countdown timers and icons for Shield, Multi-Ball, Wide Paddle, and Score Multipliers with tiered color coding.
- **Aspect Ratio Agnostic (`ResponsiveCameraController`)**: Dynamic camera math recalculates view distance on the fly to guarantee 100% visible arena boundaries on any screen (16:9 desktop, 9:16 vertical, 9:19.5 notched mobile).
- **Mobile Safe Area & Touch Support**: `SafeAreaController` dynamically adapts UI Toolkit roots to clear iPhone Dynamic Island, notches, and navigation bars.
- **Zero-Stutter Frame-0 Shader Prewarming**: Off-camera VFX priming during `Start()` forces GPU drivers (Apple Metal, WebGPU, Vulkan, DX12) to compile PSOs upfront, eliminating first-hit frame drops.
- **Dual-Engine Audio**: Handcrafted arcade sound effects paired with an automatic procedural synthesizer fallback for guaranteed sound on any device.

---

## 🕹️ 7-Level Progressive Campaign Arc

Levels are authored as modular ScriptableObjects (`Assets/Settings/Levels/SO_Level_01.asset` through `SO_Level_07.asset`) and dynamically instantiated at runtime:

| Level | Name | Theme & Star Mechanic | Grid | Blocks | Speed | Modifiers Breakdown |
| :---: | :--- | :--- | :---: | :---: | :---: | :--- |
| **1** | **First Flight** | **Warmup & Deflection Mastery** | $5 \times 3$ | **15** | `0.85x` (Paddle 5.5) | • 1x Paddle Expander<br>• *0x Hazards / Armored Bricks* |
| **2** | **Glass & Gold** | **Durability & High Scores** | $6 \times 3$ | **18** | `0.95x` (Paddle 5.0) | • 1x 2X Multiplier, 2x Glass-Enclosed, 1x Expander |
| **3** | **Chain Reaction** | **Explosive Cascades** | $7 \times 6$ | **42** | `1.05x` (Paddle 5.0) | • 2x Bombs, 2x 2X Multipliers, 1x Expander (`Checkerboard`) |
| **4** | **Kinetic Aegis** | **Speed Surge & Protective Net** | $8 \times 6$ | **48** | `1.15x` (Paddle 5.0) | • 1x Shield, 1x Extra Heart, 1x Bomb, 2x Glass, 1x 2X |
| **5** | **Multi-Ball Mayhem** | **Ball Juggling Rush** | $8 \times 6$ | **48** | `1.20x` (Paddle 5.0) | • 2x Multi-Ball, 1x Shield, 1x Bomb, 2x Glass, 1x 2X (`Checkerboard`) |
| **6** | **The High Roller** | **High Stakes & 4X Multiplier** | $9 \times 6$ | **54** | `1.28x` (Paddle 5.0) | • 1x 4X, 2x 3X, 1x 2X, 1x Heart, 1x Shield, 1x Multi-Ball, 2x Bombs, 3x Glass, 1x Expander |
| **7** | **Chaos Gauntlet** | **The Grand Climax & 5X Multiplier** | $10 \times 9$ | **90** | `1.38x` (Paddle 5.0) | • 1x 5X, 2x 4X, 2x 3X, 2x 2X, 2x Expanders, 3x Bombs, 4x Glass, 1x Heart, 2x Shields, 2x Multi-Balls (`Randomized`) |

*Completing Level 7 cycles back seamlessly to Level 1 while preserving the cumulative score.*

---

## 🎯 Controls

| Platform | Action | Input |
| :--- | :--- | :--- |
| **Desktop** | Move Paddle | Mouse Movement (1:1 tracking) or `A`/`D` / Arrow Keys |
| | Launch Ball | Left Mouse Button or `Space` |
| | Pause / Settings | `Esc` or UI Quick Action Bar |
| **Mobile / Touch** | Move Paddle | Horizontal Finger Drag anywhere on screen |
| | Launch Ball | Tap screen when docked |
| | UI Interaction | Touch buttons (all touch targets $> 80\text{px}$) |

---

## 🏗️ Technical Architecture & Engineering

### 1. Dynamic Deflection Math
Bounce angles $\theta$ off the paddle are determined by the contact offset from the paddle center:
$$\text{offset} = \frac{X_{\text{ball}} - X_{\text{paddle}}}{W_{\text{paddle}} / 2} \quad \in [-1, 1]$$
$$\theta = 90^\circ - (\text{offset} \times 60^\circ)$$
This restricts the reflection vector to $[30^\circ, 150^\circ]$, ensuring all deflections launch upward with natural player control.

### 2. UI Toolkit & Safe Area Compliance
- Uses Unity 6 native `PanelRenderer` components on `UI_HUD` and `UI_MainMenu`.
- Runtime `SafeAreaController` listens for orientation and screen resolution updates, calculating safe area insets to keep the HUD top bar safely below the iPhone notch and Dynamic Island.
- 2-row wrapped responsive level selection matrix with touch targets $> 80\text{px}$.
- Quick Action Bar: Pause/Play icon swap, Settings 360° mechanical spin, and Volume mute toggle.
- Interactive Level Settings modal with real-time sliders for Ball Speed, Paddle Speed, and Block Rows.

### 3. Graphics & Performance Optimizations
- **Universal Render Pipeline (URP 17.6.0)**: Master PBR material (`MT_Master_PBR_URP.mat`) with specialized material instances for paddle, ball, and block variants.
- **Frame-0 PSO Prewarming (`BlockVFXManager.cs`)**: Off-camera VFX priming during `Start()` while in `ReadyToLaunch` precompiles pipeline state objects upfront, preventing first-hit hitches on Apple Metal, DX12, Vulkan, and WebGPU.
- **Zero-Allocation MaterialPropertyBlock Tinting**: Dynamically tints bricks and debris while retaining 100% SRP Batcher compatibility without cloning materials.
- **iOS Debris Shader**: Custom `Arcade/VFX_BlockDebris` shader (`Assets/Shaders/VFX_BlockDebris.shader`) compiled specifically for Apple Metal to eliminate missing shader artifacts.
- **iOS Binary & Package Footprint Optimization**: High managed code stripping (`ManagedStrippingLevel.High`), IL2CPP size optimization (`OptimizeSize`), LZ4HC player compression, manual shader variant stripping (lightmaps/fog), and removal of unused packages (`visualscripting`, `physics2d`, `terrain`, etc.).

### 4. Dual-Engine Audio (`ArcadeAudioManager.cs`)
- Persistent singleton (`DontDestroyOnLoad`) wired to dedicated audio clips (`AU_Pop`, `AU_Break`, `AU_Powerup`, `AU_Powerup_Shield`, `AU_Life_Lost`, `AU_Level_Success`, `AU_Game_Over`, `AU_Button_Press`, `AU_Glass_Break`, `AU_Bomb_Explosion`).
- Built-in procedural synthesis fallback generates real-time waveforms if audio clips are absent, ensuring guaranteed acoustic feedback across all builds.

---

## 📁 Repository Structure

```text
unity-cli-mcp-test/
├── .agents/
│   └── skills/
│       └── unity-scene-workflow/       # On-demand Antigravity skill definition
│           └── SKILL.md
├── Assets/
│   ├── Audio/                          # Dedicated sound clips (AU_Pop, AU_Break, etc.)
│   ├── Editor/                         # Automation & scene builders
│   │   ├── PlayModeSceneSetup.cs       # Directs Play mode to Main Menu
│   │   ├── RunBlockBreakerTests.cs     # Command-line test runner
│   │   ├── SetupAssetPresets.cs        # Asset import preset manager
│   │   └── SetupBlockBreakerScenes.cs  # Programmatic level & scene generator
│   ├── Materials/                      # PBR master materials & instances
│   │   ├── MT_Master_PBR_URP.mat
│   │   └── BlockBreaker/               # Ball, Paddle, Glass, Debris, Tiers (Red/Green/Blue)
│   ├── Presets/                        # Unity asset presets with PC/iOS overrides
│   ├── Scenes/                         # Playable scenes
│   │   ├── LV_BlockBreaker_MainMenu.unity # Main Menu (Build Index 0)
│   │   └── LV_BlockBreaker.unity       # Primary 3D Arcade Scene (Build Index 1)
│   ├── Scripts/                        # Modular architecture (Arcade.Gameplay assembly)
│   │   ├── Audio/                      # ArcadeAudioManager & procedural synthesizer
│   │   ├── BlockBreaker/               # Paddle, Ball, Block, Modifiers, VFX, KillZone
│   │   ├── Core/                       # GameManager state machine, ResponsiveCameraController
│   │   ├── Input/                      # Cross-platform touch/mouse/keyboard coordinator
│   │   └── UI/                         # UI Toolkit controllers, SafeAreaController
│   ├── Settings/
│   │   ├── Build Profiles/             # iOS & Standalone build profiles
│   │   └── Levels/                     # SO_Level_01.asset through SO_Level_07.asset
│   ├── Shaders/                        # Custom URP shaders (VFX_BlockDebris)
│   ├── Tests/                          # NUnit test suite (87 automated tests)
│   │   └── BlockBreakerCoreTests.cs
│   ├── Textures/                       # TX_ UI icons (Hearts, Powerups, Badges)
│   ├── UI/                             # UI Toolkit documents (.uxml, .uss, PanelSettings)
│   └── VFX/                            # Visual Effect Graph assets
├── AGENTS.md                           # AI Agent context & persistent workspace memory
├── GDD.md                              # Comprehensive Game Design Document
├── README.md                           # Project overview & architectural guide
└── skills.md                           # Agent playbook & MCP tool recipes
```

---

## 🧪 Automated Test Suite

The project includes an automated test suite with **87 unit and integration tests** verifying core gameplay, math formulas, and edge cases in ~130ms:

```bash
# Execute test suite via Unity CLI / MCP:
Unity.exe -batchmode -runTests -testPlatform EditMode -testResults results.xml
```

### Coverage Highlights:
- **Physics & Deflection**: Normalized offsets, angle boundaries ($[30^\circ, 150^\circ]$), zero-drift restitution.
- **Paddle Dynamics**: Compounding $+10\%$ width calculations, hard clamping at $12.0$, adaptive arena boundary restrictions, 10-second buff timer tick, and base width reversion.
- **Powerup & Combo Behaviors**: Shield 10s timer decrement, killzone dock interception, multi-ball spawn divergence ($\pm 35^\circ$) and death tolerance, Extra Heart maximum clamping (5 max), and global combo score multipliers ($2\times$ through $5\times$) with duration refresh and tier progression.
- **Destruction Logic**: Bomb $2.5$-unit blast radius, non-recursive destruction guards, glass 2-hit durability.
- **UI & Display**: Top HUD power-up status badge timers and dynamic visibility, safe area inset calculation, aspect-ratio frustum framing across $16:9$, $9:16$, and $9:19.5$.
- **Campaign Validation**: Monotonic difficulty scaling across all 7 levels, inclusion of 4X/5X combo multipliers in levels 6 and 7, and endless cycle advancement ($7 \to 1$).

---

## 🛠️ Requirements & Environment

- **Unity**: `6000.6.0f1` (Unity 6)
- **Render Pipeline**: Universal Render Pipeline (URP 17.6.0)
- **Git**: Git 2.50+ with **Git LFS** enabled
- **iOS Toolchain**: Xcode 16+ (Swift Xcode Project lifecycle via `MainApp.swift`)
- **Graphics APIs**: Apple Metal (iOS/macOS), WebGPU / WebGL 2.0 (Web), DirectX 12 / Vulkan (PC)
- **AI Tooling**: Google Antigravity with Unity MCP Server

# BlockBreaker 3D

[![Unity Version](https://img.shields.io/badge/Unity-6%20(6000.6.0f1)-black.svg?style=flat&logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP-blue.svg)](https://unity.com/srp/universal-render-pipeline)
[![Automated Tests](https://img.shields.io/badge/Tests-183%2F183%20Passing%20(100%25)-brightgreen.svg)]()
[![Platforms](https://img.shields.io/badge/Platforms-iOS%20(Swift)%20%7C%20WebGPU%20%7C%20PC-purple.svg)]()
[![Git LFS](https://img.shields.io/badge/Git-LFS%20Enabled-orange.svg)](https://git-lfs.github.com/)
[![AI Integration](https://img.shields.io/badge/AI%20Assistant-Google%20Antigravity-green.svg)]()

**BlockBreaker 3D** is a physics-driven arcade brick breaker built with Unity 6 (6000.6.0f1) and the Universal Render Pipeline (URP). Features 3-tier stepped pyramid paddle deflection, anti-trap trajectory physics, explosive chain cascades, armored 2-hit glass bricks, twin laser blaster cannons, lone block clutch hyper-beam railguns, an escalating 15-level campaign arc, mobile safe-area touch controls, frame-0 shader prewarming, and an automated NUnit test suite (183 tests).

Acts as a production testbed for **Google Antigravity**, **Unity MCP (Model Context Protocol)**, and **Unity CLI** agentic workflows.

---

## 🎮 Key Features

- **3-Tier Inverted Stepped Pyramid Paddle**:
  - Strike Deck ($W = 5.0, H = 0.24$ at $Y = -6.0$, cyan neon rim), Mid Chassis ($W = 3.6, H = 0.20$), Keel/Thrusters ($W = 2.2, H = 0.16$). Ultra-thin $0.60$ vertical profile prevents phantom side catches.
- **Dynamic Deflection & Paddle Steering**:
  - Continuous ray reflection preserving incoming horizontal momentum, subtle offset steering ($-32 \times \text{offset}$), and paddle momentum transfer ($\pm 12^\circ$ velocity sweep), clamped to $[25^\circ, 155^\circ]$.
- **Anti-Trap Ball Physics**:
  - **Minimum Vertical Floor ($20^\circ$)**: Enforces $|v_y| \ge v \cdot \sin(20^\circ)$ to eliminate shallow horizontal traps.
  - **Consecutive Side-Wall Steepener ($35^\circ$)**: $\ge 2$ wall bounces steepen trajectory to $\ge 35^\circ$.
  - **Vertical Deadzone Exclusion ($[85^\circ, 95^\circ]$)**: Eliminates repetitive vertical loops.
  - **$45^\circ$ Continuous Corner Chamfers**: Continuous perimeter wedges at top corners eliminate corner trapping.
- **Lone Block Clutch Countdown & Option B Hyper-Beam Railgun**:
  - Activates when 1 brick remains. Decaying score multiplier ($10\times \to 1\times$) over 12 seconds.
  - On timer expiration, engages emergency Railgun Overcharge: a vertical hyper-beam surges from the paddle deck to the ceiling over $0.65\text{s}$, vaporizing remaining blocks.
- **Hybrid Skill-Based Scoring & Volley Combos**:
  - Unreturned rallies build streak: Hits 1–2 ($1\times$), 3–4 ($2\times$), 5–7 ($3\times$), 8–10 ($4\times$), 11+ ($5\times$ MAX).
  - Banks on paddle hit; HUD combo badge shows multiplier icon (`TX_Powerup_Extra_Points.png`) and temporary `"COMBO ENDED"` text notification.
  - Break SFX scales $+1$ semitone per combo hit (up to $1.68\times$).
- **Dual-Cadence Level Clear & Entity Freeze**:
  - Golden celebratory center banner displays `"LEVEL CLEARED!"` with context-aware subtext.
  - Delays scorecard modal by $1.0\text{s}$ (standard hits) or $1.5\text{s}$ (hyper-beam clear), completely freezing active balls, paddle input, and falling drops during the transition.
  - Victory scorecard modal tallies blocks, peak combo, par time vs elapsed time, and flawless life bonus (+1,000 pts) with 1–3 star rating.
- **Cross-Platform UI Toolkit Architecture**:
  - Native Unity 6 `PanelRenderer`. Powerup sprites bound directly via ScriptableObject `SO_PowerupIcons`, guaranteeing 100% reliable rendering on iOS/Apple Metal builds.
  - `SafeAreaController` adapts HUD pods to iPhone notches and Dynamic Island.
  - `ResponsiveCameraController` guarantees full arena visibility across 16:9, 9:16, and 9:19.5 aspect ratios.

---

## 🕹️ 15-Level Campaign Arc

Levels are authored as modular ScriptableObjects (`Assets/Settings/Levels/SO_Level_01.asset` to `SO_Level_15.asset`) utilizing 17 layout archetypes and custom ASCII parsing:

| Level | Name | Archetype | Grid | Blocks | Speed | Modifiers Breakdown | Par | 3-Star |
| :---: | :--- | :---: | :---: | :---: | :---: | :--- | :---: | :---: |
| **1** | **First Flight** | `Pyramid` | $7 \times 3$ | **15** | `0.92x` | • 1x Expander | 30s | 800 |
| **2** | **Glass & Gold** | `Diamond` | $7 \times 6$ | **22** | `0.96x` | • 1x 2X, 2x Glass, 1x Expander | 35s | 1,400 |
| **3** | **Twin Pillars** | `Pillars` | $7 \times 6$ | **24** | `1.00x` | • 2x Bombs, 2x 2X, 1x Expander, 1x Laser | 40s | 2,000 |
| **4** | **Kinetic Shield** | `Shield` | $8 \times 6$ | **34** | `1.04x` | • 1x Shield, 1x Heart, 1x Bomb, 2x Glass | 45s | 2,600 |
| **5** | **Multi-Ball Ring** | `HollowBox` | $8 \times 6$ | **24** | `1.08x` | • 2x Multi-Ball, 1x Shield, 1x Bomb, 2x Glass | 40s | 3,200 |
| **6** | **Royal Crown** | `Crown` | $9 \times 6$ | **52** | `1.12x` | • 1x 3X, 2x 2X, 1x Heart, 1x Shield, 1x Multi-Ball, 2x Bombs, 3x Glass, 1x Laser | 55s | 4,200 |
| **7** | **Neon Heart** | `Heart` | $9 \times 6$ | **32** | `1.16x` | • 2x Hearts, 1x Shield, 1x 3X, 2x 2X, 1x Bomb, 2x Glass, 1x Multi-Ball | 45s | 3,600 |
| **8** | **Space Invader** | `Invader` | $9 \times 6$ | **28** | `1.20x` | • 2x Bombs, 2x 2X, 2x 3X, 1x Heart, 1x Shield, 1x Multi-Ball, 2x Glass, 1x Laser | 45s | 4,000 |
| **9** | **Crossfire** | `Cross` | $9 \times 6$ | **30** | `1.24x` | • 1x 4X, 2x 2X, 1x 3X, 2x Bombs, 3x Glass, 1x Heart, 1x Shield, 1x Multi-Ball | 45s | 4,500 |
| **10** | **The Hourglass** | `Hourglass` | $9 \times 6$ | **42** | `1.28x` | • 1x 4X, 2x 3X, 2x 2X, 2x Bombs, 3x Glass, 1x Heart, 1x Shield, 1x Multi-Ball | 50s | 5,200 |
| **11** | **Chevron Strike** | `Chevron` | $9 \times 6$ | **18** | `1.32x` | • 1x 4X, 2x 3X, 2x 2X, 2x Bombs, 3x Glass, 1x Heart, 1x Shield, 2x Multi-Balls, 1x Laser | 35s | 3,800 |
| **12** | **Castle Bastion** | `Castle` | $10 \times 6$ | **45** | `1.36x` | • 2x 4X, 2x 3X, 2x 2X, 3x Bombs, 4x Glass, 1x Heart, 2x Shields, 2x Multi-Balls | 55s | 6,000 |
| **13** | **Quantum Lattice** | `CheckerboardEmpty` | $10 \times 6$ | **30** | `1.40x` | • 1x 5X, 2x 4X, 2x 3X, 2x 2X, 3x Bombs, 4x Glass, 1x Heart, 2x Shields, 2x Multi-Balls | 45s | 5,500 |
| **14** | **Striped Vault** | `Stripes` | $10 \times 6$ | **30** | `1.44x` | • 2x 5X, 2x 4X, 2x 3X, 2x 2X, 3x Bombs, 4x Glass, 1x Heart, 2x Shields, 2x Multi-Balls, 1x Laser | 50s | 6,200 |
| **15** | **Chaos Labyrinth** | `Custom` | $10 \times 6$ | **48** | `1.48x` | • 2x 5X, 2x 4X, 2x 3X, 2x 2X, 4x Bombs, 4x Glass, 2x Hearts, 2x Shields, 2x Multi-Balls, 2x Lasers | 60s | 7,500 |

*Dynamic Volley Pacing increases ball speed +8% every 10s of active rally.*

---

## ⚡ Powerups & Collectibles

- 🌟 **Paddle Expander**: Widens paddle $+10\%$ compounding (max $12.0$) with spring overshoot animation.
- ✖️ **Score Multipliers (2X–5X)**: 10-second global score multiplier for all block breaks.
- 🛡️ **Shield**: 10-second defensive safety net. Intercepts falling balls back into docked launch without life loss.
- ⚡ **Multi-Ball**: Spawns 2 extra balls at $\pm 35^\circ$ diverging angles. Points multiplied by live ball count.
- 💖 **Extra Heart**: Grants $+1$ life (up to 5 max) with flying heart HUD parabolic animation.
- 🔫 **Laser Blaster**: Twin paddle-mounted cannons fire ruby bolts ($34\text{ u/s}$) at $0.32\text{s}$ intervals for 10s.
- 💎 **Glass-Enclosed Bricks**: Translucent crystal shell requiring 2 hits (Hit 1: crystal shatter, Hit 2: brick destruction for $2\times$ pts).
- 💥 **Bomb Bricks**: Detonates adjacent bricks in a $2.5$-unit radius with compound chain multipliers.

---

## 🎯 Controls

| Platform | Action | Input |
| :--- | :--- | :--- |
| **Desktop** | Move Paddle | Mouse Movement (1:1 tracking) or `A`/`D` / Arrow Keys |
| | Launch Ball | Left Mouse Button or `Space` |
| | Pause / Options | `Esc` or UI Quick Action Bar |
| **Mobile / Touch** | Move Paddle | Horizontal Finger Drag anywhere on screen |
| | Launch Ball | Tap screen when docked |
| | UI Interaction | Touch buttons (ergonomic touch targets $\ge 54\text{px}$) |

---

## 🏗️ Technical Specifications & Environment

- **Unity**: `6000.6.0f1` (Unity 6)
- **Render Pipeline**: Universal Render Pipeline (URP 17.6.0)
- **Play Mode Start Scene**: `Assets/Scenes/LV_BlockBreaker_MainMenu.unity` (Build Index 0)
- **Gameplay Scene**: `Assets/Scenes/LV_BlockBreaker.unity` (Build Index 1)
- **Arena Boundaries**: Top Wall ($Y = 24.25$, $W = 17.4$), Side Walls ($X = \pm 10.25$, $H = 30.2$), Chamfers $(\pm 9.40, 23.40)$, Kill Zone ($Y = -9.0$).
- **Frame-0 Shader Prewarming**: Off-camera VFX priming during `Start()` compiles GPU PSOs upfront on Apple Metal, DX12, Vulkan, and WebGPU.
- **Dual-Engine Audio**: Handcrafted audio clips paired with procedural synthesizer fallback for guaranteed sound.

---

## 🧪 Automated Test Suite

The project includes **183 unit and integration tests** executing via Unity CLI EditMode test runner:

```bash
# Execute test suite via Unity CLI:
unity cmd run_tests --mode editor
```

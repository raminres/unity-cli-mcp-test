# BlockBreaker 3D

[![Unity Version](https://img.shields.io/badge/Unity-6%20(6000.6.0f1)-black.svg?style=flat&logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP-blue.svg)](https://unity.com/srp/universal-render-pipeline)
[![Automated Tests](https://img.shields.io/badge/Tests-204%2F204%20Passing%20(100%25)-brightgreen.svg)]()
[![Platforms](https://img.shields.io/badge/Platforms-iOS%20(Swift)%20%7C%20WebGPU%20%7C%20PC-purple.svg)]()
[![Git LFS](https://img.shields.io/badge/Git-LFS%20Enabled-orange.svg)](https://git-lfs.github.com/)
[![AI Integration](https://img.shields.io/badge/AI%20Assistant-Google%20Antigravity-green.svg)]()

**BlockBreaker 3D** is a physics-driven arcade brick breaker built with Unity 6 (6000.6.0f1) and the Universal Render Pipeline (URP). Features 3-tier stepped pyramid paddle deflection with tangential surface friction and kinetic speed pops, Model A brick color physical interactions (dampen/boost/scatter), anti-trap trajectory physics, explosive chain cascades, armored 2-hit glass bricks, twin laser blaster cannons, lone block clutch hyper-beam railguns, a complete hazard & powerdown debuff subsystem with tumbling diamond drops, an escalating 15-level campaign arc with high-density grids (11–14 columns), mobile safe-area touch controls, frame-0 shader prewarming, and an automated NUnit test suite (204 tests).

Acts as a production testbed for **Google Antigravity**, **Unity MCP (Model Context Protocol)**, and **Unity CLI** agentic workflows.

---

## 🎮 Key Features

- **3-Tier Inverted Stepped Pyramid Paddle**:
  - Strike Deck ($W = 5.0, H = 0.24$ at $Y = -6.0$, cyan neon rim), Mid Chassis ($W = 3.6, H = 0.20$), Keel/Thrusters ($W = 2.2, H = 0.16$). Ultra-thin $0.60$ vertical profile prevents phantom side catches.
- **Dynamic Paddle Friction, Tangential Slicing & Kinetic Pop**:
  - Tangential surface friction with expanded steering authority ($\pm 45^\circ$, $1.5\text{ deg/(u/s)}$). Slicing against the ball reverses horizontal travel across $90^\circ$ (reversal cut/hook).
  - Active strikes ($|V_x| \ge 3.5\text{ u/s}$) trigger a $+8\%$ kinetic speed impulse, high-pitch pop audio (`1.22f`), deep squash recoil ($18\%$), and electric cyan impact sparks. Stationary paddle preserves steady cushion control.
- **Model A Brick Color Physical Interactions & Audio Character**:
  - 🔴 **Red (Tier 1, 10 pts)**: Kinetic Dampener / Brake. Absorbs impact energy ($-1.2\text{ u/s}$, floored at `baseSpeed = 14f`) with deep low-pitch break audio (`pitch = 0.90f`).
  - 🟢 **Green (Tier 2, 20 pts)**: Kinetic Turbo / Spring Bumper. Imparts snappy $+10\%$ speed impulse up to `maxSpeed = 22f` with bright mid-high break audio (`pitch = 1.10f`).
  - 🔵 **Blue (Tier 3, 30 pts)**: Optical Prism Deflector / Scatter. Induces chaotic optical refraction ($\pm 18^\circ$ to $\pm 28^\circ$ exit scatter) to eliminate repetitive trajectory loops, with crystalline chime audio (`pitch = 1.25f`).
- **Anti-Trap Ball Physics**:
  - **Minimum Vertical Floor ($20^\circ$)**: Enforces $|v_y| \ge v \cdot \sin(20^\circ)$ to eliminate shallow horizontal traps.
  - **Consecutive Side-Wall Steepener ($35^\circ$)**: $\ge 2$ wall bounces steepen trajectory to $\ge 35^\circ$.
  - **Vertical Deadzone Exclusion ($[85^\circ, 95^\circ]$)**: Eliminates repetitive vertical loops.
  - **$45^\circ$ Continuous Corner Chamfers**: Continuous perimeter wedges at top corners eliminate corner trapping.
- **Powerdown & Hazard Subsystem**:
  - **Visual Differentiation**: Positive powerups radiate positive cyan glow (`#00f2fe`), whereas powerdowns display warning crimson (`#ff1744`) across brick badges, falling drop meshes, billboard sprites, and top HUD timers.
  - **Diamond Drop Geometry**: Powerdowns tumble down as sharp, faceted 3D diamonds (cube rotated $45^\circ$ along all axes) to instantly distinguish them from rounded powerup capsules.
  - **Paddle Freeze Struggle Feedback**: When immobilized by a paddle freezer, attempting to move triggers a high-frequency sinusoidal tremor (`Mathf.Sin(Time.time * 45f) * 0.07f`), visually conveying a frozen mechanical state rather than unresponsive input.
  - **Defrost Absorption**: Frozen field bricks absorb a defrost hit before shattering, preventing automatic cascading.
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
  - Native Unity 6 `PanelRenderer`. Powerup and powerdown sprites bound directly via ScriptableObject `SO_PowerupIcons`, guaranteeing 100% reliable rendering on iOS/Apple Metal builds.
  - `SafeAreaController` adapts HUD pods to iPhone notches and Dynamic Island.
  - `ResponsiveCameraController` guarantees full arena visibility across 16:9, 9:16, and 9:19.5 aspect ratios.

---

## 🕹️ 15-Level Campaign Arc

Levels are authored as modular ScriptableObjects (`Assets/Settings/Levels/SO_Level_01.asset` to `SO_Level_15.asset`) with high-density grids (11–14 columns, $13.75\text{u}$ to $17.5\text{u}$ span) and outer flank bumper blocks eliminating empty side highways:

| Level | Name | Archetype | Grid | Blocks | Speed | Modifiers Breakdown | Par | 3-Star |
| :---: | :--- | :---: | :---: | :---: | :---: | :--- | :---: | :---: |
| **1** | **First Flight** | `Pyramid` | $11 \times 6$ | **42** | `0.92x` | • 1x Expander | 35s | 1,200 |
| **2** | **Glass & Gold** | `Diamond` | $12 \times 6$ | **42** | `0.96x` | • 1x 2X, 2x Glass, 1x Expander | 40s | 1,800 |
| **3** | **Twin Pillars** | `Pillars` | $13 \times 6$ | **42** | `1.00x` | • 2x Bombs, 2x 2X, 1x Expander, 1x Laser | 45s | 2,400 |
| **4** | **Kinetic Shield** | `Shield` | $13 \times 6$ | **62** | `1.04x` | • 1x Shield, 1x Heart, 1x Bomb, 2x Glass | 50s | 3,200 |
| **5** | **Multi-Ball Ring** | `HollowBox` | $13 \times 6$ | **34** | `1.08x` | • 2x Multi-Ball, 1x Shield, 1x Bomb, 2x Glass | 45s | 3,800 |
| **6** | **Royal Crown** | `Crown` | $13 \times 6$ | **70** | `1.12x` | • 1x 3X, 2x 2X, 1x Heart, 1x Shield, 1x Multi-Ball, 2x Bombs, 3x Glass, 1x Laser | 60s | 5,000 |
| **7** | **Neon Heart** | `Heart` | $13 \times 6$ | **52** | `1.16x` | • 2x Hearts, 1x Shield, 1x 3X, 2x 2X, 1x Bomb, 2x Glass, 1x Multi-Ball | 50s | 4,200 |
| **8** | **Space Invader** | `Invader` | $13 \times 6$ | **34** | `1.20x` | • 2x Bombs, 2x 2X, 2x 3X, 1x Heart, 1x Shield, 1x Multi-Ball, 2x Glass, 1x Laser | 45s | 4,500 |
| **9** | **Crossfire** | `Cross` | $13 \times 6$ | **48** | `1.24x` | • 1x 4X, 2x 2X, 1x 3X, 2x Bombs, 3x Glass, 1x Heart, 1x Shield, 1x Multi-Ball | 50s | 5,200 |
| **10** | **The Hourglass** | `Hourglass` | $13 \times 6$ | **56** | `1.28x` | • 1x 4X, 2x 3X, 2x 2X, 2x Bombs, 3x Glass, 1x Heart, 1x Shield, 1x Multi-Ball | 55s | 6,000 |
| **11** | **Chevron Strike** | `Chevron` | $13 \times 6$ | **38** | `1.32x` | • 1x 4X, 2x 3X, 2x 2X, 2x Bombs, 3x Glass, 1x Heart, 1x Shield, 2x Multi-Balls, 1x Laser | 45s | 4,800 |
| **12** | **Castle Bastion** | `Castle` | $14 \times 6$ | **59** | `1.36x` | • 2x 4X, 2x 3X, 2x 2X, 3x Bombs, 4x Glass, 1x Heart, 2x Shields, 2x Multi-Balls | 60s | 7,000 |
| **13** | **Quantum Lattice** | `CheckerboardEmpty` | $14 \times 6$ | **45** | `1.40x` | • 1x 5X, 2x 4X, 2x 3X, 2x 2X, 3x Bombs, 4x Glass, 1x Heart, 2x Shields, 2x Multi-Balls | 50s | 6,500 |
| **14** | **Striped Vault** | `Stripes` | $14 \times 6$ | **44** | `1.44x` | • 2x 5X, 2x 4X, 2x 3X, 2x 2X, 3x Bombs, 4x Glass, 1x Heart, 2x Shields, 2x Multi-Balls, 1x Laser | 55s | 7,200 |
| **15** | **Chaos Labyrinth** | `Custom` | $14 \times 6$ | **66** | `1.48x` | • 2x 5X, 2x 4X, 2x 3X, 2x 2X, 4x Bombs, 4x Glass, 2x Hearts, 2x Shields, 2x Multi-Balls, 2x Lasers | 65s | 8,500 |

*Dynamic Volley Pacing increases ball speed +8% every 10s of active rally.*

---

## ⚡ Powerups & Hazards

### Positive Buffs (Cyan / Emerald Glow `#00f2fe`)
- 🌟 **Paddle Expander**: Widens paddle $+10\%$ compounding (max $12.0$) with spring overshoot animation.
- ✖️ **Score Multipliers (2X–5X)**: 10-second global score multiplier for all block breaks.
- 🛡️ **Shield**: 10-second defensive safety net. Intercepts falling balls back into docked launch without life loss.
- ⚡ **Multi-Ball**: Spawns 2 extra balls at $\pm 35^\circ$ diverging angles. Points multiplied by live ball count.
- 💖 **Extra Heart**: Grants $+1$ life (up to 5 max) with flying heart HUD parabolic animation.
- 🔫 **Laser Blaster**: Twin paddle-mounted cannons fire ruby bolts ($34\text{ u/s}$) at $0.32\text{s}$ intervals for 10s.

### Hazards & Debuffs (Warning Crimson Glow `#ff1744` & Tumbling Diamonds)
- 🔻 **Paddle Shortener**: Shrinks paddle width $-18\%$ (clamped to min $2.4\text{u}$) for 10s.
- 🐌 **Paddle Slower**: Introduces high-friction input inertia and sluggish paddle response for 8s.
- ❄️ **Brick Freezer**: Encases up to 5 field blocks in glacial ice, requiring 1 defrost hit before shattering.
- 🔍 **Ball Size Decreaser**: Shrinks ball radius to $60\%$ ($0.8\text{u} \to 0.48\text{u}$) for 10s.
- 🐢 **Ball Slower**: Drops ball velocity to $9.5\text{ u/s}$ for 8s.
- 🧊 **Paddle Freezer**: Freezes paddle for $1.2\text{s}$; player input triggers an active mechanical tremor animation.

### Environmental Bricks
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

The project includes **204 unit and integration tests** executing via Unity CLI EditMode test runner:

```bash
# Execute test suite via Unity CLI:
unity cmd run_tests --mode editor
```

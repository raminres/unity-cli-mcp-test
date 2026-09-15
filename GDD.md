# BlockBreaker: Game Design Document (GDD)

**Version**: 3.5  
**Status**: Living Design Specification  
**Project**: BlockBreaker (`com.RaminRasulzade.BlockBreaker`)  
**Target Engine**: Unity 6 (6000.6.0f1) Universal Render Pipeline (URP)  
**Lead Designer / Architect**: Ramin Rasulzade & Antigravity  

---

## 1. Executive Summary & Vision

### 1.1 Game Concept
**BlockBreaker** is a physics-driven 3D arcade brick breaker built with Unity 6. Blending tactical paddle deflection, skill-based volley combo multipliers, real-time floating feedback, explosive chain reactions, and vibrant Universal Render Pipeline visuals, BlockBreaker delivers an escalating arcade experience across a progressive 15-level campaign arc.

### 1.2 Core Pillars
1. **Kinetic Control & Agency**: Deflection is player-directed through 3-tier stepped paddle contact geometry, paddle momentum transfer, and anti-trap trajectory guards.
2. **Skill-Driven Hybrid Scoring**: Compounding rewards through unreturned volley rallies, compound bomb chains, multi-ball juggling, under-par speed bonuses, and flawless life bonuses.
3. **Dual-Layer Readability**: Points are communicated at the impact point (world-space floating popups) and on the HUD dashboard (score delta ticker + live combo badge).
4. **Cross-Platform Polish**: Mobile touch (iOS Dynamic Island / notch safe-area compliance) and desktop controls, zero-allocation rendering, and hitch-free Apple Metal execution.
5. **Acoustic Feedback**: Handcrafted arcade sound effects, ascending musical pitch scales on combo streaks, and procedural synthesis fallbacks.

---

## 2. Core Mechanics & Physics

### 2.1 Inverted Stepped Pyramid Paddle & Dynamic Deflection
- **3-Tier Geometry**:
  - **Tier 1 (Top Strike Deck)**: $100\%$ width ($W = 5.0$), ultra-thin profile ($H = 0.24$, $Z = 1.0$) with glowing neon cyan rim (`MI_Paddle_Deck.mat`). Top surface at $Y = -6.0$ for consistent ball docking.
  - **Tier 2 (Mid Chassis)**: Stepped inward to $72\%$ width ($W = 3.6$), height $H = 0.20$, $Z = 0.88$ in dark brushed titanium (`MI_Paddle.mat`).
  - **Tier 3 (Keel / Thrusters)**: Stepped inward to $44\%$ width ($W = 2.2$), height $H = 0.16$, $Z = 0.72$ with engine vent glow (`MI_Paddle_Core.mat`).
  - Combined vertical profile: $0.60$ with lower sides stepped inward up to $1.4$ units per side to eliminate phantom side catches.
- **Strike Collider & Contact Normal Guard**:
  - Primary `BoxCollider` fitted strictly to Tier 1 ($H = 0.24$, center $Y = +0.38$). Below $Y = -6.24$, zero collision volume exists.
  - Normal Threshold: `BallController.IsValidPaddleBounceNormal(normal)` (`normal.y >= 0.25f`) guarantees brushing balls fall cleanly into killzone.
- **Optical Ray Deflection & Paddle Steering Formula**:
  - Computed via `BallController.CalculatePaddleDeflection(inVelocity, hitOffset, steerStrength = 32f, minAngleDeg = 25f, maxAngleDeg = 155f, paddleVelocityX, velocityInfluence = 0.5f, verticalDeadzoneAngleDeg = 5f)`.
  - Preserves incoming horizontal momentum (`rayAngleDeg = Mathf.Atan2(|inVelocity.y|, inVelocity.x) * Rad2Deg`), applies offset steering `steer = -hitOffset * 32f`, applies paddle momentum transfer `clamp(-paddleVelocityX * 0.5f, -12°, +12°)`, strictly excludes vertical deadzone $[85^\circ, 95^\circ]$, and clamps to $[25^\circ, 155^\circ]$.
- **Anti-Trap Ball Physics**:
  - **Minimum Vertical Floor ($20^\circ$)**: Mathematical enforcement $|v_y| \ge v \cdot \sin(20^\circ)$ prevents shallow horizontal trapping.
  - **Consecutive Side-Wall Steepener ($35^\circ$)**: $\ge 2$ consecutive wall bounces steepens trajectory to $\ge 35^\circ$.
  - **Vertical Deadzone Exclusion ($[85^\circ, 95^\circ]$)**: Deflection and launch exclude vertical cone; continuous play enforces $|v_x| \ge v \cdot \sin(5^\circ)$, preventing vertical ping-pong loops.
  - **$45^\circ$ Continuous Corner Chamfers**: Polygonal frame joins shortened top wall ($W = 17.4$ at $Y = 24.25$) and side walls ($H = 30.2$ at $X = \pm 10.25$) with angled wedges at $(\pm 9.40, 23.40)$, eliminating corner deadzones.
- **Compounding Expansion with Spring Overshoot**:
  - $+10\%$ per expander ($W_n = W_{prev} \times 1.10$, clamped to max $12.0$). Spring-damper overshoot animation ($\approx +16\%$ with squash-and-stretch settling over $0.35\text{s}$).

### 2.2 Arena Dimensions & Camera
- **Arena Boundaries**:
  - Top Wall: $Y = 24.25$, width $17.4$. Side Walls: $X = \pm 10.25$, height $30.2$. Top Chamfers: $(\pm 9.40, 23.40)$, length $2.5$. Kill Zone: $Y = -9.0$.
- **Dynamic Frustum Framing (`ResponsiveCameraController`)**:
  - Perspective camera with $38^\circ$ vertical FOV. Dynamically adjusts $Z$-distance to guarantee 100% visible arena boundaries on any aspect ratio (16:9, 9:16, 9:19.5).

---

## 3. Hybrid Skill-Based Scoring Architecture

```
Total Awarded Points = Base Points × Active Capsule Multiplier × Volley Combo Multiplier × Multi-Ball Multiplier
```

### 3.1 Unreturned Volley Combo System
- Unreturned rallies increment streak: Hits 1–2 ($1\times$), Hits 3–4 ($2\times$), Hits 5–7 ($3\times$), Hits 8–10 ($4\times$), Hits 11+ ($5\times$ MAX).
- Banks into score upon paddle impact; resets if ball falls into killzone.
- Top HUD combo badge dynamically displays multiplier icon (`TX_Powerup_Extra_Points.png`) and streak label (`x{N} COMBO`).
- When combo ends or banks on paddle, displays temporary `"COMBO ENDED"` text notification in the combo badge for $1.2\text{s}$ before hiding.
- Break SFX ascends $+1$ semitone per consecutive hit up to $1.68\times$.

### 3.2 Powerup & Chain Synergies
- **Bomb Chains**: Detonates in $2.5$-unit radius with compounding chain multipliers ($\text{base} \times 1.5^{\text{chainIndex}}$).
- **Multi-Ball**: Spawns 2 extra balls at $\pm 35^\circ$. All points earned multiplied by live ball count ($2\times$ or $3\times$).
- **Glass Shell**: Requires 2 hits (Hit 1: crystal shatter, Hit 2: brick destruction for $2\times$ points).

### 3.3 Dual-Layer Real-Time Score Feedback
- **World-Space Floating Popups (`FloatingScoreManager.cs`)**: Spawns at impact point at $Z = -0.8\text{f}$ (`+20`, `+120 x3!`, `+450 BOMB!`), drifts up $+1.2$ units over $0.65\text{s}$.
- **HUD Dashboard Feedback**: Score delta ticker (`+150`), live combo badge, digital level timer (`MM:SS`).

### 3.4 Lone Block Clutch Countdown & Option B Hyper-Beam Railgun
- **Clutch Countdown**: When exactly 1 block remains, activates 12-second countdown with HUD badge and decaying multiplier ($10\times \to 1\times$).
- **Option B Hyper-Beam Railgun**: If timer expires without hitting the block, paddle engages emergency Railgun Overcharge. A wide vertical hyper-beam ($W \approx 3.2, H \approx 31$) progressively surges upward from the paddle deck to ceiling over $0.65\text{s}$, slicing through remaining bricks.
- **Dual-Cadence Clear Delay & Entity Freeze**:
  - Celebratory center banner displays `"LEVEL CLEARED!"` with context-aware subtext (`"STAGE COMPLETE!"`, `"FLAWLESS VICTORY!"`, or `"CLUTCH OVERCHARGE!"`).
  - Delay cadence: $1.0\text{s}$ for standard clears, $1.5\text{s}$ for hyper-beam clears before victory scorecard modal opens.
  - While clear is pending, all balls immediately freeze (`linearVelocity = 0`), paddle input is locked, and falling capsules/lasers freeze in place.

---

## 4. End-of-Level Victory Scorecard & 3-Star Rating

### 4.1 Scorecard Modal (`modal-scorecard`)
Tallies blocks destroyed, peak volley combo, elapsed time vs par time, time bonus pool, under-par speed bonus (+500 pts), and flawless bonus (+1,000 pts). Features Replay, Next Level, and Main Menu actions.

### 4.2 Star Ratings & Best Times (`HighScoreManager.cs`)
- 1 Star: Clear level. 2 Stars: Exceed Silver threshold. 3 Stars: Exceed Gold threshold.
- Persistent speedrun records (Best Clear Time) and session-based cumulative high scores saved in `PlayerPrefs`.

---

## 5. Powerups & Collectibles

| Modifier | Capsule Tint & Icon | Type | Description |
| :--- | :--- | :--- | :--- |
| **Paddle Expander** | Neon cyan & arrow | Power-up | 10s buff widening paddle $+10\%$ compounding (max $12.0$) with spring overshoot. |
| **Multiplier (2X–5X)** | Tiered gold/orange/ruby/magenta | Buff | 10s global score multiplier buff for all block breaks. |
| **Shield** | Electric blue & shield | Defensive | 10s defensive safety net at arena bottom, redocking balls without life loss. |
| **Multi-Ball** | Neon magenta & 3-ball | Kinetic | Spawns 2 extra balls at $\pm 35^\circ$. Points multiplied by live ball count. |
| **Extra Heart** | Neon pink & heart | Recovery | Grants $+1$ life (up to 5 max) with HUD flying heart animation. |
| **Laser Blaster** | Ruby red & laser guns | Offensive | 10s twin paddle cannons firing ruby bolts ($34\text{ u/s}$) at $0.32\text{s}$ intervals. |
| **Glass-Enclosed** | Translucent 1.18x shell | Armored | 2-hit brick (Hit 1: crystal shatter, Hit 2: brick destruction for $2\times$ pts). |
| **Bomb Brick** | Crimson BOMB badge | Hazard | Detonates in $2.5$-unit radius with compounding chain multipliers. |

---

## 6. Progressive 15-Level Campaign Arc

| Level | Name | Archetype | Grid | Blocks | Speed | Modifiers Breakdown | Par | 3-Star Target |
| :---: | :--- | :---: | :---: | :---: | :---: | :--- | :---: | :---: |
| **1** | **First Flight** | `Pyramid` | $7 \times 3$ | **15** | `0.92x` | • 1x Expander | 30s | 800 pts |
| **2** | **Glass & Gold** | `Diamond` | $7 \times 6$ | **22** | `0.96x` | • 1x 2X, 2x Glass, 1x Expander | 35s | 1,400 pts |
| **3** | **Twin Pillars** | `Pillars` | $7 \times 6$ | **24** | `1.00x` | • 2x Bombs, 2x 2X, 1x Expander, 1x Laser | 40s | 2,000 pts |
| **4** | **Kinetic Shield** | `Shield` | $8 \times 6$ | **34** | `1.04x` | • 1x Shield, 1x Heart, 1x Bomb, 2x Glass | 45s | 2,600 pts |
| **5** | **Multi-Ball Ring** | `HollowBox` | $8 \times 6$ | **24** | `1.08x` | • 2x Multi-Ball, 1x Shield, 1x Bomb, 2x Glass | 40s | 3,200 pts |
| **6** | **Royal Crown** | `Crown` | $9 \times 6$ | **52** | `1.12x` | • 1x 3X, 2x 2X, 1x Heart, 1x Shield, 1x Multi-Ball, 2x Bombs, 3x Glass, 1x Laser | 55s | 4,200 pts |
| **7** | **Neon Heart** | `Heart` | $9 \times 6$ | **32** | `1.16x` | • 2x Hearts, 1x Shield, 1x 3X, 2x 2X, 1x Bomb, 2x Glass, 1x Multi-Ball | 45s | 3,600 pts |
| **8** | **Space Invader** | `Invader` | $9 \times 6$ | **28** | `1.20x` | • 2x Bombs, 2x 2X, 2x 3X, 1x Heart, 1x Shield, 1x Multi-Ball, 2x Glass, 1x Laser | 45s | 4,000 pts |
| **9** | **Crossfire** | `Cross` | $9 \times 6$ | **30** | `1.24x` | • 1x 4X, 2x 2X, 1x 3X, 2x Bombs, 3x Glass, 1x Heart, 1x Shield, 1x Multi-Ball | 45s | 4,500 pts |
| **10** | **The Hourglass** | `Hourglass` | $9 \times 6$ | **42** | `1.28x` | • 1x 4X, 2x 3X, 2x 2X, 2x Bombs, 3x Glass, 1x Heart, 1x Shield, 1x Multi-Ball | 50s | 5,200 pts |
| **11** | **Chevron Strike** | `Chevron` | $9 \times 6$ | **18** | `1.32x` | • 1x 4X, 2x 3X, 2x 2X, 2x Bombs, 3x Glass, 1x Heart, 1x Shield, 2x Multi-Balls, 1x Laser | 35s | 3,800 pts |
| **12** | **Castle Bastion** | `Castle` | $10 \times 6$ | **45** | `1.36x` | • 2x 4X, 2x 3X, 2x 2X, 3x Bombs, 4x Glass, 1x Heart, 2x Shields, 2x Multi-Balls | 55s | 6,000 pts |
| **13** | **Quantum Lattice** | `CheckerboardEmpty` | $10 \times 6$ | **30** | `1.40x` | • 1x 5X, 2x 4X, 2x 3X, 2x 2X, 3x Bombs, 4x Glass, 1x Heart, 2x Shields, 2x Multi-Balls | 45s | 5,500 pts |
| **14** | **Striped Vault** | `Stripes` | $10 \times 6$ | **30** | `1.44x` | • 2x 5X, 2x 4X, 2x 3X, 2x 2X, 3x Bombs, 4x Glass, 1x Heart, 2x Shields, 2x Multi-Balls, 1x Laser | 50s | 6,200 pts |
| **15** | **Chaos Labyrinth** | `Custom` | $10 \times 6$ | **48** | `1.48x` | • 2x 5X, 2x 4X, 2x 3X, 2x 2X, 4x Bombs, 4x Glass, 2x Hearts, 2x Shields, 2x Multi-Balls, 2x Lasers | 60s | 7,500 pts |

---

## 7. UI / UX Architecture

### 7.1 HUD Layout & Dashboard Insets
- `SafeAreaController.cs` dynamically handles insets for iPhone notches and Dynamic Island.
- **Top Bar**: Left pod (5-heart life gauge), Center pod (cumulative score + delta ticker `+150`), Timer pod (`MM:SS`), Combo badge with multiplier icon & status text, Right pod (Mute, Settings, Pause).
- **Powerup & Badges**: Inspector-assigned ScriptableObject `SO_PowerupIcons` guarantees reliable sprite rendering across iOS/Metal and PC.

---

## 8. Audio Architecture

- **Dedicated Sound Effects (`AU_`)**:
  - Deflection: `AU_Pop.mp3`
  - Brick Shatter: `AU_Break.mp3` with $+1$ semitone pitch scaling per combo streak.
  - Star Tally: `PlayStarEarned` triumphant chimes for 1-star, 2-star, and 3-star scorecard reveals.
  - Powerups & Weapons: `AU_Powerup.mp3`, `AU_Powerup_Shield.mp3`, `AU_Powerup_Laser.mp3`.
  - Explosions: `AU_Bomb_Explosion.mp3`, `AU_Glass_Break.mp3`.
  - Fanfares: `AU_Life_Lost.mp3`, `AU_Level_Success.mp3`, `AU_Game_Over.mp3`.
- **Procedural Synthesizer Fallback**: Built-in fallback generating real-time waveforms if audio clips are unassigned.

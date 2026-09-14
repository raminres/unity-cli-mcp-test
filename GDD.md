# BlockBreaker: Game Design Document (GDD)

**Version**: 3.0  
**Status**: Living Design Specification  
**Project**: BlockBreaker (`com.RaminRasulzade.BlockBreaker`)  
**Target Engine**: Unity 6 (6000.6.0f1) Universal Render Pipeline (URP)  
**Lead Designer / Architect**: Ramin Rasulzade & Antigravity  

---

## 1. Executive Summary & Vision

### 1.1 Game Concept
**BlockBreaker** is a premium, physics-driven 3D arcade brick breaker built with cutting-edge Unity 6 architectural patterns. Blending tactile paddle deflection, skill-based volley combo multipliers, real-time floating feedback, explosive chain reactions, and vibrant Universal Render Pipeline visuals, BlockBreaker delivers an escalating, high-energy arcade journey across a progressive 15-level campaign arc.

### 1.2 Core Pillars
1. **Kinetic Control & Agency**: Deflection is entirely player-directed through continuous paddle contact geometry, paddle velocity momentum transfer, and anti-trap physics guards.
2. **Skill-Driven Hybrid Scoring**: Players earn multiplying rewards through unreturned volley rallies, compound bomb chain reactions, multi-ball juggling, and fast par-time clears.
3. **Dual-Layer Visceral Readability**: Every point earned is instantly communicated both at the world-space collision point (floating popups) and on the HUD dashboard (score delta ticker + live combo meter).
4. **Pristine Cross-Platform Polish**: Designed from day one for seamless touch (iOS Dynamic Island / notch safe-area compliance) and desktop gameplay, zero-allocation rendering, and hitch-free Apple Metal / WebGPU execution.
5. **Resilient Arcade Sound**: A hybrid audio engine combining handcrafted sound effects, ascending musical pitch scales on combo streaks, and procedural synthesis fallbacks for 100% offline acoustic reliability.

---

## 2. Core Mechanics & Physics

### 2.1 Inverted Stepped Pyramid Paddle & Dynamic Deflection
- **3-Tier Geometry**:
  - Tier 1 (Top Strike Deck): $100\%$ width ($W = 5.0$), ultra-thin profile ($H = 0.24$, $Z = 1.0$) with glowing neon cyan rim (`MI_Paddle_Deck.mat`). Top surface preserved exactly at $Y = -6.0$ for consistent ball docking.
  - Tier 2 (Mid Chassis): Stepped inward to $72\%$ width ($W = 3.6$), height $H = 0.20$, $Z = 0.88$ in dark brushed titanium (`MI_Paddle.mat`).
  - Tier 3 (Bottom Keel / Thrusters): Stepped inward to $44\%$ width ($W = 2.2$), height $H = 0.16$, $Z = 0.72$ with engine vent glow (`MI_Paddle_Core.mat`).
  - Total combined vertical profile: $0.60$ (reduced from legacy $1.0$ cube), with lower sides stepped inward by up to $1.4$ units on each side to eliminate phantom side catches.
- **Physical Strike Collider & Contact Normal Guard**:
  - Primary `BoxCollider` is strictly fitted to Tier 1 ($H = 0.24$, center $Y = +0.38$). Below $Y = -6.24$, there is zero collision volume.
  - Upward Normal Threshold: `BallController.IsValidPaddleBounceNormal(normal)` (`normal.y >= 0.25f`) guarantees that balls brushing the side wall or hitting from below fall cleanly into the killzone.
- **Optical Ray Deflection & Paddle Steering Formula**:
  - Deflection computed via `BallController.CalculatePaddleDeflection(inVelocity, hitOffset, steerStrength = 32f, minAngleDeg = 25f, maxAngleDeg = 155f, paddleVelocityX, velocityInfluence = 0.5f, verticalDeadzoneAngleDeg = 5f)`.
  - Preserves incoming horizontal momentum (`rayAngleDeg = Mathf.Atan2(|inVelocity.y|, inVelocity.x) * Rad2Deg`), naturally reflecting incoming vectors without unnatural direction reversal, applies subtle paddle steering based on contact offset `steer = -hitOffset * 32f`, incorporates paddle momentum transfer `clamp(-paddleVelocityX * 0.5f, -12°, +12°)`, strictly excludes near-vertical deadzone $[85^\circ, 95^\circ]$, and clamps final angle to $[25^\circ, 155^\circ]$.
- **Anti-Trap Ball Physics**:
  - **Minimum Vertical Floor ($20^\circ$)**: In `FixedUpdate()`, `SanitizeTrajectory()` mathematically enforces $|v_y| \ge \text{currentSpeed} \cdot \sin(20^\circ)$, preventing shallow horizontal traps between arena walls while preserving exact speed and sign.
  - **Consecutive Side-Wall Steepener ($35^\circ$)**: Tracks `consecutiveSideWallBounces`. On $\ge 2$ consecutive side-wall hits without hitting paddle or block, steepens vertical trajectory to $\ge 35^\circ$ ($\sin(35^\circ) \approx 0.574$), forcing rapid vertical progression. Resets to 0 on block impact, paddle save, launch, or dock.
  - **Vertical Exclusion Deadzone ($[85^\circ, 95^\circ]$)**: Deflection angle and initial launch strictly exclude the $\pm 5^\circ$ vertical cone around $90^\circ$. Continuous play enforces $|v_x| \ge \text{currentSpeed} \cdot \sin(5^\circ)$, causing straight vertical trajectories to subtly drift away from center and eliminating endless vertical ping-pong loops.
  - **Paddle Velocity Momentum Transfer**: `PaddleController.VelocityX` dynamically measures instantaneous horizontal movement velocity, imparting tactile directional steering when sweeping the paddle during impact.
- **Compounding Expansion with Spring Overshoot**:
  - $+10\%$ per expander ($W_n = W_{prev} \times 1.10$, clamped to max $12.0$, adaptive bounds $[-10 + \frac{W}{2}, 10 - \frac{W}{2}]$).
  - Features spring-damper overshoot animation ($\approx +16\%$ overshoot with $Y$-axis squash-and-stretch settling over $0.35\text{s}$) for tactile arcade feedback.

### 2.2 Arena Dimensions & Camera
- **Arena Boundaries**:
  - Top Wall: $Y = 24.25$, Left/Right Walls: $X = \pm 10.5$ (height $32.0$), Kill Zone: $Y = -9.0$.
- **Dynamic Frustum Framing (`ResponsiveCameraController`)**:
  - Perspective camera with vertical field of view $\text{FOV}_v = 38^\circ$.
  - Dynamically calculates camera $Z$-distance to guarantee 100% visible arena boundaries on any aspect ratio: 16:9 widescreen, 9:16 portrait mobile, 9:19.5 iPhone tall displays, or ultrawide.

---

## 3. Hybrid Skill-Based Scoring Architecture

Scoring combines high-speed arcade execution, in-flight volley chains, powerup synergies, and speedrun incentives:

```
Total Awarded Points = Base Points × Active Capsule Multiplier × Volley Combo Multiplier × Multi-Ball Multiplier
```

### 3.1 Unreturned Volley Combo System
- **The Core Loop**:
  - When launched, the ball begins at a $1\times$ volley multiplier.
  - Every block destroyed *before the ball returns to the paddle* increments the streak counter:
    - Hits 1–2: $1\times$
    - Hits 3–4: $2\times$
    - Hits 5–7: $3\times$
    - Hits 8–10: $4\times$
    - Hits 11+: $5\times$ (MAX COMBO)
  - **Banking**: When the ball successfully strikes the paddle, the combo safely banks into the score with a celebratory audio chime (`PlayComboBank`).
  - **Audio Pitch Scaling**: Each consecutive hit increases the break SFX pitch by $+1$ semitone ($2^{\frac{\text{streak}}{12}}$, up to $1.68\times$), playing an ascending musical scale that ratchets excitement.

### 3.2 Powerup & Chain Synergies
- **Compound Bomb Chains**: Bricks destroyed via explosive radius detonate with compound chain rewards: each consecutive exploded neighbor awards $\text{base} \times 1.5^{\text{chainIndex}}$ bonus points with floating `BOMB CHAIN!` tags.
- **Multi-Ball Juggling Synergy**: When multiple balls are live, all points are multiplied by the active ball count ($2\times$ for 2 balls, $3\times$ for 3 balls).
- **Glass Shell Shatter**: First hit shatters the crystal glass shell; second hit destroys the core block for $2\times$ base points.

### 3.3 Dual-Layer Real-Time Score Feedback
- **World-Space Floating Popups (`FloatingScoreManager.cs`)**:
  - Spawns at the brick impact point at $Z = -0.8\text{f}$ directly in front of the playfield:
    - Standard hit: `+20` (vibrant gold/white)
    - Volley Combo: `+120 x3!` (electric cyan/magenta)
    - Bomb Chain: `+450 BOMB CHAIN!` (fiery orange)
    - Clutch Overcharge: `+300 CLUTCH 10X!` (radiant amber)
  - Floats upward $+1.2$ units and fades smoothly over $0.65\text{s}$.
- **HUD Dashboard Real-Time Feedback**:
  - **Live Score Delta Ticker (`score-delta-label`)**: Floating green/gold pop next to the score readout showing `+150`, keeping the dashboard alive.
  - **Live Volley Combo Meter (`combo-pod`)**: Energetic badge displaying `🔥 x3 COMBO` with pulsing glow.
  - **Live Level Timer (`timer-pod`)**: Digital stopwatch readout (`MM:SS`) tracking active play.

### 3.4 Lone Block Clutch Countdown & Option B Hyper-Beam Railgun
- **Clutch Countdown**: When exactly 1 block remains in the level (`remainingBlocks == 1`), `ArcadeGameManager` activates Clutch Countdown mode with a 12-second live timer and animated HUD status badge (`clutch-status-badge`).
- **Decaying Score Multiplier**: Hitting the lone final block while the clutch timer is active rewards a decaying score multiplier from $10\times$ down to $1\times$ based on remaining seconds ($\text{multiplier} = \text{clamp}(\lfloor\text{timeRemaining}\rfloor + 1, 1, 10)$).
- **Option B Hyper-Beam Railgun**: If the 12-second timer expires without hitting the block, the paddle engages emergency Railgun Overcharge. A wide vertical hyper-beam ($W \approx 2.4$, $H \approx 31$) erupts from the paddle's current $X$-coordinate to the arena ceiling. As the player steers the paddle across the arena, the sustained beam vaporizes the final block on contact, eliminating tedious stale-brick stalemates while maintaining full player agency.

---

## 4. End-of-Level Victory Scorecard & 3-Star Rating

### 4.1 End-of-Level Scorecard Modal (`modal-scorecard`)
Upon clearing a level, the game transitions into a victory sequence displaying a detailed frosted-glass scorecard:
1. **Level Title**: `LEVEL {N} COMPLETE!`
2. **Interactive 3-Star Rating**: 1, 2, or 3 glowing stars animate and pop into place with celebratory audio chimes (`PlayStarEarned`).
3. **Statistical Breakdown**:
   - **Blocks Destroyed**: Count and base points earned.
   - **Highest Volley Combo**: Peak streak achieved and bonus points.
   - **Clear Time vs. Par Time**: Elapsed time compared to par time.
   - **Time Bonus**: Remaining bonus pool points awarded ($\max(0, \text{timeBonusMax} - \lfloor\text{elapsed}\rfloor \times 35)$).
   - **Under-Par Speed Bonus**: $+500\text{ pts}$ if elapsed time $\le$ par time.
   - **Flawless Clear Bonus**: $+1,000\text{ pts}$ if completed without losing any lives in that level.
   - **Final Level Score**: Aggregated total.
4. **Player Action Buttons**:
   - `[ REPLAY ]`: Restarts current level to improve stars and clear time.
   - `[ NEXT LEVEL ]`: Advances to next level.
   - `[ MAIN MENU ]`: Returns to the main menu.

### 4.2 Star Ratings & Best Clear Times (`HighScoreManager.cs`)
- **1 Star (Bronze)**: Clear the level.
- **2 Stars (Silver)**: Exceed Level Silver score threshold.
- **3 Stars (Gold)**: Exceed Level Gold score threshold (requires combo execution, fast clear, or flawless life bonus).
- **Persistent Speedrun Records**:
  - Best Clear Time (`MM:SS`) is recorded and saved per level in `PlayerPrefs`.
  - Visualized on each level selection button in the Main Menu and Settings/Pause menus.

---

## 5. Powerups & Collectibles

| Modifier | Visual Indicator | Archetype | Mechanic Description |
| :--- | :--- | :--- | :--- |
| **Paddle Expander** | Cyan capsule & arrow icon | Power-up | 10-second timed buff widening paddle by $+10\%$ compounding ($W_n = W_{prev} \times 1.10$, clamped to max $12.0$). Spring overshoot animation on collection. Recalculates boundary clamps instantly. |
| **Score Multiplier 2X–5X** | Tiered capsule (Gold 2X, Amber 3X, Ruby 4X, Magenta 5X) | Multiplier Buff | Activates 10-second global score multiplier buff multiplying all block breaks by $2\times$, $3\times$, $4\times$, or $5\times$. |
| **Shield** | Electric blue capsule & shield icon | Defensive Power-up | Activates a 10-second defensive safety barrier at the arena bottom. Intercepts balls falling into the kill zone and redocks them onto the paddle without life penalty. |
| **Multi-Ball** | Neon magenta capsule & 3-ball icon | Kinetic Power-up | Spawns 2 extra balls at $\pm 35^\circ$ diverging angles with distinct trail colors. Points multiplied by active ball count. |
| **Extra Heart** | Neon pink capsule & heart icon | Recovery Power-up | Grants $+1$ life up to the hard cap of 5. Plays flying heart parabolic HUD animation. |
| **Laser Blaster** | Ruby red capsule & laser icon | Offensive Power-up | Deploys twin plasma laser cannons on paddle edges firing high-velocity laser bolts ($34\text{ units/s}$, ruby glow, SFX `AU_Powerup_Laser.mp3`) at $0.32\text{s}$ intervals for 10 seconds. Bolts deal standard damage on block impact (`TakeHit(Vector3.down)`). |
| **Glass-Enclosed** | Translucent 1.18x outer shell | Armored Brick | Encased in crystal glass (`MI_Block_Glass.mat`). Requires 2 hits: Hit 1 shatters glass shell with crystal debris; Hit 2 destroys base brick for $2\times$ points. |
| **Bomb Brick** | `BOMB` badge (`#FF3B30` Crimson) | Area Hazard | Detonates in a $2.5$-unit radius upon impact, triggering cascading destruction of adjacent bricks with compound chain multipliers. |

---

## 6. Progressive 15-Level Campaign Arc

| Level | Name | Silhouette & Archetype | Cols $\times$ Rows | Blocks | Speed | Modifiers Breakdown | Par Time | 3-Star Target |
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
- **Safe Area Insets**: Handled dynamically by `SafeAreaController.cs` for iPhone notches, Dynamic Island, and curved display boundaries.
- **Top Bar Pods**:
  - **Left Pod**: 5-slot Heart Life Gauge.
  - **Center Pod**: Large cumulative Score readout with real-time `score-delta-label` popups (`+150`).
  - **Timer Pod**: Sleek frosted glass pod with digital stopwatch time (`00:34`).
  - **Combo Pod**: Active volley streak meter (`🔥 x3 COMBO`) pulsing with neon cyan/magenta energy.
  - **Right Pod**: Minimal quick actions (Mute, Settings, Pause).
- **Level Selection Tabs**:
  - Dynamic `<ui:ScrollView>` in Main Menu and Settings modal displaying all 15 level cards.
  - Each card shows: Level Name, Archetype silhouette, 1–3 Star Rating, and Personal Best Clear Time.

---

## 8. Audio Architecture
- **Dedicated Sound Effects (`AU_`)**:
  - Deflection: `AU_Pop.mp3`
  - Brick Shatter: `AU_Break.mp3` with $+1$ semitone ascending pitch scaling per combo streak.
  - Combo Bank: `PlayComboBank` chime upon returning ball to paddle after a streak.
  - Star Tally: `PlayStarEarned` triumphant chimes for 1-star, 2-star, and 3-star scorecard reveals.
  - Powerups: `AU_Powerup.mp3`, `AU_Powerup_Shield.mp3`, `AU_Powerup_Laser.mp3`.
  - Explosions: `AU_Bomb_Explosion.mp3`, `AU_Glass_Break.mp3`.
  - Life & Fanfare: `AU_Life_Lost.mp3`, `AU_Level_Success.mp3`, `AU_Game_Over.mp3`.
- **Procedural Synthesizer Fallback**: Guaranteed offline acoustic synthesis for all audio events.

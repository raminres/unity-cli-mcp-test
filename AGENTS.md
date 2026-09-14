# Project Context & Architecture: BlockBreaker

This file provides persistent, high-density project context across agent sessions for this Unity project.

---

## 1. Project Overview & Identity
- **Product Name**: `BlockBreaker`
- **Company Name**: `RaminRasulzade`
- **Application / Bundle Identifier**: `com.RaminRasulzade.BlockBreaker` (iOS, Standalone, Android)
- **Engine Version**: Unity 6 (6000.6.0f1)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Play Mode Start Scene**: `Assets/Scenes/LV_BlockBreaker_MainMenu.unity` (configured via [PlayModeSceneSetup.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Editor/PlayModeSceneSetup.cs))
- **Remote Repository**: `https://github.com/raminres/unity-cli-mcp-test.git`
- **Active Branch**: `feature/session-highscores-and-laser-fixes` (based off `develop`, Git LFS enabled)

### Active Scenes & Build Index
1. `Assets/Scenes/LV_BlockBreaker_MainMenu.unity` (Build Index 0)
2. `Assets/Scenes/LV_BlockBreaker.unity` (Build Index 1)

---

## 2. Naming Conventions & Asset Presets
- **Materials**: `MT_` prefix for Master Materials (`Assets/Materials/MT_Master_PBR_URP.mat`), `MI_` prefix for Material Instances (`Assets/Materials/BlockBreaker/MI_*`).
- **Textures / Icons**: `TX_` prefix with semantic suffixes (`_BaseColor`, `_Normal`, `_MetallicSmoothness`, `_AO`, `_Emissive`). UI icons in `Assets/UI/Icons/`.
- **Audio**: `AU_` prefix (e.g. `AU_Pop.mp3`, `AU_Break.mp3`, `AU_Powerup.mp3`, `AU_Button_Press.mp3`, `AU_Level_Success.mp3`, `AU_Game_Over.mp3`, `AU_Life_Lost.mp3`, `AU_Powerup_Shield.mp3`).
- **ScriptableObjects**: `SO_` prefix (`Assets/Settings/Levels/SO_Level_*.asset`).
- **Presets**: `PR_` prefix in `Assets/Presets/` managed by `ProjectSettings/PresetManager.asset` and [SetupAssetPresets.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Editor/SetupAssetPresets.cs).

---

## 3. Core Gameplay Architecture
- **Inverted Stepped Pyramid Paddle & Dynamic Deflection**:
  - **3-Tier Geometry**:
    - Tier 1 (Top Strike Deck): $100\%$ width ($W = 5.0$), ultra-thin profile ($H = 0.24$, $Z = 1.0$) with glowing neon cyan rim ([MI_Paddle_Deck.mat](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Materials/BlockBreaker/MI_Paddle_Deck.mat)). Top surface preserved exactly at $Y = -6.0$ for consistent ball docking.
    - Tier 2 (Mid Chassis): Stepped inward to $72\%$ width ($W = 3.6$), height $H = 0.20$, $Z = 0.88$ in dark brushed titanium ([MI_Paddle.mat](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Materials/BlockBreaker/MI_Paddle.mat)).
    - Tier 3 (Bottom Keel / Thrusters): Stepped inward to $44\%$ width ($W = 2.2$), height $H = 0.16$, $Z = 0.72$ with engine vent glow ([MI_Paddle_Core.mat](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Materials/BlockBreaker/MI_Paddle_Core.mat)).
    - Total combined vertical profile: $0.60$ (reduced from legacy $1.0$ cube), with lower sides stepped inward by up to $1.4$ units on each side to eliminate phantom side catches.
  - **Physical Strike Collider & Contact Normal Guard**:
    - Primary `BoxCollider` is strictly fitted to Tier 1 ($H = 0.24$, center $Y = +0.38$). Below $Y = -6.24$, there is zero collision volume.
    - Upward Normal Threshold: `BallController.IsValidPaddleBounceNormal(normal)` (`normal.y >= 0.25f`) guarantees that balls brushing the side wall or hitting from below are NOT deflected upward, falling cleanly into the killzone.
  - **Optical Ray Deflection & Paddle Steering Formula**:
    - Deflection computed via `BallController.CalculatePaddleDeflection(inVelocity, hitOffset, steerStrength = 32f, minAngleDeg = 25f, maxAngleDeg = 155f, paddleVelocityX, velocityInfluence = 0.5f, verticalDeadzoneAngleDeg = 5f)`.
    - Preserves incoming horizontal momentum (`rayAngleDeg = Mathf.Atan2(|inVelocity.y|, inVelocity.x) * Rad2Deg`), naturally reflecting incoming vectors without unnatural direction reversal, applies subtle paddle steering based on contact offset `steer = -hitOffset * 32f`, incorporates paddle momentum transfer `clamp(-paddleVelocityX * 0.5f, -12°, +12°)`, strictly excludes near-vertical deadzone $[85^\circ, 95^\circ]$, and clamps final angle to $[25^\circ, 155^\circ]$.
  - **Anti-Trap Ball Physics (Options A3 & B3)**:
    - **Minimum Vertical Floor ($20^\circ$)**: In `FixedUpdate()`, `SanitizeTrajectory()` mathematically enforces $|v_y| \ge \text{currentSpeed} \cdot \sin(20^\circ)$, preventing shallow horizontal traps between arena walls while preserving exact speed and sign.
    - **Consecutive Side-Wall Steepener ($35^\circ$)**: Tracks `consecutiveSideWallBounces`. On $\ge 2$ consecutive side-wall hits without hitting paddle or block, steepens vertical trajectory to $\ge 35^\circ$ ($\sin(35^\circ) \approx 0.574$), forcing rapid vertical progression. Resets to 0 on block impact, paddle save, launch, or dock.
    - **Vertical Exclusion Deadzone ($[85^\circ, 95^\circ]$)**: Deflection angle and initial launch strictly exclude the $\pm 5^\circ$ vertical cone around $90^\circ$. Continuous play enforces $|v_x| \ge \text{currentSpeed} \cdot \sin(5^\circ)$, causing straight vertical trajectories to subtly drift away from center and eliminating endless vertical ping-pong loops.
    - **Continuous Perimeter & Top Corner $45^\circ$ Chamfers**: The playfield boundary forms a continuous polygonal frame. The horizontal ceiling `TopWall` is shortened to width $17.4$ ($X \in [-8.70, +8.70]$ at $Y = 24.25$), and vertical side walls `LeftWall`/`RightWall` are shortened to height $30.2$ ($Y \in [-7.50, +22.70]$ at $X = \pm 10.25$). Calibrated angled boundary wedges (`Chamfer_TopLeft` at $(-9.40, 23.40, 0)$ with $+45^\circ$ rotation and `Chamfer_TopRight` at $(9.40, 23.40, 0)$ with $-45^\circ$ rotation, scale $2.5 \times 0.5 \times 2.0$) seamlessly connect the top and side walls with zero corner gaps. Serialized directly in `Assets/Scenes/LV_BlockBreaker.unity`, configured in `SetupBlockBreakerScenes.cs`, and guaranteed at runtime via `LevelGenerator.EnsureCornerChamfers()`.
    - **Paddle Velocity Momentum Transfer**: `PaddleController.VelocityX` dynamically measures instantaneous horizontal movement velocity, imparting tactile directional steering when sweeping the paddle during impact.
  - **Compounding Expansion with Spring Overshoot**:
    - $+10\%$ per expander ($W_n = W_{prev} \times 1.10$, clamped to max $12.0$, adaptive bounds $[-10 + \frac{W}{2}, 10 - \frac{W}{2}]$).
    - Features spring-damper overshoot animation ($\approx +16\%$ overshoot with $Y$-axis squash-and-stretch settling over $0.35\text{s}$) for tactile arcade feedback.
- **Arena Dimensions**:
  - Top Wall: $Y = 24.25$, width $17.4$; Left/Right Walls: $X = \pm 10.25$, height $30.2$; Top Chamfers: $(\pm 9.40, 23.40)$, length $2.5$; Kill Zone: $Y = -9.0$ ([KillZone.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/KillZone.cs)).
  - Camera: Perspective $38^\circ$ FOV with [ResponsiveCameraController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/Core/ResponsiveCameraController.cs) dynamically adjusting $Z$-distance to guarantee 100% visible arena boundaries across any aspect ratio (16:9, 9:16, 9:19.5, etc.).
- **Lives & Hybrid Skill-Based Scoring**:
  - Starting lives: 3 (expandable up to max 5 via Extra Heart powerup).
  - Tiers: Red = 10 pts (bottom), Green = 20 pts (middle), Blue = 30 pts (top).
  - **Volley Combo Multiplier**:
    - Unreturned ball rallies increment streak: Hits 1–2 = $1\times$, Hits 3–4 = $2\times$, Hits 5–7 = $3\times$, Hits 8–10 = $4\times$, Hits 11+ = $5\times$ (MAX).
    - Safely banks into score upon paddle impact with audio chime; resets streak if ball falls into kill zone.
    - Ascending musical pitch scaling on consecutive break SFX ($+1$ semitone per hit up to $1.68\times$).
  - **Powerup & Chain Synergies**:
    - Bomb blasts apply compounding chain multipliers ($\text{base} \times 1.5^{\text{chainIndex}}$).
    - Multi-ball multiplies all points earned by live ball count ($2\times$ or $3\times$).
  - **Dual-Layer Real-Time Score Feedback**:
    - **World-Space Floating Popups (`FloatingScoreManager.cs`)**: Spawns at impact point at $Z = -0.8\text{f}$ (`+30`, `+150 x3!`, `+450 BOMB!`) drifting up $+1.2$ units over $0.65\text{s}$.
    - **HUD Dashboard Indicators**: Glowing score delta ticker (`score-delta-label`) popping `+150` next to score, live combo meter (`🔥 x3 COMBO`), and digital level stopwatch (`timer-pod`).
  - **End-of-Level Victory Scorecard & 3-Star Rating**:
    - Scorecard modal (`modal-scorecard`) tallies blocks destroyed, max volley combo, clear time vs par time, time bonus pool, under-par speed bonus (+500), and flawless life bonus (+1,000).
    - Awards 1–3 stars evaluated against level thresholds and records personal Best Clear Time in `HighScoreManager`. Interactive `Replay`, `Next Level`, and `Main Menu` actions.
  - **Session-Based High Scores**:
    - Continuous playthroughs across multiple levels (e.g. Level 1 $\to$ Level 2 $\to$ Level 3...) and Main Menu Save/Continue flows share a persistent `sessionId` (GUID).
    - `HighScoreManager.RecordScore(score, level, time, sessionId)` updates the single entry in-place for that session (`score = max`, `level = max`, total run elapsed time) rather than inserting duplicate distinct per-level rows, preserving a clean Top 10 leaderboard.

---

## 4. Level Layout System & Progressive 15-Level Campaign Arc
- **17 Layout Archetypes (`LevelLayoutType`)**:
  - Procedural Geometric Shapes: `FullGrid`, `Diamond`, `Pyramid`, `InvertedPyramid`, `Hourglass`, `Cross`, `HollowBox`, `Pillars`, `Stripes`, `CheckerboardEmpty`, `Heart`, `Invader`, `Shield`, `Chevron`, `Crown`, `Castle`.
  - Infinite Custom ASCII Mode: `Custom` mode parses multi-line text (`[TextArea]`) where `.`/` ` = empty cells, `X`/`#` = filled blocks (inheriting color patterns), and `B`/`G`/`R` = explicit color tiers.
  - Active Block Counting: `TotalBlocks` dynamically evaluates `HasBlockAt(r, c)`, ensuring special blocks are strictly mapped to active blocks and level clears cleanly.
  - Dynamic ScrollView UI: Level selector tabs in Main Menu and Level Settings use `<ui:ScrollView>` with programmatic button spawning, future-proof for adding new levels dynamically.


| Level | Name | Silhouette & Archetype | Cols $\times$ Rows | Blocks | Speed | Modifiers Breakdown |
| :---: | :--- | :---: | :---: | :---: | :---: | :--- |
| **1** | **First Flight** | `Pyramid` | $7 \times 3$ | **15** | `0.92x` (Paddle 5.5) | • 1x Paddle Expander |
| **2** | **Glass & Gold** | `Diamond` | $7 \times 6$ | **22** | `0.96x` (Paddle 5.2) | • 1x 2X, 2x Glass, 1x Expander |
| **3** | **Twin Pillars** | `Pillars` | $7 \times 6$ | **24** | `1.00x` (Paddle 5.0) | • 2x Bombs, 2x 2X, 1x Expander, 1x Laser (`Checkerboard`) |
| **4** | **Kinetic Shield** | `Shield` | $8 \times 6$ | **34** | `1.04x` (Paddle 5.0) | • 1x Shield, 1x Heart, 1x Bomb, 2x Glass |
| **5** | **Multi-Ball Ring** | `HollowBox` | $8 \times 6$ | **24** | `1.08x` (Paddle 5.0) | • 2x Multi-Ball, 1x Shield, 1x Bomb, 2x Glass (`Checkerboard`) |
| **6** | **Royal Crown** | `Crown` | $9 \times 6$ | **52** | `1.12x` (Paddle 5.0) | • 1x 3X, 2x 2X, 1x Heart, 1x Shield, 1x Multi-Ball, 2x Bombs, 3x Glass, 1x Laser |
| **7** | **Neon Heart** | `Heart` | $9 \times 6$ | **32** | `1.16x` (Paddle 5.0) | • 2x Hearts, 1x Shield, 1x 3X, 2x 2X, 1x Bomb, 2x Glass, 1x Multi-Ball |
| **8** | **Space Invader** | `Invader` | $9 \times 6$ | **28** | `1.20x` (Paddle 5.0) | • 2x Bombs, 2x 2X, 2x 3X, 1x Heart, 1x Shield, 1x Multi-Ball, 2x Glass, 1x Laser (`Randomized`) |
| **9** | **Crossfire** | `Cross` | $9 \times 6$ | **30** | `1.24x` (Paddle 5.0) | • 1x 4X, 2x 2X, 1x 3X, 2x Bombs, 3x Glass, 1x Heart, 1x Shield, 1x Multi-Ball (`Checkerboard`) |
| **10** | **The Hourglass** | `Hourglass` | $9 \times 6$ | **42** | `1.28x` (Paddle 5.0) | • 1x 4X, 2x 3X, 2x 2X, 2x Bombs, 3x Glass, 1x Heart, 1x Shield, 1x Multi-Ball |
| **11** | **Chevron Strike** | `Chevron` | $9 \times 6$ | **18** | `1.32x` (Paddle 5.0) | • 1x 4X, 2x 3X, 2x 2X, 2x Bombs, 3x Glass, 1x Heart, 1x Shield, 2x Multi-Balls, 1x Laser (`Checkerboard`) |
| **12** | **Castle Bastion** | `Castle` | $10 \times 6$ | **45** | `1.36x` (Paddle 5.0) | • 2x 4X, 2x 3X, 2x 2X, 3x Bombs, 4x Glass, 1x Heart, 2x Shields, 2x Multi-Balls |
| **13** | **Quantum Lattice** | `CheckerboardEmpty` | $10 \times 6$ | **30** | `1.40x` (Paddle 5.0) | • 1x 5X, 2x 4X, 2x 3X, 2x 2X, 3x Bombs, 4x Glass, 1x Heart, 2x Shields, 2x Multi-Balls (`Randomized`) |
| **14** | **Striped Vault** | `Stripes` | $10 \times 6$ | **30** | `1.44x` (Paddle 5.0) | • 2x 5X, 2x 4X, 2x 3X, 2x 2X, 3x Bombs, 4x Glass, 1x Heart, 2x Shields, 2x Multi-Balls, 1x Laser |
| **15** | **Chaos Labyrinth** | `Custom` | $10 \times 6$ | **48** | `1.48x` (Paddle 5.0) | • 2x 5X, 2x 4X, 2x 3X, 2x 2X, 4x Bombs, 4x Glass, 2x Hearts, 2x Shields, 2x Multi-Balls, 2x Lasers (`Randomized`) |

- **Dynamic Volley Pacing**: `BallController` measures continuous active volley time. Every 10 seconds of active play, speed escalates progressively ($+8\%$ step multiplier) up to `maxSpeed`, eliminating stale stalemates and ramping up intensity. Resets back to level base speed on dock or life lost.
- **Asset Storage**: `Assets/Settings/Levels/SO_Level_01.asset` through `SO_Level_15.asset`.
- **Automation**: [SetupBlockBreakerScenes.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Editor/SetupBlockBreakerScenes.cs) configures and wires all 15 presets into both scenes.

---

## 5. Powerups & Special Brick Archetypes
- **Collectible Powerup Drops ([PowerupCapsule.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/PowerupCapsule.cs))**:
  - Tactical powerups drop tumbling 3D collectible capsules ([MI_Powerup_Capsule.mat](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Materials/BlockBreaker/MI_Powerup_Capsule.mat)) that fall at $4.5\text{ units/s}$ with 3D rotational spin and vibrant neon emissive tint. Must be intercepted by the paddle to claim:
    - **Clean Decoupled Visual Hierarchy (Zero Duplicate Primitives)**:
      - Root container `Powerup_{type}` maintains `scale = 1.0` and never rotates, ensuring smooth translation and rock-solid trigger physics. Zero MeshRenderers or SpriteRenderers on the root container.
      - Child 1: `Visual_Capsule` (scaled up $2.1\times$ to $0.85 \times 0.85 \times 0.85$) contains exclusively `MeshFilter` and `MeshRenderer` (zero SpriteRenderers, zero colliders) and performs all 3D tumbling rotation independently.
      - Child 2: `Icon_Billboard` (scaled up $4.3\times$ to $0.95$, world width $\approx 1.22$ units) contains exclusively `SpriteRenderer` (`sortingOrder = 35`, zero meshes, zero colliders). Positioned at local $Z = -0.60\text{f}$ strictly in front of the capsule mesh, locked to `rotation = Quaternion.identity` in `LateUpdate()`. It NEVER rotates or gets lost behind the tumbling body.
      - Total across entire capsule hierarchy: exactly 1 MeshRenderer and exactly 1 SpriteRenderer.
    - **Cross-Platform iOS Metal Material Resolution**:
      - `GetOrCreateCapsuleMaterial()` guarantees zero pink/unshaded materials on iOS Metal builds by dynamically compiling a Universal Render Pipeline fallback material (`Universal Render Pipeline/Lit` or `Arcade/VFX_BlockDebris`) if editor asset database is stripped in standalone player builds.
      - `MI_Powerup_Capsule.mat` is serialized directly on `ArcadeGameManager` in the gameplay scene to force Unity's build asset pipeline to bundle the material and its URP shaders for iOS/macOS.
    - **Foreground Depth ($Z = -1.0\text{f}$)**: Capsules strictly fall in front of all brick rows ($Z=0$, spanning $[-0.5, +0.5]$), eliminating brick occlusion/clipping when dropped from top rows.
    - **Paddle Expander (`PaddleExpander`)**: Drops neon cyan capsule with arrow icon; catching triggers spring overshoot expansion ($+10\%$).
    - **Extra Heart (`ExtraHeart`)**: Drops radiant neon pink capsule with heart icon; catching grants $+1$ life (up to 5 max) with HUD parabolic flight animation.
    - **Shield (`Shield`)**: Drops electric blue capsule with shield icon; catching activates 10-second defensive barrier with HUD countdown.
    - **Multi-Ball (`MultiBall`)**: Drops neon magenta capsule with multi-ball icon; catching spawns 2 extra balls at $\pm 35^\circ$ diverging angles with distinct trail colors.
    - **Score Multipliers (`ScoreMultiplier2x`, `ScoreMultiplier3x`, `ScoreMultiplier4x`, `ScoreMultiplier5x`)**: Drops glowing gold (2X), fiery orange (3X), crimson (4X), or hyper-magenta (5X) capsule with extra points icon; catching activates a 10-second score multiplier buff on `ArcadeGameManager` with animated HUD status badge.
    - **Laser Blaster (`Laser`)**: Drops glowing ruby red capsule with twin gun blaster icon ([TX_Powerup_Gun.png](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/UI/Icons/TX_Powerup_Gun.png)); catching deploys twin plasma laser cannons mounted on paddle edges ([PaddleLaserController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/PaddleLaserController.cs)) firing high-velocity laser bolts ([LaserBolt.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/LaserBolt.cs), $34\text{ units/s}$, ruby glow, SFX [AU_Powerup_Laser.mp3](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Audio/AU_Powerup_Laser.mp3)) at $0.32\text{s}$ intervals for 10 seconds. Bolts deal standard damage on block impact (`TakeHit(Vector3.down)`).
    - **In-Flight Lifecycle & Docked Intercept Guard**:
      - `PowerupCapsule.ClearAllFallingCapsules()` automatically clears and destroys all active falling capsules upon life loss, shield deflection save, level clear, game over, and level advancement, preventing stale capsules from lingering into docked state or subsequent levels.
      - `PowerupCapsule.TryIntercept()` guards against collecting powerups while docked on the paddle (`ReadyToLaunch` or `BallLost`), ensuring balls cannot be triggered prematurely before player launch.
- **Lone Block Clutch Countdown & Option B Hyper-Beam Railgun**:
  - **Clutch Countdown**: When exactly 1 block remains in the level (`remainingBlocks == 1`), `ArcadeGameManager` activates Clutch Countdown mode with a 12-second live timer, accompanied by an animated HUD status badge (`clutch-status-badge`) displaying remaining seconds and current multiplier.
  - **Decaying Score Multiplier**: Hitting the lone final block while the clutch timer is active rewards a decaying score multiplier from $10\times$ down to $1\times$ based on remaining seconds ($\text{multiplier} = \text{clamp}(\lfloor\text{timeRemaining}\rfloor + 1, 1, 10)$).
  - **Option B Hyper-Beam Railgun**: If the 12-second timer expires without hitting the block, the paddle engages emergency Railgun Overcharge. A wide vertical hyper-beam ($W \approx 3.2$, $H \approx 31$) surges progressively upward from the paddle deck to the arena ceiling over $0.35\text{s}$, slicing through bricks as it extends. Rendered via a dedicated URP gradient shader `Assets/Shaders/VFX_LaserHyperBeam.shader` on a foreground Quad ($Z = -0.3$), featuring a brilliant white-hot central core, electric pink inner glow, neon ruby outer aura with soft lateral falloff, solar amber muzzle flare at the paddle deck, and animated plasma energy ripple.
  - **Dual-Cadence Level Clear Pacing & Celebratory Center Banner**:
    - **Celebratory Banner (`level-clear-banner`)**: Immediately upon clearing the final brick, a high-contrast golden neon center banner displays `"LEVEL CLEARED!"` with context-aware cyan subtext (`"STAGE COMPLETE!"`, `"FLAWLESS VICTORY!"`, or `"CLUTCH OVERCHARGE!"`).
    - **Dual Delay Cadence**:
      - Standard Non-Beam Clear (ball/bomb hits): Quick, snappy $0.8\text{s}$ delay (`standardClearDelaySeconds = 0.8f`) allowing the floating points and shatter bursts to resolve before the victory scorecard opens.
      - Clutch Hyper-Beam Railgun Clear: Full $1.4\text{s}$ delay (`levelClearDelaySeconds = 1.4f`) to preserve the progressive beam surge ($0.35\text{s}$) and clutch bonus flight into the score ticker.
    - Safeguarded against life loss or ball resets while clear is pending (`isLevelClearPending = true`). Banner is cleanly dismissed once the victory scorecard modal opens.
- **Immediate Environmental Modifiers**:
  - **Bomb Bricks (`Bomb`)**: Explosive radius detonation ($2.5$ units) immediately detonating surrounding bricks with outward impulses. Protected by `isDestroyed` flag against recursive loops.
  - **Glass-Enclosed Bricks (`GlassEnclosed`)**: Encased in a $1.18\times$ glass shell ([MI_Block_Glass.mat](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Materials/BlockBreaker/MI_Block_Glass.mat)). Requires 2 hits (Hit 1: shatters glass shell with crystal debris; Hit 2: breaks brick for $2\times$ points).
- **World Space Badges ([BlockBadge.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/BlockBadge.cs))**: Rendered via Unity 6 `PanelRenderer` in `WorldSpace` mode (`80px` fixed dimension, 100 PPU, clamped margins). Supports Laser badge icon (`badge-icon-laser`) and plate (`badge-plate-laser`).

---

## 6. Graphics, VFX & Performance Architecture
- **VFX System & Shaded URP Particle Bursts ([BlockVFXManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/BlockVFXManager.cs))**:
  - Employs dedicated URP-native particle burst shader `Assets/Shaders/VFX_ParticleBurst.shader` (`Arcade/VFX_ParticleBurst`) with material instance `Assets/Materials/BlockBreaker/MI_VFX_Burst.mat`, eliminating unshaded pink/magenta particle artifacts in Universal Render Pipeline.
  - **Off-Screen Pool Containment (`_Pool_VFX`)**: All pooled particle bursts (`VFX_Burst_Instance`) and tumbling debris pieces (`SubBox_Debris`) are parented under a dedicated `_Pool_VFX` container situated at $Y = -500\text{f}$ far off-screen. Instantiated disabled (`SetActive(false)` before component addition) and automatically cleared (`ParticleSystem.Clear()`) and returned to $(0, -500\text{f}, 0)$ on recycle, guaranteeing zero VFX instances linger or appear in the level playfield.
  - `PlayPowerupCollect(position, color)`: Emits 32 radiant spark particles matching the powerup's distinct neon emissive hue without spawning block debris cubes.
  - `PlayBlockShatter(position, color, normal)`: Emits 24 spark particles in block color accompanied by 8 physical 3D debris fragments.
  - Fail-safe runtime fallback: `GetOrCreateParticleMaterial()` guarantees that even if unassigned in test rigs, a safe URP particle material is automatically resolved.
- **Cross-Platform Frame-0 Prewarming ([BlockVFXManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/BlockVFXManager.cs))**:
  - Primes shaders and dispatches off-camera VFX simulation during `Start()` while in `ReadyToLaunch`. Forces Apple Metal, DX12, Vulkan, and WebGPU drivers to compile compute/raster PSOs upfront, eliminating first-hit hitching.
  - Zero-allocation `MaterialPropertyBlock` tinting: preserves 100% SRP Batcher compatibility without material cloning.
  - Struct-based simulation lists (`ActiveDebris`, `ActiveVFX`) replacing per-brick coroutines.
- **Cross-Platform Cosmic Gradient Background ([LevelBackgroundController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/LevelBackgroundController.cs))**:
  - Background Quad placed at $Z = 6.0\text{f}$ (comfortably behind arena walls $Z \in [-1, 1]$ and kill zone $Z \in [-2, 2]$), scaled to $40 \times 80$ to preserve the $1:2$ texture aspect ratio and frame the playfield cleanly.
  - Uses `MI_Background_Gradient.mat` (`Universal Render Pipeline/Unlit` with double-sided rendering).
  - Explicit default texture assigned (`TX_Background_Gradient_A.png`) ensuring immediate URP shader variant compilation on Apple Metal/iOS and preventing initial white flash.
  - Randomly selects and applies one of the four cosmic nebular gradients (`TX_Background_Gradient_A.png` through `TX_Background_Gradient_D.png`) via zero-allocation `MaterialPropertyBlock`.
  - Automatically randomizes on level generation (`LevelGenerator.GenerateLevel()`) avoiding consecutive repeats, providing a distinct atmosphere for each level.
- **iOS Debris Material**: Custom Universal Render Pipeline shader `Assets/Shaders/VFX_BlockDebris.shader` (`Arcade/VFX_BlockDebris`) preventing uncompiled pink shaders on Apple Metal.
- **Level Completion Reliability ([ArcadeGameManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/Core/ArcadeGameManager.cs))**:
  - `RecordBlockDestroyed()` counts blocks destroyed across all active and transitional states (`Playing`, `ReadyToLaunch`, `BallLost`), only suppressing records if `GameOver` or `LevelClear`.
  - `CheckLevelCompletion()` integrates a fallback physical inspection of `LevelGenerator.BlocksContainer`. If all active blocks are cleared (`childCount == 0` or all `isDestroyed`), level victory triggers automatically, guaranteeing the player is never trapped on a cleared level.

---

## 7. UI Toolkit & Audio Systems
- **PanelRenderer Migration & Runtime Binding**:
  - Native Unity 6 `PanelRenderer` on `UI_HUD` and `UI_MainMenu`.
  - Due to `RegisterUIReloadCallback` firing only during editor asset reloads and not standard runtime startup, `ArcadeUIManager`, `MainMenuUIManager`, and `SafeAreaController` implement `EnsureInitialized()` with fallback reflection (`rootVisualElement` / `containerPanel.visualTree`), guaranteeing 100% reliable frame-0 UI binding.
  - Script Execution Order: `[DefaultExecutionOrder(-100)]` on `ArcadeGameManager` and `[DefaultExecutionOrder(-90)]` on `ArcadeInputHandler` guarantees manager singletons and event broadcasters are initialized before scene entities (`BallController`, `PaddleController`).
  - Pointer Event Pass-Through: Floating HUD overlays (e.g. `launch-banner`) use `picking-mode="Ignore"` and `pointer-events: none` to prevent blocking touch and click events to underlying controls.
- **Safe Area Controller ([SafeAreaController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/UI/SafeAreaController.cs))**: Dynamically resolves device safe area insets directly to child roots (`hud-root`, `root-container`), ensuring the top HUD bar clears notches and Dynamic Island.
- **HUD & Modal Controls**:
  - Detached floating frosted glass pods: Hearts/Lives left-aligned, Current Score centered (single readout, clean typography), Minimal Quick Action pod right-aligned (Mute, Settings, Pause).
  - High Scores Leaderboard: Dedicated top 10 scores modal with rank 1 (Gold), rank 2 (Silver), and rank 3 (Bronze) medal icons, formatted dates, and full reset capability in both Main Menu and Pause Menu ([HighScoreManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/Core/HighScoreManager.cs)).
  - How to Play: Quick-reference card-based gameplay guide in Main Menu and Pause Menu covering controls, dynamic deflection, block tier scoring, glass 2-hit durability, and all powerups.
  - Interactive Credits: Dedicated modal citing Unity MCP, Gemini, Antigravity, and Ramin Rasulzade, with clickable URL buttons routing to Email (`mailto:`), personal Website, LinkedIn, and GitHub via `Application.OpenURL`.
  - Scaled UI & Ergonomic Touch Targets: +10%–15% size boost across all HUD pods, badges, and modals; touch targets $\ge 54\text{px}$–$64\text{px}$ with generous spacing tailored for high-density iPhone displays ($2778 \times 1284$).
  - Typography: `FT_Montserrat` for titles, headers, and score readout; `FT_Inter` for body copy, badges, and buttons.
  - Launch Suppression: Touch release / click suppression on modal dismissal and unpause prevents balls docked in `ReadyToLaunch` from auto-launching.
  - Heart icon life indicator (`TX_Heart_Fill.png` / `TX_Heart_Empty.png`).
  - Quick action bar: Pause/Play dynamic icon swap, Settings 360° compounding mechanical spin, Volume/Mute toggle with custom icons.
  - 2-row responsive wrapped tabs for all 7 levels (`LVL 1`–`LVL 4` top, `LVL 5`–`LVL 7` bottom) with $>80\text{px}$ touch targets.
  - Live interactive tuning sliders in Level Settings modal.
- **Audio System ([ArcadeAudioManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/Audio/ArcadeAudioManager.cs))**:
  - Persistent singleton wired with dedicated audio clips and fallback procedural synthesis for full offline safety.

---

## 8. Cross-Platform Build Configurations
- **iOS & macOS Xcode**:
  - Swift Xcode Project: `PlayerSettings.xcodeProjectType = XcodeProjectType.Swift`, targeting modern Swift lifecycle (`MainApp.swift`).
  - Build Profile: `Assets/Settings/Build Profiles/iOS.asset`.
  - Remote branch `develop`. macOS environment used for iOS device compilation.
  - **Sequential Subfolder Build System ([BuildVersionUtility.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/Core/BuildVersionUtility.cs), [SetupIOSBuildProfile.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Editor/SetupIOSBuildProfile.cs))**:
    - Build outputs are automatically routed into sequential versioned subfolders under `Builds/BlockBreakerBuilds/build_{N}` (e.g. `build_1`, `build_2`, `build_3`), preventing iterative builds on macOS from overwriting previous exports.
    - Automatic Folder & Number Detection: Scans `Builds/BlockBreakerBuilds` for existing folder variations (`_build1`, `build_2`, `build3`, `_build_4`, `v5`), parses the highest existing index, compares against current `PlayerSettings.iOS.buildNumber`, and advances strictly forward to $\max(K, B) + 1$.
    - Version Synchronization: Automatically iterates `PlayerSettings.iOS.buildNumber` (CFBundleVersion) and advances marketing patch version `PlayerSettings.bundleVersion` (`0.1.0` -> `0.1.1` -> `0.1.2`), calling `AssetDatabase.SaveAssets()` on each build.
    - Interactive & CLI Access: Available via `Tools > Arcade > Build Xcode Project`, `Tools > Arcade > Prepare Next iOS Build Folder & Version`, and programmatic `SetupIOSBuildProfile.PrepareNextBuildDirectory()`.
- **WebGPU / WebGL**: Configured in `PlayerSettings` as primary graphics API.

---

## 9. Ball Trail & Visual Effects Architecture
- **Dual-Layer Stacked Trail ([BallTrail.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/BallTrail.cs))**:
  - Layer 1 (Outer Halo): Wider tapering ribbon ($0.55$ start width, $0.22\text{s}$ lifetime, $40\%$ alpha) in rich base hue.
  - Layer 2 (Inner Core): Narrower, high-opacity hot core ($0.25$ start width, $0.16\text{s}$ lifetime, $85\%$ alpha) with $+65\%$ white blend for intense luminous depth.
  - Custom quadratic decay curves smoothly taper width to zero without harsh terminal truncation.
  - Zero-allocation `MaterialPropertyBlock` syncs the ball mesh base color and emissive glow to the trail hue.
  - Multi-Ball Differentiation: Primary ball emits Electric Cyan (`#00F2FF`); secondary multi-balls receive distinct high-contrast neon tints (Magenta `#FF0099`, Gold `#FFCC00`, Neon Lime `#00FF66`).
  - Emitting state is tightly coupled to ball lifecycle: clears and silences when docked in `ReadyToLaunch` or paused, emits during play.

---

## 10. Automated Test Suite
- **Location**: `Assets/Tests/BlockBreakerCoreTests.cs`
- **Total Tests**: **133 passing tests (100%)**, executing in ~210ms.
- **Coverage**: Cosmic gradient background randomization, consecutive repeat avoidance, clamped indexing, level generation triggers, gameplay scene background placement, scoring multipliers (2X, 3X, 4X, 5X), optical ray paddle deflection math & forward momentum preservation, boundary clamping, life tracking, heart UI transitions, safe area insets, aspect-ratio frustum framing, compounding paddle widening, stepped pyramid geometry & tier ratios, spring overshoot expansion animation, powerup capsule foreground depth ($Z = -1.0\text{f}$), billboard camera-facing icon lock, decoupled visual tumbler hierarchy & enlarged scales ($0.85$ capsule / $0.95$ icon), exact single mesh and single sprite structure without duplicate primitives, cross-platform capsule URP material fallback resolution on iOS Metal, powerup capsule collection (PaddleExpander, ExtraHeart, Shield, 2X, 3X, 4X, 5X Multipliers), in-flight falling capsule clearing on life lost (`ClearAllFallingCapsules`), docked intercept prevention (`TryIntercept`), contact normal validation (`IsValidPaddleBounceNormal`), extended paddle collider depth, audio persistence, debris shader resolution, particle burst shader resolution (`Arcade/VFX_ParticleBurst`), dedicated powerup collection bursts without debris sub-boxes, off-screen pooled VFX instance containment (`_Pool_VFX` at $Y = -500\text{f}$), pause lifecycle, launch suppression window, direct touch controls, bomb radius blast, glass 2-hit durability, shield countdown & killzone intercept, multi-ball death tolerance, multi-ball distinct trail color assignment, dual-layer trail creation and curve decay, 15-level campaign existence, 17 layout archetypes (Diamond, Pyramid, Hourglass, Cross, HollowBox, Pillars, Stripes, Checkerboard, Heart, Invader, Shield, Chevron, Crown, Castle, Custom), custom ASCII pattern parsing with explicit color tier markers, snappier initial speeds (Levels 1–3) and strictly ascending progression, dynamic volley speed escalation over elapsed play intervals, fallback empty blocks container level completion, HighScoreManager sorting/clamping/resetting, sequential build directory regex pattern matching (`_build1`, `build_2`, `build3`), version string patch incrementation, existing folder collision avoidance, and PlayerSettings iOS buildNumber / bundleVersion automated preparation.


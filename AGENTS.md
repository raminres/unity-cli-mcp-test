# Project Context & Architecture: BlockBreaker

Persistent, high-density technical context for BlockBreaker.

---

## 1. Project Overview & Identity
- **Product / Bundle ID**: `BlockBreaker` / `com.RaminRasulzade.BlockBreaker`
- **Engine / Pipeline**: Unity 6 (`6000.6.0f1`), Universal Render Pipeline (`URP 17.6.0`)
- **Scenes**: 
  1. `Assets/Scenes/LV_BlockBreaker_MainMenu.unity` (Build Index 0, Start Scene configured via [PlayModeSceneSetup.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Editor/PlayModeSceneSetup.cs))
  2. `Assets/Scenes/LV_BlockBreaker.unity` (Build Index 1, Primary Gameplay)
- **Repository**: `https://github.com/raminres/unity-cli-mcp-test.git` (Active Branch: `fix/laser-gun-icon-and-lingering-issues`, Git LFS enabled)

---

## 2. Naming Conventions & Asset Presets
- **Materials**: `MT_Master_PBR_URP.mat` (Master), `MI_*` (Instances in `Assets/Materials/BlockBreaker/`).
- **Textures / Icons**: `TX_` prefix with semantic suffixes (`_BaseColor`, `_Normal`, `_Emissive`). UI icons in `Assets/UI/Icons/`.
- **Audio**: `AU_*` (e.g. `AU_Pop`, `AU_Break`, `AU_Powerup`, `AU_Powerup_Laser`, `AU_Powerup_Shield`, `AU_Life_Lost`, `AU_Level_Success`, `AU_Game_Over`).
- **ScriptableObjects**: `SO_*` (`Assets/Settings/Levels/SO_Level_*.asset`, `Assets/Settings/SO_PowerupIcons.asset`).
- **Presets**: `PR_*` in `Assets/Presets/` managed by `PresetManager.asset` and [SetupAssetPresets.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Editor/SetupAssetPresets.cs).

---

## 3. Core Gameplay Architecture
- **Inverted Stepped Pyramid Paddle & Physical Geometry**:
  - **3-Tier Profile**:
    - Tier 1 (Top Strike Deck): $100\%$ width ($W = 5.0$), ultra-thin ($H = 0.24, Z = 1.0$), glowing neon cyan rim ([MI_Paddle_Deck.mat](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Materials/BlockBreaker/MI_Paddle_Deck.mat)). Top surface preserved at $Y = -6.0$ for consistent ball docking.
    - Tier 2 (Mid Chassis): Stepped to $72\%$ width ($W = 3.6, H = 0.20, Z = 0.88$) in dark brushed titanium ([MI_Paddle.mat](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Materials/BlockBreaker/MI_Paddle.mat)).
    - Tier 3 (Keel / Thrusters): Stepped to $44\%$ width ($W = 2.2, H = 0.16, Z = 0.72$) with engine vent glow ([MI_Paddle_Core.mat](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Materials/BlockBreaker/MI_Paddle_Core.mat)).
    - Total vertical profile: $0.60$ (down from legacy $1.0$). Lower sides stepped inward $\le 1.4\text{u}$ per side to eliminate phantom side catches.
  - **Collider & Normal Guard**:
    - Primary `BoxCollider` strictly fitted to Tier 1 ($H = 0.24$, center $Y = +0.38$). Below $Y = -6.24$, zero collision volume exists.
    - `BallController.IsValidPaddleBounceNormal(normal)` (`normal.y >= 0.25f`) guarantees brushing balls fall cleanly into killzone.
  - **Optical Ray Deflection, Paddle Friction & Steering Formula**:
    - Computed via `BallController.CalculatePaddleDeflection(inVelocity, hitOffset, steerStrength = 32f, minAngleDeg = 25f, maxAngleDeg = 155f, paddleVelocityX, velocityInfluence = 1.5f, verticalDeadzoneAngleDeg = 5f, maxVelocitySteerDeg = 45f)`.
    - Preserves forward momentum on stationary paddle (`rayAngleDeg = Mathf.Atan2(|inVelocity.y|, inVelocity.x) * Rad2Deg`), applies offset steering `steer = -hitOffset * 32f`, applies paddle tangential velocity steering `clamp(-paddleVelocityX * 1.5f, -45°, +45°)`. A deliberate swipe opposite to ball direction reverses horizontal momentum across $90^\circ$ (reversal cut/slice). Strictly excludes vertical deadzone $[85^\circ, 95^\circ]$, clamped to $[25^\circ, 155^\circ]$.
    - **Kinetic Speed Pop ("Active Strike")**: Striking with moving paddle ($|V_x| \ge 3.5\text{ u/s}$) triggers $+8\%$ speed impulse (capped at `maxSpeed = 22f`), accompanied by snappier high-pitch pop audio (`pitch = 1.22f`), deep squash recoil ($18\%$), and electric cyan spark burst via `BlockVFXManager.PlayPaddleHitSpark`. Stationary paddle retains constant speed for cushion control.
  - **Anti-Trap Ball Physics**:
    - **Minimum Vertical Floor ($20^\circ$)**: In `FixedUpdate()`, enforces $|v_y| \ge \text{currentSpeed} \cdot \sin(20^\circ)$, preventing shallow horizontal traps.
    - **Consecutive Side-Wall Steepener ($35^\circ$)**: $\ge 2$ consecutive wall bounces steepens trajectory to $\ge 35^\circ$ ($\sin(35^\circ) \approx 0.574$). Resets on block hit, paddle save, launch, or dock.
    - **Vertical Exclusion Deadzone ($[85^\circ, 95^\circ]$)**: Eliminates vertical loops. Continuous play enforces $|v_x| \ge \text{currentSpeed} \cdot \sin(5^\circ)$.
    - **Continuous Perimeter & $45^\circ$ Top Chamfers**: Polygonal frame joins shortened top wall ($W = 17.4$ at $Y = 24.25$) and side walls ($H = 30.2$ at $X = \pm 10.25$) via wedges at $(\pm 9.40, 23.40, 0)$ (scale $2.5 \times 0.5 \times 2.0$, rotation $\pm 45^\circ$). Configured in `LV_BlockBreaker.unity` and guaranteed at runtime via `LevelGenerator.EnsureCornerChamfers()`.
  - **Paddle Compounding Expansion**:
    - $+10\%$ per expander ($W_n = W_{prev} \times 1.10$, max $12.0$, bounds $[-10 + \frac{W}{2}, 10 - \frac{W}{2}]$) with spring-damper overshoot animation ($\approx +16\%$ over $0.35\text{s}$).
- **Arena Dimensions & Camera**:
  - Top Wall: $Y = 24.25, W = 17.4$; Side Walls: $X = \pm 10.25, H = 30.2$; Top Chamfers: $(\pm 9.40, 23.40)$, length $2.5$; Kill Zone: $Y = -9.0$ ([KillZone.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/KillZone.cs)).
  - Camera: Perspective $38^\circ$ FOV with [ResponsiveCameraController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/Core/ResponsiveCameraController.cs) dynamically adjusting $Z$-distance for 100% boundary visibility across all aspect ratios (16:9, 9:16, 9:19.5).
- **Lives & Scoring Architecture**:
  - Lives: 3 starting (max 5 via Extra Heart). Tiers: Red = 10 pts, Green = 20 pts, Blue = 30 pts.
  - **Model A Color Physical Interactions & Audio Character**:
    - 🔴 **Red (Tier 1, 10 pts)**: Kinetic Dampener / Brake. Absorbs impact energy, slowing ball velocity by $-1.2\text{ u/s}$ down to floor `baseSpeed = 14f`. Accompanied by heavy low-pitch break audio (`pitch = 0.90f`).
    - 🟢 **Green (Tier 2, 20 pts)**: Kinetic Turbo / Spring Bumper. Imparts snappy $+10\%$ speed boost impulse up to ceiling `maxSpeed = 22f`. Accompanied by bright mid-high pitch break audio (`pitch = 1.10f`).
    - 🔵 **Blue (Tier 3, 30 pts)**: Optical Prism Deflector / Scatter. Induces chaotic optical refraction by rotating exit angle by $\pm 18^\circ$ to $\pm 28^\circ$, breaking repetitive trajectory loop deadlocks while enforcing trajectory sanitization. Accompanied by crystalline high chime break audio (`pitch = 1.25f`).
  - **Volley Combo Multiplier**:
    - Unreturned rallies: Hits 1–2 ($1\times$), 3–4 ($2\times$), 5–7 ($3\times$), 8–10 ($4\times$), 11+ ($5\times$ MAX).
    - Banks on paddle hit; resets on killzone drop.
    - Top HUD combo badge dynamically binds multiplier icon (`TX_Powerup_Extra_Points.png` / `PowerupIconSet.MultiplierSprite`) alongside streak (`x{N} COMBO`).
    - When combo ends/banks, displays `"COMBO ENDED"` text notification in combo badge for $1.2\text{s}$ before hiding. Break SFX scales $+1$ semitone per hit (up to $1.68\times$).
  - **Synergies**: Bomb chains ($\text{base} \times 1.5^{\text{chainIndex}}$), Multi-Ball ($2\times$ or $3\times$ points while multiple balls live).
  - **Real-Time Feedback**:
    - World-Space Floating Popups ([FloatingScoreManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/FloatingScoreManager.cs)): Spawns at $Z = -0.8\text{f}$ (`+30`, `+150 x3!`), floats $+1.2\text{u}$ over $0.65\text{s}$.
    - HUD Indicators: Score delta ticker (`score-delta-label`), live combo badge, digital stopwatch (`timer-pod`).
  - **End-of-Level Scorecard & Session High Scores**:
    - Modal (`modal-scorecard`) tallies blocks destroyed, peak combo, elapsed time vs par time, under-par speed bonus (+500 pts), and flawless life bonus (+1,000 pts). Awards 1–3 stars and stores Best Clear Time in `HighScoreManager`.
    - Session-based GUID tracks multi-level runs in-place, keeping a clean Top 10 leaderboard.

---

## 4. Level Layout System & Progressive 15-Level Campaign Arc
- **17 Layout Archetypes (`LevelLayoutType`)**:
  - Shapes: `FullGrid`, `Diamond`, `Pyramid`, `InvertedPyramid`, `Hourglass`, `Cross`, `HollowBox`, `Pillars`, `Stripes`, `CheckerboardEmpty`, `Heart`, `Invader`, `Shield`, `Chevron`, `Crown`, `Castle`.
  - Infinite Custom ASCII: `Custom` mode parses multi-line text (`.`/` ` = empty, `X`/`#` = block, `B`/`G`/`R` = tier).
  - Dynamic ScrollView: Main Menu & Settings tabs use dynamic `<ui:ScrollView>` with programmatic card instantiation.
- **Enriched Arena Density & Side Flank Bumper Blocks**:
  - Grid widths expanded from legacy 7–10 columns to **11–14 columns** ($13.75\text{u}$ to $17.5\text{u}$ grid span), extending bricks to within $1.6\text{u}$ of side walls ($X = \pm 10.25$).
  - `IncludeSideFlanks` enabled across campaign levels, inserting outer bumper bricks at `c = 0` and `c = cols - 1` along mid-section rows to intercept ball traversal and eliminate empty side highways.

| Level | Name | Archetype | Grid | Blocks | Speed | Modifiers Breakdown | Par | 3-Star Target |
| :---: | :--- | :---: | :---: | :---: | :---: | :--- | :---: | :---: |
| **1** | **First Flight** | `Pyramid` | $11 \times 6$ | **42** | `0.92x` | • 1x Expander | 35s | 1,200 pts |
| **2** | **Glass & Gold** | `Diamond` | $12 \times 6$ | **42** | `0.96x` | • 1x 2X, 2x Glass, 1x Expander | 40s | 1,800 pts |
| **3** | **Twin Pillars** | `Pillars` | $13 \times 6$ | **42** | `1.00x` | • 2x Bombs, 2x 2X, 1x Expander, 1x Laser | 45s | 2,400 pts |
| **4** | **Kinetic Shield** | `Shield` | $13 \times 6$ | **62** | `1.04x` | • 1x Shield, 1x Heart, 1x Bomb, 2x Glass | 50s | 3,200 pts |
| **5** | **Multi-Ball Ring** | `HollowBox` | $13 \times 6$ | **34** | `1.08x` | • 2x Multi-Ball, 1x Shield, 1x Bomb, 2x Glass | 45s | 3,800 pts |
| **6** | **Royal Crown** | `Crown` | $13 \times 6$ | **70** | `1.12x` | • 1x 3X, 2x 2X, 1x Heart, 1x Shield, 1x Multi-Ball, 2x Bombs, 3x Glass, 1x Laser | 60s | 5,000 pts |
| **7** | **Neon Heart** | `Heart` | $13 \times 6$ | **52** | `1.16x` | • 2x Hearts, 1x Shield, 1x 3X, 2x 2X, 1x Bomb, 2x Glass, 1x Multi-Ball | 50s | 4,200 pts |
| **8** | **Space Invader** | `Invader` | $13 \times 6$ | **34** | `1.20x` | • 2x Bombs, 2x 2X, 2x 3X, 1x Heart, 1x Shield, 1x Multi-Ball, 2x Glass, 1x Laser | 45s | 4,500 pts |
| **9** | **Crossfire** | `Cross` | $13 \times 6$ | **48** | `1.24x` | • 1x 4X, 2x 2X, 1x 3X, 2x Bombs, 3x Glass, 1x Heart, 1x Shield, 1x Multi-Ball | 50s | 5,200 pts |
| **10** | **The Hourglass** | `Hourglass` | $13 \times 6$ | **56** | `1.28x` | • 1x 4X, 2x 3X, 2x 2X, 2x Bombs, 3x Glass, 1x Heart, 1x Shield, 1x Multi-Ball | 55s | 6,000 pts |
| **11** | **Chevron Strike** | `Chevron` | $13 \times 6$ | **38** | `1.32x` | • 1x 4X, 2x 3X, 2x 2X, 2x Bombs, 3x Glass, 1x Heart, 1x Shield, 2x Multi-Balls, 1x Laser | 45s | 4,800 pts |
| **12** | **Castle Bastion** | `Castle` | $14 \times 6$ | **59** | `1.36x` | • 2x 4X, 2x 3X, 2x 2X, 3x Bombs, 4x Glass, 1x Heart, 2x Shields, 2x Multi-Balls | 60s | 7,000 pts |
| **13** | **Quantum Lattice** | `CheckerboardEmpty` | $14 \times 6$ | **45** | `1.40x` | • 1x 5X, 2x 4X, 2x 3X, 2x 2X, 3x Bombs, 4x Glass, 1x Heart, 2x Shields, 2x Multi-Balls | 50s | 6,500 pts |
| **14** | **Striped Vault** | `Stripes` | $14 \times 6$ | **44** | `1.44x` | • 2x 5X, 2x 4X, 2x 3X, 2x 2X, 3x Bombs, 4x Glass, 1x Heart, 2x Shields, 2x Multi-Balls, 1x Laser | 55s | 7,200 pts |
| **15** | **Chaos Labyrinth** | `Custom` | $14 \times 6$ | **66** | `1.48x` | • 2x 5X, 2x 4X, 2x 3X, 2x 2X, 4x Bombs, 4x Glass, 2x Hearts, 2x Shields, 2x Multi-Balls, 2x Lasers | 65s | 8,500 pts |

- **Volley Pacing**: Every 10s of active rally, ball speed escalates $+8\%$ up to `maxSpeed`. Resets on dock or life lost.
- **Storage & Wiring**: `Assets/Settings/Levels/SO_Level_01.asset` through `SO_Level_15.asset`. Configured via [SetupBlockBreakerScenes.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Editor/SetupBlockBreakerScenes.cs).

---

## 5. Powerups, Hazards, Clutch Hyper-Beam & Level Clear Pacing
- **Collectible Drops ([PowerupCapsule.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/PowerupCapsule.cs))**:
  - Fall speed $4.5\text{ u/s}$, tumbling 3D shapes at $Z = -1.0\text{f}$ in front of bricks.
  - **Differentiated 3D Geometries**:
    - **Powerups**: Rounded tumbling 3D capsule (`PrimitiveType.Capsule`, scale $0.85$, positive cyan emissive glow `#00f2fe`).
    - **Powerdowns / Hazards**: Sharp, faceted tumbling 3D diamond (`PrimitiveType.Cube` rotated $45^\circ$ on all axes via `Quaternion.Euler(45f, 45f, 45f)`, scale $0.72$, warning crimson emissive glow `#ff1744`).
  - Hierarchy: Root container (scale $1.0$, zero renderers/colliders), Child 1 `Visual_Capsule` (mesh renderer), Child 2 `Icon_Billboard` (scale $0.95$, `SpriteRenderer`, $Z = -0.6\text{f}$, locked to identity rotation). Exactly 1 mesh and 1 sprite renderer.
  - Lifecycle: `ClearAllFallingCapsules()` destroys drops on life loss, shield save, level clear, game over, or advancement. `TryIntercept()` blocks collection while docked.
  - **Positive Powerup Buffs (`#00f2fe`)**:
    - Expander (cyan paddle widening $+10\%$, max $12.0$), Extra Heart (pink life $+1$), Shield (blue, 10s killzone safety net), Multi-Ball (magenta, 2 extra balls at $\pm 35^\circ$), Score Multipliers (gold 2X, orange 3X, crimson 4X, magenta 5X), Laser Blaster (ruby, twin cannons at $34\text{ u/s}$ for 10s).
  - **Negative Powerdown Debuffs (`#ff1744`)**:
    - **Paddle Shortener** (`PaddleShortener`): Shrinks paddle width by $-18\%$ (clamped to `MIN_PADDLE_WIDTH = 2.4f`) for 10s.
    - **Paddle Slower** (`PaddleSlower`): Induces sluggish input drag and inertia for 8s.
    - **Brick Freezer** (`BrickFreezer`): Encites up to 5 field bricks in glacial ice; frozen bricks absorb 1 defrost hit before shattering.
    - **Ball Size Decreaser** (`BallSizeDecreaser`): Shrinks ball radius to $60\%$ ($0.8\text{u} \to 0.48\text{u}$) for 10s.
    - **Ball Slower** (`BallSlower`): Slows ball velocity to $9.5\text{ u/s}$ for 8s.
    - **Paddle Freezer** (`PaddleFreezer`): Immobilizes paddle for $1.2\text{s}$. Moving while frozen triggers a high-frequency sinusoidal struggle/tremor animation (`Mathf.Sin(Time.time * 45f) * 0.07f`) demonstrating active physical freeze rather than unresponsive input.
  - Environmental: Bomb Bricks (radius $2.5\text{u}$ cascade), Glass-Enclosed Bricks (2-hit armored crystal).
- **Lone Block Clutch Countdown & Option B Hyper-Beam Railgun**:
  - When 1 brick remains, activates 12s timer with decaying multiplier ($10\times \to 1\times$).
  - On timer expiration, emergency Railgun Overcharge fires a wide vertical hyper-beam ($W \approx 3.2, H \approx 31$) surging from paddle strike deck to arena ceiling over $0.65\text{s}$ (`beamSurgeDuration = 0.65f`). Rendered via URP shader `Arcade/VFX_LaserHyperBeam` with `MI_LaserHyperBeam.mat` and `TX_LaserHyperBeam_Gradient.png`.
- **Level Clear Delay & Entity Freeze**:
  - Golden celebratory center banner (`level-clear-banner`) displays `"LEVEL CLEARED!"` with context subtext.
  - Dual Delay: $1.0\text{s}$ standard clear, $1.5\text{s}$ hyper-beam clear before scorecard modal opens.
  - Entity Freeze: All balls freeze velocity (`BallController.FreezeBall()`, `ArcadeGameManager.FreezeAllBalls()`), paddle movement input is locked, falling capsules and laser bolts freeze.
- **Inspector-Assigned Sprites & HUD Timers ([PowerupIconSet.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/PowerupIconSet.cs))**:
  - All powerup and powerdown sprites managed in ScriptableObject `Assets/Settings/SO_PowerupIcons.asset` and assigned in C# via `new StyleBackground(sprite)`.
  - Top center HUD indicators style powerdown timers with glowing crimson borders (`.powerdown-badge`) and timer countdowns (`.powerdown-timer-label`).

---

## 6. Graphics, VFX & Performance Architecture
- **VFX System ([BlockVFXManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/BlockVFXManager.cs))**:
  - Native URP particle burst shader `Arcade/VFX_ParticleBurst` with `MI_VFX_Burst.mat`.
  - Pooled bursts and debris parented under off-screen `_Pool_VFX` at $Y = -500\text{f}$.
  - Zero-allocation `MaterialPropertyBlock` tinting retains 100% SRP Batcher compatibility.
  - Frame-0 shader prewarming in `Start()` primes GPU PSOs upfront on Apple Metal, WebGPU, Vulkan, DX12.
- **Cosmic Gradient Background ([LevelBackgroundController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/BlockBreaker/LevelBackgroundController.cs))**:
  - Background Quad at $Z = 6.0\text{f}$ (scale $40 \times 80$) using `Universal Render Pipeline/Unlit`.
  - Randomly selects from 4 cosmic gradients (`TX_Background_Gradient_A.png` to `_D.png`) per level.

---

## 7. UI Toolkit & Audio Systems
- **UI Toolkit Runtime Binding**:
  - Unity 6 `PanelRenderer` on `UI_HUD` and `UI_MainMenu`. `EnsureInitialized()` guarantees reliable frame-0 binding.
  - Execution Order: `ArcadeGameManager` (`-100`), `ArcadeInputHandler` (`-90`).
  - Overlay pass-through: `picking-mode="Ignore"` and `pointer-events: none` on prompt banners.
- **Safe Area Controller ([SafeAreaController.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/UI/SafeAreaController.cs))**:
  - Resolves screen safe area insets to child roots, clearing iPhone Dynamic Island and notches.
- **HUD & Modal Ergonomics**:
  - Detached frosted glass pods with touch targets $\ge 54\text{px}$. Typography: `FT_Montserrat` (headers, score), `FT_Inter` (body, buttons).
  - High scores leaderboard modal, card-based gameplay guide, and interactive credits modal.
- **Audio Engine ([ArcadeAudioManager.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Scripts/Audio/ArcadeAudioManager.cs))**:
  - Persistent singleton with dedicated clips (`AU_Pop`, `AU_Break`, `AU_Powerup`, `AU_Powerup_Laser`, `AU_Powerup_Shield`, `AU_Life_Lost`, `AU_Level_Success`, `AU_Game_Over`, `AU_Button_Press`, `AU_Glass_Break`, `AU_Bomb_Explosion`, `AU_Powerdown`).
  - Procedural synthesizer fallback for guaranteed sound on any platform, including downward frequency chirp synthesis for powerdowns.

---

## 8. Build Profiles & Platform Deployment
- **Supported Platforms**: Standalone PC/Mac, iOS (Xcode 16+ Swift project), WebGPU / WebGL.
- **Optimization Settings**: `ManagedStrippingLevel.High`, IL2CPP `OptimizeSize`, LZ4HC player compression, unused packages stripped.

---

## 9. Automated Testing Architecture
- **Test Suite ([BlockBreakerCoreTests.cs](file:///c:/Users/ramin/Desktop/Repos/unity-cli-mcp-test/Assets/Tests/BlockBreakerCoreTests.cs))**:
  - 203 automated NUnit EditMode unit/integration tests running via Unity CLI:
    ```bash
    unity cmd run_tests --mode editor
    ```
  - Validates physics deflection math, boundary clamps, powerup & powerdown lifecycles, diamond falling geometry, freeze struggle nudge, defrost hit absorption, campaign progression, safe area framing, level clear timing, entity freeze, and UI element bindings.

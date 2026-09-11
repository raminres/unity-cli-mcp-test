---
name: unity-scene-workflow
description: Procedures and recipes for inspecting, creating, animating, lighting, and camera-tracking Unity GameObjects in URP and Cinemachine via Unity MCP.
---

# Unity Scene Workflow Skill

This skill provides step-by-step procedures for managing scenes, materials, animations, and lighting in this Unity 6 URP project.

## When to Use
Use this skill when:
- Creating or editing GameObjects and components in Unity scenes.
- Authoring looping animation clips and animator controllers.
- Managing URP materials using the `MT_` (Master) and `MI_` (Instance/Variant) conventions.
- Importing and configuring 3D models (`SM_`, `SK_`), textures (`TX_` with suffixes), and audio (`AU_`).
- Managing Unity asset presets (`PR_` prefix) and Preset Manager default glob rules.
- Setting up studio lighting, fog, and volume post-processing.
- Configuring Cinemachine tracking cameras (Cinemachine 3.x).
- Managing arcade game scenes (`LV_` prefix), game state, and UI Toolkit menus/HUDs.
- Implementing responsive multi-aspect ratio camera framing (`ResponsiveCameraController`).
- Integrating iOS Safe Area insets with extra breathing room margins (`SafeAreaController`).
- Implementing dual-layer stacked tapering trails and multi-ball dynamic VFX (`BallTrail`).
- Binding Unity 6 UI Toolkit `PanelRenderer` components safely at runtime with fallback reflection and execution order coordination.
- Managing persistent high score leaderboards (`HighScoreManager`), card guides, and interactive credits.
- Writing and executing automated EditMode/PlayMode tests via Unity Test Framework.

## Reference Documentation
For complete workflow recipes, code examples, and MCP tool patterns, refer to [skills.md](skills.md) and [AGENTS.md](AGENTS.md).

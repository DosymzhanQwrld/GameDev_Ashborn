# Ashborn

A 2D action-platformer roguelite for Android. A fallen batyr rises from a cursed kurgan
and fights his way up through the undead. Die, spend souls on upgrades, rise again.

## Core loop
Run a level → defeat enemies, collect souls → die or win → buy upgrades → run again

## Features (MVP)
- 3 levels, 3 enemy types with state-machine AI + mini-boss
- Melee combat: attack, jump, dodge roll, ranged skill
- Permanent upgrades for souls, JSON save system
- Mobile UI: on-screen controls, Safe Area support
- Sound, VFX, rewarded ad (one revive)

## Tech
- Unity 6 (6000.x), 2D URP, uGUI, Input System
- Target: Android, landscape, reference resolution 1920×1080

## Team
| Name | Role | Systems in code |
|------|------|-----------------|
| Name 1 | Programming | player controller, combat, hitboxes |
| Name 2 | Game design & levels | enemy AI, boss, levels, balance data |
| Name 3 | UI, sound & release | HUD, menus, save system, audio, ads, store page |

## How to open
1. Clone the repo
2. Open the folder in Unity Hub (Unity 6000.x.x)
3. Open `Assets/Scenes/MainMenu.unity`

## Workflow
- `main` is always playable. No direct pushes, only pull requests.
- One branch per task: `feature/player-dash`, `fix/hud-anchors`
- One scene = one owner. Work in prefabs.
- Commit messages: `Add dodge roll with i-frames`, not `fix`

## Credits
Art: (asset packs + links) · Music: (source) · Fonts: (source)
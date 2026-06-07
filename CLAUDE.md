# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

2D side-scrolling action game built in **Unity 2024 (6000.0.31f1)** with C#. Focused on responsive combat, enemy AI with multi-phase bosses, and animation-synced mechanics. All game scripts live under `Assets/`.

## Running the Project

This is a Unity project — there is no CLI build command. Open the project in Unity 2024.0.31f1:

- **Play in editor:** Click the Play button in Unity Editor
- **Scenes:** `Assets/Scenes/` — start with `SampleScene.unity` (main level), then `CrabBossRoom`, `MartialHeroBossRoom`, `SlimeBossRoom`
- **Debug:** Attach VS Code or Rider using `.vscode/launch.json` ("Attach to Unity")
- **No automated tests** — Unity Test Framework is in dependencies but no tests are written; test manually in Play mode

## Architecture

### Player System

**`WarriorController.cs`** (~1383 lines) is the central player script. It handles:
- Movement: horizontal, jump, wall slide, wall jump, dash
- Combat: 3-hit combo chain, hold-release charge attack, downward aerial, slide
- Parry: delegates to `ParrySystem.cs` based on enemy type
- All stamina checks go through `PlayerStats.cs` before executing moves

**`PlayerStats.cs`** — singleton for player stats (max health 100, base damage 3, stamina max 100 with 20/sec regen after 1s delay). Access via `PlayerStats.Instance`.

### Combat Pipeline

```
Input (WarriorController)
  → Stamina check (PlayerStats)
  → Animation state change (Animator)
  → Hitbox activation (DealDamage.cs / raycast)
  → IDamageable.TakeDamage() on enemy
  → Enemy state/phase transition
  → Camera/VFX feedback (CameraShake, particles)
```

**`DealDamage.cs`** — hitbox component placed on weapon. Supports animation-frame-precise damage windows (`manualHitCheck`), charge-scaled damage, and override damage values.

### Enemy Architecture

Regular enemies follow a 2-script pattern: `[Name]Behavior.cs` (state machine + attack coroutines) + `[Name]Health.cs` (IDamageable implementation). Detection uses `Physics2D` overlap/raycast with cooldown coroutines.

Boss enemies use a **phase manager pattern**:
- `CrabBossBehavior.cs` (orchestrator) → `CrabBossBehaviorPhase1` + `CrabBossBehaviorPhase2` + `CrabBossHealth` + `CrabBossDeathHandler`
- `MartialHeroBehavior.cs` (Phase 1) → `MartialHeroPhase2` + `MartialHeroHealth` + `MartialHeroStun`
- `SlimeBossBehavior.cs` + `SlimeBossPhaseManager` + `SlimeBossHealth`

### Key Interfaces

- **`IDamageable`** — all enemies implement this; call `TakeDamage(float amount)`
- **`IAttackState`** — used by `ParrySystem` to detect if an enemy is in an attack state
- **`IStunnable`** — implemented by enemies that react to parry stuns

### Projectile System

Base class: `ProjectileBehaivor.cs`. Specialized variants: `ArcProjectile`, `HorizontalProjectile`, `CrabPearlProjectile`, `SamuraiProjectile` (parryable), `WizardProjectileBehavior`, etc. Projectiles are instantiated directly (no pooling).

### Scene Persistence

Player persists across scenes via `DontDestroyOnLoad()`. Scene transitions are managed by trigger scripts (`BossRoomTrigger.cs`, `CrabBossTriggerRoom.cs`, etc.) and spawners (`PlayerSceneSpawn.cs`, `SlimePlayerSpawner.cs`).

## Controls (for testing in Play mode)

| Action | Input |
|--------|-------|
| Move | A / D |
| Jump | Space |
| Slide | Shift |
| Dash | Space + Shift |
| Attack | Left Mouse |
| Charge Attack | Hold Left Mouse |
| Parry | Right Mouse |
| Wall Climb | Hold A or D |

## Key Files

| File | Role |
|------|------|
| `Assets/WarriorController.cs` | Main player controller — movement + combat |
| `Assets/PlayerStats.cs` | Singleton: health, damage, stamina |
| `Assets/ParrySystem.cs` | Routes parry logic by enemy type |
| `Assets/DealDamage.cs` | Weapon hitbox — damage application |
| `Assets/Enemies/CrabBoss/CrabBossBehavior.cs` | Boss orchestrator example |

## Third-Party Assets

`Assets/Cainos/`, `Assets/CartoonVFX9x/`, `Assets/JMO Assets/` — do not modify these; they are third-party packages. Game scripts are in `Assets/` root and `Assets/Enemies/`.

## Commit Guidelines

- Never add yourself as co-author in commit messages
- Never use emojis in code comments or commit messages
- Never use em dashes (—) in comments or strings; use plain hyphens (-) instead

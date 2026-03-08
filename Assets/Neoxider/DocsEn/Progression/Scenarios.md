# Scenarios

**What it is:** practical usage scenarios for `Progression`, with recommendations for how to configure the module in different game genres.

**How to use:**
1. Pick the closest game type.
2. Configure `LevelCurveDefinition`, `UnlockTreeDefinition`, and `PerkTreeDefinition` accordingly.
3. Connect `ProgressionManager` and the required bridges.

**Navigation:** [← Progression](./README.md)

---

## 1. Arcade / Hypercasual meta

Recommended setup:
- short `LevelCurveDefinition`
- lightweight `UnlockTreeDefinition`
- small `PerkTreeDefinition`
- rewards mostly through `Money` and occasional `PerkPoints`

## 2. Midcore RPG / Action RPG

Recommended setup:
- long `LevelCurveDefinition`
- meaningful level rewards focused on `PerkPoints`
- `UnlockTreeDefinition` for build tiers and feature gates
- `PerkTreeDefinition` for player builds and specializations

## 3. Strategy / City builder / Colony meta

Recommended setup:
- use `UnlockTreeDefinition` as a tech tree
- use level progression as an account rank or headquarters rank
- use perks for global policies or empire modifiers

## 4. Narrative / Quest-driven progression

Recommended setup:
- keep the level curve short or secondary
- use unlock nodes for story gates, world access, and feature gates
- integrate heavily with `QuestManager` and `ProgressionConditionAdapter`

## 5. Roguelite meta progression

Recommended setup:
- meta XP granted after runs
- unlock nodes for permanent content unlocks
- perk tree for account-wide upgrades

## Practical templates

### Only player levels

Use:
- `LevelCurveDefinition`
- `ProgressionManager`

### Technology tree without XP

Use:
- `UnlockTreeDefinition`
- `ProgressionManager`

Keep the level curve minimal:
- one default level at `0 XP`

### Perk-only setup

Use:
- `LevelCurveDefinition` that only grants perk points
- `PerkTreeDefinition`

### Fully no-code setup

Use:
- `ProgressionManager`
- `ProgressionNoCodeAction`
- `ProgressionConditionAdapter`
- reward targets such as `Money`, `Collection`, and `Quest`

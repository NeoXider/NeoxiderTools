# RpgStatGrowthDefinition

`RpgStatGrowthDefinition` is a ScriptableObject defining stat growth formulas for `RpgCombatant`. Using formulas and curves, it determines how characteristics such as Maximum HP, Damage multiplier, Defense multiplier, and HP Regeneration scale directly with a character's level. 

You can assign a created `RpgStatGrowthDefinition` to any `RpgCombatant` (e.g. your Player or your Enemy NPCs) so their stats scale beautifully dependent on the level you set.

## Creating a Stat Growth Definition
1. In the Project window, Right-Click -> `Create` -> `Neoxider` -> `RPG` -> `StatGrowthDefinition`.
2. Name your asset (e.g. `PlayerStatGrowth`, `EnemyMeleeStatGrowth`).
3. Select the asset. The dynamic inspector will show properties for HP, HP Regen, Damage (%), and Defense (%).

## Growth Modes
For each stat, you can select one of two Growth Modes: Formula or Curve.

### Formula Mode
Formula mode uses mathematical calculation. It reveals an additional option to select the shape of the growth:
- **Flat**: The stat does not change with level. Useful for static regen or simple enemies.
- **Linear**: Adds the provided `AddPerLevel` value uniformly for each level. Useful for regular HP or armor growth.
    - `Value = BaseValue + (AddPerLevel * (Level - 1))`
- **Exponential**: Multiplies the base value by a percentage for each level. A `MultiplierPerLevel` of 1.15 adds a +15% compound interest per level. Optimal for JRPG damage / boss health.
    - `Value = BaseValue * (MultiplierPerLevel ^ (Level - 1))`
- **Quadratic**: Adds a growing scale based on squaring the level. Useful for rapid endgame difficulty spiking.
    - `Value = BaseValue + (AddPerLevel * (Level - 1)^2)`
- **Power**: Adds values based on a customizable exponent.
    - `Value = BaseValue + (AddPerLevel * Level ^ MultiplierPerLevel)`

### Curve Mode
Curve mode allows you to draw the exact intended statistics by mapping an X,Y animation curve.
- X is the Character Level.
- Y is the absolute Output value of the stat at that level.

## Live Preview
The custom Editor window contains a **Level Preview**. It automatically plots exact calculations for 1 to 100 levels so designers can test their growth balance without needing to run the game in Play Mode.

## Integrating With Characters
Drag and drop your `StatGrowthDefinition` into the `Stat Growth (SO)` field on your `RpgCombatant` component in your prefabs. Next time the scene starts, the combatant will load it up and apply it!

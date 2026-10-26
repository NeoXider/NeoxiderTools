# SpriteMultiplayerData

ScriptableObject containing per-symbol, per-run-length payout multipliers for `CheckSpin`.

## Inspector authoring

Create it through **Create > Neoxider > Bonus > Slot > Sprite Multiplier Data**. Assign **Sprites Data**,
choose **Min Count**, **Max Count**, and **Default Multiplayer**, then enable **Generate**. Generation
initializes the table on a fresh asset, skips unassigned visual entries, and replaces the previous rows.
Keep the count range small and ordered. Assign the result to the controller's `CheckSpin` multiplier field.

## Runtime API

```csharp
checkSpin.SpritesMultiplierData = multiplierAsset;
float[] multipliers = checkSpin.GetMultiplayers(symbolIds, activeLineCount);
```

`spritesMultiplier.spriteMults` exposes `IdMult` rows (`id`, `countMult`). Each `CountMultiplayer` stores
an exact contiguous-run `count` and its `mult`. A matching row overrides the default multiplier of one.
The asset holds configuration only and does not reference scene objects or credit a wallet itself.

## See Also

- [Module Root](../README.md)

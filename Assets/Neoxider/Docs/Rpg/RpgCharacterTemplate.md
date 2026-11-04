# RpgCharacterTemplate

**What it is:** a ScriptableObject archetype for `RpgCharacter` (`Scripts/Rpg/Data/RpgCharacterTemplate.cs`, namespace `Neo.Rpg`). It stores starting resources, stats, known buffs/statuses, progression, and display data.

**How to use:**
1. Create a template via **Create > Neoxider > RPG > Character Template**.
2. Fill resources and stats with `RpgStatId`: preset or `Custom`.
3. Assign the template to `RpgCharacter`.
4. Keep `Apply Template On Awake` enabled for automatic initialization, or call
   `RpgCharacter.ApplyTemplate(template)` from code.

---

## Fields

| Field | Purpose |
|-------|---------|
| `resources` | Pools such as `HP`, `Mana`, `Stamina`, `Shield`, or custom IDs (`DarkMana`, `Rage`) |
| `stats` | Single-value stats: `Strength`, `Defense`, `FireResist`, or custom IDs |
| `knownBuffs` | SO buffs available through `RpgCharacter.ApplyBuffById(id)` |
| `knownStatuses` | SO statuses available through `RpgCharacter.ApplyStatusById(id)` |
| `progression` | Level growth and manual upgrade rules |
| `displayName`, `description`, `icon` | Optional UI/selection data |

## Runtime API

`RpgCharacter.ApplyTemplate(template)` applies a template at runtime.

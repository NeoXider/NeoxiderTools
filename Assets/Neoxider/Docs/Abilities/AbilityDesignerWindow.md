# Ability Designer (editor window)

**What it is:** a UI Toolkit editor window for browsing, authoring, and validating [AbilityDefinition](./AbilityDefinition.md) and [ModifierDefinition](./ModifierDefinition.md) assets in one place — the v10 authoring hub for the Abilities module.

**How to open:** menu `Neoxider > Windows > Ability Designer`, or the **Open in Ability Designer** flow from an ability/modifier asset inspector.

- Namespace: `Neo.Abilities.Editor`
- File: `Assets/Neoxider/Scripts/Abilities/Editor/AbilityDesignerWindow.cs`
- Editor-only (assembly `Neo.Abilities.Editor`).

## Layout

| Pane | Contents |
|------|----------|
| **Left — library** | Every `AbilityDefinition` and `ModifierDefinition` in the project, searchable by asset name or id, with buff/debuff/ability chips and a `!` marker on assets that have validation issues. `+ Ability` / `+ Modifier` create new assets. |
| **Center — board** | For an ability: the **Cast** and **Impact** phase columns as effect-node cards (colored by op kind) with add / reorder / delete and an inline **Edit** foldout. For a modifier: a live summary board — property contributions, states, tick effects, event reactions. |
| **Right — inspector** | The full default inspector of the selected asset. |
| **Bottom — status bar** | Live cross-asset validation; click an issue row to jump to the offending asset. |

The **+ Add effect** menu lists every built-in op (`damage`, `heal`, `apply_modifier`, `remove_modifier`, `dispel`, `resource_change`, `spawn`, `knockback`, `pull`, `teleport`, `execute`, `chain`); custom op ids are typed into the node's `OpId` field.

## Validation

The status bar re-scans on every edit and undo/redo. Checks:

- missing / duplicate ability and modifier ids;
- negative cooldowns; abilities whose Cast **and** Impact lists are both empty;
- `apply_modifier` / `remove_modifier` nodes with a blank or dangling `ModifierId`;
- `Chance` outside `[0..1]`;
- area selectors with no usable radius (flat `Radius` ≤ 0 and no positive `RadiusByLevel` entry);
- `chain` nodes with no hop radius (they would never bounce);
- modifiers with a tick interval but no tick effects; `Stack` policy with `MaxStacks < 1`.

## Pitfalls

- The window edits assets through `SerializedObject`, so undo/redo works and dirty assets save with the project as usual.
- The library scans the whole project via the asset database; assets inside `Samples~` (hidden) folders are not imported and will not appear.
- Validation is advisory — the runtime does not refuse to register "invalid" data.

## See also

- [AbilityDefinition](./AbilityDefinition.md) / [ModifierDefinition](./ModifierDefinition.md) — the assets it authors
- [Abilities module](./README.md) — effect nodes and ops reference

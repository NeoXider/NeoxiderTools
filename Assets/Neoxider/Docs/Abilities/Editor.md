# Ability editor API and authoring contracts

`Neo.Abilities.Editor` provides the **Neoxider > Windows > Ability Designer** window and the custom inspectors for [AbilityDefinition](./AbilityDefinition.md) and [ModifierDefinition](./ModifierDefinition.md). It edits the same blueprints used by the [runtime API](./README.md); it does not own a combat simulation or scene state.

See [Ability Designer](./AbilityDesignerWindow.md) for the pane layout. This topic covers editor integration, edit behavior and validation boundaries.

## Scene and asset workflow

1. Open the menu above, or select an ability/modifier asset and press **Open in Ability Designer** in its Inspector.
2. Search the left library by asset name or id. **+ Ability** and **+ Modifier** open a save dialog for a new asset. Canceling creates no asset.
3. Select an ability. Its **Cast** and **Impact** columns display the corresponding effect lists. **+ Add effect** inserts a fresh node using `EffectNodeData` defaults and the chosen operation id. It does not duplicate the previous node's target, amount, references or level scaling. Set the amount and any required ids/radius before using the effect.
4. Expand **Edit** on a card, or use the full Inspector on the right. The arrow and remove buttons change list order and remove entries. Serialized edits and list commands support Unity Undo/Redo. Inline edits retain the active card; Inspector edits refresh the board, library badges and validation.
5. For modifiers, the center board summarizes property contributions, states, tick effects and event reactions. Edit their lists in the right Inspector. Missing blueprints show an error, and null tick entries do not hide valid tick summaries.
6. Save edited assets through Unity's normal save workflow. The window refreshes its asset library on focus; Undo/Redo also updates the board and diagnostics.

The add menu lists built-in operations. To author a custom operation, edit the node's `OpId` in the Inspector and register the matching runtime `IEffectOperation`, as described in [Abilities](./README.md). The board is a summary of authored values, not a level/property-dependent damage preview. For `chain`, the displayed limit is the total number of **hits**, including the first target (`MaxTargets <= 0` uses four).

After authoring, register definitions through an [AbilityLibrary](./AbilityLibrary.md) or [AbilitySystemBehaviour](./AbilitySystemBehaviour.md). Grant abilities to the intended units and cast through [AbilityCasterBehaviour](./AbilityCasterBehaviour.md). An optional [AbilityNoCodeAction](./AbilityNoCodeAction.md) can invoke the scene API from UnityEvents. The editor workflow does not require NoCode components. Definition assets hold reusable configuration; scene references and projectile/prefab bindings belong on scene components.

## Editor C# API

Place editor integration in an Editor-only assembly that references `Neo.Abilities.Editor` and `Neo.Abilities`:

```csharp
using Neo.Abilities;
using Neo.Abilities.Editor;
using UnityEditor;

AbilityDefinition definition = Selection.activeObject as AbilityDefinition;
if (definition != null)
{
    AbilityDesignerWindow.Open(definition);
}
else
{
    AbilityDesignerWindow.Open();
}
```

`AbilityDesignerWindow.Open()` opens the browser. `Open(ScriptableObject)` also selects an `AbilityDefinition` or `ModifierDefinition`. The custom inspectors select their target through this same entry point. Runtime consumers reference `Neo.Abilities` and use its typed APIs; the designer assembly is included only in the Unity Editor.

## What validation covers

The status bar scans discovered ability and modifier assets across the project, independently of which runtime libraries a scene registers. Clicking an issue selects its asset. It currently checks:

- missing blueprints, missing ids and duplicate ids within each definition kind;
- negative cooldowns and abilities with no cast/impact nodes;
- effect chance outside `0..1`, missing modifier ids and references to undiscovered modifier definitions;
- area and chain radii: every entry of a nonempty `RadiusByLevel` must be positive, since that array overrides `Radius`; with no entries, the flat radius must be positive;
- ticking modifiers with no tick effects, invalid stack limits and reactions without event ids.

Ability ids, modifier ids and built-in operation ids use `OrdinalIgnoreCase` comparisons, matching the runtime catalogs and operation registry. A reference to `burn` resolves an asset whose id is `Burn`; `Fireball` and `fireball` are duplicate ability ids. `CHAIN` gets the same summary and hop-radius checks as `chain`. These rules apply to definitions authored through C# or the Inspector and consumed by scene/NoCode components. Validation and summaries preserve the original serialized spelling. Ids are not trimmed: ` HEAL `, `CHAIN_CUSTOM` and other unknown operation ids keep the custom summary and do not receive built-in-only modifier-reference or chain checks.

For example, `Radius = 6` with `RadiusByLevel = [0, 3]` produces a warning: the first authored level uses zero, not six. `Radius = 0` with `RadiusByLevel = [2, 3]` is valid for this check.

These diagnostics are authoring assistance. A modifier asset discovered in the project may still be absent from a particular runtime catalog. Separate catalogs can intentionally reuse ids. The scan does not prove that custom operations, named specials, resources or scene archetype bindings exist. **All ability data valid** means that the current editor checks found no issues; test the intended runtime setup separately.

## По-русски

Окно открывается через **Neoxider > Windows > Ability Designer** или кнопку в инспекторе определения. Новая нода получает начальные значения `EffectNodeData`; предыдущая нода не копируется. Правки справа обновляют доску и предупреждения, а редактирование внутри карточки сохраняет её раскрытое состояние. Для запуска игры зарегистрируйте определения через библиотеку или сценовый хаб и используйте C# API кастера либо опциональный NoCode action. Проверка окна охватывает найденные ассеты проекта и не заменяет проверку конкретного runtime-каталога и сценовых привязок. Id способностей, модификаторов и встроенных операций сравниваются без учёта регистра, как в runtime; исходное написание сохраняется, пробелы не обрезаются, неизвестные операции остаются custom.

Back: [Abilities](./README.md).

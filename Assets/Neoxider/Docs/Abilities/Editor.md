# Ability editor API and authoring contracts

**What it is:** `Neo.Abilities.Editor` provides the **Neoxider > Windows > Ability Designer** window and the custom inspectors for [AbilityDefinition](./AbilityDefinition.md) and [ModifierDefinition](./ModifierDefinition.md). It edits the same blueprints the [runtime API](./README.md) uses and owns no combat simulation or scene state. The pane layout is described in [Ability Designer](./AbilityDesignerWindow.md); this page covers edit behaviour, validation and editor C# integration.

**How to use:**
1. Open the window from the menu, or press **Open in Ability Designer** in a definition's Inspector.
2. Search the library on the left by asset name or id. **+ Ability** and **+ Modifier** open a save dialog; canceling creates no asset.
3. Select an ability and add nodes to its **Cast** and **Impact** columns with **+ Add effect**.
4. Edit a card with **Edit**, or in the full Inspector on the right.
5. Save the assets through Unity's normal save workflow.

---

## Editing behaviour

- **New effect nodes** start from `EffectNodeData` defaults with the chosen op id. They do not copy the previous node's target, amount, references or level scaling, so set the amount and the required ids or radius.
- **List commands:** the arrow and remove buttons reorder and remove entries. Serialized edits and list commands support Undo and Redo.
- **Refresh:** inline edits keep the active card open; Inspector edits refresh the board, library badges and validation. The window reloads its asset library on focus.
- **Modifiers:** the center board summarizes property contributions, states, tick effects and event reactions; edit these lists in the right Inspector. A missing blueprint shows an error, and null tick entries do not hide valid tick summaries.
- **Custom ops:** the add menu lists built-in operations. For a custom one, edit the node's `OpId` in the Inspector and register the matching runtime `IEffectOperation` ([Extensibility](./README.md#extensibility)).
- **The board is a summary** of authored values, not a damage preview that depends on level or properties. For `chain` the displayed limit is the total number of **hits**, including the first target (`MaxTargets <= 0` means four).

After authoring, register the definitions through an [AbilityLibrary](./AbilityLibrary.md) or [AbilitySystemBehaviour](./AbilitySystemBehaviour.md), grant abilities to units and cast through [AbilityCasterBehaviour](./AbilityCasterBehaviour.md). An [AbilityNoCodeAction](./AbilityNoCodeAction.md) can call the scene API from UnityEvents, but the editor does not require NoCode components. Definition assets hold reusable configuration; scene references and prefab bindings belong on scene components.

## Editor C# API

Put editor integration in an Editor-only assembly that references `Neo.Abilities.Editor` and `Neo.Abilities`:

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

`AbilityDesignerWindow.Open()` opens the browser. `Open(ScriptableObject)` also selects an `AbilityDefinition` or `ModifierDefinition`; the custom inspectors use the same entry point. Runtime code references only `Neo.Abilities`; the designer assembly exists only in the Unity Editor.

## What validation covers

The status bar scans every ability and modifier asset in the project, whichever runtime libraries a scene registers. Clicking an issue selects its asset. It checks:

- missing blueprints, missing ids and duplicate ids within each definition kind;
- negative cooldowns and abilities with no cast or impact nodes;
- effect chance outside `0..1`, missing modifier ids and references to modifier definitions it did not find;
- area and chain radii: every entry of a nonempty `RadiusByLevel` must be positive, because the array overrides `Radius`; with no entries, the flat radius must be positive;
- ticking modifiers with no tick effects, invalid stack limits and reactions without event ids.

For example, `Radius = 6` with `RadiusByLevel = [0, 3]` gives a warning: the first level uses zero, not six. `Radius = 0` with `RadiusByLevel = [2, 3]` is valid.

**Id comparison.** Ability ids, modifier ids and built-in op ids are compared with `OrdinalIgnoreCase`, as in the runtime catalogs and op registry. A reference to `burn` resolves an asset with the id `Burn`; `Fireball` and `fireball` are duplicates; `CHAIN` gets the same checks as `chain`. Validation keeps the original spelling and does not trim ids, so ` HEAL ` and `CHAIN_CUSTOM` stay custom ops without the built-in modifier and chain checks.

**Limits.** These checks assist authoring. A modifier asset found in the project may still be missing from a particular runtime catalog, and separate catalogs can reuse ids on purpose. The scan does not prove that custom ops, named specials, resources or scene archetype bindings exist. **All ability data valid** means only that the editor checks found nothing; test the runtime setup separately.

## Scene validator

**Neoxider > Abilities > Validate Open Scenes** (C#: `AbilitySceneValidator.Validate(scene)`) lists wiring that fails silently at runtime: nothing throws, the ability simply does not fire or shows nothing.

| Check | Example |
|-------|---------|
| An `AbilityAutoCaster`, `AbilityCooldownSource` or `AbilityNoCodeAction` names an ability or modifier that no library, unit template or action asset in the scene registers. | A typo in **Ability Ids**. |
| An ability's **Projectile Archetype Id** or an effect's **Archetype Id** is not in the hub's **Archetypes**, or an archetype has no prefab. | A zone with a visual that never appears. |
| An effect's payload **Ability Id** or **Modifier Id** does not exist. | A `projectile` effect whose payload was renamed. |
| A **Cast Cue** or **Impact Cue** is not in any `AbilityCuePlayer`. | An effect that never plays. |

## По-русски

Окно открывается через **Neoxider > Windows > Ability Designer** или кнопкой в инспекторе определения. Новая нода
получает значения по умолчанию `EffectNodeData` и не копирует предыдущую. Проверка окна охватывает все ассеты проекта и
не заменяет проверку конкретного runtime-каталога и сценовых привязок. Id сравниваются без учёта регистра, как в runtime;
исходное написание сохраняется, пробелы не обрезаются. Валидатор сцены (**Neoxider > Abilities > Validate Open Scenes**)
находит «тихие» ошибки: опечатку в id, архетип без префаба, несуществующую нагрузку или модификатор, подсказку без
проигрывателя.

## See also

- [Ability Designer](./AbilityDesignerWindow.md) — the window layout
- Back: [Abilities](./README.md)

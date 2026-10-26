# Level bridges

Optional scene adapters for level and XP operations. This is the entry point for `Neo.Core.Level.Bridge`; the namespace is `Neo.Core.Level`.

| Task | Entry point |
|------|-------------|
| Add XP or set a level from a UnityEvent | [LevelNoCodeAction](LevelNoCodeAction.md) |
| Evaluate a threshold through `IConditionEvaluator` | [LevelConditionAdapter](LevelConditionAdapter.md) |
| Configure a scene provider, curve, or persistence | [LevelComponent](../Components/LevelComponent.md) |
| Work with level models and interfaces directly in C# | [Core module](../../README.md) |

For C# gameplay, call `ILevelProvider.AddXp`, `SetLevel`, or the level/XP properties directly. Use `LevelNoCodeAction` when its UnityEvent outputs are useful, and `LevelConditionAdapter` when a caller accepts `IConditionEvaluator`. A scene provider is a `LevelComponent`; the bridges currently resolve that concrete component.

For Inspector authoring, configure the action and connect a UnityEvent to `Execute()`. For automatic condition checks and true/false events, configure [NeoCondition](../../../Condition/NeoCondition.md) with the `LevelComponent` value members described in the adapter guide. Evaluation cadence belongs to the caller or NeoCondition.

Game assemblies using these adapters reference `Neo.Core.Level.Bridge`. Code that names `ILevelProvider` or `LevelModel` also references `Neo.Core`; code naming `LevelComponent` references `Neo.Core.Level.Components`. These optional adapters add no mandatory manager, polling loop, or scene search.

# Neoxider Module Structure

**What it is:** the rules for folders, assembly definitions (asmdef), namespaces and dependencies inside `Assets/Neoxider/Scripts`, plus the current assembly map. Use it when adding a module or refactoring one.

**How to use:**
1. Before adding code, find the owning module in the [assembly map](#7-assembly-map); prefer extending it over a new assembly.
2. For a new module follow the [checklist](#9-checklist-for-a-new-module).
3. Keep rules in plain C# (`Domain/`, `Runtime/`) and put scene adapters in `Components/` and NoCode bridges in `Bridge/`.

---

## 1. Principles

- **One script, one type:** a `.cs` file contains one public type (class, interface, enum or struct). Do not combine a class with its interface or several classes in one file.
- **Module = domain:** each module lives in its own folder under `Scripts/` (for example `Core`, `Rpg`, `Progression`, `Tools/Inventory`) and has one root asmdef. Larger modules split clearly bounded parts into sub-assemblies (see the map).
- **Core first:** gameplay rules live in plain C# objects with injectable time (`IFrameClock`, `INeoClock`) and randomness (a seed, `System.Random` or `Func<int, int>`); MonoBehaviours are scene adapters and NoCode components wrap typed APIs.
- **Code style:** explicit types instead of `var` (anonymous types excepted); comments only as XML docs or `WHY:` / `TODO:` / `HACK:` notes, in English; runtime logs only through `NeoDiagnostics` or debug-gated wrappers.

## 2. Standard folders inside a module

Used as needed; not every module has every folder.

| Folder | Purpose | Contents |
|--------|---------|----------|
| **Interfaces/** | Contracts, access from other modules | Interfaces only (`ILevelProvider`, `IRpgCombatReceiver`). One interface per file. |
| **Domain/** | Domain logic without Unity | Plain C# models and calculators without `MonoBehaviour`. Subfolders per subdomain when useful (for example `Level/`, `Resources/` inside Core). |
| **Data/** | Data and configuration | ScriptableObject definitions and serializable data structures. One type per file. ScriptableObjects never hold scene object references. |
| **Components/** | MonoBehaviour components | Components for scenes and prefabs; subfolders by meaning (for example `Components/Weapons/`). One component per file. |
| **Bridge/** | NoCode and external integration | NeoCondition adapters (`*ConditionAdapter`), NoCode actions (`*NoCodeAction`) and their mode enums. One adapter/action per file; the mode enum in its own file. |
| **Runtime/** | Non-component runtime | Profile data, services and helpers (`RpgCharacterProfileData`, `RpgCharacterStatService`, `RpgCombatMath`). Not MonoBehaviours. |
| **Events/** | Module event types | Custom `UnityEvent<T>` types when extracted. One type per file. |
| **Enums/** | Module enums | Public enums when several exist and separate files improve readability. One enum per file. |
| **Editor/** | Editor-only code | Inspectors and windows, in a separate editor asmdef (`Neo.<Module>.Editor`). |

**Module root:** for a single subdomain or a small module, files may live in the root (for example `LevelProfileData.cs`, `RpgResourceId.cs`). Move types into folders as the module grows.

## 3. Namespaces

- A new module uses one root namespace `Neo.<Module>` that matches its assembly name; folders do not have to be repeated as sub-namespaces.
- Existing modules keep their namespaces even where they differ, because renaming a namespace breaks user code and serialized type references:
  - Tools submodules share `Neo.Tools` (assemblies `Neo.Tools.Inventory`, `Neo.Tools.Move`, ...);
  - Core uses `Neo.Core.Level` and `Neo.Core.Resources`;
  - Rpg uses `Neo.Rpg` plus `Neo.Rpg.Components` and `Neo.Rpg.Runtime`; the legacy combat components in `Neo.Rpg.Combat` use `Neo.Tools`.
- The root `Neo` namespace is reserved for the shared authoring attributes (`[Button]`, `[NeoDoc]`, `[CreateFromMenu]`, ...); see the [namespace policy](./Docs/README.md#namespace-policy).
- When a type must move between namespaces or assemblies, keep its script GUID and add `[MovedFrom]`; keep serialized fields with `[FormerlySerializedAs]`.

## 4. Assembly definitions

### Name and placement

- Format `Neo.<Module>` or `Neo.<Parent>.<Module>` for nested modules: `Neo.Core`, `Neo.Rpg`, `Neo.Tools.Managers`, `Neo.Tools.Inventory`.
- One asmdef per module in the module's root folder; every script below it belongs to that assembly unless a nested folder has its own asmdef.
- Editor code goes into a separate asmdef with `includePlatforms: ["Editor"]`.

### Fields

- **name** — matches the file name without extension (for example `Neo.Rpg`). One historical exception: `Scripts/Animations/Animations.asmdef` declares the name `Neo.Animations`.
- **rootNamespace** — set it to the module namespace for new assemblies. Many existing assemblies leave it empty; the namespace declared in code is authoritative.
- **references** — only the dependencies the code needs, by assembly name. Do not pull in whole modules for one helper.
- **includePlatforms** / **excludePlatforms** — empty by default (all platforms).
- **allowUnsafeCode** — `false`.
- **autoReferenced** — usually `true`.

### When to create a new asmdef

- A new **independent domain** (a new top-level folder under `Scripts/` or `Scripts/Tools/`).
- A sub-module with clear boundaries and its own dependencies, such as `Neo.GridSystem.Dice` or `Neo.Shop.Bridges`, or a contracts-only assembly that others can reference without the implementation (`Neo.Network.Contracts`, `Neo.Tools.InteractableObject.Core`).
- Small utilities that belong to an existing module stay in its assembly.

## 5. Dependencies between modules

- Declare dependencies **explicitly** in asmdef `references`; never rely on `Assembly-CSharp`.
- Keep coupling low: foundation assemblies (`Neo.Core`, `Neo.Merge`, `Neo.PropertyAttribute`, `Neo.Network.Contracts`, `Neo.Tools.InteractableObject.Core`) reference no other Neo assembly; `Neo.Extensions` references only `Neo.PropertyAttribute`. Move shared contracts into `Interfaces/` or a contracts assembly instead of referencing a large module.
- References must not form a cycle. A new reference from a lower-level module to a gameplay module (for example from `Neo.Core` to `Neo.Rpg`) is not allowed.
- Optional third-party code (Mirror, DOTween, the mobile haptics backend) is referenced by name and guarded with `#if MIRROR` / `versionDefines`, so every assembly compiles when the dependency is absent. `Neo.Network` provides solo-mode fallbacks.
- Editor assemblies (`Neo.Editor`, `Neo.Abilities.Editor`, `Neo.Audio.Editor`, `Neo.Cards.Editor`, `Neo.StateMachine.Editor`, `Neo.UI.Editor`) reference the runtime assemblies they inspect.

## 6. Tests

- EditMode tests live in `Assets/Neoxider/Tests/Edit/` (assembly `Neo.Editor.Tests`); PlayMode tests in `Assets/Neoxider/Tests/Play/` (`Neo.Tests.Play`).
- Shared helpers: `Tests/Fixtures` (`Neo.Tests.Fixtures`, `Neo.Tests.Fixtures.Network`), `Tests/Integration/Network` (`Neo.Tests.Network.Integration`, Mirror only) and `Tests/Performance` (allocation counter and `NEO_PERF` helpers).
- Test assemblies reference the runtime assemblies they cover plus `UnityEngine.TestRunner` / `UnityEditor.TestRunner`, and use `defineConstraints: ["UNITY_INCLUDE_TESTS"]`.
- Never reference the sample assemblies (`Neo.Rpg.Demo`, `Neo.Demo.Editor`) from package test assemblies: the demos live outside the package in `Assets/NeoxiderDemos` and may be absent. Use reflection with `Assume`/`Assert.Ignore`, or put the test into the demo's own `Neo.Demo.Editor.Tests`.
- Allocation checks use Unity's GC.Alloc recorder (`Is.Not.AllocatingGCMemory()`), not `GC.GetAllocatedBytesForCurrentThread()`, which reads 0 in the Unity 6 Editor.

## 7. Assembly map

Runtime assemblies under `Scripts/` and the Neo assemblies they reference (prefix `Neo.` omitted). The editor assemblies are listed in section 5; the bundled `CMF` assemblies belong to `ThirdParty/CharacterMovementFundamentals`.

| Assembly | Folder | Neo references | Other references |
|---|---|---|---|
| `Neo.Abilities` | `Scripts/Abilities/` | Core, Core.Level.Components, Reactive, PropertyAttribute, Tools.Spawner, Tools.Managers, Extensions | - |
| `Neo.Animations` | `Scripts/Animations/` | Reactive, PropertyAttribute, Extensions | - |
| `Neo.Audio` | `Scripts/Audio/` | Reactive, Tools.Managers, Extensions, PropertyAttribute, Save | UniTask |
| `Neo.Bonus` | `Scripts/Bonus/` | Reactive, Shop, Tools.Managers, PropertyAttribute, Save, Extensions, Tools.Time, Tools.Random, Network, Tools.Text | Unity.TextMeshPro, Mirror |
| `Neo.Cards` | `Scripts/Cards/` | PropertyAttribute, Extensions | UniTask, Unity.TextMeshPro |
| `Neo.Condition` | `Scripts/Condition/` | PropertyAttribute, Extensions, Network | Mirror |
| `Neo.Core` | `Scripts/Core/` | - | - |
| `Neo.Core.Level.Bridge` | `Scripts/Core/Level/Bridge/` | Core, Core.Level.Components, Extensions, Condition, Reactive, PropertyAttribute | - |
| `Neo.Core.Level.Components` | `Scripts/Core/Level/Components/` | Core, Core.Level.Data, Reactive, Save, PropertyAttribute, Extensions | - |
| `Neo.Core.Level.Data` | `Scripts/Core/Level/Data/` | Core, PropertyAttribute | - |
| `Neo.Core.Resources.Components` | `Scripts/Core/Resources/Components/` | Core, Reactive, Save, PropertyAttribute, Extensions | - |
| `Neo.Extensions` | `Scripts/Extensions/` | PropertyAttribute | - |
| `Neo.GridSystem` | `Scripts/GridSystem/` | PropertyAttribute, Extensions, Merge | - |
| `Neo.GridSystem.Dice` | `Scripts/GridSystem/Dice/` | GridSystem, Merge, PropertyAttribute, Extensions | - |
| `Neo.Haptics` | `Scripts/Haptics/` | PropertyAttribute, Extensions | tsyk5.MobileHapticFeedback (optional) |
| `Neo.Level` | `Scripts/Level/` | Tools.Managers, PropertyAttribute, Save, Tools.Text, Extensions, Network | Unity.TextMeshPro |
| `Neo.Merge` | `Scripts/Merge/` | - | - |
| `Neo.NPC` | `Scripts/NPC/` | Network.Contracts, PropertyAttribute, Extensions, Rpg, Tools.Managers, Save, Network | - |
| `Neo.Network` | `Scripts/Network/` | Network.Contracts, Extensions, Reactive, PropertyAttribute | Mirror, Mirror.Components |
| `Neo.Network.Contracts` | `Scripts/NetworkContracts/` | - | - |
| `Neo.NoCode` | `Scripts/NoCode/` | Reactive, Condition, Tools.Text, PropertyAttribute, Extensions | Unity.TextMeshPro |
| `Neo.Parallax` | `Scripts/Parallax/` | PropertyAttribute, Extensions | - |
| `Neo.Progression` | `Scripts/Progression/` | Core, Core.Level.Components, Extensions, Tools.Managers, Save, Reactive, Condition, Quest, Shop, Bonus, PropertyAttribute, Network | Mirror |
| `Neo.PropertyAttribute` | `Scripts/PropertyAttribute/` | - | - |
| `Neo.Quest` | `Scripts/Quest/` | Condition, Extensions, PropertyAttribute, Tools.Managers, Save, Network | - |
| `Neo.Reactive` | `Scripts/Reactive/` | Extensions | Unity.TextMeshPro |
| `Neo.Rpg` | `Scripts/Rpg/` | Core, Core.Level.Components, Extensions, Tools.Managers, Tools.Input, Save, Reactive, PropertyAttribute, Tools.Time, Network.Contracts | - |
| `Neo.Rpg.Combat` | `Scripts/Rpg/Combat/` | Rpg, Tools.Components, Reactive, PropertyAttribute, Extensions, Tools.Time, Tools.Move | - |
| `Neo.Rpg.ConditionBridge` | `Scripts/Rpg/Bridge/Condition/` | Rpg, Condition, Reactive, PropertyAttribute, Extensions | - |
| `Neo.Rpg.Network` | `Scripts/RpgNetwork/` | Rpg, Network, Network.Contracts, PropertyAttribute | Mirror |
| `Neo.Save` | `Scripts/Save/` | Tools.Managers, PropertyAttribute, Extensions | - |
| `Neo.Settings` | `Scripts/Settings/` | Tools.Managers, Save, PropertyAttribute, Extensions | Unity.TextMeshPro |
| `Neo.Shop` | `Scripts/Shop/` | Network.Contracts, Reactive, Extensions, Tools.Managers, Tools.Text, PropertyAttribute, Save, Network, UI | Unity.TextMeshPro, Mirror |
| `Neo.Shop.Bridges` | `Scripts/Shop/Bridges/` | Shop, Tools.Inventory, Tools.Managers, Extensions, PropertyAttribute | - |
| `Neo.StateMachine` | `Scripts/StateMachine/` | PropertyAttribute, Extensions, Condition | - |
| `Neo.Tools.Components` | `Scripts/Tools/Components/` | Reactive, Shop, PropertyAttribute, Extensions, Tools.Managers, Tools.Text, Save, Network | Unity.TextMeshPro, UniTask, Mirror |
| `Neo.Tools.Debug` | `Scripts/Tools/Debug/` | Extensions, PropertyAttribute | Unity.TextMeshPro, UnityEngine.UI |
| `Neo.Tools.Dialogue` | `Scripts/Tools/Dialogue/` | Tools.Components, PropertyAttribute, Extensions | Unity.TextMeshPro, UniTask |
| `Neo.Tools.Draw` | `Scripts/Tools/Draw/` | Reactive, Extensions, PropertyAttribute, Tools.Spawner, Tools.Managers, Tools.Input | - |
| `Neo.Tools.FakeLeaderboard` | `Scripts/Tools/FakeLeaderboard/` | Tools.Managers, Extensions, Save, PropertyAttribute | Unity.TextMeshPro |
| `Neo.Tools.Input` | `Scripts/Tools/Input/` | Tools.Managers, PropertyAttribute, Extensions | UnityEngine.UI |
| `Neo.Tools.InteractableObject` | `Scripts/Tools/InteractableObject/` | Tools.InteractableObject.Core, Network.Contracts, Reactive, Tools.Input, PropertyAttribute, Extensions, Network | Mirror |
| `Neo.Tools.InteractableObject.Core` | `Scripts/Tools/InteractableObject/Core/` | - | - |
| `Neo.Tools.Inventory` | `Scripts/Tools/Inventory/` | Extensions, PropertyAttribute, Save, Tools.Managers, Tools.View, Tools.Input, Network | Unity.TextMeshPro, Mirror |
| `Neo.Tools.Managers` | `Scripts/Tools/Managers/` | Extensions, PropertyAttribute | - |
| `Neo.Tools.Move` | `Scripts/Tools/Move/` | Network.Contracts, Reactive, Extensions, PropertyAttribute, Tools.Managers, Tools.Input, Settings, Network | Mirror, Mirror.Components, CMF |
| `Neo.Tools.Other` | `Scripts/Tools/Other/` | PropertyAttribute, Extensions | - |
| `Neo.Tools.Physics` | `Scripts/Tools/Physics/` | PropertyAttribute, Extensions | - |
| `Neo.Tools.Random` | `Scripts/Tools/Random/` | PropertyAttribute, Extensions | - |
| `Neo.Tools.Spawner` | `Scripts/Tools/Spawner/` | Tools.Random, Extensions, Tools.Managers, PropertyAttribute, Network | Mirror |
| `Neo.Tools.Text` | `Scripts/Tools/Text/` | Extensions, PropertyAttribute | Unity.TextMeshPro |
| `Neo.Tools.Time` | `Scripts/Tools/Time/` | Reactive, PropertyAttribute, Save, Extensions | UniTask, Unity.TextMeshPro |
| `Neo.Tools.View` | `Scripts/Tools/View/` | Network.Contracts, Reactive, Tools.Managers, UI, Extensions, PropertyAttribute, Animations, Tools.Components, Save, Network | Mirror |
| `Neo.UI` | `Scripts/UI/` | Reactive, Extensions, Tools.Managers, PropertyAttribute | Unity.TextMeshPro |

Changes in 12.0: `Neo.Tools.Draw` gained `Tools.Input` (pointer reads through `MouseInputCompat`), `Neo.Tools.Time` gained `Unity.TextMeshPro` (TimerObject's TMP label), and `Neo.Haptics` gained `Extensions` (`NeoDiagnostics`). The `Neo.HtmlToUitk.*` assemblies were removed (separate `com.neoxider.htmltouitk` package).

## 8. Example structure (medium-sized module)

```text
Scripts/Rpg/
  Neo.Rpg.asmdef
  Interfaces/
    IRpgCombatReceiver.cs
    IRpgDamageModifier.cs
  Enums/
    RpgHitMode.cs
    RpgTargetSelectionMode.cs
    ...
  Data/
    RpgAttackDefinition.cs
    BuffDefinition.cs
    BuffStatModifier.cs
    ...
  Events/
    RpgAttackEvent.cs
    RpgStringEvent.cs
  Components/
    RpgCharacter.cs
    RpgAttackController.cs
    Weapons/
    ...
  Bridge/
    RpgConditionEvaluationMode.cs
    RpgNoCodeAction.cs
    RpgNoCodeActionType.cs
    Condition/            (own asmdef: Neo.Rpg.ConditionBridge)
  Combat/                 (own asmdef: Neo.Rpg.Combat, legacy components)
  Runtime/
    RpgCharacterProfileData.cs
    RpgCharacterStatService.cs
    RpgCharacterProgressionService.cs
    RpgModifierMath.cs
    RpgEffectShelf.cs
    RpgCombatMath.cs
    RpgTargetingUtility.cs
```

One public type per file; namespaces `Neo.Rpg`, `Neo.Rpg.Components` and `Neo.Rpg.Runtime` (see section 3).

## 9. Checklist for a new module

1. Create the module folder under `Scripts/` (or `Scripts/Tools/`).
2. Create `Neo.<Module>.asmdef` with `name` and `rootNamespace`, and only the references it needs.
3. Lay out the code in Interfaces, Domain, Data, Components, Bridge, Runtime, Events, Enums as needed; one type per `.cs` file.
4. Use the module namespace in every new script.
5. Put the rules in plain C# with injectable time/randomness; add components and NoCode bridges as wrappers with an equivalent C# path.
6. Add at least smoke/EditMode tests of the public contract.
7. Add documentation under `Docs/<Module>/` (README with **What it is** / **Contents**, see [DOCUMENTATION.md](./DOCUMENTATION.md)), a `[NeoDoc]` link on each component, and a line in [Docs/README.md](./Docs/README.md).
8. Add a `CHANGELOG.md` entry.
9. If the module has scene components, add a demo scene through a builder in `Assets/NeoxiderDemos/Demo/Editor/Builders/<Module>/` (see [Samples](./Docs/Samples.md#how-demo-scenes-are-made)).

## See also

- [PROJECT_SUMMARY.md](./PROJECT_SUMMARY.md) — package overview and reuse-first map.
- [DOCUMENTATION_GUIDELINES.md](./DOCUMENTATION_GUIDELINES.md) — documentation rules.

## По-русски

Один `.cs` — один публичный тип; модуль — папка под `Scripts/` с одним корневым asmdef `Neo.<Module>`, зависимости только явно через `references` без циклов. Правила — в чистом C#, компоненты и NoCode — обёртки. Существующие namespaces не переименовываются (подмодули Tools используют `Neo.Tools`). Тесты: `Tests/Edit` (`Neo.Editor.Tests`) и `Tests/Play` (`Neo.Tests.Play`); сборки сэмплов из тестов пакета не подключать. Полная карта сборок — в разделе 7.

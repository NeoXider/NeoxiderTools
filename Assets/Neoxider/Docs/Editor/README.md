# Editor module

Editor-only tools: custom inspectors, auto-build, Scene Saver, settings window, missing-scripts finder, texture tools, zip backup. Code in `Editor/`. Use this page as the English module entry.

## Entry points

- Inspectors: [NeoCustomEditor](./NeoCustomEditor.md), [CustomEditorBase](./CustomEditorBase.md), [ComponentDrawer](./ComponentDrawer.md), [ResourceDrawer](./ResourceDrawer.md).
- Windows and scene authoring: [EditorWindows](./EditorWindows.md), [NeoxiderSettingsWindow](./NeoxiderSettingsWindow.md), [CreateMenuObject](./CreateMenuObject.md), [CreateSceneHierarchy](./CreateSceneHierarchy.md).
- Maintenance: [SaveProjectZip](./SaveProjectZip.md), [FindAndRemoveMissingScriptsWindow](./FindAndRemoveMissingScriptsWindow.md), [SingletonCreator](./SingletonCreator.md), [SceneSaver](./SceneSaver.md), [TextureMaxSizeChanger](./TextureMaxSizeChanger.md).

## C# and Inspector workflows

`Neo.Editor` is an Editor-only assembly. Put calls to its APIs in an Editor assembly or behind `#if UNITY_EDITOR`; runtime code should use the underlying typed gameplay APIs.

For scene authoring, select a component to run the optional attribute-based reference lookup. Arrays and `List<T>` keep their declared element types; existing non-empty assignments are preserved. Custom editors can call `ComponentDrawer.ProcessComponentAttributes(component)` and `ResourceDrawer.ProcessResourceAttributes(component)` explicitly. These editor helpers do not replace runtime initialization.

Use `[Button(PlayModeOnly = true)]` for methods that mutate live gameplay state. The inspector disables their actions in Edit Mode. A direct C# call remains an ordinary method call.

In Condition and float-binding inspectors, choose the source object, the exact component instance and a readable member. Duplicate components appear as `BoxCollider`, `BoxCollider (2)`, etc. The popup stores a zero-based occurrence among matching types, independently for both sides of a Condition. Missing or negative occurrences remain unresolved until you explicitly choose a component; the inspector does not silently select the first. Condition method previews convert stored arguments to the declared parameter type (including `byte` and `short`); open generic methods are excluded.

Runtime C# uses `ConditionEntry.ComponentOccurrence`, `OtherComponentOccurrence`, or `ComponentFloatBinding.ComponentOccurrence`. All default to zero for existing content; legacy flat UI `ComponentIndex` fields are preserved and are not occurrence values. Editor extensions can use `ComponentBindingInspectorShared.FindComponentByTypeName(root, typeName, occurrence)`, `IndexOfComponent(...)`, and `ComponentOccurrenceAt(...)`; the existing two-argument resolver still selects occurrence zero. These Editor helpers stay outside player builds.

В 11.0 можно выбрать второй и последующие компоненты одного типа. Старые данные продолжают использовать первый экземпляр; отсутствующий выбранный экземпляр требует явного исправления привязки.

The Network Context Action Relay method menu follows the runtime's argument modes: no argument, `bool`, `float`, `string`, or `GameObject`. Float input also supports widening to a `double` parameter; it does not bind `int` or `long` parameters. Existing unsupported configurations need a compatible overload or a typed C# adapter.

The missing-script window scans closed scenes in temporary preview scenes, retaining open and unsaved work. Repairs to closed scenes and prefabs are saved; repairs to already open scenes remain dirty for normal Save/Undo. See its guide for the exact persistence rules.

Scene Saver separates same-named scenes by source asset GUID. See [SceneSaver](./SceneSaver.md) for the 11.0 backup path and retention of older copies. With Mirror enabled, [lobby authoring](./EditorWindows.md#lobby-demo-authoring-mirror) saves player prefab assets and persistent presenter callbacks after a cancellable save decision.

`PrefabToSpriteUtility.GetReadableCopy(texture)` returns the caller's texture when it is already readable; otherwise it returns a new texture that the caller must destroy. `CreateSpriteFromPreview(texture, "Assets/Icons/Item.png")` owns and releases its temporary copy, including on export failure, and returns the imported Sprite.

## See also

- [PropertyAttribute](../PropertyAttribute/README.md)

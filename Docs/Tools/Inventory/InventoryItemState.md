# Inventory item state (IInventoryItemState)

**What it is:** Capture/restore JSON for unique per-instance item payload (`InventoryItemInstance.ComponentStates`), used with `InventoryItemData.SupportsInstanceState`. Files: `IInventoryItemState.cs`, `InventoryItemStateBehaviour.cs`, `InventoryItemStateUtility.cs`.

**How to use:** Enable **Supports Instance State** on item data; implement capture/restore on the world prefab; pickup/drop pipelines call the utility to pack/unpack state into the container save blob.

For C# callers, use `InventoryItemStateUtility.CaptureInstance(worldObject, itemId)` and
`InventoryItemStateUtility.RestoreInstance(restoredObject, instance)`. These are explicit public
capture/restore calls; they do not require an `Update` call or a synthetic lifecycle invocation.

`InventoryItemStateBehaviour.InventoryStateKey` uses the Inspector **Inventory State Key** override
when assigned, otherwise the component's `GetType().FullName`. The default includes its namespace;
it is neither the short type name nor the assembly-qualified name. Restore matches saved keys exactly.
A C# type alias does not translate stored keys. Renaming a type changes its default key, while moving
an unchanged full type name between assemblies does not. Assign a stable explicit key before a rename
or migrate the saved keys deliberately. Inventory does not guess short-name matches or rewrite opaque
payloads while loading a container.

Ключ по умолчанию — полное имя типа с namespace. Короткое имя не подставляется автоматически;
для стабильных сохранений при переименовании компонента задайте явный **Inventory State Key**.


← [Tools / Inventory](README.md)

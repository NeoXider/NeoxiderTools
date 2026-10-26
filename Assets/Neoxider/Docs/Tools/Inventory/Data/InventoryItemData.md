# InventoryItemData

Reusable ScriptableObject metadata for one logical item id. Namespace `Neo.Tools`.

## Setup

Create an `InventoryItemData` asset and add it to an [InventoryDatabase](InventoryDatabase.md).
Assign a stable item id; containers and Shop mappings refer to that id. The asset stores reusable
configuration and prefab/asset references, not references to scene instances.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `ItemId` | Stable non-negative runtime identifier. |
| `DisplayName`, `Description`, `Category` | Presentation metadata. |
| `Icon` | Optional explicitly authored sprite asset. |
| `MaxStack` | Per-simple-stack cap; non-positive means unlimited. Separate from total/unique inventory limits. |
| `SupportsInstanceState` | Request separate instances from component id/data add APIs. |
| `WorldDropPrefab` | Reusable prefab for world drops and view preview fallback. |

## Runtime and preview ownership

The public properties expose the serialized configuration. `InventoryComponent.GetItemData(id)`
resolves it through the assigned database; plain C# storage can instead receive `InventoryConstraints`
directly without any item assets.

In 11.0, authoring validation no longer populates an empty `Icon` with `GetPreviewSprite()`. Generated
previews belong to the Extensions cache and may be destroyed on cache clear or editor lifecycle
changes; saving such a transient sprite into the asset would leave a stale reference. Existing
authored icon references remain unchanged. Assign a persistent sprite explicitly when a durable icon
is needed. Existing assets with a stale generated icon reference may need that field cleared/reassigned;
no asset migration runs automatically.

[InventoryItemView](../InventoryItemView.md) resolves a missing icon from `WorldDropPrefab` during
binding. It borrows that preview and does not write it back into this asset. Rebind/refresh views after
explicitly clearing the preview cache. Clearing the cache does not destroy authored sprites or sprites
borrowed from a prefab's renderer.

В 11.0 `Icon` хранит только назначенную автором ссылку. Временный preview создаётся для отображения
и больше не записывается в ScriptableObject при `OnValidate`; после очистки кеша обновите view.

## See Also

- [Module Root](../README.md)

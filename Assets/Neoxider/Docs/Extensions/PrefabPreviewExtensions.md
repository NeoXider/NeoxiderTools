# PrefabPreviewExtensions

Use `using Neo.Extensions;` to obtain a display preview from a GameObject/prefab. Existing sprites work in players; the AssetPreview thumbnail fallback is Editor-only. See [Lifecycle and ownership](LifecycleAndOwnership.md) for the shared resource contract.

## API and ownership

| API | Result and owner |
| --- | --- |
| `prefab.GetPreviewTexture()` | Editor AssetPreview texture or mini-thumbnail; borrowed from Unity. Null for a missing object or in a player build. |
| `prefab.GetPreviewSprite()` | Checks the first SpriteRenderer in the hierarchy, including inactive children, then the first Image; uses its sprite when assigned. Those sprites are borrowed assets. Otherwise the Editor thumbnail becomes a cache-owned Sprite; no thumbnail fallback exists in players. |
| `PrefabPreviewExtensions.ClearPreviewSpriteCache()` | Releases only generated fallback Sprites and clears their cache. Does not destroy renderer/image sprites or Unity-owned thumbnail textures. |

Repeated fallback lookups for the same thumbnail reuse the generated Sprite. Generated entries have HideAndDontSave. Destruction is immediate in Edit Mode and deferred through Unity in Play Mode; clear any retained generated references and reacquire them after cache clearing. Cleanup also runs for editor reload/shutdown, leaving Play Mode and runtime subsystem initialization, including disabled domain reload.

Callers borrow returned values and must not destroy them. Do not serialize a generated preview into a ScriptableObject icon, prefab or save record: the cache can release it. Store an authored sprite asset as persistent configuration; resolve a temporary fallback when binding a view. A view should release its reference rather than clear a shared cache on behalf of unrelated consumers.

## C# and scene use

Bind a preview to a scene Image using the same typed API from code or from a component method exposed to UnityEvents:

```csharp
image.sprite = prefab.GetPreviewSprite();
```

After an intentional shared cache clear, rebind the view to obtain a new generated preview. An existing authored renderer/image sprite remains valid. The helper does not create or save a persistent sprite asset; use the Editor asset-authoring workflow when an exported icon is required.

**По-русски:** исходные Sprite и thumbnail-текстуры заимствованы, а generated Sprite принадлежит кэшу. Не уничтожайте возвращённый объект и не записывайте временный fallback в постоянные данные. После очистки кэша получите preview заново при Bind; в player build редакторская генерация отсутствует.

## See Also

- [Lifecycle and ownership](LifecycleAndOwnership.md)
- ← [Extensions](README.md)

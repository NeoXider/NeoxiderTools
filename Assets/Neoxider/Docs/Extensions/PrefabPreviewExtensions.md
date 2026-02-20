# PrefabPreviewExtensions

`PrefabPreviewExtensions` — методы-расширения для получения превью префаба как `Texture2D` или `Sprite`.

## Методы

- `Texture2D GetPreviewTexture(this GameObject prefab)`
- `Sprite GetPreviewSprite(this GameObject prefab)`

## Поведение

- Сначала пытается найти реальный sprite в префабе:
  - `SpriteRenderer.sprite`
  - `UnityEngine.UI.Image.sprite`
- Если sprite не найден, в Editor используется `AssetPreview`:
  - `AssetPreview.GetAssetPreview`
  - fallback: `AssetPreview.GetMiniThumbnail`
- Для texture-превью создаётся runtime `Sprite` и кешируется.

## Пример

```csharp
[SerializeField] private GameObject prefab;
[SerializeField] private UnityEngine.UI.Image icon;

private void Start()
{
    icon.sprite = prefab.GetPreviewSprite();
}
```

## Где используется в Inventory

- `InventoryItemData` — если `Icon` не задан, и есть `WorldDropPrefab`, в `OnValidate` автоматически подставляется preview sprite.
- `InventoryItemView` — если у предмета пустой `Icon`, используется preview sprite из `WorldDropPrefab`.

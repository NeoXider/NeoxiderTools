# SaveProviderSettingsComponent

`Neo.Save.SaveProviderSettingsComponent` is the scene adapter for a [`SaveProviderSettings`](./Settings/SaveProviderSettings.md) asset. Its `Awake` creates a provider and publishes it through `SaveProvider.SetProvider` only after configuration validation succeeds.

## Setup

1. Add **Neoxider → Save → SaveProviderSettingsComponent** to a bootstrap GameObject.
2. Assign the settings asset to `_settings`. It does not need to be in `Resources`.
3. Arrange this component's `Awake` before `SaveManager` and other save consumers; component placement alone does not guarantee callback order.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_settings` | Optional provider-settings asset. When absent, retains an initialized provider or uses the facade's normal Resources/PlayerPrefs initialization. |

## Invalid encryption and retry

If assigned File settings require encryption with an incomplete or invalid key/IV pair, `Awake` leaves the existing provider, its event forwarding, and the target files intact. The component reports the descriptive configuration error only when `SaveProvider.DebugLoggingEnabled` is enabled. It does not fall back to a plaintext provider.

The component applies settings once during `Awake`; editing the asset later does not automatically replace the provider. For a runtime correction, call `settings.TryCreateProvider(out ISaveProvider provider, out string error)` again and call `SaveProvider.SetProvider(provider)` only on success. The [settings guide](./Settings/SaveProviderSettings.md) includes a typed C# bootstrap example. Select the intended provider before staging or restoring gameplay state; replacement does not flush the old provider or reload scene/global caches.

При ошибке назначенных настроек текущий провайдер сохраняется. Для повторной попытки после исправления используйте C# API фабрики; автоматического повторного `Awake` нет.

## See Also

- [Save module](./README.md)
- [Static facade](./SaveProvider.md)

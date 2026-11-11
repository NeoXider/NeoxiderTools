# SaveProviderSettingsComponent

**What it is:** the scene adapter (`Scripts/Save/Settings/SaveProviderSettingsComponent.cs`, `Neo.Save.SaveProviderSettingsComponent`) for a [`SaveProviderSettings`](./Settings/SaveProviderSettings.md) asset. Its `Awake` creates a provider and publishes it through `SaveProvider.SetProvider` only after configuration validation succeeds.

**How to use:**
1. Add **Neoxider → Save → SaveProviderSettingsComponent** to a bootstrap GameObject.
2. Assign the settings asset to `_settings`. It does not need to be in `Resources`.
3. Keep it in the first scene that reads saves. It runs with `[DefaultExecutionOrder(SaveProviderSettingsComponent.ExecutionOrder)]` (-1000), so within one scene load its `Awake` runs before `SaveManager`, `GameSettingsComponent` (-200), `AMSettings` (-100) and every other default-order reader. Objects loaded earlier (for example a `DontDestroyOnLoad` bootstrap from a previous scene) have already read from the provider that was active then.

---

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_settings` | Optional provider-settings asset. When absent, retains an initialized provider or uses the facade's normal Resources/PlayerPrefs initialization. |

## Invalid encryption and retry

If assigned File settings require encryption with an incomplete or invalid key/IV pair, `Awake` leaves the existing provider, its event forwarding, and the target files intact, and logs the descriptive configuration error (key and IV values are never included). The error is logged even when `SaveProvider.DebugLoggingEnabled` is off, because saves would otherwise silently go to the previous backend. It does not fall back to a plaintext provider.

The component applies settings once during `Awake`; editing the asset later does not automatically replace the provider. For a runtime correction, call `settings.TryCreateProvider(out ISaveProvider provider, out string error)` again and call `SaveProvider.SetProvider(provider)` only on success. The [settings guide](./Settings/SaveProviderSettings.md) includes a typed C# bootstrap example. Select the intended provider before staging or restoring gameplay state; replacement does not flush the old provider or reload scene/global caches.

## See Also

- [Save module](./README.md)
- [Static facade](./SaveProvider.md)

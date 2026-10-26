# SaveProviderSettings

`Neo.Save.SaveProviderSettings` is a ScriptableObject containing provider selection, file name, and optional AES file encryption. It contains configuration rather than scene references. Creating a provider does not activate it in the static facade.

## Setup

1. Create an asset through **Assets → Create → Neoxider → Save → Save Provider Settings**.
2. Select `PlayerPrefs` or `File`. File saves use `_fileName` under `Application.persistentDataPath`; an empty name uses `save.json`.
3. For automatic lazy initialization, put the asset at `Resources/SaveProviderSettings.asset`. Alternatively, assign it to [`SaveProviderSettingsComponent`](../SaveProviderSettingsComponent.md), initializing that component before save consumers.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_providerType` | Provider selection; exposed read-only through `ProviderType`. |
| `_fileName` | File name; exposed read-only through `FileName`. |
| `_encryptFileSave` | Require encryption when the File provider is selected. Disabled by default. |
| `_fileEncryptionKey` | Custom AES key: 16, 24, or 32 UTF-8 bytes after trimming. |
| `_fileEncryptionIv` | Custom AES IV: 16 UTF-8 bytes after trimming. |

When encryption is enabled, leave both key and IV empty/whitespace to use `SaveFileEncryption`'s built-in pair, or supply both custom values. An incomplete pair or invalid byte count is rejected. With encryption disabled, these strings are ignored; PlayerPrefs also ignores all file-encryption fields. Existing built-in defaults and AES/Base64 storage remain compatible.

## Runtime API and failure handling

- `bool TryCreateProvider(out ISaveProvider provider, out string error)` returns `false`, a null provider, and a descriptive error for invalid required encryption. It rejects that configuration before constructing a file provider, reading a profile, or deleting a staging file. Success returns a provider and a null error.
- `ISaveProvider CreateProvider()` retains its signature and throws `InvalidOperationException` for the same invalid configuration.

Intentional 11.0 change: invalid enabled encryption no longer silently creates a plaintext provider. Correct the settings and retry. Neither factory replaces the active provider; publish a successfully created instance explicitly:

```csharp
using Neo.Save;

public static class SaveBootstrap
{
    public static bool Apply(SaveProviderSettings settings, out string error)
    {
        if (!settings.TryCreateProvider(out ISaveProvider provider, out error))
        {
            return false;
        }

        SaveProvider.SetProvider(provider);
        return true;
    }
}
```

The caller supplies a non-null settings asset and can display the returned error without exposing key/IV values. Successful File construction loads existing storage. `TryCreateProvider` reports encryption configuration validity; other constructor exceptions still propagate. It does not report durable writes or verify that a correctly sized key decrypts an existing profile. File reading/recovery and explicit `Save()` retain their existing contracts.

Если включённое шифрование настроено неверно, провайдер не создаётся и файлы не открываются. Исправьте пару ключ/IV и повторите вызов; для активации успешно созданного экземпляра используйте `SaveProvider.SetProvider`.

## See Also

- [Save module](../README.md)
- [Static facade](../SaveProvider.md)
- [File provider](../FileSaveProvider.md)

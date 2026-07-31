# SaveFileEncryption

**Purpose:** an AES-CBC wrapper over the JSON string before writing it to a file, and the reverse decryption on load (the format is a Base64 string, with no JSON wrapper).

It is used by **`FileSaveProvider`** only when saving to **File** is enabled in **[SaveProviderSettings](Settings/SaveProviderSettings.md)** and file encryption is checked.

---

## Encryption is disabled by default

In **`Save Provider Settings`** the file encryption checkbox (**Encrypt File Save**) is **unchecked**: files are written as plain JSON. This is the default behavior and the recommended one for development.

---

## Built-in key (when encryption is enabled)

If encryption is **enabled** but the **Key** and **IV** fields in the settings are **both empty**, constants from the code are used:

| Constant | Purpose |
|-----------|------------|
| `SaveFileEncryption.DefaultEncryptionKey` | AES key (16 UTF-8 bytes for a 16-character ASCII string). |
| `SaveFileEncryption.DefaultEncryptionIv` | AES IV (16 bytes). |

They can be **replaced**: fill in both the Key and IV fields in the Inspector (or pass your own strings when creating a **`FileSaveEncryptionConfig`** in code). If only one of the two fields is filled in, the configuration is considered invalid (see the console message).

---

## Custom keys

If you provide your own strings:

| Parameter | Format |
|----------|--------|
| Key | UTF-8: **16, 24, or 32 bytes** (conveniently, the same number of ASCII characters). |
| IV | UTF-8: **exactly 16 bytes**. |

---

## Security limitations

The built-in key is **shared across all builds** of the package — it is not a secret against build analysis. For release, set your own keys or use other measures (a server, obfuscation). This is **not** a replacement for Keychain / Keystore.

---

## See also

- [FileSaveProvider](./FileSaveProvider.md)
- [SaveProviderSettings](Settings/SaveProviderSettings.md)
- ← [Save](../README.md)

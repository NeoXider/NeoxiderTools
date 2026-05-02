# SaveProviderSettings

**Назначение:** ScriptableObject — выбор провайдера сохранений (**PlayerPrefs** или **File**), имя файла и опциональное шифрование файла (AES).

## Подключение

- Создать: **Create → Neoxider → Save → Provider Settings**.
- Положить ассет в `Resources` с именем **`SaveProviderSettings`** (или использовать компонент инициализации), чтобы **`SaveProvider`** подхватил его автоматически.

## Основные настройки (Inspector)

| Поле | Описание |
|------|----------|
| `Provider Type` | **PlayerPrefs** или **File**. |
| `File Name` | Имя JSON-файла для типа **File** (по умолчанию `save.json`), каталог — `Application.persistentDataPath`. |
| `Encrypt File Save` | **По умолчанию выключено.** Если включить при типе **File**, содержимое файла сохраняется как Base64(AES(JSON)). |
| `File Encryption Key` | Ключ AES (см. [SaveFileEncryption](../SaveFileEncryption.md)). Оставьте **пустым вместе с IV**, чтобы использовать встроенный ключ по умолчанию. |
| `File Encryption Iv` | IV AES. Пусто вместе с ключом → встроенный IV по умолчанию. |

Если шифрование включено, но ключ и IV заданы некорректно (например заполнено только одно поле), в консоли будет предупреждение и файл сохранится **без** шифрования.

## См. также

- [SaveFileEncryption](../SaveFileEncryption.md)
- [FileSaveProvider](../FileSaveProvider.md)
- [SaveProvider](../SaveProvider.md)
- [Корень модуля](../README.md)

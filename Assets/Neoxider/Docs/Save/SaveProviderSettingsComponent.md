# SaveProviderSettingsComponent

**Что это:** MonoBehaviour для инициализации SaveProvider из ScriptableObject (Settings). Вызывается в Awake; после этого SaveProvider использует заданный провайдер. Пространство имён: `Neo.Save`. Файл: `Scripts/Save/Settings/SaveProviderSettingsComponent.cs`.

**Как использовать:** добавить на объект в сцене (например, гейм-менеджер), в поле **Settings** назначить ScriptableObject с настройками провайдера. Если не указан — используется провайдер по умолчанию (PlayerPrefs).

---

## Настройка

- **Settings** — ScriptableObject с настройками провайдера (тип, путь к файлу и т.д.). Если не указан, используется провайдер по умолчанию (PlayerPrefs).

Вызывается в `Awake`, после чего `SaveProvider` использует созданный провайдер для всех операций сохранения/загрузки.

## См. также

- [Save README](./README.md)
- SaveProviderSettings (ScriptableObject)

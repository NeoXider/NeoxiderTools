# SaveProviderSettingsComponent

MonoBehaviour для настройки провайдера сохранения через Inspector. Позволяет инициализировать SaveProvider настройками из ScriptableObject без размещения в Resources.

- **Пространство имён:** `Neo.Save`
- **Путь:** `Assets/Neoxider/Scripts/Save/Settings/SaveProviderSettingsComponent.cs`

## Настройка

- **Settings** — ScriptableObject с настройками провайдера (тип, путь к файлу и т.д.). Если не указан, используется провайдер по умолчанию (PlayerPrefs).

Вызывается в `Awake`, после чего `SaveProvider` использует созданный провайдер для всех операций сохранения/загрузки.

## См. также

- [Save README](./README.md)
- SaveProviderSettings (ScriptableObject)

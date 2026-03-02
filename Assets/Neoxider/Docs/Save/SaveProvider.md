# SaveProvider

**Что это:** статический класс с API как у PlayerPrefs (GetInt/SetInt, GetFloat/SetFloat, GetString/SetString, GetBool/SetBool). Работает через текущий провайдер (ISaveProvider): по умолчанию PlayerPrefs или из SaveProviderSettings в Resources. События OnDataSaved, OnDataLoaded, OnKeyChanged. Пространство имён: `Neo.Save`. Файл: `Scripts/Save/SaveProvider.cs`.

**Как использовать:** вызывать `SaveProvider.GetInt(key)`, `SaveProvider.SetString(key, value)` и т.д. из любого места. Инициализация при первом вызове (из Resources или PlayerPrefs). Чтобы задать провайдер из сцены — использовать [SaveProviderSettingsComponent](SaveProviderSettingsComponent.md); для смены из кода — `SaveProvider.SetProvider(provider)`.

---

## Методы

| Метод | Описание |
|-------|----------|
| GetInt(key, default) / SetInt(key, value) | Целое число. |
| GetFloat(key, default) / SetFloat(key, value) | Float. |
| GetString(key, default) / SetString(key, value) | Строка. |
| GetBool(key, default) / SetBool(key, value) | Boolean. |
| CurrentProvider | Текущий ISaveProvider. |
| SetProvider(provider) | Подменить провайдер вручную. |

## См. также

- [SaveProviderSettingsComponent](SaveProviderSettingsComponent.md) — инициализация из сцены
- [ISaveProvider](ISaveProvider.md) — интерфейс провайдера
- [Save README](README.md)

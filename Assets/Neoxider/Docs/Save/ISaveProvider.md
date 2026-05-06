# ISaveProvider

**Назначение:** Интерфейс единого API для всех бэкендов сохранения. Абстрагирует хранилище — код работает одинаково независимо от того, сохраняются данные в PlayerPrefs или в JSON-файл. Реализации: `PlayerPrefsSaveProvider`, `FileSaveProvider`.

---

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `SaveProviderType ProviderType { get; }` | Тип провайдера (`PlayerPrefs` или `File`). |
| `int GetInt(string key, int defaultValue = 0)` | Получить целое число. Если ключа нет — вернёт `defaultValue`. |
| `void SetInt(string key, int value)` | Записать целое число. |
| `float GetFloat(string key, float defaultValue = 0f)` | Получить дробное число. |
| `void SetFloat(string key, float value)` | Записать дробное число. |
| `string GetString(string key, string defaultValue = "")` | Получить строку. |
| `void SetString(string key, string value)` | Записать строку. |
| `bool GetBool(string key, bool defaultValue = false)` | Получить булево значение. |
| `void SetBool(string key, bool value)` | Записать булево значение. |
| `bool HasKey(string key)` | Проверить наличие ключа в хранилище. |
| `void DeleteKey(string key)` | Удалить ключ и его значение. |
| `void DeleteAll()` | Удалить все данные. |
| `void Save()` | Принудительно записать на диск. |
| `void Load()` | Принудительно перечитать из хранилища. |

---

## Unity Events

| Событие | Описание |
|---------|----------|
| `event Action OnDataSaved` | Данные записаны на диск. |
| `event Action OnDataLoaded` | Данные загружены из хранилища. |
| `event Action<string> OnKeyChanged` | Значение ключа изменилось. Параметр — имя ключа. |

---

## Примеры

### No-Code (Inspector)
Провайдер создаётся через `SaveProviderSettingsComponent` или `SaveProvider` (синглтон). В Inspector настройте тип провайдера (`PlayerPrefs` или `File`).

### Код
```csharp
// Получить текущий провайдер
ISaveProvider provider = SaveProvider.I;

// Сохранить прогресс
provider.SetInt("Score", 1500);
provider.SetBool("MusicEnabled", false);
provider.Save();

// Загрузить
int score = provider.GetInt("Score", 0);
bool music = provider.GetBool("MusicEnabled", true);

// Подписка на изменения
provider.OnKeyChanged += key => Debug.Log($"Изменён ключ: {key}");
```

---

## См. также
- [FileSaveProvider](FileSaveProvider.md) — реализация через JSON-файл
- [PlayerPrefsSaveProvider](PlayerPrefsSaveProvider.md) — реализация через PlayerPrefs
- [SaveProvider](SaveProvider.md) — синглтон-обёртка
- [SaveProviderExtensions](SaveProviderExtensions.md) — расширения для массивов
- ← [Save](README.md)

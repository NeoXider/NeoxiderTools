# PlayerPrefsSaveProvider

**Назначение:** Реализация `ISaveProvider` через Unity `PlayerPrefs`. Самый простой вариант — данные хранятся в реестре (Windows) или файле настроек (macOS/Linux). Булевы значения хранятся как `int` (0/1) с префиксом `Bool_`.

---

## API

Все методы `ISaveProvider`: `GetInt`, `SetInt`, `GetFloat`, `SetFloat`, `GetString`, `SetString`, `GetBool`, `SetBool`, `HasKey`, `DeleteKey`, `DeleteAll`, `Save`, `Load`.

Особенности:
- `GetBool` / `SetBool` — используют `PlayerPrefs.GetInt` / `SetInt` с ключом `"Bool_" + key`.
- `HasKey` — проверяет оба варианта: обычный ключ и `"Bool_" + key`.
- `Load()` — не выполняет загрузку (PlayerPrefs загружаются автоматически), но вызывает `OnDataLoaded`.

---

## Примеры

### No-Code (Inspector)
В `SaveProviderSettingsComponent` выберите тип **PlayerPrefs** — провайдер создастся автоматически.

### Код
```csharp
var provider = new PlayerPrefsSaveProvider();

provider.SetString("PlayerName", "Alex");
provider.SetBool("TutorialDone", true);
provider.Save(); // PlayerPrefs.Save()

string name = provider.GetString("PlayerName"); // "Alex"
bool tutorial = provider.GetBool("TutorialDone"); // true
```

---

## См. также
- [ISaveProvider](ISaveProvider.md) — интерфейс
- [FileSaveProvider](FileSaveProvider.md) — альтернатива через JSON-файл
- ← [Save](README.md)

# FileSaveProvider

**Назначение:** Реализация `ISaveProvider`, которая хранит все данные в одном JSON-файле в каталоге `Application.persistentDataPath` (каталог можно переопределить через **`FileSaveProviderOptions`**). Поддерживает несколько слотов сохранения (`ChangeSlot`). При финализации может дописать изменения на диск.

Опционально файл сохраняется в формате **AES-CBC + Base64** — см. **[SaveFileEncryption](SaveFileEncryption.md)** и **[SaveProviderSettings](Settings/SaveProviderSettings.md)**.

**По умолчанию** в настройках провайдера шифрование **выключено** (обычный JSON). При включении можно оставить ключ/IV пустыми — подставятся **`SaveFileEncryption.DefaultEncryptionKey`** и **`DefaultEncryptionIv`**.

---

## Шифрование файла

При передаче **`FileSaveEncryptionConfig`** через **`FileSaveProviderOptions.Encryption`** при сохранении JSON шифруется; при загрузке поддерживаются как **открытый JSON** (начинается с `{`), так и **зашифрованный** файл — чтобы можно было включить шифрование после выпуска открытого сохранения.

---

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `FileSaveProvider(string fileName = "save.json", FileSaveProviderOptions options = null)` | Конструктор. По умолчанию каталог — `persistentDataPath`; через `options.PersistenceRoot` можно переопределить (например тестами). |
| `void ChangeSlot(string fileName)` | Сохраняет текущие данные (если есть изменения) и переключается на другой файл. |
| `SaveProviderType ProviderType` | Всегда `SaveProviderType.File`. |
| `int GetInt(string key, int default)` | Получить целое число по ключу. |
| `void SetInt(string key, int value)` | Записать целое число. Вызывает `OnKeyChanged`. |
| `float GetFloat(string key, float default)` | Получить дробное число. |
| `void SetFloat(string key, float value)` | Записать дробное число. |
| `string GetString(string key, string default)` | Получить строку. |
| `void SetString(string key, string value)` | Записать строку. |
| `bool GetBool(string key, bool default)` | Получить булево значение. |
| `void SetBool(string key, bool value)` | Записать булево значение. |
| `bool HasKey(string key)` | Есть ли ключ в хранилище. |
| `void DeleteKey(string key)` | Удалить ключ. |
| `void DeleteAll()` | Удалить все данные. |
| `void Save()` | Записать данные на диск. Вызывает `OnDataSaved`. |
| `void Load()` | Перечитать данные с диска. Вызывает `OnDataLoaded`. |

---

## Unity Events

| Событие | Когда срабатывает |
|---------|-------------------|
| `OnDataSaved` | После успешной записи файла на диск. |
| `OnDataLoaded` | После загрузки данных из файла. |
| `OnKeyChanged(string key)` | После изменения значения любого ключа. |

---

## Примеры

### No-Code (Inspector)
`FileSaveProvider` — чистый C#-класс (не MonoBehaviour). Для No-Code используйте `SaveProviderSettingsComponent`, который создаёт нужный провайдер автоматически.

### Код
```csharp
// Создать провайдер с файлом slot1.json
var save = new FileSaveProvider("slot1.json");

// Записать данные
save.SetInt("Level", 5);
save.SetString("PlayerName", "Alex");
save.SetBool("TutorialDone", true);
save.Save(); // записать на диск

// Прочитать данные
int level = save.GetInt("Level");       // 5
string name = save.GetString("PlayerName"); // "Alex"

// Переключить слот (текущие данные сохранятся автоматически)
save.ChangeSlot("slot2.json");
int otherLevel = save.GetInt("Level");  // данные из slot2
```

---

## См. также
- [SaveFileEncryption](SaveFileEncryption.md) — AES и ключи
- [ISaveProvider](ISaveProvider.md) — интерфейс, который реализует этот класс
- [PlayerPrefsSaveProvider](PlayerPrefsSaveProvider.md) — альтернативный провайдер через PlayerPrefs
- [SaveProvider](SaveProvider.md) — синглтон-обёртка
- ← [Save](README.md)

# SaveProviderExtensions

**Назначение:** Методы расширения для `ISaveProvider` — добавляют сохранение/загрузку массивов `int[]` и `float[]`. Массивы сериализуются в строку через запятую.

---

## API

| Метод | Описание |
|-------|----------|
| `void SetIntArray(this ISaveProvider, string key, int[] array)` | Сохранить массив int. При `null` или пустом массиве — удаляет ключ. |
| `int[] GetIntArray(this ISaveProvider, string key, int[] defaultValue = null)` | Загрузить массив int. Если ключа нет — вернёт `defaultValue` или пустой массив. |
| `void SetFloatArray(this ISaveProvider, string key, float[] array)` | Сохранить массив float. |
| `float[] GetFloatArray(this ISaveProvider, string key, float[] defaultValue = null)` | Загрузить массив float. |

---

## Примеры

### Код
```csharp
ISaveProvider provider = SaveProvider.I;

// Сохранить массив результатов
provider.SetIntArray("HighScores", new[] { 100, 250, 500 });
provider.Save();

// Загрузить
int[] scores = provider.GetIntArray("HighScores");
// scores = [100, 250, 500]

// С значением по умолчанию
float[] times = provider.GetFloatArray("BestTimes", new[] { 99.9f });
```

---

## См. также
- [ISaveProvider](ISaveProvider.md) — базовый интерфейс
- ← [Save](README.md)

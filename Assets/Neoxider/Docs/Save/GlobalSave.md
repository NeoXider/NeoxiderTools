# GlobalSave

**Что это:** статический класс для одного глобального объекта данных (GlobalData). Хранение через SaveProvider (по умолчанию PlayerPrefs). Загрузка при первом обращении к data, сохранение при присвоении data или вызове SaveProgress(). Пространство имён: `Neo.Save`. Файл: `Scripts/Save/GlobalSave/GlobalSave.cs`.

**Как использовать:** расширить класс GlobalData своими полями (coins, уровни и т.д.). В коде читать/писать `GlobalSave.data`; для принудительной записи вызвать `GlobalSave.SaveProgress()`. Инициализацию при старте игры выполнять после готовности SaveProvider.

---

## Описание класса

### GlobalSave
- **Пространство имен**: `Neo.Save`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Save/GlobalSave/GlobalSave.cs`

**Описание**
Статический класс для управления одним глобальным объектом сохранения (`GlobalData`).

**Ключевые особенности**
- **Глобальный доступ**: Данные доступны через `GlobalSave.data`.
- **Ленивая загрузка (Lazy Loading)**: Данные автоматически загружаются из `PlayerPrefs` при первом обращении к `GlobalSave.data`.
- **Авто-сохранение**: Данные автоматически сохраняются при присваивании нового объекта свойству `GlobalSave.data`.

**Публичные свойства и методы**
- `data`: Статическое свойство (`get`, `set`). Предоставляет доступ к единственному экземпляру `GlobalData`. При первом чтении загружает данные, при записи — сохраняет.
- `IsReady`: Статическое свойство (`get`, `set`). Флаг, показывающий, были ли данные уже загружены из `PlayerPrefs`.
- `LoadingData()`: Статический метод. Принудительно загружает данные из `PlayerPrefs`. Возвращает `void`.
- `SaveProgress()`: Статический метод. Принудительно сохраняет текущий объект `GlobalData` в `PlayerPrefs`. Возвращает `void`.

**Пример использования**
```csharp
// Получить количество монет
int currentCoins = GlobalSave.data.coins;

// Изменить и сохранить количество монет
var currentData = GlobalSave.data;
currentData.coins += 50;
GlobalSave.data = currentData; // Старые данные заменятся и автоматически сохранятся

// Или можно просто сохранить текущее состояние
GlobalSave.data.lastCompletedLevel = 5;
GlobalSave.SaveProgress();
```

# SaveProvider

**Что это:** `SaveProvider` — статический фасад поверх активного `ISaveProvider`, который даёт API в стиле `PlayerPrefs` и проксирует события сохранения, загрузки и изменения ключей. Файл: `Scripts/Save/SaveProvider.cs`, пространство имён: `Neo.Save`.

**Как использовать:**
1. Вызывайте `GetInt`, `SetString`, `GetBool` и другие методы из любого runtime-кода.
2. Если отдельный backend не задан, система инициализирует `PlayerPrefsSaveProvider`.
3. Если нужен другой backend, настройте [`SaveProviderSettingsComponent`](./SaveProviderSettingsComponent.md) или вызовите `SetProvider()` из кода.
4. Подписывайтесь на `OnDataSaved`, `OnDataLoaded` и `OnKeyChanged`, если нужно реагировать на изменения.

---

## Что делает класс

- Инициализируется лениво при первом обращении.
- Ищет `SaveProviderSettings` в `Resources`.
- Если настройки не найдены, использует `PlayerPrefsSaveProvider`.
- Проксирует вызовы к активному провайдеру.
- Проксирует события провайдера наружу через единый статический API.
- При замене провайдера корректно снимает старые подписки и вешает новые.

## Публичный API

| API | Описание |
|-----|----------|
| `CurrentProvider` | Текущий активный `ISaveProvider`. |
| `SetProvider(provider)` | Явно подменяет активный провайдер. |
| `GetInt / SetInt` | Работа с `int`. |
| `GetFloat / SetFloat` | Работа с `float`. |
| `GetString / SetString` | Работа со строками. |
| `GetBool / SetBool` | Работа с `bool`. |
| `HasKey` | Проверяет наличие ключа. |
| `DeleteKey` | Удаляет один ключ. |
| `DeleteAll` | Очищает всё хранилище провайдера. |
| `Save()` | Принуждает провайдер сохранить данные. |
| `Load()` | Принуждает провайдер обновить/загрузить данные. |

## События

| Событие | Когда вызывается |
|---------|------------------|
| `OnDataSaved` | После `Save()` у активного провайдера. |
| `OnDataLoaded` | После `Load()` у активного провайдера. |
| `OnKeyChanged` | Когда провайдер сообщает об изменении конкретного ключа. |

## Важное поведение

- `SaveProvider` не хранит данные сам по себе, он только делегирует вызовы текущему backend.
- После исправления event-forwarding старые обработчики больше не остаются висеть на предыдущем провайдере после `SetProvider()`.
- Если вы меняете провайдер в runtime, подписчики на события `SaveProvider` продолжают работать через новый backend без повторной подписки.

## Когда использовать

Используйте `SaveProvider`, если:
- нужен простой key/value API без прямой привязки к `PlayerPrefs`;
- backend должен быть сменяемым;
- вы не работаете с scene-component persistence через `SaveManager`.

## Пример

```csharp
using Neo.Save;

public static class SettingsStorage
{
    public static void SaveVolume(float value)
    {
        SaveProvider.SetFloat("audio.volume", value);
        SaveProvider.Save();
    }

    public static float LoadVolume()
    {
        return SaveProvider.GetFloat("audio.volume", 1f);
    }
}
```

## См. также

- [SaveProviderSettingsComponent](./SaveProviderSettingsComponent.md)
- [SaveManager](./SaveManager.md)
- [README](./README.md)

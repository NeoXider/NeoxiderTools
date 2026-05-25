# Расширения CoroutineExtensions

**Что это:** См. описание ниже.

**Как использовать:** см. разделы ниже.

---


## 1. Введение

`CoroutineExtensions` — это мощная утилита для работы с корутинами, которая делает их более гибкими и удобными. Она позволяет запускать корутины не только на `MonoBehaviour`, но и на `GameObject` или даже глобально, а также возвращает специальный объект `CoroutineHandle`, с помощью которого запущенную корутину можно остановить.

---

## 2. Описание методов

### CoroutineExtensions
- **Пространство имен**: `Neo.Extensions`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Extensions/CoroutineExtensions.cs`

**Статические методы**
- `Delay(this MonoBehaviour mono, float seconds, Action action, ...)`: Выполняет `action` после указанной задержки в секундах.
- `WaitUntil(this MonoBehaviour mono, Func<bool> predicate, Action action)`: Выполняет `action`, как только `predicate` вернет `true`.
- `WaitWhile(this MonoBehaviour mono, Func<bool> predicate, Action action)`: Выполняет `action`, как только `predicate` перестанет возвращать `true`.
- `DelayFrames(this MonoBehaviour mono, int frameCount, Action action, ...)`: Выполняет `action` через указанное количество кадров.
- `NextFrame(this MonoBehaviour mono, Action action)`: Выполняет `action` на следующем кадре.
- `EndOfFrame(this MonoBehaviour mono, Action action)`: Выполняет `action` в конце текущего кадра.
- `Start(IEnumerator routine)`: Глобально запускает любую корутину.

*Примечание: Большинство методов имеют перегрузки для `GameObject` и для глобального вызова (например, `CoroutineExtensions.Delay(...)`). Все методы возвращают `CoroutineHandle`, у которого можно вызвать метод `Stop()`.*

---

## 3. Lifecycle и domain reload disabled

- Глобальный `CoroutineHelper` сбрасывается через `RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)`, поэтому static-ссылка не переживает новый Play Mode запуск при отключенном domain reload.
- Для владельца корутины автоматически добавляется скрытый `CoroutineLifecycleTracker`. Если `GameObject` владельца уничтожен до завершения корутины, все связанные `CoroutineHandle` переводятся в завершенное состояние.
- `CoroutineHandle.Stop()` безопасен при уже уничтоженном владельце: handle будет завершен без исключений.
- `CoroutineExtensions.Start(null)` не создает глобальный helper и возвращает неактивный handle.

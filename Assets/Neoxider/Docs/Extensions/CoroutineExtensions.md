# Расширения CoroutineExtensions

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

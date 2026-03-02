# UnityLifecycleEvents

**Что это:** компонент пробрасывает события жизненного цикла (Awake, OnEnable, Start, OnDisable, OnDestroy, Update и др.) в UnityEvent. Настройка в Inspector. Файл: `Tools/InteractableObject/UnityLifecycleEvents.cs`.

**Как использовать:** Add Component → Neoxider → Tools → Components → UnityLifecycleEvents; подписаться на нужные события (On Awake, On Start, On Update и т.д.) в инспекторе.

## События жизненного цикла

| Событие | Когда вызывается |
|---------|-------------------|
| **On Awake** | Awake — при создании объекта. |
| **On Enable** | OnEnable — при включении объекта/компонента (появление). |
| **On Start** | Start — в первый кадр после Enable. |
| **On Disable** | OnDisable — при выключении (исчезновение). |
| **On Destroy** | OnDestroy — перед уничтожением объекта. |

## События по кадрам (включить галочкой)

| Событие | Галочка | Аргумент (float) |
|---------|---------|-------------------|
| **On Update** | Emit Update | `Time.deltaTime` — время с прошлого кадра. |
| **On Fixed Update** | Emit Fixed Update | `Time.fixedDeltaTime`. |
| **On Late Update** | Emit Late Update | `Time.deltaTime`. |

Галочки **Emit Update / Fixed Update / Late Update** нужно включать только если подписаны на соответствующие события — иначе метод кадра не вызывается и нагрузка не создаётся.

## События приложения

| Событие | Аргумент |
|---------|----------|
| **On Application Pause** | bool — true при паузе (свёрнуто окно и т.п.). |
| **On Application Focus** | bool — true при получении фокуса. |

## Примеры

- **Появление:** подписаться на **On Enable** — показать панель, включить звук.
- **Исчезновение:** подписаться на **On Disable** — спрятать панель, сохранить состояние.
- **Каждый кадр:** включить **Emit Update**, подписаться на **On Update Event** — в динамический float передаётся `deltaTime`; можно накапливать в переменную для таймера.
- **Время с начала:** в On Update Event вызывать метод, который добавляет аргумент к своей переменной — получится время с момента включения компонента.

## См. также

- [Counter](./Counter.md), [ScoreManager](./ScoreManager.md)

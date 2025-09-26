# Класс Timer

## 1. Введение

`Timer` — это мощный и гибкий класс для создания таймеров в вашей игре. В отличие от стандартных корутин или `Invoke`, этот таймер предоставляет полный контроль над своим жизненным циклом: его можно запускать, останавливать, ставить на паузу, возобновлять, зацикливать и даже изменять его длительность на лету. 

Он идеально подходит для реализации кулдаунов, обратных отсчетов, длительности эффектов и любых других механик, где требуется точный контроль над временем.

---

## 2. Описание класса

### Timer
- **Пространство имен**: `Neo`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Tools/Time/Timer.cs`
- **Тип**: Обычный C#-класс (не `MonoBehaviour` или `ScriptableObject`)

**Описание**
Класс `Timer` управляет отсчетом времени. Он использует асинхронные операции (`async/await`) для эффективной работы, не блокируя основной поток Unity. Все изменения состояния таймера и его прогресс передаются через `UnityEvent`.

**Конструктор**
- `Timer(float duration, float updateInterval = 0.05f, bool looping = false, bool useUnscaledTime = false)`:
  - `duration`: Общая длительность таймера в секундах.
  - `updateInterval`: Как часто будет вызываться событие `OnTimerUpdate` (в секундах).
  - `looping`: Если `true`, таймер будет автоматически перезапускаться после завершения.
  - `useUnscaledTime`: Если `true`, таймер будет игнорировать `Time.timeScale` (полезно для UI во время паузы).

**Ключевые свойства**
- `Duration`: Общая длительность таймера. Можно изменять во время работы.
- `UpdateInterval`: Интервал обновления.
- `IsRunning`: `true`, если таймер активен.
- `IsLooping`: `true`, если таймер зациклен.
- `UseUnscaledTime`: `true`, если таймер использует не масштабированное время.
- `IsPaused`: `true`, если таймер на паузе.
- `RemainingTime`: Оставшееся время в секундах.
- `Progress`: Прогресс таймера от 0 (начало) до 1 (конец).

**Публичные методы (Public Methods)**
- `Start()`: Запускает или возобновляет таймер. **Возвращает `Task`, поэтому должен быть вызван с `await` или без `await` в `async void` методе.**
- `Stop()`: Останавливает таймер и сбрасывает его.
- `Pause()`: Ставит таймер на паузу.
- `Resume()`: Снимает таймер с паузы.
- `Restart()`: Останавливает и немедленно запускает таймер заново.
- `AddTime(float seconds)`: Добавляет или вычитает время из текущего таймера. Можно передавать отрицательные значения.
- `Reset(float newDuration, ...)`: Полностью сбрасывает таймер с новыми параметрами.

**Unity Events**
- `OnTimerStart`: Вызывается в момент запуска таймера.
- `OnTimerEnd`: Вызывается, когда таймер завершает отсчет (если не зациклен).
- `OnTimerUpdate` (`UnityEvent<float, float>`): Вызывается с интервалом `updateInterval`. Передает оставшееся время и прогресс (0-1).
- `OnTimerPause`: Вызывается при постановке таймера на паузу.
- `OnTimerResume`: Вызывается при снятии таймера с паузы.

---

## 3. Как использовать

`Timer` — это обычный C#-класс, поэтому его нужно объявить как поле в вашем `MonoBehaviour` или другом классе.

```csharp
using Neo.Tools;
using UnityEngine;
using System.Threading.Tasks;

public class CooldownAbility : MonoBehaviour
{
    public float cooldownDuration = 5f;
    private Timer _cooldownTimer;

    void Awake()
    {
        _cooldownTimer = new Timer(cooldownDuration, 0.1f); // Таймер на 5 секунд, обновляется каждые 0.1с
        _cooldownTimer.OnTimerEnd.AddListener(OnCooldownEnd); // Подписываемся на событие завершения
        _cooldownTimer.OnTimerUpdate.AddListener(OnCooldownUpdate); // Подписываемся на обновление
    }

    public async void UseAbility()
    {
        if (_cooldownTimer.IsRunning) return; // Если кулдаун активен, ничего не делаем

        Debug.Log("Способность использована!");
        await _cooldownTimer.Start(); // Запускаем кулдаун
    }

    private void OnCooldownEnd()
    {
        Debug.Log("Кулдаун завершен! Способность снова доступна.");
    }

    private void OnCooldownUpdate(float remainingTime, float progress)
    {
        // Обновляем UI, например, прогресс-бар
        // Debug.Log($"Осталось: {remainingTime:F1}с, Прогресс: {progress:P0}");
    }
}
```

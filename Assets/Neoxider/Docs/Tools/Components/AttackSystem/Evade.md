# Evade — уклонение / рывок с перезарядкой

## 1. Введение

`Evade` — универсальный компонент для механики уклонения, рывка (dash), ролла или любой способности с **временем действия** и **перезарядкой**. Управляет длительностью самого действия (неуязвимость, анимация), временем cooldown и даёт события и реактивные поля для UI и условий.

**Типичное использование:** даш, ролл, кратковременный блок, «перекат» — всё, где есть фаза «действие» и фаза «ожидание следующего использования».

---

## 2. Класс

- **Пространство имён:** `Neo.Tools`
- **Файл:** `Assets/Neoxider/Scripts/Tools/Components/AttackSystem/Evade.cs`

### Особенности

- Настраиваемая длительность уклонения и перезарядки.
- Гибкий старт cooldown: вместе с началом уклонения или после его завершения.
- Опция **unscaled time** для cooldown (не зависит от `Time.timeScale`, удобно при паузе).
- События для всех фаз: начало/конец уклонения, начало/конец перезарядки.
- Реактивные поля для UI: прогресс перезарядки (0–1) и оставшееся время в секундах.
- Свойства для NeoCondition и рефлексии: `ReloadProgressValue`, `RemainingCooldownTimeValue`.
- API: `TryStartEvade()` (с возвратом успеха), `StartEvade()`, `ResetCooldown()`, `SetCooldownDuration()`, `SetEvadeDuration()`, `GetRemainingCooldown()`.

---

## 3. Настройки (Inspector / код)

| Поле | Тип | Описание |
|------|-----|----------|
| `evadeDuration` | float | Длительность действия уклонения в секундах (≥ 0.01). |
| `cooldownDuration` | float | Время перезарядки в секундах (≥ 0.01). |
| `cooldownStartsWithEvade` | bool | Если true — cooldown стартует при начале уклонения; если false — после его завершения. |
| `useUnscaledTimeForCooldown` | bool | Использовать unscaled time для cooldown (не зависит от паузы). |
| `cooldownUpdateInterval` | float | Интервал обновления прогресса перезарядки в секундах (≥ 0.015). |

**Обратная совместимость:** старые имена полей (`reloadTime`, `reloadImmediately`, `OnReloadStarted`, `OnReloadCompleted`) при загрузке сцены/префаба подхватываются через `FormerlySerializedAs`.

---

## 4. События (UnityEvent)

| Событие | Когда вызывается |
|---------|-------------------|
| `OnEvadeStarted` | В момент начала уклонения. |
| `OnEvadeCompleted` | По окончании действия уклонения. |
| `OnCooldownStarted` | При старте перезарядки. |
| `OnCooldownCompleted` | Когда перезарядка завершена и способность снова доступна. |

---

## 5. Реактивные поля (подписка через `.OnChanged`)

| Поле | Тип | Описание |
|------|-----|----------|
| `ReloadProgress` | ReactivePropertyFloat | Прогресс перезарядки 0–1 (0 = только началась, 1 = готова). |
| `RemainingCooldownTime` | ReactivePropertyFloat | Оставшееся время перезарядки в секундах. |

Для NeoCondition и рефлексии используйте примитивные геттеры: `ReloadProgressValue`, `RemainingCooldownTimeValue`.

---

## 6. Состояние (только чтение)

| Свойство | Тип | Описание |
|----------|-----|----------|
| `IsEvading` | bool | Идёт ли в данный момент действие уклонения. |
| `IsOnCooldown` | bool | Идёт ли перезарядка. |
| `CooldownProgress` | float | Прогресс перезарядки 0–1. |
| `CanEvade` | bool | Можно ли сейчас выполнить уклонение. |

---

## 7. Публичные методы

| Метод | Возврат | Описание |
|-------|---------|----------|
| `TryStartEvade()` | bool | Пытается начать уклонение. Возвращает true, если уклонение начато. |
| `StartEvade()` | void | Начинает уклонение, если возможно (обёртка над `TryStartEvade()`). |
| `ResetCooldown()` | void | Сбрасывает перезарядку — способность снова доступна. Текущее уклонение не прерывается. |
| `SetCooldownDuration(float seconds)` | void | Устанавливает длительность перезарядки; действует со следующего старта. |
| `SetEvadeDuration(float seconds)` | void | Устанавливает длительность действия уклонения. |
| `GetRemainingCooldown()` | float | Оставшееся время перезарядки в секундах (0, если не на cooldown). |

---

## 8. Краткий пример

```csharp
var evade = GetComponent<Evade>();

// Подписка на реактивные поля для UI
evade.ReloadProgress.OnChanged.AddListener(progress => fillImage.fillAmount = progress);
evade.RemainingCooldownTime.OnChanged.AddListener(seconds => timeText.text = $"{seconds:F1}s");

// Проверка и запуск
if (evade.CanEvade && input.EvadePressed)
    evade.TryStartEvade();

// Сброс перезарядки (чит, бонус)
evade.ResetCooldown();
```

---

## 9. Связанные компоненты

- **Health** — для отображения HP и неуязвимости во время уклонения.
- **NeoCondition** — условия по `ReloadProgressValue`, `RemainingCooldownTimeValue`, `CanEvade`.

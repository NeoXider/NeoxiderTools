# ProgressionManager

Центральный компонент системы мета-прогрессии (наследует `Singleton<ProgressionManager>`). Отвечает за накопление опыта, расчет уровней, управление очками перков и сохранение профиля.

## Содержание
- [Назначение](#назначение)
- [Поля (Inspector)](#поля-inspector)
- [Reactive State](#reactive-state)
- [API](#api)
- [Пример использования](#пример-использования)
- [См. также](#см-также)

---

## Назначение
`ProgressionManager` служит единой точкой входа для всех систем, связанных с ростом игрока. Он объединяет данные об уровнях (`LevelCurve`), технологиях (`UnlockTree`) и улучшениях (`PerkTree`), обеспечивая их синхронизацию и сохранение.

---

## Поля (Inspector)

| Поле | Тип | Описание |
|------|-----|----------|
| **Level Curve** | `LevelCurveDefinition` | Определяет пороги XP для уровней и награды за каждый уровень. |
| **Unlock Tree** | `UnlockTreeDefinition` | Дерево технологий/разблокировок. |
| **Perk Tree** | `PerkTreeDefinition` | Дерево покупаемых улучшений (перков). |
| **Save Key** | `string` | Уникальный ключ для сохранения профиля (напр. "Player_Progression"). |
| **Auto Save** | `bool` | Если включено, профиль автоматически сохраняется при любом изменении. |
| **Condition Context** | `GameObject` | Объект, используемый как контекст для проверки условий `NeoCondition`. |

---

## Reactive State
Реактивные свойства, удобные для привязки к UI через `UnityEvents` или код.

| Свойство | Тип | Описание |
|----------|-----|----------|
| **XpState** | `Int` | Текущее количество опыта. |
| **LevelState** | `Int` | Текущий уровень. |
| **PerkPointsState** | `Int` | Доступные очки для покупки перков. |
| **XpToNextLevel** | `Int` | Сколько опыта осталось до следующего уровня. |

---

## API

| Метод | Описание |
|-------|----------|
| **AddXp(int amount)** | Добавляет опыт и автоматически пересчитывает уровень. |
| **AddPerkPoints(int amount)** | Прямое добавление очков перков. |

_Обращение к менеджеру в коде происходит через `ProgressionManager.I` (или `ProgressionManager.Instance`)._
| **TryUnlockNode(string id)** | Попытка разблокировать узел (возвращает успех и причину неудачи). |
| **TryBuyPerk(string id)** | Попытка купить перк за очки. |
| **ResetProgression()** | Полный сброс профиля до начального состояния. |
| **SaveProfile()** / **LoadProfile()** | Ручное управление сохранением/загрузкой. |

---

## Пример использования

### 1. Получение опыта за убийство (C#)

```csharp
using Neo.Progression;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private int xpValue = 100;

    public void Die()
    {
        // Глобальный доступ через Singleton к менеджеру прогрессии
        if (ProgressionManager.HasInstance)
        {
            ProgressionManager.I.AddXp(xpValue);
        }
        Destroy(gameObject);
    }
}
```

### 2. Привязка к Slider XP (No-Code)

Вы можете отобразить прогресс опыта без написания кода, используя реактивные свойства менеджера:

1. Создайте UI Slider для опыта.
2. Подпишитесь на `UnityEvent` в инспекторе `ProgressionManager` (например, `OnXpChanged` или используйте `ProgressionStateListener`).
3. Передайте значение в метод `Slider.SetValueWithoutNotify()`.

### 3. Проверка уровня перед доступом (C#)

```csharp
using Neo.Progression;
using UnityEngine;

public class Portal : MonoBehaviour
{
    public void TryEnter()
    {
        // Проверяем реактивное состояние уровня напрямую
        if (ProgressionManager.I.LevelState.Value >= 10)
        {
            Debug.Log("Вход разрешен!");
        }
        else
        {
            Debug.Log($"Нужен 10 уровень. Ваш уровень: {ProgressionManager.I.LevelState.Value}");
        }
    }
}
```

---

## См. также
- [Progression No-Code Actions](./ProgressionNoCodeAction.md)
- [Level Curve Definition (SO)](./LevelCurveDefinition.md)
- [← Назад к Progression](./README.md)

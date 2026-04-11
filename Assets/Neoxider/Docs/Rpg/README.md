# Модуль RPG

Полноценная боевая система для создания RPG в 3D и 2D. Включает управление статами, способностями, поиском целей, уклонениями и статус-эффектами.

## Содержание
- [Назначение](#назначение)
- [Оглавление файлов](#оглавление-файлов)
- [Как использовать](#как-использовать)
- [Ключевые концепции](#ключевые-концепции)
- [Примеры использования](#примеры-использования)
- [См. также](#см-также)

---

## Назначение
Модуль RPG предназначен для быстрой сборки боевых механик любой сложности (от простого кликера до комплексной Action-RPG). Он разделяет данные об атаках (`AttackDefinition`) от логики выполнения, позволяя переиспользовать способности между игроком и NPC.

---

## Оглавление файлов
- [RpgStatsManager](./RpgStatsManager.md) — профиль персонажа, баффы, статы и сохранение.
- [RpgCombatant](./RpgCombatant.md) — компонент для NPC и разрушаемых объектов.
- [RpgAttackController](./RpgAttackController.md) — управление очередью и запуском атак.
- [RpgAttackDefinition](./RpgAttackDefinition.md) — ScriptableObject с параметрами атаки.
- [RpgEvadeController](./RpgEvadeController.md) — система уклонений и i-frames.
- [RpgNoCodeAction](./RpgNoCodeAction.md) — мост для UnityEvents.

---

## Как использовать

1. **Игрок**: Добавьте `RpgStatsManager`, `RpgAttackController` и `RpgEvadeController`.
2. **Враги**: Добавьте `RpgCombatant` и настройте HP.
3. **Атаки**: Создайте `RpgAttackDefinition` (Melee/Ranged/Aoe) и назначьте его в контроллер.
4. **Урон**: Используйте Unity-теги для разделения фракций (враги атакуют игрока, игрок — врагов).

---

## Ключевые концепции

### Persistence (Сохранение)
`RpgStatsManager` автоматически сохраняет уровень и состояние HP через `SaveProvider`. Это полезно для главного героя. Для обычных врагов используйте `RpgCombatant` (без сохранения).

### Data-Driven Attacks
Все параметры атак вынесены в файлы. Вы можете мгновенно изменить радиус взрыва или скорость полета снаряда во время игры без перекомпиляции.

---

## Примеры использования

### 1. Нанесение урона кнопкой (No-Code)
1. На объект кнопки или триггера добавьте `RpgNoCodeAction`.
2. Выберите действие `DealDamage`.
3. Укажите цель (это должен быть объект с `RpgStatsManager` или `RpgCombatant`).
4. Задайте силу `Power` (базовый урон).
5. Смонтируйте вызов `Execute()` на событие клика или столкновения.

### 2. Изменение статов (C#)

```csharp
using Neo.Rpg;
using UnityEngine;

public class PoisonTrap : MonoBehaviour
{
    // Ссылка на дебафф, настроенный в редакторе
    [SerializeField] private BuffDefinition poisonBuff; 

    private void OnTriggerEnter(Collider other)
    {
        // Пробуем получить менеджер статов у вошедшего объекта
        if (other.TryGetComponent(out RpgStatsManager stats))
        {
            stats.AddBuff(poisonBuff);
            Debug.Log($"{other.name} отравлен!");
        }
    }
}
```

### 3. Запуск атаки из аниматора (C#)

```csharp
using Neo.Rpg;
using UnityEngine;

public class AttackAnimationListener : MonoBehaviour
{
    [SerializeField] private RpgAttackController attackController;

    // Вызывается через AnimationEvent
    public void OnSwordSwingHit()
    {
        // 0 - индекс Primary Attack в массиве контроллера.
        // false - говорит системе, что это действие не от инпута, а от кода.
        attackController.TryPerformAttack(0, false);
    }
}
```

---

## См. также
- [Progression Module](../Progression/README.md)
- [NPC Navigation](../NPC/README.md)
- [← Назад к Docs](../README.md)

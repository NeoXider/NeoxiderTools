# Модуль RPG

Полноценная боевая система для создания RPG в 3D и 2D. Центральный компонент персонажа — `RpgCharacter`: ресурсы, статы, баффы, статусы, рост уровня, сохранение и Mirror-мультиплеер находятся в одном API.

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
- [RpgCharacter](./RpgCharacter.md) — универсальный персонаж для игрока, NPC, мобов, питомцев и разрушаемых объектов.
- [RpgCharacterTemplate](./RpgCharacterTemplate.md) — SO-шаблон ресурсов, статов, эффектов и progression.
- [RpgProgressionDefinition](./RpgProgressionDefinition.md) — режим роста уровня: all-stats, manual upgrades или hybrid.
- [RpgAttackController](./RpgAttackController.md) — управление очередью и запуском атак.
- [RpgAttackDefinition](./RpgAttackDefinition.md) — ScriptableObject с параметрами атаки.
- [RpgEvadeController](./RpgEvadeController.md) — система уклонений и i-frames.
- [RpgNoCodeAction](./RpgNoCodeAction.md) — мост для UnityEvents.
- [RpgConditionAdapter](./RpgConditionAdapter.md) — RPG-условия для NeoCondition.
- [RpgResourceBinding](./RpgResourceBinding.md) / [RpgStatBinding](./RpgStatBinding.md) — реактивная привязка ресурсов и статов к UI/NoCode.

---

## Как использовать

1. **Игрок**: Добавьте `RpgCharacter`, `RpgAttackController` и `RpgEvadeController`.
2. **Враги/NPC/питомцы**: Добавьте `RpgCharacter` и настройте нужные ресурсы (`HP`, `Mana`, `Stamina`, `Shield` или custom ID).
3. **Атаки**: Создайте `RpgAttackDefinition` (Melee/Ranged/Aoe) и назначьте его в контроллер.
4. **Урон**: Используйте Unity-теги для разделения фракций (враги атакуют игрока, игрок — врагов).

---

## Ключевые концепции

### Persistence (Сохранение)
`RpgCharacter` сохраняет уровень, XP, upgrade points, ресурсы, статы, баффы и статусы через `SaveProvider`, если включён persistence-блок и задан save key. Для обычных врагов сохранение можно не включать.

### Data-Driven Attacks
Все параметры атак вынесены в файлы. Вы можете мгновенно изменить радиус взрыва или скорость полета снаряда во время игры без перекомпиляции.

---

## Примеры использования

### 1. Нанесение урона кнопкой (No-Code)
1. На объект кнопки или триггера добавьте `RpgNoCodeAction`.
2. Выберите действие `TakeDamage`.
3. Укажите цель с `RpgCharacter`.
4. Задайте `Amount` (базовый урон).
5. Смонтируйте вызов `Execute()` на событие клика или столкновения.

### 2. Изменение статов (C#)

```csharp
using Neo.Rpg;
using Neo.Rpg.Components;
using UnityEngine;

public class PoisonTrap : MonoBehaviour
{
    [SerializeField] private StatusEffectDefinition poisonStatus;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out RpgCharacter character))
        {
            character.ApplyStatus(poisonStatus);
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

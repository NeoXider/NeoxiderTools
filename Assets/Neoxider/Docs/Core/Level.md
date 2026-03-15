# Level Component

**Что это:** MonoBehaviour, реализует `ILevelProvider`. Универсальный источник уровня и опыта: игрок, батлпасс, этап главы. Путь: `Scripts/Core/Level/Components/LevelComponent.cs`, пространство имён `Neo.Core.Level`.

**Как использовать:**
1. Добавить компонент на GameObject (Add Component → Neoxider/Core/Level Component).
2. При необходимости назначить `LevelCurveDefinition` (ScriptableObject) — три режима: **Formula** (Linear, Quadratic, Exponential, Power и др.), **Curve** (AnimationCurve: X = уровень, Y = кумулятивный XP), **Custom** (ручная таблица уровень → XP).
3. Задать стартовые уровень/XP, опционально макс. уровень. При включённом сохранении указать SaveKey.
4. Вызывать `AddXp(int)` или `SetLevel(int)` из кода/NoCode; подписаться на OnLevelUp / OnXpGained для UI или наград.
5. Progression и RPG получают уровень через ссылку на этот компонент (поле Level Provider).

---

## Поля и свойства

| Имя | Тип | Назначение |
|-----|-----|------------|
| LevelCurveDefinition | LevelCurveDefinition | Кривая уровня (опционально). |
| LevelState, XpState, XpToNextLevelState | ReactivePropertyInt | Реактивное состояние для биндинга UI. |
| LevelStateValue, XpStateValue, XpToNextLevelStateValue | int | Текущие значения реактивных полей (для NeoCondition). |
| Level | int | Текущий уровень (≥ 1). |
| TotalXp | int | Накопленный опыт. |
| XpToNextLevel | int | XP до следующего уровня (0 при макс. уровне). |
| UseXp | bool | Уровень считается по кривой из XP. |
| HasMaxLevel, MaxLevel | bool, int | Ограничение макс. уровня (0 = без лимита). |

## Методы

| Сигнатура | Возврат | Описание |
|-----------|---------|----------|
| AddXp(int amount) | void | Добавляет опыт; уровень пересчитывается по кривой. |
| SetLevel(int level) | void | Устанавливает уровень напрямую (с учётом MaxLevel). |
| GetProfileSnapshot() | LevelProfileData | Снимок данных для сохранения. |
| Save(), Load() | void | Сохранение/загрузка по SaveKey через SaveProvider. |
| Reset() | void | Сброс к стартовым уровень/XP. |

## События (UnityEvent)

| Событие | Когда вызывается | Параметры |
|---------|------------------|-----------|
| OnLevelUp | При повышении уровня после AddXp | int — новый уровень |
| OnXpGained | После добавления XP | — |
| OnProfileLoaded | После загрузки профиля | — |
| OnProfileSaved | После сохранения профиля | — |

## Примеры

```csharp
ILevelProvider p = player.GetComponent<LevelComponent>();
p.AddXp(100);
int tier = battlePass.GetComponent<LevelComponent>().Level;
```

NoCode: компонент **LevelNoCodeAction** (действия AddXp, SetLevel), **LevelConditionAdapter** (LevelAtLeast, XpAtLeast, XpToNextLevelAtMost).

Подробнее о кривой уровня: **[LevelCurveDefinition.md](./LevelCurveDefinition.md)** — режимы Formula / Curve / Custom, типы формул, поля и API.

## См. также

- [LevelCurveDefinition](./LevelCurveDefinition.md) — определение кривой уровня (Formula, Curve, Custom).
- [HealthComponent](./HealthComponent.md) — пулы HP/Mana.
- [Progression/README.md](../Progression/README.md) — награды за уровень, перки.
- [Rpg/README.md](../Rpg/README.md) — уровень и статы в бою.

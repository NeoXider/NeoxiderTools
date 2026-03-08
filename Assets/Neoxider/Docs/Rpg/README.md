# RPG

**Что это:** модуль RPG-статистики для HP, уровня, баффов и статус-эффектов. Скрипты находятся в `Scripts/Rpg/`.

**Оглавление:**
- [RpgStatsManager](./RpgStatsManager.md) — главный runtime-компонент и точка входа.
- [RpgNoCodeAction](./RpgNoCodeAction.md) — no-code bridge для UnityEvent.
- [RpgConditionAdapter](./RpgConditionAdapter.md) — адаптер условий для `NeoCondition` и других систем.

**Навигация:** [← К Docs](../README.md)

---

## Как использовать

1. Создайте asset'ы `BuffDefinition` и `StatusEffectDefinition` через меню `Neoxider/RPG`.
2. Добавьте `RpgStatsManager` на сцену и назначьте definitions в массивы.
3. Настройте `Save Key`, если для проекта нужен отдельный профиль.
4. Подключите UI к `HpState`, `HpPercentState`, `LevelState` или к UnityEvent менеджера.
5. Вызывайте `TakeDamage`, `Heal`, `TryApplyBuff`, `TryApplyStatus` из кода или через `RpgNoCodeAction`.
6. Для no-code проверок используйте `RpgConditionAdapter` или свойства менеджера через `NeoCondition`.

## Что входит в модуль

- `RpgStatsManager` — HP, уровень, баффы, статус-эффекты, регенерация, сохранение через `SaveProvider`.
- `BuffDefinition` — временные баффы с длительностью и модификаторами статов.
- `StatusEffectDefinition` — статус-эффекты (яд, замедление, DoT).
- `RpgProfileData` — сериализуемый профиль для persistence.

## Persistence

- Профиль хранится через `SaveProvider`, а не через сценовый `SaveManager`.
- По умолчанию используется ключ `RpgV1.Profile`, но его можно поменять в `RpgStatsManager`.

## Интеграция с Progression

- Уровень в `RpgStatsManager` хранится отдельно от `ProgressionManager`.
- Для синхронизации можно вызывать `SetLevel(ProgressionManager.Instance.CurrentLevel)` при изменении прогрессии.

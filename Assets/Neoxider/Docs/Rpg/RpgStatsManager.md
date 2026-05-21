# RpgStatsManager

`RpgStatsManager` удалён в v8.4.0.

Используйте [RpgCharacter](./RpgCharacter.md). Он заменяет прежний singleton-профиль и теперь является единственным источником правды для HP/Mana/Stamina/custom resources, статов, уровня, XP, upgrade points, баффов, статусов, сохранения и сетевой синхронизации.

## Миграция

1. Добавьте `RpgCharacter` на объект игрока.
2. Настройте resources/stats через `RpgCharacterTemplate` или локальные поля компонента.
3. Включите persistence и задайте save key, если нужен save/load.
4. Замените обращения `RpgStatsManager.Instance` на явную ссылку `RpgCharacter`.
5. Для NoCode используйте `RpgNoCodeAction` и `RpgConditionAdapter`.

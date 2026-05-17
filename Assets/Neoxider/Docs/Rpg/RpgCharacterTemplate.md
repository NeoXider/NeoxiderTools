# RpgCharacterTemplate

**Что это:** ScriptableObject-архетип для `RpgCharacter`. Хранит стартовые ресурсы, статы, известные баффы/статусы, progression и display-данные.

**Создание:** `Create -> Neoxider -> RPG -> Character Template`.

## Поля

| Поле | Назначение |
|------|------------|
| `resources` | Пулы `HP`, `Mana`, `Stamina`, `Shield` или custom ID (`DarkMana`, `Rage`) |
| `stats` | Однозначные характеристики: `Strength`, `Defense`, `FireResist` или custom ID |
| `knownBuffs` | SO-баффы, доступные через `RpgCharacter.ApplyBuffById(id)` |
| `knownStatuses` | SO-статусы, доступные через `RpgCharacter.ApplyStatusById(id)` |
| `progression` | Правила роста уровня и manual upgrades |
| `displayName`, `description`, `icon` | Данные для UI/инвентаря/выбора персонажа |

## Использование

1. Создайте template.
2. Заполните ресурсы и статы через `RpgStatId`: preset или `Custom`.
3. Назначьте template в `RpgCharacter`.
4. Оставьте `Apply Template On Awake` включённым для автоматической инициализации.

Runtime API: `RpgCharacter.ApplyTemplate(template)`.

# Сценарии использования

**Что это:** набор практических сценариев для `Progression` с примерами, какие asset'ы и настройки стоит использовать в разных типах игр. Основано на модуле в `Scripts/Progression/`.

**Как использовать:**
1. Выберите ближайший тип игры.
2. Соберите `LevelCurveDefinition`, `UnlockTreeDefinition` и `PerkTreeDefinition` по рекомендациям ниже.
3. Подключите `ProgressionManager` и нужные no-code bridges.

**Навигация:** [← К Progression](./README.md)

---

## 1. Arcade / Hypercasual meta

Подходит для:
- раннеров
- merge/idle mini-meta
- arcade loop с постоянными короткими сессиями

Рекомендуемые настройки:
- `LevelCurveDefinition`:
  - 5-20 уровней на первую итерацию
  - низкий рост порога XP
  - награды за уровень через `Money` и иногда `PerkPoints`
- `UnlockTreeDefinition`:
  - открытие новых скинов, усилений, арен
  - минимальное число prerequisite-связей
- `PerkTreeDefinition`:
  - короткие ветки на 3-6 перков
  - дешёвые стоимости

Что обычно подключать:
- `Money`
- `Collection`
- `ProgressionNoCodeAction`

## 2. Midcore RPG / Action RPG

Подходит для:
- экшен-RPG
- dungeon crawler
- character build systems

Рекомендуемые настройки:
- `LevelCurveDefinition`:
  - длинная шкала уровней
  - награды за уровень почти всегда дают `PerkPoints`
  - rewards уровня лучше держать редкими и значимыми
- `UnlockTreeDefinition`:
  - ветки по archetype или progression tier
  - prerequisite-цепочки и level gate
  - условия через `ConditionEntry`, если узлы зависят от сюжетного прогресса
- `PerkTreeDefinition`:
  - несколько веток билдов
  - requirement через unlocked nodes
  - стоимости 1-5 perk points с ростом к глубине дерева

Что обычно подключать:
- `QuestManager`
- `Money`
- `ProgressionConditionAdapter`
- UI экраны статуса, дерева и наград

## 3. Strategy / City builder / Colony meta

Подходит для:
- стратегии
- city builder
- management games

Рекомендуемые настройки:
- `LevelCurveDefinition`:
  - XP идёт за миссии, апгрейды, completed goals
  - уровни могут открывать tech tiers
- `UnlockTreeDefinition`:
  - использовать как technology tree
  - node rewards открывают системы, building gates, коллекции, квесты
- `PerkTreeDefinition`:
  - использовать как policy/leader bonuses
  - часто дешевле и шире, чем RPG perks

Что обычно подключать:
- `Quest`
- `Collection`
- `Shop/Money`

## 4. Narrative / Quest-driven progression

Подходит для:
- story-driven games
- adventure
- mission hub

Рекомендуемые настройки:
- `LevelCurveDefinition`:
  - можно сделать короткой или вообще использовать только для meta-rank
- `UnlockTreeDefinition`:
  - открытие сюжетных веток, NPC, регионов, функций UI
  - активная интеграция с `Condition` и `Quest`
- `PerkTreeDefinition`:
  - редко боевые, чаще utility или social perks

Что обычно подключать:
- `QuestManager`
- `ProgressionConditionAdapter`
- `ProgressionNoCodeAction`

## 5. Roguelite meta progression

Подходит для:
- roguelite
- run-based progression
- permanent unlock economy

Рекомендуемые настройки:
- `LevelCurveDefinition`:
  - мета-XP за run completion
  - награды за уровень в `PerkPoints` или `Money`
- `UnlockTreeDefinition`:
  - permanent unlocks оружия, relic pools, rooms, classes
- `PerkTreeDefinition`:
  - account-wide meta upgrades
  - желательно избегать слишком длинных prerequisite-цепей

Что обычно подключать:
- `Money`
- `Collection`
- `Quest` при наличии meta-objectives

## Практические шаблоны настройки

### Если нужен только уровень игрока

Используйте:
- `LevelCurveDefinition`
- `ProgressionManager`

Можно не назначать:
- `UnlockTreeDefinition`
- `PerkTreeDefinition`

### Если нужно дерево технологий без XP

Используйте:
- `UnlockTreeDefinition`
- `ProgressionManager`

XP-кривую можно оставить минимальной:
- 1 уровень на `0 XP`

### Если нужны только perk points и perks

Используйте:
- короткую `LevelCurveDefinition`, которая выдаёт только `PerkPoints`
- `PerkTreeDefinition`

### Если нужен полностью no-code pipeline

Используйте:
- `ProgressionManager`
- `ProgressionNoCodeAction`
- `ProgressionConditionAdapter`
- `Quest`, `Money`, `Collection` как получатели rewards

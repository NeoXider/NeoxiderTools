# NPC

**Что это:** модуль `NPC` отвечает за перемещение и поведение неигровых персонажей: навигацию, патруль, follow-режим, синхронизацию аниматора со скоростью движения и модульную интеграцию с `Neo.Rpg` для боевых NPC. Скрипты лежат в `Scripts/NPC/`.

**Оглавление:** [← К Docs](../README.md) · список основных страниц ниже

---

- **[NpcNavigation](./Navigation/NPCNavigation.md)** — навигация (Follow / Patrol / Combined).
- **[NpcAnimatorDriver](./NpcAnimatorDriver.md)** — автоматическое управление аниматором по скорости NavMeshAgent (параметры Speed и IsMoving). Добавьте на тот же объект, что NpcNavigation и Animator — анимации ходьбы/бега включаются при движении.
- **[NpcRpgCombatBrain](./Combat/NpcRpgCombatBrain.md)** — отдельный brain для автоматических melee/ranged NPC на базе `RpgTargetSelector` и `RpgAttackController`.
- **[NpcCombatPreset](./Combat/NpcCombatPreset.md)** — ScriptableObject с боевым профилем NPC: preferred distance, lose distance, restore mode, run/stop/face target flags.
- **[NpcCombatScenarios](./Combat/NpcCombatScenarios.md)** — готовые сценарии сборки melee, ranged, patrol-combat и stationary NPC.

## Как использовать

1. Добавьте `NpcNavigation` на объект с `NavMeshAgent`.
2. Настройте режим поведения, цели или точки патруля.
3. Если нужна автоматическая синхронизация анимации, добавьте `NpcAnimatorDriver`.
4. Для боевого NPC добавьте `RpgCombatant`, `RpgTargetSelector`, `RpgAttackController`, `NpcRpgCombatBrain` и назначьте `NpcCombatPreset`.
5. Для melee/ranged варианта обычно меняется только `RpgAttackPreset`/`NpcCombatPreset`, а не архитектура NPC.

## Что входит в модуль

- `NpcNavigation` — основной runtime-компонент навигации.
- `NpcAnimatorDriver` — мост между скоростью `NavMeshAgent` и Animator-параметрами.
- `NpcRpgCombatBrain` — тонкий orchestration-слой между навигацией, target selector и RPG-атаками.
- `NpcCombatPreset` — переиспользуемый профиль поведения для melee/ranged/spell NPC.

## Типовой сценарий

- NPC получает цель или маршрут.
- `NpcNavigation` двигает агента.
- `NpcAnimatorDriver` обновляет параметры аниматора на основе движения.
- Для боевого NPC `NpcRpgCombatBrain` принимает решение `Acquire -> Chase -> Hold -> Attack`, а `RpgAttackController` исполняет конкретный `RpgAttackPreset`.

## Сборка "из коробочек"

Рекомендуемая композиция для расширяемого NPC:

- База движения: `NavMeshAgent` + `NpcNavigation`
- Анимация: `NpcAnimatorDriver`
- RPG-актор: `RpgCombatant`
- Выбор цели: `RpgTargetSelector`
- Исполнение атаки: `RpgAttackController`
- Поведение: `NpcRpgCombatBrain`
- Конфиги: `RpgAttackDefinition` + `RpgAttackPreset` + `NpcCombatPreset`

Такой стек не заставляет писать отдельный огромный скрипт под каждого нового врага. Обычно новый тип NPC собирается перестановкой готовых компонентов и заменой пресетов.

## Готовые сценарии

Если нужен быстрый практический старт, используйте страницу **[NpcCombatScenarios](./Combat/NpcCombatScenarios.md)**. В ней есть готовые схемы для:

- автоматического melee NPC
- ranged NPC, который держит дистанцию
- patrol enemy с возвратом к маршруту после боя
- stationary turret / mage
- NPC с разными приоритетами выбора цели















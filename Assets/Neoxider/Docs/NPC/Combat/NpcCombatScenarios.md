# Npc Combat Scenarios

**Что это:** готовые сценарии сборки автоматических NPC на базе `NpcNavigation`, `NpcRpgCombatBrain`, `RpgTargetSelector` и `RpgAttackController`.

Эта страница нужна как практический cookbook: что добавить на объект, что настроить и как получить нужный тип врага без отдельного большого скрипта.

---

## Общая база для всех сценариев

На базового боевого NPC обычно добавляются:

1. `NavMeshAgent`
2. `NpcNavigation`
3. `NpcAnimatorDriver` при необходимости
4. `RpgCombatant`
5. `RpgTargetSelector`
6. `RpgAttackController`
7. `NpcRpgCombatBrain`

Assets-конфиги:

1. `RpgAttackDefinition`
2. `RpgAttackPreset`
3. `NpcCombatPreset`

Общий flow:

1. `RpgTargetSelector` находит цель
2. `NpcRpgCombatBrain` решает, надо ли догонять цель или уже можно атаковать
3. `NpcNavigation` подводит NPC к нужной дистанции
4. `RpgAttackController` исполняет атаку по preset

## Сценарий 1. Автоматический melee NPC

Подходит для врагов, которые должны сами идти к игроку и бить вблизи.

### Настройка компонентов

- `NpcNavigation`
- Режим можно оставить `Patrol` или `Combined`, если NPC должен сначала ходить по точкам
- `RpgAttackController`
- `Enable Built-in Input`: `false`
- `NpcRpgCombatBrain`
- `Auto Acquire Target`: `true`

### Настройка `RpgAttackDefinition`

- `DeliveryType`: `Direct` или `Area`
- `HitMode`: `Damage`
- `Range`: `2 - 3`
- `Radius`: `0.25 - 1.25`
- `Cooldown`: по балансу
- `TargetLayers`: слой игрока/целей

### Настройка `RpgAttackPreset`

- `RequireTarget`: `true`
- `UseSelectorComponentWhenAvailable`: `true`
- `AimAtTarget`: `true`
- `TargetQuery.SelectionMode`: обычно `Nearest`
- `TargetQuery.Range`: чуть больше дистанции агра, например `10 - 15`

### Настройка `NpcCombatPreset`

- `Preferred Attack Distance`: `1.5 - 2.2`
- `Lose Target Distance`: `10 - 15`
- `Run While Chasing`: `true`
- `Stop Movement Inside Attack Range`: `true`
- `Face Target Before Attack`: `true`

### Что получится

NPC сам найдёт ближайшую цель, подбежит, остановится вблизи и начнёт бить автоматически по кулдауну.

## Сценарий 2. Автоматический ranged NPC

Подходит для лучников, стрелков, магов и врагов с projectile attack.

### Настройка `RpgAttackDefinition`

- `DeliveryType`: `Projectile`
- `ProjectilePrefab`: назначить prefab с `RpgProjectile`
- `Range`: `8 - 20`
- `Radius`: `0` или небольшой splash
- `Cooldown`: по балансу
- `ProjectileSpeed`: по типу оружия
- `TargetLayers`: слой игрока/целей

### Настройка `RpgAttackPreset`

- `RequireTarget`: `true`
- `AimAtTarget`: `true`
- `TargetQuery.SelectionMode`: обычно `Nearest`
- `TargetQuery.Range`: больше или равен рабочей дистанции атаки

### Настройка `NpcCombatPreset`

- `Preferred Attack Distance`: `6 - 12`
- `Lose Target Distance`: `15 - 25`
- `Run While Chasing`: `true`
- `Stop Movement Inside Attack Range`: `true`
- `Face Target Before Attack`: `true`

### Что получится

NPC сам подойдёт только до нужной дистанции, затем остановится и будет атаковать цель издалека, не врезаясь в неё как melee враг.

## Сценарий 3. Patrol -> aggro -> attack

Подходит для врагов, которые патрулируют территорию и вступают в бой при появлении игрока.

### Настройка `NpcNavigation`

- `Mode`: `Combined`
- `Patrol Points` или `Patrol Zone`: назначить
- `Combined Target`: можно оставить пустым, если бой будет брать цель через `RpgTargetSelector`

### Настройка `NpcCombatPreset`

- `Auto Restore Navigation Mode`: `true`

### Что получится

NPC патрулирует, затем `NpcRpgCombatBrain` переводит его в боевой follow/attack flow. Когда цель потеряна, старый режим навигации восстанавливается, и NPC снова возвращается к patrol поведению.

## Сценарий 4. Stationary turret / mage

Подходит для турелей, кастеров или охранников, которым не нужно идти к цели.

### Настройка `NpcCombatPreset`

- `Preferred Attack Distance`: под дальность умения
- `Lose Target Distance`: по зоне интереса
- `Run While Chasing`: `false`
- `Stop Movement Inside Attack Range`: `true`

### Дополнительно

- Если движение не нужно вообще, не давайте NPC сценарий, требующий догонять цель
- Можно использовать большой `TargetQuery.Range`, чтобы turret выбирала цель в своей зоне

### Что получится

NPC остаётся на месте, автоматически выбирает цель и атакует без movement-heavy поведения.

## Сценарий 5. Приоритет самого слабого/раненого

Подходит для скиллов, хилеров, support NPC и специальных врагов.

### Настройка `RpgTargetSelector`

В `RpgTargetQuery` можно выбрать:

- `LowestCurrentHp`
- `LowestHpPercent`
- `HighestLevel`
- `Random`

### Что получится

Поведение меняется без нового скрипта. Тот же `NpcRpgCombatBrain` продолжает работать, но цель выбирается по другой стратегии.

## Практические советы

- Для NPC почти всегда отключайте `Enable Built-in Input` у `RpgAttackController`
- Для melee и ranged NPC меняйте в первую очередь `RpgAttackDefinition`, `RpgAttackPreset` и `NpcCombatPreset`, а не код
- Если NPC должен быстро тестироваться в инспекторе, используйте `[Button]` методы `SelectTarget()`, `EvaluateNow()` и `ForceAttack()`
- Если нужно ещё больше логики, лучше добавлять новые маленькие компоненты над brain/preset, а не наращивать один giant controller

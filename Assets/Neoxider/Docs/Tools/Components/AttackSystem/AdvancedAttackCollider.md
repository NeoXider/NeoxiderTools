# AdvancedAttackCollider

## Введение
Универсальный компонент для обработки попаданий оружия ближнего/дальнего боя в 2D/3D через триггеры и коллизии с фильтрацией по слоям.

## Описание класса
- Namespace: `Neo.Tools`
- Путь: `Assets/Neoxider/Scripts/Tools/Components/AttackSystem/AdvancedAttackCollider.cs`

## Публичные поля
- Настройки атаки:
  - `int AttackDamage`
  - `float triggerDuration`
- Настройки авто-управления:
  - `bool autoManageColliders` — если включено, компонент сам включает/выключает коллайдеры на время активации. По умолчанию false.
- Коллайдеры:
  - `Collider2D collider2D`
  - `Collider collider3D`
- Режимы обработки:
  - `bool use2D` — включить 2D (по умолчанию true)
  - `bool use3D` — включить 3D (по умолчанию true)
  - `bool useTrigger` — обрабатывать триггеры (по умолчанию true)
  - `bool useCollision` — обрабатывать коллизии (по умолчанию true)
- Фильтрация:
  - `LayerMask hittableLayers`
- Игнор целей:
  - `GameObject[] ignoreObjects`
- Уничтожение при попадании:
  - `bool destroySelfOnHit` — уничтожить этот объект при попадании (по умолчанию false)
  - `bool destroyTargetOnHit` — уничтожить объект-цель при попадании (по умолчанию false)
- Сила/эффекты:
  - `bool applyForceOnHit`, `float forceMagnitude`, `float forceDuration`
  - `ForceMode forceMode3D`, `ForceMode2D forceMode2D`
  - `bool scaleForceByMass`
  - `bool useAdvancedForceApplier` — если включено, сила через Rigidbody не применяется; используется `AdvancedForceApplier`.
  - `GameObject attackEffectPrefab`

## События
- `UnityEvent<GameObject> OnHit` — единое событие попадания (цель `GameObject`) для всех режимов (2D/3D, Trigger/Collision).
- `UnityEvent<Collider2D> OnAttackTriggerEnter2D` — обратная совместимость для 2D триггера.
- `UnityEvent<Collider> OnAttackTriggerEnter3D` — обратная совместимость для 3D триггера.
- `UnityEvent OnDeactivateTrigger` — деактивация после `triggerDuration` (если авто-управление включено).

## Поведение
- Авто-детект коллайдеров, если не назначены: `GetComponent<Collider2D>()`, `GetComponent<Collider>()`.
- Фильтр слоёв: `PassesLayer(int layer) => (hittableLayers.value & (1 << layer)) != 0`.
- Исключение повторных попаданий в течение одной активации — по коллайдеру цели.
- Игнор целей: если цель в `ignoreObjects`, урон/события не применяются, уничтожение тоже не выполняется.
- Активация:
  - `ActivateTrigger(int damage = AttackDamage)` очищает трекер попаданий.
  - Если `autoManageColliders == true`: включает коллайдеры и планирует отключение через `triggerDuration`.
  - Если `autoManageColliders == false`: коллайдеры не трогает — вы управляете ими вручную (например, через анимационные ивенты).
- Сила:
  - Если `useAdvancedForceApplier == true`: силы через `Rigidbody/Rigidbody2D` не применяются; используется `AdvancedForceApplier` (если найден на цели).
  - Если `useAdvancedForceApplier == false`: применяется сила к `Rigidbody`/`Rigidbody2D` с выбранным режимом и масштабированием по массе.
- Уничтожение:
  - Если `destroyTargetOnHit == true` — после применения урона/событий цель уничтожается.
  - Если `destroySelfOnHit == true` — после обработки попадания уничтожается текущий объект (например, пуля).

## Пример использования
- Ближний бой: включить триггер на анимационном событии удара (autoManageColliders можно оставить выключенным).
- Дальний бой: на снаряде использовать коллизию или триггер; подписаться на `OnHit` для применения урона/эффекта; включить `destroySelfOnHit`.

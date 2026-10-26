# Neo.Abilities: план после аудита

Основание: [аудит](./Neo.Abilities.md), [независимая проверка](./Neo.Abilities-review.md), `manifest.json`, baseline `ca198556`, пакет 10.19.0. План подготовлен тем же аудитором в исходной сессии. Это будущая работа; на этапе планирования исходники, тесты и публичная документация не меняются.

## Текущий результат

Прочитаны 72 runtime-файла. Завершены AB-01–AB-13: стоимость, заряды/CDR, DoT, модификаторы, ресурсы, щиты, CastId и lifecycle компонентов. Сохранены публичные сигнатуры, serialized fields и отложенный `SetLevelSource`.

Коррекции QA также завершены: NaN/Infinity/overflow отклоняются до оплаты; щит после callback проверяет актуальную активность/ёмкость. При `ShieldBroken` старый экземпляр остаётся доступен собственной реакции, но внутренний флаг исключает повторное поглощение и refresh/stack reuse. Замена переживает удаление старого в `finally`. Порядок `shield_absorbed → shield_broken → modifier_removed` сохранён; применение замены возможно внутри break callback.

Обновлены десять существующих страниц документации. Добавлены 27 методов / 37 тестовых случаев. Статическая независимая проверка прошла; проверены UTF-8, явные типы, GUID и diff. **Unity-компиляция, исполнение тестов и PlayMode пока не выполнены. Эти исправления не являются pending-задачами.**

## Приоритеты остатка

Пути `Domain/...` и `Components/...` ниже относятся к `Assets/Neoxider/Scripts/Abilities/`.

| Приоритет / статус | Evidence и воспроизведение | Владелец, минимальный следующий шаг и результат |
|---|---|---|
| P1: долг проверки | Новые lifecycle-тесты вручную вызывают методы через существующий `AbilitySceneRig`; это не Unity lifecycle. | Координатор: compile/import обеих веток Unity, весь непустой `Neo.Editor.Tests.Abilities`, затем реальные disable/re-enable, pooling уровня A→B и auto-cast. Ожидание: все 37 новых случаев и существующие регрессии проходят; новых ошибок консоли нет. |
| P2: подтверждённая несогласованность каталога | `Domain/AbilitySystem.cs`, `RegisterAbility`/`GrantAbility`/`Cast`; `Domain/Casting/AbilitySlot.cs`. Выдать способность, заменить тот же id blueprint с другими charges/cooldown: Cast читает новый каталог, slot продолжает использовать старый blueprint. | Neo.Abilities: сначала публичная регрессия, затем выполнять весь cast по snapshot `slot.Blueprint`. Новая регистрация действует на последующие выдачи; revoke/grant явно обновляет существующую. Проверить cooldown, charges, effects и level; описать семантику замены. |
| P2: подтверждённая частичная оплата custom pool | Cast использует `Resources.Decrease`: mana=100, cost=10, `MaxDecreaseAmount=1` — платится 1, cast успешен. Callbacks дополнительно меняют следующие стоимости/ресурсы. | Сначала владелец Neo.Core: `ResourcePoolModel.TrySpendBatch(IReadOnlyList<ResourceCost>, out string)` с проверкой caps/сумм и уведомлениями после записи пулов. Файлы: `Assets/Neoxider/Scripts/Core/Resources/Domain/ResourcePoolModel.cs`, новый соседний `ResourceCost.cs`. Затем Neo.Abilities использует захваченные стоимости. Текущий `TrySpend` проверяет cap, но последовательные вызовы не атомарны. Определить повторный Cast во время commit и исключения listeners. |
| P2: подтверждённое отключение внутренних реакций | `Domain/Events/AbilityEventBus.cs:Clear` очищает подписку конструктора `Domain/AbilitySystem.cs`. После `system.Events.Clear()` modifier reaction больше не исполняется. | Neo.Abilities: отделить внутренний обработчик от пользовательских списков в этих двух файлах. Сохранить порядок specific handlers → внутренняя реакция → any handlers. Clear удаляет пользовательские подписки; thorns после очистки продолжает работать. |
| P3: lifecycle-риск, исполнение не подтверждено | `Components/AbilityProjectileBehaviour.cs` освобождает cast только в `Despawn`; внешние disable/destroy обходят этот путь до domain timeout. | Neo.Abilities: воспроизвести в PlayMode. Если подтверждено, единый идемпотентный cancel из `OnDisable`/`OnDestroy` и `IPoolable.OnPoolRelease`, без повторного `PoolManager.Release`. После отмены NotifyProjectileHit возвращает false; повторная выдача не затрагивает предыдущий cast. |

Оставшиеся риски не объявляются доказанными дефектами. `OnModifierTick`/`HandleModifierReactions` пересоздают RNG из seed экземпляра; независимые временные броски требуют replay-политики. `_versions` сохраняет владельцев после churn: измерить удержание перед очисткой после DestroyUnit. Lethal health-cost, catch-up лимит 8 и повторный `Tick` требуют отдельного контракта.

## Существенное улучшение: подключаемая доставка со своим шагом

Разработчику нужен один способ выразить allied heal, point blast и directional skillshot. Сейчас `SpawnRequest.Direction` одновременно задаёт направление и неявную конечную точку; стандартный снаряд проверяет только enemies и отдельные кадры, использует scene time независимо от domain pause. Это документированные ограничения; фактический tunneling ещё нужно воспроизвести.

Переиспользовать `IAbilityWorldAdapter`, `QueryUnitsInRadius`, `UnitId`, `TargetingMode`, `AbilityTeamFilter`, `NotifyProjectileHit`, `ReleaseProjectileCast`, `PoolManager.Get/Release` и `IPoolable`. Нового пула или combat pipeline не требуется. Существующего typed delivery/segment API в runtime-поиске не найдено.

Предлагаемые дополнения в Neo.Abilities:

```csharp
public interface IAbilityDeliveryHost
{
    bool Begin(in AbilityDeliveryRequest request);
    void Step(float deltaTime);
    bool Cancel(uint castId);
}
```

`AbilityDeliveryRequest` — value-only данные: CastId, Owner, AbilityId, Targeting, TargetUnit, HasTargetPoint/TargetPoint, Direction, Range, TeamFilter, ArchetypeId, Speed. Нормализованное направление отделено от endpoint. Настройки lifetime/radius/pierce поступают из host/prefab configuration. Дополнительный необязательный `IAbilitySegmentQuery.QuerySegment(from, to, radius, results)` заполняет `List<AbilitySegmentHit>`; fallback использует текущий radius-query как broadphase и расстояние до отрезка. Итоговый порядок: расстояние вдоль сегмента, затем UnitId.

Точные точки реализации:

1. Новые `Domain/Delivery/AbilityDeliveryRequest.cs`, `Domain/Delivery/IAbilityDeliveryHost.cs`, `Domain/Delivery/IAbilitySegmentQuery.cs`, `Domain/Delivery/AbilitySegmentHit.cs`, `Domain/Delivery/ProjectileSimulation.cs`: plain C# модель шага, distinct pierce и идемпотентная отмена.
2. `Domain/AbilitySystem.cs`: необязательный `DeliveryHost`, полный контекст и один Step после ресурсов в Tick. Отказ Begin очищает pending и публикует `AbilityEvents.DeliveryFailed` с CastId; константа добавляется в `Domain/Events/AbilityEvents.cs`. Автоматического возврата оплаты нет.
3. Новый опциональный `Components/AbilityProjectileHostBehaviour.cs`; `Components/AbilitySystemBehaviour.cs` подключает его явно. `Components/AbilityProjectileBehaviour.cs` становится visual adapter для нового пути, сохраняя старый OnSpawned/Update для legacy. Использовать существующий pool lifecycle. Neo.Tools.Spawner менять только при отдельно подтверждённом отсутствии нужного hook; текущий `IPoolable` его предоставляет.
4. После domain-тестов добавить scene/NoCode настройку host. Владелец Neo.Abilities.Editor при необходимости показывает validation/подсказки; runtime не зависит от Editor.

Совместимость: `SpawnRequest`, `IAbilityWorldAdapter`, enums, serialized names, asset GUID и старые prefabs сохраняются. Отсутствие нового host выбирает прежний путь; enemy-only/scene-clock настройки не меняются автоматически. ScriptableObjects хранят данные, ссылки на scene host остаются у компонентов. Оценка — средний объём, ориентировочно 3–5 рабочих дней плюс Unity QA при сохранении текущего renderer и пула; основной риск — двойное владение движением и release.

## Порядок и условия завершения

Сначала центральная проверка текущих исправлений; затем узкие регрессии и коррекции каталога/Clear/lifecycle, совместная стоимость после API Neo.Core. После этого typed delivery, plain C# модель, component adapter, Editor helpers и документация. README остаётся входом: C# injection/Step/Cancel, отдельно Inspector/NoCode workflow и ограничения legacy. Примеры используют явные типы.

Приёмка доставки: allied heal; одинаковая дальность направлений разной длины; точная point-детонация; пересечение маленькой цели за шаг; стабильный порядок равноудалённых hits; один hit на pierced unit; pause/manual stepping; повторная отмена и pooling; ровно один release; отсутствие pending после отказа/отмены. Сначала EditMode fake world, затем PlayMode с настоящим пулом. Повтор одинакового seed/шагов должен давать одинаковые receipts. Профиль CPU/GC зафиксировать отдельно, без заявления нулевых аллокаций до измерения.

Compile gates обязательны в Unity 6.0–6.4 и 6.5+, включая обе ветки lifecycle attributes. Новые version-specific вызовы проходят через общие `NeoDiagnostics`/`NeoComponentHealth`, без распределённых `#if`. Проверить EditMode, выбранные PlayMode и mojibake-test. Release validation учитывает `Samples~`, developer `Samples` и импортированный sample root; миграция layout не нужна. При красной регрессии остановить API-расширение и исправить её. Не менять default clock/collision, replay или health-cost политику без зафиксированного решения координатора; обычные безопасные исправления дополнительного разрешения не требуют.

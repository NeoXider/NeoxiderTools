# Neo.Tools.Spawner: индивидуальный план реализации

Фаза 2, 2026-10-23. Продолжение исходного аудита; gpt-6.1-sol, reasoning max. Это план будущей реализации, а не выполненная доработка. В этой фазе записывается только данный файл.

## Текущее состояние

Аудит прочитал все 10 исходников и исправил SP-01–SP-11: чужие возвраты и уничтоженные записи пула, границы размеров/конфигурации, fallback storage, disable/reentry Despawner, повторный запуск coroutine, поколения выдач при Clear/count/expiry, капсулы, двойные host-события и сетевой unspawn. Изменены 8 исходников и 10 существующих английских руководств; canonical entry — `Assets/Neoxider/Docs/Tools/Spawner/README.md`.

Написано 29 регрессионных случаев: 15 EditMode, 12 PlayMode, 2 условных Mirror. Выполнены только статические проверки: UTF-8, ссылки, GUID, diff whitespace, сохранение извлечённых имён полей/публичных сигнатур, баланс обоих MIRROR-путей. Компиляция, physics, weaving и тесты не запускались. Повторное чтение текущего кода подтверждает наличие исправлений; заново реализовывать их не требуется. Других подтверждённых незакрытых runtime-дефектов аудит не установил.

Ниже `S/` означает `Assets/Neoxider/Scripts/Tools/Spawner/`, `D/` — `Assets/Neoxider/Docs/Tools/Spawner/`. Приоритет P0 обозначает интеграционный gate, а не новый найденный дефект.

## Проверка и оставшиеся риски

| Приоритет / статус | Основание и воспроизведение | Владелец, минимальный следующий шаг и наблюдаемый результат |
|---|---|---|
| P0, долг компиляции | `S/NeoObjectPool.cs:176`, `S/Spawner.cs:477`; новая Mirror-ветка ещё не компилировалась. | Координатор после окончания изменений других аудиторов: compile/import, weaving, затем существующие и новые fixture. При ошибке Spawner исправляет только подтверждённый локальный участок; зависимости передаются их владельцам. Ноль новых ошибок; ожидаемые тесты действительно обнаружены. |
| P1, долг сетевой проверки | `OnReleaseToPool`, host RPC и `Audit20261023NeoToolsSpawnerNetworkTests.cs`. Нужны return/reissue на host и отдельном клиенте, включая пользовательский unspawn-handler. | Координатор + Neo.Network. Старый netId исчезает из обоих реестров, серверный GameObject сохраняется, повторная выдача получает новый netId, каждый callback/event возникает один раз. При несовместимости handler сначала фиксируется воспроизведение, затем согласуется точечный release-guard; новый сетевой слой заранее не вводится. |
| P1, неподтверждённый риск сцен | `S/SpawnUtility.cs:23–28,140–145,190–220`: Single безусловно вызывает Clear, а fallback может впервые создаваться в Awake входящей сцены. Сценарий: Spawn в Awake, затем завершение Single load. | Координатор проверяет фактический порядок и сохранность новой выдачи. Только при воспроизведении Spawner отличает старое storage от созданного входящей сценой; нельзя очищать новую выдачу. Отдельно проверить additive, persistent root, два play-session без domain reload и поздний return объекта с внешним parent. |
| P1, долг регрессий | Новые Edit/Play fixture и существующие `SpawnerTests`, `AuditFixesSpawnerTests`, `SpawnerDenyZoneTests`. | Координатор: чужой return сохраняет активность; мёртвая запись заменяется; сохраняется 101 объект при max=0; Stop/Start создаёт ровно одну новую production-loop; старый Clear не забирает чужое поколение; release-callback сохраняет созданный им объект. |
| P2, ограничения и риски | `Spawner.GetRandomPointInCollider`, mutable `SpawnedObjects`, component-owned expiry; callback/clear-контракты описаны в аудите. | Spawner + координатор: выполнить containment для X/Y/Z, rotation и scale; проверить callback-reentry и disable. Отмена expiry при выключении GameObject, прежний pose в OnPoolGet и warmup callbacks — документированные контракты, не объявлять их неисправностями. Изменения политики выделены ниже. |

`Neo.Tests.Play.asmdef` имеет нужные references, но не имеет MIRROR versionDefine. Координатор проверяет effective symbols и обнаружение именно двух сетевых тестов; при отсутствии символа устраняет конфигурационный пробел централизованно, не считает пропуск успехом.

## Существенное улучшение: типизированная выдача PoolLease

**Предложение P2, не реализовано.** Разработчик сейчас вручную переносит owner/generation между delayed actions, Spawner и Despawner; один GameObject не идентифицирует выдачу. Предлагается первый самостоятельный инкремент большого предложения аудита: безопасный handle без нового менеджера или отдельного глобального registry.

Повторно использовать `PooledObjectInfo.OwnerPool`, `SpawnGeneration`, `IsInPool`, существующие GetObject/ReturnObject и общий release с Mirror unspawn. Предлагаемый API в `Neo.Tools`:

- `readonly struct PoolLease`: `GameObject Instance`, `int Generation`, `bool IsCurrent`, `bool TryReturn()`; внутри также захвачена ссылка на owner.
- `PooledObjectInfo.CaptureLease()` и `NeoObjectPool.Rent(Vector3 position, Quaternion rotation)` возвращают `PoolLease`.
- `SpawnUtility.TrySpawnLease(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, out PoolLease lease)` сохраняет manager/fallback routing; null возвращает false/default.
- `Spawner.ActiveLeases` предоставляет `IReadOnlyList<PoolLease>` текущих pooled-выдач. Это outstanding leases; активность hierarchy проверяется отдельно. Instantiate-объекты остаются в прежнем SpawnedObjects.

Default, уничтоженный объект, другой owner, возврат или новое поколение делают handle недействительным. Старый `TryReturn()` возвращает false и не меняет события/счётчики; копии актуального handle вместе возвращают объект максимум один раз. Работа остаётся в Unity main thread. Источник истины — существующие metadata, дополнительных ID и version gates не требуется.

**Точные файлы первого инкремента:** новые `S/PoolLease.cs` и `.meta`; изменения `S/NeoObjectPool.cs`, `PooledObjectInfo.cs`, `SpawnUtility.cs`, `Spawner.cs`, `Despawner.cs`. Последние два используют snapshot lease вместо дублирования проверки owner/generation; неприменимый к pooling Instantiate-путь сохраняется. PoolManager и PoolExtensions переиспользуются без обязательного нового API.

Добавить `Assets/Neoxider/Tests/Edit/Tools/Audit20261023NeoToolsSpawnerLeaseTests.cs` и `Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsSpawnerLeasePlayTests.cs`, оба с уникальными `.meta`. Минимум: default/dead/foreign/stale handles; две копии и один release; повторная выдача между producers; старый expiry/Clear; доступ через manager и fallback. Измерять активность, generation, counts и callbacks, не устройство private dictionary.

**Совместимость:** старые сигнатуры, serialized fields, UnityEvents, Get-callback order и warmup остаются прежними. SpawnedObjects не удаляется и не переопределяется; ручное редактирование списка не становится способом передачи владения lease. Новые SO, scene references в SO, миграции сцен/префабов не нужны. Документация: обновить `D/README.md`, `NeoObjectPool.md`, `PooledObjectInfo.md`, `SpawnUtility.md`, `Spawner.md`, `Despawner.md`; показать C# delayed-handle пример и прежнее Inspector/UnityEvent подключение. Новый Editor/NoCode компонент не нужен.

Оценка: 2–3 дня реализации и день интеграции. Основные риски — переходы внутри callbacks, синхронизация legacy списка и рост metadata. Новые тесты должны также удерживать старые контракты.

## Зависимости, порядок и завершение

1. Координатор стабилизирует общий source snapshot; владельцы **Neo.Tools.Managers** подтверждают `Singleton.IsCurrentSingletonInstance`, **Neo.Network** — NeoNetworkComponent и unspawn/handlers. Их файлы здесь не меняются. **Neo.Network.Contracts** потребуется только при будущем выделении optional bridge. Neo.Extensions/Neo.Tools.Random и Neo.PropertyAttribute/Neo.Editor сохраняют существующие API/helpers/buttons; новых зависимостей для leases нет.
2. Выполнить P0/P1 проверки. Сначала минимальные исправления только воспроизведённых сбоев с регрессиями, затем additive lease API, затем внутренние component adapters и документация. Не смешивать исправления аудита с новым контрактом.
3. Отдельные последующие предложения: pose-first `SpawnRequest`, creation-only warmup, раздельные active/retention limits, явные Clear modes и независимый lifetime owner. Меняют поведение и требуют продуктового выбора; не входят в первый инкремент. Выделение Mirror bridge/asmdef согласуют Neo.Network и координатор; unconditional Mirror references сейчас остаются packaging-зависимостью, а не решённой optional integration.
4. Завершение интеграции: compile на Unity 6.0 и 6.4, отдельно 6.5+; реально собрать MIRROR on/off в поддерживаемых конфигурациях. Не размножать Unity-version `#if`; при необходимости использовать централизованные StableId helpers. Прогнать указанные Edit/Play и host/remote сценарии, проверить API/serialization, UTF-8, GUID, ссылки и scoped diff.
5. Release layout остаётся `Samples~`; новые sample assets не планируются. Если координатор добавляет пример, проверить также dev `Samples`, импортированный `Assets/Samples/NeoxiderTools/10.19.0/<sample>/` и package sample paths, не переименовывая layout ради этого плана.

Gate закрывается только результатами исполнения. Ноль обнаруженных сетевых тестов, непроверенная версия Unity, потеря новой выдачи или повторный release оставляют соответствующий пункт открытым. Отсутствие воспроизведения риска фиксируется отдельно; оно не служит поводом для архитектурной переделки. В фазе планирования Unity/build/test процессы, коммиты и дочерние агенты не запускались.

# Neo.Network: план реализации после аудита

Основание: завершённый [аудит](Neo.Network.md), текущий diff и manifest; назначение `gpt-6.1-sol`, `max`. Фаза 2 изменяет только этот план.

Пути: `N` = `Assets/Neoxider/Scripts/Network`, `T` = `Assets/Neoxider/Tests`, `D` = `Assets/Neoxider/Docs/Network`. Относительные пути в таблицах раскрываются через эти корни.

## Текущий результат

Просмотрены все 37 C# файлов. Уже исправлены F1–F12: reset наследников singleton, reflection-вызов с null, пустые каналы, независимые лимиты отправителей, применение OwnerToServer на dedicated server, проверки направления/типа, неконечные snapshot-времена, переполнение fragmentation, URI discovery, отмена подключения и startup/defaults без Mirror. Это завершённые изменения в 12 исходниках, а не следующие задачи.

Обновлены 12 публичных страниц, добавлены четыре `Audit20261023NeoNetwork*Tests.cs` с metadata: 51 authored case с Mirror, 32 без него. Выполнены статические проверки UTF-8, области владения, ссылок, деклараций API/serialized fields, директив/скобок и `git diff --check`. Компиляции, NUnit, PlayMode и player-проверок не было; лексическая проверка двух веток не доказывает их компилируемость.

## Приоритеты и оставшиеся исправления

Дополнительные дефекты ниже подтверждены чтением текущего кода в фазе 2; Unity-воспроизведение ещё требуется. Они не записываются задним числом как исправленные в аудите.

| Приоритет / владелец | Доказательство и минимальное действие | Наблюдаемый результат |
|---|---|---|
| P1, координатор: сборка тестов | `T/Play/Neo.Tests.Play.asmdef` по-прежнему не содержит `Mirror.Components`; новый Lobby fixture использует `NetworkDiscovery` и наследника `NetworkRoomManager`. Добавить reference централизованно. Сверить `MIRROR`/`UNITY_INCLUDE_TESTS` в обеих тестовых assemblies. | Все четыре fixture действительно обнаружены; условные тесты не исчезают из discovery незаметно. |
| P1, координатор + Neo.Network: долг проверки F5/F6 | `Audit20261023NeoNetworkCommandTests.cs:234` вызывает woven receive delegate напрямую, обходя внешний admission/ownership Mirror. Выполнить существующие регрессии и добавить сценарий реального client→server вызова. | Владелец меняет разрешённое значение; чужой клиент и неправильное направление не меняют target или outbound snapshot. Dedicated/host вызывают listeners ровно один раз. |
| P2, Neo.Network: недопустимый reflection member | `N/Core/NetworkPropertySync.cs:404–470` кеширует property без проверки index parameters/getter и передаёт значение напрямую в setter. Target с `int this[int index]`, `_fieldName="Item"` приводит к чтению без индекса. Проверять доступность и совместимость member до чтения/записи; не вводить скрытое narrowing-преобразование. | Ошибочная привязка не пишет gameplay state, не порождает повторяющиеся исключения; одно gated-сообщение. Корректные пять типов продолжают синхронизироваться. |
| P2, Neo.Network: поздний reactive instance | `N/Core/NetworkReactiveSync.cs:294–334`: null reactive-поле устанавливает `_cacheResolved=true`. После инициализации поля последующие значения больше не разрешают ссылку. Не кешировать отсутствие instance как окончательный результат; отделить metadata member от готовности значения. | После null → `ReactivePropertyInt` следующий входящий update меняет `CurrentValue` и уведомляет один раз. Доставка уже полученного значения без нового пакета относится к Bind ниже. |
| P2, Neo.Network: networking clock | `N/Core/NeoNetworkComponent.cs:49–79`, оба sync `Update` и `N/Lobby/NeoNetworkDiscovery.cs:102–132` используют `Time.time`. При timeScale=0 повторный запрос не выходит из лимита, QuickPlay timeout останавливается. Перевести сетевые интервалы на unscaled double clock, сохранив игровые authority/pause guards. | Немедленный повтор отклонён; после 50 ms реального времени запрос проходит и при паузе. QuickPlay завершается по настроенному реальному timeout. |

Минимальные новые регрессии: `T/Edit/Network/Audit20261023NeoNetworkBindingValidationTests.cs` для accessor/type/null readiness; `T/Play/Network/Audit20261023NeoNetworkPausedClockTests.cs` для паузы с обязательным восстановлением timeScale; `T/Play/Network/Audit20261023NeoNetworkBindingLifecycleTests.cs` для настоящего admission, late join и callbacks. Сначала зафиксировать наблюдаемую ошибку, затем минимально исправлять соответствующий owned source.

## Существенное улучшение: типизированное ядро replication

**Предложение, не реализовано; P3 после P1/P2.** Сейчас polling, reflection, authority, dirty state и Mirror hooks объединены в двух MonoBehaviour. Программисту приходится писать отдельный NetworkBehaviour либо настраивать приватные scene fields; начальное значение владельца и повторная привязка не имеют явного контракта.

Повторно использовать `NetworkAuthorityMode`/`INeoOptionalNetworked` из Neo.Network.Contracts без изменения значений, `SyncPropertyDirection`, `NeoNetworkState.IsAuthorized`, `NetworkReactivePropertyBridge.SetFromNetwork`, текущие Mirror serializers/SyncVars и `EqualityComparer<T>.Default`. Делегаты read/apply заменяют необходимость вводить отдельный endpoint-интерфейс. Отдельного готового binding-сервиса в просмотренном Network API нет; новый транспорт или сторонняя библиотека не нужны.

Добавить внутри **Neo.Network**:

- `N/Core/Replication/ReplicationBinding.cs`: `ReplicationBinding<T>(Func<T> read, Action<T> apply, ReplicationOptions options, IEqualityComparer<T> equality = null, Predicate<T> validate = null)`, `TryCapture(double now, out ReplicatedValue<T> update)`, `TryApply(in ReplicatedValue<T> update, in ReplicationPeer peer, double now, out ReplicationRejectReason reason)`, `Bind(Func<T> read, Action<T> apply)`, `Reset()`.
- `N/Core/Replication/ReplicationOptions.cs`: direction, interval и initialization/owner-echo policies.
- `N/Core/Replication/ReplicationPeer.cs`: peer/result/value контракты без Unity/Mirror объектов. Peer context создаёт доверенный адаптер после Mirror admission, не клиентский payload.
- `N/Core/Replication/ReflectionValueEndpoint.cs`: внутренний resolver проверенных read/apply делегатов для Inspector.

Затем адаптировать `N/Core/NetworkPropertySync.cs` и `NetworkReactiveSync.cs`: transport вызывает ядро, reactive apply переиспользует bridge. Bind сбрасывает dirty/cache state и явно гидратирует последний принятый snapshot. Сначала реализовать legacy initialization по умолчанию; server seed, owner seed и snapshot-only сделать явным выбором. Новые компоненты, singleton и custom editor первой итерации не требуются.

Сохранить названия/типы serialized fields, enum numbers, UnityEvents, Command signatures, SyncVar ordering и wire format. Новые политики вводить аддитивно с legacy default; смена default требует описанной миграции. ScriptableObjects допускаются только для данных, scene targets остаются в MonoBehaviour. Новым package-файлам нужны GUID metadata; folder metadata согласует координатор.

Тест ядра: `T/Edit/Network/Audit20261023NeoNetworkReplicationBindingTests.cs` — injected clock, rejection без state/output mutation, dirty threshold, reset/rebind и seed policy. Lifecycle fixture проверяет пять primitive/Vector3 типов, три reactive-типа, нулевые начальные значения, host/remote owner, late join и единственное уведомление. Оценка: 3–5 инженерных дней плюс интеграционные прогоны; основной риск — порядок initial hydration и host hooks.

## Зависимости, порядок и завершение

1. Координатор завершает конкурирующие source-аудиты, исправляет тестовые references и выполняет P1. Затем Neo.Network закрывает P2 отдельными узкими изменениями с регрессиями.
2. Реализовать plain C# ядро, потом existing component adapters. Neo.Reactive владеет подписками/notification API: согласовать ожидаемый single-notify контракт до адаптации. Neo.Network.Contracts владеет общими enums/marker; новые binding-типы пока остаются в Neo.Network, чтобы не создавать обратную зависимость. Neo.Editor владеет `Assets/Neoxider/Editor/Network`; inspector diagnostics добавлять только при необходимости после runtime API, отдельным владельцем.
3. Обновить `D/README.md`, `NetworkPropertySync.md`, `NetworkReactiveSync.md`, `NeoNetworkComponent.md`, `NeoNetworkDiscovery.md`, `Multiplayer_Guide.md`: C# Bind/read/apply пример и Inspector target/type/direction/initialization рецепт. `Contracts.md` остаётся у другого владельца.
4. Централизованно проверить Unity 6.0–6.4 и 6.5+, MIRROR on/off, Network Edit/Play suites и новые fixtures. Версионные Object API проводить через `NeoDiagnostics.StableId`/editor `NeoComponentHealth.StableId`, без распределённых `#if`. Выполнить LAN non-default-port, reconnect, scene-template и IL2CPP/WebGL smoke по изменённым путям.
5. Финализировать UTF-8, ссылки, GUID, serialized/API compatibility и scoped diff. Sample assets менять не требуется; release сохраняет `Samples~`, разработка допускает `Samples`, imported roots — `Assets/Samples/NeoxiderTools/10.19.0/<sample name>`; validators учитывают активный layout.

Остановить расширение scope при несовместимой миграции или невыбранной seed policy; безопасные P1/P2 не требуют нового разрешения. Failed compile/test блокирует выпуск соответствующего изменения. Shared handler ownership (`NetClientHandlers`/`NetServerHandlers`), non-finite settings и private Mirror `hasSpawned` совместимость остаются отдельными неподтверждёнными интеграционно рисками. Ни наличие теста, ни успешный static check не закрывают эти риски; готовность отмечать только по конкретным central compile/runtime результатам.

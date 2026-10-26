# Neo.Rpg.Network — индивидуальный план реализации

Фаза 2, 2026-10-23. Тот же аудитор: `gpt-6.1-sol`, reasoning `max`.
Основание: [аудит](Neo.Rpg.Network.md), контракт планирования, manifest и актуальные исходники.
Сейчас создаётся только план; дальнейшие изменения не реализованы.

## 1. Уже выполнено

Проверены оба принадлежащих сборке C# файла. Исправлены F1–F4: входящие команды и снимки учитывают
`RpgCharacter.isNetworked`; клиентский старт обновляет унаследованный флаг; проверка снарядов использует
тот же fallback identity, что проверка ввода; локальные `Net*` вызовы работают до `Awake`.
Добавлены `Assets/Neoxider/Docs/Rpg/Network.md`, ссылки `NeoDoc`, метаданные и девять случаев в
`Assets/Neoxider/Tests/Play/Network/Audit20261023NeoRpgNetworkRegressionTests.cs`.
Исправления завершены на уровне исходников. Проверки инвентаря, API, команд, UTF-8, `MIRROR`, ссылок,
GUID и `git diff --check` были статическими. Компиляция, weaving, тесты и сетевые процессы не запускались.

## 2. Оставшийся дефект и долг проверки

### P1 / C1 — повторная инициализация при захвате снимка

**Подтверждено текущим исходным кодом, выполнение не проверено.** В
`Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs` метод `EnsureInitialized` вызывает
`RefreshAllDerived(true)` до `_initialized = true`. `RefreshStat → PushSnapshotIfServer` вызывает
`RpgCharacterNetworkAdapter.NotifyStateChanged → CaptureProfile → EnsureInitialized` повторно.
Триггер на сервере: включённый адаптер, `character.Damage(0f)` для разрешения ссылки и `ApplyTemplate`
со stat. Возможны рекурсия и изменение перебираемых коллекций. Регрессии инициализируют шаблон
до адаптера и этот путь не покрывают.

**Владелец первого изменения: `Neo.Rpg`.** Минимальное исправление: подавлять публикацию снимков
на протяжении инициализации/перестройки, начиная до очистки коллекций, и отправлять завершающее
уведомление после согласованного состояния. Защиту снимать через `finally`; не объявлять объект
полностью инициализированным преждевременно. Проверить загрузку профиля и вложенные callbacks.
Адаптерный recursion guard сам по себе не предотвращает первый вложенный rebuild.

В текущем ядре уже есть `RuntimeRebuilt`; переиспользовать его назначение, не переименовывая событие.
Оно вызывается до `LoadProfile`, поэтому само по себе не гарантирует завершение всей загрузки.
После исправления ядра владелец `Neo.Rpg.Network` добавляет
`Assets/Neoxider/Tests/Play/Network/Audit20261023NeoRpgNetworkInitializationTests.cs`:
перестройка сетевого персонажа с ресурсами и stat должна дать точные итоговые значения на источнике
и после сериализации/применения на получателе. Проверять состояние и конечную публикацию, а не только
отсутствие исключения. Не запускать известный рекурсивный сценарий на неисправленном ядре.

### P1 — централизованная проверка уже внесённых изменений

**Владелец: координатор.** После завершения изменяющих исходники аудитов скомпилировать сборку и тесты,
проверить Mirror weaving/регистрацию команд и обнаружение всех девяти случаев, затем выполнить их
и существующий `RpgCharacterNetworkAdapterTests`. Дополнительные ссылки для этих PlayMode-тестов
не нужны. Наблюдаемые результаты: HP 75/80 и единичные события до Awake; HP 100 при opt-out,
75 после разрешённого обновления; немедленный повтор команды не меняет HP; late join учитывает
обе смены флага; fallback identity блокирует клиентский снаряд, локальный объект остаётся разрешён.

### P2 — интеграционные риски, пока не подтверждённые дефекты

Координатор проверяет отдельные сервер/клиент: OwnerOnly/ServerOnly, legacy opt-in, late join, reconnect,
смену владельца, opt-out, scene reactivation и prefab снаряда. Dummy transport доставки не доказывает.
Владельцы сбоев: `Neo.Rpg` — состояние, `Neo.Rpg.Network` — RPG transport, `Neo.Network` — общий
lifecycle/authority. Исправления назначать после воспроизведения.

## 3. P2: согласованная публикация снимков с опциональным объединением

**Существенное предложение, отдельно от обязательного C1.** Сейчас каждое уведомление синхронно
сериализует полный профиль; составные изменения могут сериализовать промежуточные состояния.
Производительность не измерена: сравнить число сериализаций и объём JSON при регенерации до/после.

Переиспользовать `RpgCharacterProfileData`, `RpgCharacterProfileService`, `IRpgCharacterNetworkAdapter`
и SyncVar. Проверенные кандидаты `NetworkPropertySync` (отдельное поле) и
`SnapshotBuffer`/`SnapshotTimeline` (история/интерполяция) не заменяют захват профиля. Новый транспорт
или глобальный менеджер не нужен.

Предлагаемый добавочный transport-neutral контракт в `Neo.Rpg`:

```csharp
public interface IRpgCharacterSnapshotSource
{
    uint StateRevision { get; }
    bool TryCaptureProfile(out RpgCharacterProfileData profile);
    event Action<uint> StateCommitted;
}
```

`RpgCharacter` реализует его поверх существующего состояния. Захват возвращает false во время
незавершённого изменения; revision и событие появляются после завершения внешней операции.
Существующие `CaptureProfile`, `RuntimeRebuilt` и три члена `IRpgCharacterNetworkAdapter` сохраняются.

Обычный C# `RpgNetworkSnapshotPublisher` принимает источник и существующий codec; методы
`MarkDirty(uint revision)` и `TryPublish(double now, bool force, out string snapshot)` позволяют
проверять результат с явным временем. Повторные уведомления одной revision не публикуются дважды.
Компонент передаёт unscaled время, управляет подпиской/отпиской и доставкой. Интервал по умолчанию
равен нулю: сохраняется немедленная публикация завершённого состояния. При положительном интервале
изменения объединяются; initial-state `OnSerialize` принудительно обновляет готовый снимок перед
вызовом base, чтобы новый клиент не получил старое состояние из-за ожидания интервала.
Неготовый источник не очищает pending состояние и не заменяет последний корректный снимок.

### Точные файлы и владельцы следующей реализации

| Файл от корня репозитория | Изменение / владелец |
| --- | --- |
| `Assets/Neoxider/Scripts/Rpg/Components/RpgCharacter.cs` | Сначала C1; затем readiness/commit API — `Neo.Rpg`. |
| `Assets/Neoxider/Scripts/Rpg/Interfaces/IRpgCharacterSnapshotSource.cs` | Новый необязательный контракт без Mirror — `Neo.Rpg`. |
| `Assets/Neoxider/Scripts/RpgNetwork/RpgNetworkSnapshotPublisher.cs` | Новый обычный C# publisher — `Neo.Rpg.Network`. |
| `Assets/Neoxider/Scripts/RpgNetwork/RpgCharacterNetworkAdapter.cs` | Подключение publisher, lifecycle, интервал, initial flush — `Neo.Rpg.Network`. |
| `Assets/Neoxider/Tests/Edit/Network/Audit20261023NeoRpgNetworkSnapshotPublisherTests.cs` | Новый тест результата публикации с контролируемым временем — `Neo.Rpg.Network`. |
| `Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef` | Добавить отсутствующую сейчас ссылку `Neo.Rpg.Network` для новых EditMode-тестов — только координатор. |
| `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoRpgNetworkInitializationTests.cs` | C1 и готовность до сетевого захвата — `Neo.Rpg.Network`. |
| `Assets/Neoxider/Tests/Play/Network/Audit20261023NeoRpgNetworkRegressionTests.cs` | Сохранить текущие девять случаев; добавить initial flush при pending изменении. |
| `Assets/Neoxider/Docs/Rpg/Network.md` | C# API, настройка интервала, lifecycle и UnityEvents — `Neo.Rpg.Network`. |
| `Assets/Neoxider/Docs/Rpg/README.md` | Ссылка на сетевой topic и readiness API — владелец `Neo.Rpg`/координатор. |

Новые package-файлы получают уникальные `.meta`. `RpgAttackNetworkAdapter.cs` для этого улучшения
не требует изменений. Ядро остаётся независимым от реализации `Neo.Network` и Mirror.

## 4. Порядок, совместимость и приёмка

1. Владелец ядра исправляет C1; затем добавляются и централизованно выполняются регрессии C1/F1–F4.
2. Добавляются готовность/commit контракт и обычный publisher; сначала EditMode-проверки результата,
   затем тонкая компонентная интеграция и PlayMode initial/delta сценарии.
3. Обновляются C# примеры и сценовой workflow. UnityEvents продолжают вызывать прежние `Net*`;
   NoCode не становится обязательным, новый editor или ScriptableObject не требуется.
4. Координатор проверяет совместимость API, UTF-8, GUID, scoped diff и пакет. Не переименовывать
   serialized fields, SyncVar, command signature или существующие события. JSON сохраняет текущий
   формат; нулевой интервал не требует миграции prefab/scene. `Samples~`, developer `Samples` и
   импортированные samples не переносить; при проверке использовать активный layout.

Минимум для предложения: неготовый источник ничего не публикует; два изменения внутри интервала
дают один payload с последними значениями; force/late join доставляет последнее состояние сразу;
нулевой интервал сохраняет текущую семантику; denied command не создаёт commit; disable/enable
компонента не удваивает подписки; регенерация действительно уменьшает число сериализаций.
Проверять содержимое профиля и число публикаций, не приватную структуру реализации.

**Стоп-условия:** ошибки компиляции/weaving, необнаруженные тесты, C1, устаревшее initial состояние
или нарушенная совместимость. Нужны compiler-проверки Unity 6.0–6.4 и 6.5+ с Mirror/без него;
версионные проверки идентификаторов остаются в общих `StableId`. До прогонов runtime не проверен.

Оценка предложения: 2–4 дня разработки плюс multiplayer QA при сохранении JSON и готовом C1;
риски — порядок callbacks, вложенные изменения и initial serialization. Versioned envelope,
предел payload и отдельные command acknowledgments остаются P3-предложениями: координатор отдельно
фиксирует формат/политику совместимости перед их проектированием. Они не являются подтверждёнными
дефектами или условием завершения текущих безопасных исправлений.

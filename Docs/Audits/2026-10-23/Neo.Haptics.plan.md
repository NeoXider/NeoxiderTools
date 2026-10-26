# Neo.Haptics — индивидуальный план реализации

План второй фазы по завершённому `Neo.Haptics.md`, актуальному manifest и текущим source/docs/tests.
Предложения не реализованы. Модель по контракту: `gpt-6.1-sol`, reasoning `max`.

## 1. Текущий результат

Просмотрен 1/1 C# файл `Assets/Neoxider/Scripts/Haptics/Haptics.cs`; вложенных сборок нет.
Исправлены HAP-01–04:
чередование типов больше не обходит cooldown; пустые паттерны, нечисловые параметры и
неопределённые enum не вызывают `Played` и не изменяют историю повторов. Сохранены enum,
публичные сигнатуры, default `0.035`, alias custom→`Medium`, pattern→`Heavy` и native mappings.
Повторно реализовывать эти исправления не нужно.

Добавлены `Assets/Neoxider/Tests/Edit/Audit20261023NeoHapticsTests.cs`: 23 случая и уникальная `.meta`.
Обновлён `Assets/Neoxider/Docs/Haptics/Haptics.md`: C# API, наблюдение, fallback, lifecycle, UnityEvent.
Пройдены статические проверки inventory/UTF-8/whitespace/GUID/public API/структуры C#.
Компиляция, тесты, Play Mode и устройства не проверялись. Сейчас меняется только план.

## 2. Оставшиеся исправления и долг проверки

Оставшихся подтверждённых runtime-дефектов нет. Ниже — интеграция и проверка.

| Приоритет / владелец | Основание и минимальное действие | Наблюдаемый результат |
|---------------------|---------------------------------|----------------------|
| P1 — `Neo.Editor.Tests`, координатор | `Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef` по-прежнему не содержит `Neo.Haptics`, необходимую новым typed tests. Добавить эту reference. | Типы разрешаются; fixture `Neo.Editor.Tests.Audit20261023NeoHapticsTests` обнаруживается. |
| P1 — координатор, исправления по владельцам | После завершения source-аудитов выполнить compilation и EditMode fixture; failing case передать владельцу для минимального исправления. | Все 23 случая проходят; нет новых compile/console errors от изменения. |
| P1 — координатор / `Neo.Haptics` | Backend при аудите не установлен; ветки `NEO_MOBILE_HAPTICS`, version define `0.5.0` и absent name-reference не проверены компилятором. Проверить обе конфигурации. | Компиляция с backend/без него; fallback сохранён. Ошибку shared wiring исправлять только по compiler evidence, силами координатора. |
| P2 — координатор / `Neo.Haptics` | Нет проверки паузы, повторного входа без domain reload и реального мотора. Выполнить узкий Play Mode smoke и mobile smoke после зелёного compilation. | См. критерии ниже; `Played` сам по себе не подтверждает физическую вибрацию. |

Неподтверждённые риски: native finite pattern-значения вне диапазона, точность float-clock,
throwing delegates. Это не доказанные дефекты. Политику отрицательного/нечислового
`MinRepeatInterval` и дополнительных clamps не менять незаметно.

## 3. P2: отдельный HapticService с backend и часами — предложение

Проблема: facade смешивает политику, Unity time и dispatch. Тесты не видят native payload,
Stop/Prepare и независимые настройки consumers. `IsSupported` не описывает конкретный тип/waveform.

Переиспользовать `HapticType`, существующие gates/validation, native mapping, `Handheld.Vibrate`,
`SubsystemRegistration` и `Func<double>` для часов. Справочники `modules.md`, `tools.md`,
`extensions.md` проверены: готовый haptics service не найден. Не добавлять зависимости
`Neo.Tools.Time`, `Neo.Settings`, `Neo.Core`; settings подключаются через `Func<bool>`.

Предлагаемый публичный контракт в той же `Neo.Haptics`:

```csharp
public HapticService(IHapticBackend backend, Func<double> unscaledTime);
public HapticPlayResult TryPlay(HapticType type);
public HapticPlayResult TryPlay(float intensity, float sharpness, float durationSeconds);
public HapticPlayResult TryPlayPattern(float[] durationsSeconds, float[] amplitudes);
public void Prepare();
public void Stop();
public void ResetThrottle();
```

Сервис получает `Enabled`, `EnabledProvider`, `MinRepeatInterval`, `Played`.
`HapticPlayResult`: `Invalid`/`Disabled`/`Throttled`/`Accepted` и `Dispatched` — вызов backend,
без гарантии физического эффекта. `IHapticBackend` имеет
capability flags named/shaped/pattern, `CanPlay(HapticType)`, typed Play/PlayPattern и Stop/Prepare.
Проверка конкретного типа нужна: fallback воспроизводит только Heavy/Success/Warning/Error.
`HapticSegment` — readonly данные duration/amplitude. Сервис валидирует и копирует только paired
segments до события, передавая backend представление для чтения: caller mutation не меняет
payload. Clock/интервалы — double; facade сохраняет `Time.unscaledTime` с преобразованием.

Сохранить порядок validation → enablement/throttle → `Played` → dispatch. Silent platform всё
ещё принимает корректный запрос и занимает интервал. Исключения delegates/backend не скрывать
при extraction. Не менять finite range policy паттерна в том же изменении.

### Точные файлы будущей реализации

| Файл | Изменение / владелец |
|------|---------------------|
| `Assets/Neoxider/Scripts/Haptics/Haptics.cs` | Сохранить enum и facade; forwarding в default service, полный subsystem reset. `Neo.Haptics`. |
| `Assets/Neoxider/Scripts/Haptics/HapticService.cs` | Новый plain C# service: проверки, instance policy, cooldown, immutable snapshot. `Neo.Haptics`. |
| `Assets/Neoxider/Scripts/Haptics/HapticBackendContracts.cs` | Новые `IHapticBackend`, `HapticSegment`, capabilities, request status/result. `Neo.Haptics`; отдельная contracts assembly не нужна. |
| `Assets/Neoxider/Scripts/Haptics/HapticBackends.cs` | Internal native/fallback/silent adapters; существующий optional-package gate в одном месте. `Neo.Haptics`. |
| `Assets/Neoxider/Tests/Edit/Audit20261023NeoHapticsServiceTests.cs` | Новый recording backend, fake clock и проверки payload/commands; существующий facade fixture сохранить. `Neo.Haptics`, тестовая интеграция — `Neo.Editor.Tests`. |
| `Assets/Neoxider/Docs/Haptics/Haptics.md` | Дополнить существующий entry point: service API, результат, lifecycle, примеры C# и UnityEvent. `Neo.Haptics`. |

Новым package-файлам нужны уникальные `.meta`; runtime asmdef сохраняется. Test reference —
координатор/`Neo.Editor.Tests`. Существующий `NeoDoc` принадлежит `Neo.PropertyAttribute`, без изменений.
`Neo.Settings` не меняется. Новые MonoBehaviour/editor/NoCode wrapper, SO profile и sample scene
не нужны. Serialized fields/GUID assets сохраняются; `Samples~`/`Samples` не переименовывать.

Оценка: 2–3 рабочих дня плюс device QA. Риск средний: event/reset forwarding, ownership payload
и optional backend. Это отдельная итерация; предложение не блокирует приёмку исправлений аудита.

## 4. Порядок, приёмка и условия остановки

1. Координатор закрывает reference и запускает compilation/23 регрессии после source freeze.
   Доказанные отказы устранять раньше публичного расширения; не переделывать завершённые HAP-01–04.
2. В отдельной итерации добавить contracts/service и recording tests; затем перенести dispatch
   в adapters и подключить facade. Не вводить новый global backend override ради тестов.
3. Проверить совместимость facade, обновить каноническую документацию; component/editor слой
   добавлять только при конкретном authoring use case. Затем package/version/device matrix.

Минимальные наблюдаемые критерии:

- `Selection, Light, Selection` внутри интервала даёт ровно первые два события; empty/invalid
  запрос — ноль событий, следующий корректный Medium/Heavy проходит. Все исходные 23 случая зелёные.
- Recording tests предложения: точные scalar clamps, только paired segments, отсутствие dispatch
  для rejected/unsupported запросов, сохранение snapshot при изменении исходных массивов,
  независимость двух instances. Fake clock: чуть меньше интервала отклоняется, ровно на границе
  принимается; ResetThrottle открывает все типы.
- `Prepare` учитывает Enabled; `Stop` вызывается при уже выключенном Enabled. Accepted-событие
  предшествует dispatch, unsupported named type отражается в результате без ложного dispatch.
- Play Mode: пауза `timeScale=0` не останавливает unscaled cooldown; два входа с отключённым
  domain reload не сохраняют provider/listeners/cooldowns. Проверки должны наблюдать события
  и состояние, не private reflection; настройки и подписки smoke восстанавливаются.
- Координатор компилирует Unity 6.0–6.4 и 6.5+ с backend/без backend; Editor и Android/iOS
  player branches проверяются отдельно, поскольку Editor не доказывает player branch.
  Version-sensitive API при необходимости используют общие compatibility helpers, без новых
  локальных `#if UNITY_...` для версий. Проверить отсутствие optional-package dependency для клиента.
- На Android/iOS проверить named feedback, shaped pulse, waveform и Stop с backend; без него —
  сильные named типы и silent custom/pattern. Чувство эффекта и наличие мотора фиксируются отдельно
  от `Played`. При отсутствии устройства этот пункт остаётся явно непроверенным.

При compile/test failure остановить приёмку конфигурации и передать воспроизведение владельцу.
Непроверенные native/version branches не объявлять рабочими. Coercion interval/pattern ranges,
профили и components — отдельные product-решения; extraction сохраняет контракт. Routine fixes
не требуют нового разрешения. В планировании Unity/build/tests и коммиты не запускаются.

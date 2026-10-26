# Neo.Extensions: план после аудита

Дата: 2026-10-23. База: `ca198556`, пакет 10.19.0. Это план, а не выполненная реализация следующего этапа. В фазе планирования изменён только этот файл.

## Текущее состояние

Прочитаны [основной аудит](Neo.Extensions.md), завершённые дополнительные отчёты [Numeric/Time/Random](Extensions-Numeric-Time-Random.md) и [Coroutines/Unity](Extensions-Coroutines-Unity.md), текущие изменения исходников, новые lifecycle-тесты и границы потребителей.

Основной проход исправил 12 подтверждённых runtime-дефектов: форматирование/парсинг, целочисленные границы, fallback массивов, градиент, подписки, одноразовые коллекции и координаты. Дополнительные проходы уже исправили N01–N15: малые double, направленное округление, decimal/BigInteger, веса, числовые/time/date границы; XCU-01–07: startup/disposal/ownership корутин, viewport/depth, preview Sprite, smooth-look и HDR HEX. Эти исправления **не являются будущими задачами**. XCU-08 оставлен документированным ограничением raw native stop.

Написаны 135 случаев: 32 основных, 82 числовых, 13 синхронных Unity и 8 runtime-интеграционных. Выполнены только статические проверки: scoped diff/UTF-8/типы/ссылки/GUID. Ни компиляция, ни эти тесты, ни профилирование владельцами не запускались. Старый список рисков основного отчёта нужно читать вместе с завершёнными дополнениями.

## Приоритеты и оставшийся долг

| Приоритет / владелец | Доказательство и минимальное действие | Наблюдаемая готовность |
| --- | --- | --- |
| **P1: интеграционная проверка**, координатор + Neo.Extensions | Большая часть нового control flow находится в `Assets/Neoxider/Scripts/Extensions/CoroutineExtensions.cs`; native scheduling ещё не проверен. После фиксации общего source batch скомпилировать обе версии Unity и выполнить шесть новых extension-fixture вместе с тремя существующими. Добавить точечные сценарии reset-reentry, cleanup exception, raw stop → `handle.Stop()`, scaled/unscaled pause и фаз `WaitUntil`/fixed/end-of-frame. | Immediate completion не воскресает; cleanup идёт child→parent ровно один раз; исключение освобождает владельца; отключение иерархии отменяет, отключение только компонента сохраняет выполнение. Повторный вход без domain reload не оставляет дублирующий global host. Результаты подтверждаются XML/logs, не чтением исходника. |
| **P1: остаток копии расчётов**, Neo.Bonus после Neo.Extensions | В текущем `Assets/Neoxider/Scripts/Bonus/TimeReward/TimeReward.cs` accumulated-count **уже делегирован**, но приватный `AdvanceLastClaimTime` ещё использует `claimsGiven * cooldownSeconds` во float и `AddSeconds`. При 16777217 секундных claims теряется секунда. Делегировать существующему `lastClaimUtc.AdvanceLastClaimTime(...)`, сохранив публичный reward workflow. | Consumer-регрессия проверяет сохранённое время, дробный cooldown и верхнюю дату через публичный claim path. Формулы не расходятся с extension. Не повторять уже выполненную правку count. |
| **P2: владение preview у потребителя**, Neo.Tools.Inventory + Neo.Extensions | `Assets/Neoxider/Scripts/Tools/Inventory/Data/InventoryItemData.cs:OnValidate` присваивает результат `GetPreviewSprite()` в serialized `_icon`. Теперь generated Sprite уничтожается при очистке кэша; `InventoryItemView.cs:Bind` отдельно получает fallback. Это подтверждённое пересечение контрактов, последствия save/reload ещё не воспроизведены. Не записывать generated thumbnail в persistent data; сохранять authored/borrowed asset, UI fallback получать заново при Bind. | Исходный `_icon` переживает save/reload, cache clear не уничтожает authored asset; UI после повторного Bind показывает новый preview. Проверить Edit/Play и domain reload off. Не возвращать утечку ради сохранения временной ссылки. |
| **P2: независимость pointer API**, Neo.Extensions → Neo.Tools.Input | `UIUtils.cs` напрямую читает `Input.mousePosition`; New Input System-only не воспроизводился. Переиспользовать существующий `MouseInputCompat.TryGetPosition` через верхний слой: добавить `int UIUtils.RaycastUI(Vector2 screenPosition, List<RaycastResult> results, EventSystem eventSystem = null)` с очищением caller-buffer; тонкий адаптер в `Assets/Neoxider/Scripts/Tools/Input/MouseInputCompat.cs` передаёт позицию. Input уже зависит от Extensions; обратная ссылка создаст цикл. | Координатный API работает без legacy Input, отсутствие EventSystem даёт 0 и пустой buffer; input-адаптер проверен при Both и New-only в изолированном validation-проекте. Старые сигнатуры сохраняются; отсутствие аллокаций не обещается без измерения. |
| **P2: документация**, Neo.Extensions | В `Assets/Neoxider/Docs/Extensions/README.md` ещё нет ссылок на `NumericEdgeCases.md` и `LifecycleAndOwnership.md`. Добавить их, актуализировать `CoroutineExtensions.md`, `PrefabPreviewExtensions.md`, `ScreenExtensions.md`, RU/EN примеры и migration notes. | Из entry point доступны rounding/saturation, ticks, raw-stop boundary, generated ownership и orthographic near-relative depth. Владелец Neo.Tools.Move проверяет `ScreenPositioner.cs` и `_depth`; значения сцен автоматически не переписываются. |

Raw `owner.StopCoroutine`/`StopAllCoroutines` не имеют надёжного уведомления пакету. Это ограничение наблюдаемости, не обещание будущего автоматического исправления. Поддерживаемая отмена — `handle.Stop()`; проверка должна отличать её от native escape hatch.

## Существенное улучшение: versioned string-array codec

**Проблема.** Текущий `PlayerPrefsUtils.SetStringArray` пишет CSV, запрещает запятые, теряет различие null/empty; singleton empty не восстанавливается. JSON detection пересекается с обычным строковым содержимым. Уже потерянные legacy данные восстановить нельзя.

**Дизайн, только предложение.** Переиспользовать `PlayerPrefsUtils`, его исторический JSON wrapper и существующий manual `PlayerPrefs.Save` контракт. В той же сборке добавить чистый `Assets/Neoxider/Scripts/Extensions/StringArrayCodec.cs` и `StringArrayStorageFormat.cs`:

```csharp
string StringArrayCodec.Encode(IReadOnlyList<string> values);
bool StringArrayCodec.TryDecode(string raw, out string[] values,
    int maxElements = 4096, int maxChars = 1048576);
void PlayerPrefsUtils.SetStringArrayV2(string key, IReadOnlyList<string> values);
bool PlayerPrefsUtils.TryGetStringArray(string key, StringArrayStorageFormat format,
    out string[] values);
```

Форматы — `LegacyCsv`, `LegacyJson`, `V2`, выбираемые явно. V2: версия, count, длины UTF-16 code units и отдельный null marker; числа invariant, overflow/размеры проверяются **до** выделения массива. Пустой массив — валидный payload, null collection — ошибка аргумента, null element поддерживается. Неверная версия/обрыв/лимит дают false без частичного результата. Кодек не зависит от Unity; исторический JSON разбирается существующим адаптером.

**Совместимость и шаги.** Старые Set/Get остаются побайтно совместимыми и не начинают угадывать V2. Сначала спецификация/envelope fixtures, затем pure codec, затем additive PlayerPrefs adapters. Миграция явно выбирает исходный формат и пишет в новый ключ после успешного decode; legacy ключ не удаляется автоматически. Один magic-prefix не доказывает формат произвольного старого CSV. Автомиграция, массовая перезапись сохранений и изменение существующих serialized fields не входят в работу.

Добавить `Assets/Neoxider/Tests/Edit/Extensions/StringArrayCodecTests.cs`, `PlayerPrefsStringArrayV2Tests.cs` и уникальные meta: round-trip Unicode/emoji/quotes/newlines/commas, null/empty/singleton/empty-array; malformed lengths/counts, unknown version, лимиты, legacy fixtures и неизменность старого writer. Обновить существующий `PlayerPrefsUtils.md` и README RU/EN: C# миграция, scene method вызывает тот же API, Save остаётся явным. Оценка 2–3 дня, средний риск формата; утвердить лимиты как публичный контракт до реализации, не требуя отдельного разрешения для обычных правок.

## Следующие независимые предложения

После стабилизации текущего поведения использовать планы дополнительных владельцев: `WeightedDistribution.cs` с `TryCreate`/`SelectIndex(double unitSample)` даёт immutable snapshot и точные boundary-тесты без global RNG; `CoroutineScope.cs`/`CoroutineOperation.cs` с injected runner, `Run`/`CancelAll`, terminal state/error и только затем опциональный `CoroutineScopeBehaviour.cs` дают явное владение. Повторно не строить scheduler или RNG manager. Эти additive API не являются условием выпуска текущих исправлений; native wait phases, seeded choices и стоимость tracking измеряются отдельно.

## Порядок и условия завершения

1. Зафиксировать supplemental batch, закрыть Bonus/Inventory integration и получить регрессионную проверку текущих исправлений; при ошибке — минимальная правка и повтор затронутого набора.
2. Отдельным этапом реализовать codec и pointer overload; затем необязательные scope/distribution API. Добавлять компоненты/editor wrappers только после чистых контрактов.
3. Согласовать документацию, migration notes и package validation; новых dependencies/asmdef cycles не добавлять. `NeoDiagnostics.StableId` и editor `NeoComponentHealth.StableId` остаются единственными ID gateways.
4. Финальная приёмка: Unity 6.0–6.4 и 6.5+ действительно компилируют свои ветки; EditMode и runtime-сценарии проходят, skips объяснены. Проверить domain reload on/off, deferred preview destruction и headless thumbnail. Для rounding наблюдать `-1251` ceiling/floor, `12501` sticky digit, `decimal.MaxValue`, `double.Epsilon`, разные separators/cultures; для RNG — обе большие весовые корзины и исключение нулевых.

Sample layout остаётся release `Assets/Neoxider/Samples~`; validation учитывает также developer `Samples` и imported `Assets/Samples/NeoxiderTools/10.19.0/<sample>`. План не требует перемещения samples. Завершение фиксируется только по фактическим receipts; непроверенные платформы, allocation cost и продуктовые пределы codec остаются явно отмеченными, а предложения не выдаются за реализованные изменения.

# Extensions-Numeric-Time-Random: индивидуальный план реализации

Дата: 2026-10-23. Продолжение исходной аудиторской сессии; профиль задания: `gpt-6.1-sol`, `max`. Фаза 2 меняет только этот план. Основание: полный PLAN-CONTRACT.md, собственный завершённый аудит, supplemental-запись manifest и текущие исходники, документация, регрессии и зависимости.

## 1. Уже выполнено

Проверены восемь закреплённых helpers. N01–N15 исправлены в `Assets/Neoxider/Scripts/Extensions/NumberFormatExtensions.cs`, `PrimitiveExtensions.cs`, `RandomExtensions.cs`, `TimeSpanExtensions.cs`, `CooldownRewardExtensions.cs`: знак и точность округления, малые double, границы decimal, хвост BigInteger, форматирование, веса, вероятность, случайный диапазон, длительности, нормализация, snapping и продвижение UTC-даты. Повторная реализация этих исправлений не нужна. Остальные три исходника оставлены без изменений.

Добавлены `Assets/Neoxider/Docs/Extensions/NumericEdgeCases.md` и три файла в `Assets/Neoxider/Tests/Edit/Extensions/`: `Audit20261023NumericFormatTests.cs`, `Audit20261023NumericTimeTests.cs`, `Audit20261023NumericRandomAndPrimitiveTests.cs` — 28 методов, 82 сценария. Ранее прошли статические проверки UTF-8, whitespace, GUID и сохранности публичных сигнатур. Unity-компиляция и выполнение тестов не проводились; исправленный код ещё не равнозначен проверенному runtime-поведению.

## 2. Оставшиеся работы по приоритету

**P1, подтверждённый дефект другого владельца — Neo.Bonus.** В текущем `Assets/Neoxider/Scripts/Bonus/TimeReward/TimeReward.cs:277` приватный `AdvanceLastClaimTime` всё ещё вызывает `AddSeconds(claimsGiven * cooldownSeconds)`. Потеря точности воспроизводится арифметически при 16777217 односекундных claims; дробное продвижение зависит от профиля AddSeconds. Вызов находится в `TakeReward` перед сохранением timestamp. Минимальное исправление владельцем Bonus: делегировать `lastClaimUtc.AdvanceLastClaimTime(claimsGiven, cooldownSeconds)` существующему Extensions API. Ссылка Neo.Bonus → Neo.Extensions уже есть, asmdef менять не требуется.

Важное уточнение к историческому аудиту: текущий `GetAccumulatedClaimCount` этого потребителя **уже делегирует** Extensions; его старые ошибки больше не являются оставшейся работой. Перед изменением advancement владелец повторно проверяет актуальный код: аудит других сборок продолжается.

Регрессия потребителя: новый `Assets/Neoxider/Tests/Edit/Bonus/Audit20261023NeoBonusTimeRewardCooldownTests.cs`, владелец Neo.Bonus совместно с владельцем Edit-тестов. Через публичный `TakeReward`, существующие fixtures времени/сохранения и небольшое ограниченное число claims проверить сохранённый UTC timestamp, остаток накопления и количество событий. Не вызывать миллионы UnityEvents ради проверки большого счётчика: точность большого count уже покрывает Extensions-регрессия. Приёмка: сохранённое продвижение совпадает с typed API, без изменения save keys и публичных событий.

**P1, долг проверки — координатор.** Запустить три новые fixtures и существующие `ExtensionsTests`/`Audit20261023ExtensionsTests` после завершения писателей. Наблюдаемые результаты: ceiling/floor для −1251 дают −1.2K/−1.3K; 12501 и 12500 различаются при ToEven; scientific double.Epsilon даёт `4.94e-324`; оба предела TimeSpan форматируются; два float.MaxValue-веса выбирают оба ненулевых bucket; UTC advancement сохраняет 16777217 секунд и насыщается без исключения. Особые runtime-риски: E16 на Mono, extreme int Random.Range и восстановление RNG/diagnostics между тестами. Это пока риски, а не дополнительные подтверждённые дефекты.

**P2, документация — основной владелец Neo.Extensions.** В `Assets/Neoxider/Docs/Extensions/README.md` пока отсутствует ссылка на NumericEdgeCases.md. Добавить её в Numbers and time; новый конкурирующий entry point не создавать.

**P2, интеграционная проверка — Neo.Tools.Text и Neo.Bonus.** `Assets/Neoxider/Scripts/Tools/Text/TimeToText.cs` использует float FormatTime в Clock и TimeSpan в Compact; Set уже содержит защитный контракт для недопустимых значений. Проверить отображение обычного времени, отрицательного режима и больших допустимых длительностей. Различие legacy centiseconds и настоящих milliseconds документировано; унифицировать форматы под видом исправления нельзя. Неподтверждённого дефекта потребителя не объявлять подтверждённым.

## 3. Существенное улучшение: immutable weighted snapshot

**P2, предложение, ещё не реализовано; оценка 2–3 дня.** Повторные weighted rolls сейчас обходят список; selector-overload создаёт массив. Цель — снимок стабильных весов с O(log n) выбором без выделений после создания, пригодный для обычного C# и явного sample.

**Reuse:** сохранить `RandomExtensions.GetRandomWeightedIndex`/`GetRandomWeighted` для разовых вызовов. Текущий `Neo.Tools.ChanceManager` уже имеет `GetId(float)`, `RandomProvider` и cached double total — повторно создавать эти возможности не требуется. Использовать ChanceManager для изменяемых Inspector-таблиц. Он остаётся mutable, нормализует/санитизирует веса и выбирает линейно; snapshot нужен только для отдельного сценария стабильных многократных выборов. Направление зависимости сохраняется: Neo.Tools.Random → Neo.Extensions; обратная ссылка создала бы цикл.

Предлагаемый минимальный C# API в `Neo.Extensions`:

```csharp
public sealed class WeightedDistribution
{
    public int Count { get; }
    public double TotalWeight { get; }
    public static bool TryCreate(IReadOnlyList<float> weights, out WeightedDistribution distribution);
    public int SelectIndex(double unitSample);
}
```

Count означает исходное число позиций. Конструктор скрыт; TryCreate копирует веса в закрытые cumulative-double/original-index массивы. Null, пустой список, отрицательные/nonfinite веса и нулевая сумма дают false/null. SelectIndex принимает конечный sample [0,1], иначе бросает ArgumentOutOfRangeException; 0 выбирает первый положительный bucket, 1 — последний, внутренние границы принадлежат следующему bucket. Бинарный поиск использует строгое превышение cumulative boundary. Исходный список после создания не влияет на snapshot.

Точные будущие файлы:

- `Assets/Neoxider/Scripts/Extensions/WeightedDistribution.cs` + уникальная `.meta`, сборка Neo.Extensions; новый файл назначает координатор.
- `Assets/Neoxider/Scripts/Extensions/RandomExtensions.cs`: только additive `SampleWeightedIndex(this WeightedDistribution distribution)`, один Unity RNG draw для валидного snapshot; null → -1 без draw. Отдельное имя исключает неоднозначность существующего `GetRandomWeightedIndex(null)`.
- `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023NumericDistributionTests.cs` + `.meta`, сборка Neo.Editor.Tests.
- `Assets/Neoxider/Docs/Extensions/NumericEdgeCases.md`; ссылка из README — через основного владельца.

Минимальные тесты: invalid construction/samples; веса `{0,1,0,3,0}` при samples 0, 0.25 и 1; два float.MaxValue; неизменность результата после mutation исходного списка; отсутствие изменения Unity RNG при SelectIndex; adapter потребляет один draw. Профилирование повторного выбора на 10 000 позициях подтверждает отсутствие GC allocations и повторной валидации; не нужен отдельный benchmark framework.

Существующие методы, serialized fields, save data и seeded-последовательности старого API не менять. Для изменённых весов snapshot перестраивается явно. C# документация показывает построение/выбор, scene/NoCode — вызов из существующего компонента; новый MonoBehaviour, editor window или ScriptableObject не требуется. Риски: cumulative precision при резко разных весах, изменение ожиданий на границах и устаревший snapshot. Детерминированный sample не обещает переносимость Unity RNG между версиями. Если сценарий не требует immutable/быстрого повторного выбора, достаточно существующего ChanceManager.

## 4. Порядок и критерии завершения

1. Владелец Bonus устраняет оставшееся дублирование advancement и проверяет сохранённое публичное поведение; затем координатор закрывает долг 82 регрессий. Исправлять только воспроизведённые отказы.
2. После стабилизации исправлений реализовать opt-in snapshot, его контрактные тесты и небольшой adapter. Интеграция ChanceManager — отдельная необязательная задача владельца Neo.Tools.Random; зависимости вверх не добавлять.
3. Обновить typed/scene документацию, проверить GUID/UTF-8/API и существующие потребители. Продуктовые решения: нужен ли snapshot сейчас, требуется ли когда-либо отдельный full-range duration API или политика sub-tick cooldown. Текущие насыщение и legacy units сохраняются.
4. Координатор компилирует релевантные сборки на Unity 6.0–6.4 и отдельно 6.5+, проверяя реальные ветки общих StableId-gate. Новых version-specific вызовов нет; успешная одна ветка не доказывает другую. EditMode достаточен для чистых helpers; короткий PlayMode smoke нужен владельцам reward/text потребителей, без новой sample-сцены.
5. Release-проверка: отсутствие циклов/новых обязательных пакетов, импорт нового runtime-файла и metadata, доступность документации. Текущий `Samples~` не переименовывать; при использовании sample-проверок учитывать также developer/imported roots и версию 10.19.0.

Готовность означает наблюдаемые результаты контрактных тестов, пройденную доступную матрицу компиляции и закрытый consumer-долг. Недоступная версия Unity, непрогнанные тесты или ошибки чужой сборки остаются явно непроверенными с владельцем; не обходить их изменением shared configuration. Предложение snapshot можно оставить запланированным отдельно от завершения bugfix-интеграции.

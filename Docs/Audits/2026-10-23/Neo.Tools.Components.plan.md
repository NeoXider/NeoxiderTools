# Neo.Tools.Components: индивидуальный план реализации

Фаза 2. Основание: аудит `Neo.Tools.Components.md`, manifest и актуальные исходники.
Предложения не реализованы; Unity-проверки не выполнялись.

Префиксы точных путей: `C = Assets/Neoxider/Scripts/Tools/Components`,
`E = Assets/Neoxider/Tests/Edit/Tools`, `P = Assets/Neoxider/Tests/Play`,
`D = Assets/Neoxider/Docs/Tools/Components`.

## Выполнено и сохранено

Просмотрены 13/13 C# файлов; изменены восемь исходников и десять публичных руководств. Уже исправлены
арифметика/регистрация Counter, повторные host-события и начальные сетевые снимки, throttling по
отправителю, согласованность ScoreManager, отмена/повторный вход typewriter, запуск/очистка wrapper,
подписки TextScore, обратные float-границы, камера/RenderTexture и автоматический loot в Edit Mode.
Повторно реализовывать эти исправления не требуется.

Написаны 32 регрессии: 28 Edit и четыре Play, включая три условные Mirror. Выполнены только
статические проверки: UTF-8, маркеры повреждения текста, ссылки/GUID, сохранение 165 строк публичных
деклараций и 63 serialized-полей, `git diff --check`. Компиляция, Unity console, запуск тестов,
изображение камеры и обмен независимых клиентов остаются непроверенными.

## Приоритетные исправления и долг проверки

### P0 — интеграционная проверка; владелец: координатор

После завершения параллельных изменений выполнить compile/import и следующие существующие fixtures:
`E/Audit20261023NeoToolsComponentsNumericTests.cs`,
`E/Audit20261023NeoToolsComponentsSceneTests.cs`, `E/Audit20261023NeoToolsComponentsTypewriterTests.cs`,
`P/Audit20261023NeoToolsComponentsBindingTests.cs`,
`P/Network/Audit20261023NeoToolsComponentsNetworkTests.cs`.
Зависимость: `Assets/Neoxider/Scripts/Network/Core/NeoNetworkComponent.cs` предоставляет использованный
`RateLimitCheck(sender)`; владелец Neo.Network проверяет его совместимость и независимость соединений.
Координатор проверяет `MIRROR`/references в тестовых asmdef. Snapshot round trip не заменяет
подключение отдельного клиента. Ошибки воспроизводить адресно, затем повторять затронутые проверки.

### P1 — lifecycle TextScore; подтверждено исходниками, runtime-воспроизведение ожидается

В `C/TextScore.cs:36,44` собственные callbacks скрывают private `OnEnable/OnDisable` из
`Assets/Neoxider/Scripts/Tools/Text/SetText.cs:29,37`. Подписки очищаются, но базовый pause/resume
анимации не компонуется. Сначала владелец **Neo.Tools.Text** делает эти hooks `protected virtual`;
затем владелец **Neo.Tools.Components** переводит callbacks TextScore в override с base-вызовами.
OnDestroy/SetLink уже обеспечивает базовую очистку.
Добавить `P/Audit20261023NeoToolsComponentsLifecycleTests.cs`: выключить компонент посреди числового
tween, проверить неизменность `currentNum` во время отключения и достижение нового актуального score
после включения. Serialized-настройки анимации и legacy Best-флаг сохранить. Оценка: 0,5–1 день.

### P1 — повторный вход Counter: дополнительный подтверждённый поток кода, ещё не исправлен

`C/Counter.cs:402–408` публикует аргумент после `ApplyValueLocally`; `CmdSetValue:442–444` также
отправляет RPC после пользовательских callbacks. Подписчик `Value`, вызывающий `Set(2)` при получении
единицы, завершает внутреннее изменение, после чего внешний вызов может опубликовать устаревшую
единицу. Это вывод из текущего порядка выполнения, не выполненный сетевой тест.
Минимальное исправление владельца Components: ревизия изменения и отсечение публикации устаревшего
вызова после callbacks в обоих путях; не менять порядок всех событий побочным рефакторингом.
Дополнить Numeric/Network fixtures: после внешнего `Set(1)` сервер, получатель RPC и новый initial
snapshot показывают 2. Отдельно зафиксировать контракт typed-событий перед расширением их порядка.
Оценка: 0,5–1 день плюс сетевая проверка.

### P1 — потеря конкурентных операций Counter; подтверждённая логическая репродукция

`ApplyDelta/ApplyFactor → SetValue → CmdSetValue` вычисляет абсолютный результат на клиенте.
Два клиента с десятью отправляют одиннадцать после `Add(1)`, и сервер получает одиннадцать вместо
двенадцати. Владелец Components исправляет `C/Counter.cs`: передавать операцию/операнд, вычислять
результат на сервере, переиспользовать `CounterModifyOperation` из `C/ModifyCounterByKey.cs` и
sender-aware limiter. Публичные overloads, округление и zero-guards сохранить; абсолютный `Set`
оставить last-writer-wins. Neo.Network согласует транспортный контракт и настоящую multi-client
проверку; новый файл: `P/Network/Audit20261023NeoToolsComponentsConcurrentCounterTests.cs`.
Приёмка: два независимых клиента выполняют по одному Add в одном интервале и сходятся к 12;
host-события остаются однократными. Оценка: 1–2 дня плюс инфраструктура независимых клиентов.
Авторизация shared-счётчиков — отдельная политика, а не автоматически доказанная уязвимость.

## P2 — существенное улучшение: детерминированная модель typewriter, предложение

**Проблема:** `C/TypewriterEffect.cs` связывает UniTask, Unity audio, время, scanner и callbacks;
scanner считает UTF-16 units и любую последовательность `<...>` разметкой. Тестирование задержек
зависит от player loop. Нужна самостоятельная C# модель, доступная без scene wiring.

**Файлы:** новые `C/TypewriterSession.cs`, `TypewriterFrame.cs`, `TypewriterSettings.cs`,
`ITypewriterTokenizer.cs`, `LegacyTypewriterTokenizer.cs`, `GraphemeTypewriterTokenizer.cs`;
изменяемые `C/TypewriterEffect.cs`, `C/TypewriterEffectComponent.cs`.
Все новые типы остаются в Neo.Tools.Components; новый asmdef не требуется.

**Публичный API:** `TypewriterSession.Start(string, TypewriterSettings, ITypewriterTokenizer)`,
`Advance(float deltaSeconds)`, `Complete() : TypewriterFrame`, `Stop()`, `Reset()`,
`Current : TypewriterFrame`, `Changed : Action<TypewriterFrame>`.
Frame содержит текст, видимое/общее количество элементов, progress и состояние. Settings снимает
конфигурационный snapshot; модель не хранит AudioSource, scene references или CancellationTokenSource.

Переиспользовать текущие punctuation settings и generation/re-entry семантику. Извлечь существующий
scanner в LegacyTokenizer без второй параллельной реализации. Проверить пригодность
`System.Globalization.StringInfo` для поддерживаемого Unity runtime прежде, чем обещать grapheme
сегментацию; полный TMP grammar/ZWJ отдельно не обещать. Существующий UniTask становится драйвером
модели, AudioSource и TMP остаются адаптерами; `Neo.Reactive` сохраняет wrapper progress.

Сначала реализовать модель и `E/Audit20261023NeoToolsComponentsSessionTests.cs`, затем адаптеры.
Минимальные проверки: одинаковый результат при 30/60/120 Hz, punctuation/catch-up, Stop/restart из
callback, forced/natural completion, surrogate/combining character, malformed/вложенная разметка.
Существующие 11 typewriter-регрессий должны сохраниться без ослабления assertions.

**Совместимость:** legacy tokenizer по умолчанию; новые режимы opt-in. Сохранить constructors,
`PlayAsync/Stop/Complete/Reset`, UnityEvents, serialized-поля, callback `char` и первоначальный момент
показа символа. Новый frame-контракт для empty/tag-only текста не должен молча менять старые события.
Владелец **Neo.Tools.Dialogue** проверяет
`Assets/Neoxider/Scripts/Tools/Dialogue/DialogueController.cs`: sentence advance, skip и forwarded
characters. Обновить `D/TypewriterEffect.md` и `D/README.md` примерами C# и сценовой настройки.
Оценка: 3–5 дней; основной риск — timing/callback compatibility, затем Unicode/tokenizer scope.

## P3 — числовая точность и продуктовые ограничения, отдельное предложение

Float-backed Counter/RandomRange не обеспечивают точные последовательные целые за 2^24;
`RandomRange.RollRandomValue` дополнительно использует `maxI + 1`. Поведение предельного преобразования
Int32 ещё не проверено в Unity. Не объявлять этот риск исправленным документацией.
Следующий независимый этап: новые `C/IntCounterState.cs`, `FloatCounterState.cs`, `IntRange.cs` и
`E/Audit20261023NeoToolsComponentsExactNumericTests.cs`; typed `TryApply(operation, operand, out value)`
с явными overflow/nonfinite/division policies. Сохранить legacy backend/save keys; точные значения
сохранять новым tagged-форматом совместно с **Neo.Save**. Проверять 2^24+1, оба Int32 endpoint,
нулевой делитель и старые сохранения. Политику округления/доступа выбрать до включения opt-in backend;
default не менять. Оценка: 4–7 дней вместе с сетью/миграцией, риск высокий при замене legacy.
Loot во время scene teardown остаётся документированной политикой; выбор явного death-event вместо
OnDestroy не оформлять как безусловное изменение всех игр.

## Порядок, приёмка и условия остановки

1. Зафиксировать итоговую общую ревизию; закрыть P0. Согласовать Text hooks, затем узкие P1 и их
   регрессии. Только после этого вводить новый публичный API P2; P3 выпускать отдельно.
2. Проверить orthographic/perspective RenderTexture реально отрисованным кадром. Координатор
   синхронизирует `Assets/Neoxider/Docs/Tools/CameraAspectRatioScaler.md`, находящийся вне моего docs-root.
3. Выполнить Edit/Play smoke, Dialogue integration и настоящий remote-client сценарий; проверить
   Unity 6.0–6.4 и 6.5+, MIRROR/fallback. Version gates оставлять в NeoDiagnostics/NeoComponentHealth,
   не размножать `#if` около вызовов. Неисполненная ветка остаётся непроверенной.
4. Проверить UTF-8, GUID новых файлов, package references и документацию. Не переименовывать Samples~;
   если позже нужны samples, учитывать также developer Samples и импортированный versioned root.

Закрытие требует наблюдаемых результатов выше, отсутствия новых ошибок и сохранения сериализации.
Не принимать host-only snapshot за remote transport, статический анализ за тест или пропуск fixture
за успех. Остановить конкретное API-расширение при невыбранной политике/несогласованной зависимости;
независимые безопасные исправления продолжать. В этой фазе записан только план; код, тесты,
публичные docs, аудит и shared configuration не изменялись.

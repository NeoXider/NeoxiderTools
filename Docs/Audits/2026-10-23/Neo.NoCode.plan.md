# Neo.NoCode: индивидуальный план реализации, фаза 2

Модель: `gpt-6.1-sol`, reasoning `max`. Основание: завершённый [аудит](./Neo.NoCode.md), текущие исходники, документация, регрессии и manifest. В этой фазе создан только этот план; реализация ниже ещё не выполнена.

Обозначения путей: `N = Assets/Neoxider/Scripts/NoCode/`, `D = Assets/Neoxider/Docs/NoCode/`, `E = Assets/Neoxider/Tests/Edit/NoCode/`. Все runtime-изменения относятся к `Neo.NoCode`; чужие сборки указаны отдельно.

## Уже выполнено

Проверены все 7 файлов inventory. В `N/ComponentFloatBinding.cs`, `N/NoCodeFloatBindingBehaviour.cs`, `N/NoCodeBindText.cs`, `N/NoCodeFormattedText.cs` исправлены NOCODE-01–06: смена reactive-подписок, отключённые views и уведомления в процессе доставки, восстановление/перенастройка текстовых целей, смена host, очистка уничтоженного SourceRoot, отклонение перегруженных индексаторов. Повторно реализовывать эти исправления не требуется.

Обновлены `D/README.md`, `D/ComponentFloatBinding.md`, `D/NoCodeFloatBindingBehaviour.md`, `D/GettingStarted.md`. Добавлен `E/Audit20261023NeoNoCodeBindingTests.cs`: 20 случаев с проверками текста, событий и числа слушателей. Пройдены только статические проверки: scoped diff-check, UTF-8, ссылки, GUID, скобки, сохранение 28 serialized-полей и существующих деклараций API. Компиляция, EditMode, PlayMode и player-проверки не выполнялись.

## P1: оставшиеся исправления

Два уточнения выявлены при чтении текущего кода для плана. Они подтверждены разбором потока управления, но воспроизведение в Unity пока не запускалось. Владелец обоих исправлений — `Neo.NoCode`.

**F1. Inspector-валидация останавливает Reactive fallback.** В `N/NoCodeFloatBindingBehaviour.cs:41–45` и `N/NoCodeFormattedText.cs:70–75` `OnValidate` сбрасывает `_useReactivePollFallback`; `LateUpdate` затем отбрасывает Reactive-обновление при false. Воспроизведение: обычное float-поле уже обновляется через Reactive fallback; во время Play изменить Poll Interval в Inspector; затем изменить исходное поле. Вывод перестаёт обновляться до внешнего refresh/re-enable.

Минимальная коррекция: не обнулять рабочий fallback без последующей синхронизации. Отмечать конфигурацию неserialized-флагом dirty; обрабатывать его на основном потоке перед решением `shouldPoll`, пересчитывая подписку/fallback. В `OnValidate` не выполнять поиск компонентов или чтение Unity-источника. Сохранить Once, запрет interval polling через `_pollInLateUpdate` и отключение лишнего polling после успешной reactive-синхронизации. Приёмка: после валидации следующее разрешённое polling-обновление показывает новое значение; Once самопроизвольно не перечитывается.

**F2. Перенастройка из UI callback теряет polling.** В `N/NoCodeFloatBindingBehaviour.cs:181–201` после `ApplyFloat` используется `reactive |= TrySubscribeReactive()`. Если первый результат true, а callback переключает binding на обычное поле, финальная подписка уже снята, но накопленный флаг остаётся true; fallback не включается. Воспроизведение: `SetProgress` читает reactive float; `Slider.onValueChanged` переключает `Binding.MemberName` на обычный float; последующие изменения нового поля не доходят до Slider.

Минимальная коррекция: после `ApplyFloat` использовать актуальный результат синхронизации вместо накопления прежнего состояния. Приёмка: старый источник имеет 0 слушателей, очередной poll отображает новое поле, например normalizedValue меняется с 0.25 на 0.75.

Регрессии F1 для обоих wrappers и F2 добавить в новый `E/Audit20261023NeoNoCodeFollowupTests.cs` с уникальным `.meta`; проверять выход UI после следующего poll, а не приватный fallback-флаг. Сначала зафиксировать воспроизведение, затем минимальный патч; не заменять этим изменения NOCODE-01–06.

## P0: долг проверки перед интеграцией

Координатор после стабилизации параллельных аудитов компилирует dependency closure и запускает новый fixture вместе с `NoCodeBindEditModeTests`, `NoCodeBindLateSpawnTests`, `NoCodeBindTextLazyResolveTests` из `E`. Особенно нужны смешанные reactive/plain значения и позднее появление источника: их обработка менялась.

PlayMode-сценарий должен проверять настоящий `LateUpdate`, отключение/re-enable, деактивацию родителя, unscaled polling при `Time.timeScale = 0` и отключение view другим слушателем. Предлагаемый файл: `Assets/Neoxider/Tests/Play/Audit20261023NeoNoCodeLifecycleTests.cs`. Сейчас `Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef` не ссылается на `Neo.NoCode`: сначала координатор добавляет ссылку, затем появляется fixture. Это зависимость интеграции, не разрешение менять shared config в данной фазе.

## P2: typed source и единый владелец binding-сессии

**Проблема:** два wrappers дублируют polling/подписки; у single-source view только protected runtime refresh, у formatted view отсутствует публичная настройка источников. Строковые reflection-ссылки неудобны для C# и требуют внимания при stripping.

**Reuse:** текущий `Neo.Reactive` уже предоставляет `IReadOnlyReactiveProperty<T>` и `Subscribe(..., invokeImmediately: false) → IDisposable` в `Scripts/Reactive/ReactiveSubscription.cs` и `ReactiveProperty.cs`. Использовать их через адаптеры; отдельный reactive framework из предложения аудита не нужен. `ComponentFloatBinding`, `ReflectionCache`, `BindingSourceGameObjectResolver`, `SetText`, `TimeToText` остаются существующими механизмами.

Предлагаемый публичный контракт в `Neo.NoCode`, без новой assembly:

```csharp
public interface IFloatValueSource
{
    bool TryRead(out float value);
    bool TrySubscribe(Action<float> onChanged, out IDisposable subscription);
}
```

False у `TrySubscribe` означает polling-only источник и null handle. Обычная C# `FloatBindingSession` предоставляет `Bind(IFloatValueSource, Action<float>, NoCodeFloatUpdateMode, float pollIntervalSeconds)`, `Refresh()`, `Tick(float unscaledTime)`, `Suspend()`, `Resume()`, идемпотентный `Dispose()`. Сессия владеет handle, но не уничтожает общий источник. Rebind сначала освобождает прежнюю подписку; suspended refresh читает один раз без подписки; смена binding внутри callback учитывается по актуальному поколению сессии.

Точные новые файлы в `N`: `IFloatValueSource.cs`, `FloatBindingSession.cs`, `ReactiveFloatValueSource.cs`, `ComponentFloatValueSource.cs`, каждый со своим `.meta`. Reactive-адаптеры принимают существующий `IReadOnlyReactiveProperty<float/int/bool>`; component-адаптер сохраняет host и текущий resolver. Новые файлы сессионных тестов: `E/Audit20261023NeoNoCodeSessionTests.cs` и `.meta`.

В `N/NoCodeFloatBindingBehaviour.cs` добавить `ConfigureSource(IFloatValueSource)` и `RefreshBinding()`, сохранив protected `RefreshFromSource()` и editor helper. В `N/NoCodeFormattedText.cs` добавить `ConfigureSources(IReadOnlyList<IFloatValueSource>, string format)`, сохранив публичный refresh. Null конфигурация возвращает serialized binding; typed override не сериализуется. Formatted wrapper копирует список; для одинакового underlying reactive instance переиспользует адаптер и подписку внутри своей сессии, без глобального кеша. `NoCodeBindText` и `SetProgress` продолжают отвечать только за вывод.

**Минимальные тесты:** fake-source без GameObject проверяет initial read, Bind(A)→Bind(B), disposal, failed reads, срок polling, Suspend/Resume и rebind внутри callback. Интеграционные проверки используют существующие 20 случаев; отдельно проверить typed→serialized reset и две formatted-позиции с одним источником: одно уведомление и одна подписка.

**Совместимость:** сохранить serialized имена/значения, enum 0/1/2, target priority, интервалы 0.16/минимум 0.016 и поведение missing/invalid format. После typed API обновить те же четыре файла `D` примерами C# и Inspector. Точная integer/currency-типизация — отдельное предложение; float не становится lossless. Оценка P2: 3–5 дней плюс интеграционная проверка; риск средний из-за порядка событий, дедупликации и жизненного цикла.

## Границы, порядок и завершение

1. После стабилизации исходников зависимостей добавить регрессии F1/F2, выполнить минимальные исправления и централизованную проверку; не откладывать их ради P2.
2. Реализовать/проверить сессионное ядро и адаптеры, затем публичный API, затем по одному переводить wrappers. Никаких новых NoCode actions или editor builders.
3. `Neo.Reactive` владеет контрактами/handles; `Neo.Condition` — resolver/cache. Их текущие API достаточны; изменений не требуется, но их итоговые версии должны участвовать в проверках. `Neo.Editor` владеет `Assets/Neoxider/Editor/NoCode/ComponentFloatBindingDrawer.cs`: сохранить используемые им поля; нового drawer не требуется. `Neo.Tools.Text` сохраняет форматирование, `Neo.Extensions` — диагностику.
4. Обновить документацию; координатор проверяет существующий NoCode demo, UPM import и player stripping. Сохранять release `Samples~`, developer `Samples` и импортированный `Assets/Samples/NeoxiderTools/<version>/Demo Scenes/`; не переименовывать sample root ради новой API.
5. Перед закрытием нужны compile-проверки Unity 6.0–6.4 и 6.5+, EditMode и указанный PlayMode smoke. Версионные адаптации остаются в `NeoDiagnostics.StableId`/editor `NeoComponentHealth.StableId`, без новых распределённых `#if`.

Не считать проверку пройденной при незапущенном fixture, отсутствующей reference или ошибке зависимости: записать конкретного владельца и результат. Stripped IL2CPP и стоимость дополнительных reflection-чтений пока являются непроверенными рисками, не подтверждёнными дефектами. Автоматическое обнаружение замены полностью idle reactive-источника потребовало бы отдельной политики heartbeat; этот план сохраняет явный rebind/refresh и не добавляет скрытый polling. Произвольные методы по строкам и scene references в ScriptableObject не входят в дизайн.

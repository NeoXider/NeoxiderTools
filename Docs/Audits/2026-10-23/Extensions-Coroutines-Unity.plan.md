# Extensions-Coroutines-Unity: индивидуальный план реализации

Статус: план, будущая реализация не начата. Основание: [аудит](Extensions-Coroutines-Unity.md), `PLAN-CONTRACT.md`, manifest и текущие исходники. Продолжается исходная сессия; другие владельцы могут ещё менять свои модули.

Префиксы путей: **R** = `Assets/Neoxider/Scripts/Extensions/`, **T** = `Assets/Neoxider/Tests/Edit/Extensions/`, **D** = `Assets/Neoxider/Docs/Extensions/`. Каждый указанный ниже файл разрешается относительно этого точного префикса.

## 1. Что уже выполнено

XCU-01–07 исправлены в `R/CoroutineExtensions.cs`, `R/PrefabPreviewExtensions.cs`, `R/ScreenExtensions.cs`, `R/TransformExtensions.cs`, `R/ColorExtension.cs`: синхронное завершение, отмена и освобождение iterator, lifetime владельца, preview Sprite, viewport/depth, look-target, HDR HEX. Повторной реализации не требуют; правки основного владельца сохранены.

Добавлены `D/LifecycleAndOwnership.md`, `T/Audit20261023CoroutineUnityTests.cs` и `T/Audit20261023CoroutineUnityLifecycleTests.cs`: 13 EditMode cases и восемь runtime cases через `EnterPlayMode`/`ExitPlayMode`. В аудите зафиксированы успешные статические проверки 30 файлов, ссылок, GUID и scoped diff. Это не результаты компиляции: Unity, тесты, builds и profiling этой сессией не запускались, независимый QA не заявлен.

## 2. Остаток по приоритетам

Новых подтверждённых source defects не выявлено; XCU-08 остаётся ограничением API.

**P1 — проверка внесённых изменений; исполнитель: координатор.** Выполнить оба новых fixture и `T/ExtensionsLifecycleTests.cs`, `T/UnityExtensionsTests.cs`, `T/Audit20261023ExtensionsTests.cs`. Исправлять воспроизведённые failures. Проверить отсутствие лишних helpers после teardown, включая неуспешный тест. Это verification debt.

**P2 — XCU-08, подтверждённое ограничение API.** В `R/CoroutineExtensions.cs:95` прямо сохранена необозримость внешнего `StopCoroutine`/`StopAllCoroutines`. Repro: остановить долгую операцию native API при живом owner; tracked state/iterator могут сохраниться. Минимальная поддерживаемая коррекция обнаруженных вызывающих мест — `handle.Stop()`. Конкретный ошибочный production caller не установлен; автоматическую остановку по ненулевому native handle не изобретать. Добавить в `T/Audit20261023CoroutineUnityVerificationTests.cs` сценарий «raw stop → handle.Stop»: callback не выполняется, cleanup однократный, `IsRunning == false`, `Coroutine == null`.

**P2 — пробелы lifecycle-проверок; владелец источников: этот supplemental scope.** В том же новом verification fixture проверить reset с повторным запуском из cleanup, исключение одного Dispose при сохранении cleanup остальных, фазу custom wait относительно Update/LateUpdate, scaled/unscaled pause и отложенное уничтожение Sprite в Play Mode. Ожидаемы однократный terminal transition, сохранённый порядок child→parent и отсутствие продолжения отменённой работы. Менять `R/CoroutineExtensions.cs` или `R/PrefabPreviewExtensions.cs` только при воспроизведённом несоответствии; allocation cost пока неизвестна.

**P2 — межмодульная совместимость; владельцы ниже.**

| Владелец / точные файлы | Действие и наблюдаемая приёмка |
| --- | --- |
| `Neo.Tools.Move`: `Assets/Neoxider/Scripts/Tools/Move/ScreenPositioner.cs` | После проверки Extensions проверить orthographic, повернутую камеру и `_useDepth`. Новый depth отсчитывается от near plane; `oldDepth - nearClipPlane` сохраняет прежнюю глубину. Проверить screen pixels/world position до миграции; serialized значения автоматически не переписывать. |
| `Neo.Tools.Inventory`: `Assets/Neoxider/Scripts/Tools/Inventory/Data/InventoryItemData.cs`, `Assets/Neoxider/Scripts/Tools/Inventory/UI/InventoryItemView.cs` | `OnValidate` сохраняет fallback в `_icon`, view использует preview. Проверить очистку кэша и последующий Bind: авторский Sprite жив, generated preview переиспрашивается. Это риск потребителя, потеря иконки в исполнении ещё не подтверждена. При воспроизведении отделить runtime preview-кэш от serialized авторского `_icon`, сохранив поле и assets. |
| Основной `Neo.Extensions` | В `D/README.md` пока отсутствует ссылка на новый lifecycle guide; добавить её, синхронизировать описание depth с владельцем Move. |

**P3 — неподтверждённый Input System-only риск.** `R/UIUtils.cs:24` использует legacy `Input.mousePosition`; текущий проект работает с Both. После воспроизведения в отдельной конфигурации добавить `UIUtils.GetUIElementsAtPosition(Vector2 position)` в существующий файл, оставив старый API. Вызывающий код использует уже существующий `Neo.Tools.MouseInputCompat.TryGetPosition` из `Assets/Neoxider/Scripts/Tools/Input/MouseInputCompat.cs`, который переиспользует `OptionalInputSystemAdapter.cs`. Владелец `Neo.Tools.Input` проверяет свой adapter. Направление остаётся Input → Extensions; обратная ссылка создала бы цикл. Приёмка: заданная позиция попадает в ожидаемый UI, вне UI результат пуст, отсутствие EventSystem безопасно; backend одинаково работает в Both/new-only. Новая обязательная input-зависимость не нужна.

## 3. Существенное улучшение: CoroutineScope

**Предложение, P3, отдельно от исправлений.** Сейчас вызывающий код вручную связывает handles, cleanup и замену операций; boolean не отличает завершение, отмену и fault. Готового scope в текущих Extensions не найдено. Переиспользовать существующие runner/tracker, iterator registry, `destroyCancellationToken`, `NeoDiagnostics` и `SafeDestroy`; новый scheduler или обязательную async-библиотеку не вводить.

Предлагаемый минимальный публичный C# контракт:

```csharp
public interface ICoroutineExecutor
{
    IDisposable Start(IEnumerator routine,
        Action<CoroutineOperationState, Exception> onTerminated);
}
```

`CoroutineOperationState`: `Running`, `Completed`, `Cancelled`, `Faulted`. `CoroutineOperation` предоставляет `CoroutineOperationState State`, `Exception Error`, `void Cancel()`. `CoroutineScope(ICoroutineExecutor executor)` предоставляет `CoroutineOperation Run(IEnumerator routine)`, `int ActiveCount`, `void CancelAll()`, `void Dispose()`. `UnityCoroutineExecutor(MonoBehaviour owner)` адаптирует существующее исполнение; global выбирается явно через `static UnityCoroutineExecutor Global()`. Уничтоженный заданный owner отменяет работу без global fallback.

Точные будущие файлы: новые `R/ICoroutineExecutor.cs`, `R/CoroutineOperation.cs` с enum, `R/CoroutineScope.cs`, `R/UnityCoroutineExecutor.cs`; существующий `R/CoroutineExtensions.cs` получает terminal notification и additive owner-overload `Start(this MonoBehaviour, IEnumerator)`. `CoroutineHandle : IDisposable` делегирует Dispose в Stop. Старые методы, `IsRunning`, `Coroutine` и null-owner fallback сохраняются.

Scope — обычный C# объект без scene search и UnityEvent chains; executor-интерфейс отделяет Unity от тестируемой ownership-логики. Регистрировать operation до запуска executor: синхронный completion не должен оставлять активный элемент. Terminal state устанавливать до cleanup, уведомлять один раз после него; исходное execution exception сохранять в Error, cleanup exceptions отправлять через diagnostics. Dispose сначала закрывает scope, затем отменяет snapshot; повторный Dispose безопасен, Run после него бросает `ObjectDisposedException` без старта. API первой итерации предназначен для главного потока. `CancellationToken`, keyed Replace и scope-overload аудиофейда — следующая опциональная итерация после определения threading/reentry semantics; перекрывающиеся старые fades сохраняют своё поведение.

Новые минимальные тесты: `T/Audit20261023CoroutineUnityScopeTests.cs` с управляемым executor для синхронного completion/fault, reentry и cancellation; `T/Audit20261023CoroutineUnityScopeLifecycleTests.cs` для реального owner, scene unload и ожиданий. Проверять State/Error, callback counts, cleanup order, независимость двух scope и `ActiveCount == 0` после завершения. Новые package-файлы требуют уникальных `.meta`.

Отдельный MonoBehaviour/editor пока не требуется: scene component создаёт scope, предоставляет typed `CancelAll()` для UnityEvent и выбирает OnDisable policy. Расширить `D/LifecycleAndOwnership.md` C#/NoCode примерами EN/RU; навигацию меняет основной владелец. Оценка: 3–5 дней плюс центральная проверка; риск средний: уведомления и native waits.

## 4. Порядок и условия завершения

1. После завершения source-changing аудитов координатор фиксирует состояние и проверяет текущие исправления; сначала устраняются воспроизведённые failures и добавляются узкие regression checks.
2. Согласовать результаты consumer-проверок с Move/Inventory и primary Extensions; необязательную input-совместимость делать после подтверждения, с повторным использованием готового adapter.
3. CoroutineScope реализовывать отдельно: pure C# state/ownership → существующий Unity executor → минимальные tests → документация. Автоматическое наблюдение raw native stops не входит в обещания нового API.
4. Компилировать Unity 6.0–6.4 и 6.5+, включая `NeoDiagnostics.StableId` и preview lifecycle branch; одна ветка не доказывает другую. Запустить названные EditMode fixtures и реальные runtime-сценарии при domain reload enabled/disabled, проверить console и cleanup. Текущий EditMode asmdef уже ссылается на Extensions; Play asmdef ссылки не имеет, менять его для существующих fixture не нужно.
5. Завершать только при наблюдаемом соответствии контрактам: отменённый callback не вызывается, ресурсы освобождаются один раз, borrowed assets живы, координаты соответствуют viewport/depth. Ошибка компиляции, непредусмотренный лог, flaky frame expectation или неисполненная обязательная проверка оставляют пункт открытым.

Serialized поля/enum значения не менять. Samples не добавляются: сохраняются активный `Samples~`, package paths и поддержка dev/imported layout. Финальные UTF-8/GUID/link/diff и release/changelog проверки выполняются координатором после реализации. Выбор token-threading и keyed replacement нужен только для следующей фазы улучшения; он не задерживает проверку уже внесённых исправлений. В фазе планирования изменён только этот файл; Unity/build/test процессы, commits и child agents не запускались.

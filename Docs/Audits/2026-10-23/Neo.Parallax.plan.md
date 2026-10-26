# Neo.Parallax: индивидуальный план реализации

Статус: только план, реализация предложений не начата. Продолжение исходного аудита; профиль `gpt-6.1-sol`, reasoning `max`. Основание: полный `PLAN-CONTRACT.md`, собственный `Neo.Parallax.md`, актуальная manifest-запись, текущие исходник, документация, регрессии и границы зависимостей.

## Текущий результат

Полностью проверен единственный принадлежащий модулю C# файл `Assets/Neoxider/Scripts/Parallax/ParallaxLayer.cs`. Уже исправлены удаление чужих детей, повторное применение позиции/масштаба, восстановление состояния и предыдущего renderer, неверные stride и null-only варианты, повторная переработка padding, зависимость recycling от дальности скачка и игнорирование выключенного editor preview. Перестроение из `OnValidate` отложено; teardown отменяет callback. Уточнена существующая семантика multiplier.

Обновлён канонический `Assets/Neoxider/Docs/Parallax/ParallaxLayer.md`; добавлен `Assets/Neoxider/Tests/Edit/Parallax/Audit20261023NeoParallaxRegressionTests.cs` с 18 случаями. Статические проверки подтвердили сохранение публичного API и 15 serialized fields, UTF-8, whitespace, delimiters, GUID, ссылки тестовой сборки и чистый scoped `git diff --check`. Unity-компиляция, исполнение тестов, Play Mode и визуальная проверка не выполнялись. Эти исправления требуют проверки, а не повторной реализации.

## Остаток: приоритет, доказательство, действие

**V1 — P1, долг верификации; координатор и владелец Neo.Parallax.** После завершения изменяющих исходники аудитов собрать зависимости и выполнить 18 случаев `Neo.Editor.Tests.Audit20261023NeoParallaxRegressionTests` и существующий `ParallaxLayerEditModeTests`. Наблюдать сохранность authored hierarchy, правильные bounds/stride, восстановление renderer и стабильный пул после teleport. Два теста вручную отправляют `LateUpdate`: они не доказывают реальный порядок Unity callbacks. В Play Mode отдельно проверить немедленное выключение старых тайлов до deferred destruction, rebind/disable/re-enable/destroy, отложенную Inspector-валидацию, stop/domain reload, scaled-time scroll и материал/sorting. При реальном падении — минимальное исправление по воспроизведению с проверкой наблюдаемого результата; заранее переписывать lifecycle не требуется.

**F1 — P2, подтверждённая исходником незакрытая граница количества тайлов; владелец Neo.Parallax.** `ComputeTileCounts` выполняет `paddingTiles.x/y * 2` и сложение в `int` без явной проверки представимости; `BuildTiles` сразу использует результат. `OnValidate` ограничивает padding только снизу. Воспроизведение для будущего теста: положительный stride, orthographic camera и padding `int.MaxValue`; проверять корректный отказ, отсутствие нового пула и сохранённый source renderer. Переполнение выражения подтверждается структурой кода; конкретное поведение Unity не запускалось. Минимальная коррекция: вычислять размеры в достаточно широком типе, проверять конечность, диапазон осей и произведение до преобразования в `int` и до создания объектов. Существующие нормальные конфигурации сохранить; отказ сообщать через `NeoDiagnostics`. Добавить `Assets/Neoxider/Tests/Edit/Parallax/Audit20261023NeoParallaxCapacityTests.cs` с публичным сценарием отказа/восстановления. Не смешивать защиту представимости с новым произвольным лимитом памяти.

**R1 — P2, неподтверждённые интеграционные риски; координатор/Neo.Parallax.** Проверить signed scale вложенного источника, Sliced/Tiled renderer, разные размеры вариантов, уничтожение камеры и queued callback при disable. Успех: корректная геометрия в документированной axis-aligned области, восстановленный source, отсутствие новых ошибок. Сбой сначала воспроизвести; существующие ограничения rotated/sheared transforms и perspective coverage сами по себе не объявлять новой регрессией.

## Существенное улучшение: расчёт layout и явная конфигурация

**P2, предложение.** Программист сейчас имеет только `SetTargetCamera`; настройки и перестроение геометрии зависят от Inspector. Размеры камеры читаются один раз. Отсутствует бюджет пула: stride около `0.001` при viewport 4×4 и двух tiled axes запрашивает примерно 16 миллионов объектов. Это расчёт запроса, а не проведённый эксперимент с памятью.

Переиспользовать `ComputeCellSize`, формулу counts после F1, однократный wrap, renderer-copy/snapshot/cleanup, Unity math и `NeoDiagnostics`. Справочники `neoxider-tools` уже проверены в аудите; отдельный готовый layout-контракт в принадлежащем inventory отсутствует. Новый менеджер, pool framework, NoCode bridge или зависимость не нужны.

Выделить обычный C# расчёт без `MonoBehaviour`, scene search, `Time` и создания объектов. Он получает измеренные extents/spacing и camera bounds в координатах слоя, возвращает immutable layout или typed error. Предлагаемые контракты:

- `ParallaxLayoutCalculator.TryCreate(ParallaxLayoutSettings settings, Rect cameraBounds, out ParallaxLayout layout, out ParallaxLayoutError error)` возвращает `bool`.
- `ParallaxLayout` предоставляет rows/columns/pitch/count и `Vector2 GetWrapOffset(Vector2 tilePosition, Vector2 cameraCenter)`.
- Компонент получает `bool TryConfigure(ParallaxLayerSettings settings, SpriteRenderer template, Camera camera, out ParallaxLayoutError error)`, `bool TryRefreshLayout(out ParallaxLayoutError error)` и `void RefreshLayout()` как тонкий вход для UnityEvent.

Settings содержат данные/asset references; scene dependencies передаются отдельно. Сначала проверить конфигурацию и бюджет, затем заменить пул. Ошибка сохраняет предыдущий рабочий пул; при отсутствии рабочего пула source остаётся в исходном enabled state. `RefreshLayout` сохраняет текущую позицию и накопленный scroll; существующий `SetTargetCamera` сохраняет документированное перебазирование. Для inactive-компонента не создавать тайлы до enable.

**Точные файлы реализации:** существующий `Assets/Neoxider/Scripts/Parallax/ParallaxLayer.cs`; новые `Assets/Neoxider/Scripts/Parallax/ParallaxLayoutSettings.cs`, `Assets/Neoxider/Scripts/Parallax/ParallaxLayout.cs`, `Assets/Neoxider/Scripts/Parallax/ParallaxLayoutCalculator.cs`, `Assets/Neoxider/Scripts/Parallax/ParallaxLayoutError.cs`, `Assets/Neoxider/Scripts/Parallax/ParallaxLayerSettings.cs`. Всё остаётся в `Neo.Parallax`; новым пакетным файлам нужны уникальные `.meta`.

Добавить `Assets/Neoxider/Tests/Edit/Parallax/Audit20261023NeoParallaxLayoutTests.cs`: отрицательные координаты/телепорт, выключенные оси, overflow/бюджет, точный отказ без scene allocation. Проверять выходные размеры и покрытие, не копию алгоритма. Через `TryConfigure` проверить равенство C# и Inspector-настройки, сохранение рабочего пула после ошибки и scroll после refresh.

**Совместимость:** сохранить все 15 полей, их defaults, `MovedFrom`, оба существующих public API и направление multiplier. Новое ограничение `MaxTiles=0` означает прежнее отсутствие прикладного бюджета; положительный лимит включается явно. Обязательный новый default требует отдельного продуктового решения, но не блокирует F1. Проверки числовой представимости обязательны всегда. Расширение **P3**: измерение perspective footprint на плоскости слоя и opt-in refresh при zoom/aspect; legacy fixed counts и ручной refresh сохраняются по умолчанию. Произвольный shear пока исключить явно.

В том же английском `ParallaxLayer.md` показать typed C# конфигурацию, ошибки, сохранение scroll, настройку бюджета и Inspector/UnityEvent `RefreshLayout`. Новые scene-only references в ScriptableObject не вводить. Оценка: F1 0.5–1 день; ядро/API и тесты 3–5 дней; perspective/automatic refresh ещё 1–2 дня. Риск средний: миграция поведения и геометрия требуют сравнения реального изображения.

## Границы и порядок

1. Дождаться согласованного состояния исходников, выполнить V1. `Neo.Extensions` (владелец `/root/audit_extensions`) предоставляет `NeoDiagnostics.StableId`; текущая ветка `UNITY_6000_5_OR_NEWER` возвращает `GetEntityId().ToString()`, старая — `GetInstanceID().ToString()`. Общую совместимость исправляет только этот владелец. `Neo.PropertyAttribute` (`neo-audit-1023-b04-08`) предоставляет существующие атрибуты; изменений не требуется.
2. Реализовать F1 с capacity-регрессиями; затем расчёт layout, typed API, компонентный адаптер, документацию. Не менять общий Inspector ради добавления кнопки.
3. Для `Assets/Neoxider/Tests/Play/Audit20261023NeoParallaxLifecycleTests.cs` сначала согласовать исполнение зависимости: владелец `Neo.Tests.Play` (`neo-audit-1023-b06-04`) / координатор добавляет `Neo.Parallax` в `Assets/Neoxider/Tests/Play/Neo.Tests.Play.asmdef`. Сейчас ссылки нет. Затем добавить frame-based lifecycle-тесты. `Neo.Editor.Tests` (`neo-audit-1023-b03-04`) уже ссылается на модуль; его asmdef менять не нужно.
4. Координатор выполняет release-проверки: Editor/player compilation в Unity 6.0–6.4 и 6.5+, существующие и новые EditMode/PlayMode tests, console и визуальную проверку coverage. Package/sample layout не менять: учитывать активный `Samples~` либо developer `Samples` и импортированный `Assets/Samples/NeoxiderTools/<version>/<sample name>`. Для данной реализации изменение сцен/сэмплов не требуется.

## Приёмка и условия остановки

Обязательные результаты: authored objects переживают весь lifecycle; root/child scale применяется один раз; invalid/oversized layout не создаёт частичный пул; teleport не меняет число тайлов; stationary padding не меняет RNG/sprites; refresh сохраняет motion state; preview off не генерирует editor tiles. На обеих версиях Unity должны скомпилироваться соответствующие Editor/player и централизованные version branches.

Не считать работу проверенной при compile failure, утечке объектов, уничтожении authored hierarchy, неполном выборе тестов или отсутствии исполнения требуемой версии. Зафиксировать воспроизведение и владельца; независимые безопасные пункты можно продолжать. Непройденные Unity-проверки остаются долгом. Продуктовые решения о mandatory budget, automatic refresh и новой perspective-семантике не подменяют исправления подтверждённых ошибок. В фазе 2 создаётся только этот план; runtime, тесты, public docs, аудит и shared configuration не изменяются.

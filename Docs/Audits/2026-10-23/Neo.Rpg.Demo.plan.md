# Neo.Rpg.Demo — индивидуальный план реализации

Статус: план, реализация следующих шагов не начата. Продолжение исходной auditor/session; основание — полный аудит `Neo.Rpg.Demo.md`, `PLAN-CONTRACT.md`, собственная запись manifest и повторное чтение актуального кода. Смежные аудиторы ещё могут менять свои API; перед интеграцией зависимости сверяются повторно.

## 1. Уже выполнено

Проверены все 49 C# файлов. В 11 исходниках исправлены F1–F8: пороги XP, потеря Dice-ресурсов, подписки views, повторная генерация клеток и освобождение sprites, параметры Condition, listeners смерти, камера/время жизни health bars, restart без сервисов. Сохранены старые serialized fields и публичные сигнатуры; добавлены `ConfigureAmounts` и перегрузка `DiceDieView.Initialize`. Эти исправления повторно реализовывать не нужно.

Добавлены 14 EditMode и три PlayMode случая в `Assets/Neoxider/Tests/Edit/Editor/Audit20261023NeoRpgDemoTests.cs` и `Assets/Neoxider/Tests/Play/Audit20261023NeoRpgDemoLifecycleTests.cs`. Обновлён канонический `Assets/Neoxider/Docs/Examples/README.md`. Пройдены статические проверки ownership, UTF-8, GUID, полей/сигнатур и diff. Unity-компиляция, запуск тестов, взаимодействие и визуальное поведение не проверялись.

Ниже `S` означает `Assets/Neoxider/Samples~/Demo/Scripts`, `D` — владельца `Neo.Rpg.Demo`.

## 2. Остаток: дефекты, долг проверки, риски

| Приоритет / статус | Доказательство, владелец, минимальное действие и наблюдаемый результат |
| --- | --- |
| P0 — долг проверки F1–F8 | Координатор после завершения source-аудитов компилирует импортированный Demo и запускает обе указанные fixtures плюс `SampleRuntimeSceneSmokeTests`. D исправляет только воспроизведённые отказы своих изменений. Ожидаются HP 100→93→96, score 11, положительные XP-пороги, видимые Dice и обновления только от текущего поля. Пропуск optional assembly не считается проверкой исправлений. |
| P1 — подтверждённое владение shell | `S/Shell/NeoDemoShell.cs::Build/GetGradient`: Canvas без owner, новая texture/sprite при каждом Build, освобождения нет. D реализует ограниченный session-контракт ниже. После удаления компонента его UI и градиент исчезают; соседняя demo работает. |
| P1 — подтверждённые runtime assets | `S/Shell/AudioDemoController.cs::BuildClips/OnDestroy` создаёт шесть клипов, но только снимает listeners; `Shell/LevelDemoController.cs` создаёт `_curve` без уничтожения. D закрывает созданные playback/model consumers, затем освобождает собственные assets. Владелец `Neo.Audio` проверяет crossfade/playlist-ссылки. Посторонняя музыка и внешние assets сохраняются. |
| P1 — подтверждённый дефект между сборками | `S/Survivor/SurvivorGame.cs::DestroyPoolStorage` удаляет `<name>_Pool`; `PoolManager._pools` продолжает удерживать pool/template, одинаковые имена используют общий root. Сначала владелец `Neo.Tools.Spawner` добавляет освобождение по идентичности prefab, затем D мигрирует Survivor. Два одноимённых шаблона освобождаются независимо; повторное открытие сцены не накапливает записи. |
| P1 — недостающие сценарии F2/F3/F4/F7 | D добавляет проверки отмены drag при disable, освобождения Dice fallback sprite и сохранности внешнего Canvas/root. Координатор проверяет импортированные prefab-пути и player build без editor loaders. Наблюдения: preview удалён, tray доступен, внешние объекты живы, authored art/scale сохранены. |
| P2 — риск совместимости, не подтверждённый прогоном | `S/Neo.Rpg.Demo.asmdef` прямо ссылается на Mirror; Grid/Survivor используют legacy Input. Координатор и владельцы `Neo.Network`/`Neo.Tools.Input` определяют поддерживаемую матрицу и проверяют её в изолированном проекте. Удаление reference или переключение глобального input setting вслепую не является исправлением. |

## 3. Существенное улучшение S1: владение demo-сессией

Проблема разработчика: пример нельзя безопасно собрать и убрать как отдельный блок в существующей сцене; UI, native assets и persistent services имеют разное время жизни. Предложение ещё не реализовано.

Развить существующий `NeoDemoShell.Context` до `IDisposable`. Сохранить `Build(string title, Color accent)` и добавить `Build(string title, Color accent, Transform owner)`, `Context.IsDisposed`, `Context.SetVisible(bool)` и идемпотентный `Dispose()`. Context хранит собственные Canvas, sprite и texture; градиент перестаёт перезаписываться в статическом поле. Dispose сразу блокирует UI callbacks и освобождает Unity-объекты подходящим для режима способом. OnDisable скрывает UI, OnEnable восстанавливает его без повторного Build; OnDestroy снимает listeners и вызывает Dispose. Камера, EventSystem и общие managers имеют сценовое владение.

Созданные контроллером clips/curves/configs учитываются самим контроллером. Использовать существующие `SurvivorUI`, `ModuleDemoSceneInfo`, `AM.CurrentMusicEntry/GetCurrentMusicClip/StopMusic`, `LevelComponent`, state events и обычные retained delegates. Нового глобального resource manager не требуется. Audio cleanup останавливает только принадлежащий сессии playback; сохранённая identity записи защищает музыку другого клиента. Удержание clips при crossfade проверяет Audio-владелец; если текущих API недостаточно, он определяет отдельную узкую операцию освобождения playback.

Для Survivor переиспользовать `NeoObjectPool.Clear()`: он уже существует, но не удаляет запись manager и не определяет судьбу выданных объектов. Владелец Spawner проектирует `bool ReleasePool(GameObject prefab)` с явным правилом активных экземпляров, отменой повторного release и независимыми storage roots. D хранит реальные шаблоны вместо имён, прекращает spawning/projectiles, возвращает actors и освобождает свои pools до уничтожения templates. Общие pools остаются доступны.

Quest требует отдельной зависимости: `QuestManager.RegisterQuest` регистрирует config по ID в `_knownQuests`, unregister сейчас отсутствует. Владелец `Neo.Quest` сначала определяет снятие именно принадлежащей регистрации без удаления saved progress и чужого config с тем же ID. После этого D освобождает runtime quests. В StateMachine demo scene tint привязывается через существующий `OnStateChanged` в компоненте; `StateData` сохраняет данные без захваченного renderer. Созданные behaviour/state assets закрываются в порядке consumers → definitions.

## 4. Точные файлы и совместимость

Основная реализация — `S/Shell/NeoDemoShell.cs`; миграция его десяти callers — `S/ProgressionDemoUI.cs` и `S/Shell/{AnimatorsDemoController,AudioDemoController,LevelDemoController,NetworkDemoController,ParallaxDemoController,QuestDemoController,SaveDemoController,SettingsDemoController,StateMachineDemoController}.cs`. Отдельные lifetime-адаптеры: `S/QuestDemoView.cs`, `S/Survivor/SurvivorGame.cs`. Сохранить MonoScript GUID, namespaces, serialized fields, кнопки и persistent UnityEvents. Save/Settings/Shop сохраняют документированную persistence-семантику.

Смежные изменения принадлежат другим владельцам: `Assets/Neoxider/Scripts/Tools/Spawner/{PoolManager,NeoObjectPool}.cs` — `Neo.Tools.Spawner`; `Assets/Neoxider/Scripts/Quest/QuestManager.cs` — `Neo.Quest`; при необходимости `Assets/Neoxider/Scripts/Audio/AudioSimple/AM.cs` — `Neo.Audio`. API правил Level/StateMachine остаются существующими.

Будущие новые регрессии: `Assets/Neoxider/Tests/Play/Audit20261023NeoRpgDemoSessionTests.cs` и уникальная `.meta`. Расширить собственную lifecycle fixture недостающими сценариями; shared fixtures/asmdefs меняет координатор. В существующем Examples README описать C# owner/Dispose и scene/NoCode enable-disable workflow, принадлежность assets и ограничения зависимостей; English entry сохранить, краткое RU пояснение добавить рядом.

## 5. Порядок, приёмка, условия остановки

1. Координатор закрывает P0 на согласованном состоянии зависимостей; текущие regression sources отделяются от реально прошедших тестов.
2. D реализует shell cleanup и его минимальные регрессии; затем Audio/Level consumers и teardown. Оценка S1: 3–5 дней, средний риск callbacks/native lifetime.
3. Spawner-владелец реализует и проверяет pool API; D подключает Survivor. Quest cleanup выполняется после контракта регистрации. На Spawner-интеграцию отдельно 1–2 дня; отсутствие API останавливает только зависимый шаг, без reflection в private dictionaries.
4. Мигрировать остальные перечисленные controllers, документацию; проверить serialization и release packaging.

Минимальная приёмка S1: после прогрева общих caches выполнить 20 create/dispose циклов; после завершения deferred Destroy количество принадлежащих сессии Canvas/sprite/texture/clip возвращается к исходному. Двойной Dispose безопасен. Удаление компонента прекращает его callbacks. Две demo с общими камерой/AM/pool manager переживают закрытие одной; внешние assets и saved progress сохраняются. Disable-enable не удваивает listeners. Для pool дополнительно проверить активный экземпляр при teardown и повторное создание после release.

Координатор выполняет EditMode/PlayMode и compile checks отдельно в Unity 6.0–6.4 и 6.5+; оба несовместимых API-направления требуют реального компилятора своей версии. Версионные stable-ID gates сохраняются в `NeoDiagnostics`/`NeoComponentHealth`. Проверить developer `Samples`, release `Samples~`, импортированный `Assets/Samples/NeoxiderTools/10.19.0/Demo Scenes`; исходный release layout сохраняется, временный импорт выполняется координатором изолированно. Нужны решения только о заявленной поддержке без Mirror/new-input-only и судьбе активных pools; неподтверждённые варианты остаются рисками. До этих прогонов статус выполнения/совместимости не объявляется успешным.

В фазе планирования изменён только этот файл; Unity, компиляторы, сборки, test runners и дочерние агенты не запускались.

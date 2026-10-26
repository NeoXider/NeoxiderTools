# Neo.Tools.Move — индивидуальный план реализации

Фаза 2, 2026-10-23; профиль задания: `gpt-6.1-sol`, `max`. Прочитаны контракт, отчёт, manifest и текущий контекст.
Изменён только этот план; описанная ниже реализация не выполнялась.

Обозначения точных корней: `S = Assets/Neoxider/Scripts/Tools/Move`,
`T = Assets/Neoxider/Tests/Edit/Tools/Move`, `D = Assets/Neoxider/Docs/Tools/Move`.
Корни определяют точные пути ниже.

## Текущее состояние

Проверены 31 C# файл, 8 453 строки. Уже исправлены M01–M11: самозаземление 2D,
отбрасывание малого шага, координаты команд/мыши, поворот относительно родителя, sprint cleanup,
аналоговая скорость, кэш Z-ограничения, направление/стартовая рандомизация сил, ось 2D-вращения,
недоступный legacy input. Изменены 13 исходников и 15 руководств; эти исправления завершены.

Созданы `T/Audit20261023NeoToolsMoveRegressionTests.cs` и
`T/Audit20261023NeoToolsMovePhysics2DTests.cs`: 39 случаев. Статические проверки UTF-8, diff,
GUID, ссылок, публичных объявлений и serialized field names прошли. Unity, компиляция,
EditMode/PlayMode и визуальное поведение **не проверены выполнением**.

## Оставшиеся подтверждённые исправления

Подтверждение — трассировка кода, без runtime-воспроизведения. Владелец — `Neo.Tools.Move`.

| Приоритет / пункт | Доказательство и минимальная коррекция | Наблюдаемая приёмка / файлы |
|---|---|---|
| P1, D1: перекрывающиеся cursor-блокировки | `CursorLockController.cs:828–839` запоминает только самостоятельно выключенный gate; `ReleaseControlInternal:685–687` восстанавливает его при удалении нижнего владельца. Два владельца A/B с одним player и `DisableMovementWhileCursorVisible = true`: A.ShowCursor → B.ShowCursor → A.ReleaseControl включает движение под открытым B. Ввести внутренний учёт владельцев отдельно для movement/look: каждый запрос учитывается даже при уже выключенном gate; release снимает только свой запрос. | После release A `MovementEnabled` остаётся false; после последнего release возвращается исходное состояние, включая исходное false. Повторный release безопасен. Изменить `S/CursorLockController.cs`, добавить `T/Audit20261023NeoToolsMoveCursorOwnershipTests.cs`, уточнить `D/CursorLockController.md`. Не ограничиваться повторным вызовом top.ApplyCursorState: он не решает общий учёт владения. |
| P2, D2: скачок начального угла | `CameraRotationController.Start:64` сохраняет unsigned Euler, затем `Update:102–116` линейно ограничивает его. Начальный X=350° при диапазоне −45..45 и нулевой первой дельте превращается в +45° вместо −10°. Выбрать эквивалент начального угла относительно диапазона оси; сохранить корректность существующих диапазонов 0..360. | Первый drag без перемещения сохраняет quaternion; проверить signed/unsigned диапазоны и отключённые оси. Файлы: `S/CameraRotationController.cs`, новый `T/Audit20261023NeoToolsMoveCameraDragTests.cs`, `D/CameraRotationController.md`. |
| P2, D3: modifier включён после нажатия мыши | `CameraRotationController.Update:79` начинает drag только на buttonDown, но строка 92 вращает при buttonHeld+modifier. Нажать мышь без modifier, затем modifier: используется старый lastMousePosition, события старта нет. Вычислять переходы drag-активности; при входе обновлять baseline, при выходе/disable завершать один раз. | Start/end сбалансированы, скачка нет. Те же файлы, что D2. Внутренний `ProcessDragSample(Vector2 position, bool held, bool modifier, float deltaTime)` позволит существующей friend test assembly наблюдать Transform/события без устройства. |

## Долг проверки и неподтверждённые риски

**P0, координатор после окончания source-аудитов:** скомпилировать и выполнить оба новых fixture,
существующие `TransformMoveTests`, `CharacterControllerInputTests`, `FreeFlyCameraControllerTests`,
`CursorLockPlayerControlTests`, diagnostics defaults и Inspector button coverage. Проверить временную
Physics2D scene: создание, симуляцию, cleanup; harness тоже ещё не выполнен.

**P1, координатор + Move:** New-only/Both/Legacy, отсутствующая мышь, pause/resume, реальный disable,
Rigidbody interpolation и local/remote authority. Приёмка M01–M11: четверть стика даёт скорость 2 при
walk=8; XZ target (6,8) сохраняет Y; 0.05 units/s движется при 240 Hz; self collider не является землёй;
sprint disable возвращает walk speed; родитель не искажает мировой aim.

**P2, неподтверждённый риск:** `AdvancedForceApplier.ApplyForce:184–197` ограничивает
текущую velocity после AddForce, а native integration может применить силу позже. Сначала измерить
скорость после первого/второго physics step для Force/Impulse и обоих clamp-флагов. Владелец Move;
файлы — `S/AdvancedForceApplier.cs`, новый `T/Audit20261023NeoToolsMoveForceLimitTests.cs`,
`D/AdvancedForceApplier.md`. Корректировать только воспроизведённое нарушение заявленного ограничения;
строгий post-step cap и ограничение разового вызова требуют явно выбранного контракта.

## Существенное улучшение S1: общее математическое ядро

**Предложение, не реализация; P3, 3–5 дней.** Дублирование plane mapping, arrival и единиц ввода
уже привело к M02–M05. Добавить обычные C# value-типы и функции, без менеджера или scene references:

- `S/MovementToolkit/MotionPlane.cs`: `ProjectPoint(Vector3)`, `UnprojectPoint(Vector2, Vector3 reference)`, `ProjectDelta(Vector2)`.
- `S/MovementToolkit/MotionStep.cs`: `WorldDelta`, `Arrived`, `HasDisplacement`.
- `S/MovementToolkit/MotionMath.cs`: `StepToPoint(Vector3 position, Vector3 target, MotionPlane plane, float speed, float deltaTime, float arrivalDistance)`.
- `S/MovementToolkit/RotationMath.cs`: signed-angle/range расчёты с явно переданными временем и единицами.

Переиспользовать `WorldToPlane`, `DirectionUtils`, `NeoLookRate`, Unity vector math и input compatibility APIs.
Мигрировать вычисления в `S/MovementToolkit/KeyboardMover.cs`, `S/MovementToolkit/MouseMover2D.cs`,
`S/MovementToolkit/MouseMover3D.cs`, затем `S/CameraRotationController.cs`.
`IMover` и его исторические direct-command/event semantics сохранить.

У drag сейчас pixels/frame дополнительно умножаются на deltaTime: зависимость от FPS видна из формулы,
но чувствительность требует миграции: opt-in pixels→degrees со старым default; новый default — отдельное решение.

Минимальные тесты — новый `T/Audit20261023NeoToolsMoveMotionMathTests.cs`: все плоскости/оси,
normal coordinate, zero speed/time, arrival без overshoot, малые шаги, суммарная эквивалентность
30/60/240 Hz. Документация: `D/MovementToolkit/README.md`, `D/MovementToolkit/IMover.md`,
`D/MovementToolkit/KeyboardMover.md`, `D/MovementToolkit/MouseMover2D.md`, `D/MovementToolkit/MouseMover3D.md`,
`D/CameraRotationController.md`: C# вызов плюс Inspector workflow.
Главный риск — незаметное изменение event timing и старой чувствительности.

## Существенное улучшение S2: блокировки ввода с независимыми владельцами

**Предложение, не реализация; P3 после D1, 2–4 дня.** Развить внутренний учёт D1 в
`S/CharacterController/InputGateState.cs` и `S/CharacterController/InputBlockMask.cs`: base flags плюс scoped tokens;
`AcquireInputBlock(InputBlockMask mask, object owner)` возвращает `IDisposable`, повторный Dispose безопасен.
Существующие setters задают base intent; эффективный ввод учитывает tokens. Семантику getters/setters
зафиксировать до публичного внедрения, включая совместимость legacy movement/jump gating.

Адаптеры: `S/PlayerController3DPhysics.cs`, `S/PlayerController2DPhysics.cs`,
`S/CharacterController/NeoCharacterInput.cs`, `S/CharacterController/NeoCameraInput.cs`, затем `S/CursorLockController.cs`.
Переиспользовать существующие typed setters и EM pause/resume, без нового singleton/NoCode-компонента.
Тесты: `T/Audit20261023NeoToolsMoveInputGateTests.cs`; два меню, explicit disable во время блокировки,
destroy/unload, снятие верхнего/нижнего владельца, повторная регистрация и отсутствие удержания уничтоженных owners.
Критерий: закрытие меню не включает externally disabled input.

Обновить `D/CursorLockController.md`, `D/PlayerController2DPhysics.md`, `D/PlayerController3DPhysics.md`,
`D/CharacterController/README.md`, `D/CharacterController/NeoCharacterInput.md`, `D/CharacterController/NeoCameraInput.md`:
C# token и scene lifecycle.
Риск — изменение смысла старых setters; расширять по адаптерам, сохраняя serialized fields и UnityEvents.

## Границы, порядок и завершение

1. После параллельных аудитов координатор фиксирует source snapshot, выполняет P0. Затем D1 и D2/D3 с регрессиями; force-risk — через воспроизведение.
2. S1: сначала math API/контракты, затем компоненты и документация. S2: сначала gate model, legacy/CMF адаптеры, затем интеграции. Дополнительный editor слой не нужен.
3. Владельцы зависимостей по manifest: `Neo.Tools.Input` — Mouse/KeyInputCompat; `Neo.Tools.Managers` — EM; `Neo.Settings` — sensitivity; `Neo.Network`/`Neo.Network.Contracts` — authority. Их API сверить до интеграции, чужие файлы не менять. CMF — vendored dependency, без правки его motor.
4. `Neo.UI` владеет `Assets/Neoxider/Scripts/UI/PausePage.cs`, который самостоятельно пишет Cursor. Сначала согласовать совместимость snapshot/ownership с его владельцем; не добавлять обязательную UI→Move зависимость. `Neo.NoCode` подключать после стабильного typed API, через существующие события. Новые интеграционные PlayMode tests: `Assets/Neoxider/Tests/Play/Tools/Audit20261023NeoToolsMoveLifecycleTests.cs`; ссылки Move/CMF в общем Play asmdef добавляет координатор только при необходимости.
5. Проверить Unity 6.0–6.4 и 6.5+, MIRROR и поддерживаемый standalone fallback, UNITY_EDITOR/ODIN при установленных зависимостях. Не рассеивать version #if: идентификаторы идут через `NeoDiagnostics.StableId`/`NeoComponentHealth.StableId`.
6. Сохранить API, enum values, prefabs и serialized names; новые package-файлы получают уникальные `.meta`. Sample layout не менять: учитывать `Assets/Neoxider/Samples`, `Assets/Neoxider/Samples~`, `Assets/Samples/NeoxiderTools/<version>/<sample>`. Документация — текущие entry points, без DocsEn.

**Завершение:** нет новых compile/console ошибок, обязательные EditMode/PlayMode сценарии пройдены.
Провал harness/версии оставляет результат непроверенным; исправлять минимальную причину.
Чувствительность и ownership contract не менять молча. Новые defaults и межмодульная gate-семантика —
решения roadmap; рутинные исправления дополнительного разрешения не требуют. Unity/build/test процессы не запускались.

# Neo.Tools.Other — индивидуальный план реализации

Фаза 2 того же аудитора. Основание: завершённый `Neo.Tools.Other.md`, актуальные исходники/документация/регрессии, manifest и PLAN-CONTRACT. Ниже разделены готовые изменения, оставшиеся пробелы и предложения. Во время планирования изменяется только этот файл.

## 1. Уже выполнено

Проверены все четыре исходника, 2 121 строка baseline. Исправлены параметры DOTween, unscaled sequence, владение tween при повторных вызовах из событий, восстановление активных каналов, рекурсивный патруль, отмена ожиданий, stop/resume, positional/partial/follow arrival, диагностика и ошибки условной Spine-ветки. `RevertAmount` оставлен с намеренно неограниченным `1 - amount`.

Обновлены пять существующих страниц `Assets/Neoxider/Docs/Tools/Other/`. Добавлены 18 случаев в `Audit20261023NeoToolsOtherCameraShakeTests.cs` и `Audit20261023NeoToolsOtherNavigationTests.cs` под `Assets/Neoxider/Tests/Edit/Tools/Other/` (далее **T/**). UTF-8, лексическая структура, сохранность сигнатур, ссылки, GUID и scoped diff проверены статически. Компиляция и Unity-тесты не выполнялись; исправления O-01–O-12 повторно не планируются как незавершённые.

## 2. Оставшиеся исправления и проверочный долг

**R/** означает `Assets/Neoxider/Scripts/Tools/Other/`, **D/** — `Assets/Neoxider/Docs/Tools/Other/`.

| Приоритет / статус | Свидетельство, владелец и минимальная следующая работа | Наблюдаемая приёмка |
|---|---|---|
| P1, подтверждённый пробел обработки; engine-trigger не проверен | `R/AiNavigation.cs:CheckPathStatus` проверяет `agent.hasPath` до ветки `PathInvalid`: отсутствие пути полностью исключается. Владелец Neo.Tools.Other. Воспроизвести потерю/отказ принятого пути при готовом агенте; учитывать наличие принятого активного запроса и сохранённую destination, затем обрабатывать завершённый invalid/no-path отдельно от idle. Не убирать guard без учёта `pathPending`. | Один `onPathBlocked`, корректный `onPathStatusChanged`, `HasReachedDestination == false`; idle/Stop не создают ложную блокировку. Новый запрос восстанавливает нормальную работу. Регрессия в T/Audit20261023NeoToolsOtherNavigationTests.cs; не подменять native-состояние reflection. |
| P2, подтверждённое дублирование | `SetTarget` вызывает `UpdatePath`, затем `StartCoroutine(PathUpdateRoutine())` немедленно вызывает его снова; параллельно работает `UpdatePathfinding`. Владелец Neo.Tools.Other. Оставить один interval scheduler в Update и один первоначальный запрос; сохранить AutoUpdatePath, pause/disable и явное обновление при включении. | Один принятый initial path/event на `SetTarget`; дальнейшие автоматические обновления не чаще interval, отсутствуют после stop/disable/AutoUpdatePath=false. Движущийся target снова достигается. Файлы: R/AiNavigation.cs, T/Audit20261023NeoToolsOtherNavigationTests.cs, D/AiNavigation.md. |
| P0, долг проверки | Координатор: собрать текущий diff и выполнить обе новые fixtures, существующий `OtherToolsBehaviorTests` и проверки Inspector buttons. Fixtures используют Enter/ExitPlayMode, NavMeshBuilder и реальное время. | Все 18 случаев проходят; teardown возвращает timeScale и удаляет только собственные NavMesh/объекты/tweens; нет новых console errors. Исправлять подтверждённую причину, не ослаблять assertions ради таймингов. |
| P1, условный долг | Координатор + владелец Neo.Tools.Other: проверить `R/SpineController.cs` с поддерживаемым Spine runtime и без него. Сначала определить реальные vendor assemblies; blanket reference в базовом пакете недопустим. Новый T/Audit20261023NeoToolsOtherSpineIntegrationTests.cs — только в реально собираемом integration-профиле. | Stop → PlayDefault запускает idle; неверное имя сохраняет playback; retry initialization успешен; неуспешный skin не меняет state/prefs/event; валидный skin и offsets работают. Без Spine собирается fallback. |

Неподтверждённые P2-риски: NaN/Infinity/невалидный enum в CameraShake; уничтожение объекта внутри start/stop listeners; повёрнутые/зеркальные patrol zones под неоднородным scale; внешняя смена Spine track/skeleton. Сначала добавить реальные воспроизведения. Только при подтверждении исправлять соответствующий R/*.cs и регрессию; для zone candidate — локальный случайный point через `TransformPoint`, с последующей проверкой результата NavMesh sampling. Новый контракт числовых входов не переносить молча на legacy API.

## 3. Существенное улучшение: управляемый запрос procedural shake

**Предложение, не реализовано.** Нужны воспроизводимый seed, ручное продвижение времени, завершение отдельного импульса и независимость камеры от DOTween/другого Transform writer.

Уточнение первоначального предложения после проверки reuse: `Neo.Animations.TransformAnimationEvaluator.Evaluate(settings, basePosition, baseEulerAngles, baseScale, time, randomSeed, impulseTime, impulseStrength)` уже является чистым sampler. Есть `TransformAnimationSettings`, `TransformAnimationState` и `TransformAnimator.Shake(float)`. Последний владеет собственными часами/seed и не сообщает завершение отдельного импульса. Новый генератор шума или второй TransformAnimator не нужен.

Предлагаемый узкий API:

```csharp
// Immutable validated request: duration, axis strengths, noise speed/seed, fade policy.
public bool TryStart(ShakeRequest request);
public bool Tick(float deltaTime); // True only on active -> completed transition.
public void Stop();
public bool IsActive { get; }
public Vector3 PositionOffset { get; }
public Vector3 RotationOffset { get; }
```

Request отвергает нечисловые/отрицательные параметры до замены активного запроса; invalid delta оставляет состояние неизменным. State хранит только snapshot настроек и elapsed time, использует существующий evaluator с отключёнными rotate/bob/scale/continuous-shake каналами и маскирует оси. Завершение/Stop обнуляют offsets; общий Unity Random не используется. Детерминизм гарантируется внутри поддерживаемой версии/платформы, без обещания побитового совпадения Perlin между платформами.

### Точные файлы и границы будущей реализации

| Файлы | Владелец / назначение |
|---|---|
| `Assets/Neoxider/Scripts/Animations/ShakeRequest.cs`, `ImpulseShakeState.cs` | Neo.Animations (`neo-audit-1023-b02-02`): request/lifetime поверх существующего evaluator. Не менять его sampling без необходимости. |
| `R/Procedural/ProceduralCameraShake.cs` | Новый опциональный адаптер: `Initialize(Transform pivot)`, `TryStart(ShakeRequest)`, Inspector Start/Stop, scaled/unscaled Update и UnityEvents. Владение этой новой областью назначает координатор. |
| `R/Procedural/Neo.Tools.Other.Procedural.asmdef` | Координатор: отдельный bridge к Neo.Animations и PropertyAttribute; базовый Neo.Tools.Other не получает обязательную зависимость на animations. |
| `T/Audit20261023NeoToolsOtherImpulseShakeStateTests.cs`, `T/Audit20261023NeoToolsOtherProceduralCameraShakeTests.cs` | Чистые state-тесты и реальные lifecycle/pivot tests; coordinator добавляет bridge reference существующей Editor test assembly. |
| `D/ProceduralCameraShake.md`, `D/README.md`, `D/CameraShake.md` | C# пример ручного Tick, scene setup с отдельным child pivot, events/cancellation, выбор существующего DOTween или нового procedural компонента. |

Адаптер явно получает scene pivot; до его назначения Start отклоняется. Только адаптер хранит Transform. Он восстанавливает собственный pivot, не трогает движущийся parent, меняет target через stop/restore и защищает events поколением запроса. Новых SO/editor builders не требуется; профиль данных отложен до реального повторного использования.

Совместимость: существующий CameraShake, waveform DOTween, GUID, serialized fields, кнопки, defaults и callbacks сохраняются; новый компонент подключается явно. Новые файлы получают уникальные metas, metadata папок/asmdefs ведёт координатор. Сцены/префабы автоматически не мигрировать. Release `Samples~` не переименовывать; будущие sample checks поддерживают также developer `Samples` и imported `Assets/Samples/NeoxiderTools/<version>/<sample>`.

Оценка: 3–5 рабочих дней после согласования границ с Neo.Animations; риск средний — композиция pose и поведение событий. Neo.NPC (`neo-audit-1023-b04-04`) предоставляет NpcNavigation/NpcFollowTargetCore для новых игр, но добавлять его зависимости Network/Save/Rpg в legacy Other не следует. Neo.Extensions остаётся источником diagnostics/stable identity.

## 4. Порядок, приёмка и остановка

1. После завершения source-changing аудитов координатор фиксирует проверяемый snapshot и закрывает P0 compile/test debt; затем локальные P1/P2 corrections с новыми регрессиями и повтором затронутых проверок.
2. Neo.Animations реализует request/state: одинаковый seed/delta даёт одинаковые offsets; независимые экземпляры, маски осей, нулевая амплитуда, overshoot completion, invalid inputs и Stop проверяются без GameObject.
3. После стабилизации API — optional bridge и PlayMode: движущийся parent сохраняется; pause/unscaled, callback replacement, disable/destroy, смена pivot; completion ровно один, cancel completion не вызывает. Затем обе public workflows в документации.
4. Координатор проверяет стандартный и optional profiles в Unity 6.0–6.4 и 6.5+, обе ветки централизованного StableId, отсутствие missing references, serialized compatibility и UPM layout. Новых scattered `#if` не вводить.

Нельзя отмечать Unity/Spine работу проверенной по статике. Если native no-path trigger не воспроизводится, оставить этот сценарий как проверочный долг с точным состоянием, без выдуманной регрессии. До назначения optional bridge/Animations ownership остановить только эту предлагаемую фичу; routine fixes отдельного согласования не требуют. Разделение optional компонента и объём заявляемого детерминизма — решения для координатора перед реализацией предложения.

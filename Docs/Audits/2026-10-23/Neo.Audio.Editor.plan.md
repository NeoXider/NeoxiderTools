# Neo.Audio.Editor — индивидуальный план реализации

Дата: 2026-10-23. Прочитаны PLAN-CONTRACT, завершённый аудит, manifest entry и актуальный код.
В фазе 2 изменён только этот план. Та же сессия: `gpt-6.1-sol`, reasoning `max`.

## Текущее состояние

Все три исходника проверены. F1–F3 уже исправлены: новые clip slots очищают осиротевшие trims;
id `<none>` отделён от пустого выбора; отсутствие AM кэшируется на секунду. Добавлены явные типы,
семь методов / двенадцать регрессионных случаев и `Assets/Neoxider/Docs/Audio/Editor.md` для C# и
scene/UnityEvent workflows. Повторять эти исправления не требуется.

Статически проверены UTF-8, whitespace, скобки, API/serialized-имена, GUID, ссылки и diff.
Unity import, компиляция, тесты, IMGUI и Undo/Redo не выполнялись. Текущий код сохраняет исправления;
runtime `AudioEntry.Clips` по-прежнему не переносит индексированные trims при замене массива.

## Приоритеты и оставшаяся проверка

Подтверждённых неисправленных дефектов внутри assembly нет. Ниже — долг проверки и предложения.

| Приоритет / владелец | Основание и минимальное действие | Наблюдаемый результат |
|---|---|---|
| P1, координатор | После source-changing аудитов: import/compile в Unity 6.0–6.4 и 6.5+, затем `Audit20261023NeoAudioEditorTests`. Reflection bindings, строки 24–34, ещё не исполнялись. | Обнаружены и проходят 12 случаев; новых compile/import errors нет. При отказе — минимальная коррекция конкретной причины. |
| P2, Neo.Audio.Editor + координатор | `AudioEntryDrawer.cs:161,308,412,478`: реально добавить C через header picker, **+**, drop после сокращения A/B с trim B=0; проверить list import и замену slot. | Trim C=1, trim A сохранён; замена сохраняет trim; Undo/Redo совместно восстанавливает clips/trims и prefab override. |
| P2, Neo.Audio.Editor + координатор | `AudioIdDrawer.cs:52–59`: реально выбрать id `<none>`, пустой пункт, missing id и id с suffix. | Serialized string содержит исходный id/empty; suffix не записывается, missing value переживает repaint/Undo. |
| P2, Neo.Audio.Editor + координатор | `AudioIdDrawer.cs:96–106`: добавить/удалить AM после cached miss; проверить Play Mode без domain reload. Неподтверждённый lifecycle-риск. | Без AM доступен text field; новый AM виден за секунду; уничтоженная ссылка безопасна. |
| P3, Neo.Audio / координатор | Ссылка на Editor topic отсутствует в `Audio/README.md` и `AM.md`. | Канонический вход Audio ведёт на `Editor.md`. |

Замена/reorder `Clips` принадлежит Neo.Audio; миграция trims в setter — отдельное изменение контракта.

## Существенное улучшение: явный контекст каталога и модель выбора

**Предложение P2, не реализовано.** Глобальный `FindManager`, пересоздание списка `CollectIds` при
каждом draw и private option helpers затрудняют prefab authoring, работу с несколькими AM и тестирование.

Переиспользовать `AM.SoundEntries`, `MusicEntries`, `AudioIdKind`, ordinal ids, `SerializedProperty`,
`CustomEditorBase`. `AM.TryGetInstance(out AM)` из `Neo.Tools.Managers/Singleton.cs:144` читает
назначенную instance без lookup; использовать его в Play Mode вместо обращения к `AM.I` из drawer.
Новые runtime singletons, NoCode bridges и SO-каталоги не нужны.

Предлагаемый публичный API в namespace `Neo.Audio.Editor`:

- `AudioIdOptions.Build(IReadOnlyList<string> ids, string currentId)` создаёт immutable snapshot.
  `Items`, `SelectedIndex` и `TryGetId(int optionIndex, out string id)` отделяют value от label.
  `AudioIdOption` содержит `Value`, `Label`, `IsEmpty`, `IsMissing`. Пустые ids исключаются,
  дубликаты удаляются ordinal-сравнением с сохранением порядка. Missing/out-of-range selection
  возвращает false; пустой пункт возвращает true и empty string. Labels имеют разные категории
  для empty/configured/missing, включая ids с буквальным suffix.
- `AudioAuthoringContext.Resolve(UnityEngine.Object target, AM explicitManager, out AM manager)`
  возвращает `AudioManagerResolution` (`Found`, `Missing`, `Ambiguous`). `Invalidate()` сбрасывает
  snapshots после явных изменений через C#.

Resolution: explicit manager; target-AM; менеджер prefab stage; для scene target — назначенный
singleton в Play Mode либо единственный loaded AM. Иначе text field/hint. Prefab asset не получает
каталог основной сцены. Параметр custom editor передаётся явно, без reflection-поиска поля `_manager`.

Уточнение audit proposal: `PlayAudio.AudioPlay` и `MusicControl.PlayPool` вызывают глобальный `AM.I`.
Scene-каталог не меняет runtime routing. Несколько AM без явного контекста означают `Ambiguous`.
Multi-manager runtime и новые component references требуют отдельной работы Neo.Audio.

Кэш scene/stage включает misses. Invalidation: hierarchy changes, Undo/Redo, scene close, Play Mode,
assembly reload; TTL ≤1 секунды страхует изменения через C# без notification. Подписки однократные,
ссылки закрытого контекста освобождаются. Ключи — `Scene.handle` и context references. Уточнение:
`NeoComponentHealth.StableId` в `Editor/PropertyAttribute/Internal/NeoComponentHealth.cs:143` private,
класс internal; прямой вызов из другой assembly невозможен. Object-id здесь не нужен; при
необходимости доступный общий adapter сначала предоставляет Neo.Editor, без копирования `#if`.

### Точные файлы и границы

| Изменения последующей реализации | Владелец |
|---|---|
| Новые `Assets/Neoxider/Scripts/Audio/Editor/AudioIdOptions.cs` и `AudioAuthoringContext.cs`, их `.meta`; adapter в существующем `AudioIdDrawer.cs` | Neo.Audio.Editor |
| Существующий `Assets/Neoxider/Tests/Edit/Audio/Audit20261023NeoAudioEditorTests.cs`; новые `Audit20261023NeoAudioEditorOptionsTests.cs` и `Audit20261023NeoAudioEditorContextTests.cs` в той же папке, с `.meta` | Neo.Audio.Editor; сохранить восемь clip/trim случаев, заменить старые option/cache bindings при замене реализации |
| `Assets/Neoxider/Docs/Audio/Editor.md` | Neo.Audio.Editor: публичный C# editor API, scene/prefab fallback, явный manager и UnityEvents |
| `Assets/Neoxider/Tests/Edit/Neo.Editor.Tests.asmdef`: reference на `Neo.Audio.Editor` для прямых tests нового API | Только координатор, до добавления typed tests; текущим 12 случаям reference не нужен |
| `Assets/Neoxider/Docs/Audio/README.md`, при необходимости `AM.md` | Neo.Audio (`neo-audit-1023-b02-03`): ссылка и согласование singleton semantics |

`Neo.Tools.Managers` (`neo-audit-1023-b07-06`) поставляет существующий side-effect-free accessor;
`Neo.Editor` (`neo-audit-1023-b03-03`) — base inspector и при необходимости общий compatibility
adapter. Изменять эти сборки без координации не требуется.

## Порядок, приёмка и остановка

1. Закрыть P1/P2 проверку уже внесённых исправлений; сначала устранить только воспроизведённые
   regressions. Сохранить доказательства source revision и console результата.
2. Реализовать pure options API и прямые EditMode tests после coordinator-owned reference. Затем
   context resolver/cache и тесты explicit manager, singleton, ambiguity, prefab isolation, missing
   id и invalidation. Только после этого подключить drawer.
3. Проверить два additive contexts с разными explicit AM: каждый получает свои ids; без явного
   контекста при неоднозначности остаётся ручной ввод. Закрытие сцены удаляет её cached references.
   Профилированием 100 полей подтвердить ограниченный lookup на context/TTL, а не на каждый repaint.
4. Обновить C# и scene/NoCode документацию, связать module entry через Neo.Audio. Сохранить все
   serialized имена, значения ids, runtime API и старые assets; никаких автоматических scene edits.
5. Координатор компилирует обе Unity API-семьи, запускает focused EditMode suite и короткий Play Mode
   smoke для статуса/Next Track и cache lifecycle с domain reload on/off. Общие version branches
   проверяются реальными компиляторами; статическая проверка ветку не подтверждает.
6. Проверить уникальные `.meta`, UTF-8 и scoped diff; новые файлы остаются Editor-only. Samples не
   менять: release root `Samples~`, developer root `Samples`, imported root
   `Assets/Samples/NeoxiderTools/<version>/<sample name>` не становятся зависимостью editor API.

Оценка улучшения: 2–3 developer days плюс доступ к двум Unity версиям; основной риск — stage lifecycle
и invalidation. Готовность означает реальные passing checks и наблюдаемое сохранение строк/trim,
Undo и fallback. При падении проверки исправляется её конкретная причина; при недоступной версии
или GUI проверка остаётся явно unverified, релизная готовность не заявляется. Multi-AM runtime
routing остаётся отдельным продуктовым решением. В этой фазе никакие Unity/build/test процессы
не запускались и реализация предложения не начиналась.

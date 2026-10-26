# Итоговая интеграция 11.0.0 в demo — 2026-10-27

Актуальная сводка: все 72 первоначальных аудита и плана прочитаны root, исправления и независимые повторные ревью завершены. История ниже сохранена с исходными неудачными прогонами; её ранние pending-статусы не являются состоянием последнего дерева. Канонические результаты и ограничения: [Verification11](../../../Assets/Neoxider/Verification11.md).

- Unity 6000.3.14f1 и 6000.6.0f1: по 4596 положительных Edit и 726 Play случаев в полных базовых прогонах; контекстные cursor/end-of-frame проверки отдельно выполнены в видимых редакторах. Это квалифицированное покрытие пересекающихся групп, а не новый общий прогон без ошибок.
- На каждой SDK: Mirror-absent 6, NewOnly 17 Edit + 93 Play с настоящим указателем; настоящие три UPM sample import, 28 Edit P30 и 20 Play P31 без DOTween Pro.
- P23: три отдельных процесса Windows server/client A/client B прошли транспортную матрицу, позднее подключение, reconnect и restart. Результаты не подменялись Host loopback.
- CardGame: 149 Cards Edit P27 и 46 Cards/sample Play P29 прошли на обеих SDK. P31 текущие sample UI 10/10 на каждой SDK, включая настоящий EventSystem raycast и ровно один раунд от клика. Root вручную сыграл три раунда, проверил Free Aspect, 16:9 и портрет; исправлены Canvas scale, отступы и перехват клика обучением. Последняя правка ограничивает декоративного персонажа правым краем, не меняя runtime и настройки карт.
- Ручные проверки охватили 11 выбранных сцен; сценарии Inventory UID/use, Abilities, Save, Quest, TicTacToe и restart Survivor подтверждены. Dice OS drag/drop и качество слышимого Audio не подтверждены — ограничения сохранены в каноническом отчёте.
- Обновлены Mirror 96.11.3, UniTask 2.5.11, Unity dependencies и MCP 10.3.0. DOTween остаётся на совместимой лицензированной паре Core/Pro; для нового Core нужен доступный владельцу новый Pro. Обновлены RU/EN entry points, migration, README и пять явно помеченных concept renders.

Финальная приёмка P32 выполнена: sample-сцены **10/10** на каждой SDK, exit0 без skipped; `check_package.py` — **5856 файлов / 71 сборка / 3191 GUID / 0 ошибок**; строгий UTF-8 — **9139** текстовых файлов; новые нарушения explicit-type policy — 0; `git diff --check` — exit0. Все **10547** frozen inputs и текущее product-дерево проверены по SHA-256. Единственная недокументальная дельта от P31 — шесть YAML blocks декоративного персонажа. Независимый ревьюер подтвердил отсутствие изменений карт, input, sprite/GUID и обработчиков.

Root повторно открыл именно сохранённую сцену при 1080×1920, настоящим кликом сыграл ещё один раунд (18/18 → 17/17 → player17/bot19): колоды целиком видимы, персонаж их не закрывает. В Audio настоящими кликами Random music → Stop подтверждены состояние и переключатель ON → OFF.

P32 source manifest SHA-256: `30c3114416afb2404a011447e49764eec838294e182d717de10b97651ddde70b`; scene SHA-256: `b2eb4e86c473628477383f38d5889765e3e8bacd7d0f1e7891959bc040f2bd65`. Raw XML SHA: 6.3 `eede01ff18c3a56646974ba919f11032543729216a52074f907eae989148d72f`, 6.6 `5d5327b10ce5ad0bf3329d1d8ae0d0050998fbbaca379abcdf6fd44a8b30bf52`. Локальные receipts: `Local/Audits/assembly-2026-10-23/unity/final/p32/final-acceptance.json`, `source-identity-check.json`, `manual-card-saved-portrait.json` и `manual-audio-toggle.json`. .NET: 2852 Passed / 30 NotExecuted квалифицированы по точным unchanged source/dependency/TRX inputs, нового запуска не заявлено.

Проверочные редакторы и source workers завершены. Пользователь разрешил commit и normal push в `origin/demo`; фактический опубликованный commit определяется Git refs, а не обещанием в аудите. Пользовательский `Assets/Neoxider/DocsEn.meta` не изменён и исключён из индекса; `._count` сохранён. Удаление временных клонов было отклонено автоматической проверкой; обратимое NTFS-сжатие собственных временных Library освободило минимум **9.82 GiB**, сохранив файлы и доказательства. Установленные пользовательские Unity и сторонние проекты не удалялись. Финальная проверка staged diff сохраняет 411 upstream whitespace diagnostics в официально импортированных файлах Mirror (в основном пустые YAML-поля); vendor-исходники не переформатировались ради этой проверки. Восемь лишних EOF-строк собственных audit MD удалены; scoped whitespace check всего собственного дерева вне Mirror должен быть чистым.

---

## История интеграции

# Интеграция 11.0.0 в demo

Статус на 2026-10-25: повторные source-исправления после Source3 проверены нативными ревьюерами и root; текущие отчёты всех первоначальных владельцев прочитаны. Итоговая приёмка исправленного дерева и push ещё не выполнены. Два настоящих Unity 6.3 запуска завершили read-only диагностику всех 314 целевых ресурсов, выявив также новые ошибки импорта sample-prefabs и USS. Эти ошибки возвращены прежним владельцам; доработки продолжаются.

| Актуальный этап | Наблюдаемое состояние |
| --- | --- |
| Source3, Unity 6.3, graphical Edit | 4539 случаев: 4196 passed, 341 failed, 2 skipped. Это результаты прежнего снимка, а не исправленного дерева. |
| Source3, Unity 6.3, graphical Play | 637 случаев: 423 passed, 212 failed, 2 skipped. |
| Source3, Unity 6.3/6.6 SDK | Компиляция и непустое обнаружение выполнены для прежнего снимка. Настоящий Mirror-absent запуск выявил 30 test diagnostics; последующие guards ещё требуют нового запуска. |
| Исправления после Source3 | Повторные ревью NI38–NI56 сохраняют точные исходные assertions и различают public API, реальные native callbacks и неподтверждённые возможности среды. Текущие hashes/read receipts находятся в reviews.json и локальном журнале root. |
| Test-only runtime fixtures | 54 helper-типа перенесены в две runtime-compatible test assemblies с сохранением записанных GUID/body mappings; NI41/NI47 проверили границу и вызывающие тесты. Это source evidence, не новый native pass. |
| README и migration | README-владелец завершил финальную вычитку; root прочитал все 213 строк README и текущий отчёт. Сохранён статус 11.0.0 preparation / UNVERIFIED. |
| Native asset diagnostics | Canonical PID39040 естественно завершился exit2: 232 записи, 231 complete и известная неполная ссылка Icosphere. Legacy PID38568 завершился exit0: 82 complete. Ни записи схем, ни round-trip, ни main copyback не было. Все 17755 seed-файлов, 541 архивный и 628 исходных target asset/meta-файлов неизменны. |
| Новые native import findings | Семь NeoxiderPages example-prefabs содержат broken local PPtr/component entries; в sample остались Pro-only компоненты. AbilityDesigner.uss использует неподдерживаемый `:last-child`. Эти source-доработки выполняют прежние владельцы Managers и Abilities.Editor. |
| Следующая metadata-приёмка | Raw `.m_FileID` children меняются между процессами при одинаковых persistent parent identities. Typed semantic projection и единственный dangling vendor remap требуют отдельной source/native проверки. Старые `Assets/Samples/CharacterMovementFundamentals` служили сравнительной диагностикой; автоматический copyback в старый импорт не разрешён. |

Полные результаты диагностики: `Local/Audits/assembly-2026-10-23/unity/maintenance/ba515785480c4d2c9c791b2b91cc0e82/diagnostic-results.md`, SHA256 `6203de2d932516de02b415c6c700d1015f9378f02cbb81e635db94e089a6dbd6`. Passing operator-only probes подтверждают исправление PowerShell scope/exit propagation, но не являются Unity/product/schema acceptance.

Ниже сохранена история интеграции до Source3. Формулировки «проверка впереди» относятся к соответствующим прежним source-снимкам; они не отменяют реальные failed runs и последующие текущие исправления.

| Зависимость | Состояние исходников и основание |
| --- | --- |
| Abilities → Core CanSpend и принятая дельта | API реализован; NI4 повторно проверил исправление NI2. Положительное списание без изменения float-баланса отклоняется. Отчёт Abilities, написанный раньше NI4, сохраняет историческую отметку OPEN. Выполнение тестов впереди. |
| Core Level.Data → родительское руководство | Ссылка `Level/Data/CurveEvaluation.md` добавлена владельцем Core, отражена в его финальном отчёте. |
| Core Level.Bridge → канонический вход | Root добавил ссылку `Level/Bridge/README.md` в Core README после чтения финального отчёта Bridge. |
| Network.Contracts → Network | NI7 подтверждает отклонение неизвестных политик до доверенных исключений и ссылку на Contracts из Network README. Старое ожидание в отчёте Contracts закрыто в исходниках. Дополнительные замечания NI7 по лобби и совместимости тестов возвращены тому же владельцу. |
| Extensions UIUtils → Input | Координатные перегрузки и верхний UIInputCompat опубликованы; руководства Extensions согласованы с точными сигнатурами. Root добавил явную зависимость UnityEngine.UI в Input asmdef. Проверка New-only в Unity впереди. |
| Cards Image-регрессии → общая Edit assembly | Root добавил явную ссылку UnityEngine.UI в Neo.Editor.Tests.asmdef. Это закрывает исходную зависимость из финального отчёта Cards; компиляция ещё не выполнена. |
| Components TextScore → общая Play assembly | Root добавил Neo.Tools.Text в Neo.Tests.Play.asmdef для фактических derived-label regression sources. Наличие ссылки закрывает исходную зависимость; компиляция и lifecycle проверки впереди. |
| Grid lifecycle → общая Play assembly | Root добавил Neo.GridSystem для нового типизированного lifecycle fixture; это закрывает handoff из отчёта Grid. Компиляция и реальные coroutine/cascade результаты ещё не получены. |
| Move cursor lifecycle → общая Play assembly | Root добавил Neo.Tools.Move для нового typed Play fixture с native disable/destroy. Это закрывает точную зависимость I15; компиляция, обнаружение и 40 внутренних native сценариев ещё впереди. |
| Input I11-01 → незавершённый Release | Root прочитал исправленный release block, все четыре новые fault/normal/replacement регрессии и руководство. Замечание закрыто по исходникам в RootImplementationReview-I11.md; 76 объявленных случаев ещё не запускались. |
| StateMachine → legacy duration tests | Два post-entry вызова заменены владельцем общих тестов на machine.SetCurrentStateEnterTime; NI13 подтверждает наличие. Новая Inspector-настройка не требуется. E13-01 добавляет отдельные проверки уже реализованного failed-constructor guard. |
| Time UTC hook → Bonus | CooldownReward.GetUtcNow возвращает NeoTrustedTime.UtcNow; NI16/NI17 подтверждают исходники и сохранённые deterministic controls. Root прочитал окончательный отчёт Bonus; реальные save/reopen/deadline проверки впереди. |
| Spawner retirement/cleanup → Draw и Demo | NI14 подтверждает исправление первого prewarm callback и фактическое использование Draw: точное поколение аренды, освобождение собственного материала и удаление только своего источника. Root прочитал весь отчёт; native/material/100-cycle проверки впереди. Demo остаётся отдельным потребителем. |
| Money NI7-02 → покупки и сеть | NI14 подтверждает guard положительного списания без изменения float-баланса и сохранение client-first pending пути. Источники отдельных peer-сценариев дополняет тот же сетевой исполнитель. Все runtime результаты впереди. |
| Selector/Counter → окончательная peer-оснастка | View и Components поставили sender-aware admission и finite raw-candidate guard. Root прочитал окончательные отчёты; NI26 и NI31 проверили оснастку и исправления её ожиданий. Весь текущий сетевой отчёт прочитан из трёх фрагментов. Отдельные процессы и выполнение всех проверок ещё впереди. |
| Bonus → сохранение прогресса и цена | NI21 и root подтверждают private write revision после Save callback и положительное фактическое списание до выдачи. Добавлены девять negative/ordinary/rounded/zero controls; исходное RootBoxPrecisionFinding закрыто по исходникам. |
| HtmlToUitk → редактор и тестирование | Канонический HtmlToUitk.md теперь ссылается на HtmlToUitkEditor.md и HtmlToUitkTesting.md. Ранняя отметка отсутствующей ссылки в отчёте Tests.Editor историческая; root прочитал более поздний runtime-отчёт. |
| RPG → Network и ConditionBridge | NI18 подтверждает все три исправления NI9, command23, snapshot/replay и active-controller fixture. Первичной сетевой ветке уточняется только формулировка доказательства: facade completion отличается от изолированной проверки core notification. |
| Cards → native hierarchy callbacks | NI27 и полный root-read подтверждают исправление NI21: принятие model, освобождение deck cleanup ownership, SetParent и затем публичное уведомление. Четыре новых hierarchy callback controls сохраняют предыдущие случаи; все 87 объявленных случаев ещё не запускались. |
| Progression и Grid → повторное ревью | NI28 и полный root-read подтверждают retry незавершённой инициализации с сохранением committed state, nested first occupancy и устранение повторного occupied после удаления вложенного blocker. Все новые failure/ordinary/native controls прочитаны; выполнение впереди. |
| Audio/NPC → независимая проверка | Root полностью прочитал окончательное NI22: все изменённые runtime-файлы, fixtures и guides получили ограниченное одобрение исходников без новых подтверждённых дефектов. Native audio/NavMesh и demo consumer приёмка ещё не выполнены. |
| Physics → уничтожение emitter из callback | Root проследил обращения к gameObject после немедленного удаления emitter. Тот же автор добавил guards и 15 force/event controls; NI28 подтверждает исходники. Точный старый исход Magnetic native getter остаётся непроверенным. См. RootPhysicsEmitterReview.md. |
| Html.Editor → сохранение только своего asset | NI28 подтверждает SaveAssetIfDirty и три persisted own/foreign-dirty controls. Сохранение одного job не вызывает глобальный SaveAssets. Native serialization/reimport и modal flow ещё не запускались. |
| Demo → окончательные runtime-контракты | NI27 проверил все изменённые consumer-файлы, девять случаев и provider-контракты: точная аренда pool, условный Quest Unregister, владение Audio и Context assets. Замечаний по исходникам не осталось; сценовая приёмка впереди. |
| Parallax → представимость локальных координат | Независимое ревью выявило переполнение при конечном world period. Прежний владелец добавил widened pitch/centering и проверку крайних координат до создания tiles; NI32 и root подтверждают исправление. Предыдущие 31 случая сохранены, добавлены 10. Native float/renderer и обычное движение ещё не проверены. |
| Other Navigation → наблюдение прибытия перед refresh | NI33 и root подтверждают исправление starvation при refresh каждый кадр: предыдущий settled request проверяется до новой отправки, guards прекращают устаревший хвост после callback. Пять реальных PlayerLoop controls добавлены к прежним 19; восемь CameraShake сохранены. Native cadence/arrival ещё не проверены. |
| Network Context → dedicated и reconnect | NI31 подтверждает реальный physics identity barrier и отдельные ожидания для dedicated server, текущих клиентов и свежего B после reconnect. Полные value/count/persistence проверки сохранены. Комментарий runtime исправлен без изменения поведения; сетевые процессы ещё не запускались. |
| Обнаружение тестов → Assets и UPM | NI34 и root подтверждают исправление NI30: ProjectAssets provenance допускается только без зарегистрированного Neo и с настоящим корректным manifest 11.0.0. Ошибочная зарегистрированная версия отклоняется; настоящий UPM import остаётся строгим. Предыдущие async/graph/tree/guards сохранены, добавлены 18 admission controls. Реальные оба профиля ещё предстоит выполнить. |
| Первый frozen compile → точечные исправления | NI35/36 подтверждают namespace demo cleanup, точные attribute qualifiers, реальную dirty/Discard подготовку Html fixtures, aliases PackageInfo во всех трёх helpers и прямую Neo.Extensions reference общей Play assembly. Все прежние поведенческие проверки сохранены; повторная компиляция ожидает нового снимка. |
| Metadata → нативная совместимость | 13 старых файлов исправлены точными canonical slices, один новый Level GUID стал валидным. Отдельный Unity 6.3 maintenance завершился exit0: все шесть vendor importer metadata schema1→3, native SDK/full JSON state, GUID и DLL SHA совпадают до/после. Root перенёс только шесть `.meta`; это ещё не результат полного Neo import в 6.6. |
| Второй frozen compile → оставшиеся test API contracts | NI37 и root подтверждают World initializers Abilities, полноценные collection constraints, Scene value identity без implicit int, canonical RPG resource namespace и прямую Mirror.Components reference общей Edit assembly. Существующее serialized событие Progression теперь доступно из C# через OnProfileLoaded; invocation не менялся. Все прежние oracles сохранены. Второй compile остался историческим FAIL; новая сборка ожидает третьего снимка. |

Все исполнители использовали прежние сессии, `gpt-6.1-sol` с reasoning `max`; одновременно допускалось до 15 субагентов. Root прочитал все 72 первоначальных аудита, все 72 плана, текущие отчёты реализации, шесть первоначальных native review, повторные implementation reviews и три дополнительных независимых отчёта. История source closure не означает успешную приёмку исправленного дерева; новые native findings также должны быть закрыты.

После заморозки нужны свежие результаты Unity 6.3/6.6, непустое обнаружение Edit/Play тестов, реальные физические запросы для 1/100/1000 интерактивных потребителей, отдельные сетевые процессы, выборочные ручные сценарии и проверки упаковки/документации. Публикация коммита в `origin/demo` разрешена пользователем после этих проверок; текущего успешного результата сборки или push этот документ не объявляет.


## 2026-10-26: updated Mirror delta fixture context

P18 native Unity 6.3 targeted run: 116 cases, 115 passed, one failed. The remaining
`DeltaSnapshot_RespectsCharacterNetworkingOptOut(true, 75)` case had not completed
initial client spawning. Mirror 96.11.3 defers SyncVar hooks until that protocol phase
finishes. Root preserved the serialized snapshot, consumed-byte and both opt-out/HP
oracles. The fixture now completes connection/authentication through the public
transport callback, then delivers packed ObjectSpawnStarted/ObjectSpawnFinished
messages in a real Mirror batch through OnClientDataReceived. No private SDK flags,
private generated receive methods or production behavior were changed. This remains
a local transport-boundary fixture; independent OS-peer acceptance is still pending.
P18 raw failures remain intact. Fresh native execution of this correction is pending.

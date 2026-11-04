# Cards F01/F02 — independent native execution review

Дата: 2026-11-02. Verdict: **ACCEPTED для Unity 6000.3.14f1 Editor PlayMode**. Блокеров в проверенных F01/F02 не обнаружено. Это самостоятельная проверка фактических red/green артефактов и production diff; предыдущие source-only verdict не использованы как замена исполнения.

Проверен текущий CURRENT CONTINUATION POINT в [ROOT-HANDOFF.md](D:/Temp/Neoxider-LibraryClosure-20261102/ROOT-HANDOFF.md). Workspace: D:/Git/NeoxiderTools, demo HEAD `3e795e8c6f4d1c89e3fac5f83a79d7c02d988831`; идентичность независимо прочитана из Git-файлов и совпадает с обоими frozen manifests. Reviewer не запускал Unity, compiler, build, Player, CLI или Git и не менял product/test/config/asset. Единственная запись reviewer — этот отчёт. Unity запускал root.

| Run | Реальный результат | Native exit | Завершение UTC |
|---|---|---|---|
| cards-native-u63-red-r2 | 6 Failed, 0 Passed, 0 skipped/inconclusive | 2 | 2026-11-02T07:49:25.8781032Z |
| cards-native-u63-green-r1 | 33 Passed, 0 Failed/skipped/inconclusive | 0 | 2026-11-02T07:51:11.3757586Z |

Оба launch receipts указывают Unity 6000.3.14f1, Neo.Tests.Play, PlayMode, graphics D3D11 batch без nographics, minimum commit guard 6 GiB. Red PID 27576; green PID 12920. ExitCode и завершение подтверждаются также editor.log (red строка 600, green строка 733). Проверены все XML leaves и их bodies, точное соответствие summary/index, SHA/размер launch.json, results.xml и editor.log. Не найдено compiler error CS, crash/OOM marker в этих двух логах по соответствующим диагностическим шаблонам. Старый cards-policy-physics-u63-red-r1 с bootstrap OOM и нулём случаев не является red-доказательством F01/F02 и в результат не включён.

Fixture prefix для следующих шести полных identities: `Neo.Tests.Play.Audit20261023NeoCardsLifecycleTests.`.

| Case suffix | Red | Green |
|---|---|---|
| NativeBoardParentChanged_Clear_DoesNotResurrectAdmission | Failed, Count 0/1 | Passed |
| NativeBoardChildrenChanged_Clear_DoesNotResurrectAdmission | Failed, Count 0/1 | Passed |
| NativeBoardParentChanged_ClearAndReparent_PreservesReleasedCard | Failed, Count 0/1 | Passed |
| NativeBoardChildrenChanged_ClearAndReparent_PreservesReleasedCard | Failed, Count 0/1 | Passed |
| NativeInitialBuildParentChanged_RestartPreservesReplacementSetup | Failed, replacement Board Count 0/1 | Passed |
| NativeInitialBuildChildrenChanged_RestartPreservesReplacementSetup | Failed, replacement Board Count 0/1 | Passed |

Здесь 0/1 означает expected 0 / actual 1. Четыре Board failure находятся на fixture строке 264. До них прошли assertions hierarchyCallbacks == 1 и clears == 1 (262–263), то есть реальный одноразовый native callback и Clear были достигнуты. Assertion membersAtCallback == 1 находится позже (266), поэтому его red-прохождение не утверждается. Два Drunkard failure находятся на строке 594; до них прошли hierarchyCallbacks == 1, completionsInsideCallback == 1, draws == 5 и replacementDraws.Count == 4 (590–593). Следовательно, замена уже завершилась внутри native parenting callback до ошибочного продолжения старой операции. Более поздние проверки композиции red не достигались; их подтверждает полный green.

[HierarchyObserver](D:/Git/NeoxiderTools/Assets/Neoxider/Tests/Play/Audit20261023NeoCardsHierarchyObserver.cs) вызывает одноразовые callbacks только из Unity OnTransformParentChanged / OnTransformChildrenChanged; тесты не вызывают эти методы вручную. В green Board проверяет пустой список, отсутствие устаревших placement/full/move events, отсутствие face override и позднего движения освобождённой карты, foreign parent/position/rotation/scale, deferred native destruction для Clear, затем обычное размещение и capacity rejection. Drunkard green проверяет точные replacement model/view/hand/board составы и события, deferred destruction старой карты, отсутствие dead member, один завершённый restart, а затем настоящий PlayRound с сохранением четырёх карт.

Production diff прочитан полностью:

- **F01**, [BoardComponent.PlaceCardAsync](D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Cards/Components/BoardComponent.cs:130): _cards и существующий membership token коммитятся перед SetParent. Clear/ClearAndReturn из native hierarchy callback теперь видят входящую карту и отменяют именно это владение. Существующий IsCurrentMember повторно проверяется сразу после SetParent и после face override, прежде чем layout сможет продолжиться. Старые post-layout placement/full guards сохранены. Capacity/null admission, порядок, публичные API и serialized fields не менялись.
- **F02**, [DrunkardGame.BuildInitialCardsAsync](D:/Git/NeoxiderTools/Assets/Neoxider/Scripts/Cards/Drunkard/DrunkardGame.cs:1087): единственная добавленная строка EnsureCurrent(operation) немедленно после SetParent закрывает native callback boundary перед _initialBoard и allCards writes. Существующие post-placement guard, cancellation/generation contract, catch и generation-owned finally сохранены.

Обе правки используют уже существующие membership/generation API; новых зависимостей и параллельных ownership-систем нет.

| Source | Red SHA-256 | Green/current/candidate SHA-256 |
|---|---|---|
| BoardComponent.cs | 510bdaffb4adc7aefef7d523c52acc20630fbf220be308af12e67432a339f15b | 079c44cf7f3ff6b1fd4fcbbbabb5ffdc54042821bf9044122c0b2fbcc97e594b |
| DrunkardGame.cs | bd5f1cd9248ab5adbbda611f5caec13aae80d7d3505822b8a611a852fc268044 | c54e62fe2b52d7697575f515d92bb3a59bd0036e4282cdecc361b9386a5d169a |
| Lifecycle fixture | 409c9a39e1ea3888a340c8be7aed0c5c519c6749a0ab4488feda8b6891b8294d | 409c9a39e1ea3888a340c8be7aed0c5c519c6749a0ab4488feda8b6891b8294d |
| HierarchyObserver | aed239406a4a02cff375baedffb28f83ebd37a001b5b640123004109bef27db6 | aed239406a4a02cff375baedffb28f83ebd37a001b5b640123004109bef27db6 |

Сверены actual frozen copies, текущие файлы, candidate bytes, manifests и [application receipt](D:/Temp/Neoxider-LibraryClosure-20261102/candidates/lib-1102-03/project-application-receipt.json). Между red и green manifests изменены ровно два указанных production-файла; их key sets совпадают. Red fixture и green fixture идентичны. Сравнение с baseline fixture SHA `411312ba99cc11795e43e374b1ceba6a599fec5afff57710c0d21c84475a1514` из play-main-u63-baseline-r1-inputs даёт 312 added / 0 removed lines. Все старые 27 test identities существуют и Passed в baseline и новом green; все шесть red identities существуют и Passed в green. Green содержит ровно 33 уникальных leaves. Assertions не ослаблены, skips/Ignore в изменения не добавлены.

Каждый manifest содержит 5931 frozen identity и 1528 source copies. Независимо прочитаны и сверены SHA/размер всех 1528 source copies и 34 PRIVATE configuration copies каждого run: ноль отличий от их manifest. Независимо сверены все текущие 5931 identity с green manifest: ноль отличий; все 12 pre-main protected paths совпали с исходным protected receipt. Оба summary сообщают changedFrozenPaths=[] и protectedDifferences=[]. Это не утверждение о неизменности всех внешних vendor dependencies: область freeze задана manifest.

Prefs receipts обоих run: 51 до / 51 после, ноль added/removed/changed names. Независимое сравнение сохранённых PRIVATE snapshots подтвердило неизменность names, Kind и Value и тот же registry path; значения и registry path не публикуются. Reviewer ничего не восстанавливал и не менял.

Основные первичные источники: red [raw XML](D:/Temp/Neoxider-LibraryClosure-20261102/runs/cards-native-u63-red-r2/results.xml), [launch](D:/Temp/Neoxider-LibraryClosure-20261102/runs/cards-native-u63-red-r2/launch.json), [summary](D:/Temp/Neoxider-LibraryClosure-20261102/runs/cards-native-u63-red-r2/summary.json), [index](D:/Temp/Neoxider-LibraryClosure-20261102/runs/cards-native-u63-red-r2/testcase-index.json), [manifest](D:/Temp/Neoxider-LibraryClosure-20261102/runs/cards-native-u63-red-r2-inputs/manifest.json); green [raw XML](D:/Temp/Neoxider-LibraryClosure-20261102/runs/cards-native-u63-green-r1/results.xml), [launch](D:/Temp/Neoxider-LibraryClosure-20261102/runs/cards-native-u63-green-r1/launch.json), [summary](D:/Temp/Neoxider-LibraryClosure-20261102/runs/cards-native-u63-green-r1/summary.json), [index](D:/Temp/Neoxider-LibraryClosure-20261102/runs/cards-native-u63-green-r1/testcase-index.json), [manifest](D:/Temp/Neoxider-LibraryClosure-20261102/runs/cards-native-u63-green-r1-inputs/manifest.json). Prefs публичные receipts: [red](D:/Temp/Neoxider-LibraryClosure-20261102/runs/cards-native-u63-red-r2/editorprefs-comparison.json), [green](D:/Temp/Neoxider-LibraryClosure-20261102/runs/cards-native-u63-green-r1/editorprefs-comparison.json).

| Artifact | SHA-256 |
|---|---|
| red results.xml | 5cbf615fe4939145ebc0b5eea0f15933419b07bbbf36c7e5958f5115ab17309b |
| green results.xml | f9fd041a36afa13427e045157ffcccc90d8eae7ab5505b02962dbb1f80aef09b |
| red manifest.json | 34497fd0afd8e7029d8fc23e8ad31b1a8ad2c15c77cd3b3de70d07ce604a9214 |
| green manifest.json | e198645a606cbec4496bb5bea792088d3e2f93a77a8c0e1381ae13dae5565672 |
| application receipt | 57cb32fb998362bbe0b2df448e911ee5b1a34a87729ff8ba0016d1c812265fab |

Acceptance ограничен Cards F01/F02 и перечисленными 33 lifecycle cases на Unity 6.3 Editor PlayMode. Unity 6.6 / иные версии, Player, standalone consumer/sample/manual contexts и остальные модули библиотеки этим отчётом не закрываются. Публикация, commit и release не выполнялись.

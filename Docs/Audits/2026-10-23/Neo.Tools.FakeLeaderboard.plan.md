# Neo.Tools.FakeLeaderboard — индивидуальный план реализации

Основание: [аудит](./Neo.Tools.FakeLeaderboard.md), [контракт](./PLAN-CONTRACT.md), собственная запись manifest. Исходная сессия, `gpt-6.1-sol`, reasoning `max`. Прочитаны текущие исходники, тесты, документация и зависимости. Создаётся только этот план.

## Текущий результат

F01–F11 уже исправлены: best-score/сброс, целочисленные границы, идентификация, ранги без UI, обновление строк, загрузка/переименование игрока, необязательные TMP-поля, отмена движения, восстановление масштаба и форматирование. Это выполненная работа.

Обновлены четыре страницы `Assets/Neoxider/Docs/Tools/FakeLeaderboard/`. Добавлены 19 обычных случаев и четыре lifecycle-сценария с префиксом `Audit20261023NeoToolsFakeLeaderboard`. Статические проверки API, UTF-8, ссылок, GUID, delimiters и whitespace прошли. Компиляция, тесты, Inspector/Play Mode и визуальная проверка **не выполнены**.

## Приоритеты остатка

| Приоритет / статус | Доказательство и действие | Владелец / наблюдаемый результат |
|-------------------|--------------------------|---------------------------------|
| P1, долг проверки перед выпуском | После завершения всех изменяющих код аудитов скомпилировать пакет и выполнить оба новых fixture, существующий `FakeLeaderboardBehaviorTests` и относящиеся к модулю `ToolsDiagnosticsDefaultsTests`. Lifecycle-fixture находится в EditMode и сам входит в Play Mode. | Координатор запускает проверки; FakeLeaderboard исправляет свои подтверждённые сбои. Сохранённый receipt содержит версию Unity, ревизию/набор исходников и фактические результаты. |
| P2, подтверждённое ограничение сериализации | В `Leaderboard.cs` поле `LeaderboardUser.id` остаётся `readonly` и не сериализуется. `IsPlayerUser` правильно отклоняет пустые ID, но `SyncPlayer` не может распознать отдельную сериализованную копию игрока. Минимальное продолжение — сериализуемый ключ без изменения старого поля; детали ниже. | FakeLeaderboard. После roundtrip связанный игрок остаётся одной логической записью. Конкретное наличие лишних записей в существующих prefab пока не доказано engine-проверкой. |
| P2, долг authoring-проверки | `OnValidate` изменяет размер данных, вызывает сортировку и при включённом флаге создаёт строки. Проверить shrink/expand, сохранение/повторное открытие Inspector, summary внутри container. Устранить только воспроизведённые ошибки. | Координатор проводит Unity QA; FakeLeaderboard исправляет свой код. Игрок сохранён, ранги актуальны, отсутствуют старые видимые строки и новые ошибки консоли. |
| P3, непроверенные риски | Проверить `LeaderboardMove.Move` с реальным canvas: формула использует world Y. `useAnimStartScale` подтверждённо не используется, но желаемая семантика не задана. Большое число строк не профилировалось. | Координатор / владелец модуля. Не менять координаты, legacy-флаг и способ создания строк без воспроизведения проблемы или требования. Это не новые подтверждённые дефекты. |

### Минимальная доработка идентичности

В `Leaderboard.cs` добавить `[SerializeField] string _persistentId` и read-only `LeaderboardUser.PersistentId`. Конструктор задаёт оба ID; перед сериализацией переносить известный непустой `id` в пустой ключ. Getter ничего не генерирует. Предикат использует canonical reference или непустой persistent key; имена идентичностью не становятся.

Старые данные без ключей автоматически не восстановить. Предлагаемый `bool TryBindPlayerEntry(int sourceIndex)` заменяет выбранный слот canonical `player`, сохраняя загруженные имя/score, и сортирует; неверный индекс возвращает `false` без изменений. Другие одноимённые записи сохраняются. Альтернатива — существующие `GenerateUserList()` / `Sort()`. Массовой перезаписи assets нет.

Новый `Assets/Neoxider/Tests/Edit/Tools/FakeLeaderboard/Audit20261023NeoToolsFakeLeaderboardSerializationTests.cs`: roundtrip агрегата `player`/`users`, сохранение ключа, единственность игрока, rename/одинаковые имена, legacy JSON и явное связывание. Текущий тест отдельных пользователей агрегатный сценарий не покрывает. Оценка: 0,5–1 день; риск — неверная миграция.

## Существенное улучшение — предложение, ещё не реализовано

Проблема: рейтинг требует GameObject, UI и singleton; mutable списки осложняют синхронизацию. Предлагается обычное C# ядро внутри `Neo.Tools.FakeLeaderboard`, первоначально без нового asmdef.

Новые файлы в `Assets/Neoxider/Scripts/Tools/FakeLeaderboard/`:

- `LeaderboardEntry.cs`: неизменяемые значения `Id`, `Name`, `Score`, `HasScore`; явный конструктор, непустой ID.
- `LeaderboardSnapshot.cs`: версионированные сериализуемые DTO с `PlayerId`, направлением и записями; DTO не хранят scene references.
- `LeaderboardModel.cs`: стабильная сортировка и операции над данными, без UnityEngine и I/O в коде ядра.

Предлагаемый публичный контракт:

```text
LeaderboardModel(string playerId, SortOrder sortOrder)
void ReplaceEntries(IEnumerable<LeaderboardEntry> entries)
bool TrySetPlayerScore(int score, bool overrideBestScore = false)
void ClearPlayerScore()
bool TryGetRank(string id, out int zeroBasedRank)
IReadOnlyList<LeaderboardEntry> GetRankedEntries()
LeaderboardSnapshot CaptureSnapshot()
void RestoreSnapshot(LeaderboardSnapshot snapshot)
```

Дубликаты/пустые ID и некорректные snapshots отклоняются до изменения состояния; допустимый snapshot содержит canonical player. `TrySetPlayerScore` не создаёт отсутствующего игрока. `HasScore` отделяет отсутствие результата от допустимого нуля; старый facade продолжает считать ноль отсутствием результата и переводит `UpdatePlayerScore(0, true)` в `ClearPlayerScore`. Snapshot выдаётся отдельной копией.

`Leaderboard.cs` сохраняет scene adapter, генерацию, I/O и события. Каждый `Sort`/update импортирует актуальные поля в модель и возвращает результат в те же `LeaderboardUser`: прямые изменения `users`/`player` учитываются. `LeaderboardItem.cs` / `LeaderboardMove.cs` продолжают использовать facade, обязательных изменений нет.

Reuse: текущие random/formatting extensions и стабильная LINQ-сортировка. Отдельной ranking model в модуле нет. `NeoDiagnostics.StableId` для Unity objects не заменяет постоянные ID записей. Новые менеджер/provider/DI не нужны.

Явные `SaveSnapshot()` / `bool TryLoadSnapshot()` используют `SaveProvider.SetString/GetString`, JSON-адаптер и ключ `<playerSaveKey>_Snapshot`. Старые `_Name`/`_Score` остаются fallback; пустой save key отключает сохранение. Неподдерживаемая версия не уничтожает данные. Автосохранение ботов не включать молча.

Новый `Assets/Neoxider/Tests/Edit/Tools/FakeLeaderboard/Audit20261023NeoToolsFakeLeaderboardModelTests.cs`: направления/int-границы, ties, scored-zero/clear, неверные ID, атомарный restore, независимость snapshot. Serialization-fixture покрывает старые/новые сохранения; lifecycle-fixture — эквивалентность рангов/UI.

Обновить owned `README.md` / `Leaderboard.md`: C# ядро, mutable facade, ID, сохранение/миграция; scene/NoCode сохраняет компоненты/UnityEvents и Generate/Sort. Если понадобится migration Inspector с Undo, его делает `Neo.Editor`. Оценка: 2–3 дня плюс день интеграции; риск средний — сериализация и mutable facade.

## Границы, порядок реализации и совместимость

1. **Сначала проверка уже выполненных исправлений.** Координатор ждёт окончания параллельных source-аудитов и получает воспроизводимый baseline. Ошибки исправляются их владельцами, без отката чужих изменений.
2. **Идентичность и регрессия.** Добавить минимальный persistent key, агрегатный тест и явную migration-операцию. Старые публичные/serialized поля, `readonly id`, события и save keys не переименовывать. Возможные изменения `Assets/Neoxider/Prefabs/FakeLeaderboard/Leaderboard.prefab`, `Page LeaderBoard.prefab`, `LeaderBoard Item.prefab` выполняет координатор только для подтверждённо затронутых assets.
3. **Предложенное ядро.** Сначала entry/snapshot/model и тесты контрактов, затем facade/persistence adapter. Не менять callback ordering ради косметики; отдельно проверить подписчиков `OnSort`, включая startup generation.
4. **Документация и упаковка.** Обновить оба entry-документа; всем новым package-файлам выдать уникальные `.meta`. Сохранить release `Samples~` и `package.json.samples[].path`; validation учитывать также developer `Samples` и импортированный `Assets/Samples/NeoxiderTools/<version>/<sample>`. Перекладывание сэмплов не требуется.

Зависимости: `Neo.Save` владеет provider/flush lifecycle; `Neo.Tools.Managers` — `Singleton<T>` и его текущими параллельными изменениями; `Neo.Extensions` — random/formatting; `Neo.PropertyAttribute` — существующим authoring-слоем. Сначала стабилизировать их публичные контракты, затем интегрировать FakeLeaderboard. Никаких изменений чужих исходников не планируется. Изменения общих `Neo.Editor.Tests.asmdef`, package/config и отдельное выделение model assembly — только координатор; сейчас необходимые ссылки уже есть.

## Наблюдаемая приёмка и условия остановки

- На обеих версиях Unity — представитель 6.0–6.4 и 6.5+ — компиляция чистая; новые и существующие релевантные тесты фактически выполнены. Новые version branches не добавлять; при необходимости Unity ID API использовать централизованный compatibility helper, проверяя обе ветви соответствующим компилятором.
- Ascending принимает первый `12`; descending принимает отрицательный результат; обычный ноль не стирает best; explicit reset работает; крайние int предшествуют unscored. Ранги одинаковы без строк, с одной строкой и со всем списком.
- После сериализации/перезагрузки и rename canonical player выбран единожды; одноимённый бот сохраняет собственный score. Неоднозначный legacy-list не мигрируется по имени. Выбранная явная миграция сохраняет остальных пользователей.
- После disable до delay позиция не меняется и Invoke отсутствует; restart/disable после половины pulse возвращает исходный неединичный scale. Дополнительно проверить destroy target до scroll completion и отсутствие посторонних логов/твинов.
- Сокращение/расширение через Inspector сохраняет игрока и корректную видимость; старые saved player keys читаются; новые snapshots независимы, повреждённая/новая версия отклоняется без потери текущих данных.

При красной компиляции/регрессии дальнейшее внедрение ядра приостанавливается до узкого исправления. Без идентифицируемого legacy-player не выполнять разрушительную дедупликацию: нужен явный выбор записи или регенерации. Координатор отдельно решает включать ли предложенное ядро и snapshot-функцию в текущий выпуск; это не препятствует проверке уже исправленного кода. До центральных receipts работа остаётся непроверенной в Unity. В этой фазе Unity/build/test процессы не запускались, изменения ограничены данным планом.

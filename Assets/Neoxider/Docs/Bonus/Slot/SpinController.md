### Класс SpinController

- **Пространство имен (Namespace)**: `Neo.Bonus`
- **Путь к файлу**: `Assets/Neoxider/Scripts/Bonus/Slot/SpinController.cs`

#### Краткое описание

**Что это:** `SpinController` — это центральный компонент, управляющий всей механикой слота. Он выступает в роли "оркестратора": запускает вращение барабанов (`Row`), ожидает их остановки, анализирует выпавшие ...

**Как использовать:** см. разделы ниже.

---

`SpinController` — центральный компонент слота: **`Row`** (колонки), **`CheckSpin`** (линии и множители), ставки, события, опционально **`LineRenderer`** для подсветки выигрыша. После остановки заполняет **`Elements`** и **`finalVisuals`** (`x` — колонка, **`y=0`** — низ окна). Из кода доступны **настройка** (`ActivePaylineCount`, `ConfigureSlotRuntime`, …), **запросы матриц линий** и **`GetRuntimeSnapshot`** для UI / сохранений.

#### Публичные свойства и поля (Public Properties and Fields)
- **`checkSpin`** (`CheckSpin`): Экземпляр класса для проверки выигрышных комбинаций.
- **`betsData`** (`BetsData`): Ссылка на `ScriptableObject` с данными о доступных ставках.
- **`allSpritesData`** (`SpritesData`): Ссылка на `ScriptableObject` с данными о визуальных элементах слота.
- **`ChanceWin`** (`float`): Вероятность (0–1), что план спина будет смещён к выигрышу (поле в YAML префабов может ещё храниться как `chanseWin` — см. `FormerlySerializedAs`).
- **`finalVisuals`** (`SlotVisualData[,]`): Двумерный массив, который хранит данные о видимых элементах после остановки барабанов.
- **`moneySpend`** (`IMoneySpend`): Интерфейс для взаимодействия с системой списания денег за спин.
- **`EvaluatedPaylineDefinitionCount`**: Сколько определений линий участвует в ставке и проверке (`countLine`, ограниченный числом доступных линий).
- **`LastWinningPaylineIndices`**: Индексы выигравших линий после последнего завершённого спина (`IReadOnlyList<int>`); очищаются при старте нового спина или при проигрыше.
- **`Rows`**, **`ActivePaylineCount`**, **`VisibleWindowRows`**, **`BetSelectionIndex`**, **`DelayBetweenColumnSpins`**, **`CurrentSpinPrice`**, **`WinLinePlayback`**: см. раздел «Программная настройка и состояние».

#### Публичные методы
- **`StartSpin()`**: Основной метод для запуска вращения. Проверяет, остановлены ли барабаны, списывает стоимость спина и запускает корутину `StartSpinCoroutine`.
- **`IsStop()`**: Возвращает `true`, если все барабаны (`Row`) завершили вращение.
- **`AddLine()` / `RemoveLine()`**: Увеличивает или уменьшает количество активных выигрышных линий.
- **`SetMaxBet()`**: Устанавливает максимальный размер ставки из `betsData`.
- **`AddBet()` / `RemoveBet()`**: Увеличивает или уменьшает текущую ставку, циклически переключаясь по доступным вариантам в `betsData`.
- **`GetElementsMatrix` / `GetElementIDsMatrix`**: параметр **`refreshIfIdle`** — если **`true`** и **`IsStop()`**, матрица перечитывается с экрана; во время спина возвращается последний кэш.
- **`GetPaylineDefinitionsSnapshot()`**: снимок активных определений линий (**Lines Data** или fallback), порядок = индексы линий.
- **`GetPaylineWindowRowsMatrix()`**: **`int[lineIndex, column]`** — ряд окна (0 = низ) для каждой ячейки линии; для только «активных» по ставке — **`GetActivePaylineWindowRowsMatrix()`**.
- **`GetPaylineSymbolIdsMatrix(refresh)`** / **`GetActivePaylineSymbolIdsMatrix(refresh)`**: **`int[lineIndex, column]`** — ID символов вдоль линий.
- **`TryGetPaylineSlotElements(lineDefinitionIndex, out elements, refresh)`**: массив **`SlotElement`** по колонкам для одной линии (удобно для анимаций).
- **`GetLastWinningPaylinesSlotElements(refresh)`**: массив массивов элементов по каждой выигравшей линии (порядок совпадает с **`LastWinningPaylineIndices`**).
- **`GetLastWinningPaylinesSymbolIds(refresh)`** / **`GetLastWinningPaylinesWindowRows()`**: матрицы **`[whichWin, column]`** только для последнего выигрыша.
- **`GetRuntimeSnapshot(refreshMatrices)`**: см. раздел «Программная настройка и состояние».

#### Программная настройка и состояние (`SpinController`)

Большинство сеттеров (**`Rows`**, **`ActivePaylineCount`**, **`VisibleWindowRows`**, **`BetSelectionIndex`**, **`ConfigureSlotRuntime`**) выполняются только если **`IsStop()`** — во время вращения изменения игнорируются.

- **`Rows`**: ссылки на колонки-барабаны (`Row[]`).
- **`ActivePaylineCount`** / **`VisibleWindowRows`** / **`BetSelectionIndex`** / **`DelayBetweenColumnSpins`**: управление без инспектора; при смене высоты окна вызываются **`SetSpace()`**, пересчитывается цена (**`SetPrice`**).
- **`CurrentSpinPrice`**: цена следующего **`StartSpin()`** (после последнего **`SetPrice`**).
- **`ConfigureSlotRuntime(visibleWindowRows, activePaylineCount, fallbackMin, fallbackMax)`**: одним вызовом высота окна, число активных линий и fallback в **`checkSpin`** (**−1** / **−1** у Min/Max = все ряды окна). Внутри вызывает **`CheckSpin.SetFallbackPaylineWindowRows`**.
- **`WinLinePlayback`**: ссылка на **`WinLineRendererPlayback`** (поля можно менять из кода).
- **`GetRuntimeSnapshot(refreshMatrices)`**: возвращает **`SpinRuntimeSnapshot`** (см. таблицу ниже). При **`refreshMatrices == true`** и простое барабанов пересобирает **`Elements`** / **`finalVisuals`** перед чтением.

##### Структура `SpinRuntimeSnapshot`

| Поле | Описание |
|------|-----------|
| **`IsIdle`** | **`true`**, если все **`Row`** не крутятся (`IsStop`). |
| **`WindowHeight`** | Высота видимого окна в рядах символов. |
| **`ColumnCount`** | Число колонок (`Rows.Length`). |
| **`ActivePaylineCount`** | Значение «активных линий» в инспекторе (`countLine`). |
| **`EvaluatedPaylineCount`** | Сколько линий реально участвует в проверке (`min(countLine, defs)`). |
| **`TotalPaylineDefinitionCount`** | Всего определений линий (**Lines Data** или fallback). |
| **`BetIndex`** | Индекс текущей ставки в **`betsData.bets`**. |
| **`SpinPrice`** | То же, что **`CurrentSpinPrice`** на момент снимка. |
| **`CheckSpinActive`** | **`checkSpin.isActive`**. |
| **`UsesFallbackPaylinesOnly`** | Нет валидного **Lines Data** для текущего окна. |
| **`FallbackMinRaw`** / **`FallbackMaxRaw`** | Сериализованные −1 / число из **`CheckSpin`**. |
| **`FallbackResolvedMinRow`** / **`FallbackResolvedMaxRow`** | Фактический включительный диапазон рядов окна (**0** = низ). |
| **`LastWinningPaylineIndicesCopy`** | Копия массива индексов выигравших линий последнего спина (может быть пустым). |

В **`CheckSpin`** из кода: **`LinesDataAsset`**, **`SpritesMultiplierData`**, **`SequenceLength`**, **`SetSequenceLength`**, **`GetEffectiveLines`**, **`GetPaylineDefinitionCount`**, **`GetResolvedFallbackWindowRowRange`**, **`UsesFallbackPaylinesOnly`**, **`SetFallbackPaylineWindowRows`**, **`ClearLegacyFallbackSingleRowBinding`** — см. [CheckSpin.md](./CheckSpin.md).

#### Unity Events
- **`OnStartSpin`**: Вызывается в момент начала вращения.
- **`OnEndSpin`**: Вызывается после полной остановки всех барабанов и обработки результатов.
- **`OnEnd(bool)`**: Вызывается в самом конце. Передает `true`, если спин был выигрышным, и `false` в противном случае.
- **`OnWin(int)`**: Вызывается при выигрыше. Передает общую сумму выигрыша.
- **`OnWinLines(int[])`**: Вызывается при выигрыше. Передает массив индексов выигрышных линий.
- **`OnLose`**: Вызывается при проигрыше.
- **`OnChangeBet(string)`**: Вызывается при изменении общей стоимости спина. Передает строковое представление новой цены.
- **`OnChangeMoneyWin(string)`**: Вызывается для обновления отображения выигрыша. Передает строковое представление суммы.

#### Опциональная анимация выигрышной линии (LineRenderer)

В блоке **Visual** есть сериализуемый объект **`Win Line Playback`** (`WinLineRendererPlayback`):

- Пока **`enabled`** выключен или массив **`renderers`** пуст, поведение как раньше — только UI через **`Visual Slot Lines`**.
- Укажите один или несколько **`LineRenderer`** на том же канве / слое камеры, что и символы (обычно **world-space** позиции ячеек; для стартового материала подходит стандартный шаблон линии с поддержкой градиента по длине).
- **`SequentialSingle`** — все выигрышные линии показываются по очереди на первом не-null рендерере.
- **`ParallelWhenPossible`** — если выигрышных линий не больше числа назначенных рендереров, линии рисуются одновременно (каждая на своём `LineRenderer`).
- **`colorStyle`** — как задаётся цвет по длине линии:
  - **`AccentGlow`** — центр **`color`**, края темнее по **RGB**; **альфа** везде берётся из **`color`** (код её не уменьшает).
  - **`SolidFlat`** — ровный **`color`** (RGBA из инспектора).
  - **`LinearGradient`** — переход **`colorLineStart` → `colorLineEnd`** (альфа из каждого цвета; при «бегущем» блике фон темнее только по RGB).
  - **`CustomGradient`** — свой **`Gradient`** (все каналы, включая альфу, только из инспектора); если ключей меньше двух — откат к AccentGlow.
- **`travelSpeed` > 0** — «бегущий» яркий участок; учитывает выбранный **`colorStyle`** (в т.ч. выборка из пользовательского градиента по фазе).
- При новом спине или **`OnDisable`** воспроизведение останавливается, линии скрываются.

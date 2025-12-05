# Игра «Пьяница» (War Card Game)

Готовый компонент `DrunkardGame` для создания классической карточной игры без написания кода.

---

## Правила игры

1. Колода делится поровну между двумя игроками
2. Порядок хода настраивается (`Player Goes First`):
   - По умолчанию: сначала соперник, затем игрок (задержка настраивается)
   - Можно поменять: сначала игрок, затем соперник
3. У кого карта старше — забирает обе карты себе
4. При равенстве карт — «война»: выкладывается ещё по одной карте рубашкой вверх, затем ещё по одной лицом — кто старше, забирает все
5. Побеждает тот, кто соберёт все карты
6. Если используется HandComponent — карты улетают с стола в руку с анимацией

---

## Быстрая настройка (No-Code)

### Шаг 1: Создайте иерархию объектов

```
Canvas
├── GamePage (ваша страница игры)
│   ├── DrunkardGame (DrunkardGame.cs)
│   ├── PlayerHand (HandComponent) — опционально, для видимых карт
│   ├── OpponentHand (HandComponent) — опционально
│   ├── PlayerDeckPos (RectTransform)
│   ├── PlayerCardPos (RectTransform)
│   ├── OpponentDeckPos (RectTransform)
│   ├── OpponentCardPos (RectTransform)
│   ├── PlayerScoreText (TMP_Text)
│   ├── OpponentScoreText (TMP_Text)
│   ├── PlayButton (Button)
│   ├── WinPanel (Panel)
│   │   ├── WinText
│   │   └── RestartButton
│   └── LosePanel (Panel)
│       ├── LoseText
│       └── RestartButton
```

### Шаг 2: Создайте DeckConfig

1. ПКМ в Project → **Create → Neo → Cards → Deck Config**
2. Настройте типы колоды:
   - **Deck Type** — тип для спрайтов (можно `Standard52` если хотите универсальный конфиг)
   - **Game Deck Type** — установите `Standard36` для классической «Пьяницы»
3. Назначьте спрайты карт и рубашки

### Шаг 3: Создайте префаб карты

1. Создайте UI Image
2. Добавьте компонент `CardComponent`
3. Назначьте `Image` в поле `Card Image`
4. Настройте размер (например 100x140)
5. Сохраните как префаб

### Шаг 4: Настройте DrunkardGame

В инспекторе компонента `DrunkardGame`:

#### Config

| Поле | Описание |
|------|----------|
| **Deck Config** | Конфигурация колоды (GameDeckType = 36 карт) |
| **Card Prefab** | Префаб карты с CardComponent |
| **Initialize On Start** | Автоматически раздать карты при запуске (по умолчанию true) |
| **Debug** | Включить логи для отладки (по умолчанию false) |

#### Positions

| Поле | Описание |
|------|----------|
| **Cards Parent** | Родитель для спавна карт (GamePage или Canvas) |
| **Initial Board** | (Опционально) BoardComponent для начального спавна всех карт |
| **Player Deck Position** | Transform позиции колоды игрока. **Можно указать HandComponent!** |
| **Player Card Position** | Transform куда выкладывается карта игрока |
| **Opponent Deck Position** | Transform позиции колоды соперника. **Можно указать HandComponent!** |
| **Opponent Card Position** | Transform куда выкладывается карта соперника |

**Как работает Initial Board:**
- Если указан — все карты сначала спавнятся в BoardComponent
- Затем раздаются поровну в HandComponent (если указан) или в очередь
- Удобно для визуализации "раздачи с колоды"

#### Timing

| Поле | Значение по умолчанию | Описание |
|------|----------------------|----------|
| **Card Move Duration** | 0.3 сек | Длительность анимации перемещения карты |
| **Round Delay** | 1 сек | Задержка перед скрытием карт после раунда |
| **Turn Delay** | 0.3 сек | Задержка между ходами игроков |
| **War Continue Delay** | 0.5 сек | Задержка при продолжении «войны» |
| **Card Return Delay** | 0.1 сек | Задержка между картами при возврате в руку |

#### Game Rules

| Поле | Значение по умолчанию | Описание |
|------|----------------------|----------|
| **Player Goes First** | false | Кто ходит первым: true = игрок, false = соперник |

---

## Режимы работы

### Режим 1: Невидимые очереди (классический)

Карты хранятся в невидимых стопках, показываются только при ходе.

**Настройка:**
- `Initial Board` → пусто
- `Player Deck Position` → пустой RectTransform (позиция)
- `Opponent Deck Position` → пустой RectTransform (позиция)

### Режим 2: Видимые руки

Карты отображаются в HandComponent, игрок видит свою стопку.

**Настройка:**
- `Initial Board` → пусто (или BoardComponent)
- `Player Deck Position` → GameObject с **HandComponent**
  - В HandComponent установите **Add To Bottom ☑** (новые карты под низ стопки)
- `Opponent Deck Position` → GameObject с **HandComponent** (опционально)

При раздаче карты автоматически добавятся в HandComponent и будут видны! После победы в раунде карты **летят со стола в руку** с анимацией.

### Режим 3: Раздача через BoardComponent

Все карты сначала спавнятся в BoardComponent (визуализация колоды), затем раздаются.

**Настройка:**
- `Initial Board` → **BoardComponent** с макс. картами 36-54
- `Player Deck Position` → HandComponent или пустой Transform
- `Opponent Deck Position` → HandComponent или пустой Transform

**Визуализация:**
```
1. Спавн всех карт в Board (36 карт видны)
2. Раздача поровну → карты перемещаются в Hand
3. Игра начинается
```

---

## Подключение событий в инспекторе

### События счёта карт

| Событие | Параметр | Использование |
|---------|----------|---------------|
| `OnPlayerCardCountChanged` | `int` | Подключите `TMP_Text.SetText` для отображения счёта игрока |
| `OnOpponentCardCountChanged` | `int` | Подключите `TMP_Text.SetText` для отображения счёта соперника |

**Пример настройки:**
```
OnPlayerCardCountChanged (int):
  → PlayerScoreText.SetText (Dynamic int)
  
OnOpponentCardCountChanged (int):
  → OpponentScoreText.SetText (Dynamic int)
```

### События игры

| Событие | Использование |
|---------|---------------|
| `OnGameStarted` | Скрыть «Tap to Start» надпись |
| `OnGameRestarted` | Сбросить UI, скрыть панели результата |
| `OnPlayerWin` | Показать панель победы |
| `OnOpponentWin` | Показать панель поражения |

**Пример настройки:**
```
OnPlayerWin:
  → WinPanel.SetActive (true)
  
OnOpponentWin:
  → LosePanel.SetActive (true)
  
OnGameRestarted:
  → WinPanel.SetActive (false)
  → LosePanel.SetActive (false)
```

### События раунда

| Событие | Использование |
|---------|---------------|
| `OnRoundStarted` | Показать индикатор раунда |
| `OnRoundEnded` | Обновить UI |
| `OnPlayerWonRound` | Звук/текст победы в раунде |
| `OnOpponentWonRound` | Звук/текст поражения в раунде |
| `OnWarStarted` | Показать текст «Война!» |
| `OnWarEnded` | Скрыть текст «Война!» |

---

## Настройка управления

### Кнопка хода

```
PlayButton (Button)
└── OnClick()
    └── DrunkardGame → Play()  ← для вызова раунда
```

### Кнопка рестарта

```
RestartButton (Button)
└── OnClick()
    └── DrunkardGame → RestartGame()
```

**Примечание:** Используйте метод `Play()` (не `PlayRound`), так как он синхронный и подходит для UI кнопок.

---

## Публичные свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `PlayerCardCount` | `int` | Текущее количество карт у игрока (из руки или очереди) |
| `OpponentCardCount` | `int` | Текущее количество карт у соперника |
| `IsPlaying` | `bool` | Идёт ли сейчас раунд |
| `GameStarted` | `bool` | Началась ли игра |
| `PlayerHand` | `HandComponent` | HandComponent игрока (если указан) |
| `OpponentHand` | `HandComponent` | HandComponent соперника (если указан) |
| `UsePlayerHand` | `bool` | Используется ли рука игрока |
| `UseOpponentHand` | `bool` | Используется ли рука соперника |

---

## Публичные методы

| Метод | Описание |
|-------|----------|
| `Play()` | Выполняет один раунд игры (синхронный, для UI кнопок) |
| `PlayRound()` | Асинхронная версия раунда |
| `RestartGame()` | Перезапускает игру |

---

## Расширенная настройка (с кодом)

### Подписка на события из кода

```csharp
public class DrunkardUI : MonoBehaviour
{
    [SerializeField] private DrunkardGame _game;
    [SerializeField] private TMP_Text _playerScore;
    [SerializeField] private TMP_Text _opponentScore;
    [SerializeField] private GameObject _winPanel;
    [SerializeField] private GameObject _losePanel;

    private void OnEnable()
    {
        _game.OnPlayerCardCountChanged.AddListener(UpdatePlayerScore);
        _game.OnOpponentCardCountChanged.AddListener(UpdateOpponentScore);
        _game.OnPlayerWin.AddListener(ShowWinPanel);
        _game.OnOpponentWin.AddListener(ShowLosePanel);
        _game.OnGameRestarted.AddListener(HidePanels);
    }

    private void OnDisable()
    {
        _game.OnPlayerCardCountChanged.RemoveListener(UpdatePlayerScore);
        _game.OnOpponentCardCountChanged.RemoveListener(UpdateOpponentScore);
        _game.OnPlayerWin.RemoveListener(ShowWinPanel);
        _game.OnOpponentWin.RemoveListener(ShowLosePanel);
        _game.OnGameRestarted.RemoveListener(HidePanels);
    }

    private void UpdatePlayerScore(int count) => _playerScore.text = count.ToString();
    private void UpdateOpponentScore(int count) => _opponentScore.text = count.ToString();
    private void ShowWinPanel() { _winPanel.SetActive(true); }
    private void ShowLosePanel() { _losePanel.SetActive(true); }
    private void HidePanels() 
    { 
        _winPanel.SetActive(false); 
        _losePanel.SetActive(false); 
    }
}
```

### Звуковые эффекты

```csharp
public class DrunkardAudio : MonoBehaviour
{
    [SerializeField] private DrunkardGame _game;
[SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _cardFlip;
    [SerializeField] private AudioClip _win;
    [SerializeField] private AudioClip _lose;
    [SerializeField] private AudioClip _war;

    private void OnEnable()
    {
        _game.OnRoundStarted.AddListener(() => _audioSource.PlayOneShot(_cardFlip));
        _game.OnPlayerWin.AddListener(() => _audioSource.PlayOneShot(_win));
        _game.OnOpponentWin.AddListener(() => _audioSource.PlayOneShot(_lose));
        _game.OnWarStarted.AddListener(() => _audioSource.PlayOneShot(_war));
    }
}
```

---

## Полный список событий

### UnityEvent<int> (с параметром)

| Событие | Параметр | Когда вызывается |
|---------|----------|------------------|
| `OnPlayerCardCountChanged` | Количество карт | После каждого раунда и при рестарте |
| `OnOpponentCardCountChanged` | Количество карт | После каждого раунда и при рестарте |

### UnityEvent (без параметров)

| Событие | Когда вызывается |
|---------|------------------|
| `OnGameStarted` | При первом раунде |
| `OnGameRestarted` | При вызове RestartGame() |
| `OnPlayerWin` | Когда игрок собрал все карты |
| `OnOpponentWin` | Когда соперник собрал все карты |
| `OnRoundStarted` | В начале каждого раунда |
| `OnRoundEnded` | В конце каждого раунда |
| `OnPlayerWonRound` | Когда игрок выиграл раунд |
| `OnOpponentWonRound` | Когда соперник выиграл раунд |
| `OnWarStarted` | При начале «войны» (равные карты) |
| `OnWarEnded` | При завершении «войны» |

---

## Особенности реализации

### Hover эффект на картах

CardComponent автоматически обрабатывает hover:
- ✅ Корректно работает во время анимации
- ✅ Сбрасывается при начале новой анимации
- ✅ Не накапливает эффекты при частых кликах
- ✅ Обновляет оригинальную позицию после перестановки в HandComponent

Настройка в префабе:
```
CardComponent
├── Enable Hover Effect → ☑ (включить)
├── Hover Scale → 0.1 (дельта увеличения: 0.1 = +10%, 0.2 = +20%)
├── Hover Y Offset → 20 (подъём вверх в пикселях)
└── Hover Duration → 0.15 (скорость анимации в секундах)
```

**Примеры Hover Scale:**
- `0.1` → карта станет 110% от оригинального размера (+10%)
- `0.2` → карта станет 120% (+20%)
- `-0.1` → карта уменьшится до 90% (-10%)

### Порядок ходов

Настраивается параметром `Player Goes First`:

**Если false (по умолчанию) — соперник первый:**
1. Соперник берёт карту
2. Задержка `Turn Delay` (0.3 сек)
3. Игрок берёт карту
4. Сравнение и определение победителя

**Если true — игрок первый:**
1. Игрок берёт карту
2. Задержка `Turn Delay` (0.3 сек)
3. Соперник берёт карту
4. Сравнение и определение победителя

Далее:
5. Задержка `Round Delay` (1 сек)
6. Карты скрываются (уничтожаются или летят в руку)

### Обработка "войны"

При равенстве карт:
1. Первые 2 карты **остаются на столе**
2. Каждый кладёт 1 карту рубашкой вниз (добавляется в `warPile`)
3. Затем открывают ещё по 1 карте (создаётся **рядом** со старыми)
4. Если снова равенство — процесс повторяется (все карты остаются на столе)
5. Победитель забирает **ВСЕ** карты со стола (летят в руку с анимацией)
6. Карты переворачиваются рубашкой вверх после попадания в руку

---

## Отладка

### Включите Debug режим

```
DrunkardGame
└── Debug → ☑
```

Логи покажут:
```
[DrunkardGame] Игра перезапущена. Карт у игрока: 18, у соперника: 18
[DrunkardGame] NotifyCardCountChanged: Player=18, Opponent=18
[DrunkardGame] OnPlayerCardCountChanged listeners: 1
[DrunkardGame] Play() вызван
[DrunkardGame] ShowOpponentCard: Q♦
[DrunkardGame] Создана карта соперника
[DrunkardGame] Создана карта игрока
```

### Частые проблемы

| Проблема | Решение |
|----------|---------|
| Карты не появляются | Проверьте `Cards Parent`, `Card Prefab` и позиции назначены |
| `Play()` не вызывается | Подключите кнопку к `DrunkardGame.Play` |
| Счёт не обновляется | Подключите события `OnPlayerCardCountChanged` к TMP_Text |
| Карты за пределами экрана | Проверьте позиции Transform'ов |
| Карты невидимы | `Cards Parent` должен быть внутри активного Canvas |

---

## Результат

После настройки у вас будет рабочая игра «Пьяница» с:

- ✅ Автоматической раздачей карт (в очередь или HandComponent)
- ✅ Анимацией выкладывания и переворота карт
- ✅ Правильным порядком ходов (сначала соперник, потом игрок)
- ✅ Обработкой «войны» при равных картах
- ✅ Подсчётом карт через события
- ✅ Определением победителя
- ✅ Полной настройкой UI без кода
- ✅ Поддержкой видимых рук (HandComponent)

---

## См. также

- [CardData](../CardData.md) — структура данных карты
- [DeckConfig](../DeckConfig.md) — конфигурация колоды
- [CardComponent](../CardComponent.md) — компонент карты
- [HandComponent](../HandComponent.md) — компонент руки
- [README](../README.md) — обзор модуля Cards

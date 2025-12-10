# Changelog

All notable changes to this project will be documented in this file.

## [5.3.7] - Unreleased

### Исправлено
- **Physics модуль**: Исправлены ошибки компиляции с атрибутами Odin Inspector
  - Добавлены директивы `using Sirenix.OdinInspector` в компоненты ExplosiveForce, ImpulseZone, MagneticField

### Улучшено
- **Physics модуль**: Рефакторинг всего кода модуля
  - Улучшена структура и читаемость кода
  - Оптимизированы внутренние процессы

---

## [5.3.5] - Unreleased

### Добавлено
- **InteractiveObject**: Добавлена проверка препятствий (стен) между объектом и точкой проверки
  - Опция `checkObstacles` (по умолчанию включена) - проверяет наличие препятствий через raycast
  - Опция `obstacleLayers` - настройка слоев, которые блокируют взаимодействие
  - Предотвращает взаимодействие через стены и другие препятствия
  - Работает с 2D и 3D коллайдерами
  - Автоматически исключает коллайдер самого объекта из проверки
- **InteractiveObject**: Исправлена работа клавиатурного ввода для событий Down/Up
  - Теперь `onInteractDown` вызывается при нажатии клавиши, если объект в радиусе взаимодействия
  - Ранее требовалось наведение мыши (isHovered), что блокировало клавиатурное взаимодействие
- **Модуль Physics**: Новый модуль с интересными физическими компонентами
  - **ExplosiveForce**: Взрывная сила, которая толкает объекты в радиусе
    - Режимы активации: при старте, с задержкой, вручную
    - Фильтрация по слоям, опциональное добавление Rigidbody
    - Затухание силы по расстоянию (линейное/квадратичное)
    - Режимы силы: AddForce, AddExplosionForce
    - События OnExplode и OnObjectAffected
  - **ImpulseZone**: Зона импульса, применяющая импульс при входе объекта
    - Различные направления импульса (от центра, к центру, по направлению, кастомное)
    - Фильтрация по слоям и тегам
    - Одноразовое или многократное срабатывание с cooldown
    - События OnObjectEntered и OnImpulseApplied
  - **MagneticField**: Магнитное поле для притяжения/отталкивания объектов
    - Режимы: притяжение к себе, к Transform цели, к точке в пространстве, отталкивание, переключение
    - Постоянное воздействие с затуханием по расстоянию
    - События при входе/выходе объектов из поля и при переключении режима
    - Настраиваемые интервалы для режима переключения
  - Все компоненты поддерживают:
    - Фильтрацию по слоям (LayerMask)
    - Опциональное добавление Rigidbody на объекты без физики
    - UnityEvent для интеграции с другими системами
    - Визуализацию в редакторе через Gizmos
    - Публичный API для программного управления
    - Полную XML документацию

---

## [5.3.4] - Unreleased

### Добавлено
- **Система провайдеров сохранения (SaveProvider)**: Новая профессиональная система сохранения данных
  - Статический класс `SaveProvider` с API аналогичным PlayerPrefs
  - Интерфейс `ISaveProvider` для создания собственных провайдеров
  - Реализованные провайдеры: `PlayerPrefsSaveProvider` (по умолчанию) и `FileSaveProvider` (JSON файлы)
  - ScriptableObject `SaveProviderSettings` для настройки через Inspector
  - Автоматическая инициализация с PlayerPrefs по умолчанию
  - Поддержка событий (OnDataSaved, OnDataLoaded, OnKeyChanged)
  - Расширения для работы с массивами
- **VisualToggle**: Полностью переработан и улучшен
  - Добавлена поддержка UnityEvent (On, Off, OnValueChanged)
  - Добавлены кнопки в Inspector для тестирования
  - Улучшена интеграция с Toggle компонентом
  - Добавлена защита от рекурсии при работе с Toggle
  - Добавлена опция `setOnAwake` для автоматического вызова событий текущего состояния при старте
  - Расширенное API с методами Toggle(), SetActive(), SetInactive()
  - Полная XML документация
  - Свойство IsActive для получения/установки состояния

### Улучшено
- **SaveManager**: Теперь использует систему провайдеров вместо прямых вызовов PlayerPrefs
- **GlobalSave**: Интегрирован с системой провайдеров для единообразной работы
- **Все компоненты сохранения**: Рефакторинг для использования SaveProvider
  - Money, ScoreManager, TimeReward, Collection, Box, Map, Leaderboard, Shop
- **VisualToggle**: Теперь объединяет возможности ToggleView и старого VisualToggle
  - Поддержка множественных Image, цветов, текста и GameObject'ов
  - Автоматическое сохранение начальных значений
  - Улучшенная обработка событий

### Удалено
- **ToggleView**: Удален, полностью заменен улучшенным VisualToggle
  - Все возможности перенесены в VisualToggle
  - StarView обновлен для использования VisualToggle

---

## [5.3.3] - Unreleased

### Улучшено
- **KeyboardMover**: Поддержка 3D - выбор плоскости (XY/XZ/YZ)
- **MouseMover2D**: Расширенный AxisMask для 3D (XZ, YZ, Z)
- **Follow**: Полная переработка
  - Режимы сглаживания: MoveTowards, Lerp, SmoothDamp, Exponential
  - Distance Control, Deadzone, публичный API
- **CameraConstraint**: Типы границ (SpriteRenderer/Collider2D/Collider/Manual), 3D камеры
- **DistanceChecker**: FixedInterval режим, ContinuousTracking, оптимизация
- **InteractiveObject**: useMouseInteraction, useKeyboardInteraction, контроль дистанции
- **AiNavigation**: Патрулирование и Combined режим
  - walkSpeed/runSpeed, SetRunning()
  - Режимы: FollowTarget, Patrol, Combined
  - Combined: aggroDistance, maxFollowDistance
  - События патруля, публичный API

### Исправлено
- **Follow**: Некорректный Lerp, Time.smoothDeltaTime, проверки лимитов
- **AiNavigation**: Краш "GetRemainingDistance", проверки isOnNavMesh
- **FindAndRemoveMissingScriptsWindow**: Утечка памяти

### Удалено
- **MoveController**: Неиспользуемый компонент

### Производительность
- sqrMagnitude в DistanceChecker, AiNavigation (~20-30%)
- Кеширование хешей Animator в AiNavigation (~10-15%)

---

## [5.3.2] 

---

## [5.3.1]

### Добавлено
- **DrunkardGame**: Готовая игра "Пьяница" с no-code настройкой
  - События счёта карт, победы, раундов, войны
  - Поддержка HandComponent и BoardComponent
  - Параметр `Player Goes First` для порядка ходов
  - Анимация возврата карт в руку
- **DeckConfig**: `GameDeckType` — выбор количества карт в игре (36/52/54)
- **HandComponent**: Событие `OnCardCountChanged(int)`, методы `DrawFirst()/DrawRandom()`, параметр `Add To Bottom`
- **CardComponent**: Методы `UpdateOriginalTransform()`, `ResetHover()`
- **TypewriterEffectComponent**: Метод `Play()` теперь опционально берёт текст из TMP_Text если параметр пустой
- **CustomEditor**: Анимированная радужная линия слева (настройки через Tools → Neoxider → Visual Settings)

### Улучшено
- **CardComponent**: Hover Scale как дельта (0.1 = +10%)
- **HandComponent**: Симметричная раскладка веером, анимация добавления карт

### Исправлено
- **CardComponent**: Искажение масштаба после FlipAsync, hover при частых кликах
- **HandComponent**: NullReferenceException при вызове до Awake
- **DrunkardGame**: Карты войны не исчезают, остаются на столе
- **CustomEditorBase**: OnValidate только в Edit Mode

---

## [5.3.0]

### Добавлено
- **Cards**: Новый модуль карточных игр в MVP архитектуре
  - `CardData` — неизменяемая структура карты с поддержкой сравнения и козырей
  - `DeckModel`, `HandModel` — модели колоды и руки
  - `CardView`, `DeckView`, `HandView` — визуальные компоненты с анимациями DOTween
  - `CardPresenter`, `DeckPresenter`, `HandPresenter` — презентеры MVP
  - `DeckConfig` — ScriptableObject со спрайтами по мастям и кастомным редактором
  - No-code компоненты: `CardComponent`, `DeckComponent`, `HandComponent`, `BoardComponent`
  - Типы колод: 36, 52, 54 карты
  - Раскладки руки: Fan, Line, Stack, Grid
  - Методы сравнения: `Beats()`, `CanCover()` для игры «Дурак»
  - **Poker**: подмодуль покерных комбинаций
    - `PokerCombination` — от HighCard до RoyalFlush
    - `PokerHandEvaluator` — определение комбинации из 5-7 карт
    - `PokerRules` — сравнение рук, определение победителей, Texas Hold'em

---

(Previous versions...)

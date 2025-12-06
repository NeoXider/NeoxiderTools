# Changelog

All notable changes to this project will be documented in this file.

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

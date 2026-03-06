# План документации по модулям

**Статус:** Частично выполнено. Полностью готовы: Animations, Audio, Condition, Tools/Components, Tools/Time (XML EN, Inspector EN, Docs RU с «См. также», DocsEn README + страницы компонентов). В Save добавлен «См. также» в README. Остальные модули: DocsEn README созданы; XML/Inspector и полные Docs RU/DocsEn — по мере обновления от скриптов.

Цель: привести документацию к единому стилю ([Оформление_документации.md](./Оформление_документации.md)) по **четырём видам**:

1. **XML в коде** (English) — `/// <summary>`, `<param>`, `<returns>`, `<remarks>` для публичных типов и членов.
2. **Inspector** (English) — `[Tooltip]`, `[Header]` у всех сериализуемых полей.
3. **Docs** (русский) — .md в `Assets/Neoxider/Docs/`, ссылка `[NeoDoc("...")]`; назначение, поля, API, примеры, «См. также».
4. **DocsEn** (English) — .md в `Assets/Neoxider/DocsEn/` с той же структурой путей, что в `Docs/`.

Для каждого модуля ниже — четыре задачи (галочки обновлять по мере выполнения).

---

## Важно: источник истины — скрипты

**Документацию нужно переделывать на основе самих скриптов, а не переписывать существующие .md.**

- **Источник истины** — текущий код (поля, методы, сигнатуры, атрибуты). Сначала читаем скрипты модуля, затем пишем/обновляем XML, Tooltip/Header и .md.
- **Существующие .md часто устарели** — не опираться на них как на эталон. Проверять по коду: какие поля/методы/события есть сейчас, какие переименованы или удалены.
- Порядок работы по модулю: (1) изучить скрипты → (2) добавить/поправить XML и Inspector в коде → (3) написать/обновить Docs (RU) по актуальному API → (4) создать/обновить DocsEn (EN).

---

## Список модулей

### Верхний уровень (вне Tools)

| Модуль | Путь Scripts | Путь Docs |
|--------|--------------|-----------|
| Animations | `Scripts/Animations/` | `Docs/Animations/` |
| Audio | `Scripts/Audio/` | `Docs/Audio/` |
| Bonus | `Scripts/Bonus/` | `Docs/Bonus/` |
| Cards | `Scripts/Cards/` | `Docs/Cards/` |
| Condition | `Scripts/Condition/` | `Docs/Condition/` |
| Editor | `Editor/` | `Docs/Editor/` |
| Extensions | `Scripts/Extensions/` | `Docs/Extensions/` |
| GridSystem | `Scripts/GridSystem/` | `Docs/` (GridSystem*.md) |
| Level | `Scripts/Level/` | `Docs/Level/` |
| NPC | `Scripts/NPC/` | `Docs/NPC/` |
| Parallax | `Scripts/Parallax/` | `Docs/Parallax/` |
| PropertyAttribute | `Scripts/PropertyAttribute/`, `Editor/` | `Docs/PropertyAttribute/` |
| Save | `Scripts/Save/` | `Docs/Save/` |
| Shop | `Scripts/Shop/` | `Docs/Shop/` |
| StateMachine | `Scripts/StateMachine/` | `Docs/StateMachine/` |
| UI | `Scripts/UI/` | `Docs/UI/` |
| UI Extension | (префабы/ассеты) | `Docs/UI Extension/` |
| **NeoxiderPages** | `Samples~/NeoxiderPages/Runtime/Scripts/` | `Docs/NeoxiderPages/` |

### Tools — корень и подмодули

| Подмодуль | Путь Scripts | Путь Docs |
|-----------|--------------|-----------|
| Tools (корень) | `Scripts/Tools/` (CameraAspectRatioScaler, UpdateChilds и др.) | `Docs/Tools/` |
| Tools/Components | `Scripts/Tools/Components/` (Counter, AnimatorParameterDriver, ScoreManager, TypewriterEffect, Loot, UnityLifecycleEvents и др.) | `Docs/Tools/Components/` |
| Tools/Components/AttackSystem | `Scripts/Tools/Components/AttackSystem/` | `Docs/Tools/Components/AttackSystem/` |
| Tools/Components/Interface | `Scripts/Tools/Components/Interface/` | `Docs/Tools/Components/Interface/` |
| Tools/Debug | `Scripts/Tools/Debug/` | `Docs/Tools/Debug/` |
| Tools/Dialogue | `Scripts/Tools/Dialogue/` | `Docs/Tools/Dialogue/` |
| Tools/Draw | `Scripts/Tools/Draw/` | `Docs/Tools/Draw/` |
| Tools/FakeLeaderboard | `Scripts/Tools/FakeLeaderboard/` | `Docs/Tools/FakeLeaderboard/` |
| Tools/Input | `Scripts/Tools/Input/` | `Docs/Tools/Input/` |
| Tools/InteractableObject | `Scripts/Tools/InteractableObject/` | `Docs/Tools/InteractableObject/` |
| Tools/Inventory | `Scripts/Tools/Inventory/` | `Docs/Tools/Inventory/` |
| Tools/Managers | `Scripts/Tools/Managers/` | `Docs/Tools/Managers/` |
| Tools/Move | `Scripts/Tools/Move/` | `Docs/Tools/Move/` |
| Tools/Move/MovementToolkit | `Scripts/Tools/Move/MovementToolkit/` | `Docs/Tools/Move/MovementToolkit/` |
| Tools/Other | `Scripts/Tools/Other/` | `Docs/Tools/Other/` |
| Tools/Physics | `Scripts/Tools/Physics/` | `Docs/Tools/Physics/` |
| Tools/Random | `Scripts/Tools/Random/` | `Docs/Tools/Random/` |
| Tools/Spawner | `Scripts/Tools/Spawner/` | `Docs/Tools/Spawner/` |
| Tools/Text | `Scripts/Tools/Text/` | `Docs/Tools/Text/` |
| Tools/Time | `Scripts/Tools/Time/` | `Docs/Tools/Time/` |
| Tools/View | `Scripts/Tools/View/` | `Docs/Tools/View/` |

### Bonus — подмодули

| Подмодуль | Путь Docs |
|-----------|-----------|
| Bonus/Collection | `Docs/Bonus/Collection/` |
| Bonus/Slot | `Docs/Bonus/Slot/` |
| Bonus/Slot/Data | `Docs/Bonus/Slot/Data/` |
| Bonus/TimeReward | `Docs/Bonus/TimeReward/` |
| Bonus/WheelFortune | `Docs/Bonus/WheelFortune/` |

---

## TODO по модулям

Формат: для каждого модуля — 4 пункта (XML EN, Inspector EN, Docs RU, DocsEn EN). В скобках — что входит в модуль (ключевые скрипты/страницы).

---

### Animations

- [x] **XML (EN)** — все публичные типы и члены в `Scripts/Animations/` (Vector3Animator, FloatAnimator, ColorAnimator и др.).
- [x] **Inspector (EN)** — Tooltip/Header у всех полей компонентов.
- [x] **Docs (RU)** — README + страницы по компонентам в `Docs/Animations/`, добавлен блок «См. также» в Vector3Animator.
- [x] **DocsEn (EN)** — README + Vector3Animator.md, FloatAnimator.md, ColorAnimator.md.

---

### Audio

- [x] **XML (EN)** — AM, AMSettings, PlayAudio, PlayAudioBtn, SettingMixer, AudioControl, RandomMusicController.
- [x] **Inspector (EN)** — Tooltip/Header приведены к EN (SettingMixer, AudioControl и др.).
- [ ] **Docs (RU)** — добавить «См. также» в ключевые .md по мере обновления от скриптов.
- [x] **DocsEn (EN)** — README с перечнем скриптов и ссылками.

---

### Bonus

- [ ] **XML (EN)** — все скрипты в Bonus (LineRoulett, Collection, Slot, TimeReward, WheelFortune и подмодули).
- [ ] **Inspector (EN)** — Tooltip/Header по всем компонентам.
- [ ] **Docs (RU)** — `Docs/Bonus/` + подпапки Collection, Slot, Slot/Data, TimeReward, WheelFortune.
- [ ] **DocsEn (EN)** — `DocsEn/Bonus/` с той же структурой.

---

### Cards

- [ ] **XML (EN)** — CardComponent, DeckComponent, HandComponent, CardView, HandView, Poker, Drunkard и др.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Cards/`, View, Poker, Examples.
- [ ] **DocsEn (EN)** — `DocsEn/Cards/`.

---

### Condition

- [x] **XML (EN)** — NeoCondition, LogicMode, CheckMode, все публичные члены.
- [x] **Inspector (EN)** — уже EN.
- [ ] **Docs (RU)** — при обновлении от скриптов добавлять «См. также».
- [x] **DocsEn (EN)** — README с Quick start и API.

---

### Editor

- [ ] **XML (EN)** — скрипты в `Editor/` (Scene Saver, CustomEditor, утилиты и т.д.).
- [ ] **Inspector (EN)** — где применимо (настройки окон/компонентов редактора).
- [ ] **Docs (RU)** — `Docs/Editor/`.
- [ ] **DocsEn (EN)** — `DocsEn/Editor/`.

---

### Extensions

- [ ] **XML (EN)** — все extension-классы (уже в основном EN); единообразие summary/param/returns.
- [ ] **Inspector (EN)** — не применимо (только статические методы).
- [ ] **Docs (RU)** — `Docs/Extensions/` (README + выборочные страницы по группам методов).
- [ ] **DocsEn (EN)** — `DocsEn/Extensions/`.

---

### GridSystem

- [ ] **XML (EN)** — FieldGenerator, FieldSpawner, FieldObjectSpawner, FieldDebugDrawer, Match3, TicTacToe и др.
- [ ] **Inspector (EN)** — все поля компонентов.
- [ ] **Docs (RU)** — страницы в `Docs/` (GridSystem, Match3, TicTacToe и подпапки).
- [ ] **DocsEn (EN)** — `DocsEn/` с теми же путями.

---

### Level

- [ ] **XML (EN)** — LevelManager, SceneFlowController, LevelButton, TextLevel и др.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Level/`.
- [ ] **DocsEn (EN)** — `DocsEn/Level/`.

---

### NPC

- [ ] **XML (EN)** — NpcAnimatorDriver, NpcNavigation и скрипты навигации.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/NPC/`, `Docs/NPC/Navigation/`.
- [ ] **DocsEn (EN)** — `DocsEn/NPC/`.

---

### Parallax

- [ ] **XML (EN)** — ParallaxLayer и связанные.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Parallax/`.
- [ ] **DocsEn (EN)** — `DocsEn/Parallax/`.

---

### PropertyAttribute

- [ ] **XML (EN)** — атрибуты и drawer’ы в Scripts и Editor.
- [ ] **Inspector (EN)** — где отображаются в инспекторе (описание атрибутов).
- [ ] **Docs (RU)** — `Docs/PropertyAttribute/`.
- [ ] **DocsEn (EN)** — `DocsEn/PropertyAttribute/`.

---

### Save

- [ ] **XML (EN)** — SaveManager, SaveableBehaviour, SaveField, SaveProvider, PlayerData и др.
- [ ] **Inspector (EN)** — все поля.
- [x] **Docs (RU)** — в README добавлен блок «См. также».
- [x] **DocsEn (EN)** — README создан ранее.

---

### Shop

- [ ] **XML (EN)** — Shop, ShopItem, Money, ButtonPrice и др.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Shop/`.
- [ ] **DocsEn (EN)** — `DocsEn/Shop/`.

---

### StateMachine

- [ ] **XML (EN)** — StateMachineBehaviour, StateMachineBehaviourBase, StateTransition, NoCode-редактор и др.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/StateMachine/`.
- [ ] **DocsEn (EN)** — `DocsEn/StateMachine/`.

---

### UI

- [ ] **XML (EN)** — AnchorMove, ButtonShake, UIReady, PausePage, VisualToggle, VariantView, Simple/UI, ButtonChangePage, AnimationFly и др.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/UI/`.
- [ ] **DocsEn (EN)** — `DocsEn/UI/`.

---

### UI Extension

- [ ] **XML (EN)** — если есть скрипты; иначе только описание префабов.
- [ ] **Inspector (EN)** — по необходимости.
- [ ] **Docs (RU)** — `Docs/UI Extension/`.
- [ ] **DocsEn (EN)** — `DocsEn/UI Extension/`.

---

### NeoxiderPages

- [ ] **XML (EN)** — PM, UIPage, BtnChangePage, FakeLoad, PageSubscriber и др. в `Samples~/NeoxiderPages/Runtime/Scripts/`.
- [ ] **Inspector (EN)** — все поля компонентов страниц.
- [ ] **Docs (RU)** — `Docs/NeoxiderPages/` (README + страницы по компонентам).
- [ ] **DocsEn (EN)** — `DocsEn/NeoxiderPages/`.

---

### Tools (корень)

Компоненты в `Scripts/Tools/` без подпапки: CameraAspectRatioScaler, UpdateChilds, DistanceChecker (если в корне) и т.д.

- [ ] **XML (EN)** — все такие скрипты.
- [ ] **Inspector (EN)** — Tooltip/Header.
- [ ] **Docs (RU)** — соответствующие .md в `Docs/Tools/`.
- [ ] **DocsEn (EN)** — `DocsEn/Tools/`.

---

### Tools/Components

Counter, AnimatorParameterDriver, ScoreManager, TypewriterEffect, Loot, UnityLifecycleEvents и др. (без AttackSystem и Interface).

- [x] **XML (EN)** — AnimatorParameterDriver, Counter (полностью).
- [x] **Inspector (EN)** — AnimatorParameterDriver, Counter — Tooltip/Header EN.
- [ ] **Docs (RU)** — добавлять «См. также» в страницы компонентов при обновлении от скриптов.
- [x] **DocsEn (EN)** — README + ссылки на компоненты.

---

### Tools/Components/AttackSystem

Health, Evade, AttackExecution, AdvancedAttackCollider и др.

- [ ] **XML (EN)** — все классы в `Scripts/Tools/Components/AttackSystem/`.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Tools/Components/AttackSystem/`.
- [ ] **DocsEn (EN)** — `DocsEn/Tools/Components/AttackSystem/`.

---

### Tools/Components/Interface

InterfaceAttack и связанные интерфейсы/реализации.

- [ ] **XML (EN)** — классы в `Scripts/Tools/Components/Interface/`.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Tools/Components/Interface/`.
- [ ] **DocsEn (EN)** — `DocsEn/Tools/Components/Interface/`.

---

### Tools/Debug

FPS, ErrorLogger и др.

- [ ] **XML (EN)** — все классы в `Scripts/Tools/Debug/`.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Tools/Debug/`.
- [ ] **DocsEn (EN)** — `DocsEn/Tools/Debug/`.

---

### Tools/Dialogue

DialogueController, DialogueUI, DialogueData и др.

- [ ] **XML (EN)** — все классы в `Scripts/Tools/Dialogue/`.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Tools/Dialogue/`.
- [ ] **DocsEn (EN)** — `DocsEn/Tools/Dialogue/`.

---

### Tools/Draw

Drawer и связанные.

- [ ] **XML (EN)** — классы в `Scripts/Tools/Draw/`.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Tools/Draw/`.
- [ ] **DocsEn (EN)** — `DocsEn/Tools/Draw/`.

---

### Tools/FakeLeaderboard

Leaderboard, LeaderboardItem, LeaderboardMove.

- [ ] **XML (EN)** — все классы в `Scripts/Tools/FakeLeaderboard/`.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Tools/FakeLeaderboard/`.
- [ ] **DocsEn (EN)** — `DocsEn/Tools/FakeLeaderboard/`.

---

### Tools/Input

SwipeController, MultiKeyEventTrigger, MouseInputManager, MouseEffect и др.

- [ ] **XML (EN)** — все классы в `Scripts/Tools/Input/`.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Tools/Input/`.
- [ ] **DocsEn (EN)** — `DocsEn/Tools/Input/`.

---

### Tools/InteractableObject

InteractiveObject, PhysicsEvents2D, PhysicsEvents3D, ToggleObject.

- [ ] **XML (EN)** — все классы в `Scripts/Tools/InteractableObject/`.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Tools/InteractableObject/`.
- [ ] **DocsEn (EN)** — `DocsEn/Tools/InteractableObject/`.

---

### Tools/Inventory

InventoryComponent, PickableItem, InventoryHand, InventoryView, InventoryDropper, InventoryTotalCountText, InventoryPickupBridge и др.

- [ ] **XML (EN)** — все классы в `Scripts/Tools/Inventory/`.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Tools/Inventory/`.
- [ ] **DocsEn (EN)** — `DocsEn/Tools/Inventory/`.

---

### Tools/Managers

GM, EM, Bootstrap, Singleton, SingletonById и др.

- [ ] **XML (EN)** — все классы в `Scripts/Tools/Managers/`.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Tools/Managers/`.
- [ ] **DocsEn (EN)** — `DocsEn/Tools/Managers/`.

---

### Tools/Move

Follow, AdvancedForceApplier, CameraRotationController, PlayerController2DPhysics, PlayerController3DAnimatorDriver, ScreenPositioner, DistanceChecker, CursorLockController, UniversalRotator и др. (без MovementToolkit).

- [ ] **XML (EN)** — все классы в `Scripts/Tools/Move/` кроме MovementToolkit.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Tools/Move/`.
- [ ] **DocsEn (EN)** — `DocsEn/Tools/Move/`.

---

### Tools/Move/MovementToolkit

KeyboardMover, MouseMover2D, MouseMover3D, ConstantRotator и др.

- [ ] **XML (EN)** — все классы в `Scripts/Tools/Move/MovementToolkit/`.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Tools/Move/MovementToolkit/`.
- [ ] **DocsEn (EN)** — `DocsEn/Tools/Move/MovementToolkit/`.

---

### Tools/Other

CameraShake, AiNavigation, SpineController, RevertAmount и др.

- [ ] **XML (EN)** — все классы в `Scripts/Tools/Other/`.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Tools/Other/`.
- [ ] **DocsEn (EN)** — `DocsEn/Tools/Other/`.

---

### Tools/Physics

ExplosiveForce, ImpulseZone, MagneticField и др.

- [ ] **XML (EN)** — все классы в `Scripts/Tools/Physics/`.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Tools/Physics/`.
- [ ] **DocsEn (EN)** — `DocsEn/Tools/Physics/`.

---

### Tools/Random

ChanceSystemBehaviour, ChanceManager и Data.

- [ ] **XML (EN)** — все классы в `Scripts/Tools/Random/` (включая Data).
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Tools/Random/`, `Docs/Tools/Random/Data/`.
- [ ] **DocsEn (EN)** — `DocsEn/Tools/Random/`.

---

### Tools/Spawner

Spawner, SimpleSpawner, Despawner, PoolManager, PoolableBehaviour и др.

- [ ] **XML (EN)** — все классы в `Scripts/Tools/Spawner/`.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Tools/Spawner/`.
- [ ] **DocsEn (EN)** — `DocsEn/Tools/Spawner/`.

---

### Tools/Text

SetText, TimeToText и др.

- [ ] **XML (EN)** — все классы в `Scripts/Tools/Text/`.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Tools/Text/`.
- [ ] **DocsEn (EN)** — `DocsEn/Tools/Text/`.

---

### Tools/Time

Timer, TimerObject и др.

- [x] **XML (EN)** — TimerObject, TimerSaveMode (исправлен TimeValue).
- [x] **Inspector (EN)** — уже EN в TimerObject.
- [x] **Docs (RU)** — в TimerObject.md добавлен блок «См. также».
- [x] **DocsEn (EN)** — README + TimerObject.md.

---

### Tools/View

ImageFillAmountAnimator, BillboardUniversal, Selector, StarView, LightAnimator, MeshEmission, ZPositionAdjuster и др.

- [ ] **XML (EN)** — все классы в `Scripts/Tools/View/`.
- [ ] **Inspector (EN)** — все поля.
- [ ] **Docs (RU)** — `Docs/Tools/View/`.
- [ ] **DocsEn (EN)** — `DocsEn/Tools/View/`.

---

## Порядок выполнения (рекомендуемый)

1. Небольшие модули с уже частичной документацией: **Condition**, **Tools/Components** (AnimatorParameterDriver, Counter).
2. Часто используемые: **Tools/Time** (TimerObject), **Tools/Spawner**, **Save**, **UI**.
3. **NeoxiderPages** — отдельно, т.к. в Samples.
4. Остальные модули и подмодули Tools по приоритету использования.
5. **Extensions**, **Editor** — объёмные, можно разбивать по файлам/группам.

После выполнения блока можно отмечать галочки в этом файле.

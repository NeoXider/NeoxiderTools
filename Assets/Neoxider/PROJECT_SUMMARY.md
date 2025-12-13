# NeoxiderTools — краткий PROJECT_SUMMARY

## Архитектура и структура

- **UPM пакет**: `Assets/Neoxider/package.json` (текущая версия: **5.4.3**)
- **Unity**: 2022.1+
- **Основной namespace**: `Neo` (далее `Neo.Tools.*`, `Neo.UI.*`, `Neo.Save.*`, `Neo.Cards.*` и т.д.)
- **Модульность**: модули изолированы через `.asmdef` (см. `Assets/Neoxider/Scripts/**/Neo.*.asmdef` и `Assets/Neoxider/Editor/Neo.Editor.asmdef`)
- **Документация**: `Assets/Neoxider/Docs/**`
- **Опциональные модули**: `Assets/NeoxiderPages/**` (PageManager / `Neo.Pages`, отдельные asmdef + свои `Docs/`)

Структура:

```text
Assets/Neoxider/
  Scripts/   # runtime + часть editor-скриптов в подпапках Editor
  Editor/    # editor tools (окна/утилиты/инспектор)
  Docs/      # документация по модулям/компонентам
  Demo/      # примеры
  Prefabs/   # готовые префабы

Assets/NeoxiderPages/
  Runtime/   # runtime модуль страниц (Neo.Pages)
  Editor/    # editor инструменты (Neo.Pages.Editor)
  Prefabs/   # демо префабы PageManager
  Scenes/    # демо сцены PageManager
  Docs/      # документация NeoxiderPages
```

## Правила работы (важно)

- **Сначала переиспользуй** готовые компоненты из `Assets/Neoxider/Scripts/**` (особенно `Tools/*`, `Save/*`, `UI/*`, `Extensions/*`).
- **Не создавай дубликаты**: если нужная функция близка — расширяй существующий компонент/модуль и обновляй доки.
- **Для UI/No‑Code**: предпочитай `MonoBehaviour` + `UnityEvent` (подписки через Inspector).
- **Для данных**: предпочитай `ScriptableObject`.
- **Для Editor‑функций**: код в `Assets/Neoxider/Editor/**` или `Scripts/**/Editor/**` + правильные asmdef ссылки.
- **После изменений**: обнови соответствующий `.md` в `Docs/` и запись в `Assets/Neoxider/CHANGELOG.md`.

## Каталог скриптов (все `.cs`)

Формат: `путь — кратко что это`.

### Animations (`Assets/Neoxider/Scripts/Animations/`)

- `Assets/Neoxider/Scripts/Animations/AnimationType.cs` — enum типов анимации значений.
- `Assets/Neoxider/Scripts/Animations/AnimationUtils.cs` — утилиты анимации/интерполяции.
- `Assets/Neoxider/Scripts/Animations/ColorAnimator.cs` — компонент анимации цвета.
- `Assets/Neoxider/Scripts/Animations/FloatAnimator.cs` — компонент анимации float.
- `Assets/Neoxider/Scripts/Animations/Vector3Animator.cs` — компонент анимации Vector3.

### Audio (`Assets/Neoxider/Scripts/Audio/`)

- `Assets/Neoxider/Scripts/Audio/AMSettings.cs` — настройки аудио-системы.
- `Assets/Neoxider/Scripts/Audio/RandomMusicController.cs` — контроллер случайной музыки.
- `Assets/Neoxider/Scripts/Audio/SettingMixer.cs` — управление AudioMixer (громкости/параметры).
- `Assets/Neoxider/Scripts/Audio/AudioSimple/AM.cs` — упрощенный AudioManager.
- `Assets/Neoxider/Scripts/Audio/AudioSimple/PlayAudio.cs` — проигрывание клипа/звука.
- `Assets/Neoxider/Scripts/Audio/AudioSimple/PlayAudioBtn.cs` — проигрывание звука по кнопке/событию.
- `Assets/Neoxider/Scripts/Audio/View/AudioControl.cs` — UI контроль аудио.

### Bonus (`Assets/Neoxider/Scripts/Bonus/`)

- `Assets/Neoxider/Scripts/Bonus/LineRoulett.cs` — линейная рулетка.

#### Bonus/Collection

- `Assets/Neoxider/Scripts/Bonus/Collection/Box.cs` — контейнер/визуал бокса.
- `Assets/Neoxider/Scripts/Bonus/Collection/Collection.cs` — система коллекций.
- `Assets/Neoxider/Scripts/Bonus/Collection/CollectionVisualManager.cs` — визуализация коллекций.
- `Assets/Neoxider/Scripts/Bonus/Collection/ItemCollection.cs` — элемент коллекции.
- `Assets/Neoxider/Scripts/Bonus/Collection/ItemCollectionData.cs` — данные коллекции.
- `Assets/Neoxider/Scripts/Bonus/Collection/ItemCollectionInfo.cs` — инфо/метаданные коллекции.

#### Bonus/Slot

- `Assets/Neoxider/Scripts/Bonus/Slot/CheckSpin.cs` — проверка результата/комбинаций.
- `Assets/Neoxider/Scripts/Bonus/Slot/Row.cs` — ряд слот-машины.
- `Assets/Neoxider/Scripts/Bonus/Slot/SlotElement.cs` — элемент слота.
- `Assets/Neoxider/Scripts/Bonus/Slot/SpeedControll.cs` — управление скоростью.
- `Assets/Neoxider/Scripts/Bonus/Slot/SpinController.cs` — контроллер слот-машины.
- `Assets/Neoxider/Scripts/Bonus/Slot/VisualSlotLines.cs` — визуал линий выигрыша.
- `Assets/Neoxider/Scripts/Bonus/Slot/Data/BetsData.cs` — данные ставок.
- `Assets/Neoxider/Scripts/Bonus/Slot/Data/LinesData.cs` — данные линий.
- `Assets/Neoxider/Scripts/Bonus/Slot/Data/SpriteMultiplayerData.cs` — данные множителей.
- `Assets/Neoxider/Scripts/Bonus/Slot/Data/SpritesData.cs` — данные спрайтов.

#### Bonus/TimeReward

- `Assets/Neoxider/Scripts/Bonus/TimeReward/TimeReward.cs` — награда по времени.

#### Bonus/WheelFortune

- `Assets/Neoxider/Scripts/Bonus/WheelFortune/WheelFortune.cs` — колесо фортуны.
- `Assets/Neoxider/Scripts/Bonus/WheelFortune/WheelMoneyWin.cs` — обработка выигрыша.

### Cards (`Assets/Neoxider/Scripts/Cards/`) — модуль карточных игр (MVP + компоненты)

#### Cards/Core

- `Assets/Neoxider/Scripts/Cards/Core/Data/CardData.cs` — данные карты.
- `Assets/Neoxider/Scripts/Cards/Core/Enums/CardLocation.cs` — enum расположения карты.
- `Assets/Neoxider/Scripts/Cards/Core/Enums/DeckType.cs` — enum типа колоды.
- `Assets/Neoxider/Scripts/Cards/Core/Enums/Rank.cs` — enum ранга.
- `Assets/Neoxider/Scripts/Cards/Core/Enums/Suit.cs` — enum масти.
- `Assets/Neoxider/Scripts/Cards/Core/Interfaces/ICardContainer.cs` — интерфейс контейнера карт.
- `Assets/Neoxider/Scripts/Cards/Core/Interfaces/ICardView.cs` — интерфейс view карты.
- `Assets/Neoxider/Scripts/Cards/Core/Interfaces/IDeckView.cs` — интерфейс view колоды.
- `Assets/Neoxider/Scripts/Cards/Core/Interfaces/IHandView.cs` — интерфейс view руки.
- `Assets/Neoxider/Scripts/Cards/Core/Model/CardContainerModel.cs` — базовая модель контейнера.

#### Cards/Model

- `Assets/Neoxider/Scripts/Cards/Model/BoardModel.cs` — модель борда.
- `Assets/Neoxider/Scripts/Cards/Model/DeckModel.cs` — модель колоды.
- `Assets/Neoxider/Scripts/Cards/Model/HandModel.cs` — модель руки.

#### Cards/View

- `Assets/Neoxider/Scripts/Cards/View/CardView.cs` — визуал карты.
- `Assets/Neoxider/Scripts/Cards/View/DeckView.cs` — визуал колоды.
- `Assets/Neoxider/Scripts/Cards/View/HandView.cs` — визуал руки.

#### Cards/Presenter

- `Assets/Neoxider/Scripts/Cards/Presenter/CardPresenter.cs` — presenter карты.
- `Assets/Neoxider/Scripts/Cards/Presenter/DeckPresenter.cs` — presenter колоды.
- `Assets/Neoxider/Scripts/Cards/Presenter/HandPresenter.cs` — presenter руки.

#### Cards/Components

- `Assets/Neoxider/Scripts/Cards/Components/BoardComponent.cs` — компонент борда.
- `Assets/Neoxider/Scripts/Cards/Components/CardComponent.cs` — компонент карты.
- `Assets/Neoxider/Scripts/Cards/Components/DeckComponent.cs` — компонент колоды.
- `Assets/Neoxider/Scripts/Cards/Components/HandComponent.cs` — компонент руки.

#### Cards/Config

- `Assets/Neoxider/Scripts/Cards/Config/DeckConfig.cs` — конфиг колоды.
- `Assets/Neoxider/Scripts/Cards/Config/HandLayoutType.cs` — enum раскладки.

#### Cards/Poker

- `Assets/Neoxider/Scripts/Cards/Poker/PokerCombination.cs` — enum комбинаций.
- `Assets/Neoxider/Scripts/Cards/Poker/PokerHandEvaluator.cs` — оценка комбинаций.
- `Assets/Neoxider/Scripts/Cards/Poker/PokerHandResult.cs` — результат оценки.
- `Assets/Neoxider/Scripts/Cards/Poker/PokerRules.cs` — правила.

#### Cards/Utils

- `Assets/Neoxider/Scripts/Cards/Utils/CardComparer.cs` — сравнение карт.

#### Cards/Drunkard

- `Assets/Neoxider/Scripts/Cards/Drunkard/DrunkardGame.cs` — игра “Пьяница”.

#### Cards/Editor

- `Assets/Neoxider/Scripts/Cards/Editor/DeckConfigEditor.cs` — редактор/инспектор DeckConfig.

### Extensions (`Assets/Neoxider/Scripts/Extensions/`)

- `Assets/Neoxider/Scripts/Extensions/AudioExtensions.cs` — расширения аудио.
- `Assets/Neoxider/Scripts/Extensions/ColorExtension.cs` — расширения Color.
- `Assets/Neoxider/Scripts/Extensions/ComponentExtensions.cs` — расширения Component.
- `Assets/Neoxider/Scripts/Extensions/CoroutineExtensions.cs` — расширения корутин.
- `Assets/Neoxider/Scripts/Extensions/DebugGizmos.cs` — debug gizmos helpers.
- `Assets/Neoxider/Scripts/Extensions/EnumerableExtensions.cs` — расширения коллекций.
- `Assets/Neoxider/Scripts/Extensions/Enums.cs` — общие enum’ы.
- `Assets/Neoxider/Scripts/Extensions/GameObjectArrayExtensions.cs` — расширения массивов GameObject.
- `Assets/Neoxider/Scripts/Extensions/LayoutExtensions.cs` — расширения layout.
- `Assets/Neoxider/Scripts/Extensions/LayoutUtils.cs` — утилиты layout.
- `Assets/Neoxider/Scripts/Extensions/ObjectExtensions.cs` — расширения object/UnityEngine.Object.
- `Assets/Neoxider/Scripts/Extensions/PlayerPrefsUtils.cs` — утилиты PlayerPrefs.
- `Assets/Neoxider/Scripts/Extensions/PrimitiveExtensions.cs` — расширения примитивов (в т.ч. FormatTime).
- `Assets/Neoxider/Scripts/Extensions/RandomExtensions.cs` — random helpers.
- `Assets/Neoxider/Scripts/Extensions/RandomShapeExtensions.cs` — random shape helpers.
- `Assets/Neoxider/Scripts/Extensions/ScreenExtensions.cs` — расширения экрана.
- `Assets/Neoxider/Scripts/Extensions/Shapes.cs` — геометрические helpers.
- `Assets/Neoxider/Scripts/Extensions/StringExtension.cs` — расширения строк.
- `Assets/Neoxider/Scripts/Extensions/TransformExtensions.cs` — расширения Transform.
- `Assets/Neoxider/Scripts/Extensions/UIUtils.cs` — UI утилиты.

### GridSystem (`Assets/Neoxider/Scripts/GridSystem/`)

- `Assets/Neoxider/Scripts/GridSystem/FieldCell.cs` — ячейка сетки.
- `Assets/Neoxider/Scripts/GridSystem/FieldDebugDrawer.cs` — debug отрисовка.
- `Assets/Neoxider/Scripts/GridSystem/FieldGenerator.cs` — генерация поля.
- `Assets/Neoxider/Scripts/GridSystem/FieldGeneratorConfig.cs` — конфиг генерации.
- `Assets/Neoxider/Scripts/GridSystem/FieldObjectSpawner.cs` — спавн объектов на поле.
- `Assets/Neoxider/Scripts/GridSystem/FieldSpawner.cs` — спавнер.
- `Assets/Neoxider/Scripts/GridSystem/MovementRule.cs` — правила перемещения.

### Level (`Assets/Neoxider/Scripts/Level/`)

- `Assets/Neoxider/Scripts/Level/LevelButton.cs` — кнопка уровня.
- `Assets/Neoxider/Scripts/Level/LevelManager.cs` — менеджер уровней.
- `Assets/Neoxider/Scripts/Level/Map.cs` — карта уровней.
- `Assets/Neoxider/Scripts/Level/TextLevel.cs` — UI вывод текущего/лучшего уровня (на базе `Neo.Tools.SetText`).

### NPC (`Assets/Neoxider/Scripts/NPC/`)

- `Assets/Neoxider/Scripts/NPC/NpcNavigation.cs` — host компонент модульной навигации.
- `Assets/Neoxider/Scripts/NPC/Navigation/NpcAggroFollowCore.cs` — core агро/преследование.
- `Assets/Neoxider/Scripts/NPC/Navigation/NpcAnimationCore.cs` — core анимации.
- `Assets/Neoxider/Scripts/NPC/Navigation/NpcDestinationResolver.cs` — резолв точки назначения.
- `Assets/Neoxider/Scripts/NPC/Navigation/NpcFollowTargetCore.cs` — core следования.
- `Assets/Neoxider/Scripts/NPC/Navigation/NpcNavAgentCore.cs` — core NavMeshAgent.
- `Assets/Neoxider/Scripts/NPC/Navigation/NpcPatrolCore.cs` — core патруля.

### Parallax (`Assets/Neoxider/Scripts/Parallax/`)

- `Assets/Neoxider/Scripts/Parallax/ParallaxLayer.cs` — слой параллакса.

### Save (`Assets/Neoxider/Scripts/Save/`)

- `Assets/Neoxider/Scripts/Save/ISaveableComponent.cs` — интерфейс сохраняемого компонента.
- `Assets/Neoxider/Scripts/Save/SaveableBehaviour.cs` — базовый MonoBehaviour для сохранений.
- `Assets/Neoxider/Scripts/Save/SaveField.cs` — атрибут автосохранения полей.
- `Assets/Neoxider/Scripts/Save/SaveManager.cs` — менеджер сохранений.
- `Assets/Neoxider/Scripts/Save/SaveProvider.cs` — единый API сохранений (провайдеры).
- `Assets/Neoxider/Scripts/Save/SaveProviderExtensions.cs` — расширения SaveProvider.
- `Assets/Neoxider/Scripts/Save/Example/PlayerData.cs` — пример данных.
- `Assets/Neoxider/Scripts/Save/GlobalSave/GlobalData.cs` — контейнер глобальных данных.
- `Assets/Neoxider/Scripts/Save/GlobalSave/GlobalSave.cs` — глобальное хранилище.
- `Assets/Neoxider/Scripts/Save/Providers/ISaveProvider.cs` — интерфейс провайдера.
- `Assets/Neoxider/Scripts/Save/Providers/PlayerPrefsSaveProvider.cs` — провайдер PlayerPrefs.
- `Assets/Neoxider/Scripts/Save/Providers/FileSaveProvider.cs` — провайдер JSON файлов.
- `Assets/Neoxider/Scripts/Save/Providers/SaveProviderType.cs` — enum типов провайдера.
- `Assets/Neoxider/Scripts/Save/Settings/SaveProviderSettings.cs` — настройки (SO).
- `Assets/Neoxider/Scripts/Save/Settings/SaveProviderSettingsComponent.cs` — обертка настроек (MB).

### Shop (`Assets/Neoxider/Scripts/Shop/`)

- `Assets/Neoxider/Scripts/Shop/ButtonPrice.cs` — UI кнопка цены.
- `Assets/Neoxider/Scripts/Shop/InterfaceMoney.cs` — интерфейс валюты.
- `Assets/Neoxider/Scripts/Shop/Money.cs` — система валюты.
- `Assets/Neoxider/Scripts/Shop/Shop.cs` — контроллер магазина.
- `Assets/Neoxider/Scripts/Shop/ShopItem.cs` — элемент магазина.
- `Assets/Neoxider/Scripts/Shop/ShopItemData.cs` — данные товара.
- `Assets/Neoxider/Scripts/Shop/TextMoney.cs` — UI отображение денег.

### StateMachine (`Assets/Neoxider/Scripts/StateMachine/`)

- `Assets/Neoxider/Scripts/StateMachine/IState.cs` — интерфейс состояния.
- `Assets/Neoxider/Scripts/StateMachine/StateCondition.cs` — базовые условия.
- `Assets/Neoxider/Scripts/StateMachine/StateMachine.cs` — core state machine.
- `Assets/Neoxider/Scripts/StateMachine/StateMachineBehaviour.cs` — MonoBehaviour обертка.
- `Assets/Neoxider/Scripts/StateMachine/StateMachineBehaviourBase.cs` — базовый behaviour.
- `Assets/Neoxider/Scripts/StateMachine/StatePredicate.cs` — предикаты переходов.
- `Assets/Neoxider/Scripts/StateMachine/StateTransition.cs` — переход.
- `Assets/Neoxider/Scripts/StateMachine/NoCode/StateAction.cs` — no-code action.
- `Assets/Neoxider/Scripts/StateMachine/NoCode/StateData.cs` — no-code state (данные).
- `Assets/Neoxider/Scripts/StateMachine/NoCode/StateMachineData.cs` — no-code machine (данные).
- `Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/StateMachineEditor.cs` — editor.
- `Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/StateMachineEditorRegistrar.cs` — регистрация editor.
- `Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/TransitionEditorWindow.cs` — окно редактора переходов.

### Tools (`Assets/Neoxider/Scripts/Tools/`)

#### Tools/Time

- `Assets/Neoxider/Scripts/Tools/Time/Timer.cs` — таймер (класс).
- `Assets/Neoxider/Scripts/Tools/Time/TimerObject.cs` — MonoBehaviour таймер (события/режимы).

#### Tools/View

- `Assets/Neoxider/Scripts/Tools/View/BillboardUniversal.cs` — билборд.
- `Assets/Neoxider/Scripts/Tools/View/DOTweenUIImageFallback.cs` — fallback для DOTween UI.
- `Assets/Neoxider/Scripts/Tools/View/ImageFillAmountAnimator.cs` — аниматор fillAmount.
- `Assets/Neoxider/Scripts/Tools/View/LightAnimator.cs` — аниматор света.
- `Assets/Neoxider/Scripts/Tools/View/MeshEmission.cs` — emission меша.
- `Assets/Neoxider/Scripts/Tools/View/Selector.cs` — селектор объектов/индексов.
- `Assets/Neoxider/Scripts/Tools/View/StarView.cs` — звездный виджет.
- `Assets/Neoxider/Scripts/Tools/View/ZPositionAdjuster.cs` — корректировка Z.

#### Tools/Text

- `Assets/Neoxider/Scripts/Tools/Text/SetText.cs` — установка текста.
- `Assets/Neoxider/Scripts/Tools/Text/TimeToText.cs` — вывод времени в текст.

#### Tools/Physics

- `Assets/Neoxider/Scripts/Tools/Physics/ExplosiveForce.cs` — взрывная сила.
- `Assets/Neoxider/Scripts/Tools/Physics/ImpulseZone.cs` — зона импульса.
- `Assets/Neoxider/Scripts/Tools/Physics/MagneticField.cs` — магнитное поле.

#### Tools/Spawner

- `Assets/Neoxider/Scripts/Tools/Spawner/IPoolable.cs` — интерфейс пула.
- `Assets/Neoxider/Scripts/Tools/Spawner/NeoObjectPool.cs` — пул объектов.
- `Assets/Neoxider/Scripts/Tools/Spawner/PoolManager.cs` — менеджер пулов.
- `Assets/Neoxider/Scripts/Tools/Spawner/PooledObjectInfo.cs` — инфо объекта пула.
- `Assets/Neoxider/Scripts/Tools/Spawner/SimpleSpawner.cs` — простой спавнер.
- `Assets/Neoxider/Scripts/Tools/Spawner/Spawner.cs` — спавнер.

#### Tools/Move

- `Assets/Neoxider/Scripts/Tools/Move/AdvancedForceApplier.cs` — apply force helper.
- `Assets/Neoxider/Scripts/Tools/Move/CameraConstraint.cs` — ограничения камеры.
- `Assets/Neoxider/Scripts/Tools/Move/CameraRotationController.cs` — вращение камеры.
- `Assets/Neoxider/Scripts/Tools/Move/DistanceChecker.cs` — проверка дистанции.
- `Assets/Neoxider/Scripts/Tools/Move/Follow.cs` — следование.
- `Assets/Neoxider/Scripts/Tools/Move/ScreenPositioner.cs` — позиционирование.
- `Assets/Neoxider/Scripts/Tools/Move/UniversalRotator.cs` — вращение.
- `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/ConstantMover.cs` — постоянное движение.
- `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/ConstantRotator.cs` — постоянное вращение.
- `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/DirectionUtils.cs` — утилиты направлений.
- `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/IMover.cs` — интерфейс.
- `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/KeyboardMover.cs` — движение с клавиатуры.
- `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/MouseMover2D.cs` — движение за мышью (2D).
- `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/MouseMover3D.cs` — движение за мышью (3D).

#### Tools/Input

- `Assets/Neoxider/Scripts/Tools/Input/MultiKeyEventTrigger.cs` — хоткеи/комбинации.
- `Assets/Neoxider/Scripts/Tools/Input/MouseEffect.cs` — эффект мыши.
- `Assets/Neoxider/Scripts/Tools/Input/MouseInputManager.cs` — ввод мыши.
- `Assets/Neoxider/Scripts/Tools/Input/SwipeController.cs` — свайпы.

#### Tools/Managers

- `Assets/Neoxider/Scripts/Tools/Managers/Bootstrap.cs` — bootstrap.
- `Assets/Neoxider/Scripts/Tools/Managers/EM.cs` — event manager.
- `Assets/Neoxider/Scripts/Tools/Managers/GM.cs` — game manager.
- `Assets/Neoxider/Scripts/Tools/Managers/Singleton.cs` — singleton base.

#### Tools/Components

- `Assets/Neoxider/Scripts/Tools/Components/Loot.cs` — лут.
- `Assets/Neoxider/Scripts/Tools/Components/ScoreManager.cs` — очки/звезды.
- `Assets/Neoxider/Scripts/Tools/Components/TextScore.cs` — UI вывод текущего/лучшего счета (на базе `Neo.Tools.SetText`).
- `Assets/Neoxider/Scripts/Tools/Components/TypewriterEffect.cs` — печать текста.
- `Assets/Neoxider/Scripts/Tools/Components/TypewriterEffectComponent.cs` — обертка печати текста.
- `Assets/Neoxider/Scripts/Tools/Components/Interface/InterfaceAttack.cs` — интерфейс атаки.
- `Assets/Neoxider/Scripts/Tools/Components/AttackSystem/AdvancedAttackCollider.cs` — коллайдер атаки.
- `Assets/Neoxider/Scripts/Tools/Components/AttackSystem/AttackExecution.cs` — выполнение атаки.
- `Assets/Neoxider/Scripts/Tools/Components/AttackSystem/Evade.cs` — уклонение.
- `Assets/Neoxider/Scripts/Tools/Components/AttackSystem/Health.cs` — здоровье.

#### Tools/Dialogue

- `Assets/Neoxider/Scripts/Tools/Dialogue/DialogueController.cs` — контроллер диалога.
- `Assets/Neoxider/Scripts/Tools/Dialogue/DialogueData.cs` — данные диалога.
- `Assets/Neoxider/Scripts/Tools/Dialogue/DialogueUI.cs` — UI диалога.

#### Tools/Draw

- `Assets/Neoxider/Scripts/Tools/Draw/Drawer.cs` — рисование линий.

#### Tools/FakeLeaderboard

- `Assets/Neoxider/Scripts/Tools/FakeLeaderboard/Leaderboard.cs` — лидерборд.
- `Assets/Neoxider/Scripts/Tools/FakeLeaderboard/LeaderboardItem.cs` — элемент лидерборда.
- `Assets/Neoxider/Scripts/Tools/FakeLeaderboard/LeaderboardMove.cs` — анимация перемещения.

#### Tools/Random

- `Assets/Neoxider/Scripts/Tools/Random/ChanceManager.cs` — вероятности.
- `Assets/Neoxider/Scripts/Tools/Random/ChanceSystemBehaviour.cs` — вероятности (MB).
- `Assets/Neoxider/Scripts/Tools/Random/Data/ChanceData.cs` — данные вероятностей.

#### Tools/InteractableObject

- `Assets/Neoxider/Scripts/Tools/InteractableObject/InteractiveObject.cs` — базовое взаимодействие.
- `Assets/Neoxider/Scripts/Tools/InteractableObject/PhysicsEvents2D.cs` — события 2D.
- `Assets/Neoxider/Scripts/Tools/InteractableObject/PhysicsEvents3D.cs` — события 3D.
- `Assets/Neoxider/Scripts/Tools/InteractableObject/ToggleObject.cs` — toggle.

#### Tools/Other

- `Assets/Neoxider/Scripts/Tools/Other/AiNavigation.cs` — навигация ИИ (legacy).
- `Assets/Neoxider/Scripts/Tools/Other/CameraShake.cs` — тряска камеры.
- `Assets/Neoxider/Scripts/Tools/Other/RevertAmount.cs` — revert helper.
- `Assets/Neoxider/Scripts/Tools/Other/SpineController.cs` — фасад Spine (опционально).

#### Tools/Debug

- `Assets/Neoxider/Scripts/Tools/Debug/ErrorLogger.cs` — логирование ошибок.
- `Assets/Neoxider/Scripts/Tools/Debug/FPS.cs` — FPS.

#### Tools/Misc

- `Assets/Neoxider/Scripts/Tools/CameraAspectRatioScaler.cs` — масштаб под aspect.
- `Assets/Neoxider/Scripts/Tools/UpdateChilds.cs` — утилита для детей.

### UI (`Assets/Neoxider/Scripts/UI/`)

- `Assets/Neoxider/Scripts/UI/AnchorMove.cs` — движение UI.
- `Assets/Neoxider/Scripts/UI/AnimationFly.cs` — UI fly анимация.
- `Assets/Neoxider/Scripts/UI/PausePage.cs` — пауза.
- `Assets/Neoxider/Scripts/UI/UIReady.cs` — готовность UI.
- `Assets/Neoxider/Scripts/UI/Animation/ButtonScale.cs` — scale кнопки.
- `Assets/Neoxider/Scripts/UI/Animation/ButtonShake.cs` — shake кнопки.
- `Assets/Neoxider/Scripts/UI/Simple/ButtonChangePage.cs` — смена страниц.
- `Assets/Neoxider/Scripts/UI/Simple/FakeLoad.cs` — fake load.
- `Assets/Neoxider/Scripts/UI/Simple/UI.cs` — UI менеджер.
- `Assets/Neoxider/Scripts/UI/View/Points.cs` — points индикатор.
- `Assets/Neoxider/Scripts/UI/View/VariantView.cs` — варианты.
- `Assets/Neoxider/Scripts/UI/View/VisualToggle.cs` — визуальный toggle.

### PropertyAttribute (`Assets/Neoxider/Scripts/PropertyAttribute/`)

- `Assets/Neoxider/Scripts/PropertyAttribute/ButtonAttribute.cs` — атрибут кнопки.
- `Assets/Neoxider/Scripts/PropertyAttribute/ButtonAttributeDrawer.cs` — drawer кнопки (Editor).
- `Assets/Neoxider/Scripts/PropertyAttribute/GUIColorAttribute.cs` — атрибут цвета.
- `Assets/Neoxider/Scripts/PropertyAttribute/GUIColorAttributeDrawer.cs` — drawer цвета (Editor).
- `Assets/Neoxider/Scripts/PropertyAttribute/RequireInterface.cs` — атрибут интерфейса.
- `Assets/Neoxider/Scripts/PropertyAttribute/RequireInterfaceDrawer.cs` — drawer интерфейса (Editor).
- `Assets/Neoxider/Scripts/PropertyAttribute/InjectAttribute/FindAllInSceneAttribute.cs` — inject.
- `Assets/Neoxider/Scripts/PropertyAttribute/InjectAttribute/FindInSceneAttribute.cs` — inject.
- `Assets/Neoxider/Scripts/PropertyAttribute/InjectAttribute/GetComponentAttribute.cs` — inject.
- `Assets/Neoxider/Scripts/PropertyAttribute/InjectAttribute/GetComponentsAttribute.cs` — inject.
- `Assets/Neoxider/Scripts/PropertyAttribute/InjectAttribute/LoadAllFromResourcesAttribute.cs` — inject.
- `Assets/Neoxider/Scripts/PropertyAttribute/InjectAttribute/LoadFromResourcesAttribute.cs` — inject.

### Editor (`Assets/Neoxider/Editor/` + `**/Editor/**`)

- `Assets/Neoxider/Editor/AutoBuildName.cs` — авто-именование билдов.
- `Assets/Neoxider/Editor/FindAndRemoveMissingScriptsWindow.cs` — окно missing scripts.
- `Assets/Neoxider/Editor/GUI/EditorWindowGUI.cs` — GUI helpers.
- `Assets/Neoxider/Editor/GUI/FindAndRemoveMissingScriptsWindowGUI.cs` — GUI окна missing scripts.
- `Assets/Neoxider/Editor/GUI/NeoxiderSettingsWindowGUI.cs` — GUI окна настроек.
- `Assets/Neoxider/Editor/GUI/SceneSaverGUI.cs` — GUI SceneSaver.
- `Assets/Neoxider/Editor/GUI/TextureMaxSizeChangerGUI.cs` — GUI TextureMaxSizeChanger.
- `Assets/Neoxider/Editor/Main/CreateSceneHierarchy.cs` — настройка иерархии сцены.
- `Assets/Neoxider/Editor/Main/NeoxiderSettings.cs` — настройки Neoxider.
- `Assets/Neoxider/Editor/Main/NeoxiderSettingsWindow.cs` — окно настроек (Main).
- `Assets/Neoxider/Editor/Scene/SceneSaver.cs` — автосохранение сцен.
- `Assets/Neoxider/Editor/TextureMaxSizeChanger.cs` — массовое изменение текстур.
- `Assets/Neoxider/Editor/SaveProjectZip.cs` — zip проекта.
- `Assets/Neoxider/Editor/Create/CreateMenuObject.cs` — Create menu helpers.
- `Assets/Neoxider/Editor/Create/SingletonCreator.cs` — создание singleton.
- `Assets/Neoxider/Editor/PropertyAttribute/ComponentDrawer.cs` — drawer компонентов.
- `Assets/Neoxider/Editor/PropertyAttribute/CustomEditorBase.cs` — база кастом-инспектора.
- `Assets/Neoxider/Editor/PropertyAttribute/CustomEditorSettings.cs` — настройки кастом-инспектора.
- `Assets/Neoxider/Editor/PropertyAttribute/GradientButtonSettings.cs` — настройки градиентной кнопки.
- `Assets/Neoxider/Editor/PropertyAttribute/NeoCustomEditor.cs` — кастом-инспектор система.
- `Assets/Neoxider/Editor/PropertyAttribute/NeoCustomEditorRegistrar.cs` — регистрация кастом-инспекторов.
- `Assets/Neoxider/Editor/PropertyAttribute/NeoEditorAsmdefFixer.cs` — фиксы asmdef.
- `Assets/Neoxider/Editor/PropertyAttribute/NeoEditorAutoRegister.cs` — авто-регистрация.
- `Assets/Neoxider/Editor/PropertyAttribute/NeoInspectorSettings.cs` — настройки инспектора.
- `Assets/Neoxider/Editor/PropertyAttribute/NeoUpdateChecker.cs` — проверка обновлений.
- `Assets/Neoxider/Editor/PropertyAttribute/NeoxiderSettingsWindow.cs` — окно настроек (PropertyAttribute).
- `Assets/Neoxider/Editor/PropertyAttribute/ResourceDrawer.cs` — drawer ресурсов.
- `Assets/Neoxider/Editor/Tools/Physics/MagneticFieldEditor.cs` — scene handle для MagneticField.
- `Assets/Neoxider/UI Extension/Editor/CreateMenuObject.cs` — Create menu helpers (UI Extension).

# Полезные компоненты Neoxider (Add Component / GameObject)

Компоненты, которые удобно добавлять через **Add Component** (меню **Neoxider/…**) или через **GameObject → Neoxider/…** при создании объекта.

---

## Как добавить

- **Add Component** → в поиске ввести `Neoxider` или выбрать пункт **Neoxider** в списке категорий.
- **GameObject** → **Neoxider** → подменю по категориям (UI, Bonus, Tools, Shop, Audio и т.д.) — часть компонентов создаёт объект с компонентом и опционально префабом.

Путь в меню везде: **Neoxider/** (не Neo).

---

## По категориям (Add Component → Neoxider/…)

### Neoxider/UI
| Компонент | Кратко |
|-----------|--------|
| **VisualToggle** | Вкл/выкл видимости (GameObject, Renderer, CanvasGroup и т.д.) по событиям. |
| **UIReady** | Устаревший. Используйте SceneFlowController (Neoxider/Level). |
| **ButtonPrice** | Отображение цены, блокировка кнопки при нехватке валюты. Индикатор очков/звёзд — см. **Selector** (режим fill). |
| **UI** (Simple) | Простой контейнер кнопок/страниц. |
| **VariantView**, **AnchorMove** | Варианты отображения, сдвиг по якорям. |
| **ButtonScale**, **ButtonShake** | Анимация кнопок при нажатии. |
| **FakeLoad** (UI) | Имитация загрузки для экранов. |
| **ButtonChangePage** | Смена страницы/панели по клику. |
| **PausePage** | Страница паузы (в Tools). |

### Neoxider/Tools (подкатегории в окне Create Neoxider Object)

В окне **Create Neoxider Object** и в путях меню Tools разбит на подпапки:

| Подкатегория | Компоненты |
|--------------|------------|
| **Physics** | ExplosiveForce, ImpulseZone, MagneticField, PhysicsEvents2D, PhysicsEvents3D |
| **Movement** | Follow, DistanceChecker, CameraConstraint, CameraRotationController, CursorLockController, PlayerController2D/3D Physics, PlayerController2D/3D AnimatorDriver, ScreenPositioner, AdvancedForceApplier, MouseMover2D/3D, ConstantMover, ConstantRotator, KeyboardMover, UniversalRotator |
| **Spawner** | Spawner, SimpleSpawner, Despawner (удаление/возврат в пул, спавн при деспавне, OnDespawn) |
| **Components** | Health, Evade, AttackExecution, AdvancedAttackCollider, Counter, Loot, TextScore, TypewriterEffectComponent |
| **Dialogue** | DialogueController, DialogueUI |
| **Input** | SwipeController, MultiKeyEventTrigger, MouseEffect |
| **View** | Selector, StarView, BillboardUniversal, LightAnimator, MeshEmission, ImageFillAmountAnimator, ZPositionAdjuster |
| **Debug** | FPS, ErrorLogger |
| **Time** | TimerObject |
| **Text** | SetText, TimeToText |
| **Interact** | InteractiveObject, ToggleObject |
| **Random** | ChanceSystemBehaviour |
| **Other** | AiNavigation, CameraShake, Drawer, RevertAmount, SpineController, UpdateChilds, PausePage |
| **State Machine** | StateMachineBehaviour, State Machine Behaviour |
| **FakeLeaderboard** | LeaderboardMove, LeaderboardItem |
| **Managers** | Bootstrap |
| **Camera** | CameraAspectRatioScaler |

### Neoxider/Shop
| Компонент | Кратко |
|-----------|--------|
| **Money** | Синглтон валюты (хранение, события). |
| **ShopItem**, **Shop** | Элемент магазина и сам магазин. |
| **ButtonPrice** | Кнопка с ценой (в UI). |

### Neoxider/Audio
| Компонент | Кратко |
|-----------|--------|
| **AM** (Audio Manager) | Управление звуком сцены. |
| **AMSettings**, **SettingMixer** | Настройки и микшер. |
| **PlayAudio**, **PlayAudioBtn** | Воспроизведение по событию / по клику. |
| **AudioControl** | UI-контрол громкости. |

### Neoxider/Bonus
| Компонент | Кратко |
|-----------|--------|
| **CooldownReward**, **TimeReward** | Награда по кулдауну / по времени. |
| **WheelFortune**, **LineRoulett** | Колесо удачи, линейная рулетка. |
| **Box**, **ItemCollection**, **ItemCollectionInfo** | Коллекции и боксы. |
| **SpinController**, **Row**, **SlotElement** | Слот-механики. |

### Neoxider/Level
| Компонент | Кратко |
|-----------|--------|
| **SceneFlowController** | Загрузка сцен (по id/имени/полям), прогресс (текст, Slider, Image), события, Quit/Restart/Pause/Proceed. |
| **LevelManager** | Менеджер уровней и карт. |
| **LevelButton** | Кнопка выбора/запуска уровня. |

### Neoxider/Save
| Компонент | Кратко |
|-----------|--------|
| **SaveProviderSettingsComponent** | Компонент настроек провайдера сохранений. |
| **PlayerData** (Example) | Пример данных игрока. |

### Neoxider/Condition
| Компонент | Кратко |
|-----------|--------|
| **NeoCondition** | No-Code условия по полям компонентов, AND/OR, события. |

### Neoxider/Animations
| Компонент | Кратко |
|-----------|--------|
| **ColorAnimator**, **FloatAnimator**, **Vector3Animator** | Анимация цвета, float, Vector3. |

### Neoxider/GridSystem, Neoxider/Parallax, Neoxider/NPC
| Компонент | Кратко |
|-----------|--------|
| **FieldGenerator**, **FieldSpawner**, **FieldObjectSpawner** | Генерация и спавн сетки. |
| **ParallaxLayer** | Слой параллакса. |
| **NpcNavigation** | Навигация NPC. |

---

## GameObject → Neoxider (быстрое создание)

Через **GameObject → Neoxider → Create Neoxider Object…** открывается список компонентов с атрибутом `[CreateFromMenu]`. Выбор пункта создаёт объект с компонентом; если указан префаб и он найден в пакете — создаётся из префаба, иначе — пустой объект с компонентом (fallback).

**Соответствие путям и документации:**
- Путь в меню (MenuPath) совпадает с **Add Component** и разделами выше: Neoxider/UI, Neoxider/Tools, Neoxider/Bonus, Neoxider/Shop, Neoxider/Audio и т.д.
- Пути к префабам в пакете по возможности повторяют структуру папок скриптов (например `Prefabs/UI/`, `Prefabs/Tools/`, `Prefabs/Bonus/`). При установке из Git или пакета префабы подгружаются из корня пакета; если префаб не найден — создаётся только объект с компонентом.

**Доступные пункты:** список строится по рефлексии из всех типов с атрибутом `[CreateFromMenu]`. Категории верхнего уровня (UI, Tools, Bonus, Shop, Audio и т.д.) в окне выделены цветом для быстрого распознавания. У **Tools** есть подпапки: Physics, Movement, Spawner, Components, Dialogue, Input, View, Debug, Time, Text, Interact, Random, Other, State Machine, FakeLeaderboard, Managers, Camera. Путь пункта совпадает с Add Component (например Neoxider/Tools/Movement/PlayerController3DPhysics). У части пунктов задан префаб — тогда создаётся объект из префаба; иначе — пустой объект с компонентом.

Чтобы добавить новый пункт, повесьте на класс `[CreateFromMenu("Neoxider/Категория/Имя", PrefabPath = "Prefabs/...")]` (PrefabPath опционален).

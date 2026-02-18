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
| **UIReady** | Координация «готовности» UI (например, перед показом экрана). |
| **ButtonPrice**, **Points** | Отображение цены/очков, блокировка кнопки при нехватке валюты. |
| **UI** (Simple) | Простой контейнер кнопок/страниц. |
| **VariantView**, **AnchorMove** | Варианты отображения, сдвиг по якорям. |
| **ButtonScale**, **ButtonShake** | Анимация кнопок при нажатии. |
| **FakeLoad** (UI) | Имитация загрузки для экранов. |
| **ButtonChangePage** | Смена страницы/панели по клику. |
| **PausePage** | Страница паузы (в Tools). |

### Neoxider/Tools
| Компонент | Кратко |
|-----------|--------|
| **Spawner**, **SimpleSpawner** | Спавн префабов с задержками, пулом. |
| **TimerObject** | Таймер на GameObject (события по истечении). |
| **FPS**, **ErrorLogger** | FPS-счётчик и лог ошибок в консоль/UI. |
| **SwipeController** | Распознавание свайпов. |
| **Bootstrap** | Ранний запуск инициализации сцены. |
| **Counter** | Счётчик (инкремент/сброс, события). |
| **Health**, **Evade**, **AttackExecution** | Здоровье, уклонение, исполнение атаки. |
| **Loot**, **ChanceSystemBehaviour** | Дроп лута, шансы. |
| **Follow**, **DistanceChecker** | Следование за целью, проверка дистанции. |
| **CameraConstraint**, **CameraRotationController**, **CursorLockController** | Ограничение камеры, вращение, блокировка курсора. |
| **PhysicsEvents2D/3D**, **InteractiveObject**, **ToggleObject** | События физики, интерактивный объект, вкл/выкл объекта. |
| **DialogueController**, **DialogueUI** | Диалоги. |
| **SetText**, **TimeToText** | Установка текста, форматирование времени. |
| **Selector**, **StarView**, **BillboardUniversal** | Выбор варианта, отображение звёзд, billboard. |
| **AiNavigation**, **CameraShake**, **Drawer** | Навигация ИИ, тряска камеры, рисование линий. |
| **StateMachineBehaviour** | Конечный автомат (Tools). |

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

Через **GameObject → Neoxider** можно сразу создать объект с компонентом (и при необходимости подтянуть префаб):

- **UI:** VisualToggle, UIReady, ButtonPrice, UI, Points  
- **Bonus:** CooldownReward, WheelFortune, LineRoulett  
- **Shop:** Money, ShopItem  
- **Audio:** AM  
- **Tools:** ErrorLogger, TimerObject, FPS, SwipeController  
- **Btn:** Create Scene Hierarchy, Sort Hierarchy Objects  

Подробнее по каждому компоненту — в соответствующих разделах [документации](./README.md).

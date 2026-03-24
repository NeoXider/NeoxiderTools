# CursorLockController

**Что это:** Компонент Unity (`MonoBehaviour`, пространство имён `Neo.Tools`). Управляет `Cursor.lockState` и `Cursor.visible`, стеком владельцев при нескольких экземплярах, пресетами инспектора и опциональным снимком курсора на lifecycle (`OnEnable` / `OnDisable`). Файл: `Assets/Neoxider/Scripts/Tools/Move/CursorLockController.cs`.

**Как использовать:**
1. Добавьте компонент на объект игрока, UI-страницу (меню, пауза) или корень сцены только с UI.
2. Выберите **Preset** (**Gameplay_Default**, **UI_Page_ShowCursorWhileActive**, **UI_MenuScene_Standalone**) или **Custom** и при необходимости настройте **Mode**, **Control Mode**, **Lifecycle** и **Lifecycle snapshot**.
3. При нескольких контроллерах закрывайте временный UI через **`ReleaseControl()`** или отключение объекта — нижний контроллер восстановит своё состояние.
4. Для New Input System вызывайте **`SetCursorLocked(bool)`** или **`ToggleCursorState()`** из callback действия.

---

## 1. Сводка с PlayerController3DPhysics

На том же объекте, что и **`PlayerController3DPhysics`**, этот компонент становится единственным источником lock/toggle курсора для игрока (без двойного переключения). Остановка обзора при видимом курсоре настраивается у игрока опцией **Pause Look When Cursor Visible** — без поиска объектов и связи скриптов через `FindObjectsOfType`.

---

## 2. Preset (быстрая настройка в инспекторе)

Вверху инспектора — **Preset**. Значение **не Custom** при смене пресета в редакторе перезаписывает поля lifecycle/toggle (в `OnValidate`), чтобы не собирать десяток bool вручную. Для тонкой настройки выберите **Custom**.

| Preset | Назначение |
|--------|------------|
| **Custom** | Все поля вручную. |
| **Gameplay_Default** | Старт с lock+скрытие, Escape-toggle, без lifecycle на Enable/Disable. |
| **UI_Page_ShowCursorWhileActive** | Оверлей меню/паузы: **SaveOnEnable** + **After Disable = RestorePrevious** (как **PausePage** при пустом стеке); под геймплеем стек сам вернёт нижний контроллер. |
| **UI_MenuScene_Standalone** | Только меню: показ курсора на `OnEnable`; **Lifecycle snapshot = None**; `Apply On Disable` выключен — курсор не форсируется при отключении объекта. |

---

## 3. Стек контроллеров и смена сцен

Несколько `CursorLockController` хранятся в **статическом списке**: последний, кто вызвал захват курсора, задаёт текущее состояние; при `ReleaseControl`, отключении или **уничтожении** верхнего контроллера состояние возвращается предыдущему.

- Мёртвые (уничтоженные) ссылки **удаляются из списка** при обращении к стеку и при загрузке сцены (`SceneManager.sceneLoaded`), затем повторно применяется состояние верхнего оставшегося контроллера — это устраняет «залипание» после `LoadScene` без игрока в меню.
- **Additive**-загрузка сцен: список не очищается целиком, только невалидные записи; предыдущий геймплей-контроллер может остаться под новым UI.

---

## 4. Режим (Mode)

- **LockAndHide** — при «locked» блокирует и скрывает курсор; при «unlocked» разблокирует и показывает.
- **OnlyHide** — управляет только видимостью (Cursor.visible).
- **OnlyLock** — управляет только блокировкой (Cursor.lockState).

### Control Mode

- **AutomaticAndManual** — режим по умолчанию. Компонент работает и от lifecycle/горячей клавиши, и от ручных вызовов методов.
- **AutomaticOnly** — разрешены только automatic-сценарии (`Start`, `OnEnable`, `OnDisable`, toggle по клавише).
- **ManualOnly** — компонент не делает ничего сам и реагирует только на прямые вызовы методов (`SetCursorLocked`, `ShowCursor`, `HideCursor`, `ToggleCursorState`).

### Cursor Access Key

Это отдельный shortcut для временного включения курсора прямо во время gameplay.

По умолчанию **выключен** и вообще не влияет на поведение компонента, пока явно не включён параметр `Allow Cursor Access Key`.

- **HoldToShowCursor** — пока клавиша удерживается, курсор показывается и разблокируется; при отпускании возвращается предыдущее состояние.
- **ToggleShowCursor** — первое нажатие включает курсор, повторное возвращает предыдущее состояние.

Типичный пример: клавиша `Z`, чтобы временно открыть курсор над игровым экраном, не открывая полноценное меню.

## 5. Поведение

- **Controller Enabled**: master switch. Когда выключен, компонент не реагирует на toggle input и lifecycle-применение состояний.
- **Control Mode**: определяет, разрешены ли automatic- и/или manual-сценарии. По умолчанию стоит **AutomaticAndManual**.
- **Start**: при включённом `_lockOnStart` применяет выбранное состояние. Опционально можно не применять Start State, если `Controller Enabled = false`.
- **OnEnable**: можно отдельно включать/выключать сам контроллер (`_setControllerEnabledOnEnable`) и, если контроллер активен, применять курсорное состояние `_lockOnEnable`.
- **OnDisable**: можно отдельно включать/выключать сам контроллер (`_setControllerEnabledOnDisable`) и, если контроллер ещё активен, применять курсорное состояние `_lockOnDisable`.
- **Lifecycle snapshot** (опционально):
  - **None** — только `_lockOnEnable` / `_lockOnDisable`, без снимка.
  - **SaveOnEnable** — перед применением OnEnable сохраняются `Cursor.lockState` и `Cursor.visible`; после `ReleaseControl` на OnDisable, если **поверх стека никого нет**, срабатывает **After Lifecycle Disable**: **RestorePrevious** (как у **PausePage**), **ForceLockedHidden** (всегда lock+скрыть) или **ApplyConfigured** (`_lockOnDisable`). Если под страницей есть другой контроллер, он сам восстановит курсор — снимок только сбрасывается.
  - **SaveOnDisable** — обратный порядок: в начале OnDisable (при включённом apply) сохраняется курсор; при следующем OnEnable **After Lifecycle Enable**: **RestorePrevious** или обычное **ApplyConfigured** (`Acquire` по `_lockOnEnable`).
- **Update**: при `_allowToggle` и активном контроллере переключает состояние по клавише `_toggleKey` (по умолчанию Escape).
- **Cursor Access Key**: отдельная клавиша вроде `Z`, которая временно или в toggle-режиме показывает курсор поверх текущего состояния контроллера.
- **События**: `_onCursorLocked`, `_onCursorUnlocked`.
- **Несколько контроллеров**: активные `CursorLockController` теперь работают по принципу «последний взял управление — последний задаёт состояние». Когда верхний контроллер делает `ReleaseControl()` или выключается, управление возвращается предыдущему.

Если на том же объекте есть `PlayerController3DPhysics`, его собственные `_lockCursorOnStart` и `_toggleCursorOnEscape` больше не вмешиваются, пока `CursorLockController` активен и `Controller Enabled = true`. Это убирает двойное управление курсором.

**New Input System:** встроенной привязки к Input System нет. Вызывайте `SetCursorLocked(bool)` или `ToggleCursorState()` из callback вашего Input Action (например, UI или PlayerInput).

---

## 6. Настройка

1. Добавьте `CursorLockController` на активный объект (камера, GameManager, корень геймплея).
2. **Controller**:
   - `Controller Enabled` — мастер-переключатель
   - `Control Mode` — по умолчанию **AutomaticAndManual**
   - доступны публичные методы `SetControllerEnabled(bool)`, `EnableController()`, `DisableController()`
3. **Start State**: `_lockOnStart` — lock курсора при старте.
4. **Lifecycle**: при необходимости включите `_setControllerEnabledOnEnable` / `_setControllerEnabledOnDisable` и `_applyOnEnable` / `_applyOnDisable`.
5. **Toggle**: `_allowToggle`, `_toggleKey` — ручное переключение по клавише.
6. **Cursor Access Key**:
   - по умолчанию `_allowCursorAccessKey = false`
   - включайте его только если реально нужен shortcut доступа к курсору
   - после включения `_allowCursorAccessKey = true`
   - `_cursorAccessKey = Z` (или любая другая клавиша)
   - `_cursorAccessKeyMode = HoldToShowCursor` или `ToggleShowCursor`
7. Для паузы/меню можно использовать **PausePage** с **Control Cursor**: при паузе курсор показывается; при закрытии — по **After Pause Cursor** (по умолчанию **RestorePrevious**; для FPS включите **ForceLockedHidden**). Если `CursorLockController` отключается вместе с объектом, можно через lifecycle сразу выключать и сам контроллер.

---

## 7. Сценарий: только UI-сцена (нет игрока)

1. На корневой объект UI (или Canvas) добавьте `CursorLockController`.
2. Выберите **Preset = UI_MenuScene_Standalone** (или вручную: `Apply On Enable`, `Lock On Enable = false`, `Apply On Disable = false`, `Lock On Start = false`).
3. Убедитесь, что **Controller Enabled** включён и **Control Mode** не **ManualOnly** (иначе lifecycle не сработает без вызовов из кода).
4. Игрок и `PlayerController3DPhysics` в сцене **не нужны** — курсор управляется только этим компонентом.

---

## 8. Сценарий: CursorLockController на странице меню или паузы

`CursorLockController` не обязан жить на объекте игрока. Его можно повесить прямо на объект страницы меню/паузы и использовать как локальный «переключатель режима UI».

### Цель

- при открытии страницы:
  - показать курсор
  - разблокировать его
  - выключить обзор у `PlayerController3DPhysics`
- при закрытии страницы:
  - снова скрыть/заблокировать курсор
  - снова включить обзор

### Как настроить

1. На объект страницы меню или паузы добавьте `CursorLockController`.
2. Для страницы UI обычно удобно выставить **Preset = UI_Page_ShowCursorWhileActive** или вручную:
   - `Mode = LockAndHide`
   - `Apply On Enable = true`
   - `Lock On Enable = false`
   - `Apply On Disable = true`
   - `Lock On Disable = true`
   - `Allow Toggle = false`, если страница сама управляет открытием/закрытием и не должна слушать `Escape`
3. На объекте игрока у `PlayerController3DPhysics` оставьте включённым **Pause Look When Cursor Visible**.
4. Если у игрока **нет** `CursorLockController` на том же объекте, это нормально. В таком случае назначьте `CursorLockController` страницы в поле **External Cursor Lock Controller** у `PlayerController3DPhysics`.
5. Если меню/пауза должны гарантированно выключать обзор независимо от видимости курсора, повесьте на события страницы вызовы:
   - при открытии: `PlayerController3DPhysics.SetLookEnabled(false)`
   - при закрытии: `PlayerController3DPhysics.SetLookEnabled(true)`
6. Если у игрока используется собственный `_toggleCursorOnEscape`, отключите его, когда курсором управляет отдельная UI-страница. Иначе получите два независимых источника переключения.

### Вариант через lifecycle без кода

Если страница просто включается/выключается (`SetActive(true/false)`), этого уже достаточно:

- `OnEnable` страницы применит `Lock On Enable = false` и покажет курсор
- `OnDisable` страницы применит `Lock On Disable = true` и вернёт игровой режим курсора

Для полного UX обычно добавляют ещё два UnityEvent-вызова в логике открытия/закрытия страницы:

- `PlayerController3DPhysics.SetLookEnabled(false)`
- `PlayerController3DPhysics.SetLookEnabled(true)`

### Вариант для нескольких страниц и механик

Если в проекте есть несколько источников управления курсором, например:

- pause page
- inventory page
- settings page
- механика «сесть за компьютер»

то каждому можно дать свой `CursorLockController`.

Рекомендуемый подход:

1. Для UI-страниц используйте `Control Mode = AutomaticAndManual` или `AutomaticOnly`, если страница живёт через `SetActive`.
2. Для игровых механик, которые включаются из логики/события, используйте `ManualOnly` и методы:
   - `ShowCursor()`
   - `HideCursor()`
   - `SetCursorLocked(bool)`
   - `ReleaseControl()`
3. Когда временная механика закончилась, вызывайте `ReleaseControl()`, чтобы вернуть курсор предыдущему активному контроллеру, а не просто «угадать» нужное состояние вручную.

Это делает систему удобной и универсальной: automatic-страницы и manual-механики могут сосуществовать без жёстких зависимостей друг от друга.

### Отдельный shortcut по `Z`

Если нужно, чтобы игрок мог прямо во время gameplay быстро включать курсор:

1. На gameplay-контроллере включите `_allowCursorAccessKey`.
2. Поставьте `_cursorAccessKey = Z`.
3. Выберите режим:
   - `HoldToShowCursor` — курсор только пока удерживается `Z`
   - `ToggleShowCursor` — `Z` включает/выключает курсор как отдельный mini-mode

Если `_allowCursorAccessKey = false`, этот режим полностью отключён.

Этот shortcut работает как отдельный слой управления: он не ломает lifecycle, не мешает manual-вызовам и корректно возвращает предыдущее состояние после завершения.

### Когда это особенно полезно

- главное меню поверх gameplay-сцены
- pause overlay
- inventory / map / settings page в FPS или TPS
- любой UI, который временно забирает мышь у игрока, но не должен жить на том же объекте, что и контроллер персонажа

---

## 9. Публичный API

| Член | Описание |
|------|----------|
| `IsLocked` | Текущее состояние блокировки курсора. |
| `ControllerEnabled` | Активен ли сам контроллер. |
| `SetCursorLocked(bool)` | Установить lock/unlock и видимость. |
| `ToggleCursorState()` | Инвертировать текущее состояние. |
| `ShowCursor()` | Показать и разблокировать. |
| `HideCursor()` | Скрыть и заблокировать. |
| `ReleaseControl()` | Отпустить владение курсором и вернуть управление предыдущему активному контроллеру. |
| `SetControllerEnabled(bool)` | Включить/выключить сам контроллер. |
| `EnableController()` / `DisableController()` | Удобные методы для UnityEvent / NoCode. |
| `Preset` | Только чтение: текущий выбранный пресет (`ConfigurationPreset`). |
| `SnapshotMode` | Только чтение: режим снимка lifecycle (`LifecycleSnapshotMode`). |

---

## 10. Совместное использование с PausePage и контроллерами игрока

- **PausePage** с **Control Cursor**: при паузе курсор показывается; при закрытии — по полю **After Pause Cursor** (**RestorePrevious** по умолчанию или **ForceLockedHidden** для FPS). Подробнее: [`PausePage`](../../UI/PausePage.md).
- **PlayerController3DPhysics** больше не дублирует lock/unlock на старте и по Escape, если на том же объекте активен `CursorLockController`. При этом `Pause Look When Cursor Visible` всё так же останавливает look, когда курсор показан.
- Если `CursorLockController` расположен не на объекте игрока, а на UI-странице, управление курсором всё равно работает через его публичные методы и lifecycle. В этом случае управление обзором лучше явно связать через `SetLookEnabled(bool)` у `PlayerController3DPhysics`.

---

## См. также

- [`PlayerController3DPhysics`](./PlayerController3DPhysics.md)
- [`PausePage`](../../UI/PausePage.md)
- [`Move`](./README.md)

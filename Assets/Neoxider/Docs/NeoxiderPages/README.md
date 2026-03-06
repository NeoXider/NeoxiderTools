# NeoxiderPages (`Neo.Pages`)

`NeoxiderPages` — это опциональный sample-модуль навигации по страницам/экранам поверх Unity UI.

## Где находится модуль

После импорта sample через `Package Manager > Neoxider Tools > Samples` модуль находится в:

```text
Assets/Neoxider/Samples~/NeoxiderPages/
```

Ключевые артефакты:

- документация: `Assets/Neoxider/Docs/NeoxiderPages/README.md`
- demo-сцена: `Assets/Neoxider/Samples~/NeoxiderPages/Demo/Scenes/UI.unity`
- runtime/editor код: внутри `Assets/Neoxider/Samples~/NeoxiderPages/Runtime` и `Editor`

## Что это

Базовая идея модуля:

- **`UIPage`** — помечает GameObject как страницу и хранит идентификатор страницы.
- **`PM`** — переключает страницы, отслеживает текущую и предыдущую.
- **`UIKit`** — статический API для вызова `ShowPage(...)` без прямой ссылки на `PM`.

## Структура sample-модуля

```text
Assets/Neoxider/Samples~/NeoxiderPages/
  Runtime/     # runtime код Neo.Pages
  Editor/      # editor-инструменты Neo.Pages.Editor
  Demo/        # сцена и demo-ассеты
```

## Содержание

- [Что есть в модуле (кратко)](#что-есть-в-модуле-кратко)
- [Пример использования](#пример-использования)
- [Как добавить новую страницу](#как-добавить-новую-страницу)
- [Выбор страницы: Dropdown и Asset](#выбор-страницы-dropdown-и-asset)
- [Частые вопросы (FAQ)](#частые-вопросы-faq)
- [Скрипты (справочник)](#скрипты-справочник)

## Что есть в модуле (кратко)

- **`PM`** (`Runtime/Scripts/Page/PM.cs`)
  - singleton `PM.I`
  - `ChangePage(pageId)` — переключение (выбирает стратегию: Exclusive/Popup по настройке `UIPage`)
  - `SetPage(pageId)` — активирует страницу и деактивирует остальные (Exclusive)
  - `ActivePage(pageId)` — активирует страницу, не выключая остальные (Popup)
  - `SwitchToPreviousPage()` — возврат на предыдущую
  - `CloseCurrentPage()` — закрыть текущую
  - `OnPageChanged(UIPage)` — событие смены

- **`UIPage`** (`Runtime/Scripts/Page/UIPage.cs`)
  - поле `pageId: PageId` (ассет-идентификатор страницы)
  - флаги: `popup`, `ignoreOnExclusiveChange`
  - `StartActive()/EndActive()`
  - опциональная анимация через `DOTweenAnimation` на том же объекте

- **`BtnChangePage`** (`Runtime/Scripts/Page/BtnChangePage.cs`)
  - компонент на UI-кнопку: `Action` = OpenPage/Back/CloseCurrent
  - для OpenPage использует `targetPageId: PageId`
  - может выполнить `GameState.State` (Start/Restart/Pause/…)
  - опционально анимирует нажатие (DOTween)

- **`FakeLoad`** (`Runtime/Scripts/Page/FakeLoad.cs`)
  - фейковая загрузка с прогрессом и событиями

- **`UIKitAPI`** (`Runtime/API/UIKitAPI.cs`)
  - удобные фасады `G`, `Audio`, `Wallet`, `Score`, `Level`, `GameState`
  - позволяет Pages-слою работать «поверх» Neoxider без прямого расползания ссылок по сцене

## Пример использования

### Переключить страницу из кода

```csharp
using Neo.Pages;
using UnityEngine;

public class OpenShopExample : MonoBehaviour
{
    [SerializeField] private PageId shopPageId;

    public void OpenShop()
    {
        UIKit.ShowPage(shopPageId);
    }
}
```

### Переключить страницу кнопкой

- Добавь `BtnChangePage` на UI-кнопку.
- Выбери `Action`:
  - `OpenPage` и задай `targetPageId`
  - либо `Back` / `CloseCurrent`

### Отреагировать на смену страницы

```csharp
using Neo.Pages;
using UnityEngine;

public class PageAnalytics : MonoBehaviour
{
    [SerializeField] private PM pm;

    private void OnEnable()
    {
        if (pm != null) pm.OnPageChanged.AddListener(OnChanged);
    }

    private void OnDisable()
    {
        if (pm != null) pm.OnPageChanged.RemoveListener(OnChanged);
    }

    private static void OnChanged(UIPage newPage)
    {
        Debug.Log($"Page changed to: {newPage?.PageId?.DisplayName}");
    }
}
```

## Как добавить новую страницу

Идентификатор страницы — это **`PageId` ассет**. Добавление новой страницы не требует правок кода:

1. Создай UI (префаб/объект) страницы и повесь на корень компонент `UIPage`.
2. В инспекторе `UIPage` в секции **Generate** впиши имя (например `Menu` или `PageMenu`) и нажми **Generate & Assign**.
   - Если страница уже существует, новый ассет создан не будет — появится лог, и будет назначен существующий.
3. Убедись, что страница находится в сцене вместе с `PM`, чтобы `PM` её нашёл.

## Выбор страницы: Dropdown и Asset

В `UIPage` и `BtnChangePage` можно выбирать страницу двумя способами:

- **Dropdown**: выпадающий список всех `PageId` из `Assets/NeoxiderPages/Pages`.
- **Asset**: обычное поле ассета (Object Picker/Drag&Drop).

### Как создать PageId ассет

1. В Project: `Create → Neoxider → Pages → Page Id`.
2. Переименуй ассет в формате `Page{Name}` (например `PageMenu`, `PageShop`, `PageSettings`).
   - Ключ `PageId.Id` **генерируется автоматически из имени ассета**.
3. Если хочешь создать стандартный набор страниц одним кликом: `Tools → Neoxider → Pages → Generate Default PageIds`.

### UIPage: назначение страницы

1. На объекте страницы открой компонент `UIPage`.
2. Выбери страницу через **Dropdown** или укажи ассет вручную.

### BtnChangePage: переключение страницы

1. На кнопке открой компонент `BtnChangePage`.
2. Выбери `Action`:
   - `OpenPage` — открыть страницу по `targetPageId`
   - `Back` — вернуться на предыдущую
   - `CloseCurrent` — закрыть текущую

### Переключение из кода

```csharp
using Neo.Pages;

public class OpenPagesExamples
{
    public void Open(PageId pageId)
    {
        UIKit.ShowPage(pageId);
    }
}
```

## Частые вопросы (FAQ)

### Почему страница не переключается?

Проверь:

- В сцене есть активный `PM` (обычно из `Assets/NeoxiderPages/Prefabs/PM.prefab`).
- На странице есть `UIPage`, и у него задан `pageId`.
- Для `BtnChangePage` выбрана корректная страница.

### Почему `PM` “не видит” страницу?

`PM` собирает страницы через `Resources.FindObjectsOfTypeAll<UIPage>()` и фильтрует по текущей сцене.
Если страница лежит в другом месте (например, в префабе вне сцены) — она не попадёт в список.

### Почему некоторые страницы не выключаются при SetPage?

Скорее всего:

- у страницы включён `popup` (она открывается поверх через `ActivePage`)
- или у неё включён `ignoreOnExclusiveChange` (её не трогают при `SetPage`)

## Скрипты (справочник)

Ниже перечислены основные `.cs` файлы внутри sample-модуля и их назначение.

### Runtime

- `Assets/Neoxider/Samples~/NeoxiderPages/Runtime/Scripts/UIKit.cs`
  - `UIKit.OnShowPage` / `UIKit.ShowPage(PageId)` — запрос показа страницы по ассету

- `Assets/Neoxider/Samples~/NeoxiderPages/Runtime/Scripts/Page/PageId.cs`
  - `PageId` (ScriptableObject) — ассет-идентификатор страницы
  - `PageId.Id` генерируется из имени ассета (рекомендуемый формат: `PageMenu`, `PageShop`, …)

- `Assets/Neoxider/Samples~/NeoxiderPages/Runtime/Scripts/Page/PM.cs`
  - менеджер страниц (singleton `PM.I`)
  - основное API: `ChangePage`, `SetPage`, `ActivePage`, `SwitchToPreviousPage`, `CloseCurrentPage`
  - переключение только по `PageId`
  - событие: `OnPageChanged(UIPage)`

- `Assets/Neoxider/Samples~/NeoxiderPages/Runtime/Scripts/Page/UIPage.cs`
  - компонент “страницы” (GameObject) с `pageId: PageId`
  - флаги: `popup`, `ignoreOnExclusiveChange`
  - поведение: `StartActive()` / `EndActive()` + (опционально) DOTween-анимация

- `Assets/Neoxider/Samples~/NeoxiderPages/Runtime/Scripts/Page/BtnChangePage.cs`
  - UI-кнопка переключения страниц через `PM.I`
  - `Action`: `OpenPage` / `Back` / `CloseCurrent`
  - для `OpenPage` использует `targetPageId: PageId`
  - может выполнить `GameState.State` перед переключением

- `Assets/Neoxider/Samples~/NeoxiderPages/Runtime/Scripts/Page/FakeLoad.cs`
  - корутина “фейковой загрузки” с прогрессом
  - события: `OnStart`, `OnFinisLoad`, `OnChangePercent(int)`, `OnChange(float)`

- `Assets/Neoxider/Samples~/NeoxiderPages/Runtime/Scripts/Core/PageSubscriber.cs`
  - пример подписки на события Neoxider (`G.OnStart`) и переключения страницы через `PM`

- `Assets/Neoxider/Samples~/NeoxiderPages/Runtime/Scripts/Core/ToggleAudio.cs`
  - мостик UI → аудио-настройки (`Audio.IsActiveMusic/IsActiveSound`)
  - работает через `VisualToggle` (из `Neo.UI`)

### Runtime API (фасады)

- `Assets/Neoxider/Samples~/NeoxiderPages/Runtime/API/UIKitAPI.cs`
  - набор статических фасадов для удобного доступа из UI:
    - `Wallet`, `Score`, `Level` — значения и события изменения
    - `Audio` — включение/выключение музыки/звука + `PlayUI()`
    - `G` — события/методы управления состояниями игры (Menu/Start/Pause/Win/…)
    - `GameState` — enum + `GameState.Set(state)` для выполнения действия

### Editor Tools

- `Assets/Neoxider/Samples~/NeoxiderPages/Editor/Tools/AutoSpriteAssignerEditor.cs`
  - EditorWindow: массово назначает `Sprite` в `UnityEngine.UI.Image` по имени GameObject ↔ имени Sprite
  - меню: `Tools/UIKit/Auto Sprite Assigner`

- `Assets/Neoxider/Samples~/NeoxiderPages/Editor/Tools/AutoTMPFontAssignerEditor.cs`
  - EditorWindow: массово назначает `TMP_FontAsset` для всех `TMP_Text` в сценах
  - меню: `Tools/UIKit/Auto TMP Font Assigner`

- `Assets/Neoxider/Samples~/NeoxiderPages/Editor/Tools/PageIdGenerator.cs`
  - генератор `PageId` ассетов (в т.ч. дефолтный набор)
  - меню: `Tools/Neoxider/Pages/Generate Default PageIds`

### Editor Inspectors

- `Assets/Neoxider/Samples~/NeoxiderPages/Editor/Inspectors/UIPageEditor.cs`
  - удобный инспектор `UIPage` с переключателем **Dropdown/Asset** + генерацией `PageId`

- `Assets/Neoxider/Samples~/NeoxiderPages/Editor/Inspectors/BtnChangePageEditor.cs`
  - удобный инспектор `BtnChangePage` с переключателем **Dropdown/Asset** + генерацией `PageId`

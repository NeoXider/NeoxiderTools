# NeoxiderPages (Neo.Pages)

Отдельный **опциональный** модуль PageManager для NeoxiderTools.

## Что это

Модуль `Neo.Pages` добавляет систему страниц (PageManager) поверх базовых систем Neoxider (`GM/EM`, аудио, магазин, уровни и т.д.).\n\n`Neo` (основной пакет) **может работать без этого модуля**.

## Структура

```text
Assets/NeoxiderPages/
  Runtime/     # runtime код (Neo.Pages)
  Editor/      # editor tools (Neo.Pages.Editor)
  Prefabs/     # демо/готовые префабы
  Scenes/      # демо-сцена
  Fonts/       # шрифты для демо
  Materials/   # материалы для демо
  Docs/        # документация модуля
```

## Быстрый старт

1. Добавь в сцену префаб `Assets/NeoxiderPages/Prefabs/PM.prefab`.\n2. Для каждой страницы используй префабы из `Assets/NeoxiderPages/Prefabs/Page/*`.\n3. Переключение страниц:\n   - через `Neo.Pages.UIKit.ShowPage(Neo.Pages.UIKit.Page.Menu)`\n   - или компонентом `Neo.Pages.BtnChangePage` на UI кнопках.

## Зависимости

- asmdef: `Assets/NeoxiderPages/Runtime/Neo.Pages.asmdef`\n- asmdef: `Assets/NeoxiderPages/Editor/Neo.Pages.Editor.asmdef`\n- Модуль зависит от Neoxider (`Assets/Neoxider/Scripts/*`).\n\n## Примечания\n\n- Дублирующие скрипты (форматирование, WaitWhile, отображение денег и т.д.) **не храним** в модуле — используем реализации из Neoxider.\n+


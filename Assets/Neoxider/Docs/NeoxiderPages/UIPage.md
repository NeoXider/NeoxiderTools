# UIPage

**Назначение:** компонент UI-страницы для работы с `PM` (Page Manager). Хранит `PageId`, режим popup и настройки анимации открытия/закрытия.

## Подключение

1. Добавьте `UIPage` на корневой GameObject страницы.
2. Назначьте `Page Id`.
3. Если страница должна открываться поверх других, включите `Popup`.
4. Если страница не должна закрываться при эксклюзивном переключении, включите `Ignore On Exclusive Change`.
5. Для анимации добавьте `DOTweenAnimation` и назначьте его в поле `Animation`.

## Основные поля (Inspector)

| Поле | Описание |
|------|----------|
| `Page Id` | Идентификатор страницы для переключения через `PM`. |
| `Popup` | Страница открывается поверх текущих страниц без их деактивации. |
| `Ignore On Exclusive Change` | `PM` не деактивирует эту страницу при обычном эксклюзивном переключении. |
| `Animation` | `DOTweenAnimation`, который проигрывается при открытии/закрытии. |
| `Animation Mode` | Когда проигрывать анимацию: `ForwardOnly`, `BackwardOnly`, `ForwardAndBackward`. |

## Animation Mode

| Режим | Поведение |
|------|-----------|
| `ForwardOnly` | При `StartActive()` страница включается и forward-анимация перезапускается с начала. При `EndActive()` страница выключается сразу. |
| `BackwardOnly` | При `StartActive()` страница только включается. При `EndActive()` reverse-анимация перезапускается с конца и после неё страница выключается. |
| `ForwardAndBackward` | При открытии forward-анимация перезапускается с начала, при закрытии reverse-анимация перезапускается с конца. |

Анимация страницы принудительно переводится в unscaled time (`DOTweenAnimation.isIndependentUpdate = true`) и `autoKill = false`, чтобы она корректно работала в паузе/меню и могла перезапускаться.

При эксклюзивном переключении через `PM` (Menu → Shop и т.п.), если у **входящей** страницы есть show-анимация (`ForwardOnly` / `ForwardAndBackward` + `DOTweenAnimation`), предыдущая страница остаётся активной на время этой анимации и только потом получает `EndActive()` — без «пустого фона» под наезжающей страницей.

## API

| Метод | Назначение |
|------|------------|
| `StartActive()` | Включить страницу и проиграть анимацию открытия согласно `Animation Mode`. |
| `EndActive()` | Закрыть страницу и проиграть reverse-анимацию согласно `Animation Mode`. Если объект уже неактивен в иерархии — только `SetActive(false)`, без coroutine (защита от ошибки Unity). |
| `SetActive(bool)` | Напрямую включить/выключить GameObject страницы. |

## Совместимость

Старые поля `_playBackward` и `_onlyPlayBackward` автоматически мигрируют в `Animation Mode`:

- `_onlyPlayBackward = true` → `BackwardOnly`;
- `_playBackward = true` → `ForwardAndBackward`;
- `_playBackward = false` → `ForwardOnly`.

## См. также

- [PM](./PM.md)
- [BtnChangePage](./BtnChangePage.md)

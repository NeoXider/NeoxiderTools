# Project Audit

Дата: 2026-05-25

## Область проверки

Проверены структура Unity-проекта, asmdef-модули, документация NeoDoc, samples, тестовое покрытие, устаревшие документы аудита/планов, базовые статические риски и пакетная гигиена.

## Итог

Проект в целом структурирован как пакет модулей Neoxider с хорошей документационной базой и заметным тестовым покрытием в Core, Network, Rpg, Save и Tools. Главные риски сейчас не в архитектурном провале, а в накопленном хвосте: старые legacy-компоненты, зависимости через глобальные singleton/fallback-поиск и необходимость регулярно прогонять Unity compile/tests перед релизом.

## Что уже приведено в порядок

- Удалены старые и дублирующие документы аудитов/планов/улучшений:
  - `Docs/NO_CODE_AUDIT.md`
  - `Docs/Plan_RemoveDeprecatedScripts.md`
  - `Docs/NEXT_IMPROVEMENTS.md`
  - `Docs/README_IMPROVEMENTS.md`
  - `Docs/Tools/Inventory/InventoryHand_Plan.md`
  - `Docs/Tools/Components/SCRIPT_IMPROVEMENTS.md`
  - `Docs/Bonus/Collection/IMPROVEMENTS.md`
- Убраны активные README-ссылки на удаленные документы.
- Оставлен актуальный реестр удаления: `Docs/DEPRECATED_OR_REMOVAL_CANDIDATES.md`.
- Оставлен предметный roadmap редакторского UX: `Docs/Condition/NeoCondition_Editor_Roadmap.md`.

## Приоритеты

### P0

- Закрыть Unity Editor или выполнить проверки через уже открытый Editor и прогнать compile/import без ошибок.
- Missing scripts scan уже чистый; повторять перед релизом после Unity import.

### P1

- Поддерживать документацию в UTF-8; поврежденные активные docs-файлы уже восстановлены.
- Расширять тесты `Audio`, `Parallax`, `PropertyAttribute` дальше за пределы добавленных smoke/edit-mode проверок.
- Не добавлять новые runtime `Debug.Log` без явного debug-флага; обычные шумные логи в package-коде уже загейчены или оставлены только как user-configured logging action.
- Продолжить управляемое удаление legacy API через `DEPRECATED_OR_REMOVAL_CANDIDATES.md`: `TimeReward`, `AiNavigation`, legacy AttackSystem, integer API в Shop, `chanseWin` в Slot.

### P2

- Static reset проверен для `QuestManager`, `ProgressionManager`, `SaveManager`, `NetworkSingleton`, `Bootstrap`, `MouseInputManager`; для `SwipeController` добавлен subsystem reset.
- Fallback-зависимости сокращены в точках без явного override/throttle: `NpcTargetFinder`, `BillboardUniversal`. Существующие `NeoCondition`/`NoCode` fallback-поиски уже кешируются и throttle-ятся.
- Docs-only раздел `Docs/Gameplay` удален из RU/EN документации и индексов.
- Пакетная совместимость сверена и зафиксирована в `Docs/PackageCompatibility.md`.

### P3

- Довести английскую документацию до уровня русской.
- Уплотнить README-навигацию: оставить один канонический вход на модуль, без исторических планов и разрозненных backlog-файлов.

## Матрица модулей

| Модуль | Статус | Что улучшить следующим |
| --- | --- | --- |
| Animations | OK | Поддерживать текущую документацию и тесты. |
| Audio | Требует тестов | Добавить smoke-тесты публичных компонентов и настроек. |
| Bonus | Требует cleanup | Разобрать legacy `TimeReward`, `WheelFortune` логи, Slot typo compatibility. |
| Cards | Требует cleanup | Убрать console spam в `DrunkardGame`, расширить тесты правил и edge cases. |
| Condition | OK / roadmap | Продолжить editor UX roadmap, держать reflection-контракты тестируемыми. |
| Core | OK | Поддерживать как стабильный foundation; не расширять без явной необходимости. |
| Extensions | OK с рисками static | Проверить lifecycle helper/singleton reset и отсутствие утечек корутин. |
| Gameplay | Решено: не отдельный модуль | Docs-only раздел удален; gameplay-функции принадлежат конкретным runtime-модулям (`Rpg`, `Quest`, `Progression`, `Cards`, `GridSystem`, `Tools`, `NoCode`). |
| GridSystem | OK | Поддерживать sample/docs после исправления `Samples~` путей. |
| Level | OK | Добавить сценарные проверки загрузки/переходов при росте API. |
| Network | Сильный | Проверить Mirror sample в PlayMode, держать docs рядом с компонентами. |
| NoCode | OK / требует UX дисциплины | Не расширять inspector-only логику без тестируемых C# контрактов. |
| NPC | Требует dependency hygiene | Уменьшить runtime-поиск target/camera, добавить негативные сценарии. |
| Parallax | Требует тестов | Добавить минимальные edit/play smoke-тесты. |
| Progression | OK | Проверить singleton reset и save/load сценарии. |
| PropertyAttribute | Требует тестов | Добавить editor tests для drawer/attribute поведения. |
| Quest | OK | Проверить static state reset и scene reload сценарии. |
| Reactive | OK | Сохранить маленьким и тестируемым. |
| Rpg | Сильный | Держать legacy AttackSystem bridge документированным, расширить combat edge tests. |
| Save | Сильный | Проверить missing `SaveProviderSettings` fallback и save/load сценарии в Unity. |
| Settings | OK | Добавить проверки default/missing settings при расширении. |
| Shop | Требует migration | Довести миграцию от integer API к typed API перед v9. |
| StateMachine | OK / требует lifecycle checks | Проверить lifecycle при enable/disable/reload. |
| Tools | Сильный, широкий | Разделять deprecated, runtime и editor tools; не смешивать новые feature-доки со старыми планами. |
| UI | Требует cleanup | Проверить TMP/uGUI пути и navigation. |

## Проверки

- NeoDoc links: `OK`, 209 ссылок валидны.
- Docs/scripts verification: `OK`.
- Старые `audit/plan/improvements` документы: удалены из активных docs.
- Active markdown mojibake scan: `OK`.
- Missing scripts scan: `OK`.
- Duplicate Unity GUIDs: не обнаружены.
- `git diff --check`: без ошибок; Git предупреждает только о будущей CRLF-нормализации.

## Остаточный риск

Unity compile/EditMode/PlayMode проверки через отдельный batchmode не завершены, потому что проект открыт в Unity (`Unity.exe`, projectPath `D:\unity\UnityProject\NeoxiderTools`). До релиза обязательно прогнать compile/import и тесты через текущую открытую сессию Unity или после закрытия Editor.

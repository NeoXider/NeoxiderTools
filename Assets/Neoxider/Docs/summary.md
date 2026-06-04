# Сводка документации (RU)

Краткая навигация для AI-агентов и программистов:

- главный вход RU: [README.md](./README.md);
- главный вход EN: [../DocsEn/README.md](../DocsEn/README.md);
- компактная карта проекта: [../PROJECT_SUMMARY.md](../PROJECT_SUMMARY.md);
- перед новой механикой проверяйте reuse map в этих входных файлах.

Особенно часто уже есть готовые блоки:

- `GridSystem` + `Merge` + `Dice` для сеток, placement, connected-group merge и Dice Merge;
- `AnimationFly` для reward fly UI/world animations, включая sample-сцену `AnimationFlyDemo` с кнопками и подписанными слайдерами;
- `Cards` для deck/hand/card view workflows, включая duplicate-safe операции и lifecycle;
- `Save`, `Core`, `Shop`, `Money`, `Rpg`, `Progression`, `Quest`, `StateMachine` для типовых gameplay systems;
- `Tools` для movement, spawners, timers, input, physics и view helpers.

Статус проверки: `NeoDoc` ссылки и mojibake scan обновлены на 2026-06-04.

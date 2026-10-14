# HTML/CSS → UI Toolkit: утверждённое решение, 2026-10-14
Цель владельца: включить лучшие подходящие возможности кандидатов в NeoxiderTools, довести конвертер до проверенной поставки и запушить.
Основа: собственный HtmlToUitk из VampireDota; сохранить Core/Rules/Convert/Bake и объединить их в рабочий CLI-маршрут.
Канонический CLI: Tools/HtmlToUitk; Unity runtime/editor: Assets/Neoxider/Scripts/UI/HtmlToUitk, с отдельными asmdef и без CLI DLL в player.
Переиспользовать лицензированные принципы HTML/CSS conversion, CSS media/gradient baking и явных action bindings; источники/лицензии записать, FUI без лицензии не копировать.
Обязательные функции: cascade/layout/tokens, media width/height/aspect, linear/radial/conic gradients, nav/modal/tabs/animations и программируемые C# кнопки.
Неподдерживаемый CSS/JS получает понятную диагностику и nonzero exit; никакого тихого PASS с потерей результата или растеризации локализуемого текста.
Runtime: один владелец размеров/подписок на root, dispose/reload без двойных callbacks, безопасное прерывание переходов; UIDocument и поддерживаемый PanelRenderer.
Волны: canonical copy/provenance → converter wiring + runtime/editor integration → complete CLI/Unity fixture → independent QA/review → documentation/version/Git.
Файлы исполнителей не пересекаются; источник final до сборок/импорта, один Unity-оператор; новый C# использует явные типы по AGENTS.
Гейты: dotnet tests/build и CLI golden integration; Unity native import/compile + actions/reload + visual captures на1280×720,1920×1080,3440×1440.
Градиенты проверять по реальным pixels/assets/render, адаптив — по bounds/resize/classes; тестировать actual CLI entrypoint, отрицательные inputs и idempotent outputs.
Версии: совместимость библиотеки Unity6.0+ сохраняется; реальная матрица доступных6000.3.14f1/6000.6.0f1, остальные версии не объявлять проверенными.
Риски: UI Toolkit ограничен CSS-подмножеством; media precedence, unsupported native gradients и жизненный цикл требуют явных проверок.
Root владеет решениями/review/Git; посторонний Physics2DSettings.asset сохранён побайтно, не включать в commit; main обновлён4ca6f50f.

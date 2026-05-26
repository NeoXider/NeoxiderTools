# AGENTS.md

## Идентичность проекта

NeoxiderTools - это модульная Unity C# constructor-библиотека, а не кодовая база одной конкретной игры.

Библиотека должна помогать разработчику быстро собирать игровые системы из готовых блоков, сохраняя явный, тестируемый и переиспользуемый runtime API. Главная ценность проекта - баланс между:

- универсальными, компонуемыми C# системами;
- удобными MonoBehaviour-обертками для сценовой работы;
- опциональными NoCode/Inspector workflows для быстрого прототипирования;
- стабильными публичными API, которые можно использовать из обычного C# без обязательной scene-only связки.

## Основные принципы

- Рассматривать каждый модуль как строительный блок для разных жанров, а не как одноразовую демо-механику.
- Предпочитать маленькие сфокусированные сервисы с ясными контрактами вместо больших менеджеров, владеющих несвязанным поведением.
- Чистую игровую логику держать в обычном C# там, где это практично; MonoBehaviour использовать как сценовые адаптеры и authoring wrappers.
- NoCode-компоненты - это дополнительный слой удобства. Они должны оборачивать тестируемые C# контракты, а не заменять их.
- ScriptableObject должен хранить переиспользуемые данные и конфигурацию. Он не должен зависеть от ссылок на scene objects для runtime-поведения.
- Сохранять serialized fields, публичные API, prefabs, scenes и package compatibility, если миграция не является осознанной и задокументированной.
- Делать системы удобными для композиции: разработчик должен подключать только тот функционал, который нужен конкретной игре.
- Runtime-логи должны быть gated, throttled или debug-only.
- Предпочитать явные зависимости через serialized fields или initialization methods; scene search использовать только как fallback.
- Для нового модуля или важной фичи нужны хотя бы smoke/edit-mode тесты публичных контрактов.

## Архитектура библиотеки

Каждая gameplay-область должна строиться так:

- тестируемое runtime-ядро;
- опциональные Unity component wrappers;
- опциональные demo/sample views;
- документация рядом с модулем;
- editor или NoCode helpers только там, где они улучшают authoring и не скрывают core API.

Примеры:

- GridSystem - конфигурируемый конструктор поля плюс подключаемые механики: pathfinding, match rules, sliding/merge rules, board views, object spawning.
- Cards - custom decks, custom card data, независимые card views, board/hand/deck components, пригодные не только для одной карточной игры.
- StateMachine - чистая C# state-логика сначала, затем scene/NoCode wrappers.
- Save, Quest, Progression, Network, UI, Audio, Tools и похожие модули должны оставаться модульными и независимо используемыми.

## Направление GridSystem

GridSystem - базовый конструктор для сеточных игр.

Он должен поддерживать разные типы игр и систем: Match3, TicTacToe, 2048-like sliding/merge games, tactics, board games, inventory grids, puzzle fields, custom-shaped boards и будущие game-specific layers.

Ожидания по дизайну:

- `FieldGenerator` владеет формой поля, клетками, координатами, состоянием и conversion helpers.
- Игровые правила вроде Match3 или SlidingMerge живут в отдельных подключаемых сервисах.
- Views заменяемые. Grid game не должна требовать конкретный demo view, чтобы работать.
- Cell state должен оставаться достаточно универсальным для custom games: enabled, walkable, occupied, content id, type, flags и optional payload.
- Shape masks, disabled cells, blockers и custom movement rules должны учитываться higher-level системами.

## Направление NoCode

NoCode существует для быстрой сборки сцен и итерации, но должен оставаться профессиональным:

- scene components могут ссылаться на scene objects;
- ScriptableObjects не должны хранить scene object references;
- NoCode actions должны вызывать typed runtime APIs;
- у каждого важного NoCode поведения должен быть эквивалентный C# API path;
- сложная игровая логика не должна быть заперта внутри UnityEvent chains.

## Документация

Документация должна объяснять оба слоя:

- runtime API для программистов;
- scene/NoCode workflow для быстрой настройки.

Документацию нужно поддерживать актуальной на русском и английском, где это возможно. Исторические backlog/plan-файлы не должны быть основным входом в модуль. Для каждого модуля нужен один канонический README/entry point.

## Samples / UPM Packaging

Сейчас sample-сцены и sample-код ведутся в рабочем developer-пути `Assets/Neoxider/Samples`, потому что идет активная разработка и сцены должны быть видны в проекте.

Перед финальной упаковкой/релизом sample-папки снова переводятся в UPM-формат `Assets/Neoxider/Samples~`, а `package.json.samples[].path` должен указывать на `Samples~/...`.

После импорта UPM sample через Unity Package Manager содержимое `Samples~` копируется Unity не обратно в пакет, а в проектный путь вида `Assets/Samples/NeoxiderTools/<version>/<sample name>/...`. Для текущей версии это, например, `Assets/Samples/NeoxiderTools/9.0.0/Demo Scenes/...`.

Правила для агентов:

- во время разработки не считать отсутствие `Samples~` ошибкой, если есть рабочий `Samples`;
- тесты и validation helpers должны уметь работать с текущим активным sample root: `Assets/Neoxider/Samples`, `Assets/Neoxider/Samples~` или импортированным Unity root `Assets/Samples/NeoxiderTools/<version>/<sample name>`;
- документация может указывать текущий dev path `Assets/Neoxider/Samples/...`, но должна явно помнить release path `Assets/Neoxider/Samples~/...`;
- перед релизной финализацией обязательно сверить rename `Samples` -> `Samples~`, package sample paths, imported sample expectations, docs и changelog.

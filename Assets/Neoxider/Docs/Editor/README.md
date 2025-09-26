# Модуль Editor

Модуль **Editor** содержит набор мощных утилит и инструментов для редактора Unity. Эти скрипты не попадают в финальную сборку игры, но значительно упрощают и ускоряют процесс разработки.

Здесь собраны инструменты для очистки проекта, создания резервных копий, массового редактирования ассетов, генерации кода и расширения стандартного инспектора Unity для автоматизации рутинных задач.

## Документация по скриптам

### Корневые скрипты
- [**Find & Remove Missing Scripts**](./FindAndRemoveMissingScriptsWindow.md): Окно для поиска и удаления "потерянных" скриптов во всем проекте.
- [**Save Project Zip**](./SaveProjectZip.md): Утилита для создания ZIP-архива проекта.
- [**Texture Max Size Changer**](./TextureMaxSizeChanger.md): Инструмент для массового изменения максимального размера текстур.

### Подмодули

- [Create](#create)
- [Main](#main)
- [PropertyAttribute](#propertyattribute)
- [Scene](#scene)

#### Create
- [**CreateMenuObject**](./CreateMenuObject.md): Добавляет в меню `GameObject` быстрые команды для создания объектов и префабов из ассета.
- [**SingletonCreator**](./SingletonCreator.md): Утилита для быстрой генерации C# скриптов для синглтонов по шаблону.

#### Main
- [**CreateSceneHierarchy**](./CreateSceneHierarchy.md): Инструмент для создания стандартной иерархии объектов-разделителей на сцене.
- [**NeoxiderSettings**](./NeoxiderSettings.md): Статический класс для управления настройками всего ассета.
- [**NeoxiderSettingsWindow**](./NeoxiderSettingsWindow.md): Окно настроек ассета.

#### PropertyAttribute
- [**ComponentDrawer**](./ComponentDrawer.md): Часть кастомного инспектора, отвечающая за атрибуты поиска компонентов.
- [**CustomEditorBase**](./CustomEditorBase.md): Базовый класс для инспекторов, добавляющий поддержку `[Button]` атрибута.
- [**NeoCustomEditor**](./NeoCustomEditor.md): Главный кастомный инспектор, активирующий всю магию атрибутов.
- [**ResourceDrawer**](./ResourceDrawer.md): Часть инспектора, отвечающая за атрибуты загрузки из папок `Resources`.

#### Scene
- [**SceneSaver**](./SceneSaver.md): Утилита для автоматического фонового сохранения резервных копий сцены.

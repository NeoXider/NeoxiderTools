# Editor Windows

Модуль Editor Windows предоставляет архитектуру для создания окон редактора Unity с разделением логики и отрисовки GUI.

## Архитектура

Все окна редактора используют паттерн разделения ответственности:
- **EditorWindow** классы содержат только логику управления окном
- **EditorWindowGUI** классы содержат всю отрисовку GUI

Это обеспечивает:
- Чистую архитектуру с разделением ответственности
- Легкое тестирование и поддержку
- Переиспользование GUI компонентов

## Базовый класс

### EditorWindowGUI

Абстрактный базовый класс для всех GUI отрисовщиков окон.

```csharp
public abstract class EditorWindowGUI
{
    public abstract void OnGUI(EditorWindow window);
}
```

## Доступные окна

### NeoxiderSettingsWindow

**Путь меню:** `Tools → Neoxider → Settings`

Окно для управления глобальными настройками Neoxider:
- Общие настройки (поиск атрибутов, валидация папок)
- Структура папок проекта
- Настройки иерархии сцены

**GUI класс:** `NeoxiderSettingsWindowGUI`

### SceneSaver

**Путь меню:** `Tools → Neoxider → Scene Saver`

Утилита для автоматического сохранения резервных копий сцен:
- Настраиваемый интервал сохранения
- Автоматическое сохранение в фоне
- Сохранение даже если сцена не изменена

**GUI класс:** `SceneSaverGUI`

### FindAndRemoveMissingScriptsWindow

**Путь меню:** `Tools → Neoxider → Find & Remove Missing Scripts`

Окно для поиска и удаления Missing Scripts:
- Поиск во всех сценах и префабах
- Визуальный список найденных объектов
- Массовое или индивидуальное удаление

**GUI класс:** `FindAndRemoveMissingScriptsWindowGUI`

### TextureMaxSizeChanger

**Путь меню:** `Tools → Neoxider → Change Texture Max Size`

Инструмент для массового изменения максимального размера текстур:
- Фильтрация по типу текстуры
- Прогресс-бар обработки
- Подтверждение перед применением

**GUI класс:** `TextureMaxSizeChangerGUI`

## Создание нового окна

### Шаг 1: Создайте GUI класс

```csharp
using UnityEditor;
using Neo.Editor.GUI;

namespace Neo.Editor.GUI
{
    public class MyWindowGUI : EditorWindowGUI
    {
        public override void OnGUI(EditorWindow window)
        {
            EditorGUILayout.LabelField("My Window");
            // Ваша отрисовка GUI
        }
    }
}
```

### Шаг 2: Создайте EditorWindow класс

```csharp
using UnityEditor;
using Neo.Editor.GUI;

namespace Neo
{
    public class MyWindow : EditorWindow
    {
        private MyWindowGUI _gui;

        [MenuItem("Tools/Neoxider/My Window")]
        public static void ShowWindow()
        {
            GetWindow<MyWindow>("My Window");
        }

        private void OnEnable()
        {
            _gui = new MyWindowGUI();
        }

        private void OnGUI()
        {
            _gui?.OnGUI(this);
        }
    }
}
```

## Преимущества архитектуры

1. **Разделение ответственности**: Логика окна отделена от отрисовки
2. **Тестируемость**: GUI классы можно тестировать независимо
3. **Переиспользование**: GUI компоненты можно использовать в разных окнах
4. **Чистота кода**: EditorWindow классы содержат минимум кода
5. **Профессиональная структура**: Соответствует лучшим практикам Unity разработки


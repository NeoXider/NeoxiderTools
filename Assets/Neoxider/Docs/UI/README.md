# Модуль UI

**Что это:** раздел документации по UI: анимации кнопок (ButtonScale, ButtonShake), страницы (UI, ButtonChangePage), пауза (PausePage), переключатели (VisualToggle, VariantView), FakeLoad, AnimationFly, AnchorMove. Скрипты в `Scripts/UI/`.

**Навигация:** [← К Docs](../README.md) · оглавление — список ниже

### Корневые скрипты
- [**AnchorMove**](./AnchorMove.md): Утилита для удобного редактирования якорей `RectTransform`.
- [**AnimationFly**](./AnimationFly.md): Менеджер для создания анимации "летящих" UI-элементов (монеты, бонусы).
- [**PausePage**](./PausePage.md): Компонент для создания окон, ставящих игру на паузу.
- [**UIReady**](./UIReady.md): Устаревший. Набор методов для кнопок (сцены, выход, пауза). Используйте [SceneFlowController](../Level/SceneFlowController.md) (модуль Level).

### Подмодули

- [Animation](#animation)
- [Simple](#simple)
- [View](#view)

#### Animation
- [**ButtonScale**](./ButtonScale.md): Компонент для создания эффекта "нажатия" (уменьшения) кнопки.
- [**ButtonShake**](./ButtonShake.md): Компонент для создания эффекта "тряски" кнопки.

#### Simple
- [**ButtonChangePage**](./ButtonChangePage.md): Кнопка для переключения страниц в менеджере `UI`.
- [**FakeLoad**](./FakeLoad.md): Компонент для имитации процесса загрузки.
- [**UI**](./UI.md): Менеджер для управления UI-панелями (страницами).

#### View
- [**VisualToggle**](./VisualToggle.md): Универсальный переключатель между двумя визуальными состояниями с поддержкой множественных элементов, UnityEvent и интеграцией с Toggle.
- [**VariantView**](./VariantView.md): Мощный конструктор для управления множеством визуальных состояний элемента по индексу.

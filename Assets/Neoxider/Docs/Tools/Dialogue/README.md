# Диалоговая система

Модульная система для управления диалогами в игре. Разделена на независимые компоненты для гибкости и переиспользования.

## Компоненты

| Компонент | Описание |
|-----------|----------|
| [DialogueController](DialogueController.md) | Основная логика управления диалогами |
| [DialogueUI](DialogueUI.md) | Управление UI элементами |
| [DialogueData](DialogueData.md) | Структуры данных (Dialogue, Monolog, Sentence) |

## Редактор диалогов

Визуальный редактор на **UI Toolkit** (окно без обрезки полей справа).

- **Открыть:** кнопка **Open Dialogue Editor** в инспекторе `DialogueController` или меню **Neoxider → Tools → Dialogue → Open Dialogue Editor**.
- В окне: поле **Controller** (можно выбрать другой контроллер), кнопки **Select In Scene** и **Docs**.
- **Левая панель** — структура: Dialogues → Monologues → Sentences. Кнопки `+` / `−` / `↑` / `↓` для добавления, удаления и перемещения. Выбор элемента — клик по строке.
- **Правая панель** — детали выбранного: события (On Change Dialogue / Monolog / Sentence), поля (Character, Sprite, Text), блок **Preview** для реплики.
- События настраиваются no-code в окне или через код.

## Быстрый старт

1. Добавьте `DialogueController` на GameObject
2. Добавьте `DialogueUI` (можно на тот же объект)
3. Назначьте UI элементы в `DialogueUI`
4. Заполните массив `dialogues` в `DialogueController`
5. Вызовите `StartDialogue(0)` для запуска

## Пример использования

```csharp
// Запуск диалога
dialogueController.StartDialogue(0);

// Пропуск/переход
dialogueController.SkipOrNext();

// Перезапуск
dialogueController.RestartDialogue();

// Текущий контекст (для условий, аналитики, саунда)
Dialogue d = dialogueController.GetCurrentDialogue();
Monolog m = dialogueController.GetCurrentMonolog();
Sentence s = dialogueController.GetCurrentSentence();
```

## TypewriterEffect

Отдельный модуль эффекта печатной машинки, который можно использовать независимо:

```csharp
// Использование напрямую из кода
var typewriter = new TypewriterEffect(50f); // 50 символов/сек
await typewriter.PlayAsync("Привет!", text => myText.text = text);

// Или через компонент
typewriterComponent.Play("Привет!");
```









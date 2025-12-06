# DialogueUI

Компонент для управления UI элементами диалога.

**Namespace:** `Neo.Tools`  
**Путь:** `Assets/Neoxider/Scripts/Tools/Dialogue/DialogueUI.cs`

## Описание

Отвечает за отображение текста диалога, имени персонажа и его изображения. Можно использовать отдельно от `DialogueController`.

## Публичные поля

| Поле | Тип | Описание |
|------|-----|----------|
| `characterImage` | `Image` | Изображение персонажа |
| `characterNameText` | `TMP_Text` | Текст имени персонажа |
| `dialogueText` | `TMP_Text` | Текст диалога |
| `setNativeSize` | `bool` | Устанавливать нативный размер изображения |

## Публичные методы

| Метод | Возврат | Описание |
|-------|---------|----------|
| `SetCharacterName(string)` | `void` | Устанавливает имя персонажа |
| `SetCharacterSprite(Sprite)` | `void` | Устанавливает спрайт персонажа |
| `SetDialogueText(string)` | `void` | Устанавливает текст диалога |
| `ClearDialogueText()` | `void` | Очищает текст диалога |
| `Reset()` | `void` | Сбрасывает состояние UI |

## События (UnityEvent)

| Событие | Параметры | Описание |
|---------|-----------|----------|
| `OnCharacterChange` | `string` | Вызывается при смене персонажа |









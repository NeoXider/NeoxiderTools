# DialogueController

Основной контроллер диалоговой системы.

**Namespace:** `Neo.Tools`  
**Путь:** `Assets/Neoxider/Scripts/Tools/Dialogue/DialogueController.cs`

## Описание

Управляет потоком диалогов, монологов и предложений. Поддерживает эффект печатной машинки через UniTask и автопереходы.

## Публичные поля

| Поле | Тип | Описание |
|------|-----|----------|
| `useTypewriterEffect` | `bool` | Использовать эффект печати |
| `charactersPerSecond` | `float` | Скорость печати (символов/сек) |
| `autoNextSentence` | `bool` | Автопереход к следующему предложению |
| `autoNextMonolog` | `bool` | Автопереход к следующему монологу |
| `autoNextDialogue` | `bool` | Автопереход к следующему диалогу |
| `allowRestart` | `bool` | Разрешить перезапуск диалога |
| `autoNextSentenceDelay` | `float` | Задержка перед автопереходом (сек) |
| `autoNextMonologDelay` | `float` | Задержка перед автопереходом монолога |
| `autoNextDialogueDelay` | `float` | Задержка перед автопереходом диалога |
| `dialogues` | `Dialogue[]` | Массив диалогов |

## Публичные свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `CurrentDialogueId` | `int` | Текущий индекс диалога |
| `CurrentMonologId` | `int` | Текущий индекс монолога |
| `CurrentSentenceId` | `int` | Текущий индекс предложения |
| `IsTyping` | `bool` | Идёт ли печать текста |

## Публичные методы

| Метод | Возврат | Описание |
|-------|---------|----------|
| `StartDialogue(int, int, int)` | `void` | Запускает диалог с указанными индексами |
| `StartDialogue(int)` | `void` | Запускает диалог по индексу |
| `NextSentence()` | `void` | Переход к следующему предложению |
| `NextMonolog()` | `void` | Переход к следующему монологу |
| `NextDialogue()` | `void` | Переход к следующему диалогу |
| `SkipOrNext()` | `void` | Пропускает печать или переходит дальше |
| `RestartDialogue()` | `void` | Перезапускает текущий диалог |

## События (UnityEvent)

| Событие | Параметры | Описание |
|---------|-----------|----------|
| `OnSentenceEnd` | — | Вызывается при завершении предложения |
| `OnMonologEnd` | — | Вызывается при завершении монолога |
| `OnDialogueEnd` | — | Вызывается при завершении диалога |
| `OnCharacterChange` | `string` | Вызывается при смене персонажа |









# DialogueData

Структуры данных для диалоговой системы.

**Namespace:** `Neo.Tools`  
**Путь:** `Assets/Neoxider/Scripts/Tools/Dialogue/DialogueData.cs`

## Классы

### Dialogue

Данные одного диалога, содержащего несколько монологов.

| Поле | Тип | Описание |
|------|-----|----------|
| `OnChangeDialog` | `UnityEvent<int>` | Событие при смене диалога |
| `monologues` | `Monolog[]` | Массив монологов |

### Monolog

Данные монолога одного персонажа.

| Поле | Тип | Описание |
|------|-----|----------|
| `OnChangeMonolog` | `UnityEvent<int>` | Событие при смене монолога |
| `characterName` | `string` | Имя персонажа |
| `sentences` | `Sentence[]` | Массив предложений |

### Sentence

Данные одного предложения в диалоге.

| Поле | Тип | Описание |
|------|-----|----------|
| `OnChangeSentence` | `UnityEvent` | Событие при показе предложения |
| `sprite` | `Sprite` | Спрайт персонажа для этого предложения |
| `sentence` | `string` | Текст предложения |

## Структура иерархии

```
Dialogue[]
└── Monolog[]
    ├── characterName
    └── Sentence[]
        ├── sprite
        └── sentence (текст)
```






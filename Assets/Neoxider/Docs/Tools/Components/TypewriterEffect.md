# TypewriterEffect

Универсальный эффект печатной машинки для текста с поддержкой пауз на знаках препинания.

**Namespace:** `Neo.Tools`  
**Файлы:**
- `Assets/Neoxider/Scripts/Tools/Components/TypewriterEffect.cs`
- `Assets/Neoxider/Scripts/Tools/Components/TypewriterEffectComponent.cs`

## Описание

Модуль состоит из трёх частей:
- `PunctuationPause` — класс для настройки паузы на знаке препинания
- `TypewriterEffect` — класс для использования из кода (без MonoBehaviour)
- `TypewriterEffectComponent` — MonoBehaviour-обёртка для использования в инспекторе

## PunctuationPause (класс)

Настройка паузы для знака препинания.

| Поле | Тип | Описание |
|------|-----|----------|
| `character` | `char` | Символ знака препинания |
| `pause` | `float` | Дополнительная пауза в секундах |

## TypewriterEffect (класс)

### Конструкторы

```csharp
new TypewriterEffect()
new TypewriterEffect(float charactersPerSecond, bool useUnscaledTime = false)
```

### Статические поля

| Поле | Тип | Описание |
|------|-----|----------|
| `DefaultPunctuationPauses` | `PunctuationPause[]` | Предопределённый список пауз по умолчанию |

### Паузы по умолчанию

| Символ | Пауза (сек) |
|--------|-------------|
| `.` | 0.3 |
| `!` | 0.3 |
| `?` | 0.3 |
| `,` | 0.15 |
| `;` | 0.15 |
| `:` | 0.2 |
| `—` | 0.2 |
| `-` | 0.1 |
| `…` | 0.5 |
| `\n` | 0.2 |

### Публичные свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `CharactersPerSecond` | `float` | Скорость печати |
| `UseUnscaledTime` | `bool` | Игнорировать Time.timeScale |
| `UsePunctuationPauses` | `bool` | Использовать паузы на знаках препинания |
| `PunctuationPauses` | `List<PunctuationPause>` | Список пауз для знаков препинания |
| `IsTyping` | `bool` | Идёт ли печать |
| `Progress` | `float` | Прогресс (0-1) |
| `CurrentText` | `string` | Текущий напечатанный текст |
| `FullText` | `string` | Полный текст |
| `DelayMs` | `int` | Базовая задержка между символами (мс) |

### Публичные методы

| Метод | Возврат | Описание |
|-------|---------|----------|
| `PlayAsync(string, Action<string>, CancellationToken)` | `UniTask` | Запускает эффект асинхронно |
| `PlayAsync(string, TMP_Text, CancellationToken)` | `UniTask` | Запускает эффект с TMP_Text |
| `Stop()` | `void` | Останавливает эффект |
| `Complete()` | `string` | Завершает мгновенно, возвращает полный текст |
| `Reset()` | `void` | Сбрасывает состояние |
| `SetDefaultPunctuationPauses()` | `void` | Устанавливает паузы по умолчанию |
| `ClearPunctuationPauses()` | `void` | Очищает все паузы |
| `SetPunctuationPause(char, float)` | `void` | Добавляет/обновляет паузу для символа (в секундах) |
| `SetPunctuationPauses(Dictionary<char, float>)` | `void` | Устанавливает паузы из словаря (заменяет текущие) |
| `AddPunctuationPauses(Dictionary<char, float>)` | `void` | Добавляет паузы из словаря (объединяет с текущими) |
| `RemovePunctuationPause(char)` | `void` | Удаляет паузу для символа |
| `GetPunctuationPause(char)` | `float` | Получает паузу для символа в секундах (0 если нет) |
| `RebuildPauseMap()` | `void` | Перестраивает внутренний словарь пауз |

### События

| Событие | Тип | Описание |
|---------|-----|----------|
| `OnStart` | `Action` | При старте печати |
| `OnComplete` | `Action` | При завершении |
| `OnCharacterTyped` | `Action<char>` | При печати символа |
| `OnProgressChanged` | `Action<float>` | При изменении прогресса |

### Пример использования из кода

```csharp
var typewriter = new TypewriterEffect(50f);

// С callback
await typewriter.PlayAsync("Привет!", text => myText.text = text);

// С TMP_Text
await typewriter.PlayAsync("Привет!", tmpText);

// С отменой
var cts = new CancellationTokenSource();
await typewriter.PlayAsync("Текст", tmpText, cts.Token);
cts.Cancel(); // Отменить

// Настройка пауз для знаков препинания (в секундах)
typewriter.SetPunctuationPause('.', 0.5f);  // Увеличить паузу на точке
typewriter.SetPunctuationPause('!', 0.4f);  // Добавить паузу на восклицательном знаке
typewriter.RemovePunctuationPause(',');     // Убрать паузу на запятой

// Установить паузы из словаря (заменяет все текущие)
typewriter.SetPunctuationPauses(new Dictionary<char, float>
{
    { '.', 0.5f },
    { ',', 0.2f },
    { '!', 0.4f }
});

// Добавить паузы из словаря (объединяет с текущими)
typewriter.AddPunctuationPauses(new Dictionary<char, float>
{
    { '?', 0.35f },
    { ';', 0.1f }
});

// Отключить паузы на знаках препинания
typewriter.UsePunctuationPauses = false;

// Сбросить на паузы по умолчанию
typewriter.SetDefaultPunctuationPauses();
```

## TypewriterEffectComponent (MonoBehaviour)

### Настройки в инспекторе

| Поле | Тип | По умолчанию | Описание |
|------|-----|--------------|----------|
| `_targetText` | `TMP_Text` | — | Целевой текстовый компонент. Если не указан, ищется на этом объекте |
| `_autoStart` | `bool` | `true` | Автоматически запустить эффект при `Start()` |
| `_playOnEnable` | `bool` | `false` | Запускать эффект каждый раз при `OnEnable()` |
| `_autoStartText` | `string` | — | Текст для автозапуска. Если пусто, берётся из `TargetText` |

### Публичные свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `Effect` | `TypewriterEffect` | Внутренний экземпляр эффекта |
| `TargetText` | `TMP_Text` | Целевой текстовый компонент |
| `IsTyping` | `bool` | Идёт ли печать |
| `Progress` | `float` | Прогресс (0-1) |

### Публичные методы

| Метод | Возврат | Описание |
|-------|---------|----------|
| `Play(string text = "")` | `void` | Запускает эффект. Если text пустой — берёт из TargetText |
| `PlayAutoText()` | `void` | Запускает эффект с текстом из AutoStartText или TargetText |
| `Complete()` | `void` | Завершает мгновенно |
| `Stop()` | `void` | Останавливает |
| `Clear()` | `void` | Очищает текст |
| `TrySkip()` | `bool` | Пропускает если печатает |

**Примечание:** Метод `Play()` можно вызвать без параметров из UI кнопки — текст автоматически возьмётся из TMP_Text компонента.

### События (UnityEvent)

| Событие | Параметры | Описание |
|---------|-----------|----------|
| `OnStart` | — | При старте печати |
| `OnComplete` | — | При завершении |
| `OnCharacterTyped` | `char` | При печати символа |
| `OnProgressChanged` | `float` | При изменении прогресса |

### Пример использования в инспекторе

1. Добавьте компонент `TypewriterEffectComponent` на объект
2. Назначьте `TMP_Text` в поле `Target Text` (или компонент найдётся автоматически)
3. Включите `Auto Start` для автоматического запуска
4. Укажите текст в `Auto Start Text` или оставьте пустым (возьмётся из текстового компонента)
5. Включите `Play On Enable` если нужно перезапускать эффект при каждом включении объекта

### Пример использования из кода

```csharp
// Получаем компонент
var typewriter = GetComponent<TypewriterEffectComponent>();

// Запускаем эффект с текстом
typewriter.Play("Новый текст для печати");

// Или без параметров - возьмёт текст из TMP_Text
typewriter.Play();

// Пропустить/завершить мгновенно
if (Input.GetKeyDown(KeyCode.Space))
{
    typewriter.TrySkip();
}
```

### Пример настройки через UI кнопку

```
TMP_Text: "Привет, игрок!"
├── TypewriterEffectComponent
    └── Auto Start → ☐ (выключен)

Button
└── OnClick()
    └── TypewriterEffectComponent → Play()  ← вызов без параметров!
```

Текст автоматически возьмётся из TMP_Text компонента.


# CardComponent

Компонент карты для работы без кода с полной поддержкой анимаций и интерактивности.

---

## Описание

No-code компонент для отображения игральной карты. Поддерживает:
- Переворот лицом вверх/вниз
- Анимации перемещения и переворота
- Hover эффект
- UnityEvent для настройки без кода

---

## Настройки в инспекторе

### Config

| Поле | Описание |
|------|----------|
| **Config** | DeckConfig со спрайтами |
| **Suit** | Масть карты |
| **Rank** | Ранг карты |
| **Is Joker** | Является ли джокером |
| **Is Red Joker** | Красный ли джокер |

### State

| Поле | Описание |
|------|----------|
| **Is Face Up** | Показана лицом вверх |
| **Is Interactable** | Можно ли кликать |

### Visual

| Поле | Описание |
|------|----------|
| **Card Image** | UI Image для отображения (для UI) |
| **Sprite Renderer** | SpriteRenderer для отображения (для 2D) |

### Animation

| Поле | Значение по умолчанию | Описание |
|------|----------------------|----------|
| **Flip Duration** | 0.3 сек | Длительность переворота |
| **Move Duration** | 0.2 сек | Длительность перемещения |
| **Flip Ease** | OutQuad | Тип easing для переворота |
| **Move Ease** | OutQuad | Тип easing для перемещения |

### Hover Effect

| Поле | Значение по умолчанию | Описание |
|------|----------------------|----------|
| **Enable Hover Effect** | true | Включить эффект наведения |
| **Hover Scale** | 0.1 | Дельта увеличения (0.1 = +10%). **Если 0 — эффект масштаба отключен** |
| **Hover Y Offset** | 20 | Подъём вверх в пикселях. **Если 0 — эффект перемещения отключен** |
| **Hover Duration** | 0.15 сек | Скорость анимации |

**Примеры настройки:**
- `Hover Scale = 0.1, Hover Y Offset = 20` → увеличение + подъём
- `Hover Scale = 0, Hover Y Offset = 20` → только подъём (без масштаба)
- `Hover Scale = 0.1, Hover Y Offset = 0` → только увеличение (без перемещения)
- `Hover Scale = 0, Hover Y Offset = 0` → hover полностью отключен

**Примеры Hover Scale:**
- `0` → эффект масштаба отключен
- `0.1` → увеличение на 10% (станет 110% размера)
- `0.2` → увеличение на 20% (станет 120% размера)
- `-0.1` → уменьшение на 10% (станет 90% размера)

---

## События (UnityEvent)

| Событие | Когда вызывается |
|---------|------------------|
| `OnClick` | Клик по карте |
| `OnFlip` | Карта перевернулась |
| `OnMoveComplete` | Перемещение завершено |
| `OnHoverEnter` | Курсор наведён на карту |
| `OnHoverExit` | Курсор покинул карту |

### Пример настройки в инспекторе

```
OnClick:
  → GameManager.CardClicked
  
OnFlip:
  → AudioSource.PlayOneShot (cardFlipSound)
```

---

## Методы

### SetData

```csharp
public void SetData(CardData data, bool faceUp = true);
```

Устанавливает данные карты и обновляет визуал.

### Flip / FlipAsync

```csharp
[Button]
public void Flip();

public async UniTask FlipAsync();
public async UniTask FlipAsync(float duration);
```

Переворачивает карту (с анимацией или без).

### MoveToAsync / MoveToLocalAsync

```csharp
public async UniTask MoveToAsync(Vector3 position);
public async UniTask MoveToAsync(Vector3 position, float duration);
public async UniTask MoveToLocalAsync(Vector3 localPosition, float duration);
```

Перемещает карту в позицию с анимацией.

### UpdateOriginalTransform

```csharp
public void UpdateOriginalTransform();
```

Обновляет сохранённую позицию и масштаб. Вызывается автоматически HandComponent после расстановки карт.

### ResetHover

```csharp
public void ResetHover();
```

Сбрасывает hover эффект с анимацией.

---

## Свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `Data` | `CardData` | Данные карты |
| `IsFaceUp` | `bool` | Показана лицом вверх (сеттер обновляет визуал) |
| `IsInteractable` | `bool` | Можно ли кликать |
| `Config` | `DeckConfig` | Конфигурация колоды (сеттер обновляет визуал) |

---

## Особенности реализации

### Сохранение масштаба

Компонент использует две переменные для масштаба:
- `_originalScale` — неизменный масштаб из префаба (устанавливается в Awake)
- `_currentTargetScale` — текущий целевой масштаб (обновляется при перемещении)

При анимации переворота:
1. Сохраняется текущий масштаб **до** начала анимации
2. Карта сжимается по X до 0
3. Меняется лицо/рубашка
4. Карта возвращается к **сохранённому** масштабу
5. Масштаб принудительно устанавливается для точности

**Результат:** Масштаб карты **никогда не искажается**, даже при множественных переворотах.

### Hover эффект

При наведении курсора (`IPointerEnterHandler`):
1. Сохраняется оригинальная позиция
2. Карта увеличивается на `_hoverScale` (дельта) от **текущего** масштаба
3. Карта поднимается на `_hoverYOffset` пикселей

При уходе курсора (`IPointerExitHandler`):
1. Карта возвращается к оригинальной позиции
2. Карта возвращается к оригинальному масштабу

**Автоматическая настройка:**
- В Awake автоматически включается `Image.raycastTarget = true` для работы hover эффекта
- Для UI карт убедитесь, что на сцене есть `EventSystem`

**Защита от багов:**
- Если hover срабатывает во время анимации — анимация завершается корректно
- Если повторный hover — предыдущий сбрасывается мгновенно
- Hover автоматически сбрасывается при начале новой анимации перемещения/переворота
- Использует **текущий** масштаб, а не оригинальный из Awake

### OnValidate

При изменении параметров в инспекторе (только в Edit Mode):
- Данные карты пересоздаются
- Визуал обновляется
- Работает только если назначен DeckConfig

---

## Пример использования

### В UI (Image)

```csharp
// Префаб карты
GameObject cardPrefab
├── Image (Card Image)
└── CardComponent
    ├── Config → DeckConfig
    ├── Card Image → Image
    └── События настроены
```

### В 2D (SpriteRenderer)

```csharp
GameObject cardPrefab
├── SpriteRenderer
└── CardComponent
    ├── Config → DeckConfig
    ├── Sprite Renderer → SpriteRenderer
    └── Hover Y Offset → 0.5 (для мирового пространства)
```

---

## См. также

- [CardData](./CardData.md) — структура данных карты
- [DeckConfig](./DeckConfig.md) — конфигурация колоды
- [HandComponent](./HandComponent.md) — компонент руки
- [CardView](./CardView.md) — MVP версия (для кода)


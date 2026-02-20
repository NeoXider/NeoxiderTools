# TextScore

Компонент для отображения очков из **ScoreManager** без кода. Наследует [SetText](../Text/SetText.md): подписывается на синглтон `ScoreManager.I` и обновляет текст при изменении счёта или рекорда.

**Добавить:** Add Component → Neoxider → Tools → Components → TextScore.

## Режимы

| Режим | Событие | Описание |
|-------|---------|----------|
| **Current** | OnValueChange | Текущий счёт (Score). |
| **Best** | OnBestValueChange | Лучший результат (BestScore). |

В инспекторе выберите режим отображения. Компонент ждёт появления `ScoreManager.I` (например после Bootstrap) и затем подписывается на соответствующее событие. При отключении подписка снимается.

## Требования

- В сцене должен быть **ScoreManager** (синглтон). См. [ScoreManager](./ScoreManager.md).

## См. также

- [ScoreManager](./ScoreManager.md) — менеджер очков и рекордов
- [SetText](../Text/SetText.md) — базовый компонент вывода текста

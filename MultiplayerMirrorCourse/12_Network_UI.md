# Урок 12: сетевой UI без сетевой каши

**Навигация:** [Оглавление](README.md) · [Старт](00_START_HERE.md) · [Оформление](LESSON_STYLE.md) · базовый трек · урок 12/15 · Mirror `96.x`

| Ключевые слова | HUD, View, model, local player, disconnect UI |
|----------------|-----------------------------------------------|

---

## Карта урока

| Что | Ответ |
|-----|-------|
| Объект работы | HUD, buttons, local player binding. |
| Кто владеет state | UI отображает; server/network model владеет gameplay state. |
| Как проверить | UI двух clients показывает свой local state и не управляет чужим player. |
| Артефакт | HUD binding через local player + отсутствие authority в UI layer. |

---

## Что должно получиться

Ваш UI показывает сетевое состояние, но не хранит авторитетную логику. Canvas не решает урон, покупки и победу.

---

## Проблема

Новички часто ставят `[SyncVar]` на UI Slider или делают кнопку, которая напрямую меняет HP. Это смешивает представление и сетевую модель.

---

## Разделение ролей

| Слой | Делает |
|------|--------|
| Network model | `SyncVar`, Sync-коллекции, Commands, server validation. |
| Presenter/Binder | Подписывается на local player и events. |
| View/UI | Показывает текст, шкалы, ошибки, кнопки. |

UI отправляет намерение в player/network model, но не меняет state напрямую.

---

## Практика: HUD подписывается на HP

```csharp
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHud : MonoBehaviour
{
    [SerializeField] Slider hpSlider;

    Health observed;

    public void Bind(Health health)
    {
        if (observed != null)
            observed.Changed -= OnHealthChanged;

        observed = health;
        observed.Changed += OnHealthChanged;
        OnHealthChanged(observed.Current);
    }

    void OnDestroy()
    {
        if (observed != null)
            observed.Changed -= OnHealthChanged;
    }

    void OnHealthChanged(int value)
    {
        hpSlider.value = value;
    }
}
```

`Health` остаётся сетевым `NetworkBehaviour`; UI - обычный локальный MonoBehaviour.

---

## Disconnect UI

Отдельно обработайте:

- timeout;
- kick;
- version mismatch;
- server full;
- connection refused.

Не оставляйте игрока на пустой сцене без объяснения.

---

## Проверка себя

- HUD показывает только локального игрока.
- UI не содержит `[SyncVar]`.
- Disconnect показывает понятную причину.
- После смены сцены UI не держит ссылку на destroyed object.

---

## Минимальная диагностика

| Симптом | Что проверить |
|---------|---------------|
| Кнопка работает только в Host | UI вызывает Command не с owned player. |
| HUD показывает чужое HP | Binding не привязан к `OnStartLocalPlayer`. |
| После смены сцены null references | UI не отписался или держит destroyed object. |
| Client меняет валюту через UI | UI должен отправить request, server меняет state. |

---

## Частые ошибки

- `GameObject.Find` каждый кадр.
- HUD показывает чужие HP без фильтра.
- UI вызывает `Command` со сценового объекта без authority.
- Нет отписок от events.
- Нет UI для failed connection.

---

## Лайфхаки

- В `OnStartLocalPlayer` регистрируйте player в UI-сервисе.
- Для кнопок используйте методы локального player: `localPlayer.Cmd...`.
- Ошибки подключения держите в отдельной модели, не в destroyed NetworkBehaviour.
- World-space UI подчиняется visibility; проверяйте это с Interest Management.

---

## Профессиональный минимум

- UI не содержит авторитетной gameplay-логики.
- Все кнопки идут через local player/controller.
- Подписки очищаются при despawn/scene change.
- UI test включает два clients с разными состояниями.

---

## Домашнее задание

Сделайте HUD:

1. HP приходит через `SyncVar` на player.
2. UI подписывается на local player.
3. Кнопка покупки вызывает command через player model.
4. Disconnect показывает причину и кнопку возврата в меню.

# RpgResourceBinding

**Что это:** лёгкий компонент привязки одного ресурса `RpgCharacter` к UnityEvents и NoCode UI.

Добавьте его на UI-объект, назначьте `RpgCharacter`, выберите ресурс через preset или `Custom`, затем привяжите события:

| Событие | Значение |
|---------|----------|
| `OnCurrent` | Текущее значение ресурса |
| `OnMax` | Максимум ресурса |
| `OnPercent` | Процент 0-1 |

## Примеры

- Stamina bar: `_resourceId = Stamina`, `OnPercent -> Slider.value`.
- Dark mana text: `_resourceId = Custom/DarkMana`, `OnCurrent -> SetText`.
- Shield UI: `_resourceId = Shield`, `OnPercent -> Image.fillAmount`.

Для условий без отдельного binding можно использовать `RpgConditionAdapter.ResourceAtLeast` / `ResourcePercentBelow`.

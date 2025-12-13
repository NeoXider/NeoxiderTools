# MIGRATION (из `_UI Core` / `_UI Kit`)

## Откуда переехало

- Было: `Assets/_UI Core/*` и `Assets/_UI Kit/*`\n- Стало: `Assets/NeoxiderPages/*`

## Что заменили (дедуп)

- `TextMoney` → `Neo.Shop.TextMoney`\n- `UISelector` → `Neo.Tools.Selector`\n- `UIToggleView` → `Neo.UI.VisualToggle`\n- `PlayAudio` (клик) → `Neo.Audio.PlayAudioBtn`\n- `WaitWhile/FormatWithSeparator/...` → `Neo.Extensions.*`

## Где менялись префабы

Изменения делались напрямую в YAML префабов (перепривязка `m_Script` по GUID) в `Assets/NeoxiderPages/Prefabs/**`.



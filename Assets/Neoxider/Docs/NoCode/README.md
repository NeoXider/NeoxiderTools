# Neo.NoCode — привязка float к UI без лишних скриптов

Сборка **`Neo.NoCode`** (`Assets/Neoxider/Scripts/NoCode/`) даёт компоненты, которые читают **число** с поля или свойства другого компонента (или из **`ReactivePropertyFloat`**) и обновляют **текст** или **индикатор прогресса** (Slider / Image).

Рефлексия выполняется только при резолве и после инвалидации кеша — см. принципы в [**`NO_CODE_AUDIT.md`**](../NO_CODE_AUDIT.md).

## Компоненты

| Компонент | Назначение |
|-----------|------------|
| **`NoCodeBindText`** | Вызывает **`SetText.Set(float)`** на том же объекте (или по ссылке), иначе пишет значение в **`TMP_Text`** (инвариантная строка). |
| **`SetProgress`** | Маппинг значения в \([0,1]\) через **`InverseLerp(min, max, value)`** → **`Slider.normalizedValue`** и/или **`Image.fillAmount`**. |

Меню создания: **Neoxider → NoCode → …**

## Настройка источника

Общий блок **`ComponentFloatBinding`** у **`NoCodeBindText`** и **`SetProgress`** (в инспекторе — секция **Binding**):

1. **Find By Name** — как в **NeoCondition**: искать корневой **`GameObject`** через **`GameObject.Find`** по строке **Object Name** вместо прямой ссылки.
2. **Object Name** — имя объекта в активной сцене (при включённом Find By Name).
3. **Wait For Object** — если объект ещё не появился (спавн, префаб), можно включить ожидание **без однократного предупреждения** в консоли (логика в **`BindingSourceGameObjectResolver`**; не блокирует кадр).
4. **Find Retry Interval (sec)** — как в **NeoCondition**: не чаще чем раз в N секунд повторять **`GameObject.Find`**, пока объект ещё не в сцене. **0** = повторять при каждой проверке (без троттлинга). По умолчанию **1** с.
5. **Prefab Preview** (только редактор; как у **`NeoCondition`**): если инстанса с нужным именем ещё нет в сцене — перетащите префаб из проекта, чтобы выбрать компонент и поле до появления объекта в сцене. В рантайме не используется.
6. **Source Root** — прямой объект для выпадающих списков компонента и члена; показывается **только когда Find By Name выключен**. Если поле пусто — используется **`GameObject`**, на котором висит сам NoCode-компонент.

Те же правила резолва объекта (поиск по имени / ссылка / fallback на хост), что и в условиях, вынесены в общий код **`BindingSourceGameObjectResolver`** (сборка `Neo.Condition`, используется и NoCode).

**Component** и **Member** в инспекторе выбираются из выпадающих списков (типы компонентов на источнике и допустимые поля/свойства: число или **`ReactivePropertyFloat`**). Ручные строки **типа** и **члена** по-прежнему видны в подсказке «Manual names (advanced)».

Произвольные методы вызова по строке в **v1** не поддерживаются (в отличие от расширенного режима NeoCondition).

## Режимы обновления

- **Once** — один раз при включении компонента.
- **Reactive** — если член имеет тип **`ReactivePropertyFloat`**, подписка на изменения через **`ReactiveProperty`** (уведомления доставляются и в Edit Mode при тестах/настройке); иначе значение читается при включении как при **Once**.
- **Poll** — при включённом опросе — обновление в **`LateUpdate`** (флаг **Poll In Late Update**).

## **`SetText` и привязка к данным**

**`SetText`** только форматирует и выводит текст. Чтобы подставлять число с **другого компонента** без своего скрипта, на тот же объект добавьте **`NoCode Bind Text`** (`Neo.NoCode`): он использует тот же **`ComponentFloatBinding`**, что и **`SetProgress`**. В инспекторе **`SetText`** есть подсказка и кнопка добавления **`NoCodeBindText`**, если компонента ещё нет.

## Зависимости сборки

`Neo.Condition` (**ReflectionCache**, **`BindingSourceGameObjectResolver`**), `Neo.Reactive`, `Neo.Extensions`, `Neo.Tools.Text`, `Neo.PropertyAttribute`, **UnityEngine.UI**, **TextMeshPro**.

## См. также

- [**`NO_CODE_AUDIT.md`**](../NO_CODE_AUDIT.md) — границы No-Code, roadmap.
- **`SetText`**: [`Tools/Text/SetText.md`](../Tools/Text/SetText.md)
- Условия и тот же стиль резолва объекта/полей: [`Condition/NeoCondition.md`](../Condition/NeoCondition.md)

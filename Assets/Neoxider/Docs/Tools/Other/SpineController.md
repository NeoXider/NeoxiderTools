# SpineController

Универсальный фасад над `SkeletonAnimation`, упрощающий работу с анимациями и скинами Spine как в редакторе, так и при запуске игры. Компонент берёт на себя инициализацию, автозаполнение списков клипов и предоставляет удобный API для UI, скриптов и UnityEvent.

---

## Основные возможности
- автоматическая инициализация `SkeletonAnimation` / `SkeletonDataAsset` и поддержка режима редактирования;
- автосбор списков анимаций и скинов из `SkeletonData` (при необходимости можно заполнить вручную);
- проигрывание клипов по имени/индексу, задание `mixDuration`, возврат к idle после нецикличных анимаций;
- смена скинов с сохранением выбора в `PlayerPrefs`, пролистывание `Next/Previous` с учётом смещения индексов;
- UnityEvent‑дружественные методы с одним аргументом — можно навешивать в инспекторе без вспомогательных скриптов;
- событие `OnSwapSkin`, которое срабатывает после успешной смены скина;
- доступ к спискам анимаций/скинов (`IReadOnlyList`) для внешних систем.

---

## Расположение файлов
- Скрипт: `Assets/Neoxider/Scripts/Tools/Other/SpineController.cs`
- Документ: `Assets/Neoxider/Docs/Tools/Other/SpineController.md`

---

## Быстрый старт
1. Установите Spine Unity Runtime и добавьте `SkeletonAnimation` на объект.
2. Повесьте компонент `SpineController` на тот же объект.
3. В инспекторе включите `Auto Populate Animations/Skins`, либо заполните списки вручную.
4. Укажите idle-анимацию (`Default Animation Name` или `Default Animation Index`) и при необходимости активируйте `Play Default On Enable`.
5. Настройте работу со скинами: `Default Skin Index`, `Persist Skin Selection`, `Skin Index Offset` (если первый элемент списка служебный).
6. Подпишите UI/UnityEvents на нужные методы (см. ниже).

---

## Публичные методы
### Анимации
- `TrackEntry Play(string name, bool loop = false, float mixDuration = 0f, bool queueDefault = true)`
- `TrackEntry Play(int index, bool loop = false, float mixDuration = 0f, bool queueDefault = true)`
- `void PlayDefault(bool forceRestart = false)` / `void Stop()`
- `void SetDefaultAnimation(string name, bool playImmediately = true)`
- `void SetDefaultAnimationByIndex(int index, bool playImmediately = true)`

### Скины
- `void SetSkin(string name, bool persist = true)`
- `void SetSkinByIndex(int index, bool persist = true)`
- `void NextSkin()` / `void PreviousSkin()`

### UnityEvent‑friendly обёртки (1 аргумент)
- `PlayAnimationByName(string name)` / `PlayAnimationLoopByName(string name)`
- `PlayAnimationByIndex(int index)` / `PlayAnimationLoopByIndex(int index)`
- `PlayDefault()` / `PlayDefaultForced()`
- `SetDefaultAnimation(string name)` / `SetDefaultAnimationByIndex(int index)`

Все методы принимают один параметр либо не принимают вовсе, что делает их удобными для вызова из Inspector или `AnimationEvent`.

---

## Пример использования
```csharp
public class SpineUIButton : MonoBehaviour
{
    [SerializeField] private SpineController controller;

    public void OnJumpPressed()
    {
        controller.Play("jump", loop: false, mixDuration: 0.2f);
    }

    public void OnEquipSkin(string skinName)
    {
        controller.SetSkin(skinName);
    }
}
```
В UI можно назначить:
- кнопке «Attack» — `PlayAnimationByName("attack")`;
- кнопке «Idle» — `PlayDefaultForced()`;
- кнопке «Следующий костюм» — `NextSkin()` без аргументов.

---

## Советы
- Если используете `Skin Index Offset` или унаследованный `Legacy Add Index`, убедитесь, что `Default Skin Index` не выходит за пределы логического диапазона.
- Для плавных переходов между клипами задавайте `mixDuration` сразу после вызова `Play`.
- При отключении `Persist Skin Selection` управление `PlayerPrefs` можно вынести в собственную систему прогресса.
- Метод `Stop()` очищает треки и отменяет подписку на события — полезно при паузе или смене сцены.

---

## Требования
- Unity 2021.3+ (проект ориентирован на LTS ветку).
- Spine Unity Runtime совместимой версии.

`SpineController` можно расширять: например, добавить работу с несколькими дорожками (`SetAnimation` на track > 0), поддержку `SkeletonGraphic` или обвязку под Timeline Track.

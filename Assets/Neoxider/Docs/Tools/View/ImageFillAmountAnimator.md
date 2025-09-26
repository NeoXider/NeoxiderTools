# Компонент Image Fill Amount Animator

## 1. Введение

`ImageFillAmountAnimator` — это простой, но эффективный компонент для создания плавной анимации заполнения UI-изображений. Он идеально подходит для визуализации прогресса, таких как полоски здоровья, индикаторы кулдауна, загрузочные полосы или шкалы опыта.

Вместо мгновенного изменения `fillAmount` у `Image`, этот компонент использует библиотеку DOTween для создания красивого, анимированного перехода, что делает интерфейс более живым и отзывчивым.

---

## 2. Описание класса

### ImageFillAmountAnimator
- **Пространство имен**: Глобальное
- **Путь к файлу**: `Assets/Neoxider/Scripts/Tools/View/ImageFillAmountAnimator.cs`

**Описание**
Компонент, который управляет свойством `fillAmount` у компонента `Image` (тип `Filled`). Он принимает целевое значение (от 0 до 1) и плавно анимирует `fillAmount` до этого значения за заданное время.

**Ключевые поля**
- `_image` (`Image`): **Обязательное поле.** Ссылка на компонент `Image`, `fillAmount` которого будет анимироваться. Убедитесь, что у этого `Image` установлен `Image Type` в `Filled`.
- `_duration` (`float`): Длительность анимации заполнения в секундах.

**Публичные методы (Public Methods)**
- `SetValue(float value)`: Основной метод. Устанавливает целевое значение `fillAmount` (от 0 до 1) и запускает анимацию. Если анимация уже идет, она будет прервана и начнется новая.

---

## 3. Зависимости

- **DOTween**: **Критически важная зависимость!** Этот компонент не будет работать без установленного в проекте ассета **DOTween (Demigiant Tween Engine)**, так как вся анимация выполняется с его помощью.
- **Unity UI**: Требует наличия компонента `UnityEngine.UI.Image` на том же `GameObject`.

---

## 4. Как использовать

1.  Создайте UI-элемент `Image` (GameObject -> UI -> Image).
2.  В инспекторе `Image` установите `Image Type` в `Filled` и выберите `Fill Method` (например, `Radial 360` или `Horizontal`).
3.  Добавьте компонент `ImageFillAmountAnimator` на тот же `GameObject`.
4.  Настройте `_duration` анимации.
5.  Из другого скрипта (например, скрипта здоровья игрока или таймера) вызывайте метод `SetValue()`:

    ```csharp
    // Пример: обновление полоски здоровья
    public ImageFillAmountAnimator healthBarAnimator;
    public float currentHealth = 75f;
    public float maxHealth = 100f;

    void UpdateHealthBar()
    {
        healthBarAnimator.SetValue(currentHealth / maxHealth);
    }
    ```

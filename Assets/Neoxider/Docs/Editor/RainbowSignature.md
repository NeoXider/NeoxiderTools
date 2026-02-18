# Rainbow Effects для Neo компонентов

## Описание

Все компоненты из пространства имён `Neo` (включая `Neo.Tools`, `Neo.Cards`, `Neo.UI` и другие) отображаются в инспекторе Unity с красивыми эффектами:

- **Анимированная радужная надпись "by Neoxider"** - текст плавно переливается всеми цветами радуги
- **Вертикальная радужная линия слева** - градиент от красного до фиолетового
- **Радужная обводка текста** - опциональный эффект свечения
- **Анимация** - можно включить/выключить отдельно для текста и линии

---

## Настройки через меню

**Tools → Neoxider → Visual Settings**

Откроется окно с настройками:

### Текст (Signature)
- ☑ **Включить Rainbow Signature** - показывать цветной текст "by Neoxider"
- ☑ **Анимация текста** - переливание цветов

### Линия (Rainbow Line)
- ☑ **Включить Rainbow Outline** - обводка текста
- ☑ **Включить Rainbow Line (слева)** - вертикальная линия
- ☑ **Анимация линии** - движение градиента

### Скорость анимации
- **Rainbow Speed** (0.0 - 1.0) - скорость анимации

### Сброс настроек
- **[Сбросить все настройки]** - вернуть значения по умолчанию

**Примечание:** Все настройки сохраняются в `EditorPrefs` и сохраняются между сессиями Unity.

---

## Настройки в коде

Все настройки используют `EditorPrefs` и сохраняются между сессиями:

### Через CustomEditorSettings

```csharp
// Текст
CustomEditorSettings.EnableRainbowSignature          // Вкл/выкл цветной текст
CustomEditorSettings.EnableRainbowSignatureAnimation // Вкл/выкл анимацию текста

// Линия
CustomEditorSettings.EnableRainbowOutline            // Вкл/выкл обводку текста
CustomEditorSettings.EnableRainbowComponentOutline   // Вкл/выкл линию слева
CustomEditorSettings.EnableRainbowLineAnimation      // Вкл/выкл анимацию линии

// Скорость
CustomEditorSettings.RainbowSpeed                    // 0.0 - 1.0

// Setters
CustomEditorSettings.SetEnableRainbowSignature(bool value);
CustomEditorSettings.SetEnableRainbowSignatureAnimation(bool value);
CustomEditorSettings.SetEnableRainbowLineAnimation(bool value);
CustomEditorSettings.SetRainbowSpeed(float value);
```

### Значения по умолчанию

| Параметр | Значение |
|----------|----------|
| EnableRainbowSignature | `true` |
| EnableRainbowSignatureAnimation | `true` |
| EnableRainbowOutline | `true` |
| EnableRainbowComponentOutline | `true` |
| EnableRainbowLineAnimation | `true` |
| RainbowSpeed | `0.1` |
| RainbowSaturation | `0.8` |
| RainbowBrightness | `1.0` |

## Как использовать

1. **Создайте компонент в пространстве имён Neo:**
   ```csharp
   namespace Neo.Tools
   {
       public class MyComponent : MonoBehaviour
       {
           // Ваш код
       }
   }
   ```

2. **Добавьте компонент на GameObject в сцене**

3. **Откройте инспектор** - вы увидите анимированную радужную надпись "by Neoxider" вверху компонента

## Настройка эффекта

### Отключить анимацию

Если вы хотите отключить радужную анимацию, измените в `CustomEditorSettings.cs`:

```csharp
public static bool EnableRainbowSignature => false;
```

### Отключить только обводку

Если вы хотите оставить только цветную надпись без обводки:

```csharp
public static bool EnableRainbowOutline => false;
```

### Изменить скорость анимации

Для более медленной анимации:

```csharp
public static float RainbowSpeed => 0.1f; // Медленная радуга
```

Для более быстрой анимации:

```csharp
public static float RainbowSpeed => 1.0f; // Быстрая радуга
```

### Сделать цвета более насыщенными

```csharp
public static float RainbowSaturation => 1.0f; // Максимальная насыщенность
public static float RainbowBrightness => 1.0f; // Максимальная яркость
```

### Увеличить размер обводки

```csharp
public static float RainbowOutlineSize => 3.0f; // Более толстая обводка
public static float RainbowOutlineAlpha => 0.8f; // Более видимая обводка
```

## Примеры использования

### Пример 1: Тестовый компонент

Создан тестовый компонент `RainbowTestComponent.cs` для демонстрации эффекта:

```csharp
namespace Neo.Tools.View
{
    [AddComponentMenu("Neoxider/Tools/Rainbow Test")]
    public class RainbowTestComponent : MonoBehaviour
    {
        public string testMessage = "Посмотрите на надпись 'by Neoxider' сверху!";
    }
}
```

### Пример 2: Существующие Neo компоненты

Все существующие компоненты автоматически получат радужный эффект:
- `HandComponent`
- `DeckComponent`
- `CardComponent`
- `StarView`
- `VisualToggle`
- И все остальные компоненты в пространстве имён `Neo.*`

## Технические детали

### Как это работает

1. **CustomEditorBase** - базовый класс для всех кастомных редакторов Neo компонентов
2. **Проверка namespace** - редактор проверяет, принадлежит ли компонент пространству имён `Neo` или начинается с `Neo.`
3. **Анимация** - используется `EditorApplication.timeSinceStartup` для создания плавной анимации
4. **HSV цветовая модель** - для создания радужного эффекта используется HSV (Hue, Saturation, Value)
5. **Автоматический Repaint** - редактор автоматически обновляется для анимации

### Производительность

- Анимация оптимизирована и не влияет на производительность редактора
- Используется встроенная система обновления Unity `EditorApplication.update`
- Repaint вызывается только для активных компонентов в инспекторе

## Совместимость

- ✅ Unity 2020.3 и выше
- ✅ Работает с Odin Inspector
- ✅ Работает со всеми компонентами пространства имён `Neo`
- ✅ Не влияет на runtime производительность (только в редакторе)

## Путь к файлам

- **Настройки**: `Assets/Neoxider/Editor/PropertyAttribute/CustomEditorSettings.cs`
- **Реализация**: `Assets/Neoxider/Editor/PropertyAttribute/CustomEditorBase.cs`
- **Тестовый компонент**: `Assets/Neoxider/Scripts/Tools/View/RainbowTestComponent.cs`
- **Документация**: `Assets/Neoxider/Docs/Editor/RainbowSignature.md`


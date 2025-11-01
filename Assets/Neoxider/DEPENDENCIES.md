# Зависимости Neoxider Tools

Этот документ описывает зависимости, необходимые для работы пакета Neoxider Tools.

---

## Обязательные зависимости

### Unity Editor
- **Минимальная версия:** Unity 2022.1 или выше
- **Рекомендуемая версия:** Unity 2022.1 или выше

### Unity Packages

#### TextMeshPro
- **Пакет:** `com.unity.textmeshpro`
- **Версия:** 3.0.6 или выше
- **Статус:** ✅ Автоматически добавляется как зависимость в `package.json`
- **Использование:** Компоненты UI, текст, форматирование чисел (`SetText`)

---

## Основные зависимости (для Runtime модулей)

Эти зависимости требуются для работы модулей `Runtime/` (логирование, DI, реактивное программирование):

### R3
- **Пакет:** NuGet (не Unity Package Manager)
- **Установка:** Через NuGetForUnity
- **Статус:** ⚠️ Требуется ручная установка через NuGetForUnity
- **Использование:** Реактивное программирование в Runtime модулях
- **Версия:** 1.3.0+
- **Документация:** https://github.com/Cysharp/R3
- **NuGet:** https://www.nuget.org/packages/R3
- **GitHub:** https://github.com/Cysharp/R3

**Установка R3:**
1. Установите NuGetForUnity через Package Manager: `https://github.com/GlitchEnzo/NuGetForUnity.git`
2. Откройте `NuGet > Manage NuGet Packages`
3. Найдите и установите: `R3` (версия 1.3.0 или выше)

### VContainer
- **Пакет:** `jp.hadashikick.vcontainer`
- **Установка:** Git URL в `Packages/manifest.json`: `https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer`
- **Статус:** ⚠️ Требуется ручная установка в `manifest.json` проекта
- **Использование:** Dependency Injection контейнер для Runtime модулей
- **Документация:** https://vcontainer.hadashikick.jp/
- **GitHub:** https://github.com/hadashiA/VContainer
- **Важно:** Unity Package Manager не может автоматически установить зависимости через Git URL из `package.json` пакета

### MessagePipe
- **Пакет:** `com.cysharp.messagepipe`
- **Установка:** Git URL в `Packages/manifest.json`: `https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe`
- **Статус:** ⚠️ Требуется ручная установка в `manifest.json` проекта
- **Использование:** Система сообщений Pub/Sub
- **Документация:** https://github.com/Cysharp/MessagePipe
- **GitHub:** https://github.com/Cysharp/MessagePipe
- **Важно:** Unity Package Manager не может автоматически установить зависимости через Git URL из `package.json` пакета

### MessagePipe.VContainer
- **Пакет:** `com.cysharp.messagepipe.vcontainer`
- **Установка:** Git URL в `Packages/manifest.json`: `https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe.VContainer`
- **Статус:** ⚠️ Требуется ручная установка в `manifest.json` проекта
- **Использование:** Адаптер MessagePipe для интеграции с VContainer
- **GitHub:** https://github.com/Cysharp/MessagePipe (в том же репозитории)
- **Важно:** Unity Package Manager не может автоматически установить зависимости через Git URL из `package.json` пакета

### Serilog
- **Пакет:** NuGet (не Unity Package Manager)
- **Установка:** Через NuGetForUnity
- **Статус:** ⚠️ Требуется ручная установка через NuGetForUnity
- **Использование:** Структурированное логирование в `Runtime/Logging`
- **Версия:** 4.3.0+
- **Документация:** https://serilog.net/
- **NuGet:** https://www.nuget.org/packages/Serilog

**Установка Serilog:**
1. Установите NuGetForUnity через Package Manager: `https://github.com/GlitchEnzo/NuGetForUnity.git`
2. Откройте `NuGet > Manage NuGet Packages`
3. Найдите и установите: `Serilog`, `Serilog.Extensions.Logging`, `Serilog.Sinks.File`

### Serilog.Sinks.File
- **Пакет:** NuGet (не Unity Package Manager)
- **Установка:** Через NuGetForUnity (устанавливается вместе с Serilog)
- **Статус:** ⚠️ Требуется ручная установка через NuGetForUnity
- **Использование:** Файловый вывод логов в `Runtime/Logging`
- **Версия:** 7.0.0+
- **NuGet:** https://www.nuget.org/packages/Serilog.Sinks.File

### Serilog.Extensions.Logging
- **Пакет:** NuGet (не Unity Package Manager)
- **Установка:** Через NuGetForUnity (устанавливается вместе с Serilog)
- **Статус:** ⚠️ Требуется ручная установка через NuGetForUnity
- **Использование:** Интеграция Serilog с Microsoft.Extensions.Logging
- **Версия:** 9.0.2+
- **NuGet:** https://www.nuget.org/packages/Serilog.Extensions.Logging

---

## Опциональные зависимости

Эти зависимости не являются обязательными, но добавляют дополнительный функционал к определенным модулям.

### DOTween
- **Пакет:** `com.demigiant.dotween` или `DG.Tweening` (Asset Store)
- **Статус:** ⚠️ Опционально (рекомендуется)
- **Использование:**
  - Анимации UI (`ImageFillAmountAnimator`, `AnimationFly`)
  - Плавные переходы чисел в `SetText`
  - Различные анимации и эффекты

**Установка:**
- Через Asset Store: [DOTween Pro](http://dotween.demigiant.com/getstarted.php)
- Через OpenUPM (free версия): `com.demigiant.dotween` (если доступно)

**Проверка наличия:**
Пакет проверяет наличие DOTween через define `DOTWEEN` в `Neo.asmdef`. Если DOTween отсутствует, соответствующий функционал будет отключен.

### Spine Unity Runtime
- **Пакет:** Spine Unity Runtime (Asset Store)
- **Статус:** ⚠️ Опционально (только для SpineController)
- **Использование:**
  - Компонент `SpineController` для управления Spine анимациями
  - Автоматическое отключение, если Spine Runtime отсутствует

**Установка:**
- Asset Store: [Spine Unity Runtime](https://assetstore.unity.com/packages/tools/animation/spine-unity-2d-skeletal-animation-56455)
- Или через официальный сайт: [esotericsoftware.com](http://esotericsoftware.com/)

**Проверка наличия:**
Код использует условную компиляцию `#if SPINE_UNITY`. Если Spine отсутствует, `SpineController` будет недоступен.

---

## Установка через Package Manager

### Добавление опциональных зависимостей вручную

#### DOTween (через Git, если доступно)
```json
{
  "dependencies": {
    "com.demigiant.dotween": "https://github.com/Demigiant/dotween.git"
  }
}
```

#### Spine (только через Asset Store или вручную)
Spine Unity Runtime нельзя установить через Package Manager напрямую. Используйте Asset Store Package Manager или импортируйте вручную.

---

## Проверка зависимостей в коде

### Spine
```csharp
#if SPINE_UNITY
    // Код с Spine
    using Spine.Unity;
#endif
```

---

## Совместимость версий

| Зависимость | Минимальная версия | Рекомендуемая версия | Статус | Способ установки |
|------------|-------------------|---------------------|--------|------------------|
| Unity | 2022.1 | 2022.1+ | ✅ Обязательно | - |
| TextMeshPro | 3.0.6 | 3.0.6+ | ✅ Обязательно | Package Manager (авто) |
| R3 | 1.3.0 | Latest | ✅ Обязательно (Runtime) | NuGetForUnity |
| VContainer | Latest | Latest | ✅ Обязательно (Runtime) | Git URL (ручная) |
| MessagePipe | Latest | Latest | ✅ Обязательно (Runtime) | Git URL (ручная) |
| MessagePipe.VContainer | Latest | Latest | ✅ Обязательно (Runtime) | Git URL (ручная) |
| Serilog | 4.3.0 | Latest | ⚠️ Обязательно (Runtime) | NuGetForUnity |
| Serilog.Sinks.File | 7.0.0 | Latest | ⚠️ Обязательно (Runtime) | NuGetForUnity |
| Serilog.Extensions.Logging | 9.0.2 | Latest | ⚠️ Обязательно (Runtime) | NuGetForUnity |
| DOTween | 1.2.632 | Latest | ⚠️ Обязательно (Runtime) | Asset Store / Сайт |
| Spine Runtime | 4.0 | Latest | ⚠️ Опционально | Asset Store |

---

## FAQ

**Q: Можно ли использовать пакет без DOTween?**  
A: нет, пакет работает с DOTween, и некоторые анимации и эффекты будут недоступны.

**Q: Можно ли использовать пакет без Spine?**  
A: Да, все функции работают, кроме компонента `SpineController`. Код автоматически проверяет наличие Spine и отключает соответствующий функционал.

**Q: Как узнать, какие зависимости установлены?**  
A: Проверьте наличие define символов в `Player Settings > Other Settings > Scripting Define Symbols`:  `SPINE_UNITY` для Spine.

**Q: Где найти примеры использования?**  
A: Примеры находятся в папке `Demo/` проекта.

---

## Дополнительная информация

Для получения подробной информации о модулях, использующих эти зависимости, см.:
- **DOTween:** `Docs/Tools/View/` (анимации UI)
- **Spine:** `Docs/Tools/Other/SpineController.md`

---

**Обновлено:** 2025


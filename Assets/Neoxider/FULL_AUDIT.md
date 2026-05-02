# Neoxider Tools — полный аудит кодовой базы

| Поле | Значение |
|------|----------|
| Дата отчёта | 2026-05-03 |
| Версия пакета (`package.json`) | 7.14.0 |
| Целевая платформа | Unity 2022.1+ (см. `Assets/Neoxider/package.json`) |

## Методология и ограничения

Аудит выполнен **статическим анализом** репозитория (структура, grep-паттерны, выборочное чтение ключевых типов). **Не выполнялось:** запуск Unity Editor, Play Mode / Edit Mode тестов, профилирование CPU/GPU/памяти, ручное прохождение демо-сцен, проверка сборок под консоли/мобильные SDK.

Итог — ориентир для мейнтейнеров и roadmap; для релизных гарантий нужны прогон **Test Runner** и таргет-профилирование под ваши сценарии.

---

## 1. Масштаб и архитектура

| Метрика | Оценка |
|---------|--------|
| Файлов `*.cs` под `Assets/Neoxider` | **~541** |
| Файлов `*.asmdef` под `Assets/Neoxider` | **42** |
| Тестовые сборки (`Assets/Tests`) | **44+** файлов тестов (Edit + Play), включая Save encryption, Dialogue, UI (**VisualToggle**), Shop (Play) |

- Модульная нарезка **Assembly Definition** ускоряет компиляцию и изолирует домены (Save, Cards, NPC, Tools.* и т.д.).
- Риск: рост числа сборок повышает цену ошибок в ссылках между asmdef; при рефакторинге нужен контроль циклических зависимостей.
- Синглтоны **`Singleton<T>`** / **`SingletonById<T>`** с **`SingletonRuntimeReset`** и подсистемными хуками (**`SaveManagerSubsystemRegistration`**, **`MouseInputManagerSubsystemRegistration`**) — осознанный отход от `[RuntimeInitializeOnLoadMethod]` на generic-базах (ограничение Unity).

---

## 2. Сильные стороны

1. **Широта модулей** — Condition, Save, Quest, Shop, StateMachine, Cards, GridSystem, NPC, RPG, Progression, Bonus и др.; единый стиль меню создания и атрибутов документации (`NeoDoc`).
2. **Тесты на критические участки** — Save, Level, Bootstrap, NeoCondition, инвентарь, часть RPG/NPC/Grid (см. `Assets/Tests/Edit`, `Assets/Tests/Play`).
3. **Документация** — канонический индекс `Docs/README.md`, англоязычный вход `DocsEn/README.md`, отдельный **Coverage** для EN: [`DocsEn/COVERAGE_AUDIT.md`](DocsEn/COVERAGE_AUDIT.md).
4. **Явная депрекация** — часть legacy API помечена `[Obsolete]` с указанием преемников (RPG, NpcNavigation, UIReady → SceneFlowController и т.д.).
5. **NeoxiderPages / PM** — после доработки `FindAllScenePages` использует **`GetComponentsInChildren<UIPage>(true)`** вместо глобального `Resources.FindObjectsOfTypeAll` (поиск ограничен поддеревом объекта с PM).

---

## 3. Зоны риска по категориям

### 3.1 Legacy и технический долг

Найдены типы/члены с **`[Obsolete]`** (неполный список):

| Область | Файлы / заметки |
|---------|-----------------|
| Старый бой | `AttackExecution`, `Health`, `AdvancedAttackCollider`, `Evade` → RPG-слой |
| Навигация | `AiNavigation` → `Neo.NPC.NpcNavigation` |
| UI | `UIReady` → `SceneFlowController` |
| Карты | `HandLayoutType`, часть `HandComponent` |
| Bonus | `WheelFortune`, `TimeReward` (части API) |

**Риск:** демо и пользователи продолжают тянуть старые компоненты; стоит усилить указатели в Create-меню и верхних README.

### 3.2 Производительность и GC

| Тема | Где смотреть | Комментарий |
|------|----------------|-------------|
| Первый доступ **`Singleton<T>.I`** | `Scripts/Tools/Managers/Singleton.cs` | `FindObjectsByType` при первом разрешении — избегать раннего частого доступа до `Awake`. |
| **`Resources.FindObjectsOfTypeAll<UIPage>`** | `Samples~/NeoxiderPages/Runtime/Scripts/Core/PageSubscriber.cs` | Тяжёлый глобальный обход; при большом числе страниц/ассетов — рассмотреть привязку к PM или кэш по сцене (аналогично подходу PM). |
| **LINQ** | `Save/SaveManager.cs`, модули Cards | Уместно вне горячих кадров; при спайках GC — профилировать и заменить циклами на критических путях. |
| **Рефлексия / скан сборок** | `SingletonRuntimeReset`, `InteractiveObjectSceneSetup` | Нужны для сброса и типов; при боли в производительности редактора — сузить набор сборок или кэшировать. |

### 3.3 Надёжность и обработка ошибок

| Находка | Файл |
|---------|------|
| Пустые `catch (Exception) { }` | `Scripts/Tools/View/Selector.cs` (заглушки при переборе) — ошибки глотаются без логирования |

Рекомендация: хотя бы условный лог в `DEVELOPMENT_BUILD` или однократное предупреждение.

### 3.4 Уничтожение объектов (`Destroy` / `DestroyImmediate`)

- **`ObjectExtensions.SafeDestroy`** корректно разделяет Play vs Edit.
- **`DestroyImmediate`** в рантайм-коде встречается в части скриптов (например `MeshEmission`, `ParallaxLayer`, `TransformExtensions`) — для **игрового** Play Mode предпочтительно `Destroy`, если только это не защищено веткой `!Application.isPlaying`. Уже исправляли подобное в changelog — имеет смысл периодически ревьюить новые вызовы.

### 3.5 Сохранения и данные

- **SaveManager** — рефлексия полей, **JsonUtility**, провайдеры PlayerPrefs/файл. Ограничения Unity JSON (полиморфизм, словари, корневые массивы) описаны в модульной документации.
- **FileSaveProvider** — опционально **AES-CBC + Base64** для содержимого файла (`SaveFileEncryption`, настройки в **SaveProviderSettings**). Это **не** замена платформенным секрет-хранилищам и не защита от модифицированного клиента при ключе в билде — см. [`Docs/Save/SaveFileEncryption.md`](Docs/Save/SaveFileEncryption.md).
- Файловые сейвы на диске остаются чувствительными к целостности и переносу между платформами; для высоких требований к античиту нужны сервер или специализированные решения.

### 3.6 Многопоточность

Явного массового использования **`Task.Run` / потоков** в **`Scripts`** по выборочному поиску не выявлено; игровая логика ориентирована на главный поток Unity — типично для данного стека.

### 3.7 Тестирование

- Покрытие всё ещё **неполное** относительно всего каталога модулей; добавлены целевые тесты: **шифрование файловых сейвов**, **DialogueController** (индексы после `StartDialogue`), **VisualToggle**, **Shop** (Play Mode, бесплатная покупка).
- **Приоритет расширения:** оставшиеся краевые сценарии Shop/Quest/StateMachine по метрикам багов; сетевые сценарии при появлении отдельного транспорта.

### 3.8 Документация (i18n)

Согласно [`DocsEn/COVERAGE_AUDIT.md`](DocsEn/COVERAGE_AUDIT.md): полное EN-покрытие для части модулей; глубокие страницы многих разделов остаются **RU-only** с перекрёстными ссылками — это осознанный компромисс, но барьер для EN-only команд.

### 3.9 Зависимости

- **UPM-пакет** (`package.json`): TMP, AI Navigation, URP — минимальный набор для публикации.
- **Шаблон проекта** (`Packages/manifest.json`): Input System, Visual Scripting, VFX Graph и др. — для потребителей библиотеки важно документировать, что **обязательно** для ядра Neoxider, а что только для демо/шаблона.

### 3.10 Видимость internal API для тестов

- Файлы **`Neo.Save.InternalsVisibleTo.cs`**, **`Neo.Tools.Input.InternalsVisibleTo.cs`** открывают internal методы сборке **`Neo.Editor.Tests`** — нормальная практика; следить, чтобы список сборок не разрастался без контроля.

---

## 4. Сводная таблица «горячих» точек (Editor vs Runtime)

| Механизм | Runtime | Editor-only |
|----------|---------|-------------|
| `Resources.FindObjectsOfTypeAll` в PageSubscriber | да | — |
| `FindObjectsOfType*` в ResourceDrawer, AutoTMP/Sprite assigner | — | да |
| `FindObjectOfType` в ProgressionDemoUI (sample) | да (демо) | — |

---

## 5. Рекомендации (приоритет)

1. **Высокий:** Снизить использование **`FindObjectsOfTypeAll`** в **`PageSubscriber`** или заменить резолвом через **`PM`** / явными ссылками на `PageId`.
2. **Высокий:** Периодически прогонять **Unity Test Runner** в CI и фиксировать регрессии по Save/Singleton/subsystem reset.
3. **Средний:** Логирование вместо пустых catch в **`Selector`**; централизованный флаг «тихого» режима для **`Debug.Log`** в Save/PM при продакшене.
4. **Средний:** Расширять тесты для модулей с частыми багрепортами.
5. **Низкий:** Продолжить миграцию Obsolete API в доках и префабах демо.

---

## 6. Вывод

Проект **Neoxider Tools** — крупный зрелый пакет с модульной архитектурой, документацией и базой автотестов. Основные области внимания на ближайшие итерации: **снижение тяжёлых поисков объектов в runtime-сэмплах**, **расширение тестового покрытия**, **контроль технического долга Obsolete** и **прозрачность ограничений Save/JSON**. Полная «гарантия качества» для конечной игры требует дополнительно **профилирования** и **игровых регрессионных сценариев** в целевых билдах.

---

*Документ можно обновлять при крупных релизах: скорректировать версию, дату и разделы 3–5 по результатам новых измерений.*

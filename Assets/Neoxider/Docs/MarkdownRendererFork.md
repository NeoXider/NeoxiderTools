# Форк MarkdownRenderer (NeoXider)

Инструкции по доработке форка [NeoXider/MarkdownRenderer](https://github.com/NeoXider/MarkdownRenderer): исправление ошибок, переименование пакета и план улучшений перед интеграцией в NeoxiderTools.

**Поддерживаемая версия Unity:** 2022.3 и выше.

---

## 1. Ошибка CS0246: UxmlElement / UxmlElementAttribute

**Причина:** `[UxmlElement]` и `partial class` — API Unity 6. В Unity 2022.3+ используется паттерн **UxmlFactory** + **UxmlTraits**.

**Файлы в форке** (правка в обоих, если есть): `Editor/VideoElement/VideoPlayerElement.cs` и `MarkdownRenderer/Editor/VideoElement/VideoPlayerElement.cs`

**Было:**
```csharp
[UxmlElement]
public partial class VideoPlayerElement : VisualElement
{
```

**Нужно заменить на:**
```csharp
public class VideoPlayerElement : VisualElement
{
    public new class UxmlFactory : UxmlFactory<VideoPlayerElement, UxmlTraits> { }
    public new class UxmlTraits : VisualElement.UxmlTraits { }
```

Класс больше не должен быть `partial`. После правки проект собирается в Unity 2022.3 и выше.

---

## 2. Переименование пакета в com.neoxider.markdownrenderer

Чтобы в проекте пакет отображался как свой (Neoxider), в форке нужно сменить имя пакета.

### 2.1 package.json

В корне репозитория форка в `package.json` заменить:

- **Было:** `"name": "com.rtl.markdownrenderer"`
- **Стало:** `"name": "com.neoxider.markdownrenderer"`

При желании можно обновить `displayName`, например: `"Markdown Renderer (Neoxider)"`.

### 2.2 Папка пакета (если используется)

При установке по Git URL Unity кладёт пакет в `Library/PackageCache` с именем из `package.json` и хешем. После смены `name` на `com.neoxider.markdownrenderer` при следующем обновлении пакета папка в кеше будет вида `com.neoxider.markdownrenderer@<hash>`. Отдельно переименовывать папки в репозитории не обязательно: структура может остаться (например, подпапка `MarkdownRenderer/` и т.д.).

### 2.3 Установка в проекте (NeoxiderTools и другие)

**Рекомендуемый способ:** Package Manager → **+** → Add package from git URL → вставить:

```
https://github.com/NeoXider/MarkdownRenderer.git
```

Если в форке переименован пакет в `com.neoxider.markdownrenderer`, в `Packages/manifest.json` зависимость будет вида:

- `"com.neoxider.markdownrenderer": "https://github.com/NeoXider/MarkdownRenderer.git"`

(ранее могло быть `"com.rtl.markdownrenderer": "https://github.com/NeoXider/MarkdownRenderer.git"`).

И в `packages-lock.json` Unity при следующем разрешении пакетов подставит новое имя сам (или можно удалить lock и переоткрыть проект).

### 2.4 Asmdef (опционально)

Имена сборок в пакете сейчас `Rtl.MarkdownRenderer.Editor` и т.д. Для единообразия можно переименовать в `Neoxider.MarkdownRenderer.Editor` и обновить имена файлов asmdef, но это не обязательно для работы: ссылки из NeoxiderTools идут по имени пакета, а не по имени сборки.

---

## 3. Улучшения форка перед интеграцией

Перед тем как встраивать документацию в инспектор NeoxiderTools, в форке имеет смысл добавить:

### 3.1 Удобный поиск .md (Assets и Packages)

- **Поиск по имени/расширению** в окне просмотра Markdown (или в отдельном окне): искать файлы `.md` и в `Assets/`, и в `Packages/` (в т.ч. `Packages/com.neoxider.tools/...` при установке Neoxider как пакета).
- Использовать `AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets", "Packages" })` и фильтр по расширению, либо поиск по GUID в нужных папках, чтобы путь к документу не зависел от способа установки (Git в Assets или UPM в Packages).

### 3.2 Кнопка и навигация

- Явная кнопка «Открыть в окне» (или аналог) в кастомном инспекторе .md или в списке документов, чтобы открывать выбранный файл в окне Markdown Doc View.
- Возможность открывать .md по project-relative пути (`Assets/...` или `Packages/...`) из кода (как в плане интеграции с NeoxiderTools: «Открыть в окне» из инспектора компонента).

### 3.3 Красивое отображение

- Стили/USS для читаемости (заголовки, код, ссылки) в окне просмотра.
- Поддержка относительных путей к картинкам и ссылкам относительно текущего .md (как в [документации MarkdownRenderer](https://github.com/UnityGuillaume/MarkdownRenderer) — relative path от расположения файла), чтобы доки Neoxider с картинками и перекрёстными ссылками выглядели корректно и в Assets, и в Packages.

После этих доработок форк можно подключать в NeoxiderTools и использовать блок «Документация» в инспекторе с кнопкой «Открыть в окне» и корректными путями.

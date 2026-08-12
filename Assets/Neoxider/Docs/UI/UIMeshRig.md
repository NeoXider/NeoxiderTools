# UI Mesh Rig

UI Mesh Rig деформирует один Sprite в четырёх средах из одного renderer-agnostic ядра:

- `UIMeshRigGraphic` — стандартный `MaskableGraphic` внутри uGUI Canvas;
- `UIMeshRigElement` — custom `VisualElement` для UI Toolkit/UXML/UI Builder;
- `UIMeshRigWorldRenderer` — `MeshFilter` + `MeshRenderer` для сцены без Canvas;
- `UIMeshRigSpriteRenderer` — обычный `SpriteRenderer` (sorting layers, 2D-свет, sprite masks, SRP batching).

Ядро `UIMeshRigGeometryBuilder` одинаково строит вершины, индексы и UV, рассчитывает эллиптическое
влияние, falloff, позу и процедурное движение. Адаптеры только переводят готовую геометрию в API своей
среды. Поэтому `Columns`, `Rows`, `Preserve Aspect`, `Deformation Enabled`, Sprite/Color и motion presets
имеют одинаковый смысл во всех вариантах.

## Быстрый старт

- uGUI: `GameObject > UI > Neoxider UI Mesh Rig`.
- UI Toolkit host: `GameObject > UI Toolkit > Neoxider UI Mesh Rig`.
- UI Toolkit UXML: `Assets > Create > Neoxider > UI Mesh Rig (UI Toolkit UXML)`, либо добавьте
  `UIMeshRigElement` в UI Builder из `Library > Custom Controls > Neoxider > UI Mesh Rig`.
- World mesh: `GameObject > 2D Object > Neoxider UI Mesh Rig (World)`.
- SpriteRenderer: `GameObject > 2D Object > Neoxider UI Mesh Rig (Sprite Renderer)`.

Каждый пункт создаёт видимый NeoLogo, разумный grid и уже движущийся preset. Все инспекторы используют
общую шапку Module. Точки редактируются в Scene View: Setup меняет bind pose, Pose / Animate — текущую
деформацию. `Capture Rest Pose` принимает текущую позу за нейтральную, `Reset Pose` возвращает bind pose.

## Инспекторы и Scene-гизмо

Поля компонентов объявлены обычными `[Header]` / `[Tooltip]`, а рисует их `CustomEditorBase` — тот же
механизм, что даёт сворачиваемые секции со счётчиками, ON/OFF-переключатели и цветные полосы у остальных
компонентов пакета. Кастомные редакторы добавляют только то, что атрибутами не выражается: кнопку
`Apply Layout & Preview`, диагностику сетки, список точек и Scene-хендлы. Наследуемые поля uGUI
(`Raycast Target`, `Raycast Padding`, `Maskable`, `Material`, `Color`) видны в общем проходе и больше не
спрятаны в свёрнутом фолдауте `Advanced Rig Controls`; поле `Script` не скрывается и не переносится —
это единственный способ починить компонент со слетевшей ссылкой на скрипт.

Авторское значение `Raycast Padding` теперь хранится скрытым полем: видимое поле рига пересчитывается
каждый кадр под деформированную сетку, поэтому «два Raycast Padding подряд, один из которых бессмыслен»
больше нет. Ручная правка видимого поля подхватывается как новое авторское значение.

В Scene View есть накладная панель рига: переключатель `Setup` / `Pose / Animate`, выбор инструмента
(Move / Rotate / Scale) и два тумблера читаемости — `Labels` и `All rings`. По умолчанию невыбранные точки
рисуют одно бледное внешнее кольцо без подписи, поэтому семь точек больше не превращаются в месиво из
четырнадцати эллипсов и семи подписей внахлёст. Хендлы bind-позы (якорь и радиусы) доступны в обоих
режимах, а не только в Setup; радиусы тянутся с четырёх сторон (±X и ±Y), якорь заметно крупнее и имеет
тёмное контрастное кольцо.

> `Handles.Label` не подчиняется `Handles.color`: подписи рисуются через GUI-стиль, поэтому их
> прозрачность задаётся собственным `GUIStyle.normal.textColor`, а не альфой `Handles.color`.

## uGUI

`UIMeshRigGraphic` сохраняет прежний workflow и публичный API. Дочерние `UIMeshRigPoint` —
`RectTransform`, поэтому Position/Rotation/Scale можно записывать обычным Animator или Timeline.
Контекстное меню Simple `Image` поддерживает in-place и non-destructive conversion. Raycast modes:

- `Rect` — исходный RectTransform;
- `Deformed Mesh` — фактическая деформированная сетка;
- `Sprite Alpha` — прозрачность Sprite (требует Read/Write Enabled, иначе безопасно использует mesh).

## UI Toolkit

`UIMeshRigElement` объявлен как Unity 6 custom control через `[UxmlElement]` на `partial`-классе и
`[UxmlAttribute]` на свойствах. Он рисует в `generateVisualContent`, выделяет данные через
`MeshGenerationContext.Allocate(...)` и заполняет `Vertex.position`, `Vertex.tint` и `Vertex.uv`.
Adapter учитывает `MeshWriteData.uvRegion`, поэтому текстура корректна, когда UI Toolkit помещает её в
atlas. В Unity 6.3 remap выполняется автоматически, но чтение региона сохранено для совместимости ветки
Unity 6.x.

Для UXML/UI Builder используйте элемент напрямую. `UIMeshRigUIToolkitHost` — необязательная сценовая
обёртка: она создаёт элемент и задаёт Sprite, Size, Position, grid, preset и motion.

**Хост работает через `PanelRenderer`, а не через `UIDocument`.** Начиная с Unity 6.4 world-space
UI Toolkit рендерится `PanelRenderer`, поэтому хост сначала ищет его на своём GameObject и подписывается
на `RegisterUIReloadCallback` — элемент добавляется в тот корень, который отдаёт рендерер, и переезжает
при каждой перезагрузке дерева. `UIDocument` остаётся только фолбэком для редакторов, где `PanelRenderer`
ещё нет (проверено рефлексией: в Unity 6000.3 типа `PanelRenderer` в сборке нет вовсе, поэтому ветка
закрыта `#if UNITY_6000_4_OR_NEWER`). `[RequireComponent(typeof(UIDocument))]` снят: он навязывал
legacy-компонент проектам, которые с `UIDocument` уже ушли. Текущую привязку показывает `Host Kind` в
инспекторе; если `PanelRenderer` ещё не отдал корень, инспектор говорит об этом прямо, а не молчит.

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:neo="Neo.UI">
    <neo:UIMeshRigElement name="rig" layout-preset="Character"
        style="width: 300px; height: 300px;" />
</ui:UXML>
```

## World

`UIMeshRigWorldRenderer` работает без Canvas. Он пишет ту же геометрию в runtime `Mesh` компонента
`MeshFilter`, а Sprite/Color — в `MeshRenderer`. Дочерние `UIMeshRigPoint` и `UIMeshRigPointMotion`
переиспользуются без копии deformation-кода. `Pixels Per Unit` переводит pixel-authored amplitudes
motion presets в мировые единицы.

`UIMeshRigWorldRenderer` остаётся для случаев, где нужен собственный материал или шейдер, произвольный
размер и pivot независимо от импортных настроек спрайта.

## SpriteRenderer

`UIMeshRigSpriteRenderer` деформирует артворк, оставляя обычный `SpriteRenderer`: sorting layers,
2D-свет, sprite masks и SRP batching продолжают работать. Размер берётся из самого Sprite
(`rect / pixelsPerUnit`), его же `Pixels Per Unit` переводит pixel-authored amplitudes в мировые единицы.

**Импортированный ассет не изменяется никогда.** Компонент создаёт runtime-клон (`Sprite.Create` по
текстуре и `textureRect` исходника), пишет геометрию в клон и отдаёт клон рендереру; на `OnDisable` клон
уничтожается, а рендереру возвращается исходный Sprite. Ассет — общее состояние проекта: правка пережила
бы выход из Play Mode и молча испортила бы спрайт во всём проекте.

**Почему не `Sprite.OverrideGeometry`.** Метод публичный и не требует 2D Animation, но на спрайте, не
подкреплённом импортом, он молча ничего не делает: вызов на runtime-клоне оставляет и число вершин, и их
позиции без изменений (замерено в живом редакторе 6000.3.14f1 — 173 вершины до и после). Заставить его
сработать можно только на импортированном ассете, то есть ровно той мутацией общего состояния, которой
адаптер обязан избежать. Поэтому используется публичный `UnityEngine.U2D.SpriteDataAccessExtensions`
(`SetVertexCount` / `SetVertexAttribute` / `SetIndices`): он пишет позиции, UV и индексы в любой экземпляр
Sprite, живёт в `UnityEngine.CoreModule` и не требует дополнительных пакетов. Рендерер подхватывает новую
геометрию без переприсваивания `SpriteRenderer.sprite` (проверено рендером до и после перезаписи).
`SpriteRendererDataAccessExtensions.SetDeformableBuffer` для сравнения — `internal`, полагаться на него
в пакете нельзя.

**Bounds Headroom.** `Sprite.bounds` считаются из `rect / pixelsPerUnit` и не растут вместе с записанной
геометрией, поэтому сильно деформированный спрайт может быть отсечён culling у края экрана. Поле
`Bounds Headroom` (по умолчанию 0.25) создаёт клон с пропорционально меньшим PPU: увеличиваются только
границы, вершины остаются в честных мировых единицах.

Draw Mode у `SpriteRenderer` должен быть `Simple`: `Sliced` и `Tiled` строят собственную геометрию и
затирают деформацию. Инспектор предупреждает об этом явно.

## Влияние и движение

У точки два независимых эллипса: внутри INNER действует полный вес, снаружи OUTER вес равен нулю,
между ними применяется Falloff Curve. `UIMeshRigPointMotion` добавляет процедурную позу поверх Transform
и не перезаписывает Animator keys. Presets: Float, Breathe, BodySway, HeadSway, SoftJiggle, Pulse,
SquashStretch, Wave и Noise. Общие layouts: SimpleBounce, Character и FlagCloth.

## Runtime API

```csharp
uguiRig.SetSource(sprite, Color.white);
uguiRig.SetGridResolution(16, 20);

worldRig.SetSource(sprite, Color.white);
worldRig.SetSize(new Vector2(3f, 3f));
UIMeshRigLayoutBuilder.Apply(worldRig, UIMeshRigLayoutPreset.FlagCloth);

spriteRig.SetSource(sprite, Color.white);
UIMeshRigLayoutBuilder.Apply(spriteRig, UIMeshRigLayoutPreset.SimpleBounce);
spriteRig.Rebuild(); // немедленная пересборка клона, без ожидания LateUpdate

UIMeshRigElement element = new UIMeshRigElement
{
    Sprite = sprite,
    Columns = 16,
    Rows = 20,
    LayoutPreset = UIMeshRigLayoutPreset.Character
};
document.rootVisualElement.Add(element);
```

## Demo

Импортируйте sample `Demo Scenes` и откройте `Demo/UI Mesh Rig/UIMeshRigDemo.unity`. Верхний ряд
сохраняет три прежних uGUI workflow (static pose, procedural motion, Animator). Нижний ряд показывает
три output adapter рядом: uGUI Simple Bounce, UI Toolkit Character и world Flag Cloth.
Карточка Animator шевелится только в Play Mode: Unity не проигрывает Animator-клипы вне Play Mode, а
соседние карточки продолжают двигаться в редакторе за счёт edit-mode preview у `UIMeshRigPointMotion`.
Примера `UIMeshRigSpriteRenderer` в сцене пока нет — создайте его пунктом
`GameObject > 2D Object > Neoxider UI Mesh Rig (Sprite Renderer)`.

## English summary

UI Mesh Rig has one geometry/deformation core and four thin outputs: uGUI `UIMeshRigGraphic`, UI Toolkit
`UIMeshRigElement`, world-space `UIMeshRigWorldRenderer`, and `UIMeshRigSpriteRenderer` for a plain
`SpriteRenderer`. Create each from its GameObject menu, or use the UI Toolkit custom control directly in
UXML/UI Builder. The SpriteRenderer adapter writes geometry into a runtime clone through the public
`SpriteDataAccessExtensions` API and never modifies the imported Sprite asset; `Sprite.OverrideGeometry`
is not used because it is a no-op on runtime sprites and only bites on the shared asset. The UI Toolkit
host binds to `PanelRenderer` on Unity 6.4+ and falls back to `UIDocument` only on older editors.

## Ограничения / limitations

- uGUI conversion рассчитана на `Image.Type.Simple`; Sliced, Tiled и Filled имеют другую геометрию.
- UI Toolkit использует `ushort` indices; текущий предел 40x40 значительно ниже лимита.
- `UIMeshRigSpriteRenderer` требует `Draw Mode = Simple`; `Sliced` и `Tiled` перестраивают геометрию сами.
- Границы клона растут только на `Bounds Headroom`; при экстремальной деформации увеличьте значение.
- Для многослойного персонажа, IK и большого набора skeletal clips лучше специализированный 2D rig.
- Плотная сетка и множество движущихся точек повышают стоимость rebuild; измеряйте Profiler.

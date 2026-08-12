# UI Mesh Rig

`UIMeshRigGraphic` деформирует один Sprite прямо внутри обычного uGUI Canvas. Камера, RenderTexture,
SpriteRenderer и отдельный формат анимации не нужны. Дочерние `UIMeshRigPoint` остаются RectTransform,
поэтому их можно анимировать стандартным Unity Animator.

## Быстрый старт

1. Создайте `GameObject > UI > Neoxider UI Mesh Rig` либо выберите Simple `Image` и используйте его
   контекстное меню конвертации.
2. В инспекторе `UI Mesh Rig Graphic` задайте Sprite и разрешение сетки. Для мобильного UI обычно
   достаточно 16x20; повышайте значение только после проверки Profiler.
3. В режиме **Setup** нажмите **Add Point**.
4. Перетащите отдельный жёлтый центр-якорь в Scene View.
5. Измените cyan **INNER / FULL** и orange **OUTER / ZERO** эллипсы их X/Y-маркерами:
   внутри INNER действует 100% веса, за OUTER — 0%, между ними работает Falloff Curve.
6. Переключитесь в **Pose / Animate** и двигайте, вращайте или масштабируйте точки.

Все три компонента используют общий фирменный `CustomEditorBase`: аватар и версия Neoxider Tools,
update status, Documentation и единое оформление секций. У выбранной точки контуры толще; стандартный
Transform gizmo в Setup скрывается, чтобы не перекрывать центр и радиусы.

## Два способа анимации

### Unity Animator

Добавьте Animator на rig или его родителя и записывайте Position, Rotation и Scale точек. Никакой
дополнительный компонент motion не требуется. Это лучший вариант для авторских клипов и Timeline.

### Встроенные кривые

Добавьте `UIMeshRigPointMotion` только на нужную точку. Доступны пресеты Float, Breathe, BodySway,
HeadSway, SoftJiggle, Pulse и SquashStretch, а также собственные кривые Position X/Y, Rotation и Scale X/Y.
Кнопка **Start Preview** автоматически переводит rig в Pose; Pause, Restart и Stop работают в Edit Mode.
Процедурная поза складывается с Transform-анимацией и не перезаписывает ключи Animator.

## Статическая деформация

В Pose расположите точки как нужно и сохраните сцену/Prefab. **Reset Pose** возвращает bind pose,
а **Capture Rest Pose** принимает текущую позу за новую нейтральную.

## Клики и Button

`UIMeshRigGraphic` — стандартный `MaskableGraphic`. Включите **Raycast Target** и добавьте обычный uGUI
`Button` на тот же GameObject. Режимы Hit Test:

- **Rect** — прямоугольник RectTransform;
- **Deformed Mesh** — фактическая деформированная сетка, включая выход за исходный Rect;
- **Sprite Alpha** — прозрачность Sprite; требует Read/Write Enabled и безопасно откатывается к mesh test.

Пустой или отключённый Sprite не перехватывает ввод. Конвертация интерактивного Image переносит
`Selectable.targetGraphic`; non-destructive вариант поддерживает один Undo и сохраняет disabled state.

## Компоненты

- `UIMeshRigGraphic` — один обязательный компонент изображения и сетки.
- `UIMeshRigPoint` — одна управляемая точка; отдельный компонент нужен, чтобы Animator мог адресовать её
  RectTransform и чтобы точку было удобно выбирать в Hierarchy/Scene.
- `UIMeshRigPointMotion` — необязателен; добавляется только для встроенной процедурной анимации.

## Runtime API

```csharp
rig.SetSource(sprite, Color.white);
rig.SetGridResolution(16, 20);
point.SetInfluenceRadii(new Vector2(0.08f, 0.12f), new Vector2(0.24f, 0.30f));
point.ApplyFalloffPreset(UIMeshRigFalloffPreset.Smooth);
point.SetProceduralPose(new Vector2(0f, 3f), 1.5f, new Vector2(1.02f, 0.99f));
```

## Demo

Импортируйте sample `Demo/UI Mesh Rig` и откройте `UIMeshRigDemo.unity`. В сцене три одинаковых
интерактивных изображения: постоянная ручная деформация, встроенная процедурная анимация и loop-клип
Unity Animator. Все три используют точный hit-test и обычный Button.

## Ограничения

- Конвертация рассчитана на `Image.Type.Simple`; Sliced, Tiled и Filled имеют другую геометрию.
- Для сложного многослойного персонажа, IK и сотен клипов лучше Spine/AnyPortrait.
- Чем плотнее сетка и больше одновременно движущихся точек, тем дороже Canvas rebuild; измеряйте Profiler.

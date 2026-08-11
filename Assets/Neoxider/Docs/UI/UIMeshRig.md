# UI Mesh Rig

## Version 10.8: two-zone gizmos and interaction

- Yellow center disc is a dedicated 2D drag handle.
- Cyan INNER / FULL ellipse receives 100% influence and has independent X/Y handles.
- Orange OUTER / ZERO ellipse reaches zero influence and has independent X/Y handles.
- Linear, Smooth, Soft, Sharp and Custom curves blend the band between both ellipses.
- **Full Smooth From Center** removes the solid inner zone for continuous center-to-edge deformation.

`UIMeshRigGraphic` is a standard uGUI `MaskableGraphic`. Enable **Raycast Target** and add a normal `Button`
to the same GameObject. Hit Test supports Rect, Deformed Mesh and Sprite Alpha. Sprite Alpha requires a
readable texture and safely falls back to mesh hit testing when the texture cannot be read.

`UIMeshRigGraphic` — деформируемый `uGUI Graphic` для обычного Canvas. Он рисует Sprite как
подразделённую сетку, а дочерние `UIMeshRigPoint` работают как кости с эллиптической областью влияния.
Точки остаются обычными `RectTransform`, поэтому Unity Animator умеет записывать их Position, Rotation
и Scale без собственного формата анимаций.

## Когда использовать

- лёгкая скелетная анимация персонажа, декора или кнопки внутри Canvas;
- дыхание, покачивание головы/руки, squash/stretch;
- постоянная ручная коррекция или искажение одного PNG;
- UI, которому нужны Mask, Canvas sorting, RectTransform и адаптивные якоря.

Для сложного многослойного персонажа, IK и сотен клипов лучше Spine/AnyPortrait. Unity SpriteSkin подходит
для `SpriteRenderer`, но не является прямой заменой `uGUI Graphic`.

## Быстрый старт

1. Создайте `GameObject > UI > Neoxider UI Mesh Rig` или выберите `Image` и вызовите
   `Create Neoxider Mesh Rig Child` из контекстного меню компонента.
2. Назначьте Sprite и разрешение сетки. 16×20 обычно достаточно для телефона.
3. В режиме **Setup** нажмите **Add Point**. Переместите точку в Scene View.
4. Внешний эллипс задаёт область влияния. Квадратные маркеры меняют ширину/высоту.
   Внутренний эллипс показывает сплошную область; круглый маркер меняет Falloff.
5. Переключитесь в **Pose / Animate**. Выберите Move, Rotate или Scale и деформируйте изображение прямо
   в Scene View.
6. Для статического искажения оставьте сохранённые Transform точек в Pose. Для анимации откройте Animation,
   включите Record и записывайте Transform дочерних точек.

## Два режима

### Setup

Перемещение точки меняет bind/rest pose. Сетка не деформируется. Здесь настраиваются расположение,
радиус и затухание веса.

### Pose / Animate

Трансформации считаются относительно bind pose и сразу деформируют сетку. Кнопка **Reset Pose** возвращает
все точки в bind pose. **Capture Rest Pose** принимает текущие положения как новую нейтральную позу.

## Runtime API

```csharp
using Neo.UI;

rig.SetSource(sprite, Color.white);
rig.SetGridResolution(16, 20);
rig.SetAuthoringMode(UIMeshRigAuthoringMode.Pose);
rig.ResetPose();
```

`UIMeshRigPoint.RadiusNormalized` и `Falloff` доступны из кода. Несколько перекрывающихся точек смешиваются
нормализованно, поэтому сетка не получает двойной полный transform.

## Производительность

16×20 создаёт 357 вершин и 640 треугольников. Сетка перестраивается только когда изменилась точка или
параметр. Для десятков одновременно анимированных крупных UI‑ригoв уменьшайте сетку до 10×14 и измеряйте
Canvas rebuild в Profiler.

---

## English quick reference

`UIMeshRigGraphic` is a deformable uGUI sprite driven by child `UIMeshRigPoint` RectTransforms. Use Setup
to edit bind positions, influence ellipses and falloff. Use Pose / Animate to move, rotate and scale points
in Scene View or record their native Transform properties in Unity Animator. Saved Pose transforms can also
serve as a permanent static deformation. Reset Pose restores the bind pose; Capture Rest Pose accepts the
current transforms as the new bind pose.

## Процедурная анимация без Animator

Добавьте `UIMeshRigPointMotion` на любую точку. Компонент вычисляет пять независимых редактируемых кривых:

- локальное смещение X/Y в UI-пикселях;
- поворот по Z в градусах;
- множитель масштаба X/Y относительно `(1, 1)`.

Компонент не записывает `RectTransform`. Его результат складывается решателем сетки поверх Transform-позы,
поэтому Unity Animator и процедурное дыхание/покачивание могут работать одновременно. Из кода доступны
`Play`, `Pause`, `Resume`, `Stop`, `Restart`, `SetTime` и детерминированный `EvaluateAt`.

Пресеты `Float`, `Breathe`, `BodySway`, `HeadSway`, `SoftJiggle`, `Pulse` и `SquashStretch` — это только
удобные стартовые настройки. При применении они копируют обычные `AnimationCurve`, после чего любые ключи,
амплитуды и длительность можно менять вручную. `Phase` разносит синхронные точки по фазе, `Speed` меняет
скорость, а `Use Unscaled Time` подходит для UI поверх паузы.

## Адаптивность и жизненный цикл

- Прямые точки сохраняют bind-центр нормализованными anchors, поэтому CanvasScaler и изменение RectTransform
  не создают ложное смещение.
- Иерархия дочерних точек поддерживается; Reset Pose восстанавливает их локальные transform.
- Вложенный `UIMeshRigGraphic` владеет только своими ближайшими точками.
- Выключенная или неактивная точка имеет нулевой вес.
- В Player деформацией управляет `Deformation Enabled`; переключатель Setup/Pose относится только к Scene View.
- Stop/Disable процедурного motion возвращает точную identity-позу и не меняет Animator/Transform.

## Ограничения конвертации Image

Контекстная команда рассчитана на обычный `Image.Type.Simple` и сохраняет effective sprite, tint, material,
raycast, maskable и preserve-aspect. Sliced, Tiled и Filled имеют другую геометрию и не должны молча
конвертироваться в деформируемую сетку: сначала подготовьте Simple sprite либо оставьте исходный Image.

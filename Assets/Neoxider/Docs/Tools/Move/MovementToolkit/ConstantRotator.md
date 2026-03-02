# ConstantRotator

**Что это:** компонент постоянного вращения объекта вокруг оси. Режимы: Transform, Rigidbody, Rigidbody2D. Ось задаётся через AxisSource (LocalForward3D, Up2D, Right2D, Custom). Пространство имён `Neo.Tools`, файл `Scripts/Tools/Move/MovementToolkit/ConstantRotator.cs`.

**Как использовать:** Add Component → Neoxider → Tools → Movement → ConstantRotator; выбрать режим и источник оси, задать скорость (град/сек при useDeltaTime).

---

## Параметры

- **mode** — Transform | Rigidbody | Rigidbody2D.
- **axisSource** — None | LocalForward3D | Up2D | Right2D | Custom; при Custom — **customAxis**.
- **speed** — градусы в секунду (при useDeltaTime) или за кадр.
- **spaceLocal**, **useDeltaTime** — как в [ConstantMover](./ConstantMover.md).

## См. также

- [ConstantMover](./ConstantMover.md), [KeyboardMover](./KeyboardMover.md)

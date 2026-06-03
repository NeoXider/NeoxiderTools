# GridSystem Dice

`Neo.GridSystem.Dice` - переиспользуемый слой размещения игральных кубиков поверх `FieldGenerator` и универсального ядра `Neo.Merge`.

## Runtime API

- `DicePiece` описывает одиночный кубик или пару с local offsets и методами поворота.
- `DicePieceGenerator` создает одиночные/парные фишки из пула значений, с равными шансами и без одинаковых значений в паре.
- `DiceBoardService` проверяет размещение, пишет значения в `FieldCell.ContentId` и запускает dice merge через `GridMergeResolver`.

Модуль отвечает только за размещение и merge: 3+ одинаковых значения, касание гранями, результат `old + 1`, cascade от result cell. Очки, progression пула, win/loss и UI остаются в игре или sample-сцене.

## Sample

Playable sample scene:

`Assets/Neoxider/Samples/Demo/Scenes/GridSystem/GridSystemDiceMergeDemo.unity`

Sample использует спрайты `Assets/Neoxider/Sprites/Dice`, drag/drop, поворот пары в tray, score, progression пула и game-over.

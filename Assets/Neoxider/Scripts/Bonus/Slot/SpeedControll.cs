// Assets/Neoxider/Scripts/Bonus/Slot/SpeedControll.cs
using System;

namespace Neo.Bonus
{
    /// <summary>
    /// Параметры скорости барабана:
    /// - speed: стартовая скорость (юн/с), знак задаёт направление ( + вверх, - вниз ).
    /// - timeSpin: время фазовой прокрутки на постоянной скорости перед началом торможения (сек).
    /// </summary>
    [Serializable]
    public struct SpeedControll
    {
        public float speed;
        public float timeSpin;

        public static SpeedControll Default(float speed = 5000f, float timeSpin = 1f)
            => new SpeedControll { speed = speed, timeSpin = timeSpin };
    }
}
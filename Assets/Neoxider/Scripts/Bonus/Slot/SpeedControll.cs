// Assets/Neoxider/Scripts/Bonus/Slot/SpeedControll.cs

using System;

namespace Neo.Bonus
{
    /// <summary>
    ///     Reel speed parameters:
    ///     - speed: starting speed (units/s); sign is direction (+ up, - down).
    ///     - timeSpin: duration of constant-speed phase before braking starts (seconds).
    /// </summary>
    [Serializable]
    public struct SpeedControll
    {
        public float speed;
        public float timeSpin;

        public static SpeedControll Default(float speed = 5000f, float timeSpin = 1f)
        {
            return new SpeedControll { speed = speed, timeSpin = timeSpin };
        }
    }
}

using System;
using UnityEngine;

namespace Neo.Bonus
{
    [Serializable]
    public class SpeedControll
    {
        [Tooltip("Начальная скорость вращения барабана.")]
        public float speed = 30f;

        [Tooltip("Время вращения с постоянной скоростью (до начала замедления).")]
        public float timeSpin = 2f;

        [Tooltip("Время, за которое барабан полностью замедлится до минимальной скорости.")] [Min(0.1f)]
        public float decelerationTime = 1f;

        [Tooltip("Минимальная скорость, до которой замедляется барабан перед полной остановкой.")]
        public float minSpeed = 5;
    }
}
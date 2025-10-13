using System;
using DG.Tweening;
using UnityEngine;

namespace Neo.Bonus
{
    [Serializable]
    public class SpeedControll
    {
        [Tooltip("Начальная скорость вращения барабана в юнитах/сек.")]
        public float speed = 30f;

        [Tooltip("Время вращения с постоянной скоростью (до начала замедления).")]
        public float timeSpin = 2f;

        [Tooltip("Время, за которое барабан плавно остановится.")]
        [Min(0.1f)]
        public float decelerationTime = 1f;

        [Tooltip("Тип замедления из стандартного набора DOTween.")]
        public Ease decelerationEase = Ease.InOutQuad;
    }
}

using UnityEngine;

namespace Neo.Bonus
{
    [System.Serializable]
    public class SpeedControll
    {
        public float speed = 30f;
        public float timeSpin = 3f;
        [Min(0)] public float changeEndSpeed = 5;
        public float minSpeed = 5;
    }
}

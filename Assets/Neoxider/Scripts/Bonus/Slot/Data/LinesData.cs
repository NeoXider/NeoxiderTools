using System;
using UnityEngine;

namespace Neo.Bonus
{
    [CreateAssetMenu(fileName = "LinesData", menuName = "Neoxider/Slot/LinesData", order = 1)]
    public class LinesData : ScriptableObject
    {
        [SerializeField] private InnerArray[] _lines =
        {
            new() { corY = new[] { 0, 0, 0 } },
            new() { corY = new[] { 1, 1, 1 } },
            new() { corY = new[] { 2, 2, 2 } }
        };

        public InnerArray[] lines => _lines;

        [Serializable]
        public class InnerArray
        {
            public int[] corY;
        }
    }
}
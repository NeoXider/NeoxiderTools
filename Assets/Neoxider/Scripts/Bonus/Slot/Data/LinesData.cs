using UnityEngine;

namespace Neo.Bonus
{
    [CreateAssetMenu(fileName = "LinesData", menuName = "Neoxider/Slot/LinesData", order = 1)]
    public class LinesData : ScriptableObject
    {
        [System.Serializable]
        public class InnerArray
        {
            public int[] corY;
        }

        [SerializeField]
        private InnerArray[] _lines =
        {
        new InnerArray { corY = new int[] { 0, 0, 0 } },
        new InnerArray { corY = new int[] { 1, 1, 1 } },
        new InnerArray { corY = new int[] { 2, 2, 2 } }
    };

        public InnerArray[] lines => _lines;
    }
}
using System;
using UnityEngine;

namespace Neo.Bonus
{
    /// <summary>
    ///     ScriptableObject holding win-line configuration for the slot machine.
    ///     Defines symbol coordinates for each win line.
    /// </summary>
    [CreateAssetMenu(fileName = "Lines Data", menuName = "Neoxider/Bonus/Slot/Lines Data", order = 2)]
    public class LinesData : ScriptableObject
    {
        [Tooltip("Win lines array. Each line contains Y coordinates per column.")] [SerializeField]
        private InnerArray[] _lines =
        {
            new() { corY = new[] { 0, 0, 0 } },
            new() { corY = new[] { 1, 1, 1 } },
            new() { corY = new[] { 2, 2, 2 } }
        };

        /// <summary>
        ///     Win lines array.
        /// </summary>
        public InnerArray[] lines => _lines;

        /// <summary>
        ///     Inner type storing coordinates for one line.
        /// </summary>
        [Serializable]
        public class InnerArray
        {
            [Tooltip("Y coordinates for each column of the line")]
            public int[] corY;
        }
    }
}

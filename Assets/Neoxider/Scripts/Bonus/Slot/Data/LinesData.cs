using System;
using UnityEngine;

namespace Neo.Bonus
{
    /// <summary>
    ///     ScriptableObject для хранения конфигурации линий выигрыша в слот-машине.
    ///     Определяет координаты символов для каждой выигрышной линии.
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
        ///     Массив линий выигрыша.
        /// </summary>
        public InnerArray[] lines => _lines;

        /// <summary>
        ///     Внутренний класс для хранения координат одной линии.
        /// </summary>
        [Serializable]
        public class InnerArray
        {
            [Tooltip("Y coordinates for each column of the line")]
            public int[] corY;
        }
    }
}
using System;
using UnityEngine;

namespace Neo.Bonus
{
    /// <summary>
    ///     ScriptableObject для хранения конфигурации линий выигрыша в слот-машине.
    ///     Определяет координаты символов для каждой выигрышной линии.
    /// </summary>
    [CreateAssetMenu(fileName = "Lines Data", menuName = "Neo/Bonus/Slot/Lines Data", order = 2)]
    public class LinesData : ScriptableObject
    {
        [Tooltip("Массив линий выигрыша. Каждая линия содержит координаты Y для каждого столбца.")]
        [SerializeField] private InnerArray[] _lines =
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
            [Tooltip("Координаты Y для каждого столбца линии")]
            public int[] corY;
        }
    }
}
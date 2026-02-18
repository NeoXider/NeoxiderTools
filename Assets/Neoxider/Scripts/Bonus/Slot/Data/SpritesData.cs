using System;
using UnityEngine;

namespace Neo.Bonus
{
    /// <summary>
    ///     Визуальные данные для одного символа слот-машины.
    /// </summary>
    [Serializable]
    public class SlotVisualData
    {
        [Tooltip("Element ID, assigned automatically from array index.")]
        public int id;

        [Tooltip("Symbol sprite for slot machine display")]
        public Sprite sprite;

        [Tooltip("Symbol description")] [TextArea(1, 3)]
        public string description;
    }

    /// <summary>
    ///     ScriptableObject для хранения визуальных данных всех символов слот-машины.
    ///     Автоматически присваивает ID каждому символу на основе индекса.
    /// </summary>
    [CreateAssetMenu(fileName = "Sprites Data", menuName = "Neo/Bonus/Slot/Sprites Data", order = 3)]
    public class SpritesData : ScriptableObject
    {
        [Tooltip("Visual data array for all slot machine symbols")] [SerializeField]
        private SlotVisualData[] _visuals;

        /// <summary>
        ///     Массив визуальных данных символов.
        /// </summary>
        public SlotVisualData[] visuals => _visuals;

        private void OnValidate()
        {
            if (_visuals == null)
            {
                return;
            }

            // Автоматически присваиваем ID на основе индекса
            for (int i = 0; i < _visuals.Length; i++)
            {
                if (_visuals[i] != null)
                {
                    _visuals[i].id = i;
                }
            }
        }
    }
}
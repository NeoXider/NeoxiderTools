using System;
using UnityEngine;

namespace Neo.Bonus
{
    [Serializable]
    public class SlotVisualData
    {
        [Tooltip("ID элемента, присваивается автоматически на основе его индекса в массиве.")]
        public int id;

        public Sprite sprite;
        [TextArea] public string description;
    }

    [CreateAssetMenu(fileName = "SpritesDefault", menuName = "Neoxider/Slot/Sprites")]
    public class SpritesData : ScriptableObject
    {
        [SerializeField] private SlotVisualData[] _visuals;
        public SlotVisualData[] visuals => _visuals;

        private void OnValidate()
        {
            if (_visuals == null) return;
            // Автоматически присваиваем ID на основе индекса
            for (var i = 0; i < _visuals.Length; i++)
                if (_visuals[i] != null)
                    _visuals[i].id = i;
        }
    }
}
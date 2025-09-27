using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Bonus
{
    [CreateAssetMenu(fileName = "SpritesMultipliersDefault", menuName = "Neoxider/Slot/SpritesMult")]
    public class SpriteMultiplayerData : ScriptableObject
    {
        [SerializeField] private SpritesMultiplier _spritesMultiplier;

        [Space] [Header("Auto Generate")] [SerializeField]
        private bool _generate;

        [SerializeField] private int _minCount = 3;
        [SerializeField] private int _maxCount = 3;
        [SerializeField] private int defaultMultiplayer = 1;
        [SerializeField] private SpritesData _spritesData;
        public SpritesMultiplier spritesMultiplier => _spritesMultiplier;


        private void OnValidate()
        {
            if (_generate)
            {
                _generate = false;

                if (_spritesData != null) AutoGenerateSpriteMultiplayer();
            }
        }

        private void AutoGenerateSpriteMultiplayer()
        {
            var list = new List<IdMult>();

            if (_spritesData.visuals == null) return;

            for (var s = 0; s < _spritesData.visuals.Length; s++)
            {
                var countList = new List<CountMultiplayer>();

                for (var i = _minCount; i <= _maxCount; i++)
                    countList.Add(new CountMultiplayer { count = i, mult = defaultMultiplayer });

                // Используем ID из SpritesData
                list.Add(new IdMult { id = _spritesData.visuals[s].id, countMult = countList.ToArray() });
            }

            _spritesMultiplier.spriteMults = list.ToArray();
        }

        #region structs

        [Serializable]
        public class SpritesMultiplier
        {
            public IdMult[] spriteMults;
        }

        [Serializable]
        public struct IdMult
        {
            [Tooltip("ID элемента из SpritesData")]
            public int id;

            public CountMultiplayer[] countMult;
        }

        [Serializable]
        public struct CountMultiplayer
        {
            public int count;
            public float mult;
        }

        #endregion
    }
}
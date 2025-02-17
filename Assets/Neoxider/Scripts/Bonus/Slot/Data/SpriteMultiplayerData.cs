using System.Collections.Generic;
using UnityEngine;

namespace Neo.Bonus
{
    [CreateAssetMenu(fileName = "SpritesMultipliersDafualt", menuName = "Neoxider/Slot/SpritesMult")]
    public class SpriteMultiplayerData : ScriptableObject
    {
        #region structs
        [System.Serializable]
        public class SpritesMultiplier
        {
            public SpriteMult[] spriteMults;
        }

        [System.Serializable]
        public struct SpriteMult
        {
            public Sprite sprite;
            public CountMultiplayer[] countMult;
        }

        [System.Serializable]
        public struct CountMultiplayer
        {
            public int count;
            public float mult;
        }
        #endregion

        [SerializeField] private SpritesMultiplier _spritesMultiplier;
        public SpritesMultiplier spritesMultiplier => _spritesMultiplier;

        [Space, Header("Auto Generate")]
        [SerializeField] private bool _generate;
        [SerializeField] private int _minCount = 3;
        [SerializeField] private int _maxCount = 3;
        [SerializeField] private int defaultMultiplayer = 1;
        [SerializeField] private SpritesData _spritesData;


        private void OnValidate()
        {
            if (_generate)
            {
                _generate = false;

                if (_spritesData != null)
                {
                    AutoGenerateSpriteMultiplayer();
                }
            }
        }

        private void AutoGenerateSpriteMultiplayer()
        {
            List<SpriteMult> list = new List<SpriteMult>();

            for (int s = 0; s < _spritesData.sprites.Length; s++)
            {
                List<CountMultiplayer> countList = new List<CountMultiplayer>();

                for (int i = _minCount; i <= _maxCount; i++)
                {
                    countList.Add(new CountMultiplayer { count = i, mult = defaultMultiplayer });
                }

                list.Add(new SpriteMult { sprite = _spritesData.sprites[s], countMult = countList.ToArray() });
            }

            _spritesMultiplier.spriteMults = list.ToArray();

        }
    }
}
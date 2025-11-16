using System;
using System.Collections.Generic;
using UnityEngine;

namespace Neo.Bonus
{
    /// <summary>
    ///     ScriptableObject для хранения множителей выигрыша в зависимости от количества символов.
    ///     Поддерживает автоматическую генерацию множителей на основе SpritesData.
    /// </summary>
    [CreateAssetMenu(fileName = "Sprite Multiplier Data", menuName = "Neo/Bonus/Slot/Sprite Multiplier Data", order = 4)]
    public class SpriteMultiplayerData : ScriptableObject
    {
        [Tooltip("Конфигурация множителей для каждого символа")]
        [SerializeField] private SpritesMultiplier _spritesMultiplier;

        [Space] 
        [Header("Auto Generate")] 
        [Tooltip("Включить автоматическую генерацию множителей на основе SpritesData")]
        [SerializeField]
        private bool _generate;

        [Tooltip("Минимальное количество символов для генерации")]
        [SerializeField] private int _minCount = 3;
        
        [Tooltip("Максимальное количество символов для генерации")]
        [SerializeField] private int _maxCount = 3;
        
        [Tooltip("Множитель по умолчанию для всех комбинаций")]
        [SerializeField] private int defaultMultiplayer = 1;
        
        [Tooltip("Ссылка на SpritesData для автоматической генерации")]
        [SerializeField] private SpritesData _spritesData;
        
        /// <summary>
        ///     Конфигурация множителей.
        /// </summary>
        public SpritesMultiplier spritesMultiplier => _spritesMultiplier;


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
            List<IdMult> list = new();

            if (_spritesData.visuals == null)
            {
                return;
            }

            for (int s = 0; s < _spritesData.visuals.Length; s++)
            {
                List<CountMultiplayer> countList = new();

                for (int i = _minCount; i <= _maxCount; i++)
                {
                    countList.Add(new CountMultiplayer { count = i, mult = defaultMultiplayer });
                }

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
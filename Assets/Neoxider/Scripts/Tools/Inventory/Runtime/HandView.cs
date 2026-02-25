using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Вьюшка руки: вешается на префаб подбираемого предмета (тот же, что WorldDropPrefab).
    ///     InventoryHand при отображении предмета в руке ищет этот компонент на экземпляре и применяет
    ///     смещение позиции, поворота и базовый масштаб; поверх применяется общий масштаб руки (дельта или фиксированный).
    /// </summary>
    [NeoDoc("Tools/Inventory/HandView.md")]
    [CreateFromMenu("Neoxider/Tools/Inventory/HandView")]
    [AddComponentMenu("Neoxider/" + "Tools/Inventory/" + nameof(HandView))]
    public sealed class HandView : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Смещение позиции предмета относительно точки руки (локальные координаты).")]
        private Vector3 _positionOffset;

        [SerializeField]
        [Tooltip("Смещение поворота в градусах (Euler) относительно руки.")]
        private Vector3 _rotationOffset;

        [SerializeField]
        [Tooltip("Базовый масштаб этого предмета в руке (1 = без изменения). Поверх применяется общий масштаб руки (дельта или фиксированный).")]
        [Min(0.01f)]
        private float _scaleInHand = 1f;

        /// <summary>Смещение позиции в локальных координатах руки.</summary>
        public Vector3 PositionOffset => _positionOffset;

        /// <summary>Смещение поворота в градусах (Euler).</summary>
        public Vector3 RotationOffset => _rotationOffset;

        /// <summary>Базовый масштаб предмета в руке.</summary>
        public float ScaleInHand => Mathf.Max(0.01f, _scaleInHand);

        private void Reset()
        {
            _scaleInHand = 1f;
        }
    }
}

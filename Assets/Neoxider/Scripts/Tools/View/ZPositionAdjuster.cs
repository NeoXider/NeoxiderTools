using UnityEngine;

namespace Neo.Tools
{
    public class ZPositionAdjuster : MonoBehaviour
    {
        [SerializeField]
        private bool _useNormalizeToUnit = true;

        [Min(0)]
        [SerializeField]
        private float _ratio = 1;

        void LateUpdate()
        {
            AdjustZBasedOnY();
        }

        private void AdjustZBasedOnY()
        {
            Vector3 position = transform.position;

            float newY = _useNormalizeToUnit ? position.y.NormalizeToUnit() : position.y.NormalizeToRangeMinusOneToOne();

            position.z = newY * _ratio;
            transform.position = position;
        }

        private void OnValidate()
        {
            AdjustZBasedOnY();
        }
    }
}
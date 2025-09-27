using Neo.Extensions;
using UnityEngine;

namespace Neo.Tools
{
    public class ZPositionAdjuster : MonoBehaviour
    {
        [SerializeField] private bool _useNormalizeToUnit = true;

        [Min(0)] [SerializeField] private float _ratio = 1;

        private void LateUpdate()
        {
            AdjustZBasedOnY();
        }

        private void OnValidate()
        {
            AdjustZBasedOnY();
        }

        private void AdjustZBasedOnY()
        {
            var position = transform.position;

            var newY = _useNormalizeToUnit ? position.y.NormalizeToUnit() : position.y.NormalizeToRange();

            position.z = newY * _ratio;
            transform.position = position;
        }
    }
}
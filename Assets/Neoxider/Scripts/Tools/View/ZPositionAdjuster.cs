using Neo.Extensions;
using UnityEngine;

namespace Neo.Tools
{
    [NeoDoc("Tools/View/ZPositionAdjuster.md")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(ZPositionAdjuster))]
    public class ZPositionAdjuster : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] private bool _useNormalizeToUnit = true;

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
            Vector3 position = transform.position;

            float newY = _useNormalizeToUnit ? position.y.NormalizeToUnit() : position.y.NormalizeToRange();

            position.z = newY * _ratio;
            transform.position = position;
        }
    }
}

using System.Collections;
using UnityEngine;

namespace Neo.Tools
{
    public class CameraShake : MonoBehaviour
    {
        [Header("Shake Settings")] [SerializeField]
        private float _shakeDuration = 0.5f;

        [SerializeField] private float _shakeMagnitude = 0.5f;

        [SerializeField] private bool _shakeX = true;
        [SerializeField] private bool _shakeY = true;
        [SerializeField] private bool _shakeZ;

        private Vector3 startPos;

        private void Awake()
        {
            startPos = transform.localPosition;
        }

        public void StartShake(float duration)
        {
            StartShake(duration, _shakeMagnitude);
        }

        public void StartShake(float duration, float magnitude)
        {
            StopAllCoroutines();
            StartCoroutine(Shake(duration, magnitude));
        }

        public void StartDefaultShake()
        {
            StartShake(_shakeDuration, _shakeMagnitude);
        }

        private IEnumerator Shake(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float x = _shakeX ? Random.Range(-1f, 1f) * magnitude : 0f;
                float y = _shakeY ? Random.Range(-1f, 1f) * magnitude : 0f;
                float z = _shakeZ ? Random.Range(-1f, 1f) * magnitude : 0f;

                transform.localPosition = new Vector3(startPos.x + x, startPos.y + y, startPos.z + z);

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = startPos;
        }
    }
}
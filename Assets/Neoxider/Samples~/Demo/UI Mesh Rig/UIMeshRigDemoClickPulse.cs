using System.Collections;
using Neo.UI;
using UnityEngine;

namespace Neo.Samples.UI
{
    /// <summary>Small click feedback used only by the UI Mesh Rig sample.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIMeshRigGraphic))]
    public sealed class UIMeshRigDemoClickPulse : MonoBehaviour
    {
        [SerializeField] private Color _pressedColor = new Color(0.35f, 0.95f, 1f, 1f);
        [Min(0.05f)] [SerializeField] private float _duration = 0.28f;

        private UIMeshRigGraphic _graphic;
        private Coroutine _pulse;

        public void Play()
        {
            if (_graphic == null)
            {
                _graphic = GetComponent<UIMeshRigGraphic>();
            }

            if (_pulse != null)
            {
                StopCoroutine(_pulse);
            }

            _pulse = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            Color original = _graphic.color;
            float elapsed = 0f;
            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / _duration);
                float strength = Mathf.Sin(normalized * Mathf.PI);
                _graphic.color = Color.Lerp(original, _pressedColor, strength * 0.72f);
                yield return null;
            }

            _graphic.color = original;
            _pulse = null;
        }

        private void OnDisable()
        {
            if (_graphic != null)
            {
                _graphic.color = Color.white;
            }

            _pulse = null;
        }
    }
}

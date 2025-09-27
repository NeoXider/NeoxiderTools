using UnityEngine;
using UnityEngine.UI;

namespace Neo
{
    namespace Audio
    {
        [AddComponentMenu("Neoxider/" + "Audio/" + nameof(PlayAudioBtn))]
        public class PlayAudioBtn : MonoBehaviour
        {
            [SerializeField] private int _idClip;

            [SerializeField] [GetComponent] private Button _button;

            private void OnEnable()
            {
                if (_button != null)
                    _button.onClick.AddListener(AudioPlay);
            }

            private void OnDisable()
            {
                if (_button != null)
                    _button.onClick.RemoveListener(AudioPlay);
            }

            public void AudioPlay()
            {
                AM.I.Play(_idClip);
            }
        }
    }
}
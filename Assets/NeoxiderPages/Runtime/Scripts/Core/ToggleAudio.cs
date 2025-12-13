using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;
using Neo.Extensions;
using Neo.UI;

namespace Neo.Pages
{
    [MovedFrom("")]
    [AddComponentMenu("Neo/Pages/" + nameof(ToggleAudio))]
    public class ToggleAudio : MonoBehaviour
    {
        [FormerlySerializedAs("_toggleView")] [SerializeField]
        private VisualToggle uiToggleView;

        [SerializeField] private bool isMusic;

        private void Awake()
        {
            this.WaitWhile(() => !G.Inited, Init);
        }

        private void Init()
        {
            if (isMusic)
            {
                uiToggleView?.OnValueChanged.AddListener((value) => Audio.IsActiveMusic = value);
            }
            else
            {
                uiToggleView?.OnValueChanged.AddListener((value) => Audio.IsActiveSound = value);
            }
        }

        private void OnEnable()
        {
            uiToggleView.SetActive(isMusic ? Audio.IsActiveMusic : Audio.IsActiveSound);
        }

        private void OnValidate()
        {
            uiToggleView ??= GetComponent<VisualToggle>();
        }
    }
}
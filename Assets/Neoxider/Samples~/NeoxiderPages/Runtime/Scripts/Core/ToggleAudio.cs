using Neo;
using Neo.Audio;
using Neo.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Neo.Pages
{
    /// <summary>
    ///     Binds a <see cref="VisualToggle" /> to the music or sound-effects switch of <see cref="AMSettings" />
    ///     through the <see cref="Audio" /> facade. Works without GM; without AMSettings the toggle is left as is.
    /// </summary>
    [MovedFrom("")]
    [NeoDoc("NeoxiderPages/ToggleAudio.md")]
    [AddComponentMenu("Neoxider/Pages/" + nameof(ToggleAudio))]
    public class ToggleAudio : MonoBehaviour
    {
        [FormerlySerializedAs("_toggleView")] [SerializeField]
        private VisualToggle uiToggleView;

        [SerializeField] private bool isMusic;

        private UnityAction<bool> _onToggleChanged;
        private VisualToggle _subscribedToggle;
        private bool _started;

        private void Start()
        {
            // WHY: the switch only needs AMSettings (resolved lazily by the facade); waiting for GM left the toggle
            // unbound in scenes without a game manager. Sync and subscribe in Start, after VisualToggle.Awake has
            // read its own state, so the toggle's initial value never overwrites the saved audio settings.
            SyncFromSettings();
            Subscribe();
            _started = true;
        }

        private void OnEnable()
        {
            if (_started)
            {
                SyncFromSettings();
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void OnValidate()
        {
            if (uiToggleView == null)
            {
                uiToggleView = GetComponent<VisualToggle>();
            }
        }

        /// <summary>Shows the current AMSettings state on the toggle (no-op without AMSettings or a toggle).</summary>
        public void SyncFromSettings()
        {
            if (uiToggleView == null || AMSettings.I == null)
            {
                return;
            }

            uiToggleView.SetActive(isMusic ? Audio.IsActiveMusic : Audio.IsActiveSound);
        }

        private void Subscribe()
        {
            if (uiToggleView == null)
            {
                NeoDiagnostics.LogWarning("[ToggleAudio] UI Toggle View is not assigned.", this);
                return;
            }

            _onToggleChanged ??= ApplyToSettings;
            _subscribedToggle = uiToggleView;
            _subscribedToggle.Value.AddListener(_onToggleChanged);
        }

        private void Unsubscribe()
        {
            if (_subscribedToggle == null || _onToggleChanged == null)
            {
                return;
            }

            _subscribedToggle.Value.RemoveListener(_onToggleChanged);
            _subscribedToggle = null;
        }

        private void ApplyToSettings(bool value)
        {
            if (isMusic)
            {
                Audio.IsActiveMusic = value;
            }
            else
            {
                Audio.IsActiveSound = value;
            }
        }
    }
}

using Neo.Tools;
using TMPro;
using UnityEngine;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>Selector</b> demo (Selector.unity). Three <see cref="Selector" /> components
    ///     - a skin picker (index mode), a level bar (fill mode) and hearts (fill mode with index offset -1) - their
    ///     item objects, the buttons and the info card are saved in the scene. Buttons
    ///     call <see cref="Selector.Next" />, <see cref="Selector.Previous" />, <see cref="Selector.SetRandom()" /> and
    ///     <see cref="Selector.SetLast" /> directly through persistent UnityEvents, and every selector's
    ///     <see cref="Selector.OnSelectionChanged" /> drives a readout without code (the NoCode path). This component
    ///     only listens to the same events to name the selected skin and log each change. It builds nothing at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Selector Demo")]
    public sealed class SelectorDemoController : MonoBehaviour
    {
        [Header("Selectors")]
        [SerializeField] private Selector _skins;
        [SerializeField] private Selector _levelBar;
        [SerializeField] private Selector _hearts;

        [Header("Skin names (same order as the skin items)")]
        [SerializeField] private string[] _skinNames = new string[0];

        [Header("UI")]
        [SerializeField] private TMP_Text _skinName;
        [SerializeField] private DemoLogFeed _log;

        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned.</summary>
        public bool IsSceneBuilt => _skins != null && _levelBar != null && _hearts != null && _log != null;

        public Selector Skins => _skins;
        public Selector LevelBar => _levelBar;
        public Selector Hearts => _hearts;

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            if (_skins != null)
            {
                ShowSkin(_skins.Value);
            }

            Log("3 Selectors ready - buttons call Next / Previous / SetRandom / SetLast directly");
        }

        /// <summary>Skin selector OnSelectionChanged listener.</summary>
        public void OnSkinChanged(int index)
        {
            ShowSkin(index);
            Log("Skins.OnSelectionChanged(" + index + ") -> " + SkinName(index));
        }

        /// <summary>Level bar OnSelectionChanged listener (fill mode: items 0..index are active).</summary>
        public void OnLevelChanged(int index)
        {
            Log("LevelBar.OnSelectionChanged(" + index + ") -> " + (index + 1) + " of " + Count(_levelBar) + " segments lit");
        }

        /// <summary>Hearts OnSelectionChanged listener (fill mode, index offset -1: index 0 shows no heart).</summary>
        public void OnHeartsChanged(int index)
        {
            Log("Hearts.OnSelectionChanged(" + index + ") -> " + index + " heart" + (index == 1 ? string.Empty : "s"));
        }

        /// <summary>Hearts OnFinished listener (loop is off, so Next at the end reports instead of wrapping).</summary>
        public void OnHeartsFull()
        {
            Log("Hearts.OnFinished -> already at full health (Loop is off)");
        }

        private void ShowSkin(int index)
        {
            if (_skinName != null)
            {
                _skinName.text = SkinName(index);
            }
        }

        private string SkinName(int index)
        {
            return _skinNames != null && index >= 0 && index < _skinNames.Length ? _skinNames[index] : "#" + index;
        }

        private static int Count(Selector selector)
        {
            return selector != null ? selector.Count : 0;
        }

        private void Log(string message)
        {
            if (_log != null)
            {
                _log.Log(message);
            }
        }

        private void WarnUnbuilt()
        {
            if (_warnedUnbuilt)
            {
                return;
            }

            _warnedUnbuilt = true;
            global::Neo.Demo.SampleDiagnostics.Warn(
                "[SelectorDemoController] The scene is not authored. Re-import the scene from the Demo download.",
                this);
        }
    }
}

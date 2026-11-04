using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace Neo.Samples
{
    /// <summary>
    ///     Tutorial info card of a demo scene: module title, purpose, runtime API, scene workflow, verification
    ///     and hint lines with a collapse button. The card itself is an authored uGUI + TextMeshPro object saved
    ///     in the scene (the Neo.Demo.Editor DemoSceneKit builds it and wires the collapse button as a persistent
    ///     listener to <see cref="ToggleCollapsed" />). This component only pushes its texts into the authored
    ///     labels and toggles the collapsible parts; it never constructs UI at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Demo/Module Demo Scene Info")]
    public sealed class ModuleDemoSceneInfo : MonoBehaviour
    {
        public enum Corner
        {
            TopLeft = 0,
            TopRight = 1,
            BottomLeft = 2,
            BottomRight = 3
        }

        [SerializeField] private string _moduleName = "Module";
        [SerializeField] private string _purpose = "Smoke demo scene.";
        [SerializeField] private string _runtimeApi = "Use the module runtime API from C#.";
        [SerializeField] private string _sceneWorkflow = "Use scene components as optional authoring wrappers.";
        [SerializeField] private string _verification = "Open scene and enter Play Mode.";
        [SerializeField] private bool _showRuntimeOverlay = true;

        [Header("Overlay Look")]
        [SerializeField] private Corner _corner = Corner.TopLeft;
        [SerializeField] private Color _accentColor = new Color32(124, 92, 255, 255);
        [SerializeField] private Color _backgroundColor = new Color32(30, 30, 40, 217);
        [SerializeField] [Min(240f)] private float _panelWidth = 460f;
        [SerializeField] private bool _startCollapsed;

        [Tooltip("Extra hint lines (what to press / what happens) shown under the description.")]
        [SerializeField] private string[] _hints = Array.Empty<string>();

        [Header("Authored Overlay")]
        [Tooltip("Root of the info card saved in the scene. Activated on play when Show Runtime Overlay is on.")]
        [SerializeField] private GameObject _overlayRoot;

        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _descriptionLabel;
        [SerializeField] private TMP_Text _hintsLabel;

        [Tooltip("Label of the collapse button (shows - or +).")]
        [SerializeField] private TMP_Text _collapseLabel;

        [Tooltip("Objects hidden while the card is collapsed (divider and body).")]
        [SerializeField] private GameObject[] _collapsibleParts = Array.Empty<GameObject>();

        private bool _collapsed;
        private bool _warnedMissingOverlay;

        public string ModuleName => _moduleName;
        public string Purpose => _purpose;
        public string RuntimeApi => _runtimeApi;
        public string SceneWorkflow => _sceneWorkflow;
        public string Verification => _verification;
        public IReadOnlyList<string> Hints => _hints;
        public bool ShowRuntimeOverlay => _showRuntimeOverlay;
        public Corner OverlayCorner => _corner;
        public Color AccentColor => _accentColor;
        public Color BackgroundColor => _backgroundColor;
        public float PanelWidth => _panelWidth;
        public bool StartCollapsed => _startCollapsed;
        public bool IsCollapsed => _collapsed;
        public GameObject OverlayRoot => _overlayRoot;

        /// <summary>True when the card is authored in the scene and this component points at it.</summary>
        public bool HasAuthoredOverlay => _overlayRoot != null && _titleLabel != null;

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            _collapsed = _startCollapsed;
            if (!HasAuthoredOverlay)
            {
                WarnMissingOverlay();
                return;
            }

            ApplyContent();
            SetOverlayVisible(_showRuntimeOverlay);
        }

        /// <summary>Creates a standalone info card at runtime for demos assembled from code.</summary>
        [Obsolete("Runtime-built info cards are deprecated. Author the card in the scene with the Neo.Demo.Editor " +
                  "DemoSceneKit (dev-docs/DemoSceneBuilders.md).")]
        public static ModuleDemoSceneInfo Show(string title, string description, params string[] hints)
        {
            GameObject host = new("ModuleDemoSceneInfo");
            host.SetActive(false);
            ModuleDemoSceneInfo info = host.AddComponent<ModuleDemoSceneInfo>();
            info._moduleName = title;
            info._purpose = description;
            info._runtimeApi = string.Empty;
            info._sceneWorkflow = string.Empty;
            info._verification = string.Empty;
            info._hints = hints ?? Array.Empty<string>();
            info._showRuntimeOverlay = true;
            NeoDemoShell.BuildRuntimeInfoOverlay(info);
            host.SetActive(true);
            return info;
        }

        /// <summary>Sets the texts and pushes them into the authored card.</summary>
        public void Configure(
            string moduleName,
            string purpose,
            string runtimeApi,
            string sceneWorkflow,
            string verification,
            bool showRuntimeOverlay = true)
        {
            _moduleName = moduleName;
            _purpose = purpose;
            _runtimeApi = runtimeApi;
            _sceneWorkflow = sceneWorkflow;
            _verification = verification;
            _showRuntimeOverlay = showRuntimeOverlay;
            Rebuild();
        }

        /// <summary>Replaces the hint lines and pushes them into the authored card.</summary>
        public void SetHints(params string[] hints)
        {
            _hints = hints ?? Array.Empty<string>();
            ApplyContent();
        }

        /// <summary>Re-applies the current texts to the authored card (no objects are created).</summary>
        public void Rebuild()
        {
            ApplyContent();
            if (Application.isPlaying && isActiveAndEnabled && _overlayRoot != null)
            {
                SetOverlayVisible(_showRuntimeOverlay);
            }
        }

        /// <summary>
        ///     Points this component at an authored card. Called by the editor kit at authoring time (and by the
        ///     deprecated runtime fallback); the references are serialized with the scene.
        /// </summary>
        public void AssignOverlay(
            GameObject overlayRoot,
            TMP_Text titleLabel,
            TMP_Text descriptionLabel,
            TMP_Text hintsLabel,
            TMP_Text collapseLabel,
            params GameObject[] collapsibleParts)
        {
            _overlayRoot = overlayRoot;
            _titleLabel = titleLabel;
            _descriptionLabel = descriptionLabel;
            _hintsLabel = hintsLabel;
            _collapseLabel = collapseLabel;
            _collapsibleParts = collapsibleParts ?? Array.Empty<GameObject>();
        }

        /// <summary>Writes title, description and hint texts plus the collapsed state into the authored labels.</summary>
        public void ApplyContent()
        {
            if (_titleLabel != null)
            {
                _titleLabel.text = _moduleName;
            }

            if (_descriptionLabel != null)
            {
                _descriptionLabel.text = _purpose ?? string.Empty;
                _descriptionLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(_purpose));
            }

            if (_hintsLabel != null)
            {
                string hints = ComposeHintsText();
                _hintsLabel.text = hints;
                _hintsLabel.gameObject.SetActive(hints.Length > 0);
            }

            ApplyCollapsedState();
        }

        public void SetCollapsed(bool collapsed)
        {
            _collapsed = collapsed;
            ApplyCollapsedState();
        }

        /// <summary>Collapse button target (wired as a persistent listener by the editor kit).</summary>
        public void ToggleCollapsed()
        {
            SetCollapsed(!_collapsed);
        }

        /// <summary>Shows or hides the authored card.</summary>
        public void SetOverlayVisible(bool visible)
        {
            if (_overlayRoot == null || _overlayRoot.activeSelf == visible)
            {
                return;
            }

            // WHY: toggling an ancestor of this component while it is being enabled throws in Unity.
            if (_overlayRoot == gameObject || transform.IsChildOf(_overlayRoot.transform))
            {
                return;
            }

            _overlayRoot.SetActive(visible);
        }

        /// <summary>Rich-text hint block: labeled Runtime API / Scene workflow / Verification lines, then hints.</summary>
        public string ComposeHintsText()
        {
            StringBuilder builder = new();
            string accent = ColorUtility.ToHtmlStringRGB(_accentColor);
            AppendLabeledHint(builder, accent, "Runtime API", _runtimeApi);
            AppendLabeledHint(builder, accent, "Scene workflow", _sceneWorkflow);
            AppendLabeledHint(builder, accent, "Verification", _verification);
            if (_hints != null)
            {
                foreach (string hint in _hints)
                {
                    if (string.IsNullOrWhiteSpace(hint))
                    {
                        continue;
                    }

                    AppendSeparator(builder);
                    builder.Append("<color=#").Append(accent).Append(">»</color>  ").Append(hint);
                }
            }

            return builder.ToString();
        }

        private void ApplyCollapsedState()
        {
            if (_collapsibleParts != null)
            {
                foreach (GameObject part in _collapsibleParts)
                {
                    if (part != null && part.activeSelf == _collapsed)
                    {
                        part.SetActive(!_collapsed);
                    }
                }
            }

            if (_collapseLabel != null)
            {
                _collapseLabel.text = _collapsed ? "+" : "-";
            }
        }

        private void WarnMissingOverlay()
        {
            if (_warnedMissingOverlay || !_showRuntimeOverlay)
            {
                return;
            }

            _warnedMissingOverlay = true;
            global::Neo.Demo.SampleDiagnostics.Warn(
                "[ModuleDemoSceneInfo] '" + _moduleName + "' has no authored info card. Rebuild the scene with " +
                "Neoxider/Demo/Rebuild Active Demo Scene (or Author Info Card In Active Scene).", this);
        }

        private static void AppendLabeledHint(StringBuilder builder, string accent, string label, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            AppendSeparator(builder);
            builder.Append("<color=#").Append(accent).Append(">»</color>  <b>").Append(label).Append(":</b> ")
                .Append(text);
        }

        private static void AppendSeparator(StringBuilder builder)
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }
        }
    }
}

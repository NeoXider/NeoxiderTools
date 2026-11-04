using UnityEngine;

namespace Neo.Tools.View
{
    /// <summary>
    ///     Syncs mesh emission with a light source.
    ///     Copies intensity and color from a Light in real time.
    /// </summary>
    [NeoDoc("Tools/View/MeshEmission.md")]
    [CreateFromMenu("Neoxider/Tools/View/MeshEmission")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(MeshEmission))]
    public class MeshEmission : MonoBehaviour
    {
        private const string EmissionKW = "_EMISSION";

        // WHY: two property IDs because pipelines differ (Built-in/URP use _EmissionColor, HDRP uses _EmissiveColor)
        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissiveColorID = Shader.PropertyToID("_EmissiveColor");

        [Header("Sync Mode")] [Tooltip("Enable sync with light source")]
        public bool syncWithLight = true;

        [Tooltip("Light source to sync with (can be on another object)")]
        public Light targetLight;

        [Header("Sync Settings")] [Tooltip("Sync intensity")]
        public bool syncIntensity = true;

        [Tooltip("Sync color")] public bool syncColor = true;

        [Tooltip("Intensity multiplier (1.0 = same as light)")] [Range(0f, 10f)]
        public float intensityMultiplier = 0.1f;

        [Header("Color Enhancement")] [Tooltip("Intensity at which color brightening starts")] [Range(0f, 50f)]
        public float whiteThreshold = 10f;

        [Tooltip("Brightening range (intensity from whiteThreshold to whiteThreshold + whiteRange)")] [Range(1f, 50f)]
        public float whiteRange = 10f;

        [Header("Visibility")] [Tooltip("Threshold below which emission is fully disabled")] [Range(0f, 5f)]
        public float emissionCutoff = 0.3f;

        [Header("Diagnostics")] [SerializeField]
        private bool debugLog;

        [SerializeField] private bool _logWarnings;

        private Material _material;
        private Material _originalMaterial;

        private MeshRenderer _meshRenderer;
        private Color _originalEmissionColor;

        // WHY cached: HasProperty, EnableKeyword(string) and the GI flags are native calls (the keyword one also a
        // string lookup); per frame they only need to run when the emission actually changes or switches on/off.
        private bool _hasEmissionColor;
        private bool _hasEmissiveColor;
        private bool _emissionWritten;
        private bool _emissionOn;
        private Color _writtenEmission;

        /// <summary>
        ///     Current emission intensity (read-only).
        /// </summary>
        public float CurrentIntensity { get; private set; }

        /// <summary>
        ///     Current emission color (read-only).
        /// </summary>
        public Color CurrentColor { get; private set; }

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
            {
                NeoDiagnostics.LogError("MeshEmission: No MeshRenderer found on this GameObject!", this);
                enabled = false;
                return;
            }

            _originalMaterial = _meshRenderer.sharedMaterial;
            if (_originalMaterial == null)
            {
                NeoDiagnostics.LogError("MeshEmission: No material assigned to the MeshRenderer!", this);
                enabled = false;
                return;
            }

            _material = new Material(_originalMaterial);
            _meshRenderer.sharedMaterial = _material;
            _material.EnableKeyword(EmissionKW);
            _material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            _emissionOn = true;
            _emissionWritten = false;
            _hasEmissionColor = _material.HasProperty(EmissionColorID);
            _hasEmissiveColor = _material.HasProperty(EmissiveColorID);
            _originalEmissionColor = _hasEmissionColor
                ? _material.GetColor(EmissionColorID)
                : _hasEmissiveColor ? _material.GetColor(EmissiveColorID) : Color.black;
        }

        private void Start()
        {
            CurrentIntensity = 1f;
            CurrentColor = _originalEmissionColor;

            if (syncWithLight && targetLight == null)
            {
                FindAndAttachLight();
            }
        }

        private void Update()
        {
            if (_material == null || !syncWithLight || targetLight == null)
            {
                return;
            }

            float lightIntensity = targetLight.intensity;
            Color lightColor = targetLight.color;

            float targetIntensity = syncIntensity ? lightIntensity * intensityMultiplier : 1f;
            Color finalEmissionColor = syncColor ? lightColor : _originalEmissionColor;

            // WHY: Boost at high intensity for brighter emission
            float finalIntensity = targetIntensity;
            if (targetIntensity > whiteThreshold)
            {
                float whiteFactor = Mathf.Clamp01((targetIntensity - whiteThreshold) / whiteRange);
                finalIntensity = targetIntensity * (1f + whiteFactor);
            }

            ApplyEmission(finalEmissionColor, finalIntensity);

            CurrentIntensity = finalIntensity;
            CurrentColor = finalEmissionColor;
        }

        private void OnDisable()
        {
            ResetToOriginal();
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                if (_meshRenderer != null && _meshRenderer.sharedMaterial == _material)
                {
                    _meshRenderer.sharedMaterial = _originalMaterial;
                }

                if (Application.isPlaying)
                {
                    Destroy(_material);
                }
                else
                {
                    DestroyImmediate(_material);
                }

                _material = null;
            }
        }

        private void OnValidate()
        {
            if (intensityMultiplier < 0f)
            {
                intensityMultiplier = 0f;
            }

            if (whiteThreshold < 0f)
            {
                whiteThreshold = 0f;
            }

            if (whiteRange < 1f)
            {
                whiteRange = 1f;
            }

            if (emissionCutoff < 0f)
            {
                emissionCutoff = 0f;
            }
        }

        /// <summary>
        ///     Writes emission and fully disables it when values are very small.
        /// </summary>
        private void ApplyEmission(Color color, float intensity)
        {
            if (_material == null)
            {
                return;
            }

            if (intensity <= emissionCutoff)
            {
                WriteEmission(Color.black, false, false);
                return;
            }

            WriteEmission(color * intensity, true, false);
        }

        /// <summary>
        ///     Writes the emission color and on/off state to the owned material, skipping what is already there.
        ///     <paramref name="force" /> rewrites everything, for restoring after outside edits.
        /// </summary>
        private void WriteEmission(Color emission, bool emissionOn, bool force)
        {
            bool colorChanged = force || !_emissionWritten || !emission.Equals(_writtenEmission);
            if (colorChanged)
            {
                if (_hasEmissionColor)
                {
                    _material.SetColor(EmissionColorID, emission);
                }

                if (_hasEmissiveColor)
                {
                    _material.SetColor(EmissiveColorID, emission);
                }
            }

            if (force || !_emissionWritten || emissionOn != _emissionOn)
            {
                if (emissionOn)
                {
                    _material.EnableKeyword(EmissionKW);
                }
                else
                {
                    _material.DisableKeyword(EmissionKW);
                }

                _material.globalIlluminationFlags = emissionOn
                    ? MaterialGlobalIlluminationFlags.RealtimeEmissive
                    : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                _emissionOn = emissionOn;
            }

            _writtenEmission = emission;
            _emissionWritten = true;
        }

        /// <summary>
        ///     Restores the owned material to the captured baseline: the source emission color with emission on,
        ///     whatever <see cref="emissionCutoff" /> is.
        /// </summary>
        public void ResetToOriginal()
        {
            if (_material != null)
            {
                // WHY not ApplyEmission(original, 1): with a cutoff of 1 or more that blacked the emission out
                // while CurrentColor still reported the original color.
                WriteEmission(_originalEmissionColor, true, true);
                CurrentIntensity = 1f;
                CurrentColor = _originalEmissionColor;
            }
        }

        /// <summary>
        ///     Finds and assigns a Light on this object or in children.
        /// </summary>
        public void FindAndAttachLight()
        {
            targetLight = GetComponent<Light>();

            if (targetLight == null)
            {
                targetLight = GetComponentInChildren<Light>();
            }

            if (targetLight != null)
            {
                if (debugLog)
                {
                    NeoDiagnostics.Log($"[{gameObject.name}] Found Light: {targetLight.gameObject.name}", this);
                }
            }
            else
            {
                if (_logWarnings)
                {
                    NeoDiagnostics.LogWarning($"[{gameObject.name}] No Light found on self or children!", this);
                }
            }
        }
    }
}

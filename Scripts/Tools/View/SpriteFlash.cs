using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools.View
{
    /// <summary>
    ///     Flashes sprites with a solid colour for a moment: the usual 2D feedback for a hit. For the duration the
    ///     sprites are drawn with a shared flash material; afterwards each gets its own material back.
    /// </summary>
    [NeoDoc("Tools/View/SpriteFlash.md")]
    [CreateFromMenu("Neoxider/Tools/View/SpriteFlash")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(SpriteFlash))]
    public sealed class SpriteFlash : MonoBehaviour
    {
        /// <summary>Shader of the material used when <see cref="FlashMaterial" /> is not assigned.</summary>
        public const string ShaderName = "Neoxider/Sprite Flash";

        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

        private static Material _sharedMaterial;
        private static MaterialPropertyBlock _block;
        private static bool _missingShaderReported;

        [Tooltip("Sprites to flash. Empty = every SpriteRenderer on this object and its children.")]
        [SerializeField]
        private SpriteRenderer[] _renderers = Array.Empty<SpriteRenderer>();

        [Tooltip("Material drawn during the flash. Empty = a shared material with the 'Neoxider/Sprite Flash' shader.")]
        [SerializeField]
        private Material _flashMaterial;

        [Tooltip("Colour of the flash. Alpha scales its strength.")] [SerializeField]
        private Color _color = Color.white;

        [Tooltip("Seconds one flash lasts.")] [Min(0.01f)] [SerializeField]
        private float _duration = 0.1f;

        [Tooltip("On = the flash fades out over Duration. Off = full colour until it ends.")] [SerializeField]
        private bool _fadeOut = true;

        [Tooltip("Advance on unscaled time, so a flash also ends while Time.timeScale is 0.")] [SerializeField]
        private bool _useUnscaledTime;

        [Header("Events")] [SerializeField] private UnityEvent _onFlashStarted = new UnityEvent();
        [SerializeField] private UnityEvent _onFlashFinished = new UnityEvent();

        private SpriteRenderer[] _targets;
        private Material[] _originals;
        private bool[] _hadBlock;
        private Material _activeMaterial;
        private Color _activeColor;
        private float _total;
        private float _remaining;

        /// <summary>Colour used by <see cref="Flash()" />.</summary>
        public Color Color
        {
            get => _color;
            set => _color = value;
        }

        /// <summary>Seconds used by <see cref="Flash()" />.</summary>
        public float Duration
        {
            get => _duration;
            set => _duration = value;
        }

        /// <summary>Whether the flash fades out instead of holding the full colour.</summary>
        public bool FadeOut
        {
            get => _fadeOut;
            set => _fadeOut = value;
        }

        /// <summary>Whether the flash advances on unscaled time.</summary>
        public bool UseUnscaledTime
        {
            get => _useUnscaledTime;
            set => _useUnscaledTime = value;
        }

        /// <summary>Material drawn during the flash; null selects the shared built-in one.</summary>
        public Material FlashMaterial
        {
            get => _flashMaterial;
            set => _flashMaterial = value;
        }

        /// <summary>True between the start of a flash and its end.</summary>
        public bool IsFlashing { get; private set; }

        /// <summary>Raised when a flash starts; a flash restarted while running does not raise it again.</summary>
        public UnityEvent OnFlashStarted => _onFlashStarted;

        /// <summary>Raised when the sprites have their own materials back.</summary>
        public UnityEvent OnFlashFinished => _onFlashFinished;

        private void Update()
        {
            if (IsFlashing)
            {
                Tick(_useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
            }
        }

        private void OnDisable()
        {
            // WHY: a pooled object switched off in the middle of a flash must not come back white.
            Stop();
        }

        /// <summary>Flashes with <see cref="Color" /> for <see cref="Duration" />. Calling it again restarts the flash.</summary>
        public void Flash()
        {
            Flash(_color, _duration);
        }

        /// <summary>Flashes with <see cref="Color" /> for the given number of seconds.</summary>
        public void FlashFor(float seconds)
        {
            Flash(_color, seconds);
        }

        /// <summary>Flashes with a colour for a number of seconds.</summary>
        public void Flash(Color color, float seconds)
        {
            if (!(seconds > 0f) || float.IsInfinity(seconds))
            {
                return;
            }

            Material material = ResolveFlashMaterial();
            if (material == null)
            {
                return;
            }

            ResolveTargets();
            _activeColor = color;
            _total = seconds;
            _remaining = seconds;

            if (!IsFlashing)
            {
                _activeMaterial = material;
                for (int i = 0; i < _targets.Length; i++)
                {
                    SpriteRenderer target = _targets[i];
                    if (target == null)
                    {
                        continue;
                    }

                    _originals[i] = target.sharedMaterial;
                    _hadBlock[i] = target.HasPropertyBlock();
                    target.sharedMaterial = material;
                }

                IsFlashing = true;
                Apply(1f);
                _onFlashStarted.Invoke();
                return;
            }

            Apply(1f);
        }

        /// <summary>Advances a running flash by a time step. Update calls it; tests and manual loops may too.</summary>
        public void Tick(float deltaTime)
        {
            if (!IsFlashing)
            {
                return;
            }

            _remaining -= deltaTime;
            if (!(_remaining > 0f))
            {
                Stop();
                return;
            }

            if (_fadeOut)
            {
                Apply(_remaining / _total);
            }
        }

        /// <summary>Ends a running flash now and gives the sprites their materials back.</summary>
        public void Stop()
        {
            if (!IsFlashing)
            {
                return;
            }

            IsFlashing = false;
            for (int i = 0; i < _targets.Length; i++)
            {
                SpriteRenderer target = _targets[i];
                if (target == null)
                {
                    continue;
                }

                // WHY: a material someone else assigned during the flash is theirs; only our own swap is undone.
                if (target.sharedMaterial == _activeMaterial)
                {
                    target.sharedMaterial = _originals[i];
                }

                if (_hadBlock[i])
                {
                    target.GetPropertyBlock(Block);
                    Block.SetFloat(FlashAmountId, 0f);
                    target.SetPropertyBlock(Block);
                }
                else
                {
                    target.SetPropertyBlock(null);
                }

                _originals[i] = null;
            }

            _activeMaterial = null;
            _onFlashFinished.Invoke();
        }

        private static MaterialPropertyBlock Block => _block ??= new MaterialPropertyBlock();

        private void Apply(float amount)
        {
            for (int i = 0; i < _targets.Length; i++)
            {
                SpriteRenderer target = _targets[i];
                if (target == null)
                {
                    continue;
                }

                target.GetPropertyBlock(Block);
                Block.SetColor(FlashColorId, _activeColor);
                Block.SetFloat(FlashAmountId, amount);
                target.SetPropertyBlock(Block);
            }
        }

        private void ResolveTargets()
        {
            if (_targets != null)
            {
                return;
            }

            _targets = _renderers != null && _renderers.Length > 0
                ? _renderers
                : GetComponentsInChildren<SpriteRenderer>(true);
            _originals = new Material[_targets.Length];
            _hadBlock = new bool[_targets.Length];
        }

        private Material ResolveFlashMaterial()
        {
            if (_flashMaterial != null)
            {
                return _flashMaterial;
            }

            if (_sharedMaterial != null)
            {
                return _sharedMaterial;
            }

            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                // WHY: a build strips a shader no asset references; say so once instead of failing silently.
                if (!_missingShaderReported)
                {
                    _missingShaderReported = true;
                    NeoDiagnostics.LogWarning("[SpriteFlash] Shader '" + ShaderName + "' is not in the build. " +
                                              "Assign Flash Material or add the shader to Always Included Shaders.",
                        this);
                }

                return null;
            }

            _sharedMaterial = new Material(shader)
            {
                name = "SpriteFlash (shared)",
                hideFlags = HideFlags.HideAndDontSave
            };
            return _sharedMaterial;
        }
    }
}

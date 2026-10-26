using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neo.HtmlToUitk.Effects
{
    public enum BackdropRefresh
    {
        Manual,
        OnEnable,
        EveryFrame
    }

    [DisallowMultipleComponent]
    [AddComponentMenu("Neoxider/HTML to UITK/Backdrop Blur Pass")]
    public sealed class BackdropBlurPass : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private string targetElement = string.Empty;
        [SerializeField] private Camera sourceCamera;
        [SerializeField] private Texture sourceTexture;
        [SerializeField, Min(0f)] private float radius = 12f;
        [SerializeField, Range(1, 8)] private int downsample = 4;
        [SerializeField] private BackdropRefresh refresh = BackdropRefresh.OnEnable;
        [SerializeField] private bool preferNative = true;

        private RenderTexture _capture;
        private RenderTexture _output;
        private VisualElement _target;
        private bool _usingNative;
        private Action _restoreNative;
        private bool _ownsBackground;
        private StyleBackground _backgroundImage;
        private StyleBackgroundSize _backgroundSize;

        public bool IsUsingNative => _usingNative;

        public RenderTexture Output => _output;

        public void Configure(UIDocument newDocument, string element, Camera camera, Texture texture)
        {
            Release();
            document = newDocument;
            targetElement = element;
            sourceCamera = camera;
            sourceTexture = texture;
        }

        private void OnEnable()
        {
            if (refresh == BackdropRefresh.OnEnable)
                Refresh();
        }

        private void LateUpdate()
        {
            if (refresh == BackdropRefresh.EveryFrame)
                Refresh();
        }

        private void OnDisable()
        {
            Release();
        }

        public void Refresh()
        {
            VisualElement target = ResolveTarget();
            if (target == null)
                return;
            if (preferNative && BackdropBlur.IsNativeAvailable(document.panelSettings))
            {
                if (!_usingNative) _restoreNative = BackdropBlur.CaptureNative(target);
                if (BackdropBlur.TryApplyNative(target, radius, document.panelSettings))
                {
                    RestoreBackground();
                    ReleaseTexture(ref _capture);
                    ReleaseTexture(ref _output);
                    _usingNative = true;
                    return;
                }
            }

            _restoreNative?.Invoke();
            _restoreNative = null;
            _usingNative = false;
            Texture source = Capture();
            if (source == null)
                return;
            Blur(source);
            if (!_ownsBackground)
            {
                _backgroundImage = target.style.backgroundImage;
                _backgroundSize = target.style.backgroundSize;
                _ownsBackground = true;
            }
            target.style.backgroundImage = Background.FromRenderTexture(_output);
            target.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
        }

        public void Release()
        {
            RestoreBackground();
            _restoreNative?.Invoke();
            _restoreNative = null;
            ReleaseTexture(ref _capture);
            ReleaseTexture(ref _output);
            _target = null;
            _usingNative = false;
        }

        private VisualElement ResolveTarget()
        {
            VisualElement target = document == null || string.IsNullOrEmpty(targetElement) || document.rootVisualElement == null
                ? null : document.rootVisualElement.Q<VisualElement>(targetElement);
            if (!ReferenceEquals(target, _target))
            {
                Release();
                _target = target;
            }
            return _target;
        }

        private void RestoreBackground()
        {
            if (_ownsBackground && _target != null)
            {
                _target.style.backgroundImage = _backgroundImage;
                _target.style.backgroundSize = _backgroundSize;
            }
            _ownsBackground = false;
            _backgroundImage = default;
            _backgroundSize = default;
        }

        private Texture Capture()
        {
            if (sourceTexture != null)
                return sourceTexture;
            if (sourceCamera == null)
                return null;
            int width = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);
            EnsureTexture(ref _capture, width, height);
            RenderTexture previous = sourceCamera.targetTexture;
            try
            {
                sourceCamera.targetTexture = _capture;
                sourceCamera.Render();
            }
            finally
            {
                sourceCamera.targetTexture = previous;
            }
            return _capture;
        }

        private void Blur(Texture source)
        {
            int iterations = BlurPlan.IterationsForRadius(radius, downsample);
            IReadOnlyList<BlurLevel> levels = BlurPlan.Levels(source.width, source.height, downsample, iterations);
            EnsureTexture(ref _output, levels[0].Width, levels[0].Height);
            List<RenderTexture> chain = new List<RenderTexture>(levels.Count);
            RenderTexture previous = RenderTexture.active;
            try
            {
                for (int i = 0; i < levels.Count; i++)
                {
                    RenderTexture level = RenderTexture.GetTemporary(levels[i].Width, levels[i].Height, 0, RenderTextureFormat.ARGB32);
                    chain.Add(level);
                    level.filterMode = FilterMode.Bilinear;
                }

                Graphics.Blit(source, chain[0]);
                for (int i = 1; i < chain.Count; i++)
                    Graphics.Blit(chain[i - 1], chain[i]);
                for (int i = chain.Count - 2; i >= 0; i--)
                    Graphics.Blit(chain[i + 1], chain[i]);
                Graphics.Blit(chain[0], _output);
            }
            finally
            {
                RenderTexture.active = previous;
                for (int i = 0; i < chain.Count; i++)
                    RenderTexture.ReleaseTemporary(chain[i]);
            }
        }

        private static void EnsureTexture(ref RenderTexture texture, int width, int height)
        {
            if (texture != null && texture.width == width && texture.height == height)
                return;
            ReleaseTexture(ref texture);
            texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            texture.Create();
        }

        private static void ReleaseTexture(ref RenderTexture texture)
        {
            if (texture == null)
                return;
            texture.Release();
            if (Application.isPlaying)
                Destroy(texture);
            else
                DestroyImmediate(texture);
            texture = null;
        }
    }
}

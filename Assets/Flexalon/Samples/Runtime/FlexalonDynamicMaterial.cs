using UnityEngine;
using UnityEngine.Rendering;

namespace Flexalon.Samples
{
    // Automatically selects the right material based on render pipeline and provides helpers for setting the color.
    [ExecuteAlways, AddComponentMenu("Flexalon Samples/Flexalon Dynamic Material")]
    public class FlexalonDynamicMaterial : MonoBehaviour
    {
        public Material Standard;
        public Material URP;
        public Material HDRP;

        [SerializeField]
        private Color _color = Color.white;
        public Color Color => _color;

        private MeshRenderer _meshRenderer;

        void OnEnable()
        {
            UpdateMeshRenderer();
            if (_meshRenderer)
            {
#if UNITY_6000_0_OR_NEWER
                var renderPipeline = GraphicsSettings.defaultRenderPipeline;
#else
                var renderPipeline = GraphicsSettings.renderPipelineAsset;
#endif
                if (renderPipeline?.GetType().Name.Contains("HDRenderPipelineAsset") ?? false)
                {
                    _meshRenderer.sharedMaterial = HDRP;
                }
                else if (renderPipeline?.GetType().Name.Contains("UniversalRenderPipelineAsset") ?? false)
                {
                    _meshRenderer.sharedMaterial = URP;
                }
                else
                {
                    _meshRenderer.sharedMaterial = Standard;
                }

                SetColor(_color);
            }
        }

        private string GetColorPropertyName()
        {
            if (_meshRenderer.sharedMaterial.HasProperty("_BaseColor")) // HRDP.Lit / URP.Lit
            {
                return "_BaseColor";
            }
            else if (_meshRenderer.sharedMaterial.HasProperty("_Color")) // Standard
            {
                return "_Color";
            }

            return null;
        }

        public void SetColor(Color color)
        {
            _color = color;
            UpdateMeshRenderer();
            if (_meshRenderer)
            {
                var propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetColor(GetColorPropertyName(), color);
                _meshRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void UpdateMeshRenderer()
        {
            if (_meshRenderer == null)
            {
                _meshRenderer = GetComponent<MeshRenderer>();
            }
        }
    }
}
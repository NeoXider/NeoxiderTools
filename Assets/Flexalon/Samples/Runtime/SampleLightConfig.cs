using UnityEngine;
using UnityEngine.Rendering;

namespace Flexalon.Samples
{
    [ExecuteAlways]
    public class SampleLightConfig : MonoBehaviour
    {
        public float StandardIntensity = 3.14f;
        public float HDRPIntensity = 20000f;

        void Update()
        {
            var light = GetComponent<Light>();
            if (light)
            {
#if UNITY_6000_0_OR_NEWER
                var renderPipeline = GraphicsSettings.defaultRenderPipeline;
#else
                var renderPipeline = GraphicsSettings.renderPipelineAsset;
#endif
                if (renderPipeline?.GetType().Name.Contains("HDRenderPipelineAsset") ?? false)
                {
                    light.intensity =  HDRPIntensity;
                }
                else
                {
                    light.intensity = StandardIntensity;
                }
            }
        }
    }
}
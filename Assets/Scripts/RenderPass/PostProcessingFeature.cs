using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MyWaterSystem
{
    public class PostProcessingFeature : ScriptableRendererFeature
    {
        WakePass wakePass;

        public override void Create()
        {
            // WaterFX Pass
            wakePass = new WakePass {renderPassEvent = RenderPassEvent.BeforeRenderingOpaques};
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(wakePass);
        }
    }
}
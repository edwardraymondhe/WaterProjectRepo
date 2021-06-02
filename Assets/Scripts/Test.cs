using UnityEngine;
using UnityEngine.Rendering;

namespace MyWaterSystem
{
    [ExecuteAlways]
    public class Test : MonoBehaviour
    {
        private void OnEnable()
        {
            Debug.Log("Enable");
            RenderPipelineManager.beginCameraRendering += Log;
        }
        private void OnDisable()
        {
            Debug.Log("Disable");
            RenderPipelineManager.beginCameraRendering -= Log;
        }
        private void Log(ScriptableRenderContext src, Camera cam)
        {
            Debug.Log($"Beginning rendering the camera: {cam.name}");
        }
    }
}
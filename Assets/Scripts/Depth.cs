using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace MyWaterSystem
{
    public class Depth : MonoBehaviour
    {
        [SerializeField] public RenderTexture _depthTex;
        private Camera _depthCam;
        public float visibility;
        public Texture2D rampTexture;
        
        public Gradient absorptionRamp;
        public Gradient scatterRamp;
        public AnimationCurve foam;
        public Texture2D foamRamp;
        
        private static readonly int shader_WaterDepthMap = Shader.PropertyToID("_WaterDepthMap");
        
        private static readonly int shader_DepthCamParams = Shader.PropertyToID("_WaterDepthCamParams");
        private static readonly int shader_ScatteringRamp = Shader.PropertyToID("_ScatteringRamp");
        public void CaptureDepthMap()
        {
            //Generate the camera
            if (_depthCam == null)
            {
                GameObject go =
                    new GameObject("depthCamera") {hideFlags = HideFlags.HideAndDontSave}; //create the cameraObject
                _depthCam = go.AddComponent<Camera>();
            }

            if (_depthCam.TryGetComponent<UniversalAdditionalCameraData>(
                out UniversalAdditionalCameraData additionalCamData))
            {
                additionalCamData.renderShadows = false;
                additionalCamData.requiresColorOption = CameraOverrideOption.Off;
                additionalCamData.requiresDepthOption = CameraOverrideOption.Off;
            }

            Transform t = _depthCam.transform;
            float depthExtra = 4.0f;
            t.position =
                Vector3.up * (transform.position.y + depthExtra); //center the camera on this water plane height
            t.up = Vector3.forward; //face the camera down

            _depthCam.enabled = true;
            _depthCam.orthographic = true;
            _depthCam.orthographicSize = 250; //hardcoded = 1k area - TODO
            _depthCam.nearClipPlane = 0.01f;
            _depthCam.farClipPlane = visibility + depthExtra;
            _depthCam.allowHDR = false;
            _depthCam.allowMSAA = false;
            _depthCam.cullingMask = (1 << 10);
            //Generate RT
            if (!_depthTex)
                _depthTex = new RenderTexture(1024, 1024, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);

            _depthTex.filterMode = FilterMode.Point;
            _depthTex.wrapMode = TextureWrapMode.Clamp;
            _depthTex.name = "WaterDepthMap";
            //do depth capture
            _depthCam.targetTexture = _depthTex;
            _depthCam.Render();
            Shader.SetGlobalTexture(shader_WaterDepthMap, _depthTex);
            // set depth bufferParams for depth cam(since it doesnt exist and only temporary)
            Vector4 _params = new Vector4(t.position.y, 250, 0, 0);
            Shader.SetGlobalVector(shader_DepthCamParams, _params);

            _depthCam.enabled = false;
            _depthCam.targetTexture = null;
        }
        
        public void CaptureColorRamp()
        {
            if (rampTexture == null)
                rampTexture = new Texture2D(128, 4, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None);
            rampTexture.wrapMode = TextureWrapMode.Clamp;

            var cols = new Color[512];
            for (var i = 0; i < 128; i++)
            {
                cols[i] = absorptionRamp.Evaluate(i / 128f);
            }

            for (var i = 0; i < 128; i++)
            {
                cols[i + 128] = scatterRamp.Evaluate(i / 128f);
            }

            for (var i = 0; i < 128; i++)
            {
                cols[i + 256] = foamRamp.GetPixelBilinear(foam.Evaluate(i / 128f),
                    0.5f);
            }

            rampTexture.SetPixels(cols);
            rampTexture.Apply();
            Shader.SetGlobalTexture(shader_ScatteringRamp, rampTexture);
        }
    }
}
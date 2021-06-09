using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace MyWaterSystem
{
    [ExecuteAlways]
    public class Reflection : MonoBehaviour
    {
        public float scaleValue;
        public float clipPlaneOffset = 0.07f;
        public LayerMask reflectLayers = -1;
        
        private static Camera reflectionCamera;
        public RenderTexture reflectionTexture;
        private static readonly int shader_planarReflectionTexture = Shader.PropertyToID("_PlanarReflectionTexture");


        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += BeginReflectionRendering;
        }

        // Cleanup all the objects we possibly have created
        private void OnDisable()
        {
            Cleanup();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            RenderPipelineManager.beginCameraRendering -= BeginReflectionRendering;

            if (reflectionCamera)
            {
                reflectionCamera.targetTexture = null;
                SafeDestroy(reflectionCamera.gameObject);
            }

            if (reflectionTexture)
            {
                RenderTexture.ReleaseTemporary(reflectionTexture);
            }
        }

        private void BeginReflectionRendering(ScriptableRenderContext src, Camera cam)
        {
            // we dont want to render planar reflections in reflections or previews
            if (cam.cameraType == CameraType.Reflection || cam.cameraType == CameraType.Preview)
                return;

            UpdateReflectionCamera(cam); // create reflected camera
            PlanarReflectionTexture(cam); // create and assign RenderTexture
           
            bool tmp_fog = RenderSettings.fog;
            int tmp_maxLod = QualitySettings.maximumLODLevel;
            float tmp_lodBias = QualitySettings.lodBias;
            
            GL.invertCulling = true;
            RenderSettings.fog = false;
            QualitySettings.maximumLODLevel = 1;
            QualitySettings.lodBias = tmp_lodBias * 0.5f;
            
            UniversalRenderPipeline.RenderSingleCamera(src, reflectionCamera); // render planar reflections

            GL.invertCulling = false;
            RenderSettings.fog = tmp_fog;
            QualitySettings.maximumLODLevel = tmp_maxLod;
            QualitySettings.lodBias = tmp_lodBias;
            
            Shader.SetGlobalTexture(shader_planarReflectionTexture,
                reflectionTexture); // Assign texture to water shader
        }

        private static void SafeDestroy(Object o)
        {
            if (Application.isPlaying)
                Destroy(o);
            else
                DestroyImmediate(o);
        }

        private void UpdateReflectionCamera(Camera realCamera)
        {
            if (reflectionCamera == null)
                reflectionCamera = CreateMirror();

            // find out the reflection plane: position and normal in world space
            Vector3 pos = Vector3.zero;
            Vector3 normal = Vector3.up;

            reflectionCamera.CopyFrom(realCamera);
            reflectionCamera.useOcclusionCulling = false;
            if (reflectionCamera.gameObject.TryGetComponent(out UniversalAdditionalCameraData camData))
            {
                camData.renderShadows = false;
            }

            // Render reflection
            // Reflect camera around reflection plane
            float d = -Vector3.Dot(normal, pos) - clipPlaneOffset;
            Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);

            Matrix4x4 reflection = Matrix4x4.identity;
            reflection *= Matrix4x4.Scale(new Vector3(1, -1, 1));

            CalculateReflectionMatrix(ref reflection, reflectionPlane);
            Vector3 oldPosition = realCamera.transform.position - new Vector3(0, pos.y * 2, 0);
            Vector3 newPosition = ReflectPosition(oldPosition);
            reflectionCamera.transform.forward = Vector3.Scale(realCamera.transform.forward, new Vector3(1, -1, 1));
            reflectionCamera.worldToCameraMatrix = realCamera.worldToCameraMatrix * reflection;

            // Setup oblique projection matrix so that near plane is our reflection
            // plane. This way we clip everything below/above it for free.
            
            Vector4 clipPlane = CameraSpacePlane(reflectionCamera, pos - Vector3.up * 0.1f, normal, 1.0f);
            Matrix4x4 projection = realCamera.CalculateObliqueMatrix(clipPlane);
            reflectionCamera.projectionMatrix = projection;
            reflectionCamera.cullingMask = reflectLayers; // never render water layer
            reflectionCamera.transform.position = newPosition;
        }

        private Camera CreateMirror()
        {
            GameObject go = new GameObject("Planar Reflections", typeof(Camera));
            UniversalAdditionalCameraData cameraData =
                go.AddComponent(typeof(UniversalAdditionalCameraData)) as UniversalAdditionalCameraData;

            cameraData.requiresColorOption = CameraOverrideOption.Off;
            cameraData.requiresDepthOption = CameraOverrideOption.Off;
            cameraData.SetRenderer(1);

            Transform t = transform;
            Camera reflectionCamera = go.GetComponent<Camera>();
            reflectionCamera.transform.SetPositionAndRotation(t.position, t.rotation);
            reflectionCamera.depth = -10;
            reflectionCamera.enabled = false;
            go.hideFlags = HideFlags.HideAndDontSave;

            return reflectionCamera;
        }

        private void PlanarReflectionTexture(Camera cam)
        {
            if (reflectionTexture == null)
            {
                int2 res = ReflectionResolution(cam, UniversalRenderPipeline.asset.renderScale);
                const RenderTextureFormat hdrFormat = RenderTextureFormat.RGB111110Float;
                reflectionTexture = RenderTexture.GetTemporary(res.x, res.y, 16,
                    GraphicsFormatUtility.GetGraphicsFormat(hdrFormat, true));
            }

            reflectionCamera.targetTexture = reflectionTexture;
        }

        private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
        {
            Vector3 offsetPos = pos + normal * clipPlaneOffset;
            Matrix4x4 m = cam.worldToCameraMatrix;
            Vector3 cameraPosition = m.MultiplyPoint(offsetPos);
            Vector3 cameraNormal = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cameraNormal.x, cameraNormal.y, cameraNormal.z, -Vector3.Dot(cameraPosition, cameraNormal));
        }
        
        private static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
        {
            reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
            reflectionMat.m01 = (-2F * plane[0] * plane[1]);
            reflectionMat.m02 = (-2F * plane[0] * plane[2]);
            reflectionMat.m03 = (-2F * plane[3] * plane[0]);

            reflectionMat.m10 = (-2F * plane[1] * plane[0]);
            reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
            reflectionMat.m12 = (-2F * plane[1] * plane[2]);
            reflectionMat.m13 = (-2F * plane[3] * plane[1]);

            reflectionMat.m20 = (-2F * plane[2] * plane[0]);
            reflectionMat.m21 = (-2F * plane[2] * plane[1]);
            reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
            reflectionMat.m23 = (-2F * plane[3] * plane[2]);

            reflectionMat.m30 = 0F;
            reflectionMat.m31 = 0F;
            reflectionMat.m32 = 0F;
            reflectionMat.m33 = 1F;
        }
        
        private int2 ReflectionResolution(Camera cam, float scale)
        {
            int x = (int)(cam.pixelWidth * scale * scaleValue);
            int y = (int)(cam.pixelHeight * scale * scaleValue);
            return new int2(x, y);
        }
        
        private static Vector3 ReflectPosition(Vector3 pos)
        {
            Vector3 newPos = new Vector3(pos.x, -pos.y, pos.z);
            return newPos;
        }
    }
}
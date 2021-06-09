using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using MyWaterSystem.Data;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MyWaterSystem
{
    [ExecuteAlways]
    public class Water : MonoBehaviour
    {
        // Singleton
        private static Water _instance;

        public static Water Instance
        {
            get
            {
                if (_instance == null)
                    _instance = (Water) FindObjectOfType(typeof(Water));
                return _instance;
            }
        }

        [SerializeField] public Wave[] _waves;
        private float _maxWaveHeight;
        private float _waveHeight;


        [SerializeField] public WaterWaveData waveData;
        [SerializeField] private WaterResourcesData resources;


        private Reflection reflection;
        private Depth depth;
        public float depth_visibility;
        
        public Gradient depth_absorptionRamp;
        public Gradient depth_scatterRamp;
        
        public float reflection_scaleValue = 0.33f;
        public float reflection_clipPlaneOffset = 0.24f;
        public LayerMask reflection_reflectLayers = -1;
        
        
        
        
        private static readonly int shader_WaveHeight = Shader.PropertyToID("_WaveHeight");
        private static readonly int shader_MaxWaveHeight = Shader.PropertyToID("_MaxWaveHeight");
        private static readonly int shader_WaveCount = Shader.PropertyToID("_WaveCount");
        private static readonly int shader_WaveData = Shader.PropertyToID("waveData");
        private static readonly int shader_Visibility = Shader.PropertyToID("_Visibility");
        

        private void OnEnable()
        {
            Init();

            RenderPipelineManager.beginCameraRendering += BeginCameraRendering;

            EnableReflection();
            
            Invoke("EnableDepth", 1.0f);
            
            if (resources == null)
            {
                resources = Resources.Load("WaterResoucesData") as WaterResourcesData;
            }

            
        }

        private void EnableReflection()
        {
            if (!gameObject.TryGetComponent(out reflection))
            {
                reflection = gameObject.AddComponent<Reflection>();
            }

            reflection.hideFlags = HideFlags.HideAndDontSave;
            reflection.reflectLayers = reflection_reflectLayers;
            reflection.scaleValue = reflection_scaleValue;
            reflection.clipPlaneOffset = reflection_clipPlaneOffset;
            reflection.enabled = true;
        }

        private void EnableDepth()
        {
            if (!gameObject.TryGetComponent(out depth))
            {
                depth = gameObject.AddComponent<Depth>();
            }

            depth.enabled = true;
            reflection.hideFlags = HideFlags.HideAndDontSave;
            depth.visibility = depth_visibility;
            depth.scatterRamp = depth_scatterRamp;
            depth.absorptionRamp = depth_absorptionRamp;
            depth.foam = new AnimationCurve(new Keyframe[2]{new Keyframe(0.25f, 0f),
                new Keyframe(1f, 1f)});
            depth.foamRamp = resources.defaultFoamRamp;
            depth.CaptureDepthMap();
            depth.CaptureColorRamp();
        }
        
        private void OnDisable()
        {
            Cleanup();
        }

        void Cleanup()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
        }

        private void BeginCameraRendering(ScriptableRenderContext src, Camera cam)
        {
            Debug.Log($"Beginning rendering the camera: {cam.name}");
            if (cam.cameraType == CameraType.Preview) return;

            float roll = cam.transform.localEulerAngles.z;
            // Water matrix
            const float quantizeValue = 6.25f;
            const float forwards = 10f;
            const float yOffset = -0.25f;

            Vector3 newPos = cam.transform.TransformPoint(Vector3.forward * forwards);
            newPos.y = yOffset;
            newPos.x = quantizeValue * (int) (newPos.x / quantizeValue);
            newPos.z = quantizeValue * (int) (newPos.z / quantizeValue);

            Matrix4x4 matrix =
                Matrix4x4.TRS(newPos + transform.position, Quaternion.identity,
                    Vector3.one); // transform.localToWorldMatrix;

            foreach (Mesh mesh in resources.defaultWaterMeshes)
            {
                Graphics.DrawMesh(mesh,
                    matrix,
                    resources.defaultSeaMaterial,
                    gameObject.layer,
                    cam,
                    0,
                    null,
                    ShadowCastingMode.Off,
                    true,
                    null,
                    LightProbeUsage.Off,
                    null);
            }
        }

        private static void SafeDestroy(Object o)
        {
            if (Application.isPlaying)
                Destroy(o);
            else
                DestroyImmediate(o);
        }

        public void Init()
        {
            SetWaves();
            if (resources == null)
            {
                resources = Resources.Load("WaterResourcesData") as WaterResourcesData;
            }
            
        }

        private void LateUpdate()
        {
        }

        private void SetWaves()
        {
            SetupWaves();

            _maxWaveHeight = 0f;
            foreach (Wave w in _waves)
            {
                _maxWaveHeight += w.amplitude;
            }

            _maxWaveHeight /= _waves.Length;

            _waveHeight = transform.position.y;

            Shader.SetGlobalInt(shader_WaveCount, _waves.Length);
            Shader.SetGlobalFloat(shader_WaveHeight, _waveHeight);
            Shader.SetGlobalFloat(shader_MaxWaveHeight, _maxWaveHeight);
            Shader.SetGlobalFloat(shader_Visibility,depth_visibility);
            Shader.SetGlobalVectorArray(shader_WaveData, GetWaveData());
        }

        private Vector4[] GetWaveData()
        {
            Vector4[] waveData = new Vector4[20];
            for (int i = 0; i < _waves.Length; i++)
            {
                waveData[i] = new Vector4(_waves[i].amplitude, _waves[i].direction, _waves[i].wavelength, 0);
            }

            return waveData;
        }

        private void SetupWaves()
        {
            //create basic waves based off basic wave settings
            Random.State backupSeed = Random.state;
            Random.InitState(waveData.randomSeed);
            BasicWaves basicWaves = waveData._basicWaveSettings;
            float a = basicWaves.amplitude;
            float d = basicWaves.direction;
            float l = basicWaves.wavelength;
            int numWave = basicWaves.numWaves;
            _waves = new Wave[numWave];

            float r = 1f / numWave;

            for (int i = 0; i < numWave; i++)
            {
                float p = Mathf.Lerp(0.5f, 1.5f, i * r);
                float amp = a * p * Random.Range(0.8f, 1.2f);
                float dir = d + Random.Range(-90f, 90f);
                float len = l * p * Random.Range(0.6f, 1.4f);
                _waves[i] = new Wave(amp, dir, len);
                Random.InitState(waveData.randomSeed + i + 1);
            }

            Random.state = backupSeed;
        }
        
       
    }
}
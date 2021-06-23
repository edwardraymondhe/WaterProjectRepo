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
using System.Collections;

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

        public Wave[] _waves;
        private float _maxWaveHeight;
        private float _waveHeight;


        public WaterWaveData waveData;
        [SerializeField] private WaterResourcesData resources;


        private Reflection reflection;
        private Depth depth;
        public float depth_visibility;
        
        public Gradient depth_absorptionRamp;
        public Gradient depth_scatterRamp;
        
        public Gradient depth_absorptionRamp_backup;
        public Gradient depth_scatterRamp_backup;
        
        public float reflection_scaleValue = 0.33f;
        public float reflection_clipPlaneOffset = 0.24f;
        public LayerMask reflection_reflectLayers = -1;

        private static readonly int shader_WaveHeight = Shader.PropertyToID("_WaveHeight");
        private static readonly int shader_MaxWaveHeight = Shader.PropertyToID("_MaxWaveHeight");
        private static readonly int shader_WaveCount = Shader.PropertyToID("_WaveCount");
        private static readonly int shader_WaveData = Shader.PropertyToID("waveData");
        private static readonly int shader_Visibility = Shader.PropertyToID("_Visibility");
        
        private static readonly int shader_FoamMap = Shader.PropertyToID("_FoamMap");
        private static readonly int shader_SurfaceMap = Shader.PropertyToID("_SurfaceMap");
        

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
            
            Shader.SetGlobalTexture(shader_FoamMap, resources.defaultFoamMap);
            Shader.SetGlobalTexture(shader_SurfaceMap, resources.defaultSurfaceMap);
        }

        private void LateUpdate()
        {
        }

        private void SetWaves()
        {
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


        public void SetBasicWaveDataWaveNum(Slider slider)
        {
            waveData.SetWaveNum((int)slider.value);
            ApplyWaveChanges();
        }
        public void SetBasicWaveDataAmp(Slider slider)
        {
            waveData.SetAmp(slider.value);
            ApplyWaveChanges();
        }
        public void SetBasicWaveDataDir(Slider slider)
        {
            waveData.SetDir(slider.value);
            ApplyWaveChanges();
        }
        public void SetBasicWaveDataLen(Slider slider)
        {
            waveData.SetLen(slider.value);
            ApplyWaveChanges();
        }

        public void ApplyWaveChanges()
        {
            SetWaves();
        }

        public Vector3 GetWaves(Vector3 position)
        {
            Vector2 pos = new Vector2(position.x, position.y);
            int waveNum = _waves.Length;

            Vector3 waveOut = Vector3.zero;

            for (int i = 0; i < waveNum; i++)
            {
                float amplitude = _waves[i].amplitude;
                float direction = _waves[i].direction;
                float waveLength = _waves[i].wavelength;

                float time = Time.time;

                Vector3 wave = Vector3.zero; // wave vector
                float w = 6.28318f / waveLength; // 2pi over wavelength(hardcoded)
                float wSpeed = Mathf.Sqrt(9.8f * w); // frequency of the wave based off wavelength
                float peak = 0.8f; // peak value, 1 is the sharpest peaks
                float qi = peak / (amplitude * w * waveNum);

                direction = (float)(Math.PI / 180.0f * direction); // convert the incoming degrees to radians, for directional waves
                Vector2 dirWaveInput = new Vector2(Mathf.Sin(direction), Mathf.Cos(direction));

                Vector2 windDir = dirWaveInput.normalized; // calculate wind direction
                float dir = Vector2.Dot(windDir, pos); // calculate a gradient along the wind direction

                ////////////////////////////position output calculations/////////////////////////
                float calc = dir * w + -time * wSpeed; // the wave calculation
                float cosCalc = Mathf.Cos(calc); // cosine version(used for horizontal undulation)
                float sinCalc = Mathf.Sin(calc); // sin version(used for vertical undulation)

                // calculate the offsets for the current point
                wave.x = qi * amplitude * windDir.x * cosCalc;
                wave.z = qi * amplitude * windDir.y * cosCalc;
                wave.y = ((sinCalc * amplitude)) / waveNum; // the height is divided by the number of waves


                float Amp = amplitude * 10000;
                if (Amp >= 1)
                    Amp = 1;
                else if (Amp <= 0)
                    Amp = 0;

                waveOut += wave * Amp;
            }

            return waveOut + position;
        }

        public float GetWaveHeight(Vector3 position)
        {
            ArrayList waves = new ArrayList();
            for (float x = position.x - 2.0f; x < position.x + 2.0f; x += 0.1f)
            {
                for (float z = position.y - 2.0f; x < position.y + 2.0f; z += 0.1f)
                {
                    waves.Add(GetWaves(new Vector3(x, z, position.z)));
                }
            }

            float minDis = 9999f;
            Vector3 nearest = new Vector3();

            foreach (Vector3 pos in waves)
            {
                float dis = (pos - position).magnitude;
                if (dis < minDis)
                {
                    minDis = dis;
                    nearest = pos;
                }
            }

            return nearest.y;
        }
    }
}
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace MyWaterSystem.Data
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "WaterWaveData", menuName = "WaterSystem/WaterWaveData", order = 0)]
    public class WaterWaveData : ScriptableObject
    {
        public List<Wave> _waves = new List<Wave>();
        public int randomSeed = 3234;
        public BasicWaves _basicWaveSettings = new BasicWaves(1.5f, 45.0f, 5.0f);
        [SerializeField]
        public bool _init = false;
    }

    [System.Serializable]
    public struct Wave
    {
        public float amplitude; // height of the wave in units(m)
        public float direction; // direction the wave travels in degrees from Z+
        public float wavelength; // distance between crest>crest

        public Wave(float amp, float dir, float length)
        {
            amplitude = amp;
            direction = dir;
            wavelength = length;
        }
    }

    [System.Serializable]
    public class BasicWaves
    {
        public int numWaves = 6;
        public float amplitude;
        public float direction;
        public float wavelength;

        public BasicWaves(float amp, float dir, float len)
        {
            numWaves = 6;
            amplitude = amp;
            direction = dir;
            wavelength = len;
        }
    }
}
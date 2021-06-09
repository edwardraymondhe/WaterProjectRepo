using UnityEngine;

namespace MyWaterSystem.Data
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "WaterResoucesData", menuName = "WaterSystem/WaterResoucesData", order = 0)]
    public class WaterResourcesData : ScriptableObject
    {
        public Texture2D defaultFoamRamp; // a default foam ramp for the basic foam setting
        public Texture2D defaultFoamMap; // a default foam texture map
        public Texture2D defaultSurfaceMap; // a default normal/caustic map
        public Material defaultSeaMaterial;
        public Mesh[] defaultWaterMeshes;
    }
}
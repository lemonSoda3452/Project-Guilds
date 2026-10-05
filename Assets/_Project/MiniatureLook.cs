using UnityEngine;

namespace HeroManagement
{
    // The "toy diorama" mood of the Link's Awakening remake: warm flat ambient light, soft shadows
    // and a light haze in the distance that makes everything look like a miniature.
    // For the blurred top and bottom of the image (tilt-shift) add a Depth of Field override to a
    // Global Volume (URP/HDRP) or the Post Processing package, with the focus on the tavern.
    public class MiniatureLook : MonoBehaviour
    {
        [Header("Ambient")]
        public Color ambientColor = new Color(0.8f, 0.76f, 0.84f);

        [Header("Haze")]
        public bool useFog = true;
        public Color fogColor = new Color(0.78f, 0.88f, 0.96f);
        public float fogStart = 24f;
        public float fogEnd = 70f;

        [Header("Sun")]
        public Light sun;
        public Color sunColor = new Color(1f, 0.95f, 0.86f);
        public float sunIntensity = 1.1f;
        [Range(0f, 1f)] public float shadowStrength = 0.55f;

        void Awake()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;

            RenderSettings.fog = useFog;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;

            if (sun == null) sun = RenderSettings.sun;
            if (sun != null)
            {
                sun.color = sunColor;
                sun.intensity = sunIntensity;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = shadowStrength;
            }
        }
    }
}

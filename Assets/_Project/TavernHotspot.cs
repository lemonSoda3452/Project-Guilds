using UnityEngine;
using UnityEngine.Events;

namespace HeroManagement
{
    // A clickable object of the 3D tavern (notice board, counter, forge...).
    // Clicking it moves the camera to its shot and opens the matching UI panels.
    [RequireComponent(typeof(Collider))]
    public class TavernHotspot : MonoBehaviour
    {
        public string hotspotName = "Hotspot";
        [Tooltip("Name of the camera shot in the TavernCameraRig")]
        public string cameraAnchor = "Overview";
        [Tooltip("UI panels shown while the camera is here")]
        public GameObject[] panelsToShow;
        [Tooltip("Called when the player focuses this spot (use it to refresh the panels)")]
        public UnityEvent onFocus = new UnityEvent();
        public Color highlightColor = new Color(1f, 0.85f, 0.3f);

        private Renderer[] renderers;
        private Color[] baseColors;
        private bool hovered = false;

        void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            baseColors = new Color[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                // .material creates a per-object copy, so highlighting one hotspot never affects the others
                baseColors[i] = renderers[i].material.color;
            }
        }

        public void SetHover(bool on)
        {
            if (renderers == null || hovered == on) return;
            hovered = on;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].material.color = on ? Color.Lerp(baseColors[i], highlightColor, 0.55f) : baseColors[i];
            }
        }
    }
}

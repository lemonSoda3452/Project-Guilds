using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HeroManagement
{
    // Links the 3D tavern to the UI: hover and click on hotspots, move the camera, show the right panels.
    // ESC or the Back button returns to the overview shot.
    public class Tavern3DController : MonoBehaviour
    {
        public TavernCameraRig rig;
        public Camera cam;
        public List<TavernHotspot> hotspots = new List<TavernHotspot>();

        [Tooltip("Every panel that must be hidden in the overview")]
        public GameObject[] allFocusPanels;
        public ManagementMenuUIManager menu;
        [Tooltip("Shown only while a hotspot is focused")]
        public GameObject backButton;

        private TavernHotspot hovered;
        private TavernHotspot focused;

        void Start()
        {
            if (cam == null) cam = Camera.main;
            ReturnToOverview();
        }

        void Update()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            if (PointerInput.EscapePressed())
            {
                ReturnToOverview();
                return;
            }

            TavernHotspot hit = null;

            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (!overUI)
            {
                Ray ray = cam.ScreenPointToRay(PointerInput.Position());
                RaycastHit info;
                if (Physics.Raycast(ray, out info, 100f))
                {
                    hit = info.collider.GetComponentInParent<TavernHotspot>();
                }
            }

            if (hit != hovered)
            {
                if (hovered != null) hovered.SetHover(false);
                hovered = hit;
                if (hovered != null) hovered.SetHover(true);
            }

            if (hit != null && PointerInput.ClickedThisFrame()) Focus(hit);
        }

        public void Focus(TavernHotspot hotspot)
        {
            if (hotspot == null) return;

            CloseAllPanels();
            focused = hotspot;

            if (rig != null) rig.FocusOn(hotspot.cameraAnchor);

            if (hotspot.panelsToShow != null)
            {
                foreach (var panel in hotspot.panelsToShow)
                {
                    if (panel != null) panel.SetActive(true);
                }
            }

            if (backButton != null) backButton.SetActive(true);

            // Panels are active now, so their Refresh() calls actually draw
            hotspot.onFocus.Invoke();
        }

        public void ReturnToOverview()
        {
            focused = null;
            CloseAllPanels();
            if (rig != null) rig.ReturnToOverview();
            if (backButton != null) backButton.SetActive(false);
        }

        public void OnBackClicked() { ReturnToOverview(); }

        private void CloseAllPanels()
        {
            if (allFocusPanels != null)
            {
                foreach (var p in allFocusPanels)
                {
                    if (p != null) p.SetActive(false);
                }
            }

            if (menu != null) menu.CloseAll();
        }
    }
}

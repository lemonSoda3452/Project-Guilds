using System.Collections.Generic;
using UnityEngine;

namespace HeroManagement
{
    // Diorama camera (Link's Awakening remake mood): it never roams freely, it glides between fixed,
    // slightly tilted shots of the tavern, like a hand moving a toy camera over a miniature
    public class TavernCameraRig : MonoBehaviour
    {
        [System.Serializable]
        public class CameraAnchor
        {
            public string anchorName;
            public Transform point;
            public float fieldOfView = 60f;
        }

        public Camera cam;
        public List<CameraAnchor> anchors = new List<CameraAnchor>();
        public string overviewAnchor = "Overview";
        [Tooltip("Higher = the camera arrives faster")]
        [Range(1f, 20f)] public float moveSpeed = 5f;

        private CameraAnchor target;

        void Start()
        {
            if (cam == null) cam = Camera.main;
            JumpTo(overviewAnchor);
        }

        private CameraAnchor Find(string anchorName)
        {
            return anchors.Find(a => a != null && a.anchorName == anchorName);
        }

        public void FocusOn(string anchorName)
        {
            var anchor = Find(anchorName);
            if (anchor != null && anchor.point != null) target = anchor;
        }

        public void ReturnToOverview() { FocusOn(overviewAnchor); }

        // Instant cut (used at the start)
        public void JumpTo(string anchorName)
        {
            var anchor = Find(anchorName);
            if (anchor == null || anchor.point == null || cam == null) return;

            target = anchor;
            cam.transform.SetPositionAndRotation(anchor.point.position, anchor.point.rotation);
            cam.fieldOfView = anchor.fieldOfView;
        }

        void LateUpdate()
        {
            if (cam == null || target == null || target.point == null) return;

            float t = 1f - Mathf.Exp(-moveSpeed * Time.unscaledDeltaTime);
            cam.transform.position = Vector3.Lerp(cam.transform.position, target.point.position, t);
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, target.point.rotation, t);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, target.fieldOfView, t);
        }
    }
}

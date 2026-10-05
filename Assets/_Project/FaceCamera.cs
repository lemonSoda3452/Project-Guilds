using UnityEngine;

namespace HeroManagement
{
    // Keeps floating labels (names above characters) facing the camera
    public class FaceCamera : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }
}

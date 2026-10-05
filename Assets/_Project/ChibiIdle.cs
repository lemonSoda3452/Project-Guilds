using UnityEngine;

namespace HeroManagement
{
    // A little toy-like bounce so characters never stand perfectly still
    public class ChibiIdle : MonoBehaviour
    {
        public Transform visual;
        public float bobHeight = 0.05f;
        public float bobSpeed = 3f;

        private float phase;

        void Start() { phase = Random.value * 6.28f; }

        void Update()
        {
            if (visual == null) return;

            float t = Time.time * bobSpeed + phase;
            visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(t)) * bobHeight, 0f);

            // tiny squash and stretch
            float squash = Mathf.Sin(t * 2f) * 0.025f;
            visual.localScale = new Vector3(1f + squash, 1f - squash, 1f + squash);
        }
    }
}

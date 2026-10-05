using UnityEngine;
using UnityEngine.UI;

namespace HeroManagement
{
    // A simple radar (spider) chart drawn as a UI mesh. Used for hero and party stats.
    public class RadarChart : MaskableGraphic
    {
        public float maxValue = 20f;
        public float[] values = new float[HeroStats.Count];
        [Range(0.3f, 1f)] public float radiusScale = 0.8f;
        public bool outlineOnly = false;
        public float outlineWidth = 3f;

        public void SetValues(float[] newValues, float newMax)
        {
            values = newValues;
            maxValue = newMax;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (values == null || values.Length < 3) return;

            int n = values.Length;
            Rect rect = GetPixelAdjustedRect();
            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f * radiusScale;

            Vector2[] points = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float t = maxValue > 0f ? Mathf.Clamp01(values[i] / maxValue) : 0f;
                float angle = Mathf.PI / 2f - i * 2f * Mathf.PI / n; // first axis points up, then clockwise
                points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * t;
            }

            if (!outlineOnly)
            {
                vh.AddVert(center, color, Vector2.zero);
                for (int i = 0; i < n; i++) vh.AddVert(points[i], color, Vector2.zero);
                for (int i = 0; i < n; i++) vh.AddTriangle(0, i + 1, (i + 1) % n + 1);
            }
            else
            {
                for (int i = 0; i < n; i++) AddLine(vh, points[i], points[(i + 1) % n], outlineWidth);
            }
        }

        private void AddLine(VertexHelper vh, Vector2 a, Vector2 b, float width)
        {
            Vector2 dir = (b - a);
            if (dir.sqrMagnitude < 0.0001f) return;
            dir.Normalize();
            Vector2 normal = new Vector2(-dir.y, dir.x) * (width * 0.5f);

            int start = vh.currentVertCount;
            vh.AddVert(a - normal, color, Vector2.zero);
            vh.AddVert(a + normal, color, Vector2.zero);
            vh.AddVert(b + normal, color, Vector2.zero);
            vh.AddVert(b - normal, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}

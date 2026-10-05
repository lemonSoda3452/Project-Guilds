using UnityEngine;
using UnityEngine.UI;

namespace HeroManagement
{
    // The flat shapes of a Persona-style menu: slanted slabs, diamonds and wedges
    public class PersonaShape : MaskableGraphic
    {
        public enum ShapeType { Slab, Diamond, Wedge }

        public ShapeType shape = ShapeType.Slab;
        [Tooltip("Degrees of skew for slabs (24 = the classic slanted look)")]
        public float shearDegrees = 24f;

        public void SetShape(ShapeType newShape, float shear)
        {
            shape = newShape;
            shearDegrees = shear;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect r = GetPixelAdjustedRect();
            Vector2[] p;

            switch (shape)
            {
                case ShapeType.Diamond:
                    p = new[]
                    {
                        new Vector2(r.center.x, r.yMax), new Vector2(r.xMax, r.center.y),
                        new Vector2(r.center.x, r.yMin), new Vector2(r.xMin, r.center.y)
                    };
                    break;

                case ShapeType.Wedge: // a long thin triangle pointing right
                    p = new[] { new Vector2(r.xMin, r.yMax), new Vector2(r.xMax, r.center.y), new Vector2(r.xMin, r.yMin) };
                    break;

                default: // slab: the top edge leans one way, the bottom edge the other
                {
                    float offset = r.height * Mathf.Tan(shearDegrees * Mathf.Deg2Rad) * 0.5f;
                    p = new[]
                    {
                        new Vector2(r.xMin + offset, r.yMax), new Vector2(r.xMax + offset, r.yMax),
                        new Vector2(r.xMax - offset, r.yMin), new Vector2(r.xMin - offset, r.yMin)
                    };
                    break;
                }
            }

            for (int i = 0; i < p.Length; i++) vh.AddVert(p[i], color, Vector2.zero);
            for (int i = 1; i < p.Length - 1; i++) vh.AddTriangle(0, i, i + 1);
        }
    }
}

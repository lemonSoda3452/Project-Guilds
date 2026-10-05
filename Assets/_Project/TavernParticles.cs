using UnityEngine;

namespace HeroManagement
{
    // Small effects that make the diorama feel alive: forge sparks, cauldron bubbles, fireplace embers, chimney smoke.
    // They use the Sprites/Default shader, which works in the built-in pipeline and in URP.
    public class TavernParticles : MonoBehaviour
    {
        public Transform forgeSparks;
        public Transform cauldronBubbles;
        public Transform fireplaceEmbers;
        public Transform chimneySmoke;

        private Material material;

        void Start()
        {
            material = new Material(Shader.Find("Sprites/Default"));
            material.mainTexture = MakeSoftCircle();

            if (forgeSparks != null)
                Build(forgeSparks, "Sparks", new Color(1f, 0.75f, 0.25f), new Color(1f, 0.3f, 0.1f), 0.6f, 1.0f, 1.5f, 3f, 0.08f, 0.16f, 16f, 25f, -0.2f, false);

            if (cauldronBubbles != null)
                Build(cauldronBubbles, "Bubbles", new Color(0.75f, 0.45f, 1f), new Color(0.5f, 1f, 0.7f), 1.5f, 2.5f, 0.3f, 0.7f, 0.12f, 0.28f, 6f, 20f, -0.02f, true);

            if (fireplaceEmbers != null)
                Build(fireplaceEmbers, "Embers", new Color(1f, 0.6f, 0.2f), new Color(1f, 0.25f, 0.05f), 1.2f, 2f, 0.4f, 0.9f, 0.05f, 0.1f, 8f, 15f, -0.05f, false);

            if (chimneySmoke != null)
                Build(chimneySmoke, "Smoke", new Color(0.85f, 0.85f, 0.88f), new Color(0.6f, 0.6f, 0.65f), 3f, 4f, 0.4f, 0.8f, 0.3f, 0.5f, 3f, 12f, -0.02f, true);
        }

        private void Build(Transform at, string name, Color colorA, Color colorB, float lifeMin, float lifeMax,
            float speedMin, float speedMax, float sizeMin, float sizeMax, float rate, float coneAngle, float gravity, bool grow)
        {
            var go = new GameObject(name);
            go.transform.SetParent(at, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // particles shoot upwards

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop();

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = rate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = coneAngle;
            shape.radius = 0.1f;

            // Fade out at the end of life
            var colorOverLife = ps.colorOverLifetime;
            colorOverLife.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            colorOverLife.color = new ParticleSystem.MinMaxGradient(gradient);

            if (grow)
            {
                var sizeOverLife = ps.sizeOverLifetime;
                sizeOverLife.enabled = true;
                sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(1f, 1.6f)));
            }

            var psRenderer = go.GetComponent<ParticleSystemRenderer>();
            psRenderer.sharedMaterial = material;
            psRenderer.renderMode = ParticleSystemRenderMode.Billboard;

            ps.Play();
        }

        // A soft round sprite, so particles are bubbles and sparks instead of squares
        private Texture2D MakeSoftCircle()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }

            tex.Apply();
            return tex;
        }
    }
}

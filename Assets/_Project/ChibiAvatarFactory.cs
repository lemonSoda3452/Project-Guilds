using UnityEngine;

namespace HeroManagement
{
    // Builds a small "toy" character from primitives, in the spirit of the Link's Awakening remake:
    // a big round head, a tiny body, simple saturated colours. The class decides hat and colours,
    // the race decides skin, ears, tusks and beard. Replace it with real models whenever you want.
    public static class ChibiAvatarFactory
    {
        public static GameObject Create(HeroData hero, Transform parent)
        {
            var root = new GameObject("Avatar_" + hero.heroName);
            root.transform.SetParent(parent, false);

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);

            Color skin = SkinColor(hero.heroRace);
            Color outfit = OutfitColor(hero.heroClass);
            Color dark = new Color(0.25f, 0.17f, 0.12f);

            // Body, head, feet, eyes
            Part(visual, PrimitiveType.Capsule, "Body", new Vector3(0f, 0.3f, 0f), new Vector3(0.46f, 0.3f, 0.42f), outfit);
            Part(visual, PrimitiveType.Sphere, "Head", new Vector3(0f, 0.86f, 0f), new Vector3(0.64f, 0.6f, 0.62f), skin);
            Part(visual, PrimitiveType.Sphere, "FootL", new Vector3(-0.12f, 0.06f, 0.05f), new Vector3(0.2f, 0.12f, 0.3f), dark);
            Part(visual, PrimitiveType.Sphere, "FootR", new Vector3(0.12f, 0.06f, 0.05f), new Vector3(0.2f, 0.12f, 0.3f), dark);
            Part(visual, PrimitiveType.Sphere, "EyeL", new Vector3(-0.12f, 0.88f, 0.28f), new Vector3(0.08f, 0.11f, 0.06f), Color.black);
            Part(visual, PrimitiveType.Sphere, "EyeR", new Vector3(0.12f, 0.88f, 0.28f), new Vector3(0.08f, 0.11f, 0.06f), Color.black);

            AddClassGear(visual, hero.heroClass, outfit);
            AddRaceTraits(visual, hero.heroRace, skin);

            // Name tag
            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.75f, 0f);

            var tm = labelObject.AddComponent<TextMesh>();
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 48;
            tm.characterSize = 0.08f;
            tm.color = Color.white;

#if UNITY_2022_2_OR_NEWER
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            tm.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            var mr = labelObject.GetComponent<MeshRenderer>();
            if (mr != null && tm.font != null) mr.sharedMaterial = tm.font.material;
            labelObject.AddComponent<FaceCamera>();

            var idle = root.AddComponent<ChibiIdle>();
            idle.visual = visual;

            return root;
        }

        // ---------------------------------------------------------------- class: hat and accessories

        private static void AddClassGear(Transform v, HeroClass heroClass, Color outfit)
        {
            switch (heroClass)
            {
                case HeroClass.Warrior: // red helmet with a plume
                    Part(v, PrimitiveType.Sphere, "Helmet", new Vector3(0f, 0.99f, -0.02f), new Vector3(0.68f, 0.42f, 0.66f), outfit);
                    Part(v, PrimitiveType.Capsule, "Plume", new Vector3(0f, 1.24f, -0.05f), new Vector3(0.08f, 0.14f, 0.2f), new Color(0.95f, 0.8f, 0.2f));
                    break;

                case HeroClass.Ranger: // green hood and a feather
                    Part(v, PrimitiveType.Sphere, "Hood", new Vector3(0f, 0.99f, -0.02f), new Vector3(0.68f, 0.44f, 0.68f), outfit);
                    Part(v, PrimitiveType.Capsule, "Feather", new Vector3(0.22f, 1.2f, 0f), new Vector3(0.05f, 0.15f, 0.05f), new Color(0.95f, 0.85f, 0.3f), new Vector3(0f, 0f, -30f));
                    break;

                case HeroClass.Mage: // wide brim and a tall pointed hat
                    Part(v, PrimitiveType.Cylinder, "Brim", new Vector3(0f, 1.1f, 0f), new Vector3(0.8f, 0.02f, 0.8f), outfit);
                    Part(v, PrimitiveType.Capsule, "Hat", new Vector3(0f, 1.38f, 0f), new Vector3(0.3f, 0.34f, 0.3f), outfit);
                    break;

                case HeroClass.Cleric: // light hood and a golden halo
                    Part(v, PrimitiveType.Sphere, "Hood", new Vector3(0f, 0.99f, -0.02f), new Vector3(0.68f, 0.42f, 0.68f), outfit);
                    Part(v, PrimitiveType.Cylinder, "Halo", new Vector3(0f, 1.38f, 0f), new Vector3(0.45f, 0.015f, 0.45f), new Color(1f, 0.85f, 0.25f));
                    break;

                case HeroClass.Rogue: // purple hood and a dark mask
                    Part(v, PrimitiveType.Sphere, "Hood", new Vector3(0f, 0.99f, -0.02f), new Vector3(0.68f, 0.44f, 0.68f), outfit);
                    Part(v, PrimitiveType.Cube, "Mask", new Vector3(0f, 0.76f, 0.28f), new Vector3(0.38f, 0.12f, 0.06f), new Color(0.15f, 0.1f, 0.2f));
                    break;
            }
        }

        // ---------------------------------------------------------------- race: ears, tusks, beard, tail

        private static void AddRaceTraits(Transform v, HeroRace race, Color skin)
        {
            switch (race)
            {
                case HeroRace.Orc: // tusks
                    Part(v, PrimitiveType.Cube, "TuskL", new Vector3(-0.1f, 0.72f, 0.3f), new Vector3(0.05f, 0.1f, 0.05f), Color.white);
                    Part(v, PrimitiveType.Cube, "TuskR", new Vector3(0.1f, 0.72f, 0.3f), new Vector3(0.05f, 0.1f, 0.05f), Color.white);
                    break;

                case HeroRace.Elf: // long pointed ears
                    Part(v, PrimitiveType.Sphere, "EarL", new Vector3(-0.38f, 0.92f, 0f), new Vector3(0.24f, 0.07f, 0.07f), skin, new Vector3(0f, 0f, 20f));
                    Part(v, PrimitiveType.Sphere, "EarR", new Vector3(0.38f, 0.92f, 0f), new Vector3(0.24f, 0.07f, 0.07f), skin, new Vector3(0f, 0f, -20f));
                    break;

                case HeroRace.Dwarf: // big grey beard
                    Part(v, PrimitiveType.Sphere, "Beard", new Vector3(0f, 0.66f, 0.17f), new Vector3(0.46f, 0.36f, 0.3f), new Color(0.82f, 0.82f, 0.86f));
                    break;

                case HeroRace.Beastkin: // animal ears and a tail
                    Part(v, PrimitiveType.Sphere, "BeastEarL", new Vector3(-0.2f, 1.2f, 0f), new Vector3(0.16f, 0.22f, 0.1f), new Color(0.95f, 0.6f, 0.25f));
                    Part(v, PrimitiveType.Sphere, "BeastEarR", new Vector3(0.2f, 1.2f, 0f), new Vector3(0.16f, 0.22f, 0.1f), new Color(0.95f, 0.6f, 0.25f));
                    Part(v, PrimitiveType.Sphere, "Tail", new Vector3(0f, 0.22f, -0.3f), new Vector3(0.14f, 0.14f, 0.34f), new Color(0.95f, 0.6f, 0.25f));
                    break;
            }
        }

        // ---------------------------------------------------------------- colours

        private static Color SkinColor(HeroRace race)
        {
            switch (race)
            {
                case HeroRace.Elf: return new Color(1f, 0.89f, 0.78f);
                case HeroRace.Orc: return new Color(0.55f, 0.76f, 0.4f);
                case HeroRace.Dwarf: return new Color(0.96f, 0.76f, 0.62f);
                case HeroRace.DemiHuman: return new Color(0.82f, 0.8f, 0.96f);
                case HeroRace.Beastkin: return new Color(1f, 0.86f, 0.72f);
                default: return new Color(1f, 0.82f, 0.68f);
            }
        }

        private static Color OutfitColor(HeroClass heroClass)
        {
            switch (heroClass)
            {
                case HeroClass.Warrior: return new Color(0.88f, 0.28f, 0.24f);
                case HeroClass.Ranger: return new Color(0.32f, 0.72f, 0.34f);
                case HeroClass.Mage: return new Color(0.32f, 0.44f, 0.92f);
                case HeroClass.Cleric: return new Color(0.96f, 0.93f, 0.72f);
                case HeroClass.Rogue: return new Color(0.58f, 0.34f, 0.74f);
                default: return Color.gray;
            }
        }

        // ---------------------------------------------------------------- helper

        private static GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Color color, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);

            // Characters must never block the clicks meant for the hotspots
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            go.GetComponent<Renderer>().material.color = color;
            return go;
        }
    }
}

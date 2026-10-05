using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// IMPORTANT: this file must live in a folder called "Editor" (e.g. Assets/Editor/Tavern3DBuilder.cs)
// Run "Hero Management > Build Demo Scene" FIRST, then this one in the same scene.
//
// Art direction: a toy DIORAMA, like the Link's Awakening remake. A thick floating slab of a tavern with
// cut-away walls, round chunky furniture, warm pastel colours, a long lens seen from above at 3/4 angle.
namespace HeroManagement.EditorTools
{
    public static class Tavern3DBuilder
    {
        private const string MatFolder = "Assets/_Project/Materials/Tavern3D";

        // ---- palette (warm, saturated pastels)
        private static readonly Color Cream = new Color(0.98f, 0.91f, 0.75f);
        private static readonly Color Plank = new Color(0.8f, 0.57f, 0.34f);
        private static readonly Color LightWood = new Color(0.9f, 0.7f, 0.45f);
        private static readonly Color DarkWood = new Color(0.56f, 0.37f, 0.21f);
        private static readonly Color Stone = new Color(0.74f, 0.74f, 0.78f);
        private static readonly Color DarkStone = new Color(0.42f, 0.42f, 0.48f);
        private static readonly Color Grass = new Color(0.52f, 0.82f, 0.38f);
        private static readonly Color DarkGrass = new Color(0.38f, 0.68f, 0.32f);
        private static readonly Color Leaf = new Color(0.45f, 0.78f, 0.35f);
        private static readonly Color Red = new Color(0.88f, 0.34f, 0.3f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
        private static readonly Color Fire = new Color(1f, 0.55f, 0.15f);
        private static readonly Color Sky = new Color(0.62f, 0.83f, 0.95f);

        [MenuItem("Hero Management/Build 3D Tavern")]
        public static void Build()
        {
            if (GameObject.Find("Tavern3D") != null)
            {
                EditorUtility.DisplayDialog("Hero Management",
                    "A 'Tavern3D' object already exists in this scene.\nDelete it (and 'CameraRig', 'Avatars', 'Tavern3DController', 'Effects', 'Audio', 'MiniatureLook') first.", "OK");
                return;
            }

            var canvasGO = GameObject.Find("Canvas");
            if (canvasGO == null)
            {
                EditorUtility.DisplayDialog("Hero Management",
                    "No 'Canvas' found. Run 'Build Demo Scene' first, so the 3D tavern can be linked to the UI panels.", "OK");
                return;
            }

            EnsureFolder(MatFolder);

            var room = new GameObject("Tavern3D").transform;

            // ================= THE DIORAMA SLAB =================
            Part(room, "Grass", PrimitiveType.Cube, new Vector3(0f, -1.0f, 2f), new Vector3(34f, 0.4f, 26f), Grass);
            Part(room, "Hill", PrimitiveType.Sphere, new Vector3(0f, -3.5f, 17f), new Vector3(22f, 8f, 10f), DarkGrass);
            Part(room, "BasePlate", PrimitiveType.Cube, new Vector3(0f, -0.55f, 0f), new Vector3(15.2f, 0.5f, 11.2f), DarkWood);
            Part(room, "Floor", PrimitiveType.Cube, new Vector3(0f, -0.15f, 0f), new Vector3(14f, 0.3f, 10f), Plank);
            for (int i = -3; i <= 3; i++)
                Part(room, "PlankLine" + i, PrimitiveType.Cube, new Vector3(i * 2f, 0.011f, 0f), new Vector3(0.05f, 0.02f, 10f), DarkWood);
            Part(room, "Path", PrimitiveType.Cube, new Vector3(0f, -0.76f, -8.5f), new Vector3(3.2f, 0.05f, 5f), Stone);
            Part(room, "Rug", PrimitiveType.Cylinder, new Vector3(0f, 0.012f, -1.6f), new Vector3(3.4f, 0.01f, 3.4f), Red);
            Part(room, "RugInner", PrimitiveType.Cylinder, new Vector3(0f, 0.016f, -1.6f), new Vector3(2.4f, 0.01f, 2.4f), Cream);

            // Cut-away walls: the front is open so the camera sees everything
            Part(room, "WallBack", PrimitiveType.Cube, new Vector3(0f, 1.8f, 5.15f), new Vector3(14.6f, 3.6f, 0.3f), Cream);
            Part(room, "WallLeft", PrimitiveType.Cube, new Vector3(-7.15f, 1.8f, 0f), new Vector3(0.3f, 3.6f, 10.3f), Cream);
            Part(room, "WallRight", PrimitiveType.Cube, new Vector3(7.15f, 1.8f, 0f), new Vector3(0.3f, 3.6f, 10.3f), Cream);
            Part(room, "BeamBack", PrimitiveType.Cube, new Vector3(0f, 3.65f, 5.15f), new Vector3(14.8f, 0.25f, 0.45f), DarkWood);
            Part(room, "BeamLeft", PrimitiveType.Cube, new Vector3(-7.15f, 3.65f, 0f), new Vector3(0.45f, 0.25f, 10.4f), DarkWood);
            Part(room, "BeamRight", PrimitiveType.Cube, new Vector3(7.15f, 3.65f, 0f), new Vector3(0.45f, 0.25f, 10.4f), DarkWood);
            foreach (var corner in new[] { new Vector2(-7.15f, 5.15f), new Vector2(7.15f, 5.15f), new Vector2(-7.15f, -5.0f), new Vector2(7.15f, -5.0f) })
                Part(room, "Post", PrimitiveType.Cube, new Vector3(corner.x, 1.95f, corner.y), new Vector3(0.5f, 3.9f, 0.5f), DarkWood);

            // Window
            Part(room, "WindowFrame", PrimitiveType.Cube, new Vector3(2.9f, 2.4f, 4.99f), new Vector3(1.6f, 1.3f, 0.06f), DarkWood);
            Part(room, "WindowGlass", PrimitiveType.Cube, new Vector3(2.9f, 2.4f, 4.95f), new Vector3(1.4f, 1.1f, 0.05f), Sky);

            // Fireplace with its chimney
            Part(room, "Fireplace", PrimitiveType.Cube, new Vector3(0f, 1.0f, 4.6f), new Vector3(2.6f, 2.0f, 0.9f), Stone);
            Part(room, "FireplaceHole", PrimitiveType.Cube, new Vector3(0f, 0.65f, 4.13f), new Vector3(1.4f, 1.1f, 0.1f), new Color(0.13f, 0.09f, 0.08f));
            Part(room, "FireGlow", PrimitiveType.Cube, new Vector3(0f, 0.45f, 4.1f), new Vector3(0.9f, 0.45f, 0.08f), Fire);
            Part(room, "LogL", PrimitiveType.Cylinder, new Vector3(-0.25f, 0.22f, 4.08f), new Vector3(0.12f, 0.35f, 0.12f), DarkWood, new Vector3(0f, 0f, 90f));
            Part(room, "LogR", PrimitiveType.Cylinder, new Vector3(0.25f, 0.22f, 4.08f), new Vector3(0.12f, 0.35f, 0.12f), DarkWood, new Vector3(0f, 0f, 90f));
            Part(room, "Chimney", PrimitiveType.Cube, new Vector3(0f, 3.4f, 4.75f), new Vector3(1.1f, 2.8f, 0.8f), Stone);

            // ================= HOTSPOTS =================
            // Notice board (on the back wall, above the counter)
            var board = Prop(room, "NoticeBoard", new Vector3(-4.2f, 2.4f, 4.97f), new Vector3(0f, 0f, -0.1f), new Vector3(2.8f, 1.8f, 0.4f));
            Part(board.transform, "Frame", PrimitiveType.Cube, Vector3.zero, new Vector3(2.8f, 1.8f, 0.12f), DarkWood);
            Part(board.transform, "Cork", PrimitiveType.Cube, new Vector3(0f, 0f, -0.06f), new Vector3(2.55f, 1.55f, 0.08f), LightWood);
            Part(board.transform, "Paper1", PrimitiveType.Cube, new Vector3(-0.8f, 0.3f, -0.12f), new Vector3(0.55f, 0.45f, 0.02f), Cream, new Vector3(0f, 0f, 6f));
            Part(board.transform, "Paper2", PrimitiveType.Cube, new Vector3(0f, -0.25f, -0.12f), new Vector3(0.5f, 0.6f, 0.02f), Color.white, new Vector3(0f, 0f, -5f));
            Part(board.transform, "Paper3", PrimitiveType.Cube, new Vector3(0.8f, 0.25f, -0.12f), new Vector3(0.55f, 0.4f, 0.02f), Cream, new Vector3(0f, 0f, 4f));
            Part(board.transform, "Pin1", PrimitiveType.Sphere, new Vector3(-0.8f, 0.5f, -0.14f), new Vector3(0.07f, 0.07f, 0.07f), Red);
            Part(board.transform, "Pin2", PrimitiveType.Sphere, new Vector3(0.8f, 0.43f, -0.14f), new Vector3(0.07f, 0.07f, 0.07f), Red);

            // Counter (recruitment)
            var counter = Prop(room, "Counter", new Vector3(-3.6f, 0f, 3.9f), new Vector3(0f, 0.6f, 0f), new Vector3(4.3f, 1.3f, 1.1f));
            Part(counter.transform, "Body", PrimitiveType.Cube, new Vector3(0f, 0.5f, 0f), new Vector3(4f, 1f, 0.9f), DarkWood);
            Part(counter.transform, "Top", PrimitiveType.Cube, new Vector3(0f, 1.06f, 0f), new Vector3(4.3f, 0.12f, 1.1f), LightWood);
            Part(counter.transform, "Mug1", PrimitiveType.Cylinder, new Vector3(-1.2f, 1.23f, 0f), new Vector3(0.2f, 0.1f, 0.2f), Cream);
            Part(counter.transform, "Mug2", PrimitiveType.Cylinder, new Vector3(0f, 1.23f, 0.1f), new Vector3(0.2f, 0.1f, 0.2f), Red);
            Part(counter.transform, "Mug3", PrimitiveType.Cylinder, new Vector3(1.1f, 1.23f, -0.1f), new Vector3(0.2f, 0.1f, 0.2f), Cream);
            for (int i = -1; i <= 1; i++)
                Part(counter.transform, "Stool" + i, PrimitiveType.Cylinder, new Vector3(i * 1.3f, 0.24f, -1.0f), new Vector3(0.5f, 0.24f, 0.5f), Red);

            // Heroes' table (round)
            var heroTable = RoundTable(room, "HeroesTable", new Vector3(-2f, 0f, 0.5f), true);
            Part(room, "StoolA", PrimitiveType.Cylinder, new Vector3(-3.3f, 0.2f, 1.1f), new Vector3(0.5f, 0.2f, 0.5f), Red);
            Part(room, "StoolB", PrimitiveType.Cylinder, new Vector3(-0.7f, 0.2f, 1.1f), new Vector3(0.5f, 0.2f, 0.5f), Red);
            Part(room, "StoolC", PrimitiveType.Cylinder, new Vector3(-2f, 0.2f, -0.9f), new Vector3(0.5f, 0.2f, 0.5f), Red);

            // A second table, just for decoration
            RoundTable(room, "TableRight", new Vector3(2f, 0f, 0.5f), false);

            // A miniature of the Spire on the table: one disc per 10 floors, lit in gold as the guild climbs
            var spire = new GameObject("SpireModel").transform;
            spire.SetParent(room, false);
            spire.localPosition = new Vector3(2f, 0.9f, 0.5f);
            var spireSegments = new List<Renderer>();
            for (int s10 = 0; s10 < 10; s10++)
            {
                float diameter = 0.7f - s10 * 0.03f;
                var segment = Part(spire, "Floors" + (s10 * 10 + 1) + "-" + (s10 * 10 + 10), PrimitiveType.Cylinder,
                    new Vector3(0f, 0.08f + s10 * 0.16f, 0f), new Vector3(diameter, 0.08f, diameter), new Color(0.58f, 0.58f, 0.66f));
                spireSegments.Add(segment.GetComponent<Renderer>());
            }
            Part(spire, "Flag", PrimitiveType.Sphere, new Vector3(0f, 1.72f, 0f), new Vector3(0.14f, 0.14f, 0.14f), Red);
            spire.gameObject.AddComponent<TowerVisual>().segments = spireSegments.ToArray();
            Part(room, "StoolD", PrimitiveType.Cylinder, new Vector3(0.7f, 0.2f, 1.1f), new Vector3(0.5f, 0.2f, 0.5f), Red);
            Part(room, "StoolE", PrimitiveType.Cylinder, new Vector3(3.3f, 0.2f, 1.1f), new Vector3(0.5f, 0.2f, 0.5f), Red);

            // Forge
            var forge = Prop(room, "Forge", new Vector3(5.6f, 0f, 3.4f), new Vector3(0f, 1.1f, -0.4f), new Vector3(2.1f, 2.2f, 2.6f));
            Part(forge.transform, "Body", PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f), new Vector3(1.9f, 1.1f, 1.5f), DarkStone);
            Part(forge.transform, "Top", PrimitiveType.Cube, new Vector3(0f, 1.16f, 0f), new Vector3(2.0f, 0.12f, 1.6f), Stone);
            Part(forge.transform, "Chimney", PrimitiveType.Cylinder, new Vector3(0.5f, 2.6f, 0.45f), new Vector3(0.5f, 1.4f, 0.5f), Stone);
            Part(forge.transform, "FireMouth", PrimitiveType.Cube, new Vector3(0f, 0.6f, -0.78f), new Vector3(1.0f, 0.5f, 0.08f), Fire);
            Part(forge.transform, "Coals", PrimitiveType.Cube, new Vector3(0f, 1.24f, 0f), new Vector3(0.8f, 0.06f, 0.8f), Fire);
            Part(forge.transform, "AnvilBase", PrimitiveType.Cylinder, new Vector3(0f, 0.2f, -1.3f), new Vector3(0.35f, 0.2f, 0.35f), DarkStone);
            Part(forge.transform, "AnvilTop", PrimitiveType.Cube, new Vector3(0f, 0.5f, -1.3f), new Vector3(0.85f, 0.2f, 0.4f), DarkStone);

            // Witch's cauldron
            var witch = Prop(room, "WitchCauldron", new Vector3(-5.8f, 0f, -1f), new Vector3(0f, 0.7f, 0f), new Vector3(1.5f, 1.5f, 1.5f));
            Part(witch.transform, "Pot", PrimitiveType.Sphere, new Vector3(0f, 0.62f, 0f), new Vector3(1.3f, 1.1f, 1.3f), new Color(0.35f, 0.2f, 0.5f));
            Part(witch.transform, "Rim", PrimitiveType.Cylinder, new Vector3(0f, 1.12f, 0f), new Vector3(1.05f, 0.05f, 1.05f), new Color(0.58f, 0.35f, 0.75f));
            Part(witch.transform, "Potion", PrimitiveType.Cylinder, new Vector3(0f, 1.15f, 0f), new Vector3(0.95f, 0.02f, 0.95f), new Color(0.4f, 1f, 0.6f));
            Part(witch.transform, "MushroomCap", PrimitiveType.Sphere, new Vector3(1.0f, 0.3f, 0.3f), new Vector3(0.38f, 0.22f, 0.38f), Red);
            Part(witch.transform, "MushroomStem", PrimitiveType.Cylinder, new Vector3(1.0f, 0.14f, 0.3f), new Vector3(0.1f, 0.14f, 0.1f), Cream);

            // Storage chest with barrels
            var chest = Prop(room, "StorageChest", new Vector3(5.8f, 0f, -1.5f), new Vector3(-0.3f, 0.5f, 0.1f), new Vector3(2.7f, 1.0f, 1.2f));
            Part(chest.transform, "Body", PrimitiveType.Cube, new Vector3(0f, 0.33f, 0f), new Vector3(1.4f, 0.65f, 0.9f), DarkWood);
            Part(chest.transform, "Lid", PrimitiveType.Cube, new Vector3(0f, 0.74f, 0f), new Vector3(1.4f, 0.18f, 0.95f), LightWood);
            Part(chest.transform, "BandL", PrimitiveType.Cube, new Vector3(-0.45f, 0.5f, 0f), new Vector3(0.12f, 0.9f, 0.97f), Gold);
            Part(chest.transform, "BandR", PrimitiveType.Cube, new Vector3(0.45f, 0.5f, 0f), new Vector3(0.12f, 0.9f, 0.97f), Gold);
            Part(chest.transform, "Lock", PrimitiveType.Cube, new Vector3(0f, 0.55f, -0.49f), new Vector3(0.2f, 0.2f, 0.05f), Gold);
            Part(chest.transform, "Barrel1", PrimitiveType.Cylinder, new Vector3(-1.2f, 0.5f, 0.2f), new Vector3(0.8f, 0.5f, 0.8f), DarkWood);
            Part(chest.transform, "Barrel2", PrimitiveType.Cylinder, new Vector3(-1.2f, 0.5f, -0.6f), new Vector3(0.7f, 0.45f, 0.7f), LightWood);

            // ================= OUTSIDE: trees and bushes =================
            Tree(room, new Vector3(-10.5f, 0f, 5f), 1.2f);
            Tree(room, new Vector3(10.5f, 0f, 4f), 1.1f);
            Tree(room, new Vector3(-11.5f, 0f, -2.5f), 1.0f);
            Tree(room, new Vector3(11.5f, 0f, -3f), 1.3f);
            Part(room, "BushL", PrimitiveType.Sphere, new Vector3(-9f, 0.1f, -7f), new Vector3(1.8f, 1.2f, 1.8f), Leaf);
            Part(room, "BushR", PrimitiveType.Sphere, new Vector3(9f, 0.1f, -7.5f), new Vector3(2.0f, 1.3f, 2.0f), Leaf);

            // ================= LIGHT & MOOD =================
            Light sun;
            var sunGO = GameObject.Find("Directional Light");
            if (sunGO != null) sun = sunGO.GetComponent<Light>();
            else
            {
                sunGO = new GameObject("Directional Light");
                sun = sunGO.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
                sun.intensity = 1.1f;
                sun.color = new Color(1f, 0.95f, 0.86f);
                sun.shadows = LightShadows.Soft;
            }

            AddPointLight(room, "TavernLamp", new Vector3(0f, 3.2f, 0f), new Color(1f, 0.85f, 0.6f), 3f, 12f);
            AddPointLight(room, "FireLight", new Vector3(0f, 1.2f, 3.6f), new Color(1f, 0.6f, 0.3f), 6f, 8f);
            AddPointLight(room, "ForgeLight", new Vector3(5.6f, 1.8f, 2.6f), new Color(1f, 0.45f, 0.2f), 5f, 6f);
            AddPointLight(room, "WitchLight", new Vector3(-5.8f, 1.5f, -1f), new Color(0.7f, 0.4f, 1f), 4f, 5f);

            var look = new GameObject("MiniatureLook").AddComponent<MiniatureLook>();
            look.sun = sun;

            // ================= CAMERA: fixed, slightly tilted shots with a long lens =================
            var camGO = GameObject.Find("Main Camera");
            if (camGO == null)
            {
                camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camGO.tag = "MainCamera";
            }
            var cam = camGO.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Sky;

            var rigGO = new GameObject("CameraRig");
            var rig = rigGO.AddComponent<TavernCameraRig>();
            rig.cam = cam;
            rig.moveSpeed = 4f;

            OrbitShot(rig, rigGO.transform, "Overview", new Vector3(0f, 0.6f, 0.4f), 19f, 46f, 0f, 32f);
            OrbitShot(rig, rigGO.transform, "Board", new Vector3(-4.2f, 2.2f, 4.9f), 8f, 22f, 0f, 32f);
            OrbitShot(rig, rigGO.transform, "Counter", new Vector3(-3.6f, 0.9f, 3.7f), 8f, 38f, 8f, 32f);
            OrbitShot(rig, rigGO.transform, "HeroesTable", new Vector3(-2f, 0.7f, 0.5f), 9f, 44f, 0f, 32f);
            OrbitShot(rig, rigGO.transform, "Forge", new Vector3(5.6f, 1.0f, 3.4f), 8f, 38f, -10f, 32f);
            OrbitShot(rig, rigGO.transform, "Witch", new Vector3(-5.8f, 0.8f, -1f), 8f, 40f, 10f, 32f);
            OrbitShot(rig, rigGO.transform, "Storage", new Vector3(5.8f, 0.6f, -1.5f), 8f, 40f, -10f, 32f);

            // ================= CHARACTER SLOTS (chibi heroes) =================
            var avatarsGO = new GameObject("Avatars");
            var spawner = avatarsGO.AddComponent<HeroAvatarSpawner>();

            var guildSlots = new List<Transform>();
            Vector3[] guildPos =
            {
                new Vector3(-3.6f, 0f, -0.7f), new Vector3(-0.6f, 0f, -1.1f), new Vector3(1.0f, 0f, -2.0f),
                new Vector3(3.6f, 0f, -0.9f), new Vector3(-1.0f, 0f, 2.6f), new Vector3(2.0f, 0f, 2.6f)
            };
            for (int i = 0; i < guildPos.Length; i++) guildSlots.Add(Slot(avatarsGO.transform, "GuildSlot" + i, guildPos[i]));

            var visitorSlots = new List<Transform>();
            Vector3[] visitorPos = { new Vector3(-4.9f, 0f, 2.2f), new Vector3(-3.6f, 0f, 2.2f), new Vector3(-2.3f, 0f, 2.2f) };
            for (int i = 0; i < visitorPos.Length; i++) visitorSlots.Add(Slot(avatarsGO.transform, "VisitorSlot" + i, visitorPos[i]));

            spawner.guildSlots = guildSlots.ToArray();
            spawner.visitorSlots = visitorSlots.ToArray();

            // ================= EFFECTS & AUDIO =================
            var effectsGO = new GameObject("Effects");
            var particles = effectsGO.AddComponent<TavernParticles>();
            particles.forgeSparks = Anchor(effectsGO.transform, "ForgeSparks", new Vector3(5.6f, 1.3f, 3.4f));
            particles.cauldronBubbles = Anchor(effectsGO.transform, "CauldronBubbles", new Vector3(-5.8f, 1.2f, -1f));
            particles.fireplaceEmbers = Anchor(effectsGO.transform, "FireplaceEmbers", new Vector3(0f, 0.6f, 4.0f));
            particles.chimneySmoke = Anchor(effectsGO.transform, "ChimneySmoke", new Vector3(0f, 4.8f, 4.75f));

            new GameObject("Audio").AddComponent<TavernAudioManager>();

            // ================= UI LINKS =================
            Transform cv = canvasGO.transform;
            GameObject FindPanel(string name)
            {
                var t = cv.Find(name);
                return t != null ? t.gameObject : null;
            }

            var recUI = canvasGO.GetComponent<RecruitmentUIManager>();
            var tavernUI = canvasGO.GetComponent<TavernUIManager>();
            var sheetUI = canvasGO.GetComponent<HeroSheetUIManager>();
            var expUI = canvasGO.GetComponent<ExpeditionsUIManager>();
            var buildUI = canvasGO.GetComponent<TavernBuildUIManager>();
            var witchUI = canvasGO.GetComponent<WitchUIManager>();
            var invUI = canvasGO.GetComponent<InventoryUIManager>();
            var boardUI = canvasGO.GetComponent<BoardUIManager>();

            // From now on the 3D scene decides when the board is shown
            if (boardUI != null) boardUI.externalVisibility = true;

            var hsBoard = Hotspot(board, "Notice Board", "Board", FindPanel("BoardPanel"));
            if (boardUI != null) UnityEventTools.AddPersistentListener(hsBoard.onFocus, boardUI.RefreshFromOutside);

            var hsCounter = Hotspot(counter, "Counter", "Counter", FindPanel("RecruitmentPanel"), FindPanel("TavernPanel"));
            if (recUI != null) UnityEventTools.AddPersistentListener(hsCounter.onFocus, recUI.Refresh);
            if (tavernUI != null) UnityEventTools.AddPersistentListener(hsCounter.onFocus, tavernUI.RefreshRosterUI);

            var hsTable = Hotspot(heroTable, "Heroes' Table", "HeroesTable", FindPanel("HeroSheetPanel"), FindPanel("ExpeditionsPanel"));
            if (sheetUI != null) UnityEventTools.AddPersistentListener(hsTable.onFocus, sheetUI.Refresh);
            if (expUI != null) UnityEventTools.AddPersistentListener(hsTable.onFocus, expUI.Refresh);

            var hsForge = Hotspot(forge, "Forge", "Forge", FindPanel("TavernBuildPanel"));
            if (buildUI != null) UnityEventTools.AddPersistentListener(hsForge.onFocus, buildUI.Refresh);

            var hsWitch = Hotspot(witch, "Witch", "Witch", FindPanel("WitchPanel"));
            if (witchUI != null) UnityEventTools.AddPersistentListener(hsWitch.onFocus, witchUI.Refresh);

            var hsChest = Hotspot(chest, "Storage", "Storage", FindPanel("InventoryPanel"));
            if (invUI != null) UnityEventTools.AddPersistentListener(hsChest.onFocus, invUI.Refresh);

            // ================= CONTROLLER =================
            var ctrlGO = new GameObject("Tavern3DController");
            var ctrl = ctrlGO.AddComponent<Tavern3DController>();
            ctrl.rig = rig;
            ctrl.cam = cam;
            ctrl.hotspots = new List<TavernHotspot> { hsBoard, hsCounter, hsTable, hsForge, hsWitch, hsChest };
            ctrl.menu = canvasGO.GetComponent<ManagementMenuUIManager>();

            var panels = new List<GameObject>();
            foreach (var hs in ctrl.hotspots)
            {
                foreach (var p in hs.panelsToShow)
                {
                    if (p != null && !panels.Contains(p)) panels.Add(p);
                }
            }
            ctrl.allFocusPanels = panels.ToArray();
            ctrl.backButton = MakeBackButton(cv, ctrl);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = ctrlGO;

            EditorUtility.DisplayDialog("Hero Management",
                "3D tavern diorama built!\n\nSave the scene (Ctrl+S) and press Play.\n" +
                "- Click an object (board, counter, table, forge, cauldron, chest) to move the camera there and open its panel.\n" +
                "- ESC or the Back button returns to the overview.\n\n" +
                "For the true miniature look, add a Global Volume with a Depth of Field override (focus ~19 m for the overview).", "OK");
        }

        // ---------------------------------------------------------------- scenery helpers

        private static void Tree(Transform parent, Vector3 position, float scale)
        {
            Part(parent, "TreeTrunk", PrimitiveType.Cylinder, position + new Vector3(0f, 1.0f * scale, 0f), new Vector3(0.5f, 1.0f, 0.5f) * scale, DarkWood);
            Part(parent, "TreeCanopy", PrimitiveType.Sphere, position + new Vector3(0f, 3.2f * scale, 0f), new Vector3(3.0f, 2.8f, 3.0f) * scale, Leaf);
            Part(parent, "TreeCanopy2", PrimitiveType.Sphere, position + new Vector3(0.9f * scale, 3.8f * scale, 0.2f * scale), new Vector3(2.0f, 2.0f, 2.0f) * scale, new Color(0.58f, 0.86f, 0.4f));
        }

        private static GameObject RoundTable(Transform parent, string name, Vector3 position, bool withCollider)
        {
            var table = Prop(parent, name, position, new Vector3(0f, 0.45f, 0f), new Vector3(1.9f, 0.9f, 1.9f), withCollider);
            Part(table.transform, "Top", PrimitiveType.Cylinder, new Vector3(0f, 0.82f, 0f), new Vector3(1.9f, 0.07f, 1.9f), LightWood);
            Part(table.transform, "Leg", PrimitiveType.Cylinder, new Vector3(0f, 0.4f, 0f), new Vector3(0.3f, 0.4f, 0.3f), DarkWood);
            Part(table.transform, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.9f, 0.04f, 0.9f), DarkWood);
            Part(table.transform, "Mug1", PrimitiveType.Cylinder, new Vector3(0.35f, 0.95f, 0.2f), new Vector3(0.18f, 0.09f, 0.18f), Cream);
            Part(table.transform, "Mug2", PrimitiveType.Cylinder, new Vector3(-0.3f, 0.95f, -0.3f), new Vector3(0.18f, 0.09f, 0.18f), Red);
            return table;
        }

        // ---------------------------------------------------------------- generic helpers

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        // One material asset per colour, built from the default material of the project's render pipeline
        private static Material GetMat(Color color)
        {
            string path = MatFolder + "/Tavern_" + ColorUtility.ToHtmlStringRGB(color) + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var baseMaterial = temp.GetComponent<Renderer>().sharedMaterial;
            Object.DestroyImmediate(temp);

            var mat = new Material(baseMaterial);
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // Decorative piece: a primitive WITHOUT collider (only props with a hotspot get a collider)
        private static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            go.GetComponent<Renderer>().sharedMaterial = GetMat(color);

            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            return go;
        }

        // A container for several parts, with ONE box collider for clicking
        private static GameObject Prop(Transform parent, string name, Vector3 position, Vector3 colliderCenter, Vector3 colliderSize, bool withCollider = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            if (withCollider)
            {
                var box = go.AddComponent<BoxCollider>();
                box.center = colliderCenter;
                box.size = colliderSize;
            }
            return go;
        }

        private static void AddPointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
        }

        // A camera shot orbiting a point: distance, pitch (looking down) and yaw
        private static void OrbitShot(TavernCameraRig rig, Transform parent, string name, Vector3 target, float distance, float pitch, float yaw, float fov)
        {
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var position = target + rotation * new Vector3(0f, 0f, -distance);

            var go = new GameObject("Shot_" + name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);

            rig.anchors.Add(new TavernCameraRig.CameraAnchor { anchorName = name, point = go.transform, fieldOfView = fov });
        }

        private static Transform Slot(Transform parent, string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // heroes face the camera side
            return go.transform;
        }

        private static Transform Anchor(Transform parent, string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            return go.transform;
        }

        private static TavernHotspot Hotspot(GameObject target, string hotspotName, string anchor, params GameObject[] panels)
        {
            var hs = target.AddComponent<TavernHotspot>();
            hs.hotspotName = hotspotName;
            hs.cameraAnchor = anchor;

            var list = new List<GameObject>();
            foreach (var p in panels)
            {
                if (p != null) list.Add(p);
            }
            hs.panelsToShow = list.ToArray();
            return hs;
        }

        private static Font GetFont()
        {
#if UNITY_2022_2_OR_NEWER
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }

        private static GameObject MakeBackButton(Transform canvas, Tavern3DController controller)
        {
            var go = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(canvas, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.88f, 0.885f);
            rt.anchorMax = new Vector2(0.98f, 0.93f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.6f, 0.25f, 0.25f, 1f);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(go.transform, false);
            var trt = (RectTransform)textGO.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            var t = textGO.GetComponent<Text>();
            t.font = GetFont();
            t.text = "< Back";
            t.fontSize = 22;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.raycastTarget = false;

            UnityEventTools.AddPersistentListener(btn.onClick, controller.OnBackClicked);

            go.SetActive(false);
            return go;
        }
    }
}

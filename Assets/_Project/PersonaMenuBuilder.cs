using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// IMPORTANT: this file must live in a folder called "Editor" (e.g. Assets/Editor/PersonaMenuBuilder.cs)
// Run it AFTER "Build Demo Scene". The Persona menu replaces the four buttons of the top bar.
namespace HeroManagement.EditorTools
{
    public static class PersonaMenuBuilder
    {
        [MenuItem("Hero Management/Add Persona Menu")]
        public static void Build()
        {
            var canvasGO = GameObject.Find("Canvas");
            if (canvasGO == null)
            {
                EditorUtility.DisplayDialog("Hero Management", "No 'Canvas' found. Run 'Build Demo Scene' first.", "OK");
                return;
            }

            if (canvasGO.transform.Find("PersonaMenu") != null)
            {
                EditorUtility.DisplayDialog("Hero Management", "The scene already has a 'PersonaMenu'. Delete it first to build it again.", "OK");
                return;
            }

            // ---- the menu object (always active: it listens for TAB even when the menu is closed)
            var go = new GameObject("PersonaMenu", typeof(RectTransform));
            go.transform.SetParent(canvasGO.transform, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Above everything, but below the end screen
            var endPanel = canvasGO.transform.Find("EndPanel");
            if (endPanel != null) go.transform.SetSiblingIndex(endPanel.GetSiblingIndex());
            else go.transform.SetAsLastSibling();

            var menu = go.AddComponent<PersonaMenu>();
            menu.openOnStart = true;

            // ---- the items
            var mgmt = canvasGO.GetComponent<ManagementMenuUIManager>();
            var managers = GameObject.Find("Managers");
            var saveLoad = managers != null ? managers.GetComponent<SaveLoadManager>() : null;

            AddItem(menu, "Play", menu.Close, false);

            if (mgmt != null)
            {
                AddItem(menu, "Heroes", mgmt.OnHeroesClicked, false);
                AddItem(menu, "Inventory", mgmt.OnInventoryClicked, false);
                AddItem(menu, "Witch", mgmt.OnWitchClicked, false);
                AddItem(menu, "Tavern", mgmt.OnTavernClicked, false);
            }

            if (saveLoad != null)
            {
                AddItem(menu, "Save", saveLoad.OnSaveClicked, true);
                AddItem(menu, "Load", saveLoad.OnLoadClicked, true);
            }

            AddItem(menu, "Theme", menu.NextTheme, true);

            // ---- the four old buttons are replaced by one "Menu" button
            foreach (var oldName in new[] { "MenuHeroes", "MenuInventory", "MenuWitch", "MenuTavern" })
            {
                var old = canvasGO.transform.Find(oldName);
                if (old != null) old.gameObject.SetActive(false);
            }

            var button = MakeButton(canvasGO.transform, "MenuButton", new Vector2(0.49f, 0.935f), new Vector2(0.62f, 0.99f), "MENU [Tab]");
            UnityEventTools.AddPersistentListener(button.onClick, menu.Toggle);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = go;

            EditorUtility.DisplayDialog("Hero Management",
                "Persona menu added!\n\n- It opens by itself when the game starts. Choose 'Play' to begin.\n" +
                "- TAB (or the MENU button) opens and closes it. W/S or the mouse choose, ENTER or a click confirm, ESC closes.\n" +
                "- To start without the menu, untick 'Open On Start' on the PersonaMenu object.\n\nSave the scene (Ctrl+S).", "OK");
        }

        private static void AddItem(PersonaMenu menu, string label, UnityAction action, bool keepOpen)
        {
            var item = new PersonaMenuItem { label = label, keepOpen = keepOpen };
            if (action != null) UnityEventTools.AddPersistentListener(item.onSelect, action);
            menu.items.Add(item);
        }

        private static Font GetFont()
        {
#if UNITY_2022_2_OR_NEWER
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }

        private static Button MakeButton(Transform parent, string name, Vector2 min, Vector2 max, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.85f, 0.1f, 0.15f, 1f);
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
            t.text = label;
            t.fontSize = 22;
            t.fontStyle = FontStyle.BoldAndItalic;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.raycastTarget = false;

            return btn;
        }
    }
}

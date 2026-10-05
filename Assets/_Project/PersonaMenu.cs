using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HeroManagement
{
    [Serializable]
    public class PersonaMenuItem
    {
        public string label = "Item";
        public UnityEvent onSelect = new UnityEvent();
        [Tooltip("Stay in the menu after choosing it (Theme, Save, Load...)")]
        public bool keepOpen = false;
    }

    [Serializable]
    public class PersonaTheme
    {
        public string name = "Red";
        public Color main = new Color(0.9f, 0.1f, 0.15f);
        public Color dark = new Color(0.05f, 0.05f, 0.07f);
        public Color text = Color.white;
    }

    // A Persona-style menu: drifting diamonds and wedges, slanted slabs in a diagonal, pop-in with bounce,
    // flash and punch on open, inverted selection, a glint on the title, and a red + black sweep between pages.
    // It builds its whole UI from code. Open it with TAB (or the Menu button), W/S or the mouse to choose,
    // ENTER or a click to confirm, ESC to close.
    [RequireComponent(typeof(RectTransform))]
    public class PersonaMenu : MonoBehaviour
    {
        [Header("Items")]
        public List<PersonaMenuItem> items = new List<PersonaMenuItem>();

        [Header("Layout (y grows downwards, like the original sketch)")]
        public Vector2 firstItemPos = new Vector2(-480f, -190f);
        public Vector2 itemStep = new Vector2(55f, 105f);
        public Vector2 itemSize = new Vector2(560f, 88f);
        public float itemRotation = -8f;
        public float shear = 24f;

        [Header("Motion")]
        public float stagger = 0.1f;
        public float selectedScale = 1.22f;
        public float dimScale = 0.92f;
        public float punch = 1.015f;
        public float flashTime = 0.2f;
        public float backgroundDrift = 40f;
        [Tooltip("Degrees per second")]
        public float hueSpeed = 20f;
        public float glintEvery = 2.6f;

        [Header("Page sweep")]
        public Vector2 sweepSlab = new Vector2(1500f, 1700f);
        public float sweepTime = 0.4f;
        public float chaseDelay = 0.09f;

        [Header("Background")]
        public int shapeCount = 28;

        [Header("Themes")]
        public List<PersonaTheme> themes = new List<PersonaTheme>();
        public string title = "MENU";

        [Header("Start")]
        [Tooltip("Open the menu as soon as the game starts (like a title screen). Choose 'Play' to start.")]
        public bool openOnStart = true;

        // ---- runtime
        private class ItemView
        {
            public RectTransform rt;
            public PersonaShape slab;
            public PersonaShape shadow;
            public PersonaShape marker;
            public Text label;
            public float scale;
            public Coroutine routine;
        }

        private class ShapeView
        {
            public RectTransform rt;
            public PersonaShape graphic;
            public float speed;
            public float spin;
            public int slot;
        }

        private RectTransform canvasRect;
        private RectTransform menuRoot;
        private RectTransform content;
        private Image flash;
        private Text titleText;
        private Text titleShadow;
        private Text clockText;
        private Text hintText;
        private PersonaShape glint;
        private RectTransform glintRect;
        private PersonaShape sweepRed;
        private PersonaShape sweepBlack;
        private Font font;

        private readonly List<ItemView> views = new List<ItemView>();
        private readonly List<ShapeView> shapes = new List<ShapeView>();
        private int selected = 0;
        private int themeIndex = 0;
        private bool isOpen = false;
        private bool busy = false;
        private float hue = 0f;
        private Coroutine glintRoutine;

        public bool IsOpen { get { return isOpen; } }

        // ================================================================ setup

        void Awake()
        {
            var canvas = GetComponentInParent<Canvas>();
            canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : (RectTransform)transform;

#if UNITY_2022_2_OR_NEWER
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif

            if (themes == null || themes.Count == 0) themes = DefaultThemes();

            Stretch((RectTransform)transform);
            BuildUI();
            menuRoot.gameObject.SetActive(false);
        }

        private static List<PersonaTheme> DefaultThemes()
        {
            return new List<PersonaTheme>
            {
                new PersonaTheme { name = "Red", main = new Color(0.92f, 0.1f, 0.16f), dark = new Color(0.05f, 0.05f, 0.07f), text = Color.white },
                new PersonaTheme { name = "Blue", main = new Color(0.1f, 0.5f, 0.98f), dark = new Color(0.03f, 0.05f, 0.13f), text = Color.white },
                new PersonaTheme { name = "Green", main = new Color(0.12f, 0.78f, 0.38f), dark = new Color(0.03f, 0.1f, 0.05f), text = Color.white },
                new PersonaTheme { name = "Purple", main = new Color(0.68f, 0.28f, 0.95f), dark = new Color(0.08f, 0.03f, 0.13f), text = Color.white },
                new PersonaTheme { name = "Gold", main = new Color(1f, 0.78f, 0.15f), dark = new Color(0.12f, 0.09f, 0.02f), text = Color.white }
            };
        }

        private PersonaTheme Theme { get { return themes[themeIndex % themes.Count]; } }

        // ---- tiny UI helpers
        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Center(RectTransform rt, Vector2 size, Vector2 position)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
        }

        private PersonaShape NewShape(string name, Transform parent, PersonaShape.ShapeType type, bool blocksClicks)
        {
            var rt = NewRect(name, parent);
            var shape = rt.gameObject.AddComponent<PersonaShape>();
            shape.shape = type;
            shape.shearDegrees = shear;
            shape.raycastTarget = blocksClicks;
            return shape;
        }

        private Text NewText(string name, Transform parent, string text, int size, TextAnchor anchor)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyle.BoldAndItalic;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private void BuildUI()
        {
            menuRoot = NewRect("Menu", transform);
            Stretch(menuRoot);

            // Dark backdrop that also blocks clicks on the game below
            var dim = NewRect("Dim", menuRoot);
            Stretch(dim);
            var dimImage = dim.gameObject.AddComponent<Image>();
            dimImage.color = new Color(0f, 0f, 0f, 0.8f);

            // Everything that "punches" together
            content = NewRect("Content", menuRoot);
            Stretch(content);

            // ---- background shapes
            var shapesRoot = NewRect("Shapes", content);
            Stretch(shapesRoot);

            for (int i = 0; i < shapeCount; i++)
            {
                bool diamond = i % 2 == 0;
                var g = NewShape(diamond ? "Diamond" : "Wedge", shapesRoot, diamond ? PersonaShape.ShapeType.Diamond : PersonaShape.ShapeType.Wedge, false);

                float size = UnityEngine.Random.Range(50f, 230f);
                var view = new ShapeView
                {
                    rt = g.rectTransform,
                    graphic = g,
                    speed = UnityEngine.Random.Range(0.5f, 1.6f),
                    spin = UnityEngine.Random.Range(-12f, 12f),
                    slot = i % 3
                };

                Center(view.rt, diamond ? new Vector2(size, size) : new Vector2(size * 1.8f, size * 0.55f), RandomScreenPoint());
                view.rt.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
                shapes.Add(view);
            }

            // ---- title with a shadow and a glint
            var titleRoot = NewRect("Title", content);
            Center(titleRoot, new Vector2(760f, 170f), new Vector2(-560f, 400f));
            titleRoot.localRotation = Quaternion.Euler(0f, 0f, itemRotation);

            titleShadow = NewText("Shadow", titleRoot, title, 150, TextAnchor.MiddleLeft);
            Stretch(titleShadow.rectTransform);
            titleShadow.rectTransform.offsetMin = new Vector2(10f, -10f);
            titleShadow.rectTransform.offsetMax = new Vector2(10f, -10f);

            titleText = NewText("Text", titleRoot, title, 150, TextAnchor.MiddleLeft);
            Stretch(titleText.rectTransform);

            glint = NewShape("Glint", titleRoot, PersonaShape.ShapeType.Slab, false);
            glintRect = glint.rectTransform;
            Center(glintRect, new Vector2(60f, 170f), new Vector2(-600f, 0f));
            glint.color = new Color(1f, 1f, 1f, 0f);

            // ---- the items
            var itemsRoot = NewRect("Items", content);
            Stretch(itemsRoot);

            for (int i = 0; i < items.Count; i++)
            {
                var rt = NewRect("Item_" + i, itemsRoot);
                Center(rt, itemSize, new Vector2(firstItemPos.x + i * itemStep.x, -(firstItemPos.y + i * itemStep.y)));
                rt.localRotation = Quaternion.Euler(0f, 0f, itemRotation);

                var view = new ItemView { rt = rt };

                view.shadow = NewShape("Shadow", rt, PersonaShape.ShapeType.Slab, false);
                Stretch(view.shadow.rectTransform);
                view.shadow.rectTransform.offsetMin = new Vector2(12f, -12f);
                view.shadow.rectTransform.offsetMax = new Vector2(12f, -12f);

                view.slab = NewShape("Slab", rt, PersonaShape.ShapeType.Slab, true);
                Stretch(view.slab.rectTransform);

                view.marker = NewShape("Marker", rt, PersonaShape.ShapeType.Diamond, false);
                Center(view.marker.rectTransform, new Vector2(34f, 34f), new Vector2(-itemSize.x * 0.5f + 52f, 0f));

                view.label = NewText("Label", rt, items[i].label.ToUpper(), 44, TextAnchor.MiddleLeft);
                Stretch(view.label.rectTransform);
                view.label.rectTransform.offsetMin = new Vector2(96f, 0f);

                views.Add(view);
            }

            // ---- HUD: clock and hints
            clockText = NewText("Clock", content, "", 64, TextAnchor.UpperRight);
            clockText.supportRichText = true;
            clockText.rectTransform.anchorMin = new Vector2(1f, 1f);
            clockText.rectTransform.anchorMax = new Vector2(1f, 1f);
            clockText.rectTransform.pivot = new Vector2(1f, 1f);
            clockText.rectTransform.sizeDelta = new Vector2(700f, 160f);
            clockText.rectTransform.anchoredPosition = new Vector2(-60f, -40f);

            hintText = NewText("Hints", content, "W/S  SELECT      ENTER  OK      TAB / ESC  CLOSE", 26, TextAnchor.LowerLeft);
            hintText.rectTransform.anchorMin = new Vector2(0f, 0f);
            hintText.rectTransform.anchorMax = new Vector2(0f, 0f);
            hintText.rectTransform.pivot = new Vector2(0f, 0f);
            hintText.rectTransform.sizeDelta = new Vector2(1100f, 60f);
            hintText.rectTransform.anchoredPosition = new Vector2(60f, 40f);

            // ---- white flash on opening
            var flashRt = NewRect("Flash", menuRoot);
            Stretch(flashRt);
            flash = flashRt.gameObject.AddComponent<Image>();
            flash.color = new Color(1f, 1f, 1f, 0f);
            flash.raycastTarget = false;

            // ---- the page sweep lives OUTSIDE the menu, so it can cover the screen while the menu closes
            var sweeps = NewRect("Sweeps", transform);
            Stretch(sweeps);

            sweepRed = NewShape("SweepRed", sweeps, PersonaShape.ShapeType.Slab, false);
            Center(sweepRed.rectTransform, sweepSlab, new Vector2(-5000f, 0f));
            sweepRed.gameObject.SetActive(false);

            sweepBlack = NewShape("SweepBlack", sweeps, PersonaShape.ShapeType.Slab, false);
            Center(sweepBlack.rectTransform, sweepSlab, new Vector2(-5000f, 0f));
            sweepBlack.gameObject.SetActive(false);

            ApplyTheme();
        }

        private Vector2 RandomScreenPoint()
        {
            float w = canvasRect != null ? canvasRect.rect.width : 1920f;
            float h = canvasRect != null ? canvasRect.rect.height : 1080f;
            return new Vector2(UnityEngine.Random.Range(-w * 0.55f, w * 0.55f), UnityEngine.Random.Range(-h * 0.55f, h * 0.55f));
        }

        // ================================================================ theme

        public void NextTheme()
        {
            themeIndex = (themeIndex + 1) % themes.Count;
            ApplyTheme();
            StartCoroutine(FlashRoutine(0.12f));
        }

        private void ApplyTheme()
        {
            var t = Theme;

            foreach (var s in shapes)
            {
                Color c = s.slot == 0 ? t.main : (s.slot == 1 ? t.dark : t.text);
                c.a = s.slot == 2 ? 0.22f : 0.85f;
                s.graphic.color = c;
            }

            titleShadow.color = t.main;
            sweepRed.color = t.main;
            sweepBlack.color = t.dark;
            clockText.color = t.text;
            hintText.color = new Color(t.text.r, t.text.g, t.text.b, 0.75f);

            for (int i = 0; i < views.Count; i++) ApplyItemColors(i);
        }

        // Selected = inverted colours (white slab, themed text). The others stay dark and a bit dimmer.
        private void ApplyItemColors(int i)
        {
            var t = Theme;
            var v = views[i];
            bool isSelected = i == selected;

            v.slab.color = isSelected ? t.text : t.dark;
            v.label.color = isSelected ? t.main : t.text;
            v.marker.color = isSelected ? t.main : t.main;
            v.shadow.color = isSelected ? t.main : new Color(t.main.r, t.main.g, t.main.b, 0.7f);
        }

        void Start()
        {
            if (openOnStart) Open();
        }

        // ================================================================ open / close

        public void Toggle()
        {
            if (busy) return;
            if (isOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (views.Count == 0)
            {
                Debug.LogWarning("[MENU] The PersonaMenu has no items. Use 'Hero Management > Add Persona Menu' to build it already filled.");
                return;
            }

            if (isOpen || busy) return;

            isOpen = true;
            menuRoot.gameObject.SetActive(true);
            selected = 0;
            hue = 0f;
            ApplyTheme();

            // Items pop in one after the other, with a bounce
            for (int i = 0; i < views.Count; i++)
            {
                views[i].scale = 0f;
                views[i].rt.localScale = Vector3.zero;
                ScaleItem(views[i], i * stagger, TargetScale(i), 0.35f);
            }

            StartCoroutine(FlashRoutine(flashTime));
            StartCoroutine(PunchRoutine());

            if (glintRoutine != null) StopCoroutine(glintRoutine);
            glintRoutine = StartCoroutine(GlintLoop());
        }

        public void Close()
        {
            if (!isOpen) return;

            isOpen = false;
            foreach (var v in views)
            {
                if (v.routine != null) StopCoroutine(v.routine);
            }
            if (glintRoutine != null) StopCoroutine(glintRoutine);

            menuRoot.gameObject.SetActive(false);
        }

        private float TargetScale(int i) { return i == selected ? selectedScale : dimScale; }

        // ================================================================ selection

        private void SetSelected(int index)
        {
            if (views.Count == 0) return;

            index = (index % views.Count + views.Count) % views.Count;
            if (index == selected) return;

            selected = index;
            for (int i = 0; i < views.Count; i++)
            {
                ApplyItemColors(i);
                ScaleItem(views[i], 0f, TargetScale(i), 0.2f);
            }
        }

        private void Confirm()
        {
            if (busy || selected < 0 || selected >= items.Count) return;

            var item = items[selected];

            if (item.keepOpen)
            {
                StartCoroutine(FlashRoutine(0.12f));
                item.onSelect.Invoke();
                return;
            }

            // The page swaps while the screen is covered by the sweep
            StartCoroutine(TransitionRoutine(() =>
            {
                Close();
                item.onSelect.Invoke();
            }));
        }

        // ================================================================ per-frame

        void Update()
        {
            if (MenuInput.TogglePressed()) Toggle();
            if (!isOpen) return;

            float dt = Time.unscaledDeltaTime;

            // Background drift: diamonds and wedges slide towards the upper left
            float w = canvasRect != null ? canvasRect.rect.width : 1920f;
            float h = canvasRect != null ? canvasRect.rect.height : 1080f;
            Vector2 drift = new Vector2(-1f, 0.5f).normalized;

            foreach (var s in shapes)
            {
                s.rt.anchoredPosition += drift * backgroundDrift * s.speed * dt;
                s.rt.Rotate(0f, 0f, s.spin * dt);

                Vector2 p = s.rt.anchoredPosition;
                if (p.x < -w * 0.6f) p.x = w * 0.6f;
                if (p.y > h * 0.6f) p.y = -h * 0.6f;
                s.rt.anchoredPosition = p;
            }

            // Title colour slowly changes hue
            hue = Mathf.Repeat(hue + hueSpeed / 360f * dt, 1f);
            titleText.color = Color.HSVToRGB(hue, 0.18f, 1f);

            UpdateHud();

            if (busy) return;

            if (MenuInput.UpPressed()) SetSelected(selected - 1);
            if (MenuInput.DownPressed()) SetSelected(selected + 1);
            if (MenuInput.ConfirmPressed()) Confirm();
            if (MenuInput.CancelPressed()) Close();

            // Mouse: hover selects, click confirms
            if (isOpen)
            {
                Vector2 pointer = PointerInput.Position();
                int hover = -1;
                for (int i = 0; i < views.Count; i++)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(views[i].rt, pointer, null)) { hover = i; break; }
                }

                if (hover >= 0 && hover != selected) SetSelected(hover);
                if (hover >= 0 && PointerInput.ClickedThisFrame()) Confirm();
            }
        }

        private void UpdateHud()
        {
            var gm = GameManager.Instance;
            if (gm == null || clockText == null) return;

            clockText.text = $"DAY {gm.currentDay}\n<size=40>{gm.currentPhase.ToString().ToUpper()}   |   {gm.playerGold} G</size>";
        }

        // ================================================================ animation routines

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private static float EaseInOut(float t) { return t * t * (3f - 2f * t); }

        private void ScaleItem(ItemView v, float delay, float to, float duration)
        {
            if (v.routine != null) StopCoroutine(v.routine);
            v.routine = StartCoroutine(ScaleRoutine(v, delay, to, duration));
        }

        private IEnumerator ScaleRoutine(ItemView v, float delay, float to, float duration)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

            float from = v.scale;
            float t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                v.scale = Mathf.LerpUnclamped(from, to, EaseOutBack(Mathf.Clamp01(t / duration)));
                v.rt.localScale = Vector3.one * v.scale;
                yield return null;
            }

            v.scale = to;
            v.rt.localScale = Vector3.one * to;
        }

        private IEnumerator FlashRoutine(float duration)
        {
            flash.gameObject.SetActive(true);
            float t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                flash.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01(t / duration));
                yield return null;
            }

            flash.color = new Color(1f, 1f, 1f, 0f);
        }

        // A small "camera punch": the whole menu starts a bit zoomed in and settles
        private IEnumerator PunchRoutine()
        {
            float t = 0f;
            const float duration = 0.25f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 3f);
                content.localScale = Vector3.one * Mathf.Lerp(punch, 1f, e);
                yield return null;
            }

            content.localScale = Vector3.one;
        }

        // A white shine crosses the title every few seconds
        private IEnumerator GlintLoop()
        {
            while (isOpen)
            {
                yield return new WaitForSecondsRealtime(glintEvery);

                float t = 0f;
                const float duration = 0.45f;
                while (t < duration && isOpen)
                {
                    t += Time.unscaledDeltaTime;
                    float p = Mathf.Clamp01(t / duration);
                    glintRect.anchoredPosition = new Vector2(Mathf.Lerp(-420f, 420f, p), 0f);
                    glint.color = new Color(1f, 1f, 1f, Mathf.Sin(p * Mathf.PI) * 0.85f);
                    yield return null;
                }

                glint.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        // A red slab crosses the screen and a black one chases it. Halfway, while the screen is covered,
        // the page is swapped (whileCovered).
        private IEnumerator TransitionRoutine(Action whileCovered)
        {
            busy = true;

            float w = canvasRect != null ? canvasRect.rect.width : 1920f;
            float width = Mathf.Max(sweepSlab.x, w + 300f);
            sweepRed.rectTransform.sizeDelta = new Vector2(width, sweepSlab.y);
            sweepBlack.rectTransform.sizeDelta = new Vector2(width, sweepSlab.y);
            sweepRed.color = Theme.main;
            sweepBlack.color = Theme.dark;
            sweepRed.gameObject.SetActive(true);
            sweepBlack.gameObject.SetActive(true);

            float startX = -(w * 0.5f + width * 0.5f + 200f);
            float endX = w * 0.5f + width * 0.5f + 200f;

            float t = 0f;
            bool swapped = false;

            while (t < sweepTime + chaseDelay)
            {
                t += Time.unscaledDeltaTime;

                float pRed = Mathf.Clamp01(t / sweepTime);
                float pBlack = Mathf.Clamp01((t - chaseDelay) / sweepTime);

                sweepRed.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(startX, endX, EaseInOut(pRed)), 0f);
                sweepBlack.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(startX, endX, EaseInOut(pBlack)), 0f);

                if (!swapped && pBlack >= 0.5f)
                {
                    swapped = true;
                    if (whileCovered != null) whileCovered();
                }

                yield return null;
            }

            sweepRed.gameObject.SetActive(false);
            sweepBlack.gameObject.SetActive(false);
            busy = false;
        }
    }
}

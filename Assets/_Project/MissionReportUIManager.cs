using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HeroManagement
{
    // The mission report: appears when a party comes home (or is lost) and shows gold, loot,
    // who got wounded or died, and the experience bars filling up (with level-ups).
    public class MissionReportUIManager : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panel;
        public Text titleText;
        public Text summaryText;
        public Text lootText;

        [Header("Hero rows (up to 3)")]
        public GameObject[] rowObjects;
        public Text[] nameTexts;
        [Tooltip("The coloured bar inside each row's grey bar. Its right anchor is animated.")]
        public RectTransform[] barFills;
        public Text[] expTexts;

        [Header("Animation")]
        public float secondsPerSegment = 0.9f;

        private readonly Queue<MissionReport> queue = new Queue<MissionReport>();
        private MissionReport current;

        // One bar animation = a list of (from, to) segments; a level-up makes the bar fill, then restart
        private readonly List<Vector2>[] segments = { new List<Vector2>(), new List<Vector2>(), new List<Vector2>() };
        private float animTime;

        void OnEnable() { MissionDispatcher.OnMissionReport += Enqueue; }
        void OnDisable() { MissionDispatcher.OnMissionReport -= Enqueue; }

        void Start()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void Enqueue(MissionReport report)
        {
            if (report == null) return;

            queue.Enqueue(report);
            if (current == null) ShowNext();
        }

        private void ShowNext()
        {
            if (queue.Count == 0)
            {
                current = null;
                if (panel != null) panel.SetActive(false);
                return;
            }

            current = queue.Dequeue();
            Display(current);
        }

        private int ExpNeeded(int level)
        {
            return HeroProgressionManager.Instance != null ? HeroProgressionManager.Instance.GetExpNeededForLevel(level) : 100;
        }

        private void Display(MissionReport r)
        {
            if (panel == null) return;
            panel.SetActive(true);

            string region = r.contract != null ? r.contract.targetRegion.ToString() : "?";

            if (titleText != null)
            {
                if (r.partyWiped) titleText.text = $"<color=#FF5555>SPEDIZIONE PERDUTA</color> - {region}";
                else if (r.success) titleText.text = $"<color=#88FF88>MISSIONE RIUSCITA</color> - {region}";
                else titleText.text = $"<color=#FFAA44>MISSIONE FALLITA</color> - {region}";
            }

            if (summaryText != null)
            {
                string s = r.contract != null ? $"<b>{r.contract.clientName}</b>  ({r.contract.kingdomName})\n" : "";
                s += $"Tempo fuori: {r.daysAway} giorni\n\n";

                if (r.partyWiped) s += "Nessuno e' tornato. Un corvo portera' la notizia al mattino.";
                else if (r.success)
                    s += $"Ricompensa: <color=#FFD700>{r.rewardGold} oro</color> - arriva domani mattina\n(agli eroi: {r.heroShares} oro, alla gilda: {Mathf.Max(0, r.rewardGold - r.heroShares)} oro)";
                else s += "La missione e' fallita: nessuna ricompensa.";

                summaryText.text = s;
            }

            if (lootText != null)
            {
                string loot = "<b>Bottino:</b> ";
                if (!r.success || r.loot == null || r.loot.Count == 0) loot += "nessuno";
                else
                {
                    var parts = new List<string>();
                    foreach (var l in r.loot)
                    {
                        if (l != null && l.item != null) parts.Add($"{l.item.GetDisplayName()} x{l.amount}");
                    }
                    loot += string.Join(", ", parts);
                }

                if (r.heroes != null && r.heroes.Count > 3) loot += $"   (+{r.heroes.Count - 3} altri eroi non mostrati)";
                lootText.text = loot;
            }

            // Hero rows
            for (int i = 0; i < 3; i++)
            {
                segments[i].Clear();

                bool used = rowObjects != null && i < rowObjects.Length && r.heroes != null && i < r.heroes.Count;
                if (rowObjects != null && i < rowObjects.Length && rowObjects[i] != null) rowObjects[i].SetActive(used);
                if (!used) continue;

                var e = r.heroes[i];
                string status = e.died
                    ? "<color=#FF5555>MORTO</color>"
                    : (e.injury >= 50 ? $"<color=#FF8855>Ferite {e.injury}%</color>" : (e.injury > 0 ? $"Ferite {e.injury}%" : "<color=#88FF88>Illeso</color>"));
                if (e.gearBroke) status += " <color=#FFAA44>(equipaggiamento rotto)</color>";

                if (nameTexts != null && i < nameTexts.Length && nameTexts[i] != null)
                    nameTexts[i].text = $"<b>{(e.hero != null ? e.hero.heroName : "?")}</b>\n{status}";

                if (expTexts != null && i < expTexts.Length && expTexts[i] != null)
                {
                    string exp = e.died ? "-" : $"+{e.expGained} XP";
                    if (e.levelAfter > e.levelBefore) exp += $"\n<color=#FFD700>LIVELLO {e.levelAfter}!</color>";
                    expTexts[i].text = exp;
                }

                BuildSegments(i, e);
            }

            animTime = 0f;
            ApplyBars();
        }

        private void BuildSegments(int row, HeroReportEntry e)
        {
            if (e.died) return;

            for (int level = e.levelBefore; level < e.levelAfter; level++)
            {
                float from = level == e.levelBefore ? e.expBefore / (float)Mathf.Max(1, ExpNeeded(level)) : 0f;
                segments[row].Add(new Vector2(from, 1f));
            }

            float start = e.levelAfter > e.levelBefore ? 0f : e.expBefore / (float)Mathf.Max(1, ExpNeeded(e.levelBefore));
            float end = e.expAfter / (float)Mathf.Max(1, ExpNeeded(e.levelAfter));
            segments[row].Add(new Vector2(start, end));
        }

        void Update()
        {
            if (current == null) return;

            animTime += Time.unscaledDeltaTime;
            ApplyBars();
        }

        private void ApplyBars()
        {
            if (barFills == null) return;

            for (int i = 0; i < 3 && i < barFills.Length; i++)
            {
                if (barFills[i] == null) continue;

                var segs = segments[i];
                float value = 0f;

                if (segs.Count > 0)
                {
                    float position = animTime / Mathf.Max(0.1f, secondsPerSegment);
                    int index = Mathf.Clamp(Mathf.FloorToInt(position), 0, segs.Count - 1);
                    float t = Mathf.Clamp01(position - index);
                    if (position >= segs.Count) t = 1f;

                    value = Mathf.Lerp(segs[index].x, segs[index].y, t);
                }

                var max = barFills[i].anchorMax;
                barFills[i].anchorMax = new Vector2(Mathf.Clamp01(value), max.y);
            }
        }

        // Hook to the "Continue" button
        public void OnContinueClicked() { ShowNext(); }
    }
}

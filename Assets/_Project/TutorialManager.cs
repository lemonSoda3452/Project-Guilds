using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HeroManagement
{
    // Short tips for the first days. It watches the state of the game (no events needed) and shows
    // the tip that fits the moment, next to the objective, and writes it once in the log too.
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [Tooltip("The text of the top bar that shows the objective")]
        public Text hintText;
        public bool enableTips = true;

        private readonly HashSet<string> flags = new HashSet<string>();
        private string currentTip = null;
        private string lastObjective = null;
        private float timer = 0f;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public List<string> GetFlags() { return new List<string>(flags); }

        public void RestoreFlags(List<string> saved)
        {
            flags.Clear();
            if (saved != null)
            {
                foreach (var f in saved) flags.Add(f);
            }
            currentTip = null; // forces a refresh
        }

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < 0.5f) return;

            timer = 0f;
            Evaluate();
        }

        private void Evaluate()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            int guild = 0;
            bool anyHurt = false;
            bool anyLowLoyalty = false;

            if (gm.allHeroesDatabase != null)
            {
                foreach (var h in gm.allHeroesDatabase)
                {
                    if (h == null || h.isDead) continue;

                    guild++;
                    if (h.injury > 0 || h.restDaysLeft > 0) anyHurt = true;
                    if (h.loyalty < 50 && h.currentContract != EmploymentType.None) anyLowLoyalty = true;
                }
            }

            bool traveling = MissionDispatcher.Instance != null && MissionDispatcher.Instance.ongoingExpeditions.Count > 0;
            if (traveling) flags.Add("dispatched");
            if (flags.Contains("dispatched") && !traveling) flags.Add("returned");

            bool built = false;
            if (TavernFacilitiesManager.Instance != null && TavernFacilitiesManager.Instance.currentLevels != null)
            {
                foreach (int level in TavernFacilitiesManager.Instance.currentLevels)
                {
                    if (level > 0) built = true;
                }
            }
            if (built) flags.Add("built");

            string tip;
            if (guild == 0 && !flags.Contains("dispatched"))
                tip = "Click the counter (or press H) to hire your first hero. Heroes need a bed: you start with 3.";
            else if (!flags.Contains("dispatched"))
                tip = "Open the notice board, pick a contract, add heroes with 'Add' and press 'Send Party'. Yellow shows what the contract recommends.";
            else if (!flags.Contains("returned"))
                tip = "Press 'Next Phase' to let time pass. Gold arrives the morning after the mission ends; the party walks back home.";
            else if (anyLowLoyalty)
                tip = "A hero is losing loyalty: underpaid, wounded, idle too long or a companion died. Renew contracts and send restless heroes out.";
            else if (anyHurt)
                tip = "Wounded heroes rest in the infirmary. More Beds heal them faster; potions heal at once (Inventory).";
            else if (!flags.Contains("built") && gm.playerGold >= 700)
                tip = "Build in the Tavern menu: Beds (more heroes), Brewery and Kitchen (daily income), Forge (weapons and repairs).";
            else
                tip = "";

            if (!enableTips) tip = "";

            string objective = GameFlowManager.Instance != null ? GameFlowManager.Instance.GetObjectiveText() : "";
            bool tipChanged = tip != currentTip;
            if (!tipChanged && objective == lastObjective) return;

            currentTip = tip;
            lastObjective = objective;

            if (hintText != null)
            {
                hintText.supportRichText = true;
                hintText.text = tip.Length > 0 ? objective + "   |   <color=#FFD27F>TIP:</color> " + tip : objective;
            }

            if (tipChanged && tip.Length > 0 && SystemLogUIManager.Instance != null) SystemLogUIManager.Instance.LogMessage("[TIP] " + tip);
        }
    }
}

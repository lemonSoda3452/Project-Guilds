using UnityEngine;
using UnityEngine.UI;

namespace HeroManagement
{
    // Tavern recruitment panel: browse the visitors, pick the contract terms, hire
    public class RecruitmentUIManager : MonoBehaviour
    {
        [Header("Texts")]
        public Text visitorText;
        public Text costText;

        [Header("Stat Chart (radar)")]
        [Tooltip("Visualizes the stats of the mercenary you are inspecting")]
        public RadarChart heroChart;
        public float chartMax = 20f;

        [Header("Optional")]
        [Tooltip("Refreshed after a hire so the roster list updates")]
        public TavernUIManager tavernUI;

        private struct ContractOption
        {
            public EmploymentType type;
            public int duration;
            public string label;
        }

        private readonly ContractOption[] options =
        {
            new ContractOption { type = EmploymentType.MissionBased, duration = 3,  label = "3 missions" },
            new ContractOption { type = EmploymentType.MissionBased, duration = 6,  label = "6 missions" },
            new ContractOption { type = EmploymentType.TimeBased,    duration = 30, label = "1 month" },
            new ContractOption { type = EmploymentType.TimeBased,    duration = 60, label = "2 months" },
            new ContractOption { type = EmploymentType.Permanent,    duration = 0,  label = "Permanent" },
        };

        private readonly int[] shareOptions = { 10, 20, 30, 40 };

        private int visitorIndex = 0;
        private int optionIndex = 0;
        private int shareIndex = 1;
        private bool freeEquipment = false;

        void OnEnable()
        {
            RecruitmentManager.OnVisitorsChanged += Refresh;
            GameManager.OnGoldChanged += HandleGoldChanged;
        }

        void OnDisable()
        {
            RecruitmentManager.OnVisitorsChanged -= Refresh;
            GameManager.OnGoldChanged -= HandleGoldChanged;
        }

        void Start() { Refresh(); }

        private void HandleGoldChanged(int gold) { Refresh(); }

        private HeroData CurrentVisitor()
        {
            if (RecruitmentManager.Instance == null) return null;

            var visitors = RecruitmentManager.Instance.currentTavernVisitors;
            if (visitors == null || visitors.Count == 0) return null;

            visitorIndex = Mathf.Clamp(visitorIndex, 0, visitors.Count - 1);
            return visitors[visitorIndex];
        }

        public void Refresh()
        {
            var hero = CurrentVisitor();

            if (hero == null)
            {
                if (visitorText != null) visitorText.text = "La taverna è vuota per oggi.";
                if (costText != null) costText.text = "";
                if (heroChart != null) heroChart.SetValues(new float[HeroStats.Count], chartMax);
                return;
            }

            var option = options[optionIndex];
            int share = shareOptions[shareIndex];
            int cost = RecruitmentManager.Instance.CalculateHireCost(hero, option.type, option.duration, freeEquipment, share);
            bool canAfford = GameManager.Instance != null && GameManager.Instance.playerGold >= cost;

            if (visitorText != null)
            {
                string info = $"<b>{hero.GetFormattedName()}</b> - {hero.personality}\n";
                info += $"<color=#CCCCCC>Fama:</color> {hero.fameTier} | <color=#CCCCCC>Età:</color> {hero.age} | <color=#CCCCCC>Cavallo:</color> {hero.mount}\n\n";
                info += $"<color=#FFAA44>Power Base:</color> {hero.GetPower()} | <color=#FFAA44>Stipendio:</color> ~{hero.GetExpectedPayPerMission()} Oro/Miss\n";
                info += $"<color=#FFAA44>Cibo e Alloggio:</color> {hero.dailyUpkeepCost} Oro/Giorno\n";
                visitorText.text = info;
            }

            if (heroChart != null)
            {
                heroChart.SetValues(hero.GetStats().ToArray(), chartMax);
            }

            if (costText != null)
            {
                string costInfo = $"<b>Contratto:</b> {option.type} ({option.label})\n";
                costInfo += $"<b>Taglia sulle Missioni:</b> {share}%\n";
                costInfo += $"<b>Armatura a spese della Gilda:</b> {(freeEquipment ? "<color=green>SI</color>" : "<color=red>NO</color>")}\n";

                string costColor = canAfford ? "#00FF00" : "#FF0000";
                costInfo += $"\n<size=22>Costo Assunzione: <color={costColor}>{cost} Oro</color></size>";

                costText.text = costInfo;
            }
        }

        // ---- Button handlers
        public void OnNextVisitorClicked()
        {
            if (RecruitmentManager.Instance == null) return;

            int count = RecruitmentManager.Instance.currentTavernVisitors.Count;
            if (count == 0) return;

            visitorIndex = (visitorIndex + 1) % count;
            Refresh();
        }

        public void OnCycleContractClicked()
        {
            optionIndex = (optionIndex + 1) % options.Length;
            Refresh();
        }

        public void OnCycleShareClicked()
        {
            shareIndex = (shareIndex + 1) % shareOptions.Length;
            Refresh();
        }

        public void OnToggleEquipmentClicked()
        {
            freeEquipment = !freeEquipment;
            Refresh();
        }

        public void OnHireClicked()
        {
            var hero = CurrentVisitor();
            if (hero == null || RecruitmentManager.Instance == null) return;

            var option = options[optionIndex];
            RecruitmentManager.Instance.HireHero(hero, option.type, option.duration, freeEquipment, shareOptions[shareIndex]);

            if (tavernUI != null) tavernUI.RefreshRosterUI();
            Refresh();
        }
    }
}

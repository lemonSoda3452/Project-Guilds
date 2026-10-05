using UnityEngine;

namespace HeroManagement
{
    // The guild's reputation. Successes (especially hard ones) raise it, failures and dead heroes lower it.
    // A famous guild attracts famous mercenaries, gets more ravens and pays less to hire.
    public class ReputationManager : MonoBehaviour
    {
        public static ReputationManager Instance { get; private set; }

        [Header("Progress")]
        public int reputation = 0;

        [Header("Tiers")]
        [Tooltip("Points needed for each tier (the first one must be 0)")]
        public int[] tierThresholds = { 0, 100, 300, 700, 1500 };
        public string[] tierNames = { "Sconosciuta", "Locale", "Rispettata", "Famosa", "Leggendaria" };

        [Header("Gains and losses")]
        public int successBase = 10;
        [Tooltip("Extra points per point of mission difficulty (the contract penalty)")]
        public float perDifficultyPoint = 0.5f;
        public int towerBonusPerTenFloors = 2;
        public int failurePenalty = 5;
        public int deathPenalty = 15;
        public int wipePenalty = 10;

        [Header("Effects")]
        [Tooltip("Cheaper hiring per tier (0.04 = -4%)")]
        public float hireDiscountPerTier = 0.04f;

        public static event System.Action OnReputationChanged;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void OnEnable() { MissionDispatcher.OnMissionReport += HandleReport; }
        void OnDisable() { MissionDispatcher.OnMissionReport -= HandleReport; }

        private void Log(string message)
        {
            if (SystemLogUIManager.Instance != null) SystemLogUIManager.Instance.LogMessage(message);
        }

        public int Tier
        {
            get
            {
                for (int i = tierThresholds.Length - 1; i >= 0; i--)
                {
                    if (reputation >= tierThresholds[i]) return i;
                }
                return 0;
            }
        }

        public string TierName
        {
            get
            {
                int t = Tier;
                return t < tierNames.Length ? tierNames[t] : "Tier " + t;
            }
        }

        // Unknown mercenaries come to everybody, local heroes to a Local guild, veterans to a Respected one...
        public bool CanAttract(FameTier fame) { return (int)fame <= Tier; }

        public float GetHireCostMultiplier() { return Mathf.Max(0.7f, 1f - hireDiscountPerTier * Tier); }

        // More ravens when the guild is Respected (+1) and Legendary (+2)
        public int GetRavenBonus() { return Tier >= 4 ? 2 : (Tier >= 2 ? 1 : 0); }

        public string GetStatusText()
        {
            return $"Reputazione: {TierName} ({reputation} pt)";
        }

        private void HandleReport(MissionReport report)
        {
            if (report == null || report.contract == null) return;

            int delta = 0;

            if (report.success)
            {
                delta += successBase + Mathf.RoundToInt(Mathf.Abs(report.contract.GetDifficultyPenalty()) * perDifficultyPoint);
                if (report.contract.isTowerRaid) delta += (report.contract.towerFloors / 10) * towerBonusPerTenFloors;
            }
            else if (!report.partyWiped)
            {
                delta -= failurePenalty;
            }

            if (report.heroes != null)
            {
                foreach (var h in report.heroes)
                {
                    if (h != null && h.died) delta -= deathPenalty;
                }
            }

            if (report.partyWiped) delta -= wipePenalty;

            Add(delta);
        }

        public void Add(int amount)
        {
            if (amount == 0) return;

            int tierBefore = Tier;
            reputation = Mathf.Max(0, reputation + amount);

            Log($"[REPUTATION] {(amount > 0 ? "+" : "")}{amount} ({reputation} pt)");
            if (Tier > tierBefore) Log($"[REPUTATION] The guild is now {TierName}! Better mercenaries and more ravens will come.");
            else if (Tier < tierBefore) Log($"[REPUTATION] The guild's name fell to {TierName}...");

            OnReputationChanged?.Invoke();
        }

        public void Restore(int saved)
        {
            reputation = Mathf.Max(0, saved);
            OnReputationChanged?.Invoke();
        }
    }
}

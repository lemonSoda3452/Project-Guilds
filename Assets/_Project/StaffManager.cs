using System.Collections.Generic;
using UnityEngine;

namespace HeroManagement
{
    // The four standard heroes, who run the tavern: the Witch (scrolls, potions, potential), the Butler (beds, food, mail),
    // the Blacksmith (the forge) and the Barmaid (beer and food sales). They are weak, cannot level much and,
    // in Normal mode, cannot die. In Permadeath they can die: then a replacement must be paid every month.
    public class StaffManager : MonoBehaviour
    {
        public static StaffManager Instance { get; private set; }

        [Header("Standard party")]
        public List<HeroData> standardHeroes = new List<HeroData>();

        [Header("Replacements (Permadeath)")]
        public int monthlyReplacementCost = 90;
        public int daysPerMonth = 30;

        [Header("Effects while the role is covered")]
        [Range(0f, 0.5f)] public float butlerUpkeepDiscount = 0.10f;
        [Range(0f, 0.5f)] public float butlerRecoveryBonus = 0.10f;
        public int butlerMailExtraDays = 1;
        [Tooltip("Without the Barmaid the brewery and the kitchen earn only this fraction")]
        [Range(0f, 1f)] public float incomeWithoutBarmaid = 0.5f;
        [Tooltip("Potions heal this much more per Witch level above 1")]
        public int potionBonusPerWitchLevel = 5;

        public static event System.Action OnStaffChanged;

        private readonly Dictionary<StandardRole, int> paidUntil = new Dictionary<StandardRole, int>();
        private readonly HashSet<StandardRole> warned = new HashSet<StandardRole>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void OnEnable() { GameManager.OnDayChanged += HandleNewDay; }
        void OnDisable() { GameManager.OnDayChanged -= HandleNewDay; }

        void Start() { EnsureStandardParty(); }

        private void Log(string message)
        {
            if (SystemLogUIManager.Instance != null) SystemLogUIManager.Instance.LogMessage(message);
        }

        private int CurrentDay() { return GameManager.Instance != null ? GameManager.Instance.currentDay : 1; }

        // The standard heroes are part of the guild from day 1: permanent contract, no wage, no cut of the rewards
        public void EnsureStandardParty()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.allHeroesDatabase == null) gm.allHeroesDatabase = new List<HeroData>();

            foreach (var hero in standardHeroes)
            {
                if (hero == null) continue;

                hero.currentContract = EmploymentType.Permanent;
                hero.contractDurationLeft = 0;
                hero.loyalty = 100;
                hero.agreedSharePercent = 0;

                if (!gm.allHeroesDatabase.Contains(hero)) gm.allHeroesDatabase.Add(hero);
            }
        }

        // ------------------------------------------------------------ who is working?

        public HeroData GetHero(StandardRole role)
        {
            foreach (var h in standardHeroes)
            {
                if (h != null && h.standardRole == role) return h;
            }
            return null;
        }

        public bool IsHeroPresent(StandardRole role)
        {
            var h = GetHero(role);
            return h != null && !h.isDead && !h.isBusy;
        }

        public bool HasReplacement(StandardRole role)
        {
            var h = GetHero(role);
            int until;
            return h != null && h.isDead && paidUntil.TryGetValue(role, out until) && until >= CurrentDay();
        }

        // The role is covered if the hero is at the tavern, or if he died and a replacement is paid
        public bool IsActive(StandardRole role) { return IsHeroPresent(role) || HasReplacement(role); }

        // ------------------------------------------------------------ effects

        public float GetUpkeepMultiplier() { return IsActive(StandardRole.Butler) ? 1f - butlerUpkeepDiscount : 1f; }
        public float GetRecoveryBonus() { return IsActive(StandardRole.Butler) ? butlerRecoveryBonus : 0f; }
        public float GetIncomeMultiplier() { return IsActive(StandardRole.Barmaid) ? 1f : incomeWithoutBarmaid; }
        public bool ForgeWorking() { return IsActive(StandardRole.Blacksmith); }
        public bool WitchAvailable() { return IsActive(StandardRole.Witch); }
        public int GetPostedDaysBonus() { return IsActive(StandardRole.Butler) ? butlerMailExtraDays : 0; }

        public int GetPotionBonus()
        {
            var witch = GetHero(StandardRole.Witch);
            if (witch == null || witch.isDead) return 0;
            return Mathf.Max(0, witch.level - 1) * potionBonusPerWitchLevel;
        }

        // ------------------------------------------------------------ monthly replacements

        private void HandleNewDay(int day)
        {
            foreach (var hero in standardHeroes)
            {
                if (hero == null || !hero.isDead) continue;

                StandardRole role = hero.standardRole;
                int until;
                paidUntil.TryGetValue(role, out until);
                if (until >= day) continue; // the current month is already paid

                int fee = Mathf.RoundToInt(monthlyReplacementCost * GameModeManager.CostMult());

                if (EconomyManager.Instance != null && EconomyManager.Instance.TrySpendGold(fee))
                {
                    paidUntil[role] = day + daysPerMonth - 1;
                    warned.Remove(role);
                    Log($"[STAFF] A new {role} takes {hero.heroName}'s place for a month ({fee} gold).");
                    OnStaffChanged?.Invoke();
                }
                else if (warned.Add(role))
                {
                    Log($"[STAFF] The {role} is gone and you cannot afford a replacement ({fee} gold): their work has stopped.");
                    OnStaffChanged?.Invoke();
                }
            }
        }

        // ---- save / load
        public int[] GetPaidUntil()
        {
            var result = new int[5];
            foreach (var pair in paidUntil)
            {
                int index = (int)pair.Key;
                if (index >= 0 && index < result.Length) result[index] = pair.Value;
            }
            return result;
        }

        public void RestorePaidUntil(int[] saved)
        {
            paidUntil.Clear();
            warned.Clear();
            if (saved != null)
            {
                for (int i = 1; i < saved.Length; i++)
                {
                    if (saved[i] > 0) paidUntil[(StandardRole)i] = saved[i];
                }
            }
            OnStaffChanged?.Invoke();
        }
    }
}

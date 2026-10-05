using System.Collections.Generic;
using UnityEngine;

namespace HeroManagement
{
    // Keep the order: used as an index. Add new types only at the END.
    public enum FacilityType { Beds, Brewery, Kitchen, Forge }

    // The tavern's buildings, paid with gold:
    //   Beds    -> more heroes in the guild + faster recovery for the wounded
    //   Brewery -> daily income from beer
    //   Kitchen -> daily income from food + discount on the heroes' upkeep
    //   Forge   -> unlocks forging (see ForgeManager), repairs and cheaper repairs
    public class TavernFacilitiesManager : MonoBehaviour
    {
        public static TavernFacilitiesManager Instance { get; private set; }

        [System.Serializable]
        public class FacilityConfig
        {
            public FacilityType type;
            public string displayName;
            [TextArea(1, 3)] public string description;
            [Tooltip("Gold needed to reach level 1, 2, 3... (the length of this list is the max level)")]
            public int[] upgradeCosts = { 300, 800, 2000 };

            public int MaxLevel { get { return upgradeCosts != null ? upgradeCosts.Length : 0; } }
        }

        [Header("Facilities")]
        [Tooltip("Leave empty to use the default set (Beds, Brewery, Kitchen, Forge)")]
        public List<FacilityConfig> facilities = new List<FacilityConfig>();
        [Tooltip("Level of each facility at the start, in this order: Beds, Brewery, Kitchen, Forge")]
        public int[] startingLevels = new int[4];
        [Tooltip("Current levels (runtime)")]
        public int[] currentLevels = new int[4];

        [Header("Beds")]
        public int baseBeds = 3;
        public int bedsPerLevel = 2;
        [Tooltip("Faster recovery per level (0.25 = +25%)")]
        public float recoveryBonusPerLevel = 0.25f;

        [Header("Products")]
        public int breweryIncomePerLevel = 35;
        public int kitchenIncomePerLevel = 25;
        [Tooltip("Upkeep discount per Kitchen level (0.05 = -5%)")]
        public float kitchenUpkeepDiscountPerLevel = 0.05f;
        [Range(0f, 0.9f)] public float incomeVariance = 0.2f;

        [Header("Forge")]
        [Tooltip("Cheaper repairs per Forge level (0.15 = -15%)")]
        public float repairDiscountPerLevel = 0.15f;

        public static event System.Action OnFacilitiesChanged;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            EnsureDefaultConfig();

            currentLevels = new int[System.Enum.GetValues(typeof(FacilityType)).Length];
            for (int i = 0; i < currentLevels.Length; i++)
            {
                int start = (startingLevels != null && i < startingLevels.Length) ? startingLevels[i] : 0;
                currentLevels[i] = Mathf.Max(0, start);
            }
        }

        void OnEnable() { GameManager.OnDayChanged += HandleNewDay; }
        void OnDisable() { GameManager.OnDayChanged -= HandleNewDay; }

        private void EnsureDefaultConfig()
        {
            if (facilities != null && facilities.Count > 0) return;

            facilities = new List<FacilityConfig>
            {
                new FacilityConfig { type = FacilityType.Beds, displayName = "Letti",
                    description = "Più letti = più eroi in gilda e un riposo più veloce per i feriti.",
                    upgradeCosts = new[] { 300, 800, 2000 } },
                new FacilityConfig { type = FacilityType.Brewery, displayName = "Birreria",
                    description = "Produce birra: entrate ogni giorno.",
                    upgradeCosts = new[] { 400, 900, 2200 } },
                new FacilityConfig { type = FacilityType.Kitchen, displayName = "Cucina",
                    description = "Cibo per clienti ed eroi: entrate ogni giorno e sconto sul mantenimento.",
                    upgradeCosts = new[] { 350, 800, 2000 } },
                new FacilityConfig { type = FacilityType.Forge, displayName = "Fucina",
                    description = "Forgia armi e armature con i minerali, ripara l'equipaggiamento.",
                    upgradeCosts = new[] { 600, 1500, 3500 } },
            };
        }

        private void Log(string message)
        {
            if (SystemLogUIManager.Instance != null) SystemLogUIManager.Instance.LogMessage(message);
        }

        // ------------------------------------------------------------ levels & upgrades

        public int GetLevel(FacilityType type)
        {
            int i = (int)type;
            return (currentLevels != null && i >= 0 && i < currentLevels.Length) ? currentLevels[i] : 0;
        }

        public FacilityConfig GetConfig(FacilityType type)
        {
            if (facilities == null) return null;
            return facilities.Find(f => f != null && f.type == type);
        }

        public int GetMaxLevel(FacilityType type)
        {
            var cfg = GetConfig(type);
            return cfg != null ? cfg.MaxLevel : 0;
        }

        // Gold for the next level, or -1 if the facility is at its maximum
        public int GetNextCost(FacilityType type)
        {
            var cfg = GetConfig(type);
            int level = GetLevel(type);
            if (cfg == null || cfg.upgradeCosts == null || level >= cfg.MaxLevel) return -1;
            return cfg.upgradeCosts[level];
        }

        public bool TryUpgrade(FacilityType type)
        {
            var cfg = GetConfig(type);
            int cost = GetNextCost(type);

            if (cfg == null || cost < 0)
            {
                Log("This building is already at its maximum level.");
                return false;
            }

            if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpendGold(cost))
            {
                Log($"Not enough gold to improve the {cfg.displayName} ({cost} gold).");
                return false;
            }

            currentLevels[(int)type]++;
            Log($"The {cfg.displayName} is now level {currentLevels[(int)type]}!");
            OnFacilitiesChanged?.Invoke();
            return true;
        }

        // ------------------------------------------------------------ effects

        public bool HasForge { get { return GetLevel(FacilityType.Forge) >= 1; } }

        public int GetBedCapacity() { return baseBeds + bedsPerLevel * GetLevel(FacilityType.Beds); }

        public int GetUsedBeds()
        {
            int used = 0;
            if (GameManager.Instance != null && GameManager.Instance.allHeroesDatabase != null)
            {
                foreach (var h in GameManager.Instance.allHeroesDatabase)
                {
                    if (h != null && !h.isDead) used++;
                }
            }
            return used;
        }

        public bool HasFreeBed() { return GetUsedBeds() < GetBedCapacity(); }

        public float GetRecoveryMultiplier() { return 1f + recoveryBonusPerLevel * GetLevel(FacilityType.Beds); }

        public float GetUpkeepMultiplier()
        {
            return Mathf.Max(0.5f, 1f - kitchenUpkeepDiscountPerLevel * GetLevel(FacilityType.Kitchen));
        }

        public float GetRepairMultiplier()
        {
            return Mathf.Max(0.3f, 1f - repairDiscountPerLevel * GetLevel(FacilityType.Forge));
        }

        // Average daily income (the real one varies a little)
        public int GetDailyIncome()
        {
            return GetLevel(FacilityType.Brewery) * breweryIncomePerLevel + GetLevel(FacilityType.Kitchen) * kitchenIncomePerLevel;
        }

        public string GetEffectText(FacilityType type, int level)
        {
            switch (type)
            {
                case FacilityType.Beds:
                    return $"{baseBeds + bedsPerLevel * level} letti | riposo x{1f + recoveryBonusPerLevel * level:0.00}";
                case FacilityType.Brewery:
                    return level == 0 ? "Non costruita" : $"~{breweryIncomePerLevel * level} oro/giorno";
                case FacilityType.Kitchen:
                    return level == 0 ? "Non costruita" : $"~{kitchenIncomePerLevel * level} oro/giorno | mantenimento -{kitchenUpkeepDiscountPerLevel * level * 100f:0}%";
                case FacilityType.Forge:
                    return level == 0 ? "Non costruita" : $"{level} fornace/i | riparazioni -{repairDiscountPerLevel * level * 100f:0}%";
                default:
                    return "";
            }
        }

        public void RestoreLevels(int[] levels)
        {
            if (levels != null)
            {
                for (int i = 0; i < currentLevels.Length && i < levels.Length; i++) currentLevels[i] = Mathf.Max(0, levels[i]);
            }
            OnFacilitiesChanged?.Invoke();
        }

        // ------------------------------------------------------------ daily income

        private void HandleNewDay(int day)
        {
            int income = RollIncome(GetLevel(FacilityType.Brewery) * breweryIncomePerLevel)
                       + RollIncome(GetLevel(FacilityType.Kitchen) * kitchenIncomePerLevel);

            if (income <= 0) return;

            if (EconomyManager.Instance != null) EconomyManager.Instance.AddGold(income);
            Log($"[TAVERN] Beer and food sales brought in {income} gold.");
        }

        private int RollIncome(int baseIncome)
        {
            if (baseIncome <= 0) return 0;
            return Mathf.Max(0, Mathf.RoundToInt(baseIncome * Random.Range(1f - incomeVariance, 1f + incomeVariance)));
        }
    }
}

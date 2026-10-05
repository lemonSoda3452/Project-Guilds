using UnityEngine;

namespace HeroManagement
{
    public class HeroProgressionManager : MonoBehaviour
    {
        public static HeroProgressionManager Instance { get; private set; }

        [Header("Leveling Curve")]
        [Tooltip("Base EXP required to reach Level 2")]
        public int baseExpNeeded = 100;
        [Tooltip("Multiplier for each subsequent level (e.g., 1.5x)")]
        public float expMultiplierPerLevel = 1.5f;
        public int maxLevel = 30;

        [Header("Level Up Rewards")]
        [Tooltip("Stat points handed out at each level-up, spread over the stats the hero's class cares about")]
        public int statPointsPerLevel = 3;
        [Tooltip("Heroes get greedier as they grow")]
        public int upkeepPerLevel = 2;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void GrantExperience(HeroData hero, int amount)
        {
            if (hero == null || hero.isDead || amount <= 0) return;

            hero.currentExp += amount;
            Debug.Log($"[PROGRESSION] {hero.GetFormattedName()} gained {amount} EXP.");

            CheckLevelUp(hero);
        }

        private void CheckLevelUp(HeroData hero)
        {
            while (hero.level < maxLevel)
            {
                int needed = GetExpNeededForLevel(hero.level);
                if (hero.currentExp < needed) break;

                hero.currentExp -= needed;
                hero.level++;

                // Stats go into levelBonusStats and are undone by ResetRuntimeState
                hero.GrowStats(statPointsPerLevel);
                hero.dailyUpkeepCost += upkeepPerLevel;
                hero.levelsGained++;
                hero.bonusUpkeepGained += upkeepPerLevel;

                Debug.Log($"[PROGRESSION] {hero.heroName} leveled up to Level {hero.level}!");

                if (SystemLogUIManager.Instance != null)
                {
                    SystemLogUIManager.Instance.LogMessage($"{hero.heroName} is now Level {hero.level}! Upkeep increased.");
                }
            }

            if (hero.level >= maxLevel) hero.currentExp = 0;
        }

        public int GetExpNeededForLevel(int currentLevel)
        {
            // Never 0 or negative: that would make the level-up loop spin forever
            float multiplier = Mathf.Max(1f, expMultiplierPerLevel);
            return Mathf.Max(1, Mathf.RoundToInt(baseExpNeeded * Mathf.Pow(multiplier, currentLevel - 1)));
        }
    }
}

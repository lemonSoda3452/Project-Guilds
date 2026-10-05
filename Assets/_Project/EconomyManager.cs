using UnityEngine;
using System.Collections.Generic;

namespace HeroManagement
{
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [Header("Mission Balance")]
        public int baseSuccessChance = 40;
        public int successPerHero = 4;
        public int preferredClassBonus = 10;
        public int requiredSkillBonus = 12;
        public int missingSkillPenalty = 8;
        public int temporaryPotentialBonus = 10;
        public int permanentPotentialBonus = 5;

        [Header("Party Power")]
        [Tooltip("Success points per point of (party power - contract recommended power)")]
        public float powerWeight = 0.5f;
        public int maxPowerModifier = 25;

        [Header("Stat Profile (the radar chart)")]
        [Tooltip("Penalty per missing stat point compared to the contract's recommended party stats")]
        public float statDeficitWeight = 0.5f;
        public float statExcessWeight = 0.2f;
        public int maxStatPenalty = 25;
        public int maxStatBonus = 10;

        [Header("Gear")]
        public float gearBonusWeight = 0.5f;
        public int maxGearBonus = 10;

        [Header("Party Chemistry")]
        public int hatedPairPenalty = 5;
        public int favoredPairBonus = 3;
        public int lowLoyaltyThreshold = 30;
        public int lowLoyaltyPenalty = 3;

        [Header("Injuries")]
        public float baseInjury = 6f;
        public float failureInjury = 25f;

        [Header("Death Risk (on failure)")]
        public int baseDeathRisk = 30;

        [Header("Payments")]
        [Tooltip("The heroes can never take more than this share of a reward")]
        [Range(0.1f, 1f)] public float maxHeroShareOfReward = 0.5f;

        [Header("Experience")]
        public int successExpBase = 30;
        public int successExpPerDay = 10;
        public int failExp = 10;

        // The treasury lives in the GameManager (so the UI stays in sync)
        public int Gold => GameManager.Instance != null ? GameManager.Instance.playerGold : 0;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void AddGold(int amount)
        {
            if (GameManager.Instance != null) GameManager.Instance.AddGold(amount);
        }

        public bool TrySpendGold(int amount)
        {
            return GameManager.Instance != null && GameManager.Instance.TrySpendGold(amount);
        }

        private void Log(string message)
        {
            if (SystemLogUIManager.Instance != null) SystemLogUIManager.Instance.LogMessage(message);
        }

        // XP given during the mission being resolved (for the mission report)
        private readonly Dictionary<HeroData, int> expLog = new Dictionary<HeroData, int>();

        private void GrantExp(HeroData hero, int amount)
        {
            if (HeroProgressionManager.Instance != null)
                HeroProgressionManager.Instance.GrantExperience(hero, amount);

            int total;
            expLog.TryGetValue(hero, out total);
            expLog[hero] = total + amount;
        }

        // Resolves a mission the moment the party reaches its destination.
        // NOTE: gold is NOT paid here: the MissionDispatcher delivers it the next morning,
        // and the heroes (survivors) stay busy while they travel home.
        public MissionOutcome ResolveMission(ContractData contract, List<HeroData> heroes)
        {
            if (contract == null || heroes == null || heroes.Count == 0) return null;

            List<HeroData> living = new List<HeroData>();
            foreach (var h in heroes)
            {
                if (h != null && !h.isDead) living.Add(h);
            }
            if (living.Count == 0) return null;

            MissionOutcome outcome = new MissionOutcome();

            // Remember where everybody was, to show it in the mission report
            expLog.Clear();
            var before = new Dictionary<HeroData, int[]>();
            foreach (var h0 in living) before[h0] = new[] { h0.level, h0.currentExp };

            // ---- Gather party data
            int partyPower = 0;
            bool classMatch = false;
            bool skillMatch = false;
            int potentialBonus = 0;
            int loyaltyPenalty = 0;
            float gearTotal = 0f;
            HeroStats partyStats = new HeroStats();

            foreach (var hero in living)
            {
                partyPower += hero.GetPower();
                partyStats = partyStats + hero.GetStats();
                gearTotal += hero.GetGearRating();

                if (contract.hasPreferredClass && hero.heroClass == contract.preferredClass) classMatch = true;
                if (hero.HasAptitudeFor(contract.primarySkillRequired)) skillMatch = true;
                if (hero.hasTemporaryPotential) potentialBonus += temporaryPotentialBonus;
                if (hero.hasPermanentPotential) potentialBonus += permanentPotentialBonus;
                if (hero.loyalty < lowLoyaltyThreshold) loyaltyPenalty += lowLoyaltyPenalty;
            }

            int chemistry = 0;
            foreach (var a in living)
            {
                foreach (var b in living)
                {
                    if (a == b) continue;
                    if (a.hatedCompanions != null && a.hatedCompanions.Contains(b)) chemistry -= hatedPairPenalty;
                    if (a.favoredCompanions != null && a.favoredCompanions.Contains(b)) chemistry += favoredPairBonus;
                }
            }

            // Stat profile: how close is the party to the contract's recommended stats?
            int statMod = 0;
            if (contract.recommendedPartyStats.Total > 0)
            {
                float deficit = 0f;
                float excess = 0f;
                for (int i = 0; i < HeroStats.Count; i++)
                {
                    int recommended = contract.recommendedPartyStats.Get((StatType)i);
                    if (recommended <= 0) continue;

                    int have = partyStats.Get((StatType)i);
                    if (have < recommended) deficit += recommended - have;
                    else excess += have - recommended;
                }
                statMod = Mathf.RoundToInt(Mathf.Clamp(-deficit * statDeficitWeight + excess * statExcessWeight, -maxStatPenalty, maxStatBonus));
            }

            // ---- Success chance
            int successChance = baseSuccessChance;
            successChance += living.Count * successPerHero;
            if (classMatch) successChance += preferredClassBonus;

            if (contract.primarySkillRequired != RequiredSkill.None)
                successChance += skillMatch ? requiredSkillBonus : -missingSkillPenalty;

            if (contract.recommendedPower > 0)
            {
                int powerMod = Mathf.RoundToInt((partyPower - contract.recommendedPower) * powerWeight);
                successChance += Mathf.Clamp(powerMod, -maxPowerModifier, maxPowerModifier);
            }

            successChance += statMod;
            successChance += Mathf.Clamp(Mathf.RoundToInt((gearTotal / living.Count) * gearBonusWeight), 0, maxGearBonus);
            successChance += potentialBonus;
            successChance += chemistry;
            successChance -= loyaltyPenalty;
            successChance += contract.GetDifficultyPenalty(); // already negative (includes the Tower penalty)
            successChance = Mathf.Clamp(successChance, 5, 95);

            int roll = Random.Range(0, 100);
            bool isSuccess = roll < successChance;
            outcome.success = isSuccess;

            Debug.Log($"[ECONOMY] Mission Roll: {roll} vs {successChance}% (power {partyPower}/{contract.recommendedPower}, stat mod {statMod}). Success: {isSuccess}");

            if (isSuccess)
            {
                outcome.rewardGold = contract.baseRewardGold;
                outcome.loot = RollLoot(contract);
                contract.isCompleted = true;

                BuildPays(outcome, living);

                Log($"Mission in {contract.targetRegion} successful! The reward ({contract.baseRewardGold} gold) arrives tomorrow morning.");

                foreach (var hero in living)
                {
                    hero.successCount++;
                    ApplyInjury(hero, contract, true);
                    ApplyGearWear(hero, contract, true, outcome);
                    outcome.survivors.Add(hero);
                    GrantExp(hero, successExpBase + (contract.estimatedDays * successExpPerDay) + Mathf.Abs(contract.GetDifficultyPenalty()));
                }
            }
            else
            {
                contract.isAccepted = false; // the contract goes back on the board

                Log($"Mission in {contract.targetRegion} failed. The party was ambushed!");

                foreach (var hero in living)
                {
                    hero.failCount++;
                    ApplyInjury(hero, contract, false);
                    ApplyGearWear(hero, contract, false, outcome);

                    int deathRisk = baseDeathRisk + Mathf.Abs(contract.GetDifficultyPenalty())
                                    - hero.GetRacialSurvivalBonus() - Mathf.Max(0, hero.level - 1);
                    deathRisk = Mathf.Clamp(deathRisk, 5, 90);

                    if (Random.Range(0, 100) < deathRisk)
                    {
                        hero.isDead = true;
                        hero.isBusy = false;
                        outcome.casualties.Add(hero);
                        Log($"{hero.heroName} did not survive the mission...");
                    }
                    else
                    {
                        outcome.survivors.Add(hero);
                        GrantExp(hero, failExp);
                    }
                }
            }

            // The temporary buff is consumed by the expedition, win or lose
            foreach (var hero in heroes)
            {
                if (hero != null) hero.ResetTemporaryPotential();
            }

            foreach (var h1 in living)
            {
                int[] b = before[h1];
                int gained;
                expLog.TryGetValue(h1, out gained);

                outcome.report.Add(new HeroReportEntry
                {
                    hero = h1,
                    levelBefore = b[0],
                    expBefore = b[1],
                    levelAfter = h1.level,
                    expAfter = h1.currentExp,
                    expGained = gained,
                    injury = h1.injury,
                    died = h1.isDead,
                    gearBroke = outcome.gearBroken.Contains(h1)
                });
            }

            // Loyalty reactions: failure, injuries, dead companions, broken gear
            if (EmploymentManager.Instance != null)
            {
                EmploymentManager.Instance.RegisterMissionOutcome(contract, outcome);
            }

            return outcome;
        }

        private List<LootEntry> RollLoot(ContractData contract)
        {
            var loot = new List<LootEntry>();
            if (contract.rewardItems == null) return loot;

            foreach (var reward in contract.rewardItems)
            {
                if (reward == null || reward.item == null || reward.amount <= 0) continue;
                if (Random.Range(0, 100) >= reward.chancePercent) continue;

                loot.Add(new LootEntry { item = reward.item, amount = reward.amount });
            }

            return loot;
        }

        // Every hero takes home his agreed percentage of the reward
        private void BuildPays(MissionOutcome outcome, List<HeroData> living)
        {
            int total = 0;

            foreach (var hero in living)
            {
                int pay = Mathf.RoundToInt(outcome.rewardGold * Mathf.Clamp(hero.agreedSharePercent, 0, 100) / 100f);
                outcome.pays.Add(new HeroPay { hero = hero, amount = pay });
                total += pay;
            }

            int cap = Mathf.RoundToInt(outcome.rewardGold * maxHeroShareOfReward);
            if (total > cap && total > 0)
            {
                foreach (var p in outcome.pays)
                {
                    p.amount = Mathf.RoundToInt(p.amount * (float)cap / total);
                }
            }
        }

        private void ApplyInjury(HeroData hero, ContractData contract, bool success)
        {
            float amount = baseInjury + Mathf.Abs(contract.GetDifficultyPenalty()) * 0.5f + (success ? 0f : failureInjury);
            amount *= Random.Range(0.5f, 1.5f);

            // Tough heroes and good armor soften the blows
            float resist = Mathf.Clamp(hero.GetStat(StatType.Vitality) * 0.02f + hero.GetArmorRating() * 0.01f, 0f, 0.5f);
            int injuryGained = Mathf.Max(0, Mathf.RoundToInt(amount * (1f - resist)));

            hero.injury = Mathf.Clamp(hero.injury + injuryGained, 0, 100);
        }

        // Tower raids wear gear down fast; normal missions only when they fail
        private void ApplyGearWear(HeroData hero, ContractData contract, bool success, MissionOutcome outcome)
        {
            int wear = contract.isTowerRaid
                ? (contract.towerFloors / 4) + Random.Range(0, 10)
                : (success ? 0 : Random.Range(3, 8));

            if (wear <= 0) return;

            bool broke = false;

            if (hero.equippedWeapon != null && hero.weaponDurability > 0)
            {
                hero.weaponDurability = Mathf.Max(0, hero.weaponDurability - wear);
                if (hero.weaponDurability == 0)
                {
                    broke = true;
                    Log($"{hero.heroName}'s {hero.equippedWeapon.GetDisplayName()} has broken!");
                }
            }

            if (hero.equippedArmor != null && hero.armorDurability > 0)
            {
                hero.armorDurability = Mathf.Max(0, hero.armorDurability - wear);
                if (hero.armorDurability == 0)
                {
                    broke = true;
                    Log($"{hero.heroName}'s {hero.equippedArmor.GetDisplayName()} has been destroyed!");
                }
            }

            if (broke) outcome.gearBroken.Add(hero);
        }

        // Daily upkeep for hired heroes (those with an employment contract)
        public int ProcessDailyUpkeep(List<HeroData> roster)
        {
            if (roster == null || roster.Count == 0) return 0;

            int totalUpkeep = 0;

            foreach (var hero in roster)
            {
                if (hero != null && !hero.isDead && hero.currentContract != EmploymentType.None)
                {
                    totalUpkeep += hero.dailyUpkeepCost;
                }
            }

            // The Kitchen makes food and lodging cheaper
            if (TavernFacilitiesManager.Instance != null)
                totalUpkeep = Mathf.RoundToInt(totalUpkeep * TavernFacilitiesManager.Instance.GetUpkeepMultiplier());

            if (totalUpkeep <= 0) return 0;

            AddGold(-totalUpkeep);
            Log($"[TAVERN] Daily upkeep paid: -{totalUpkeep} gold for food and lodging.");

            if (Gold < 0)
            {
                Debug.LogWarning("[ECONOMY] The Guild is in debt!");
                Log("WARNING: The Guild is in debt! Morale is dropping.");
            }

            return totalUpkeep;
        }
    }
}

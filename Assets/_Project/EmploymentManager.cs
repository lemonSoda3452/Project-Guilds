using UnityEngine;
using System.Collections.Generic;

namespace HeroManagement
{
    // Hero contracts, loyalty, recovery, quitting and renewals
    public class EmploymentManager : MonoBehaviour
    {
        public static EmploymentManager Instance { get; private set; }

        [Header("Daily Loyalty")]
        public int loyaltyLossWhenInDebt = 10;
        public int dailyLoyaltyRecovery = 1;
        public int loyaltyPerHatedCompanion = -2;
        public int loyaltyPerFavoredCompanion = 1;

        [Header("Mission Events")]
        public int loyaltyGainOnSuccess = 3;
        public int loyaltyLossOnFailure = 5;
        public int heavyInjuryThreshold = 50;
        public int heavyInjuryPenalty = 4;
        public int severeInjuryThreshold = 75;
        public int severeInjuryPenalty = 8;
        [Tooltip("Per companion who died in the same party")]
        public int companionDeathPenalty = 8;
        public int gearBrokenPenalty = 6;

        [Header("Pay")]
        public int wellPaidBonus = 2;
        [Tooltip("Loyalty lost when a hero receives nothing compared to what he expects")]
        public int underpaidMaxPenalty = 8;

        [Header("Idle Heroes")]
        [Tooltip("Days at the tavern (healthy, not resting) before the hero gets restless and asks for a mission")]
        public int idleDaysBeforeRestless = 4;
        public int idleLoyaltyLossPerDay = 1;

        [Header("Leaving & Contracts")]
        [Tooltip("A hero at or below this loyalty leaves the guild")]
        public int quitThreshold = 0;
        public int expiryWarningDays = 1;
        [Range(0.1f, 2f)] public float renewalCostMultiplier = 0.7f;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void OnEnable() { GameManager.OnDayChanged += HandleNewDay; }
        void OnDisable() { GameManager.OnDayChanged -= HandleNewDay; }

        private void Log(string message)
        {
            if (SystemLogUIManager.Instance != null) SystemLogUIManager.Instance.LogMessage(message);
        }

        private List<HeroData> Roster()
        {
            return GameManager.Instance != null ? GameManager.Instance.allHeroesDatabase : null;
        }

        // ------------------------------------------------------------ loyalty helper

        public void AdjustLoyalty(HeroData hero, int delta, string reason, bool log = true)
        {
            if (hero == null || hero.isDead || delta == 0) return;

            hero.loyalty = Mathf.Clamp(hero.loyalty + delta, 0, 100);

            if (log && delta < 0 && !string.IsNullOrEmpty(reason))
            {
                Log($"{hero.heroName}'s loyalty drops ({delta}): {reason}.");
            }
        }

        // ------------------------------------------------------------ daily update

        private void HandleNewDay(int day)
        {
            var roster = Roster();
            if (roster == null || GameManager.Instance == null) return;

            bool inDebt = GameManager.Instance.playerGold < 0;

            // Backwards: heroes may leave the roster while we iterate
            for (int i = roster.Count - 1; i >= 0; i--)
            {
                var hero = roster[i];
                if (hero == null || hero.isDead) continue;

                // ---- Recovery: everybody who is home heals and rests
                if (!hero.isBusy)
                {
                    if (hero.injury > 0)
                        hero.injury = Mathf.Max(0, hero.injury - Mathf.Max(1, Mathf.RoundToInt(hero.GetDailyRecovery() * (TavernFacilitiesManager.Instance != null ? TavernFacilitiesManager.Instance.GetRecoveryMultiplier() : 1f))));
                    if (hero.restDaysLeft > 0)
                        hero.restDaysLeft--;
                }

                if (hero.currentContract == EmploymentType.None) continue;

                // ---- Daily loyalty
                int delta = inDebt ? -loyaltyLossWhenInDebt : dailyLoyaltyRecovery;

                foreach (var other in roster)
                {
                    if (other == null || other == hero || other.isDead) continue;
                    if (hero.hatedCompanions != null && hero.hatedCompanions.Contains(other)) delta += loyaltyPerHatedCompanion;
                    if (hero.favoredCompanions != null && hero.favoredCompanions.Contains(other)) delta += loyaltyPerFavoredCompanion;
                }

                if (delta < 0) AdjustLoyalty(hero, delta, inDebt ? "the guild cannot pay its debts" : "he cannot stand some companions");
                else hero.loyalty = Mathf.Clamp(hero.loyalty + delta, 0, 100);

                // ---- Idle: healthy and waiting at the tavern, asking for a mission
                bool available = !hero.isBusy && hero.restDaysLeft <= 0;
                if (available)
                {
                    hero.idleDays++;

                    if (hero.idleDays >= idleDaysBeforeRestless)
                    {
                        hero.missionRequests++;
                        int loss = idleLoyaltyLossPerDay + hero.missionRequests / 3; // asking again and again hurts more
                        AdjustLoyalty(hero, -loss, "", false);

                        if (hero.missionRequests % 2 == 1)
                        {
                            Log($"{hero.heroName} is restless and asks for a mission (request #{hero.missionRequests}). Loyalty is dropping.");
                        }
                    }
                }
                else
                {
                    hero.idleDays = 0;
                }

                // ---- Time-based contracts count down by day
                if (hero.currentContract == EmploymentType.TimeBased)
                {
                    hero.contractDurationLeft = Mathf.Max(0, hero.contractDurationLeft - 1);

                    if (hero.contractDurationLeft > 0 && hero.contractDurationLeft <= expiryWarningDays)
                    {
                        Log($"{hero.heroName}'s contract expires in {hero.contractDurationLeft} day(s). Renew it or lose them!");
                    }
                }

                // ---- Leaving (a hero who is away on a mission leaves once he is back)
                if (hero.loyalty <= quitThreshold)
                {
                    TryRelease(hero, $"{hero.heroName} has lost faith in the guild and leaves.");
                }
                else if (hero.currentContract == EmploymentType.TimeBased && hero.contractDurationLeft <= 0)
                {
                    TryRelease(hero, $"{hero.heroName}'s contract has expired. They return to the tavern.");
                }
                else if (hero.currentContract == EmploymentType.MissionBased && hero.contractDurationLeft <= 0)
                {
                    TryRelease(hero, $"{hero.heroName} has completed their contracted missions and leaves the guild.");
                }
            }
        }

        // ------------------------------------------------------------ mission events

        // Called by the EconomyManager when a mission is resolved
        public void RegisterMissionOutcome(ContractData contract, MissionOutcome outcome)
        {
            if (outcome == null) return;

            foreach (var hero in outcome.survivors)
            {
                if (hero == null) continue;

                int delta = outcome.success ? loyaltyGainOnSuccess : -loyaltyLossOnFailure;
                var reasons = new List<string>();

                if (!outcome.success) reasons.Add("the mission failed");

                if (hero.injury >= severeInjuryThreshold) { delta -= severeInjuryPenalty; reasons.Add("he almost died"); }
                else if (hero.injury >= heavyInjuryThreshold) { delta -= heavyInjuryPenalty; reasons.Add("he was badly wounded"); }

                if (outcome.casualties.Count > 0)
                {
                    delta -= companionDeathPenalty * outcome.casualties.Count;
                    reasons.Add("a companion died");
                }

                if (outcome.gearBroken.Contains(hero))
                {
                    delta -= gearBrokenPenalty;
                    reasons.Add("his gear broke");
                }

                if (delta < 0) AdjustLoyalty(hero, delta, string.Join(", ", reasons));
                else hero.loyalty = Mathf.Clamp(hero.loyalty + delta, 0, 100);

                // Mission-based contracts count one mission (the release happens once the hero is home)
                if (hero.currentContract == EmploymentType.MissionBased)
                {
                    hero.contractDurationLeft = Mathf.Max(0, hero.contractDurationLeft - 1);

                    if (hero.contractDurationLeft > 0 && hero.contractDurationLeft <= expiryWarningDays)
                    {
                        Log($"{hero.heroName} has {hero.contractDurationLeft} mission(s) left on their contract.");
                    }
                }
            }
        }

        // Called by the MissionDispatcher when the reward arrives. Compares the hero's cut with what he expects.
        public void RegisterPayment(HeroData hero, int paid)
        {
            if (hero == null || hero.isDead) return;

            int expected = Mathf.Max(1, hero.GetExpectedPayPerMission());

            if (paid >= expected)
            {
                hero.loyalty = Mathf.Clamp(hero.loyalty + wellPaidBonus, 0, 100);
            }
            else
            {
                float missing = (expected - paid) / (float)expected;
                int penalty = Mathf.Max(1, Mathf.CeilToInt(underpaidMaxPenalty * missing));
                AdjustLoyalty(hero, -penalty, $"he got {paid} gold but expected about {expected}");
            }
        }

        // ------------------------------------------------------------ leaving & renewal

        // The hero leaves the guild (keeps level, EXP and history, and may come back through the tavern).
        // A hero who is out on a mission cannot leave: the check repeats every day.
        public bool TryRelease(HeroData hero, string logMessage)
        {
            var roster = Roster();
            if (hero == null || roster == null || hero.isBusy) return false;

            roster.Remove(hero);

            hero.currentContract = EmploymentType.None;
            hero.contractDurationLeft = 0;
            hero.hasFreeEquipmentClause = false;
            hero.loyalty = 100;
            hero.idleDays = 0;
            hero.missionRequests = 0;

            if (!string.IsNullOrEmpty(logMessage)) Log(logMessage);
            return true;
        }

        public int GetRenewalCost(HeroData hero, EmploymentType type, int duration, bool freeEquipment)
        {
            if (hero == null || RecruitmentManager.Instance == null) return 0;

            int fullCost = RecruitmentManager.Instance.CalculateHireCost(hero, type, duration, freeEquipment, hero.agreedSharePercent);
            return Mathf.Max(10, Mathf.RoundToInt(fullCost * renewalCostMultiplier));
        }

        public bool TryRenewContract(HeroData hero, EmploymentType type, int duration, bool freeEquipment)
        {
            var roster = Roster();
            if (hero == null || hero.isDead || type == EmploymentType.None || roster == null) return false;
            if (!roster.Contains(hero) || GameManager.Instance == null) return false;

            int cost = GetRenewalCost(hero, type, duration, freeEquipment);

            if (!GameManager.Instance.TrySpendGold(cost))
            {
                Log($"Not enough gold to renew {hero.heroName}'s contract. Required: {cost} Gold.");
                return false;
            }

            hero.currentContract = type;
            hero.contractDurationLeft = duration;
            hero.hasFreeEquipmentClause = freeEquipment;
            hero.loyalty = Mathf.Min(100, hero.loyalty + 10);

            Log($"{hero.heroName}'s contract renewed for {cost} Gold. ({type})");
            return true;
        }
    }
}

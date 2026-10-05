using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HeroManagement
{
    public class RecruitmentManager : MonoBehaviour
    {
        public static RecruitmentManager Instance { get; private set; }

        [Header("Recruitment Database")]
        [Tooltip("Global database of heroes created by the developer")]
        public List<HeroData> availableRecruitsDatabase = new List<HeroData>();

        [Header("Tavern Visitors")]
        [Tooltip("Mercenaries currently in the tavern waiting for a contract")]
        public List<HeroData> currentTavernVisitors = new List<HeroData>();

        [Header("Settings")]
        public int visitorsPerDay = 3;

        // UIs listen to this to refresh the visitor list
        public static event System.Action OnVisitorsChanged;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            // Clean leftovers from the previous Play session (ScriptableObjects keep their data in the Editor)
            foreach (var hero in availableRecruitsDatabase)
            {
                if (hero != null) hero.ResetRuntimeState();
            }
        }

        void OnEnable() { GameManager.OnPhaseChanged += HandlePhaseChange; }
        void OnDisable() { GameManager.OnPhaseChanged -= HandlePhaseChange; }

        void Start()
        {
            // Day 1 starts in the Morning without any phase event: fill the tavern manually
            RefreshTavernVisitors(visitorsPerDay);
        }

        private void Log(string message)
        {
            if (SystemLogUIManager.Instance != null) SystemLogUIManager.Instance.LogMessage(message);
        }

        private void HandlePhaseChange(GameManager.DayPhase newPhase)
        {
            if (newPhase == GameManager.DayPhase.Morning) RefreshTavernVisitors(visitorsPerDay);
        }

        public void RefreshTavernVisitors(int amount)
        {
            DoRefreshTavernVisitors(amount);
            OnVisitorsChanged?.Invoke();
        }

        private void DoRefreshTavernVisitors(int amount)
        {
            currentTavernVisitors.Clear();

            if (availableRecruitsDatabase == null || availableRecruitsDatabase.Count == 0)
            {
                Log("The recruitment database is empty!");
                return;
            }

            // Only heroes who are NOT already in the guild
            List<HeroData> pool = new List<HeroData>();
            foreach (var hero in availableRecruitsDatabase)
            {
                bool alreadyInGuild = GameManager.Instance != null &&
                                      GameManager.Instance.allHeroesDatabase != null &&
                                      GameManager.Instance.allHeroesDatabase.Contains(hero);

                bool famousEnough = ReputationManager.Instance == null || ReputationManager.Instance.CanAttract(hero != null ? hero.fameTier : FameTier.Unknown);
                if (hero != null && !alreadyInGuild && !hero.isDead && famousEnough) pool.Add(hero);
            }

            if (pool.Count == 0)
            {
                Log("The tavern is deserted today: no new mercenaries available.");
                return;
            }

            int toSpawn = Mathf.Min(amount, pool.Count);
            for (int i = 0; i < toSpawn; i++)
            {
                int index = Random.Range(0, pool.Count);
                currentTavernVisitors.Add(pool[index]);
                pool.RemoveAt(index);
            }

            Log($"{currentTavernVisitors.Count} mercenaries are waiting for a contract in the tavern.");
        }

        // THE HIRING COST FORMULA
        // sharePercent = the cut of each mission reward the hero will take home (a bigger cut makes him cheaper to hire)
        public int CalculateHireCost(HeroData hero, EmploymentType employment, int contractDuration, bool freeEquipment, int sharePercent = 20)
        {
            if (hero == null) return 50;

            // 1. Base value: level, total stats, gear
            float baseValue = (hero.level * 25) + (hero.TotalStatPoints * 2) + (hero.GetGearRating() * 2);

            // Fame
            float fameMod = 1.0f;
            if (hero.fameTier == FameTier.LocalHero) fameMod = 1.2f;
            else if (hero.fameTier == FameTier.Veteran) fameMod = 1.5f;
            else if (hero.fameTier == FameTier.Legend) fameMod = 2.5f;
            baseValue *= fameMod;

            // 2. Reputation (wins and losses)
            int performanceMod = (hero.successCount * 25) - (hero.failCount * 20);

            // 3. Personality and age
            float personalityMod = 1.0f;
            if (hero.personality == PersonalityType.Greedy) personalityMod = 1.3f;
            else if (hero.personality == PersonalityType.Altruistic) personalityMod = 0.8f;
            else if (hero.personality == PersonalityType.GlorySeeker) personalityMod = 0.9f;

            if (hero.age < 20) personalityMod -= 0.1f;
            else if (hero.age > 50) personalityMod += 0.2f;

            // 4. Synergy with heroes already in the guild
            float synergyMod = 1.0f;
            var guild = GameManager.Instance != null ? GameManager.Instance.allHeroesDatabase : null;
            if (guild != null)
            {
                foreach (var member in guild)
                {
                    if (member == null) continue;
                    if (hero.favoredCompanions != null && hero.favoredCompanions.Contains(member)) synergyMod -= 0.2f;
                    if (hero.hatedCompanions != null && hero.hatedCompanions.Contains(member)) synergyMod += 0.5f;
                }
            }
            synergyMod = Mathf.Clamp(synergyMod, 0.5f, 3.0f);

            // 5. Hazard pay (guild mortality tax)
            int deadCount = guild != null ? guild.Count(h => h != null && h.isDead) : 0;
            float hazardMod = 1.0f + (deadCount * 0.05f);

            // 6. Employment type: per mission, per day (a month = 30 days) or permanent
            float contractMod = 1.0f;
            switch (employment)
            {
                case EmploymentType.Permanent: contractMod = 7.5f; break;
                case EmploymentType.MissionBased: contractMod = 0.5f * contractDuration; break;
                case EmploymentType.TimeBased: contractMod = 0.05f * contractDuration; break;
            }
            if (freeEquipment) contractMod += 0.5f;

            // 7. Reward cut: 10% -> x1.2, 20% -> x1.0, 30% -> x0.8, 40% -> x0.6
            float shareMod = Mathf.Clamp(1.4f - sharePercent / 50f, 0.6f, 1.3f);

            float reputationMod = ReputationManager.Instance != null ? ReputationManager.Instance.GetHireCostMultiplier() : 1f;
            float finalCost = (baseValue + performanceMod) * personalityMod * synergyMod * hazardMod * contractMod * shareMod * reputationMod;
            return Mathf.Max(15, Mathf.RoundToInt(finalCost));
        }

        public bool HireHero(HeroData hero, EmploymentType employment, int duration, bool freeEquipment, int sharePercent = 20)
        {
            if (hero == null || employment == EmploymentType.None || GameManager.Instance == null) return false;

            if (TavernFacilitiesManager.Instance != null && !TavernFacilitiesManager.Instance.HasFreeBed())
            {
                Log($"No free beds for {hero.heroName}! Improve the Beds to hire more heroes ({TavernFacilitiesManager.Instance.GetUsedBeds()}/{TavernFacilitiesManager.Instance.GetBedCapacity()}).");
                return false;
            }

            int cost = CalculateHireCost(hero, employment, duration, freeEquipment, sharePercent);

            if (!GameManager.Instance.TrySpendGold(cost))
            {
                Log($"Not enough gold to hire {hero.heroName}. Required: {cost} Gold.");
                return false;
            }

            hero.currentContract = employment;
            hero.contractDurationLeft = duration;
            hero.hasFreeEquipmentClause = freeEquipment;
            hero.agreedSharePercent = sharePercent;
            hero.loyalty = 100;
            hero.isBusy = false;
            hero.idleDays = 0;
            hero.missionRequests = 0;

            currentTavernVisitors.Remove(hero);
            if (!GameManager.Instance.allHeroesDatabase.Contains(hero))
            {
                GameManager.Instance.allHeroesDatabase.Add(hero);
            }

            Log($"Hired {hero.heroName} for {cost} Gold! ({employment}, takes {sharePercent}% of rewards)");
            OnVisitorsChanged?.Invoke();
            return true;
        }

        public void RestoreVisitors(List<HeroData> visitors)
        {
            currentTavernVisitors = visitors ?? new List<HeroData>();
            OnVisitorsChanged?.Invoke();
        }

        // MERCENARY INITIATIVE: evaluates a contract on the board and forms a party.
        // The expedition goes through the MissionDispatcher (travel, requirements, slot limits).
        public bool EvaluateAndAcceptContract(HeroData mercenary, ContractData contract)
        {
            if (mercenary == null || contract == null || mercenary.isDead || mercenary.isBusy || mercenary.restDaysLeft > 0) return false;
            if (contract.isAccepted || contract.isCompleted) return false;
            if (MissionDispatcher.Instance == null) return false;

            bool classMatches = !contract.hasPreferredClass || contract.preferredClass == mercenary.heroClass;

            // Unknown mercenaries avoid missions outside their specialty
            if (!classMatches && mercenary.fameTier == FameTier.Unknown) return false;

            bool prefersGroup = mercenary.personality == PersonalityType.Altruistic ||
                                mercenary.personality == PersonalityType.Cautious;

            List<HeroData> party = new List<HeroData> { mercenary };

            if (prefersGroup || !classMatches || contract.HasMandatoryRequirements)
            {
                List<HeroData> peers = HeroManager.Instance != null
                    ? HeroManager.Instance.GetAvailableHeroes()
                    : new List<HeroData>();

                peers.Remove(mercenary);

                if (mercenary.personality == PersonalityType.Greedy)
                {
                    peers = peers.OrderByDescending(h => h.level).ToList();
                }

                foreach (var peer in peers)
                {
                    if (party.Count >= contract.maxAllowedSlots) break;
                    if (peer != null) party.Add(peer);
                }
            }

            return MissionDispatcher.Instance.DispatchPartyToContract(contract, party);
        }
    }
}

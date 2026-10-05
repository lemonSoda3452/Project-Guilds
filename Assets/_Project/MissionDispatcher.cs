using UnityEngine;
using System.Collections.Generic;

namespace HeroManagement
{
    // Sends parties out and brings them home. A mission has 3 steps:
    //   1. Outbound trip   2. Resolution on arrival (reward gold is delivered the NEXT MORNING)   3. Return trip
    public class MissionDispatcher : MonoBehaviour
    {
        public static MissionDispatcher Instance { get; private set; }

        [System.Serializable]
        public class ActiveExpedition
        {
            public ContractData contract;
            public List<HeroData> heroes;
            public int daysRemaining;
            public ExpeditionPhase phase = ExpeditionPhase.Outbound;
            public int outboundDays;
            public List<LootEntry> loot = new List<LootEntry>();
            public int returnDays;
            [System.NonSerialized] public MissionOutcome outcome;
        }

        [System.Serializable]
        public class PendingPayout
        {
            public int payDay;
            public string region;
            public int grossGold;
            public List<HeroPay> heroPays = new List<HeroPay>();
        }

        [Header("Return Trip Balance")]
        [Tooltip("Extra return time at 100% average injury (0.8 = +80%)")]
        public float returnInjuryPenalty = 0.8f;
        [Tooltip("Load (gear + loot) / capacity above this slows the party")]
        public float returnLoadThreshold = 0.6f;
        public float returnLoadPenalty = 1.5f;

        [Header("Tower Raids")]
        public int underEquippedLoyaltyLoss = 10;

        [Header("Ongoing")]
        public List<ActiveExpedition> ongoingExpeditions = new List<ActiveExpedition>();
        public List<PendingPayout> pendingPayouts = new List<PendingPayout>();

        // UIs listen to this to refresh (departure, resolution, arrival, payments)
        public static event System.Action OnExpeditionsChanged;

        // Fired when a party is back home (or lost): the mission report screen listens to it
        public static event System.Action<MissionReport> OnMissionReport;

        private Queue<string> morningRavenQueue = new Queue<string>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void OnEnable()
        {
            GameManager.OnDayChanged += AdvanceTime;
            GameManager.OnPhaseChanged += HandlePhaseChange;
        }

        void OnDisable()
        {
            GameManager.OnDayChanged -= AdvanceTime;
            GameManager.OnPhaseChanged -= HandlePhaseChange;
        }

        private void Log(string message)
        {
            if (SystemLogUIManager.Instance != null) SystemLogUIManager.Instance.LogMessage(message);
        }

        private void HandlePhaseChange(GameManager.DayPhase newPhase)
        {
            if (newPhase != GameManager.DayPhase.Morning) return;

            if (morningRavenQueue.Count > 0) Log("--- DAWN: Ravens arrive from distant lands carrying news ---");
            while (morningRavenQueue.Count > 0) Log(morningRavenQueue.Dequeue());
        }

        public ActiveExpedition GetExpeditionForContract(ContractData contract)
        {
            if (contract == null) return null;
            return ongoingExpeditions.Find(e => e != null && e.contract == contract);
        }

        // ------------------------------------------------------------ travel formulas

        // The party travels at the speed of its slowest member (speed stat x mount)
        public float GetPartySpeed(List<HeroData> heroes)
        {
            float speed = float.MaxValue;
            if (heroes != null)
            {
                foreach (var h in heroes)
                {
                    if (h != null) speed = Mathf.Min(speed, h.GetTravelSpeedMultiplier());
                }
            }
            return speed == float.MaxValue ? 1f : speed;
        }

        public int EstimateOutboundDays(ContractData contract, List<HeroData> heroes)
        {
            if (contract == null) return 0;
            return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(1, contract.estimatedDays) / GetPartySpeed(heroes)));
        }

        // Kg the party can still carry after its own armor and weapons
        public float GetPartyFreeCapacity(List<HeroData> heroes)
        {
            float free = 0f;
            if (heroes != null)
            {
                foreach (var h in heroes)
                {
                    if (h != null) free += h.GetCarryCapacity() - h.GetGearWeight();
                }
            }
            return free;
        }

        // Same duration as the way out, longer if the party is wounded or heavily loaded
        public int EstimateReturnDays(ActiveExpedition exp)
        {
            if (exp == null || exp.heroes == null) return 1;

            float injury = 0f, capacity = 0f, load = 0f;
            int count = 0;

            foreach (var h in exp.heroes)
            {
                if (h == null || h.isDead) continue;
                count++;
                injury += h.injury;
                capacity += h.GetCarryCapacity();
                load += h.GetGearWeight();
            }

            if (count == 0) return 1;

            if (exp.loot != null)
            {
                foreach (var l in exp.loot) load += l.TotalWeight;
            }

            float injuryMod = (injury / count) / 100f * returnInjuryPenalty;
            float loadRatio = capacity > 0f ? load / capacity : 2f;
            float loadMod = Mathf.Max(0f, loadRatio - returnLoadThreshold) * returnLoadPenalty;

            return Mathf.Max(1, Mathf.CeilToInt(exp.outboundDays * (1f + injuryMod + loadMod)));
        }

        // ------------------------------------------------------------ dispatch

        public bool DispatchPartyToContract(ContractData contract, List<HeroData> assignedHeroes)
        {
            if (contract == null || contract.isAccepted || contract.isCompleted) return false;

            if (assignedHeroes == null || assignedHeroes.Count == 0 || assignedHeroes.Count > contract.maxAllowedSlots)
            {
                Debug.LogWarning("[MISSION] Invalid number of heroes for this contract!");
                return false;
            }

            foreach (var hero in assignedHeroes)
            {
                if (hero == null || hero.isBusy || hero.isDead || hero.restDaysLeft > 0)
                {
                    Log("Cannot depart: a hero is missing, busy, dead or still resting.");
                    return false;
                }
            }

            string missing;
            if (!contract.PartyMeetsRequirements(assignedHeroes, out missing))
            {
                Log("Cannot depart: " + missing + ".");
                return false;
            }

            contract.isAccepted = true;

            foreach (var hero in assignedHeroes)
            {
                hero.isBusy = true;
                hero.idleDays = 0;
                hero.missionRequests = 0;

                // Heroes promised free equipment expect to be armed properly for the Tower
                if (contract.isTowerRaid && hero.hasFreeEquipmentClause && hero.GetGearRating() < contract.minGearRating)
                {
                    if (EmploymentManager.Instance != null)
                    {
                        EmploymentManager.Instance.AdjustLoyalty(hero, -underEquippedLoyaltyLoss,
                            "the guild sent him into the Tower poorly equipped");
                    }
                }
            }

            ActiveExpedition exp = new ActiveExpedition
            {
                contract = contract,
                heroes = new List<HeroData>(assignedHeroes),
                phase = ExpeditionPhase.Outbound,
                outboundDays = EstimateOutboundDays(contract, assignedHeroes)
            };
            exp.daysRemaining = exp.outboundDays;

            ongoingExpeditions.Add(exp);

            Log($"A party has departed for {contract.targetRegion} under the banner of {contract.kingdomName}. Arrival in {exp.outboundDays} day(s).");
            OnExpeditionsChanged?.Invoke();
            return true;
        }

        // ------------------------------------------------------------ time

        // Runs on OnDayChanged, BEFORE OnPhaseChanged(Morning), so ravens and payments arrive at dawn
        private void AdvanceTime(int newDay)
        {
            for (int i = ongoingExpeditions.Count - 1; i >= 0; i--)
            {
                var exp = ongoingExpeditions[i];

                if (exp == null)
                {
                    ongoingExpeditions.RemoveAt(i);
                    continue;
                }

                exp.daysRemaining--;
                if (exp.daysRemaining > 0) continue;

                bool remove;
                if (exp.phase == ExpeditionPhase.Outbound)
                {
                    remove = ResolveExpedition(exp, newDay);
                }
                else
                {
                    ArriveHome(exp);
                    FireReport(exp, exp.outcome, false);
                    remove = true;
                }

                if (remove) ongoingExpeditions.RemoveAt(i);
            }

            ProcessPayouts(newDay);
            OnExpeditionsChanged?.Invoke();
        }

        // The party reached its destination. Returns true if the expedition is over (nobody survived).
        private bool ResolveExpedition(ActiveExpedition exp, int newDay)
        {
            var contract = exp.contract;

            if (contract == null)
            {
                FreeHeroes(exp.heroes);
                return true;
            }

            MissionOutcome outcome = EconomyManager.Instance != null
                ? EconomyManager.Instance.ResolveMission(contract, exp.heroes)
                : null;

            if (outcome != null) exp.outcome = outcome;

            if (outcome == null)
            {
                Debug.LogWarning("[EXPEDITION] The mission could not be resolved: it is cancelled.");
                FreeHeroes(exp.heroes);
                contract.isAccepted = false;
                return true;
            }

            // The gold does not travel with the party: a messenger brings it the morning after
            if (outcome.success)
            {
                pendingPayouts.Add(new PendingPayout
                {
                    payDay = newDay + 1,
                    region = contract.targetRegion.ToString(),
                    grossGold = outcome.rewardGold,
                    heroPays = outcome.pays
                });
            }

            if (outcome.survivors.Count == 0)
            {
                string kingdom = !string.IsNullOrEmpty(contract.kingdomName) ? contract.kingdomName : "Unknown Kingdom";
                morningRavenQueue.Enqueue(
                    $"[THE RAVEN FROM {kingdom.ToUpper()}] A raven arrives at dawn with grim news from {contract.targetRegion}: the entire party has been wiped out!");
                FireReport(exp, outcome, true);
                return true;
            }

            // Survivors head home carrying the loot
            exp.heroes = new List<HeroData>(outcome.survivors);
            exp.loot = outcome.loot;
            exp.phase = ExpeditionPhase.Returning;
            exp.daysRemaining = EstimateReturnDays(exp);
            exp.returnDays = exp.daysRemaining;

            Log($"The party leaves {contract.targetRegion} and heads home ({exp.daysRemaining} day(s)).");
            return false;
        }

        private void ArriveHome(ActiveExpedition exp)
        {
            var contract = exp.contract;

            foreach (var hero in exp.heroes)
            {
                if (hero == null || hero.isDead) continue;

                hero.isBusy = false;
                hero.restDaysLeft = hero.GetRestDaysFor(hero.injury);
                if (TavernFacilitiesManager.Instance != null && hero.restDaysLeft > 0)
                {
                    hero.restDaysLeft = Mathf.Max(1, Mathf.CeilToInt(hero.restDaysLeft / TavernFacilitiesManager.Instance.GetRecoveryMultiplier()));
                }
                hero.idleDays = 0;
            }

            // Loot reaches the guild stash only if the party made it home
            if (exp.loot != null && exp.loot.Count > 0)
            {
                foreach (var entry in exp.loot)
                {
                    if (entry == null || entry.item == null) continue;

                    if (InventoryManager.Instance != null) InventoryManager.Instance.AddItem(entry.item, entry.amount);
                    else Debug.LogWarning("[EXPEDITION] No InventoryManager: the loot is lost.");
                }
            }

            Log($"The party has returned from {(contract != null ? contract.targetRegion.ToString() : "the mission")}.");

            foreach (var hero in exp.heroes)
            {
                if (hero != null && !hero.isDead && hero.restDaysLeft > 0)
                {
                    Log($"{hero.heroName} needs {hero.restDaysLeft} day(s) of rest (injury {hero.injury}%).");
                }
            }
        }

        private void ProcessPayouts(int currentDay)
        {
            for (int i = pendingPayouts.Count - 1; i >= 0; i--)
            {
                var payout = pendingPayouts[i];
                if (payout == null) { pendingPayouts.RemoveAt(i); continue; }
                if (payout.payDay > currentDay) continue;

                int heroTotal = 0;
                foreach (var p in payout.heroPays) heroTotal += p.amount;

                int net = Mathf.Max(0, payout.grossGold - heroTotal);

                if (EconomyManager.Instance != null) EconomyManager.Instance.AddGold(net);

                Log($"[MESSENGER] {payout.grossGold} gold arrive from {payout.region}: {net} for the guild, {heroTotal} paid to the heroes.");

                if (EmploymentManager.Instance != null)
                {
                    foreach (var p in payout.heroPays) EmploymentManager.Instance.RegisterPayment(p.hero, p.amount);
                }

                pendingPayouts.RemoveAt(i);
            }
        }

        public List<string> GetRavenQueueSnapshot() { return new List<string>(morningRavenQueue); }

        public void RestoreState(List<ActiveExpedition> expeditions, List<PendingPayout> payouts, List<string> ravenMessages)
        {
            ongoingExpeditions = expeditions ?? new List<ActiveExpedition>();
            pendingPayouts = payouts ?? new List<PendingPayout>();
            morningRavenQueue = new Queue<string>(ravenMessages ?? new List<string>());
            OnExpeditionsChanged?.Invoke();
        }

        private void FireReport(ActiveExpedition exp, MissionOutcome outcome, bool wiped)
        {
            if (exp == null || exp.contract == null || outcome == null) return;

            int shares = 0;
            foreach (var p in outcome.pays) shares += p.amount;

            OnMissionReport?.Invoke(new MissionReport
            {
                contract = exp.contract,
                success = outcome.success,
                rewardGold = outcome.rewardGold,
                heroShares = shares,
                daysAway = exp.outboundDays + exp.returnDays,
                partyWiped = wiped,
                loot = outcome.loot,
                heroes = outcome.report
            });
        }

        private void FreeHeroes(List<HeroData> heroes)
        {
            if (heroes == null) return;
            foreach (var h in heroes)
            {
                if (h != null) h.isBusy = false;
            }
        }
    }
}

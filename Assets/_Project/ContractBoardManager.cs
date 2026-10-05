using System.Collections.Generic;
using UnityEngine;

namespace HeroManagement
{
    // Decides which contracts are on the board:
    //   MORNING   -> ravens bring contracts from kingdoms, orders and nobles (well paid, far away, with requirements)
    //   DAY       -> townsfolk pin small, local requests on the board
    //   AFTERNOON -> restless mercenaries may take the initiative and grab a contract on their own
    // Contracts expire after a few days if nobody takes them.
    public class ContractBoardManager : MonoBehaviour
    {
        public static ContractBoardManager Instance { get; private set; }

        [Header("Contract Pool")]
        [Tooltip("Every contract that can show up. If empty, GameManager.allContractsDatabase is used.")]
        public List<ContractData> contractPool = new List<ContractData>();

        [Header("Daily Arrivals")]
        public int ravenContractsMin = 1;
        public int ravenContractsMax = 2;
        public int boardContractsMin = 1;
        public int boardContractsMax = 3;
        [Tooltip("The board never holds more contracts than this")]
        public int maxPostedContracts = 8;

        [Header("Mercenary Initiative")]
        public bool allowMercenaryInitiative = true;
        [Tooltip("A hero acts on his own after asking for a mission this many times")]
        public int initiativeAfterRequests = 3;

        [Header("Posted Right Now (runtime)")]
        public List<ContractData> postedContracts = new List<ContractData>();

        // UIs listen to this to refresh the board
        public static event System.Action OnBoardChanged;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            // ScriptableObjects keep their data between Play sessions in the Editor
            foreach (var c in contractPool)
            {
                if (c != null) c.ResetRuntimeState();
            }
        }

        void OnEnable()
        {
            GameManager.OnPhaseChanged += HandlePhaseChanged;
            MissionDispatcher.OnExpeditionsChanged += HandleExpeditionsChanged;
        }

        void OnDisable()
        {
            GameManager.OnPhaseChanged -= HandlePhaseChanged;
            MissionDispatcher.OnExpeditionsChanged -= HandleExpeditionsChanged;
        }

        void Start()
        {
            // Day 1 starts in the Morning without a phase event: the first ravens must still arrive
            DeliverRavenContracts();
        }

        private void Log(string message)
        {
            if (SystemLogUIManager.Instance != null) SystemLogUIManager.Instance.LogMessage(message);
        }

        private int CurrentDay()
        {
            return GameManager.Instance != null ? GameManager.Instance.currentDay : 1;
        }

        private List<ContractData> GetPool()
        {
            if (contractPool != null && contractPool.Count > 0) return contractPool;
            if (GameManager.Instance != null && GameManager.Instance.allContractsDatabase != null) return GameManager.Instance.allContractsDatabase;
            return new List<ContractData>();
        }

        // Days left (today included) before the contract expires
        public int GetDaysLeft(ContractData contract)
        {
            if (contract == null) return 0;
            return Mathf.Max(0, contract.postedUntilDay - CurrentDay() + 1);
        }

        // ------------------------------------------------------------ phases

        private void HandlePhaseChanged(GameManager.DayPhase phase)
        {
            switch (phase)
            {
                case GameManager.DayPhase.Morning:
                    ExpireContracts();
                    DeliverRavenContracts();
                    break;
                case GameManager.DayPhase.Day:
                    DeliverCommonerContracts();
                    break;
                case GameManager.DayPhase.Afternoon:
                    MercenaryInitiative();
                    break;
            }
        }

        // Accepted or completed contracts leave the board
        private void HandleExpeditionsChanged()
        {
            bool changed = false;

            for (int i = postedContracts.Count - 1; i >= 0; i--)
            {
                var c = postedContracts[i];
                if (c == null || c.isAccepted || c.isCompleted)
                {
                    if (c != null) c.isPosted = false;
                    postedContracts.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed) OnBoardChanged?.Invoke();
        }

        // ------------------------------------------------------------ deliveries

        private void DeliverRavenContracts()
        {
            var drawn = Draw(ContractSource.Raven, Random.Range(ravenContractsMin, ravenContractsMax + 1) + (ReputationManager.Instance != null ? ReputationManager.Instance.GetRavenBonus() : 0));

            foreach (var c in drawn)
            {
                Post(c);
                string sender = !string.IsNullOrEmpty(c.kingdomName) ? c.kingdomName : "a distant land";
                Log($"[RAVEN] A raven from {sender} brings a contract: {c.clientName} ({c.targetRegion}).");
            }

            if (drawn.Count > 0) OnBoardChanged?.Invoke();
        }

        private void DeliverCommonerContracts()
        {
            var drawn = Draw(ContractSource.Board, Random.Range(boardContractsMin, boardContractsMax + 1));

            foreach (var c in drawn)
            {
                Post(c);
                Log($"[BOARD] {c.clientName} pins a request on the board ({c.targetRegion}).");
            }

            if (drawn.Count > 0) OnBoardChanged?.Invoke();
        }

        // Weighted random draw, without repeating the same contract
        private List<ContractData> Draw(ContractSource source, int count)
        {
            int day = CurrentDay();
            var candidates = new List<ContractData>();

            foreach (var c in GetPool())
            {
                if (c == null || c.source != source) continue;
                if (c.isAccepted || c.isCompleted || c.isPosted) continue;
                if (c.availableFromDay > day) continue;
                if (c.isTowerRaid && TowerManager.HighestFloor < c.towerStartFloor - 1) continue; // earlier floors first
                candidates.Add(c);
            }

            var result = new List<ContractData>();

            while (result.Count < count && candidates.Count > 0 && postedContracts.Count + result.Count < maxPostedContracts)
            {
                int total = 0;
                foreach (var c in candidates) total += Mathf.Max(1, c.spawnWeight);

                int roll = Random.Range(0, total);
                ContractData picked = candidates[candidates.Count - 1];

                foreach (var c in candidates)
                {
                    roll -= Mathf.Max(1, c.spawnWeight);
                    if (roll < 0) { picked = c; break; }
                }

                result.Add(picked);
                candidates.Remove(picked);
            }

            return result;
        }

        private void Post(ContractData contract)
        {
            contract.isPosted = true;
            contract.postedUntilDay = CurrentDay() + Mathf.Max(1, contract.postedDurationDays) - 1;
            postedContracts.Add(contract);
        }

        private void ExpireContracts()
        {
            int day = CurrentDay();
            bool changed = false;

            for (int i = postedContracts.Count - 1; i >= 0; i--)
            {
                var c = postedContracts[i];

                if (c == null || c.isAccepted || c.isCompleted)
                {
                    if (c != null) c.isPosted = false;
                    postedContracts.RemoveAt(i);
                    changed = true;
                    continue;
                }

                if (day > c.postedUntilDay)
                {
                    c.isPosted = false; // it can come back later
                    postedContracts.RemoveAt(i);
                    changed = true;
                    Log($"The contract of {c.clientName} expired: nobody took it.");
                }
            }

            if (changed) OnBoardChanged?.Invoke();
        }

        public void RestorePosted(List<ContractData> posted)
        {
            postedContracts = posted ?? new List<ContractData>();
            OnBoardChanged?.Invoke();
        }

        // ------------------------------------------------------------ mercenary initiative

        // Heroes who asked for a mission again and again stop waiting for the player
        private void MercenaryInitiative()
        {
            if (!allowMercenaryInitiative || RecruitmentManager.Instance == null || HeroManager.Instance == null) return;
            if (postedContracts.Count == 0) return;

            foreach (var hero in HeroManager.Instance.GetAvailableHeroes())
            {
                if (hero == null || hero.isBusy || hero.missionRequests < initiativeAfterRequests) continue;

                foreach (var contract in new List<ContractData>(postedContracts))
                {
                    if (contract == null || contract.isAccepted) continue;

                    if (RecruitmentManager.Instance.EvaluateAndAcceptContract(hero, contract))
                    {
                        Log($"{hero.heroName} lost patience and took the initiative: off to {contract.targetRegion}!");
                        break;
                    }
                }
            }
        }
    }
}

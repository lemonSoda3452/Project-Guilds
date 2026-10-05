using UnityEngine;
using System.Collections.Generic;

namespace HeroManagement
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum DayPhase { Morning, Day, Afternoon, Evening }

        [Header("Game State")]
        public int currentDay = 1;
        public DayPhase currentPhase = DayPhase.Morning;
        [Tooltip("Gold at the start of a new session")]
        public int startingGold = 250;
        public int playerGold = 0; // The single treasury of the guild

        [Header("Global Databases (For Reset)")]
        [Tooltip("Heroes that belong to the guild. Hired heroes are added here at runtime.")]
        public List<HeroData> allHeroesDatabase;
        public List<ContractData> allContractsDatabase;

        public delegate void PhaseChangeHandler(DayPhase newPhase);
        public static event PhaseChangeHandler OnPhaseChanged;

        public delegate void DayChangeHandler(int newDay);
        public static event DayChangeHandler OnDayChanged;

        public delegate void GoldChangeHandler(int newAmount);
        public static event GoldChangeHandler OnGoldChanged;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            playerGold = startingGold;
            ResetGameData();
        }

        void Start()
        {
            OnGoldChanged?.Invoke(playerGold);
        }

        private void ResetGameData()
        {
            if (allHeroesDatabase != null)
            {
                foreach (var hero in allHeroesDatabase)
                {
                    if (hero != null) hero.ResetRuntimeState();
                }
            }

            if (allContractsDatabase != null)
            {
                foreach (var contract in allContractsDatabase)
                {
                    if (contract != null) contract.ResetRuntimeState();
                }
            }

            Debug.Log("[SYSTEM] Game data reset for a new session.");
        }

        public void AdvancePhase()
        {
            if (GameFlowManager.Instance != null && GameFlowManager.Instance.isGameOver) return;

            if (currentPhase == DayPhase.Evening)
            {
                currentPhase = DayPhase.Morning;
                currentDay++;

                // Single upkeep calculation, handled by the EconomyManager
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.ProcessDailyUpkeep(allHeroesDatabase);
                }

                OnDayChanged?.Invoke(currentDay);
            }
            else
            {
                currentPhase++;
            }

            OnPhaseChanged?.Invoke(currentPhase);
        }

        // Fired after a saved game is loaded so every UI can redraw (no day or phase processing runs)
        public static event System.Action OnStateRestored;

        public void RestoreState(int day, DayPhase phase, int gold, List<HeroData> guild)
        {
            currentDay = Mathf.Max(1, day);
            currentPhase = phase;
            playerGold = gold;

            if (allHeroesDatabase == null) allHeroesDatabase = new List<HeroData>();
            allHeroesDatabase.Clear();
            if (guild != null) allHeroesDatabase.AddRange(guild);

            OnGoldChanged?.Invoke(playerGold);
            OnStateRestored?.Invoke();
        }

        public void AddGold(int amount)
        {
            playerGold += amount;
            OnGoldChanged?.Invoke(playerGold);
        }

        // Returns false (and spends nothing) if the guild cannot afford it
        public bool TrySpendGold(int amount)
        {
            if (amount < 0 || playerGold < amount) return false;
            AddGold(-amount);
            return true;
        }
    }
}

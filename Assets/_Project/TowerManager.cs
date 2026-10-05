using UnityEngine;

namespace HeroManagement
{
    // The Spire: a 100-floor tower climbed in ranges (floors 1-20, 21-40...). Every range is a Tower Raid contract.
    // A range appears on the board only after the floors before it were cleared. Clearing floor 100 can win the game.
    public class TowerManager : MonoBehaviour
    {
        public static TowerManager Instance { get; private set; }

        public static int HighestFloor { get { return Instance != null ? Instance.highestFloorCleared : 0; } }

        [Header("Progress")]
        public int highestFloorCleared = 0;
        public int topFloor = 100;

        [Header("Milestones")]
        [Tooltip("Every N floors the Spire's guardian drops a gold reward")]
        public int milestoneEvery = 10;
        [Tooltip("Reward = this x (floor / milestoneEvery)")]
        public int milestoneGoldPerStep = 300;

        // Fired when new floors are cleared (the game flow listens to it for the victory)
        public static event System.Action<int> OnFloorReached;
        // Fired when a saved game restores the progress (visuals listen to it)
        public static event System.Action OnProgressRestored;

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

        private void HandleReport(MissionReport report)
        {
            if (report == null || !report.success || report.contract == null || !report.contract.isTowerRaid) return;

            int reached = Mathf.Min(topFloor, report.contract.GetTowerEndFloor());
            if (reached <= highestFloorCleared) return;

            int previous = highestFloorCleared;
            highestFloorCleared = reached;

            Log($"[SPIRE] The guild reached floor {reached} of the Spire!");

            if (milestoneEvery > 0 && EconomyManager.Instance != null)
            {
                for (int floor = previous + 1; floor <= reached; floor++)
                {
                    if (floor % milestoneEvery != 0) continue;

                    int gold = milestoneGoldPerStep * (floor / milestoneEvery);
                    EconomyManager.Instance.AddGold(gold);
                    Log($"[SPIRE] Floor {floor}: the guardian falls and drops {gold} gold!");
                }
            }

            OnFloorReached?.Invoke(highestFloorCleared);
        }

        public void RestoreProgress(int floor)
        {
            highestFloorCleared = Mathf.Clamp(floor, 0, topFloor);
            OnProgressRestored?.Invoke();
        }

        public string GetStatusText()
        {
            return $"Torre: piano piu' alto {highestFloorCleared}/{topFloor}";
        }
    }
}

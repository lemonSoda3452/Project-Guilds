using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeroManagement
{
    // Victory and defeat conditions of the demo
    public class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager Instance { get; private set; }

        [Header("Victory")]
        public int targetGold = 5000;
        [Tooltip("The demo lasts this many days")]
        public int maxDays = 30;

        [Header("Defeat")]
        [Tooltip("Days in a row with negative gold before the guild collapses")]
        public int maxConsecutiveDebtDays = 5;
        public bool loseWhenGuildWipedOut = true;
        [Tooltip("If every hero is dead and the guild has less gold than this, it cannot rebuild")]
        public int minGoldToRebuild = 150;

        [Header("Tower victory")]
        public bool winByConqueringTower = true;
        public int towerVictoryFloor = 100;

        public bool isGameOver { get; private set; }
        public bool hasWon { get; private set; }
        public string endReason { get; private set; }

        // UI can listen to this to show the end screen: (won, reason)
        public static event System.Action<bool, string> OnGameEnded;

        private int consecutiveDebtDays = 0;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void OnEnable()
        {
            GameManager.OnPhaseChanged += HandlePhaseChanged;
            GameManager.OnGoldChanged += HandleGoldChanged;
            TowerManager.OnFloorReached += HandleTowerFloor;
        }

        void OnDisable()
        {
            GameManager.OnPhaseChanged -= HandlePhaseChanged;
            GameManager.OnGoldChanged -= HandleGoldChanged;
            TowerManager.OnFloorReached -= HandleTowerFloor;
        }

        public string GetObjectiveText()
        {
            int day = GameManager.Instance != null ? GameManager.Instance.currentDay : 1;
            return $"Goal: {targetGold} gold by day {maxDays} (day {day})." +
                   (winByConqueringTower ? $" Or conquer the Spire ({TowerManager.HighestFloor}/{towerVictoryFloor})." : "");
        }

        private void HandleTowerFloor(int floor)
        {
            if (!isGameOver && winByConqueringTower && floor >= towerVictoryFloor)
            {
                EndGame(true, "The guild reached the summit of the Spire. The whole realm sings your name!");
            }
        }

        private void HandleGoldChanged(int gold)
        {
            if (!isGameOver && gold >= targetGold)
            {
                EndGame(true, $"The guild reached {targetGold} gold. Your name will be sung in every tavern!");
            }
        }

        // Fires at dawn, after the day change was processed (expeditions, upkeep, deaths)
        private void HandlePhaseChanged(GameManager.DayPhase phase)
        {
            if (isGameOver || phase != GameManager.DayPhase.Morning || GameManager.Instance == null) return;

            // Debt
            if (GameManager.Instance.playerGold < 0) consecutiveDebtDays++;
            else consecutiveDebtDays = 0;

            if (consecutiveDebtDays >= maxConsecutiveDebtDays)
            {
                EndGame(false, "The guild has been in debt for too long. The creditors seize everything.");
                return;
            }

            // Wipe-out
            if (loseWhenGuildWipedOut && GameManager.Instance.allHeroesDatabase != null)
            {
                var roster = GameManager.Instance.allHeroesDatabase;
                int living = 0;
                foreach (var h in roster)
                {
                    if (h != null && !h.isDead) living++;
                }

                if (roster.Count > 0 && living == 0 && GameManager.Instance.playerGold < minGoldToRebuild)
                {
                    EndGame(false, "Every hero has fallen and the guild cannot afford new recruits.");
                    return;
                }
            }

            // Time limit
            if (GameManager.Instance.currentDay > maxDays)
            {
                EndGame(false, $"Time is up: the guild did not reach {targetGold} gold in {maxDays} days.");
            }
        }

        private void EndGame(bool won, string reason)
        {
            if (isGameOver) return;

            isGameOver = true;
            hasWon = won;
            endReason = reason;

            Debug.Log($"[GAME] {(won ? "VICTORY" : "DEFEAT")}: {reason}");

            if (SystemLogUIManager.Instance != null)
            {
                SystemLogUIManager.Instance.LogMessage($"{(won ? "VICTORY" : "GAME OVER")}: {reason}");
            }

            OnGameEnded?.Invoke(won, reason);
        }

        // Used when a saved game is loaded: the run is alive again
        public void ResetForLoad()
        {
            isGameOver = false;
            hasWon = false;
            endReason = "";
            consecutiveDebtDays = 0;
        }

        // Hook to a "Play again" button
        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}

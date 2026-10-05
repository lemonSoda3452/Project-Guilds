using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeroManagement
{
    public enum GameMode { Normal, Permadeath }

    // What survives a scene reload: a new game keeps the chosen mode; a seized tavern (Normal mode) keeps part of the progress
    public static class RunCarryover
    {
        public static bool active = false;
        public static GameMode mode = GameMode.Normal;
        public static int towerFloor = 0;
        public static int reputation = -1; // -1 = use the default starting reputation

        public static void Clear() { active = false; }
    }

    // NORMAL: nobody dies and you cannot lose the game for good (a seized tavern gives you a fresh start).
    // PERMADEATH: heroes die for good, everything is harder and more expensive, and you can lose.
    public class GameModeManager : MonoBehaviour
    {
        public static GameModeManager Instance { get; private set; }

        public GameMode mode = GameMode.Normal;
        [Tooltip("What the menu's 'New Game' will start")]
        public GameMode menuChoice = GameMode.Normal;

        [Header("Permadeath: harder missions")]
        [Tooltip("Subtracted from the base success chance")]
        public int successPenalty = 10;
        [Tooltip("Extra negative difficulty on every contract")]
        public int extraDifficulty = 8;
        [Tooltip("Each hero adds this much LESS to the success chance (missions need bigger parties)")]
        public int perHeroReduction = 2;

        [Header("Permadeath: costs and rewards")]
        public float costMultiplier = 1.4f;
        public float goldRewardMultiplier = 0.75f;
        public float lootMultiplier = 0.6f;

        [Header("Permadeath: loyalty and reputation")]
        public float loyaltyGainMultiplier = 0.5f;
        public float loyaltyLossMultiplier = 1.5f;
        public float reputationLossMultiplier = 2f;

        public static event Action OnModeChanged;

        public static bool IsPermadeath { get { return Instance != null && Instance.mode == GameMode.Permadeath; } }
        public static bool HeroesCanDie { get { return IsPermadeath; } }

        public static float CostMult() { return IsPermadeath ? Instance.costMultiplier : 1f; }
        public static float RewardMult() { return IsPermadeath ? Instance.goldRewardMultiplier : 1f; }
        public static float LootMult() { return IsPermadeath ? Instance.lootMultiplier : 1f; }
        public static float LoyaltyGainMult() { return IsPermadeath ? Instance.loyaltyGainMultiplier : 1f; }
        public static float LoyaltyLossMult() { return IsPermadeath ? Instance.loyaltyLossMultiplier : 1f; }
        public static float ReputationLossMult() { return IsPermadeath ? Instance.reputationLossMultiplier : 1f; }
        public static int SuccessPenalty() { return IsPermadeath ? Instance.successPenalty : 0; }
        public static int ExtraDifficulty() { return IsPermadeath ? Instance.extraDifficulty : 0; }
        public static int PerHeroReduction() { return IsPermadeath ? Instance.perHeroReduction : 0; }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            if (RunCarryover.active)
            {
                mode = RunCarryover.mode;
                menuChoice = mode;
            }
        }

        private void Log(string message)
        {
            if (SystemLogUIManager.Instance != null) SystemLogUIManager.Instance.LogMessage(message);
        }

        public string ModeName { get { return mode == GameMode.Permadeath ? "PERMADEATH" : "NORMAL"; } }

        // ---- menu hooks
        public void ToggleChoice()
        {
            menuChoice = menuChoice == GameMode.Normal ? GameMode.Permadeath : GameMode.Normal;
            OnModeChanged?.Invoke();
        }

        public string GetChoiceLabel()
        {
            return "Mode: " + (menuChoice == GameMode.Permadeath ? "PERMADEATH" : "NORMAL");
        }

        // Starts a game in the chosen mode. At day 1 nothing has happened yet, so the mode simply changes;
        // later, the scene is reloaded for a clean new game.
        public void StartChosen()
        {
            var gm = GameManager.Instance;

            bool progressed = gm != null && (gm.currentDay > 1 || HasHiredHeroes() ||
                (MissionDispatcher.Instance != null && MissionDispatcher.Instance.ongoingExpeditions.Count > 0));

            if (progressed)
            {
                RunCarryover.active = true;
                RunCarryover.mode = menuChoice;
                RunCarryover.towerFloor = 0;
                RunCarryover.reputation = -1;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }

            mode = menuChoice;
            OnModeChanged?.Invoke();
            Log(mode == GameMode.Permadeath
                ? "[MODE] PERMADEATH: heroes die for good, everything is harder. Good luck."
                : "[MODE] NORMAL: nobody dies and the tavern always gets a second chance.");
        }

        private bool HasHiredHeroes()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.allHeroesDatabase == null) return false;

            foreach (var h in gm.allHeroesDatabase)
            {
                if (h != null && !h.IsStandard) return true;
            }
            return false;
        }
    }
}

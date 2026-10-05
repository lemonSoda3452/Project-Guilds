using UnityEngine;
using UnityEngine.UI;

namespace HeroManagement
{
    // Victory / defeat screen. Put this script on an object that is always active (NOT on the panel itself).
    public class EndScreenUIManager : MonoBehaviour
    {
        public static EndScreenUIManager Instance { get; private set; }

        public GameObject endPanel;
        public Text titleText;
        public Text reasonText;

        void Awake()
        {
            if (Instance == null) Instance = this;
            if (endPanel != null) endPanel.SetActive(false);
        }

        void OnEnable() { GameFlowManager.OnGameEnded += Show; }
        void OnDisable() { GameFlowManager.OnGameEnded -= Show; }

        private void Show(bool won, string reason)
        {
            if (endPanel != null) endPanel.SetActive(true);
            if (titleText != null) titleText.text = won ? "VICTORY!" : "GAME OVER";
            if (reasonText != null) reasonText.text = reason;
        }

        public void Hide()
        {
            if (endPanel != null) endPanel.SetActive(false);
        }

        // Hook to the "Play again" button
        public void OnRestartClicked()
        {
            if (GameFlowManager.Instance != null) GameFlowManager.Instance.RestartGame();
        }
    }
}

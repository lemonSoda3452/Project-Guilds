using UnityEngine;
using UnityEngine.UI;

namespace HeroManagement
{
    public class UIManager : MonoBehaviour
    {
        [Header("Screen Texts")]
        public Text dayText;
        public Text phaseText;
        public Text goldText;

        void OnEnable()
        {
            GameManager.OnDayChanged += UpdateDayText;
            GameManager.OnPhaseChanged += UpdatePhaseText;
            GameManager.OnGoldChanged += UpdateGoldText;
            GameManager.OnStateRestored += RefreshAll; // a saved game was loaded
        }

        void OnDisable()
        {
            GameManager.OnDayChanged -= UpdateDayText;
            GameManager.OnPhaseChanged -= UpdatePhaseText;
            GameManager.OnGoldChanged -= UpdateGoldText;
            GameManager.OnStateRestored -= RefreshAll;
        }

        void Start() { RefreshAll(); }

        private void RefreshAll()
        {
            if (GameManager.Instance == null) return;

            UpdateDayText(GameManager.Instance.currentDay);
            UpdatePhaseText(GameManager.Instance.currentPhase);
            UpdateGoldText(GameManager.Instance.playerGold);
        }

        private void UpdateDayText(int day)
        {
            if (dayText != null) dayText.text = "GIORNO: " + day;
        }

        private void UpdatePhaseText(GameManager.DayPhase phase)
        {
            if (phaseText != null) phaseText.text = "FASE: " + phase.ToString().ToUpper();
        }

        private void UpdateGoldText(int gold)
        {
            if (goldText != null) goldText.text = "ORO: " + gold;
        }

        // Hooked to the OnClick event of the "Next Phase" button
        public void OnAdvancePhaseButtonClicked()
        {
            if (GameManager.Instance != null) GameManager.Instance.AdvancePhase();
        }
    }
}

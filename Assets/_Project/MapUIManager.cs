using UnityEngine;
using UnityEngine.UI;

namespace HeroManagement
{
    public class MapUIManager : MonoBehaviour
    {
        public static MapUIManager Instance { get; private set; }

        [Header("Panel References")]
        public GameObject mapPanel;

        [Header("Contract Info Display")]
        public Text regionText;
        public Text kingdomText;
        public Text difficultyText;
        public Text rewardText;
        public Text requirementsText;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            if (mapPanel != null) mapPanel.SetActive(false);
        }

        public void ShowContractDetails(ContractData contract)
        {
            if (contract == null) return;

            if (mapPanel != null) mapPanel.SetActive(true);

            if (regionText != null) regionText.text = $"Destination: {contract.targetRegion}";
            if (kingdomText != null) kingdomText.text = $"Kingdom: {contract.kingdomName}";
            if (difficultyText != null) difficultyText.text = $"Travel Difficulty: {contract.travelDifficulty} ({contract.estimatedDays} days)";
            if (rewardText != null) rewardText.text = $"Reward: {contract.baseRewardGold} Gold";

            if (requirementsText != null)
            {
                string preferred = contract.hasPreferredClass ? contract.preferredClass.ToString() : "Any";
                requirementsText.text = $"Required Skill: {contract.primarySkillRequired} | Preferred: {preferred}";
            }
        }

        public void CloseMapPanel()
        {
            if (mapPanel != null) mapPanel.SetActive(false);
        }
    }
}

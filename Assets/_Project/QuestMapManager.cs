using UnityEngine;

namespace HeroManagement
{
    public class QuestMapManager : MonoBehaviour
    {
        public static QuestMapManager Instance { get; private set; }

        [Header("Map State")]
        public ContractData selectedContract;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        // Called when you click a village/icon on the 3D or UI map
        public void SelectContractOnMap(ContractData contract)
        {
            if (contract == null) return;

            selectedContract = contract;
            Debug.Log($"[MAP] Selected contract in {contract.targetRegion}. Kingdom: {contract.kingdomName}");

            if (MapUIManager.Instance != null)
            {
                MapUIManager.Instance.ShowContractDetails(contract);
            }
        }

        public void ClearSelection()
        {
            selectedContract = null;
            if (MapUIManager.Instance != null) MapUIManager.Instance.CloseMapPanel();
        }

        // Returns the active expedition for this contract, or null if none
        public MissionDispatcher.ActiveExpedition GetActiveExpedition(ContractData contract)
        {
            if (contract == null || MissionDispatcher.Instance == null) return null;
            return MissionDispatcher.Instance.GetExpeditionForContract(contract);
        }

        // Peek at an expedition while it is travelling
        public void CheckActiveExpeditionForContract(ContractData contract)
        {
            if (contract == null) return;

            var expedition = GetActiveExpedition(contract);

            if (expedition != null)
            {
                Debug.Log($"[MAP] The party heading to {contract.targetRegion} has {expedition.daysRemaining} days left.");
            }
            else
            {
                Debug.LogWarning("[MAP] No active expedition found for this location. The party might have returned or died.");
            }
        }
    }
}

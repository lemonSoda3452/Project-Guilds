using UnityEngine;

namespace HeroManagement
{
    // Emergency sales: buildings (half of what a level cost), heroes (to other taverns) and contracts on the board.
    // Items and scrolls are sold from the Inventory panel.
    public class LiquidationManager : MonoBehaviour
    {
        public static LiquidationManager Instance { get; private set; }

        [Header("Prices")]
        [Tooltip("A hero's price = what it would cost to hire him for 3 missions, times this")]
        [Range(0.1f, 1f)] public float heroSaleFactor = 0.6f;
        [Tooltip("A posted contract is sold to another tavern for this part of its reward")]
        [Range(0.05f, 1f)] public float contractSaleFactor = 0.25f;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Log(string message)
        {
            if (SystemLogUIManager.Instance != null) SystemLogUIManager.Instance.LogMessage(message);
        }

        // ---------------------------------------------------------------- heroes

        public bool CanSellHero(HeroData hero)
        {
            return hero != null && !hero.IsStandard && !hero.isDead && !hero.isBusy;
        }

        public int GetHeroSaleValue(HeroData hero)
        {
            if (hero == null || RecruitmentManager.Instance == null) return 0;

            int cost = RecruitmentManager.Instance.CalculateHireCost(hero, EmploymentType.MissionBased, 3, false, hero.agreedSharePercent);
            return Mathf.Max(10, Mathf.RoundToInt(cost * heroSaleFactor));
        }

        public bool SellHero(HeroData hero)
        {
            if (!CanSellHero(hero) || EmploymentManager.Instance == null || EconomyManager.Instance == null) return false;

            int value = GetHeroSaleValue(hero);

            if (!EmploymentManager.Instance.TryRelease(hero, null)) return false;

            // He now works for another tavern: he will not come back
            if (RecruitmentManager.Instance != null)
            {
                RecruitmentManager.Instance.availableRecruitsDatabase.Remove(hero);
                RecruitmentManager.Instance.currentTavernVisitors.Remove(hero);
            }

            EconomyManager.Instance.AddGold(value);
            Log($"[SALE] {hero.heroName} was sold to another tavern for {value} gold.");
            return true;
        }

        // ---------------------------------------------------------------- contracts

        public bool CanSellContract(ContractData contract)
        {
            return contract != null && !contract.isTowerRaid && !contract.isAccepted && !contract.isCompleted;
        }

        public int GetContractSaleValue(ContractData contract)
        {
            return contract == null ? 0 : Mathf.Max(5, Mathf.RoundToInt(contract.baseRewardGold * contractSaleFactor));
        }

        public bool SellContract(ContractData contract)
        {
            if (!CanSellContract(contract) || ContractBoardManager.Instance == null || EconomyManager.Instance == null) return false;

            int value = GetContractSaleValue(contract);

            ContractBoardManager.Instance.RemovePosted(contract);
            contract.isCompleted = true; // another tavern took it: it never comes back

            EconomyManager.Instance.AddGold(value);
            Log($"[SALE] The contract of {contract.clientName} was sold to another tavern for {value} gold.");
            return true;
        }

        // ---------------------------------------------------------------- buildings

        public bool SellFacility(FacilityType type)
        {
            return TavernFacilitiesManager.Instance != null && TavernFacilitiesManager.Instance.TrySellLevel(type) >= 0;
        }
    }
}

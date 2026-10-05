using System.Collections.Generic;
using UnityEngine;

namespace HeroManagement
{
    // Small random events that make days feel different: a merchant, a brawl, a generous patron, rats in the cellar,
    // a bard, a pickpocket. They happen at the start of the Day phase and are told in the log.
    public class TavernEventManager : MonoBehaviour
    {
        public static TavernEventManager Instance { get; private set; }

        [Header("Frequency")]
        public bool enableEvents = true;
        [Range(0f, 1f)] public float chancePerDay = 0.35f;
        [Tooltip("No events during the first days, so the player can learn the game")]
        public int firstDayWithEvents = 3;

        [Header("Amounts")]
        public int patronGoldMin = 60;
        public int patronGoldMax = 150;
        public int brawlDamageMin = 30;
        public int brawlDamageMax = 80;
        public float merchantPriceMultiplier = 1.5f;
        [Range(0f, 0.3f)] public float pickpocketPercent = 0.05f;
        public int pickpocketMax = 300;
        public int bardLoyaltyBonus = 3;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void OnEnable() { GameManager.OnPhaseChanged += HandlePhase; }
        void OnDisable() { GameManager.OnPhaseChanged -= HandlePhase; }

        private void Log(string message)
        {
            if (SystemLogUIManager.Instance != null) SystemLogUIManager.Instance.LogMessage(message);
        }

        private void HandlePhase(GameManager.DayPhase phase)
        {
            if (!enableEvents || phase != GameManager.DayPhase.Day || GameManager.Instance == null) return;
            if (GameManager.Instance.currentDay < firstDayWithEvents) return;
            if (Random.value > chancePerDay) return;

            switch (Random.Range(0, 6))
            {
                case 0: TravelingMerchant(); break;
                case 1: BarBrawl(); break;
                case 2: GenerousPatron(); break;
                case 3: RatsInTheCellar(); break;
                case 4: WanderingBard(); break;
                default: Pickpocket(); break;
            }
        }

        // ---------------------------------------------------------------- events

        // A merchant buys one item from your stash for more than the market price
        private void TravelingMerchant()
        {
            var inv = InventoryManager.Instance;
            if (inv == null || EconomyManager.Instance == null) return;

            var candidates = new List<ItemData>();
            foreach (var slot in inv.stash)
            {
                if (slot != null && slot.item != null && slot.item.GetSellValue() > 0 && !candidates.Contains(slot.item)) candidates.Add(slot.item);
            }

            if (candidates.Count == 0)
            {
                Log("[EVENT] A traveling merchant stops by, but you have nothing to sell him.");
                return;
            }

            var item = candidates[Random.Range(0, candidates.Count)];
            int price = Mathf.RoundToInt(item.GetSellValue() * merchantPriceMultiplier);

            if (inv.TryConsumeItem(item, 1))
            {
                EconomyManager.Instance.AddGold(price);
                Log($"[EVENT] A traveling merchant loves your {item.GetDisplayName()} and pays {price} gold for it!");
            }
        }

        // Fists, broken chairs and a bill
        private void BarBrawl()
        {
            int damage = Random.Range(brawlDamageMin, brawlDamageMax + 1);
            if (EconomyManager.Instance != null) EconomyManager.Instance.AddGold(-damage);

            string extra = "";
            var heroes = HeroManager.Instance != null ? HeroManager.Instance.GetAvailableHeroes() : new List<HeroData>();
            if (heroes.Count > 0)
            {
                var hero = heroes[Random.Range(0, heroes.Count)];
                hero.injury = Mathf.Min(100, hero.injury + 10);
                hero.restDaysLeft = Mathf.Max(hero.restDaysLeft, hero.GetRestDaysFor(hero.injury));

                if (EmploymentManager.Instance != null) EmploymentManager.Instance.AdjustLoyalty(hero, -3, "he was dragged into a bar brawl");
                extra = $" {hero.heroName} got hurt in the mess.";
            }

            Log($"[EVENT] A brawl breaks out in the tavern! Repairs cost {damage} gold.{extra}");
        }

        private void GenerousPatron()
        {
            int gold = Random.Range(patronGoldMin, patronGoldMax + 1);
            if (EconomyManager.Instance != null) EconomyManager.Instance.AddGold(gold);
            Log($"[EVENT] A generous patron celebrates at your tavern and leaves {gold} gold as a tip!");
        }

        // The rats eat some of your raw materials (or the pantry)
        private void RatsInTheCellar()
        {
            var inv = InventoryManager.Instance;
            var materials = new List<ItemData>();

            if (inv != null)
            {
                foreach (var slot in inv.stash)
                {
                    if (slot != null && slot.item != null && slot.item.itemType == ItemType.Material && !materials.Contains(slot.item))
                        materials.Add(slot.item);
                }
            }

            if (materials.Count > 0)
            {
                var item = materials[Random.Range(0, materials.Count)];
                int lost = Mathf.Min(inv.GetAmount(item), Random.Range(1, 4));
                inv.TryConsumeItem(item, lost);
                Log($"[EVENT] Rats got into the cellar and ruined {lost}x {item.GetDisplayName()}!");
            }
            else if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddGold(-20);
                Log("[EVENT] Rats got into the pantry. The food they ruined costs 20 gold.");
            }
        }

        // A bard cheers up everybody who is at the tavern
        private void WanderingBard()
        {
            var heroes = HeroManager.Instance != null ? HeroManager.Instance.GetAvailableHeroes() : new List<HeroData>();
            int cheered = 0;

            foreach (var hero in heroes)
            {
                if (hero.currentContract == EmploymentType.None) continue;
                hero.loyalty = Mathf.Clamp(hero.loyalty + bardLoyaltyBonus, 0, 100);
                cheered++;
            }

            Log(cheered > 0
                ? $"[EVENT] A wandering bard sings in the tavern. {cheered} hero(es) feel more loyal to the guild."
                : "[EVENT] A wandering bard sings in the tavern, but nobody is around to listen.");
        }

        private void Pickpocket()
        {
            if (EconomyManager.Instance == null) return;

            int stolen = Mathf.Min(pickpocketMax, Mathf.RoundToInt(EconomyManager.Instance.Gold * pickpocketPercent));
            if (stolen <= 0)
            {
                Log("[EVENT] A pickpocket tried his luck in the tavern but found an empty purse.");
                return;
            }

            EconomyManager.Instance.AddGold(-stolen);
            Log($"[EVENT] A pickpocket slipped away with {stolen} gold!");
        }
    }
}

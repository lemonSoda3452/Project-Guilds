using UnityEngine;
using UnityEngine.UI;

namespace HeroManagement
{
    // Inventory: browse the stash, sell, use scrolls and potions, equip and repair gear of a chosen hero
    public class InventoryUIManager : MonoBehaviour
    {
        [Header("UI")]
        public GameObject panel;
        public Text itemListText;
        public Text itemDetailText;
        public Text heroText;

        private int itemIndex = 0;
        private int heroIndex = 0;

        void OnEnable()
        {
            InventoryManager.OnInventoryChanged += Refresh;
            GameManager.OnGoldChanged += HandleGold;
            MissionDispatcher.OnExpeditionsChanged += Refresh;
        }

        void OnDisable()
        {
            InventoryManager.OnInventoryChanged -= Refresh;
            GameManager.OnGoldChanged -= HandleGold;
            MissionDispatcher.OnExpeditionsChanged -= Refresh;
        }

        private void HandleGold(int gold) { Refresh(); }

        private void Log(string message)
        {
            if (SystemLogUIManager.Instance != null) SystemLogUIManager.Instance.LogMessage(message);
        }

        private string RarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Uncommon: return "#66FF66";
                case ItemRarity.Rare: return "#66AAFF";
                case ItemRarity.Epic: return "#CC66FF";
                default: return "#FFFFFF";
            }
        }

        private ItemData CurrentItem()
        {
            var inv = InventoryManager.Instance;
            if (inv == null || inv.stash.Count == 0) return null;

            itemIndex = Mathf.Clamp(itemIndex, 0, inv.stash.Count - 1);
            var slot = inv.stash[itemIndex];
            return slot != null ? slot.item : null;
        }

        private HeroData CurrentHero()
        {
            var list = HeroManager.GetGuildHeroes();
            if (list.Count == 0) return null;

            heroIndex = Mathf.Clamp(heroIndex, 0, list.Count - 1);
            return list[heroIndex];
        }

        public void Refresh()
        {
            if (panel == null || !panel.activeInHierarchy) return;

            var inv = InventoryManager.Instance;
            if (inv == null)
            {
                if (itemListText != null) itemListText.text = "InventoryManager non trovato.";
                return;
            }

            // ---- Stash list
            if (inv.stash.Count > 0) itemIndex = Mathf.Clamp(itemIndex, 0, inv.stash.Count - 1);
            else itemIndex = 0;

            if (itemListText != null)
            {
                string list = $"<b>MAGAZZINO ({inv.stash.Count}/{inv.maxStashSlots})</b>\n";
                if (inv.stash.Count == 0) list += "<i>vuoto</i>";

                for (int i = 0; i < inv.stash.Count; i++)
                {
                    var slot = inv.stash[i];
                    if (slot == null || slot.item == null) continue;

                    string marker = i == itemIndex ? "<color=#FFD700>></color> " : "   ";
                    list += $"{marker}<color={RarityColor(slot.item.rarity)}>{slot.item.GetDisplayName()}</color> x{slot.amount}\n";
                }

                itemListText.text = list;
            }

            // ---- Selected item
            var item = CurrentItem();
            if (itemDetailText != null)
            {
                if (item == null)
                {
                    itemDetailText.text = "";
                }
                else
                {
                    string d = $"<b><color={RarityColor(item.rarity)}>{item.GetDisplayName()}</color></b>  ({item.itemType}, {item.rarity})\n";
                    d += $"<color=#CCCCCC>{item.description}</color>\n";
                    d += $"Posseduti: {inv.GetAmount(item)} | Peso: {item.weight:0.#} kg ciascuno\n";
                    d += $"Valore di vendita: <color=#FFD700>{item.GetSellValue()} oro</color>\n";

                    if (item.IsEquipment) d += $"Bonus potere: +{item.powerBonus}\n";
                    if (item.itemType == ItemType.Scroll && item.skillTaught != null) d += $"Insegna: {item.skillTaught.skillName}\n";
                    if (item.itemType == ItemType.Consumable && item.healInjury > 0) d += $"Cura {item.healInjury}% di ferite\n";

                    itemDetailText.text = d;
                }
            }

            // ---- Selected hero
            var hero = CurrentHero();
            if (heroText != null)
            {
                if (hero == null)
                {
                    heroText.text = "Nessun eroe in gilda.";
                }
                else
                {
                    string h = $"<b>EROE: {hero.heroName}</b> (Lv.{hero.level} {hero.heroClass})\n";
                    h += $"Ferite {hero.injury}% | {(hero.isBusy ? "in missione" : "a casa")}\n";
                    h += hero.equippedWeapon != null ? $"Arma: {hero.equippedWeapon.GetDisplayName()} ({hero.weaponDurability}%)\n" : "Arma: -\n";
                    h += hero.equippedArmor != null ? $"Armatura: {hero.equippedArmor.GetDisplayName()} ({hero.armorDurability}%)\n" : "Armatura: -\n";
                    h += $"Gear {hero.GetGearRating()} | peso {hero.GetGearWeight():0.#}/{hero.GetCarryCapacity():0} kg\n";

                    bool needsRepair = (hero.equippedWeapon != null && hero.weaponDurability < 100) || (hero.equippedArmor != null && hero.armorDurability < 100);
                    if (needsRepair)
                    {
                        bool forge = TavernFacilitiesManager.Instance == null || TavernFacilitiesManager.Instance.HasForge;
                        h += forge
                            ? $"Riparazione: <color=#FFD700>{Mathf.Max(1, inv.GetRepairCost(hero))} oro</color>"
                            : "<color=#FF6666>Serve una Fucina per riparare</color>";
                    }

                    heroText.text = h;
                }
            }
        }

        // ---- Buttons
        public void OnPrevItemClicked()
        {
            var inv = InventoryManager.Instance;
            if (inv == null || inv.stash.Count == 0) return;

            itemIndex = (itemIndex - 1 + inv.stash.Count) % inv.stash.Count;
            Refresh();
        }

        public void OnNextItemClicked()
        {
            var inv = InventoryManager.Instance;
            if (inv == null || inv.stash.Count == 0) return;

            itemIndex = (itemIndex + 1) % inv.stash.Count;
            Refresh();
        }

        public void OnPrevHeroClicked()
        {
            var list = HeroManager.GetGuildHeroes();
            if (list.Count == 0) return;

            heroIndex = (heroIndex - 1 + list.Count) % list.Count;
            Refresh();
        }

        public void OnNextHeroClicked()
        {
            var list = HeroManager.GetGuildHeroes();
            if (list.Count == 0) return;

            heroIndex = (heroIndex + 1) % list.Count;
            Refresh();
        }

        public void OnSellClicked()
        {
            var item = CurrentItem();
            if (item == null || InventoryManager.Instance == null) return;

            InventoryManager.Instance.SellItem(item, 1);
            Refresh();
        }

        public void OnSellAllClicked()
        {
            var item = CurrentItem();
            if (item == null || InventoryManager.Instance == null) return;

            InventoryManager.Instance.SellItem(item, InventoryManager.Instance.GetAmount(item));
            Refresh();
        }

        // Scroll -> teach the skill, weapon/armor -> equip, potion -> heal
        public void OnUseClicked()
        {
            var item = CurrentItem();
            var hero = CurrentHero();
            var inv = InventoryManager.Instance;
            if (item == null || hero == null || inv == null) return;

            switch (item.itemType)
            {
                case ItemType.Scroll: inv.UseScroll(item, hero); break;
                case ItemType.Weapon:
                case ItemType.Armor: inv.EquipItem(hero, item); break;
                case ItemType.Consumable: inv.UseConsumable(item, hero); break;
                default: Log($"{item.GetDisplayName()} cannot be used."); break;
            }

            Refresh();
        }

        public void OnRepairClicked()
        {
            var hero = CurrentHero();
            if (hero == null || InventoryManager.Instance == null) return;

            InventoryManager.Instance.RepairGear(hero);
            Refresh();
        }
    }
}

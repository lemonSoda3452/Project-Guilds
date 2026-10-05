using UnityEngine;
using System.Collections.Generic;

namespace HeroManagement
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [System.Serializable]
        public class InventorySlot
        {
            public ItemData item;
            public int amount;

            public bool IsFull => item != null && amount >= Mathf.Max(1, item.maxStack);
        }

        [Header("Guild Stash")]
        [Tooltip("All the items, materials, and scrolls owned by the guild.")]
        public List<InventorySlot> stash = new List<InventorySlot>();

        [Header("Limits")]
        [Tooltip("Maximum number of different item slots available in the stash")]
        public int maxStashSlots = 30;

        // For inventory UIs: refresh when anything changes
        public static event System.Action OnInventoryChanged;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            SanitizeStash();
        }

        private void Log(string message)
        {
            if (SystemLogUIManager.Instance != null) SystemLogUIManager.Instance.LogMessage(message);
        }

        private static int StackLimit(ItemData item)
        {
            return Mathf.Max(1, item.maxStack);
        }

        // Cleans slots edited by hand in the Inspector (empty, zero amount, over the stack limit)
        private void SanitizeStash()
        {
            for (int i = stash.Count - 1; i >= 0; i--)
            {
                var slot = stash[i];
                if (slot == null || slot.item == null || slot.amount <= 0)
                {
                    stash.RemoveAt(i);
                    continue;
                }

                slot.amount = Mathf.Min(slot.amount, StackLimit(slot.item));
            }
        }

        public int GetAmount(ItemData item)
        {
            if (item == null) return 0;

            int total = 0;
            foreach (var slot in stash)
            {
                if (slot != null && slot.item == item) total += slot.amount;
            }
            return total;
        }

        public bool HasItem(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return false;
            return GetAmount(item) >= amount;
        }

        // True if the whole amount fits (free space in existing stacks + free slots)
        public bool CanAdd(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return false;

            int limit = StackLimit(item);
            long space = 0;

            foreach (var slot in stash)
            {
                if (slot != null && slot.item == item) space += Mathf.Max(0, limit - slot.amount);
            }

            int freeSlots = Mathf.Max(0, maxStashSlots - stash.Count);
            space += (long)freeSlots * limit;

            return space >= amount;
        }

        // All or nothing: if it does not fit, nothing is added and false is returned
        public bool AddItem(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return false;

            if (!CanAdd(item, amount))
            {
                Debug.LogWarning("[INVENTORY] Guild stash is full! Nothing was added.");
                Log("WARNING: Guild stash is full! Items were left behind.");
                return false;
            }

            int limit = StackLimit(item);
            int remaining = amount;

            // 1. Fill existing stacks first
            foreach (var slot in stash)
            {
                if (remaining <= 0) break;
                if (slot == null || slot.item != item || slot.IsFull) continue;

                int add = Mathf.Min(remaining, limit - slot.amount);
                slot.amount += add;
                remaining -= add;
            }

            // 2. Then open new slots
            while (remaining > 0)
            {
                int add = Mathf.Min(remaining, limit);
                stash.Add(new InventorySlot { item = item, amount = add });
                remaining -= add;
            }

            Debug.Log($"[INVENTORY] Added {amount}x {item.GetDisplayName()} to the stash.");
            Log(item.itemType == ItemType.Scroll
                ? $"Acquired Scroll: {item.GetDisplayName()}!"
                : $"Added {amount}x {item.GetDisplayName()} to inventory.");

            OnInventoryChanged?.Invoke();
            return true;
        }

        // Removes items from the stash (crafting, selling, using)
        public bool TryConsumeItem(ItemData item, int amount)
        {
            if (!HasItem(item, amount)) return false;

            int remaining = amount;

            for (int i = stash.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var slot = stash[i];
                if (slot == null || slot.item != item) continue;

                if (slot.amount <= remaining)
                {
                    remaining -= slot.amount;
                    stash.RemoveAt(i);
                }
                else
                {
                    slot.amount -= remaining;
                    remaining = 0;
                }
            }

            Debug.Log($"[INVENTORY] Consumed {amount}x {item.GetDisplayName()} from stash.");
            OnInventoryChanged?.Invoke();
            return true;
        }

        // Sells items to the market for gold
        public bool SellItem(ItemData item, int amount)
        {
            if (item == null || amount <= 0 || EconomyManager.Instance == null) return false;
            if (!HasItem(item, amount)) return false;

            int total = item.GetSellValue() * amount;

            if (!TryConsumeItem(item, amount)) return false;

            EconomyManager.Instance.AddGold(total);
            Log($"Sold {amount}x {item.GetDisplayName()} for {total} gold.");
            return true;
        }

        // Uses a scroll on a hero. The scroll is consumed ONLY if the hero really learns the skill
        // (if the player cancels the replacement panel, the scroll stays in the stash).
        public bool UseScroll(ItemData scroll, HeroData hero)
        {
            if (scroll == null || hero == null || hero.isDead) return false;
            if (scroll.itemType != ItemType.Scroll || scroll.skillTaught == null) return false;
            if (!HasItem(scroll, 1)) return false;
            if (SkillReplacementUIManager.Instance == null)
            {
                Debug.LogWarning("[INVENTORY] No SkillReplacementUIManager in the scene: cannot use scrolls.");
                return false;
            }

            return SkillReplacementUIManager.Instance.TryLearnNewSkill(hero, scroll.skillTaught, learned =>
            {
                if (learned) TryConsumeItem(scroll, 1);
            });
        }

        // Uses a consumable on a hero (potions heal injuries). Returns true if it was consumed.
        public bool UseConsumable(ItemData item, HeroData hero)
        {
            if (item == null || hero == null || hero.isDead) return false;

            if (item.itemType != ItemType.Consumable || item.healInjury <= 0)
            {
                Log($"{item.GetDisplayName()} cannot be used like this.");
                return false;
            }

            if (hero.injury <= 0)
            {
                Log($"{hero.heroName} is not injured.");
                return false;
            }

            if (!TryConsumeItem(item, 1)) return false;

            hero.injury = Mathf.Max(0, hero.injury - item.healInjury);
            if (!hero.isBusy) hero.restDaysLeft = Mathf.Min(hero.restDaysLeft, hero.GetRestDaysFor(hero.injury));

            Log($"{hero.heroName} uses {item.GetDisplayName()}: injury is now {hero.injury}%.");
            return true;
        }

        // Equips a weapon or armor from the stash. The previous item goes back to the stash.
        public bool EquipItem(HeroData hero, ItemData item)
        {
            if (hero == null || item == null || hero.isDead || hero.isBusy) return false;
            if (!item.IsEquipment || !HasItem(item, 1)) return false;

            bool isWeapon = item.itemType == ItemType.Weapon;
            ItemData old = isWeapon ? hero.equippedWeapon : hero.equippedArmor;

            TryConsumeItem(item, 1);

            if (old != null && !AddItem(old, 1))
            {
                AddItem(item, 1); // no room for the old item: put the new one back, nothing changes
                return false;
            }

            hero.SetEquipment(item, isWeapon);
            Log($"{hero.heroName} equipped {item.GetDisplayName()}.");
            return true;
        }

        // Repair cost: 40% of the item value, scaled by the missing durability
        public int GetRepairCost(HeroData hero)
        {
            if (hero == null) return 0;

            float cost = 0f;
            if (hero.equippedWeapon != null) cost += hero.equippedWeapon.goldValue * 0.4f * (100 - hero.weaponDurability) / 100f;
            if (hero.equippedArmor != null) cost += hero.equippedArmor.goldValue * 0.4f * (100 - hero.armorDurability) / 100f;
            if (TavernFacilitiesManager.Instance != null) cost *= TavernFacilitiesManager.Instance.GetRepairMultiplier();
            return Mathf.CeilToInt(cost);
        }

        public bool RepairGear(HeroData hero)
        {
            if (hero == null || hero.isBusy || hero.isDead) return false;

            if (TavernFacilitiesManager.Instance != null && !TavernFacilitiesManager.Instance.HasForge)
            {
                Log("You need a Forge to repair gear.");
                return false;
            }

            bool needsRepair = (hero.equippedWeapon != null && hero.weaponDurability < 100) ||
                               (hero.equippedArmor != null && hero.armorDurability < 100);
            if (!needsRepair) return false;

            int cost = Mathf.Max(1, GetRepairCost(hero));

            if (EconomyManager.Instance == null || !EconomyManager.Instance.TrySpendGold(cost))
            {
                Log($"Not enough gold to repair {hero.heroName}'s gear ({cost} gold).");
                return false;
            }

            hero.weaponDurability = 100;
            hero.armorDurability = 100;
            Log($"{hero.heroName}'s gear was repaired for {cost} gold.");
            return true;
        }

        public void RestoreStash(List<InventorySlot> slots)
        {
            stash = slots ?? new List<InventorySlot>();
            OnInventoryChanged?.Invoke();
        }

        // Editor/testing helper. Not called automatically: the stash you set in the Inspector is your starting inventory.
        public void ResetRuntimeState()
        {
            stash.Clear();
            OnInventoryChanged?.Invoke();
        }
    }
}

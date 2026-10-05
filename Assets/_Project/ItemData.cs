using UnityEngine;

namespace HeroManagement
{
    // Keep the order: Unity saves enums as integers. Add new values only at the END.
    public enum ItemType { Material, Consumable, Scroll, Valuables, Weapon, Armor }
    public enum ItemRarity { Common, Uncommon, Rare, Epic }

    [CreateAssetMenu(fileName = "New Item", menuName = "Hero Management/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Item Info")]
        public string itemName;
        [TextArea(2, 3)]
        public string description;
        public Sprite icon;

        [Header("Classification")]
        public ItemType itemType;
        public ItemRarity rarity;

        [Header("Inventory")]
        [Tooltip("How many copies fit in one inventory slot (1 = not stackable)")]
        [Min(1)] public int maxStack = 99;

        [Header("Economy")]
        [Tooltip("How much gold the guild gets if they sell this item")]
        [Min(0)] public int goldValue;

        [Header("Weight & Equipment")]
        [Tooltip("Weight in kg. Heroes carry loot and gear: heavy loads slow the way home.")]
        [Min(0f)] public float weight = 1f;
        [Tooltip("Weapons/Armor only: bonus to the hero's gear rating")]
        public int powerBonus = 0;

        public bool IsEquipment { get { return itemType == ItemType.Weapon || itemType == ItemType.Armor; } }

        [Header("Consumable")]
        [Tooltip("Injury points healed when used (potions)")]
        [Min(0)] public int healInjury = 0;

        [Header("Scroll Only")]
        [Tooltip("The skill a hero learns when this scroll is used (only for ItemType.Scroll)")]
        public SkillData skillTaught;

        // For scrolls the skill's own market value wins, so the price is defined in one place
        public int GetSellValue()
        {
            if (itemType == ItemType.Scroll && skillTaught != null && skillTaught.scrollMarketValue > 0)
                return skillTaught.scrollMarketValue;

            return goldValue;
        }

        public string GetDisplayName()
        {
            return string.IsNullOrEmpty(itemName) ? name : itemName;
        }

#if UNITY_EDITOR
        // Warns in the Console while you are editing the asset, not during play
        void OnValidate()
        {
            if (itemType == ItemType.Scroll && skillTaught == null)
                Debug.LogWarning($"[ITEM] Scroll '{name}' has no skillTaught assigned.", this);

            if (itemType != ItemType.Scroll && skillTaught != null)
                Debug.LogWarning($"[ITEM] '{name}' is not a Scroll but has a skillTaught assigned.", this);
        }
#endif
    }
}

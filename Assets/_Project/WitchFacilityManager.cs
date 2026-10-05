using UnityEngine;
using System.Collections.Generic;

namespace HeroManagement
{
    public class WitchFacilityManager : MonoBehaviour
    {
        public static WitchFacilityManager Instance { get; private set; }

        [System.Serializable]
        public class MaterialCost
        {
            public ItemData item;
            [Min(1)] public int amount = 1;
        }

        [System.Serializable]
        public class ScrollRecipe
        {
            [Tooltip("The skill this scroll teaches")]
            public SkillData skill;
            [Tooltip("The Scroll item that ends up in the stash (its skillTaught should be the same skill)")]
            public ItemData scrollItem;
            public int goldCost = 100;
            public List<MaterialCost> materials = new List<MaterialCost>();
        }

        [Header("Potential Costs")]
        public int tempPotentialGoldCost = 50;
        public int permPotentialGoldCost = 1000;

        [Header("Scroll Crafting")]
        public List<ScrollRecipe> recipes = new List<ScrollRecipe>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Log(string message)
        {
            if (SystemLogUIManager.Instance != null) SystemLogUIManager.Instance.LogMessage(message);
        }

        // ---------------- POTENTIAL ----------------

        // Unlocks the potential for a single mission
        public bool UnlockTemporaryPotential(HeroData hero)
        {
            if (hero == null || hero.hasTemporaryPotential || hero.isDead) return false;

            if (EconomyManager.Instance != null && EconomyManager.Instance.TrySpendGold(tempPotentialGoldCost))
            {
                hero.hasTemporaryPotential = true;
                Log($"The Witch awakens {hero.heroName}'s potential for the next mission.");
                return true;
            }

            Log($"Not enough gold for Temporary Potential ({tempPotentialGoldCost} Gold).");
            return false;
        }

        // Expensive ritual for a unique permanent buff
        public bool UnlockPermanentPotential(HeroData hero)
        {
            if (hero == null || hero.hasPermanentPotential || hero.isDead) return false;

            if (EconomyManager.Instance != null && EconomyManager.Instance.TrySpendGold(permPotentialGoldCost))
            {
                hero.hasPermanentPotential = true;
                hero.permanentEnchantmentDesc = GenerateUniqueEnchantment(hero.heroRace, hero.heroClass);

                Log($"PERMANENT POTENTIAL for {hero.heroName}: {hero.permanentEnchantmentDesc}");
                return true;
            }

            Log($"Not enough gold for Permanent Potential ({permPotentialGoldCost} Gold).");
            return false;
        }

        private string GenerateUniqueEnchantment(HeroRace race, HeroClass hClass)
        {
            if (race == HeroRace.Orc && hClass == HeroClass.Warrior)
                return "Blood Fury: Fills skill slots with physical damage multipliers. Immune to pain.";
            if (race == HeroRace.Elf && hClass == HeroClass.Mage)
                return "Astral Focus: Spells cost half mana. Elemental damage ignores basic armor.";
            if (race == HeroRace.Dwarf && hClass == HeroClass.Cleric)
                return "Stone Blessing: Healing spells also grant physical shields based on defense.";
            if (race == HeroRace.Beastkin && hClass == HeroClass.Rogue)
                return "Feral Instinct: Critical hits cause heavy bleeding. Always attacks first.";

            return $"Awakened {race} Soul: Enhances all primary {hClass} abilities by 30%.";
        }

        // ---------------- SCROLL CRAFTING ----------------

        public ScrollRecipe FindRecipe(SkillData skill)
        {
            if (skill == null) return null;
            return recipes.Find(r => r != null && r.skill == skill);
        }

        public bool CanCraft(ScrollRecipe recipe)
        {
            if (recipe == null || recipe.skill == null || recipe.scrollItem == null) return false;
            if (EconomyManager.Instance == null || InventoryManager.Instance == null) return false;
            if (EconomyManager.Instance.Gold < recipe.goldCost) return false;

            foreach (var mat in recipe.materials)
            {
                if (mat == null || mat.item == null) continue;
                if (!InventoryManager.Instance.HasItem(mat.item, mat.amount)) return false;
            }

            return InventoryManager.Instance.CanAdd(recipe.scrollItem, 1);
        }

        // Checks everything first, then spends: it never takes your gold or materials for nothing
        public bool CraftScroll(SkillData targetSkill)
        {
            var recipe = FindRecipe(targetSkill);

            if (recipe == null)
            {
                Log("The Witch does not know how to make that scroll.");
                return false;
            }

            if (!CanCraft(recipe))
            {
                Log($"Cannot craft the scroll of {targetSkill.skillName}: missing gold, materials or stash space.");
                return false;
            }

            EconomyManager.Instance.TrySpendGold(recipe.goldCost);

            foreach (var mat in recipe.materials)
            {
                if (mat == null || mat.item == null) continue;
                InventoryManager.Instance.TryConsumeItem(mat.item, mat.amount);
            }

            InventoryManager.Instance.AddItem(recipe.scrollItem, 1);
            Log($"The Witch crafted a scroll of {targetSkill.skillName}.");
            return true;
        }
    }
}

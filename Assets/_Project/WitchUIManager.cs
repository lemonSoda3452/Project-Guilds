using UnityEngine;
using UnityEngine.UI;

namespace HeroManagement
{
    // The Witch: awakens a hero's potential and writes scrolls from ore and gold
    public class WitchUIManager : MonoBehaviour
    {
        [Header("UI")]
        public GameObject panel;
        public Text heroText;
        public Text recipeText;

        private int heroIndex = 0;
        private int recipeIndex = 0;

        void OnEnable()
        {
            GameManager.OnGoldChanged += HandleGold;
            InventoryManager.OnInventoryChanged += Refresh;
            MissionDispatcher.OnExpeditionsChanged += Refresh;
        }

        void OnDisable()
        {
            GameManager.OnGoldChanged -= HandleGold;
            InventoryManager.OnInventoryChanged -= Refresh;
            MissionDispatcher.OnExpeditionsChanged -= Refresh;
        }

        private void HandleGold(int gold) { Refresh(); }

        private HeroData CurrentHero()
        {
            var list = HeroManager.GetGuildHeroes();
            if (list.Count == 0) return null;

            heroIndex = Mathf.Clamp(heroIndex, 0, list.Count - 1);
            return list[heroIndex];
        }

        private WitchFacilityManager.ScrollRecipe CurrentRecipe()
        {
            var witch = WitchFacilityManager.Instance;
            if (witch == null || witch.recipes.Count == 0) return null;

            recipeIndex = Mathf.Clamp(recipeIndex, 0, witch.recipes.Count - 1);
            return witch.recipes[recipeIndex];
        }

        public void Refresh()
        {
            if (panel == null || !panel.activeInHierarchy) return;

            var witch = WitchFacilityManager.Instance;
            var hero = CurrentHero();

            if (heroText != null)
            {
                if (hero == null || witch == null)
                {
                    heroText.text = witch == null ? "WitchFacilityManager non trovato." : "Nessun eroe in gilda.";
                }
                else
                {
                    string t = $"<b>{hero.heroName}</b> (Lv.{hero.level} {hero.heroRace} {hero.heroClass})\n\n";

                    t += hero.hasTemporaryPotential
                        ? "Potenziale temporaneo: <color=#88CCFF>ATTIVO</color> (dura una missione)\n"
                        : $"Potenziale temporaneo: no  (<color=#FFD700>{witch.tempPotentialGoldCost} oro</color>)\n";

                    t += hero.hasPermanentPotential
                        ? $"Potenziale permanente: <color=#CC88FF>{hero.permanentEnchantmentDesc}</color>\n"
                        : $"Potenziale permanente: no  (<color=#FFD700>{witch.permPotentialGoldCost} oro</color>)\n";

                    t += "\n<b>Abilità conosciute</b>\n";
                    hero.NormalizeSkillSlots();
                    for (int i = 0; i < HeroData.MaxSkillSlots; i++)
                    {
                        var s = hero.equippedSkills[i];
                        t += $"{i + 1}. {(s != null ? s.skillName : "<color=#777777>vuoto</color>")}\n";
                    }

                    heroText.text = t;
                }
            }

            if (recipeText != null)
            {
                var recipe = CurrentRecipe();

                if (witch == null || recipe == null || recipe.skill == null)
                {
                    recipeText.text = "<b>PERGAMENE</b>\nLa Strega non conosce ancora nessuna ricetta.";
                }
                else
                {
                    string r = $"<b>PERGAMENA {recipeIndex + 1}/{witch.recipes.Count}</b>\n";
                    r += $"{(recipe.scrollItem != null ? recipe.scrollItem.GetDisplayName() : "?")} -> insegna <b>{recipe.skill.skillName}</b>\n";
                    r += $"<color=#CCCCCC>{recipe.skill.description}</color>\n\n";
                    r += $"Costo: <color=#FFD700>{recipe.goldCost} oro</color>\n";
                    r += "Materiali:\n";

                    foreach (var mat in recipe.materials)
                    {
                        if (mat == null || mat.item == null) continue;
                        int have = InventoryManager.Instance != null ? InventoryManager.Instance.GetAmount(mat.item) : 0;
                        string color = have >= mat.amount ? "#88FF88" : "#FF6666";
                        r += $" - {mat.item.GetDisplayName()} x{mat.amount} <color={color}>(hai {have})</color>\n";
                    }

                    r += witch.CanCraft(recipe) ? "\n<color=#88FF88>Puoi crearla.</color>" : "\n<color=#FF6666>Mancano oro, materiali o spazio.</color>";
                    recipeText.text = r;
                }
            }
        }

        // ---- Buttons
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

        public void OnPrevRecipeClicked()
        {
            var witch = WitchFacilityManager.Instance;
            if (witch == null || witch.recipes.Count == 0) return;

            recipeIndex = (recipeIndex - 1 + witch.recipes.Count) % witch.recipes.Count;
            Refresh();
        }

        public void OnNextRecipeClicked()
        {
            var witch = WitchFacilityManager.Instance;
            if (witch == null || witch.recipes.Count == 0) return;

            recipeIndex = (recipeIndex + 1) % witch.recipes.Count;
            Refresh();
        }

        public void OnTempPotentialClicked()
        {
            var hero = CurrentHero();
            if (hero == null || WitchFacilityManager.Instance == null) return;

            WitchFacilityManager.Instance.UnlockTemporaryPotential(hero);
            Refresh();
        }

        public void OnPermPotentialClicked()
        {
            var hero = CurrentHero();
            if (hero == null || WitchFacilityManager.Instance == null) return;

            WitchFacilityManager.Instance.UnlockPermanentPotential(hero);
            Refresh();
        }

        public void OnCraftClicked()
        {
            var recipe = CurrentRecipe();
            if (recipe == null || WitchFacilityManager.Instance == null) return;

            WitchFacilityManager.Instance.CraftScroll(recipe.skill);
            Refresh();
        }
    }
}

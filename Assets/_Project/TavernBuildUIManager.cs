using UnityEngine;
using UnityEngine.UI;

namespace HeroManagement
{
    // Tavern buildings (beds, brewery, kitchen, forge) and the forge's recipes and jobs
    public class TavernBuildUIManager : MonoBehaviour
    {
        [Header("UI")]
        public GameObject panel;
        public Text facilitiesText;
        public Text forgeText;
        public Text summaryText;

        private int facilityIndex = 0;
        private int recipeIndex = 0;

        void OnEnable()
        {
            TavernFacilitiesManager.OnFacilitiesChanged += Refresh;
            ForgeManager.OnForgeChanged += Refresh;
            InventoryManager.OnInventoryChanged += Refresh;
            GameManager.OnGoldChanged += HandleGold;
        }

        void OnDisable()
        {
            TavernFacilitiesManager.OnFacilitiesChanged -= Refresh;
            ForgeManager.OnForgeChanged -= Refresh;
            InventoryManager.OnInventoryChanged -= Refresh;
            GameManager.OnGoldChanged -= HandleGold;
        }

        private void HandleGold(int gold) { Refresh(); }

        public void Refresh()
        {
            if (panel == null || !panel.activeInHierarchy) return;

            RefreshFacilities();
            RefreshForge();
            RefreshSummary();
        }

        private void RefreshFacilities()
        {
            if (facilitiesText == null) return;

            var fm = TavernFacilitiesManager.Instance;
            if (fm == null || fm.facilities.Count == 0)
            {
                facilitiesText.text = "TavernFacilitiesManager non trovato.";
                return;
            }

            facilityIndex = Mathf.Clamp(facilityIndex, 0, fm.facilities.Count - 1);

            string t = "<b>STRUTTURE</b>\n";
            for (int i = 0; i < fm.facilities.Count; i++)
            {
                var cfg = fm.facilities[i];
                if (cfg == null) continue;

                int level = fm.GetLevel(cfg.type);
                int next = fm.GetNextCost(cfg.type);
                string marker = i == facilityIndex ? "<color=#FFD700>></color> " : "   ";
                string nextText = next < 0 ? "<color=#88FF88>MAX</color>" : $"prossimo: <color=#FFD700>{next} oro</color>";

                t += $"{marker}<b>{cfg.displayName}</b> Lv.{level}/{cfg.MaxLevel}  ({nextText})\n";
                t += $"      <size=15><color=#CCCCCC>{fm.GetEffectText(cfg.type, level)}</color></size>\n";
            }

            var selected = fm.facilities[facilityIndex];
            if (selected != null) t += $"\n<i>{selected.description}</i>";

            facilitiesText.text = t;
        }

        private void RefreshForge()
        {
            if (forgeText == null) return;

            var forge = ForgeManager.Instance;
            if (forge == null)
            {
                forgeText.text = "ForgeManager non trovato.";
                return;
            }

            int level = forge.GetForgeLevel();
            string t = $"<b>FUCINA</b> Lv.{level}  ({forge.jobs.Count}/{level} fornaci in uso)\n";

            if (forge.recipes.Count == 0)
            {
                t += "Nessuna ricetta.";
                forgeText.text = t;
                return;
            }

            recipeIndex = Mathf.Clamp(recipeIndex, 0, forge.recipes.Count - 1);

            for (int i = 0; i < forge.recipes.Count; i++)
            {
                var r = forge.recipes[i];
                if (r == null) continue;

                string marker = i == recipeIndex ? "<color=#FFD700>></color> " : "   ";
                t += $"{marker}{r.GetName()} x{r.amount}  <size=15>(Fucina Lv.{r.requiredForgeLevel}, {r.days} gg)</size>\n";
            }

            var sel = forge.recipes[recipeIndex];
            if (sel != null)
            {
                t += $"\n<b>{sel.GetName()}</b>: <color=#FFD700>{sel.goldCost} oro</color>\n";

                foreach (var mat in sel.materials)
                {
                    if (mat == null || mat.item == null) continue;
                    int have = InventoryManager.Instance != null ? InventoryManager.Instance.GetAmount(mat.item) : 0;
                    string color = have >= mat.amount ? "#88FF88" : "#FF6666";
                    t += $" - {mat.item.GetDisplayName()} x{mat.amount} <color={color}>(hai {have})</color>\n";
                }

                string reason;
                t += forge.CanForge(sel, out reason) ? "<color=#88FF88>Pronta da forgiare.</color>\n" : $"<color=#FF6666>{reason}</color>\n";
            }

            if (forge.jobs.Count > 0)
            {
                t += "\n<b>IN LAVORAZIONE</b>\n";
                foreach (var job in forge.jobs)
                {
                    if (job != null && job.recipe != null) t += $" - {job.recipe.GetName()}: {job.daysLeft} giorno/i\n";
                }
            }

            forgeText.text = t;
        }

        private void RefreshSummary()
        {
            if (summaryText == null) return;

            var fm = TavernFacilitiesManager.Instance;
            if (fm == null) { summaryText.text = ""; return; }

            summaryText.text =
                $"Letti: {fm.GetUsedBeds()}/{fm.GetBedCapacity()} occupati | Riposo x{fm.GetRecoveryMultiplier():0.00}\n" +
                $"Entrate giornaliere: ~{fm.GetDailyIncome()} oro | Mantenimento: -{(1f - fm.GetUpkeepMultiplier()) * 100f:0}%\n" +
                $"Riparazioni: -{(1f - fm.GetRepairMultiplier()) * 100f:0}%" +
                (TowerManager.Instance != null ? "\n" + TowerManager.Instance.GetStatusText() : "") +
                (ReputationManager.Instance != null ? "\n" + ReputationManager.Instance.GetStatusText() : "");
        }

        // ---- Buttons
        public void OnPrevFacilityClicked()
        {
            var fm = TavernFacilitiesManager.Instance;
            if (fm == null || fm.facilities.Count == 0) return;

            facilityIndex = (facilityIndex - 1 + fm.facilities.Count) % fm.facilities.Count;
            Refresh();
        }

        public void OnNextFacilityClicked()
        {
            var fm = TavernFacilitiesManager.Instance;
            if (fm == null || fm.facilities.Count == 0) return;

            facilityIndex = (facilityIndex + 1) % fm.facilities.Count;
            Refresh();
        }

        public void OnUpgradeClicked()
        {
            var fm = TavernFacilitiesManager.Instance;
            if (fm == null || fm.facilities.Count == 0) return;

            facilityIndex = Mathf.Clamp(facilityIndex, 0, fm.facilities.Count - 1);
            var cfg = fm.facilities[facilityIndex];
            if (cfg != null) fm.TryUpgrade(cfg.type);
            Refresh();
        }

        public void OnPrevRecipeClicked()
        {
            var forge = ForgeManager.Instance;
            if (forge == null || forge.recipes.Count == 0) return;

            recipeIndex = (recipeIndex - 1 + forge.recipes.Count) % forge.recipes.Count;
            Refresh();
        }

        public void OnNextRecipeClicked()
        {
            var forge = ForgeManager.Instance;
            if (forge == null || forge.recipes.Count == 0) return;

            recipeIndex = (recipeIndex + 1) % forge.recipes.Count;
            Refresh();
        }

        public void OnForgeClicked()
        {
            var forge = ForgeManager.Instance;
            if (forge == null || forge.recipes.Count == 0) return;

            recipeIndex = Mathf.Clamp(recipeIndex, 0, forge.recipes.Count - 1);
            forge.StartForge(forge.recipes[recipeIndex]);
            Refresh();
        }
    }
}

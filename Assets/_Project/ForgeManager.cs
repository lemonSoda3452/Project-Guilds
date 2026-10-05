using System.Collections.Generic;
using UnityEngine;

namespace HeroManagement
{
    // The Forge: turns ore and gold into weapons and armor, which you sell or give to your heroes.
    // Each forge level adds one furnace (one job at a time per furnace).
    public class ForgeManager : MonoBehaviour
    {
        public static ForgeManager Instance { get; private set; }

        [System.Serializable]
        public class ForgeRecipe
        {
            public ItemData result;
            [Min(1)] public int amount = 1;
            public int goldCost = 50;
            public List<WitchFacilityManager.MaterialCost> materials = new List<WitchFacilityManager.MaterialCost>();
            [Min(1)] public int days = 1;
            [Min(1)] public int requiredForgeLevel = 1;

            public string GetName() { return result != null ? result.GetDisplayName() : "???"; }
        }

        [System.Serializable]
        public class ForgeJob
        {
            public ForgeRecipe recipe;
            public int daysLeft;
        }

        [Header("Recipes")]
        public List<ForgeRecipe> recipes = new List<ForgeRecipe>();

        [Header("Jobs in progress (runtime)")]
        public List<ForgeJob> jobs = new List<ForgeJob>();

        public static event System.Action OnForgeChanged;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void OnEnable() { GameManager.OnDayChanged += HandleNewDay; }
        void OnDisable() { GameManager.OnDayChanged -= HandleNewDay; }

        private void Log(string message)
        {
            if (SystemLogUIManager.Instance != null) SystemLogUIManager.Instance.LogMessage(message);
        }

        public int GetForgeLevel()
        {
            return TavernFacilitiesManager.Instance != null ? TavernFacilitiesManager.Instance.GetLevel(FacilityType.Forge) : 0;
        }

        public bool CanForge(ForgeRecipe recipe, out string reason)
        {
            reason = "";

            if (recipe == null || recipe.result == null) { reason = "Ricetta non valida"; return false; }

            int level = GetForgeLevel();
            if (level < 1) { reason = "Serve una Fucina"; return false; }
            if (level < recipe.requiredForgeLevel) { reason = $"Serve la Fucina al livello {recipe.requiredForgeLevel}"; return false; }
            if (jobs.Count >= level) { reason = "Tutte le fornaci sono occupate"; return false; }
            if (EconomyManager.Instance == null || EconomyManager.Instance.Gold < recipe.goldCost) { reason = "Oro insufficiente"; return false; }
            if (InventoryManager.Instance == null) { reason = "Nessun inventario"; return false; }

            foreach (var mat in recipe.materials)
            {
                if (mat == null || mat.item == null) continue;
                if (!InventoryManager.Instance.HasItem(mat.item, mat.amount))
                {
                    reason = $"Mancano materiali: {mat.item.GetDisplayName()}";
                    return false;
                }
            }

            return true;
        }

        // Checks everything first, then pays: it never takes gold or ore for nothing
        public bool StartForge(ForgeRecipe recipe)
        {
            string reason;
            if (!CanForge(recipe, out reason))
            {
                Log("Cannot forge: " + reason + ".");
                return false;
            }

            EconomyManager.Instance.TrySpendGold(recipe.goldCost);

            foreach (var mat in recipe.materials)
            {
                if (mat == null || mat.item == null) continue;
                InventoryManager.Instance.TryConsumeItem(mat.item, mat.amount);
            }

            jobs.Add(new ForgeJob { recipe = recipe, daysLeft = Mathf.Max(1, recipe.days) });

            Log($"The forge starts working on {recipe.GetName()} ({recipe.days} day(s)).");
            OnForgeChanged?.Invoke();
            return true;
        }

        public void RestoreJobs(List<ForgeJob> restored)
        {
            jobs = restored ?? new List<ForgeJob>();
            OnForgeChanged?.Invoke();
        }

        private void HandleNewDay(int day)
        {
            bool changed = false;

            for (int i = jobs.Count - 1; i >= 0; i--)
            {
                var job = jobs[i];
                if (job == null || job.recipe == null) { jobs.RemoveAt(i); changed = true; continue; }

                job.daysLeft = Mathf.Max(0, job.daysLeft - 1);
                if (job.daysLeft > 0) continue;

                // Done: it goes to the stash. If the stash is full the job waits and retries tomorrow.
                if (InventoryManager.Instance != null && InventoryManager.Instance.AddItem(job.recipe.result, job.recipe.amount))
                {
                    Log($"The forge finished {job.recipe.amount}x {job.recipe.GetName()}!");
                    jobs.RemoveAt(i);
                }
                else
                {
                    Log($"{job.recipe.GetName()} is ready but the stash is full. Free some space!");
                }

                changed = true;
            }

            if (changed) OnForgeChanged?.Invoke();
        }
    }
}

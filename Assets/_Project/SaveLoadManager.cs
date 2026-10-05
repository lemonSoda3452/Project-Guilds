using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HeroManagement
{
    // Saves and loads the whole run as a JSON file (Application.persistentDataPath/savegame.json):
    // day, phase, gold, every hero's state, contracts and the board, stash, buildings, forge jobs,
    // expeditions on the road, gold still on its way and the tutorial progress.
    // ScriptableObjects are saved by asset name and found again among the loaded assets.
    public class SaveLoadManager : MonoBehaviour
    {
        public static SaveLoadManager Instance { get; private set; }

        [Header("Settings")]
        public string fileName = "savegame.json";
        [Tooltip("Saves automatically every time the 'Day' phase starts")]
        public bool autosave = true;

        // ------------------------------------------------------------ save format

        [System.Serializable] public class ItemAmount { public string item; public int amount; }

        [System.Serializable]
        public class HeroSave
        {
            public string id;
            public int levelsGained;
            public HeroStats levelBonusStats;
            public int bonusUpkeepGained;
            public int currentExp;
            public int successCount;
            public int failCount;
            public List<string> skills = new List<string>();
            public string weapon;
            public string armor;
            public int weaponDurability;
            public int armorDurability;
            public int currentContract;
            public int contractDurationLeft;
            public bool hasFreeEquipmentClause;
            public int agreedSharePercent;
            public int loyalty;
            public int injury;
            public int restDaysLeft;
            public int idleDays;
            public int missionRequests;
            public bool hasTemporaryPotential;
            public bool hasPermanentPotential;
            public string permanentEnchantmentDesc;
            public bool isBusy;
            public bool isDead;
        }

        [System.Serializable]
        public class ContractSave
        {
            public string id;
            public bool isAccepted;
            public bool isCompleted;
            public bool isPosted;
            public int postedUntilDay;
        }

        [System.Serializable]
        public class ExpeditionSave
        {
            public string contract;
            public List<string> heroes = new List<string>();
            public int daysRemaining;
            public int phase;
            public int outboundDays;
            public int returnDays;
            public List<ItemAmount> loot = new List<ItemAmount>();
        }

        [System.Serializable] public class PaySave { public string hero; public int amount; }

        [System.Serializable]
        public class PayoutSave
        {
            public int payDay;
            public string region;
            public int grossGold;
            public List<PaySave> heroPays = new List<PaySave>();
        }

        [System.Serializable] public class ForgeJobSave { public int recipeIndex; public int daysLeft; }

        [System.Serializable]
        public class SaveData
        {
            public int version = 1;
            public int day;
            public int phase;
            public int gold;
            public int towerFloor;
            public int reputation;
            public List<HeroSave> heroes = new List<HeroSave>();
            public List<string> guild = new List<string>();
            public List<string> visitors = new List<string>();
            public List<ContractSave> contracts = new List<ContractSave>();
            public List<string> posted = new List<string>();
            public List<ItemAmount> stash = new List<ItemAmount>();
            public int[] facilityLevels;
            public List<ForgeJobSave> forgeJobs = new List<ForgeJobSave>();
            public List<ExpeditionSave> expeditions = new List<ExpeditionSave>();
            public List<PayoutSave> payouts = new List<PayoutSave>();
            public List<string> ravenQueue = new List<string>();
            public List<string> tutorial = new List<string>();
        }

        // ------------------------------------------------------------ setup

        private readonly Dictionary<string, HeroData> heroLookup = new Dictionary<string, HeroData>();
        private readonly Dictionary<string, ContractData> contractLookup = new Dictionary<string, ContractData>();
        private readonly Dictionary<string, ItemData> itemLookup = new Dictionary<string, ItemData>();
        private readonly Dictionary<string, SkillData> skillLookup = new Dictionary<string, SkillData>();

        public string SavePath { get { return Path.Combine(Application.persistentDataPath, fileName); } }
        public bool HasSave() { return File.Exists(SavePath); }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void OnEnable() { GameManager.OnPhaseChanged += HandlePhaseChanged; }
        void OnDisable() { GameManager.OnPhaseChanged -= HandlePhaseChanged; }

        private void HandlePhaseChanged(GameManager.DayPhase phase)
        {
            if (autosave && phase == GameManager.DayPhase.Day) Save(false);
        }

        private void Log(string message)
        {
            if (SystemLogUIManager.Instance != null) SystemLogUIManager.Instance.LogMessage(message);
        }

        // ---- UI buttons
        public void OnSaveClicked() { Save(true); }
        public void OnLoadClicked() { Load(); }

        // ------------------------------------------------------------ save

        private static void AddUnique<T>(List<T> list, T item) where T : Object
        {
            if (item != null && !list.Contains(item)) list.Add(item);
        }

        private List<ContractData> GetContracts()
        {
            if (ContractBoardManager.Instance != null && ContractBoardManager.Instance.contractPool.Count > 0)
                return ContractBoardManager.Instance.contractPool;

            if (GameManager.Instance != null && GameManager.Instance.allContractsDatabase != null)
                return GameManager.Instance.allContractsDatabase;

            return new List<ContractData>();
        }

        private static string NameOf(Object o) { return o != null ? o.name : ""; }

        public bool Save(bool announce)
        {
            var gm = GameManager.Instance;
            if (gm == null) return false;

            var data = new SaveData { day = gm.currentDay, phase = (int)gm.currentPhase, gold = gm.playerGold };

            // ---- heroes: everybody we know about
            var heroes = new List<HeroData>();
            if (gm.allHeroesDatabase != null)
            {
                foreach (var h in gm.allHeroesDatabase) AddUnique(heroes, h);
            }
            if (RecruitmentManager.Instance != null)
            {
                foreach (var h in RecruitmentManager.Instance.availableRecruitsDatabase) AddUnique(heroes, h);
                foreach (var h in RecruitmentManager.Instance.currentTavernVisitors) AddUnique(heroes, h);
            }

            foreach (var h in heroes) data.heroes.Add(HeroToSave(h));

            if (gm.allHeroesDatabase != null)
            {
                foreach (var h in gm.allHeroesDatabase)
                {
                    if (h != null) data.guild.Add(h.name);
                }
            }
            if (RecruitmentManager.Instance != null)
            {
                foreach (var h in RecruitmentManager.Instance.currentTavernVisitors)
                {
                    if (h != null) data.visitors.Add(h.name);
                }
            }

            // ---- contracts and the board
            foreach (var c in GetContracts())
            {
                if (c == null) continue;
                data.contracts.Add(new ContractSave
                {
                    id = c.name, isAccepted = c.isAccepted, isCompleted = c.isCompleted,
                    isPosted = c.isPosted, postedUntilDay = c.postedUntilDay
                });
            }
            if (ContractBoardManager.Instance != null)
            {
                foreach (var c in ContractBoardManager.Instance.postedContracts)
                {
                    if (c != null) data.posted.Add(c.name);
                }
            }

            // ---- stash, buildings, forge
            if (InventoryManager.Instance != null)
            {
                foreach (var slot in InventoryManager.Instance.stash)
                {
                    if (slot != null && slot.item != null) data.stash.Add(new ItemAmount { item = slot.item.name, amount = slot.amount });
                }
            }

            if (TavernFacilitiesManager.Instance != null)
                data.facilityLevels = (int[])TavernFacilitiesManager.Instance.currentLevels.Clone();

            if (ForgeManager.Instance != null)
            {
                foreach (var job in ForgeManager.Instance.jobs)
                {
                    if (job == null || job.recipe == null) continue;
                    int index = ForgeManager.Instance.recipes.IndexOf(job.recipe);
                    if (index >= 0) data.forgeJobs.Add(new ForgeJobSave { recipeIndex = index, daysLeft = job.daysLeft });
                }
            }

            // ---- expeditions, gold on its way, raven messages
            var dispatcher = MissionDispatcher.Instance;
            if (dispatcher != null)
            {
                foreach (var e in dispatcher.ongoingExpeditions)
                {
                    if (e == null || e.contract == null) continue;

                    var es = new ExpeditionSave
                    {
                        contract = e.contract.name, daysRemaining = e.daysRemaining, phase = (int)e.phase,
                        outboundDays = e.outboundDays, returnDays = e.returnDays
                    };
                    foreach (var h in e.heroes) { if (h != null) es.heroes.Add(h.name); }
                    if (e.loot != null)
                    {
                        foreach (var l in e.loot) { if (l != null && l.item != null) es.loot.Add(new ItemAmount { item = l.item.name, amount = l.amount }); }
                    }
                    data.expeditions.Add(es);
                }

                foreach (var p in dispatcher.pendingPayouts)
                {
                    if (p == null) continue;

                    var ps = new PayoutSave { payDay = p.payDay, region = p.region, grossGold = p.grossGold };
                    foreach (var hp in p.heroPays) { if (hp != null && hp.hero != null) ps.heroPays.Add(new PaySave { hero = hp.hero.name, amount = hp.amount }); }
                    data.payouts.Add(ps);
                }

                data.ravenQueue = dispatcher.GetRavenQueueSnapshot();
            }

            data.towerFloor = TowerManager.HighestFloor;
            data.reputation = ReputationManager.Instance != null ? ReputationManager.Instance.reputation : 0;
            if (TutorialManager.Instance != null) data.tutorial = TutorialManager.Instance.GetFlags();

            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[SAVE] Could not write the save file: " + ex.Message);
                if (announce) Log("Could not save the game.");
                return false;
            }

            if (announce) Log("Game saved.");
            Debug.Log("[SAVE] Saved to " + SavePath);
            return true;
        }

        private HeroSave HeroToSave(HeroData h)
        {
            h.NormalizeSkillSlots();

            var s = new HeroSave
            {
                id = h.name, levelsGained = h.levelsGained, levelBonusStats = h.levelBonusStats,
                bonusUpkeepGained = h.bonusUpkeepGained, currentExp = h.currentExp,
                successCount = h.successCount, failCount = h.failCount,
                weapon = NameOf(h.equippedWeapon), armor = NameOf(h.equippedArmor),
                weaponDurability = h.weaponDurability, armorDurability = h.armorDurability,
                currentContract = (int)h.currentContract, contractDurationLeft = h.contractDurationLeft,
                hasFreeEquipmentClause = h.hasFreeEquipmentClause, agreedSharePercent = h.agreedSharePercent,
                loyalty = h.loyalty, injury = h.injury, restDaysLeft = h.restDaysLeft,
                idleDays = h.idleDays, missionRequests = h.missionRequests,
                hasTemporaryPotential = h.hasTemporaryPotential, hasPermanentPotential = h.hasPermanentPotential,
                permanentEnchantmentDesc = h.permanentEnchantmentDesc, isBusy = h.isBusy, isDead = h.isDead
            };

            for (int i = 0; i < HeroData.MaxSkillSlots; i++) s.skills.Add(NameOf(h.equippedSkills[i]));
            return s;
        }

        // ------------------------------------------------------------ load

        private void BuildRegistry()
        {
            heroLookup.Clear(); contractLookup.Clear(); itemLookup.Clear(); skillLookup.Clear();

            foreach (var o in Resources.FindObjectsOfTypeAll<HeroData>()) { if (o != null && !heroLookup.ContainsKey(o.name)) heroLookup[o.name] = o; }
            foreach (var o in Resources.FindObjectsOfTypeAll<ContractData>()) { if (o != null && !contractLookup.ContainsKey(o.name)) contractLookup[o.name] = o; }
            foreach (var o in Resources.FindObjectsOfTypeAll<ItemData>()) { if (o != null && !itemLookup.ContainsKey(o.name)) itemLookup[o.name] = o; }
            foreach (var o in Resources.FindObjectsOfTypeAll<SkillData>()) { if (o != null && !skillLookup.ContainsKey(o.name)) skillLookup[o.name] = o; }
        }

        private static T Find<T>(Dictionary<string, T> lookup, string id) where T : class
        {
            if (string.IsNullOrEmpty(id)) return null;
            T value;
            return lookup.TryGetValue(id, out value) ? value : null;
        }

        public bool Load()
        {
            if (!HasSave()) { Log("No saved game found."); return false; }
            if (GameManager.Instance == null) return false;

            SaveData data;
            try { data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath)); }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[SAVE] Could not read the save file: " + ex.Message);
                Log("The save file could not be read.");
                return false;
            }

            if (data == null || data.version != 1) { Log("The save file is not valid."); return false; }

            BuildRegistry();

            // 1. Everything goes back to its starting state, then the saved changes are applied on top.
            //    (This also keeps the Editor's ScriptableObjects restorable at the next Play.)
            foreach (var h in heroLookup.Values) h.ResetRuntimeState();
            foreach (var c in contractLookup.Values) c.ResetRuntimeState();

            foreach (var hs in data.heroes)
            {
                var hero = Find(heroLookup, hs.id);
                if (hero != null) ApplyHeroSave(hero, hs);
            }

            foreach (var cs in data.contracts)
            {
                var contract = Find(contractLookup, cs.id);
                if (contract == null) continue;
                contract.isAccepted = cs.isAccepted;
                contract.isCompleted = cs.isCompleted;
                contract.isPosted = cs.isPosted;
                contract.postedUntilDay = cs.postedUntilDay;
            }

            // 2. Lists
            var guild = new List<HeroData>();
            foreach (var id in data.guild) { var h = Find(heroLookup, id); if (h != null) guild.Add(h); }

            var visitors = new List<HeroData>();
            foreach (var id in data.visitors) { var h = Find(heroLookup, id); if (h != null) visitors.Add(h); }

            var posted = new List<ContractData>();
            foreach (var id in data.posted) { var c = Find(contractLookup, id); if (c != null) posted.Add(c); }

            var stash = new List<InventoryManager.InventorySlot>();
            foreach (var ia in data.stash)
            {
                var item = Find(itemLookup, ia.item);
                if (item != null && ia.amount > 0) stash.Add(new InventoryManager.InventorySlot { item = item, amount = ia.amount });
            }

            var jobs = new List<ForgeManager.ForgeJob>();
            if (ForgeManager.Instance != null)
            {
                foreach (var js in data.forgeJobs)
                {
                    if (js.recipeIndex >= 0 && js.recipeIndex < ForgeManager.Instance.recipes.Count)
                        jobs.Add(new ForgeManager.ForgeJob { recipe = ForgeManager.Instance.recipes[js.recipeIndex], daysLeft = js.daysLeft });
                }
            }

            var expeditions = new List<MissionDispatcher.ActiveExpedition>();
            foreach (var es in data.expeditions)
            {
                var contract = Find(contractLookup, es.contract);
                if (contract == null) continue;

                var exp = new MissionDispatcher.ActiveExpedition
                {
                    contract = contract, heroes = new List<HeroData>(), daysRemaining = es.daysRemaining,
                    phase = (ExpeditionPhase)es.phase, outboundDays = es.outboundDays, returnDays = es.returnDays
                };
                foreach (var id in es.heroes) { var h = Find(heroLookup, id); if (h != null) exp.heroes.Add(h); }
                foreach (var ia in es.loot)
                {
                    var item = Find(itemLookup, ia.item);
                    if (item != null) exp.loot.Add(new LootEntry { item = item, amount = ia.amount });
                }
                expeditions.Add(exp);
            }

            var payouts = new List<MissionDispatcher.PendingPayout>();
            foreach (var ps in data.payouts)
            {
                var payout = new MissionDispatcher.PendingPayout { payDay = ps.payDay, region = ps.region, grossGold = ps.grossGold };
                foreach (var pay in ps.heroPays)
                {
                    var h = Find(heroLookup, pay.hero);
                    if (h != null) payout.heroPays.Add(new HeroPay { hero = h, amount = pay.amount });
                }
                payouts.Add(payout);
            }

            // 3. Hand everything to the managers (each one tells its UI to redraw)
            if (GameFlowManager.Instance != null) GameFlowManager.Instance.ResetForLoad();
            if (EndScreenUIManager.Instance != null) EndScreenUIManager.Instance.Hide();
            if (TutorialManager.Instance != null) TutorialManager.Instance.RestoreFlags(data.tutorial);

            if (TowerManager.Instance != null) TowerManager.Instance.RestoreProgress(data.towerFloor);
            if (ReputationManager.Instance != null) ReputationManager.Instance.Restore(data.reputation);
            GameManager.Instance.RestoreState(data.day, (GameManager.DayPhase)data.phase, data.gold, guild);
            if (TavernFacilitiesManager.Instance != null) TavernFacilitiesManager.Instance.RestoreLevels(data.facilityLevels);
            if (InventoryManager.Instance != null) InventoryManager.Instance.RestoreStash(stash);
            if (ForgeManager.Instance != null) ForgeManager.Instance.RestoreJobs(jobs);
            if (RecruitmentManager.Instance != null) RecruitmentManager.Instance.RestoreVisitors(visitors);
            if (ContractBoardManager.Instance != null) ContractBoardManager.Instance.RestorePosted(posted);
            if (MissionDispatcher.Instance != null) MissionDispatcher.Instance.RestoreState(expeditions, payouts, data.ravenQueue);

            Log($"Game loaded (day {data.day}).");
            return true;
        }

        private void ApplyHeroSave(HeroData h, HeroSave s)
        {
            // ResetRuntimeState() was already called: we are at the designer's starting values
            h.level += s.levelsGained;
            h.levelsGained = s.levelsGained;
            h.levelBonusStats = s.levelBonusStats;
            h.bonusUpkeepGained = s.bonusUpkeepGained;
            h.dailyUpkeepCost += s.bonusUpkeepGained;
            h.currentExp = s.currentExp;
            h.successCount = s.successCount;
            h.failCount = s.failCount;

            h.NormalizeSkillSlots();
            for (int i = 0; i < HeroData.MaxSkillSlots; i++)
            {
                var skill = Find(skillLookup, i < s.skills.Count ? s.skills[i] : "");
                if (skill != null && h.equippedSkills[i] != skill) h.LearnSkill(skill, i);
            }

            var weapon = Find(itemLookup, s.weapon);
            if (weapon != h.equippedWeapon) h.SetEquipment(weapon, true);
            var armor = Find(itemLookup, s.armor);
            if (armor != h.equippedArmor) h.SetEquipment(armor, false);
            h.weaponDurability = s.weaponDurability;
            h.armorDurability = s.armorDurability;

            h.currentContract = (EmploymentType)s.currentContract;
            h.contractDurationLeft = s.contractDurationLeft;
            h.hasFreeEquipmentClause = s.hasFreeEquipmentClause;
            h.agreedSharePercent = s.agreedSharePercent;
            h.loyalty = s.loyalty;
            h.injury = s.injury;
            h.restDaysLeft = s.restDaysLeft;
            h.idleDays = s.idleDays;
            h.missionRequests = s.missionRequests;
            h.hasTemporaryPotential = s.hasTemporaryPotential;
            h.hasPermanentPotential = s.hasPermanentPotential;
            h.permanentEnchantmentDesc = s.permanentEnchantmentDesc;
            h.isBusy = s.isBusy;
            h.isDead = s.isDead;
        }
    }
}

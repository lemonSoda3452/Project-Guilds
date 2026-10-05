using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// IMPORTANT: this file must live in a folder called "Editor" (e.g. Assets/Editor/DemoSceneBuilder.cs)
namespace HeroManagement.EditorTools
{
    public static class DemoSceneBuilder
    {
        private const string DataFolder = "Assets/_Project/Data/Demo";

        [MenuItem("Hero Management/Build Demo Scene")]
        public static void Build()
        {
            if (GameObject.Find("Managers") != null || GameObject.Find("Canvas") != null)
            {
                EditorUtility.DisplayDialog("Hero Management",
                    "This scene already has a 'Managers' or 'Canvas' object.\nOpen an empty scene (File > New Scene) and try again.", "OK");
                return;
            }

            EnsureFolder(DataFolder);

            // ================= DEMO CONTENT (ScriptableObjects) =================
            var fireball = MakeSkill("Skill_Fireball", "Fireball", "A blazing sphere of flame.", SkillType.Magical, 30, 10, "CastFireball", 120);
            var powerStrike = MakeSkill("Skill_PowerStrike", "Power Strike", "A crushing blow.", SkillType.Physical, 20, 0, "PowerStrike", 60);
            var heal = MakeSkill("Skill_Heal", "Healing Light", "Mends wounds.", SkillType.Magical, 25, 8, "CastHeal", 90);
            var shadowStep = MakeSkill("Skill_ShadowStep", "Shadow Step", "Vanish and reappear.", SkillType.Utility, 0, 5, "ShadowStep", 80);

            var ironOre = MakeItem("Item_IronOre", "Iron Ore", "Raw ore, used in crafting.", ItemType.Material, ItemRarity.Common, 10, 99, null, 2f, 0);
            var potion = MakeItem("Item_HealingPotion", "Healing Potion", "Restores health.", ItemType.Consumable, ItemRarity.Common, 25, 10, null, 0.5f, 0);
            if (potion.healInjury == 0) { potion.healInjury = 30; EditorUtility.SetDirty(potion); }
            var gem = MakeItem("Item_Gemstone", "Gemstone", "Sells well at the market.", ItemType.Valuables, ItemRarity.Uncommon, 80, 20, null, 0.3f, 0);
            var scrollFireball = MakeItem("Item_Scroll_Fireball", "Scroll of Fireball", "Teaches Fireball.", ItemType.Scroll, ItemRarity.Rare, 100, 5, fireball, 0.2f, 0);
            var ironSword = MakeItem("Item_IronSword", "Iron Sword", "A sturdy blade.", ItemType.Weapon, ItemRarity.Common, 60, 1, null, 6f, 4);
            var leatherArmor = MakeItem("Item_LeatherArmor", "Leather Armor", "Light and cheap.", ItemType.Armor, ItemRarity.Common, 50, 1, null, 8f, 3);
            var steelArmor = MakeItem("Item_SteelArmor", "Steel Armor", "Heavy, but it saves lives.", ItemType.Armor, ItemRarity.Uncommon, 150, 1, null, 18f, 8);
            var steelSword = MakeItem("Item_SteelSword", "Steel Sword", "Finely forged steel.", ItemType.Weapon, ItemRarity.Uncommon, 140, 1, null, 8f, 7);

            //                                   stats: STR INT SPD VIT HEAL CHA
            var grog = MakeHero("Hero_Grog", "Grog", HeroClass.Warrior, HeroRace.Orc, PersonalityType.Greedy, 30, 2, new HeroStats(14, 4, 6, 12, 2, 4), 9, FameTier.LocalHero, "A brawler who fights for coin.", MountType.None, ironSword, leatherArmor, powerStrike);
            var lyra = MakeHero("Hero_Lyra", "Lyra", HeroClass.Mage, HeroRace.Elf, PersonalityType.Cautious, 28, 1, new HeroStats(4, 15, 9, 6, 6, 7), 7, FameTier.Unknown, "A scholar of the arcane.", MountType.None, null, null, fireball);
            var bram = MakeHero("Hero_Bram", "Bram", HeroClass.Cleric, HeroRace.Dwarf, PersonalityType.Altruistic, 45, 2, new HeroStats(9, 8, 4, 12, 13, 6), 6, FameTier.Veteran, "Heals friend and foe alike.", MountType.None, null, leatherArmor, heal);
            var shawn = MakeHero("Hero_Shawn", "Shawn", HeroClass.Ranger, HeroRace.Human, PersonalityType.GlorySeeker, 22, 1, new HeroStats(8, 7, 12, 8, 3, 9), 5, FameTier.Unknown, "An adventurer in search of fortune.", MountType.Horse, ironSword, null);
            var kira = MakeHero("Hero_Kira", "Kira", HeroClass.Rogue, HeroRace.Beastkin, PersonalityType.Neutral, 19, 1, new HeroStats(7, 6, 15, 7, 2, 10), 6, FameTier.Unknown, "Quick, quiet and curious.", MountType.None, null, null, shadowStep);

            var c1 = MakeContract("Contract_Farmer", "Desperate Farmer", "Creatures are infesting my land...", "Millston",
                Region.Millston, TravelDifficulty.Easy, 2, 300, 2, RequiredSkill.Combat, false, HeroClass.Warrior, 25, MissionType.Combat,
                new[] { new ContractData.ItemReward { item = ironOre, amount = 2, chancePercent = 100 } },
                c => { c.recommendedPartyStats = new HeroStats(14, 0, 0, 10, 0, 0); });

            var c2 = MakeContract("Contract_Caravan", "Merchant Guild", "Escort our caravan to Ironcrest.", "Ironcrest",
                Region.Ironcrest, TravelDifficulty.Moderate, 3, 600, 3, RequiredSkill.Tracking, true, HeroClass.Ranger, 35, MissionType.Escort,
                new[] { new ContractData.ItemReward { item = gem, amount = 1, chancePercent = 50 } },
                c => { c.clientType = ClientType.Merchant; c.recommendedPartyStats = new HeroStats(0, 0, 20, 0, 0, 12); });

            var c3 = MakeContract("Contract_NorthRuins", "Order of the Silver Hand", "Investigate the ruins in the Northern Wastes. Only a mage can read the runes.", "Northern Wastes",
                Region.NorthernWastes, TravelDifficulty.Hard, 5, 1400, 3, RequiredSkill.Mind, true, HeroClass.Mage, 45, MissionType.Investigation,
                new[] { new ContractData.ItemReward { item = scrollFireball, amount = 1, chancePercent = 30 } },
                c => { c.source = ContractSource.Raven; c.clientType = ClientType.Order; c.postedDurationDays = 3; c.requiresClass = true; c.requiredClass = HeroClass.Mage; c.recommendedPartyStats = new HeroStats(0, 30, 0, 10, 10, 0); });

            var c4 = MakeContract("Contract_Relic", "King of Aethelgard", "Recover the relic from the cursed vault. Only a dwarf can open the door.", "Aethelgard",
                Region.Aethelgard, TravelDifficulty.Suicidal, 7, 4000, 3, RequiredSkill.Combat, false, HeroClass.Warrior, 70, MissionType.Combat,
                new ContractData.ItemReward[0],
                c => { c.source = ContractSource.Raven; c.clientType = ClientType.Kingdom; c.postedDurationDays = 4; c.availableFromDay = 5; c.requiresRace = true; c.requiredRace = HeroRace.Dwarf; c.requiredMinLevel = 2; c.recommendedPartyStats = new HeroStats(30, 0, 0, 25, 0, 0); });

            var c5 = MakeContract("Contract_Spire", "The Spire Expedition", "Climb the first 20 floors of the Spire. Bring good gear: it will take a beating.", "Millston",
                Region.Millston, TravelDifficulty.Moderate, 4, 900, 3, RequiredSkill.Combat, false, HeroClass.Warrior, 40, MissionType.Combat,
                new[] { new ContractData.ItemReward { item = ironOre, amount = 4, chancePercent = 100 }, new ContractData.ItemReward { item = scrollFireball, amount = 1, chancePercent = 20 } },
                c => { c.source = ContractSource.Raven; c.clientType = ClientType.Noble; c.postedDurationDays = 3; c.availableFromDay = 2; c.isTowerRaid = true; c.towerFloors = 20; c.minGearRating = 6; c.recommendedPartyStats = new HeroStats(24, 0, 10, 18, 6, 0); });

            var c6 = MakeContract("Contract_Goat", "Old Marta", "My goat ran off into the hills. Please find her!", "Millston",
                Region.Millston, TravelDifficulty.Easy, 1, 120, 1, RequiredSkill.Tracking, false, HeroClass.Ranger, 8, MissionType.Gathering,
                new ContractData.ItemReward[0],
                c => { c.recommendedPartyStats = new HeroStats(0, 0, 8, 0, 0, 0); });

            var c7 = MakeContract("Contract_Rats", "Tavern Keeper Olaf", "Giant rats took over my cellar!", "Millston",
                Region.Millston, TravelDifficulty.Easy, 1, 160, 2, RequiredSkill.Combat, false, HeroClass.Warrior, 10, MissionType.Combat,
                new[] { new ContractData.ItemReward { item = potion, amount = 1, chancePercent = 50 } },
                c => { c.recommendedPartyStats = new HeroStats(8, 0, 0, 6, 0, 0); });

            var c8 = MakeContract("Contract_Herbs", "Healer Sybil", "I need rare herbs from the forest edge.", "Millston",
                Region.Millston, TravelDifficulty.Easy, 2, 220, 2, RequiredSkill.Tracking, false, HeroClass.Ranger, 15, MissionType.Gathering,
                new[] { new ContractData.ItemReward { item = potion, amount = 2, chancePercent = 100 } },
                c => { c.recommendedPartyStats = new HeroStats(0, 6, 8, 0, 6, 0); });

            var c9 = MakeContract("Contract_SilverMine", "Ironcrest Mining Company", "Escort our silver shipment through the pass. We need a proper fighter.", "Ironcrest",
                Region.Ironcrest, TravelDifficulty.Moderate, 4, 1100, 3, RequiredSkill.Combat, true, HeroClass.Warrior, 38, MissionType.Escort,
                new[] { new ContractData.ItemReward { item = gem, amount = 1, chancePercent = 60 } },
                c => { c.source = ContractSource.Raven; c.clientType = ClientType.Merchant; c.postedDurationDays = 3; c.requiresClass = true; c.requiredClass = HeroClass.Warrior; c.recommendedPartyStats = new HeroStats(22, 0, 10, 16, 0, 6); });

            var c10 = MakeContract("Contract_Border", "Marshal of Ironcrest", "Orc raiders cross the border. We need someone who knows their tactics: an Orc.", "Ironcrest",
                Region.Ironcrest, TravelDifficulty.Hard, 3, 1600, 3, RequiredSkill.Combat, true, HeroClass.Warrior, 50, MissionType.Combat,
                new[] { new ContractData.ItemReward { item = ironOre, amount = 3, chancePercent = 100 } },
                c => { c.source = ContractSource.Raven; c.clientType = ClientType.Kingdom; c.postedDurationDays = 3; c.availableFromDay = 3; c.requiresRace = true; c.requiredRace = HeroRace.Orc; c.recommendedPartyStats = new HeroStats(28, 0, 8, 22, 0, 0); });

            // The Spire: ranges of 20 floors, each unlocked by clearing the previous one (see TowerManager)
            var c11 = MakeContract("Contract_Spire_21_40", "The Spire: Floors 21-40", "The lower halls are clear. Deeper, the monsters get serious.", "Millston",
                Region.Millston, TravelDifficulty.Hard, 5, 1800, 3, RequiredSkill.Combat, false, HeroClass.Warrior, 60, MissionType.Combat,
                new[] { new ContractData.ItemReward { item = gem, amount = 2, chancePercent = 100 }, new ContractData.ItemReward { item = scrollFireball, amount = 1, chancePercent = 25 } },
                c => { c.source = ContractSource.Raven; c.clientType = ClientType.Noble; c.postedDurationDays = 5; c.spawnWeight = 2;
                       c.isTowerRaid = true; c.towerStartFloor = 21; c.towerFloors = 20; c.minGearRating = 9; c.requiredMinLevel = 2;
                       c.recommendedPartyStats = new HeroStats(30, 0, 12, 22, 8, 0); });

            var c12 = MakeContract("Contract_Spire_41_60", "The Spire: Floors 41-60", "Few come back from the middle floors. Bring healers and good armor.", "Millston",
                Region.Millston, TravelDifficulty.Hard, 6, 3000, 3, RequiredSkill.Combat, false, HeroClass.Warrior, 85, MissionType.Combat,
                new[] { new ContractData.ItemReward { item = ironOre, amount = 6, chancePercent = 100 }, new ContractData.ItemReward { item = steelArmor, amount = 1, chancePercent = 30 } },
                c => { c.source = ContractSource.Raven; c.clientType = ClientType.Noble; c.postedDurationDays = 5; c.spawnWeight = 2;
                       c.isTowerRaid = true; c.towerStartFloor = 41; c.towerFloors = 20; c.minGearRating = 13; c.requiredMinLevel = 3;
                       c.recommendedPartyStats = new HeroStats(36, 0, 14, 28, 10, 0); });

            var c13 = MakeContract("Contract_Spire_61_80", "The Spire: Floors 61-80", "The walls whisper. Only veterans have made it this far.", "Millston",
                Region.Millston, TravelDifficulty.Hard, 7, 4800, 3, RequiredSkill.Combat, false, HeroClass.Warrior, 115, MissionType.Combat,
                new[] { new ContractData.ItemReward { item = gem, amount = 3, chancePercent = 100 }, new ContractData.ItemReward { item = steelSword, amount = 1, chancePercent = 35 } },
                c => { c.source = ContractSource.Raven; c.clientType = ClientType.Kingdom; c.postedDurationDays = 6; c.spawnWeight = 2;
                       c.isTowerRaid = true; c.towerStartFloor = 61; c.towerFloors = 20; c.minGearRating = 17; c.requiredMinLevel = 4;
                       c.recommendedPartyStats = new HeroStats(44, 0, 16, 34, 12, 0); });

            var c14 = MakeContract("Contract_Spire_81_100", "The Spire: The Summit", "The last twenty floors. At the top, the Spire's master waits.", "Millston",
                Region.Millston, TravelDifficulty.Suicidal, 8, 8000, 3, RequiredSkill.Combat, false, HeroClass.Warrior, 150, MissionType.Combat,
                new[] { new ContractData.ItemReward { item = gem, amount = 5, chancePercent = 100 }, new ContractData.ItemReward { item = scrollFireball, amount = 1, chancePercent = 60 } },
                c => { c.source = ContractSource.Raven; c.clientType = ClientType.Kingdom; c.postedDurationDays = 7; c.spawnWeight = 3;
                       c.isTowerRaid = true; c.towerStartFloor = 81; c.towerFloors = 20; c.minGearRating = 22; c.requiredMinLevel = 5;
                       c.recommendedPartyStats = new HeroStats(52, 10, 18, 40, 16, 0); });

            AssetDatabase.SaveAssets();

            var heroes = new List<HeroData> { grog, lyra, bram, shawn, kira };
            var contracts = new List<ContractData> { c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14 };

            // ================= MANAGERS =================
            var managers = new GameObject("Managers");

            var gm = managers.AddComponent<GameManager>();
            gm.allHeroesDatabase = new List<HeroData>();
            gm.allContractsDatabase = new List<ContractData>(contracts);
            gm.startingGold = 100000; // DEMO: lots of gold to test everything (the real game starts poor)
            var boardManager = managers.AddComponent<ContractBoardManager>();
            boardManager.contractPool = new List<ContractData>(contracts);

            managers.AddComponent<TavernFacilitiesManager>();
            managers.AddComponent<SaveLoadManager>();
            managers.AddComponent<TutorialManager>();
            managers.AddComponent<TowerManager>();
            managers.AddComponent<TavernEventManager>();
            managers.AddComponent<ReputationManager>();

            var forge = managers.AddComponent<ForgeManager>();
            var rIronSword = new ForgeManager.ForgeRecipe { result = ironSword, amount = 1, goldCost = 40, days = 1, requiredForgeLevel = 1 };
            rIronSword.materials.Add(new WitchFacilityManager.MaterialCost { item = ironOre, amount = 3 });
            var rSteelSword = new ForgeManager.ForgeRecipe { result = steelSword, amount = 1, goldCost = 120, days = 2, requiredForgeLevel = 2 };
            rSteelSword.materials.Add(new WitchFacilityManager.MaterialCost { item = ironOre, amount = 6 });
            rSteelSword.materials.Add(new WitchFacilityManager.MaterialCost { item = gem, amount = 1 });
            var rSteelArmor = new ForgeManager.ForgeRecipe { result = steelArmor, amount = 1, goldCost = 150, days = 2, requiredForgeLevel = 2 };
            rSteelArmor.materials.Add(new WitchFacilityManager.MaterialCost { item = ironOre, amount = 8 });
            forge.recipes.Add(rIronSword);
            forge.recipes.Add(rSteelSword);
            forge.recipes.Add(rSteelArmor);

            managers.AddComponent<EconomyManager>();
            managers.AddComponent<EmploymentManager>();
            managers.AddComponent<GameFlowManager>();
            managers.AddComponent<MissionDispatcher>();
            managers.AddComponent<HeroManager>();
            managers.AddComponent<HeroProgressionManager>();
            managers.AddComponent<SkillReplacementUIManager>();
            managers.AddComponent<QuestMapManager>();
            managers.AddComponent<DebugInputManager>();

            var recruitment = managers.AddComponent<RecruitmentManager>();
            recruitment.availableRecruitsDatabase = new List<HeroData>(heroes);
            recruitment.availableRecruitsDatabase.AddRange(HeroPoolGenerator.GetOrGenerate(25)); // 25 more mercenaries, so the tavern never runs dry

            var inventory = managers.AddComponent<InventoryManager>();
            inventory.stash.Add(new InventoryManager.InventorySlot { item = ironOre, amount = 20 });
            inventory.stash.Add(new InventoryManager.InventorySlot { item = potion, amount = 5 });
            inventory.stash.Add(new InventoryManager.InventorySlot { item = gem, amount = 3 });
            inventory.stash.Add(new InventoryManager.InventorySlot { item = steelArmor, amount = 1 });
            inventory.stash.Add(new InventoryManager.InventorySlot { item = ironSword, amount = 1 });

            var witch = managers.AddComponent<WitchFacilityManager>();
            var recipe = new WitchFacilityManager.ScrollRecipe { skill = fireball, scrollItem = scrollFireball, goldCost = 100 };
            recipe.materials.Add(new WitchFacilityManager.MaterialCost { item = ironOre, amount = 2 });
            witch.recipes.Add(recipe);

            var log = managers.AddComponent<SystemLogUIManager>();
            log.maxLogLines = 6;

            // ================= UI =================
            if (GameObject.Find("EventSystem") == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }

            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            Transform root = canvasGO.transform;

            // ---- HUD
            var dayText = Label("DayText", root, V(0.01f, 0.93f), V(0.12f, 0.99f), "DAY", 28, TextAnchor.MiddleLeft);
            var phaseText = Label("PhaseText", root, V(0.12f, 0.93f), V(0.3f, 0.99f), "PHASE", 28, TextAnchor.MiddleLeft);
            var goldText = Label("GoldText", root, V(0.3f, 0.93f), V(0.48f, 0.99f), "GOLD", 28, TextAnchor.MiddleLeft);
            var advanceBtn = MakeButton("AdvanceButton", root, V(0.8f, 0.93f), V(0.98f, 0.99f), "Next Phase");
            var menuHeroes = MakeButton("MenuHeroes", root, V(0.49f, 0.935f), V(0.56f, 0.99f), "Heroes");
            var menuInventory = MakeButton("MenuInventory", root, V(0.565f, 0.935f), V(0.635f, 0.99f), "Inventory");
            var menuWitch = MakeButton("MenuWitch", root, V(0.64f, 0.935f), V(0.71f, 0.99f), "Witch");
            var menuTavern = MakeButton("MenuTavern", root, V(0.715f, 0.935f), V(0.785f, 0.99f), "Tavern");

            var gf = managers.GetComponent<GameFlowManager>();
            gf.targetGold = 103000; // starting gold + 3000
            var hint = Label("HintText", root, V(0.01f, 0.885f), V(0.6f, 0.93f),
                gf.GetObjectiveText(), 16, TextAnchor.MiddleLeft);
            hint.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            var saveBtn = MakeButton("SaveButton", root, V(0.62f, 0.885f), V(0.69f, 0.93f), "Save");
            var loadBtn = MakeButton("LoadButton", root, V(0.7f, 0.885f), V(0.77f, 0.93f), "Load");

            // ---- Tavern roster
            var tavernPanel = Panel("TavernPanel", root, V(0.01f, 0.64f), V(0.25f, 0.88f), DarkPanel());
            var rosterText = Label("RosterText", tavernPanel.transform, V(0.04f, 0.04f), V(0.96f, 0.96f), "Tavern", 22, TextAnchor.UpperLeft);

            // ---- Recruitment
            var recPanel = Panel("RecruitmentPanel", root, V(0.01f, 0.2f), V(0.25f, 0.63f), DarkPanel());
            var visitorText = Label("VisitorText", recPanel.transform, V(0.04f, 0.62f), V(0.96f, 0.97f), "", 16, TextAnchor.UpperLeft);

            RadarChart recChart, recChartUnused;
            BuildChartArea(recPanel.transform, "RecruitChart", V(0.24f, 0.4f), new Color(0.3f, 0.6f, 1f, 0.65f), Color.clear, false, out recChart, out recChartUnused, 150f);

            var costText = Label("CostText", recPanel.transform, V(0.5f, 0.2f), V(0.97f, 0.6f), "", 17, TextAnchor.UpperLeft);
            var recNext = MakeButton("NextVisitorButton", recPanel.transform, V(0.02f, 0.02f), V(0.21f, 0.17f), "Next");
            var recContract = MakeButton("TermButton", recPanel.transform, V(0.22f, 0.02f), V(0.41f, 0.17f), "Term");
            var recShare = MakeButton("CutButton", recPanel.transform, V(0.42f, 0.02f), V(0.61f, 0.17f), "Cut");
            var recGear = MakeButton("GearButton", recPanel.transform, V(0.62f, 0.02f), V(0.81f, 0.17f), "Gear");
            var recHire = MakeButton("HireButton", recPanel.transform, V(0.82f, 0.02f), V(0.98f, 0.17f), "Hire");

            // ---- Board (contract, requirements, radar charts, party)
            var boardPanel = Panel("BoardPanel", root, V(0.26f, 0.2f), V(0.74f, 0.88f), DarkPanel());
            var clientText = Label("ClientText", boardPanel.transform, V(0.03f, 0.92f), V(0.7f, 0.99f), "", 22, TextAnchor.MiddleLeft);
            var bPrevC = MakeButton("PrevContractButton", boardPanel.transform, V(0.72f, 0.92f), V(0.84f, 0.99f), "<");
            var bNextC = MakeButton("NextContractButton", boardPanel.transform, V(0.86f, 0.92f), V(0.97f, 0.99f), ">");
            var descText = Label("DescriptionText", boardPanel.transform, V(0.03f, 0.82f), V(0.97f, 0.92f), "", 17, TextAnchor.UpperLeft);
            var rewardText = Label("RewardText", boardPanel.transform, V(0.03f, 0.76f), V(0.97f, 0.82f), "", 17, TextAnchor.MiddleLeft);
            var reqText = Label("RequirementsText", boardPanel.transform, V(0.03f, 0.655f), V(0.5f, 0.76f), "", 16, TextAnchor.UpperLeft);
            var travelText = Label("TravelText", boardPanel.transform, V(0.52f, 0.655f), V(0.97f, 0.76f), "", 16, TextAnchor.UpperLeft);
            Label("HeroChartTitle", boardPanel.transform, V(0.03f, 0.6f), V(0.5f, 0.655f), "Candidate hero", 18, TextAnchor.MiddleCenter);
            Label("PartyChartTitle", boardPanel.transform, V(0.52f, 0.6f), V(0.97f, 0.655f), "Party (green) vs recommended (yellow)", 18, TextAnchor.MiddleCenter);

            RadarChart heroChart, heroChartUnused, partyChart, reqChart;
            BuildChartArea(boardPanel.transform, "HeroChart", V(0.26f, 0.4f), new Color(0.3f, 0.6f, 1f, 0.65f), Color.clear, false, out heroChart, out heroChartUnused);
            BuildChartArea(boardPanel.transform, "PartyChart", V(0.74f, 0.4f), new Color(0.3f, 0.85f, 0.4f, 0.55f), new Color(1f, 0.85f, 0.2f, 1f), true, out partyChart, out reqChart);

            var selectedHeroText = Label("SelectedHeroText", boardPanel.transform, V(0.03f, 0.1f), V(0.5f, 0.195f), "", 16, TextAnchor.UpperLeft);
            var partyText = Label("PartyText", boardPanel.transform, V(0.52f, 0.1f), V(0.97f, 0.195f), "", 18, TextAnchor.UpperLeft);
            var bNext = MakeButton("NextHeroButton", boardPanel.transform, V(0.02f, 0.01f), V(0.25f, 0.085f), "Next Hero");
            var bAdd = MakeButton("AddHeroButton", boardPanel.transform, V(0.26f, 0.01f), V(0.49f, 0.085f), "Add");
            var bClear = MakeButton("ClearPartyButton", boardPanel.transform, V(0.51f, 0.01f), V(0.74f, 0.085f), "Clear");
            var bAccept = MakeButton("AcceptButton", boardPanel.transform, V(0.75f, 0.01f), V(0.98f, 0.085f), "Send Party");

            // ---- Expeditions
            var expPanel = Panel("ExpeditionsPanel", root, V(0.75f, 0.45f), V(0.99f, 0.88f), DarkPanel());
            var expText = Label("ExpeditionsText", expPanel.transform, V(0.04f, 0.04f), V(0.96f, 0.96f), "", 22, TextAnchor.UpperLeft);

            // ---- Log
            var logPanel = Panel("LogPanel", root, V(0.01f, 0.01f), V(0.99f, 0.19f), DarkPanel());
            var logText = Label("LogText", logPanel.transform, V(0.01f, 0.04f), V(0.99f, 0.96f), "", 20, TextAnchor.LowerLeft);
            log.logText = logText;

            // ================= MANAGEMENT PANELS (opened from the top menu buttons) =================
            Color mgmtColor = new Color(0.06f, 0.07f, 0.1f, 0.98f);

            // ---- Hero sheet
            var sheetPanel = Panel("HeroSheetPanel", root, V(0.26f, 0.2f), V(0.74f, 0.88f), mgmtColor);
            Label("Title", sheetPanel.transform, V(0.03f, 0.93f), V(0.97f, 0.99f), "HERO SHEET", 26, TextAnchor.MiddleLeft);
            RadarChart sheetChart, sheetChartUnused;
            BuildChartArea(sheetPanel.transform, "SheetChart", V(0.22f, 0.64f), new Color(0.3f, 0.6f, 1f, 0.65f), Color.clear, false, out sheetChart, out sheetChartUnused);
            var sheetInfo = Label("InfoText", sheetPanel.transform, V(0.45f, 0.3f), V(0.97f, 0.92f), "", 17, TextAnchor.UpperLeft);
            var sheetEquip = Label("EquipText", sheetPanel.transform, V(0.03f, 0.12f), V(0.45f, 0.4f), "", 16, TextAnchor.UpperLeft);
            var sPrev = MakeButton("PrevHeroButton", sheetPanel.transform, V(0.02f, 0.02f), V(0.2f, 0.1f), "Prev");
            var sNext = MakeButton("NextHeroButton", sheetPanel.transform, V(0.21f, 0.02f), V(0.39f, 0.1f), "Next");
            var sRenew = MakeButton("RenewButton", sheetPanel.transform, V(0.45f, 0.02f), V(0.7f, 0.1f), "Renew");
            var sDismiss = MakeButton("DismissButton", sheetPanel.transform, V(0.72f, 0.02f), V(0.97f, 0.1f), "Dismiss");
            sheetPanel.gameObject.SetActive(false);

            // ---- Inventory
            var invPanel = Panel("InventoryPanel", root, V(0.26f, 0.2f), V(0.74f, 0.88f), mgmtColor);
            Label("Title", invPanel.transform, V(0.03f, 0.93f), V(0.97f, 0.99f), "INVENTORY", 26, TextAnchor.MiddleLeft);
            var invList = Label("ItemListText", invPanel.transform, V(0.03f, 0.3f), V(0.5f, 0.92f), "", 17, TextAnchor.UpperLeft);
            var invDetail = Label("ItemDetailText", invPanel.transform, V(0.52f, 0.62f), V(0.97f, 0.92f), "", 17, TextAnchor.UpperLeft);
            var invHero = Label("HeroText", invPanel.transform, V(0.52f, 0.3f), V(0.97f, 0.6f), "", 17, TextAnchor.UpperLeft);
            var iPrevItem = MakeButton("PrevItemButton", invPanel.transform, V(0.02f, 0.17f), V(0.25f, 0.27f), "Prev Item");
            var iNextItem = MakeButton("NextItemButton", invPanel.transform, V(0.26f, 0.17f), V(0.49f, 0.27f), "Next Item");
            var iPrevHero = MakeButton("PrevHeroButton", invPanel.transform, V(0.51f, 0.17f), V(0.74f, 0.27f), "Prev Hero");
            var iNextHero = MakeButton("NextHeroButton", invPanel.transform, V(0.75f, 0.17f), V(0.98f, 0.27f), "Next Hero");
            var iSell = MakeButton("SellButton", invPanel.transform, V(0.02f, 0.04f), V(0.25f, 0.14f), "Sell 1");
            var iSellAll = MakeButton("SellAllButton", invPanel.transform, V(0.26f, 0.04f), V(0.49f, 0.14f), "Sell All");
            var iUse = MakeButton("UseButton", invPanel.transform, V(0.51f, 0.04f), V(0.74f, 0.14f), "Use / Equip");
            var iRepair = MakeButton("RepairButton", invPanel.transform, V(0.75f, 0.04f), V(0.98f, 0.14f), "Repair");
            invPanel.gameObject.SetActive(false);

            // ---- Witch
            var witchPanel = Panel("WitchPanel", root, V(0.26f, 0.2f), V(0.74f, 0.88f), mgmtColor);
            Label("Title", witchPanel.transform, V(0.03f, 0.93f), V(0.97f, 0.99f), "THE WITCH", 26, TextAnchor.MiddleLeft);
            var witchHero = Label("HeroText", witchPanel.transform, V(0.03f, 0.5f), V(0.5f, 0.92f), "", 18, TextAnchor.UpperLeft);
            var witchRecipe = Label("RecipeText", witchPanel.transform, V(0.52f, 0.5f), V(0.97f, 0.92f), "", 18, TextAnchor.UpperLeft);
            var wPrevHero = MakeButton("PrevHeroButton", witchPanel.transform, V(0.02f, 0.37f), V(0.25f, 0.47f), "Prev Hero");
            var wNextHero = MakeButton("NextHeroButton", witchPanel.transform, V(0.26f, 0.37f), V(0.49f, 0.47f), "Next Hero");
            var wPrevRecipe = MakeButton("PrevRecipeButton", witchPanel.transform, V(0.51f, 0.37f), V(0.74f, 0.47f), "Prev Scroll");
            var wNextRecipe = MakeButton("NextRecipeButton", witchPanel.transform, V(0.75f, 0.37f), V(0.98f, 0.47f), "Next Scroll");
            var wTemp = MakeButton("TempButton", witchPanel.transform, V(0.02f, 0.24f), V(0.33f, 0.34f), "Temp Potential");
            var wPerm = MakeButton("PermButton", witchPanel.transform, V(0.34f, 0.24f), V(0.65f, 0.34f), "Perm Potential");
            var wCraft = MakeButton("CraftButton", witchPanel.transform, V(0.66f, 0.24f), V(0.98f, 0.34f), "Craft Scroll");
            Label("HintText", witchPanel.transform, V(0.03f, 0.04f), V(0.97f, 0.2f),
                "Temporary potential: a boost for the next mission only. Permanent potential: a unique enchantment forever. Scrolls teach new skills (use them from the Inventory).", 16, TextAnchor.UpperLeft);
            witchPanel.gameObject.SetActive(false);

            // ---- Tavern & forge
            var buildPanel = Panel("TavernBuildPanel", root, V(0.26f, 0.2f), V(0.74f, 0.88f), mgmtColor);
            Label("Title", buildPanel.transform, V(0.03f, 0.93f), V(0.97f, 0.99f), "TAVERN & FORGE", 26, TextAnchor.MiddleLeft);
            var buildFacilities = Label("FacilitiesText", buildPanel.transform, V(0.03f, 0.4f), V(0.5f, 0.92f), "", 17, TextAnchor.UpperLeft);
            var buildForge = Label("ForgeText", buildPanel.transform, V(0.52f, 0.4f), V(0.97f, 0.92f), "", 17, TextAnchor.UpperLeft);
            var bPrevFac = MakeButton("PrevFacilityButton", buildPanel.transform, V(0.02f, 0.28f), V(0.17f, 0.38f), "Prev");
            var bNextFac = MakeButton("NextFacilityButton", buildPanel.transform, V(0.18f, 0.28f), V(0.33f, 0.38f), "Next");
            var bUpgrade = MakeButton("UpgradeButton", buildPanel.transform, V(0.34f, 0.28f), V(0.49f, 0.38f), "Build");
            var bPrevRec = MakeButton("PrevRecipeButton", buildPanel.transform, V(0.51f, 0.28f), V(0.66f, 0.38f), "Prev");
            var bNextRec = MakeButton("NextRecipeButton", buildPanel.transform, V(0.67f, 0.28f), V(0.82f, 0.38f), "Next");
            var bForgeBtn = MakeButton("ForgeButton", buildPanel.transform, V(0.83f, 0.28f), V(0.98f, 0.38f), "Forge");
            var buildSummary = Label("SummaryText", buildPanel.transform, V(0.03f, 0.04f), V(0.97f, 0.26f), "", 17, TextAnchor.UpperLeft);
            buildPanel.gameObject.SetActive(false);

            // ---- Mission report (appears when a party comes home)
            var reportPanel = Panel("ReportPanel", root, V(0.3f, 0.22f), V(0.7f, 0.86f), new Color(0.1f, 0.09f, 0.07f, 0.98f));
            var reportTitle = Label("Title", reportPanel.transform, V(0.03f, 0.88f), V(0.97f, 0.98f), "", 28, TextAnchor.MiddleLeft);
            var reportSummary = Label("Summary", reportPanel.transform, V(0.03f, 0.62f), V(0.97f, 0.87f), "", 18, TextAnchor.UpperLeft);
            var reportLoot = Label("Loot", reportPanel.transform, V(0.03f, 0.5f), V(0.97f, 0.62f), "", 17, TextAnchor.UpperLeft);
            var reportRows = new GameObject[3];
            var reportNames = new Text[3];
            var reportFills = new RectTransform[3];
            var reportExps = new Text[3];
            for (int ri = 0; ri < 3; ri++)
            {
                float rowY = 0.36f - ri * 0.12f;
                var rowPanel = Panel("Row" + ri, reportPanel.transform, V(0.02f, rowY), V(0.98f, rowY + 0.11f), new Color(1f, 1f, 1f, 0.05f));
                reportRows[ri] = rowPanel.gameObject;
                reportNames[ri] = Label("Name", rowPanel.transform, V(0.02f, 0f), V(0.4f, 1f), "", 16, TextAnchor.MiddleLeft);
                var barBg = Panel("BarBg", rowPanel.transform, V(0.42f, 0.3f), V(0.78f, 0.7f), new Color(0.22f, 0.22f, 0.22f, 1f));
                var barFill = Panel("Fill", barBg.transform, V(0f, 0f), V(0f, 1f), new Color(0.35f, 0.8f, 0.4f, 1f));
                reportFills[ri] = (RectTransform)barFill.transform;
                reportExps[ri] = Label("Exp", rowPanel.transform, V(0.8f, 0f), V(0.99f, 1f), "", 15, TextAnchor.MiddleLeft);
            }
            var reportContinue = MakeButton("ContinueButton", reportPanel.transform, V(0.35f, 0.02f), V(0.65f, 0.11f), "Continue");
            reportPanel.gameObject.SetActive(false);

            // ---- End screen (created last so it draws on top)
            var endPanel = Panel("EndPanel", root, V(0f, 0f), V(1f, 1f), new Color(0f, 0f, 0f, 0.88f));
            var endTitle = Label("EndTitle", endPanel.transform, V(0.2f, 0.55f), V(0.8f, 0.75f), "", 64, TextAnchor.MiddleCenter);
            var endReason = Label("EndReason", endPanel.transform, V(0.2f, 0.4f), V(0.8f, 0.55f), "", 28, TextAnchor.MiddleCenter);
            var endRestart = MakeButton("RestartButton", endPanel.transform, V(0.4f, 0.25f), V(0.6f, 0.35f), "Play Again");

            // ================= WIRING =================
            var ui = canvasGO.AddComponent<UIManager>();
            ui.dayText = dayText; ui.phaseText = phaseText; ui.goldText = goldText;
            UnityEventTools.AddPersistentListener(advanceBtn.onClick, ui.OnAdvancePhaseButtonClicked);

            var tavern = canvasGO.AddComponent<TavernUIManager>();
            tavern.rosterText = rosterText;

            var expeditions = canvasGO.AddComponent<ExpeditionsUIManager>();
            expeditions.expeditionsText = expText;

            var board = canvasGO.AddComponent<BoardUIManager>();
            board.bachecaPanel = boardPanel.gameObject;
            board.clientNameText = clientText;
            board.descriptionText = descText;
            board.rewardText = rewardText;
            board.selectedHeroText = selectedHeroText;
            board.currentPartyText = partyText;
            board.requirementsText = reqText;
            board.travelInfoText = travelText;
            board.acceptButton = bAccept;
            board.heroChart = heroChart;
            board.partyChart = partyChart;
            board.requirementChart = reqChart;
            UnityEventTools.AddPersistentListener(bNext.onClick, board.OnNextHeroClicked);
            UnityEventTools.AddPersistentListener(bAdd.onClick, board.OnAddHeroToPartyClicked);
            UnityEventTools.AddPersistentListener(bClear.onClick, board.OnClearPartyClicked);
            UnityEventTools.AddPersistentListener(bAccept.onClick, board.OnAcceptContractClicked);
            UnityEventTools.AddPersistentListener(bPrevC.onClick, board.OnPrevContractClicked);
            UnityEventTools.AddPersistentListener(bNextC.onClick, board.OnNextContractClicked);

            var recUI = canvasGO.AddComponent<RecruitmentUIManager>();
            recUI.visitorText = visitorText;
            recUI.costText = costText;
            recUI.heroChart = recChart;
            recUI.chartMax = 20f;
            recUI.tavernUI = tavern;
            UnityEventTools.AddPersistentListener(recNext.onClick, recUI.OnNextVisitorClicked);
            UnityEventTools.AddPersistentListener(recContract.onClick, recUI.OnCycleContractClicked);
            UnityEventTools.AddPersistentListener(recShare.onClick, recUI.OnCycleShareClicked);
            UnityEventTools.AddPersistentListener(recGear.onClick, recUI.OnToggleEquipmentClicked);
            UnityEventTools.AddPersistentListener(recHire.onClick, recUI.OnHireClicked);

            var endUI = canvasGO.AddComponent<EndScreenUIManager>();
            endUI.endPanel = endPanel.gameObject;
            endUI.titleText = endTitle;
            endUI.reasonText = endReason;
            UnityEventTools.AddPersistentListener(endRestart.onClick, endUI.OnRestartClicked);

            // ---- Management panels
            var sheetUI = canvasGO.AddComponent<HeroSheetUIManager>();
            sheetUI.panel = sheetPanel.gameObject; sheetUI.infoText = sheetInfo; sheetUI.equipText = sheetEquip;
            sheetUI.statChart = sheetChart; sheetUI.chartMax = 20f;
            UnityEventTools.AddPersistentListener(sPrev.onClick, sheetUI.OnPrevHeroClicked);
            UnityEventTools.AddPersistentListener(sNext.onClick, sheetUI.OnNextHeroClicked);
            UnityEventTools.AddPersistentListener(sRenew.onClick, sheetUI.OnRenewClicked);
            UnityEventTools.AddPersistentListener(sDismiss.onClick, sheetUI.OnDismissClicked);

            var invUI = canvasGO.AddComponent<InventoryUIManager>();
            invUI.panel = invPanel.gameObject; invUI.itemListText = invList; invUI.itemDetailText = invDetail; invUI.heroText = invHero;
            UnityEventTools.AddPersistentListener(iPrevItem.onClick, invUI.OnPrevItemClicked);
            UnityEventTools.AddPersistentListener(iNextItem.onClick, invUI.OnNextItemClicked);
            UnityEventTools.AddPersistentListener(iPrevHero.onClick, invUI.OnPrevHeroClicked);
            UnityEventTools.AddPersistentListener(iNextHero.onClick, invUI.OnNextHeroClicked);
            UnityEventTools.AddPersistentListener(iSell.onClick, invUI.OnSellClicked);
            UnityEventTools.AddPersistentListener(iSellAll.onClick, invUI.OnSellAllClicked);
            UnityEventTools.AddPersistentListener(iUse.onClick, invUI.OnUseClicked);
            UnityEventTools.AddPersistentListener(iRepair.onClick, invUI.OnRepairClicked);

            var witchUI = canvasGO.AddComponent<WitchUIManager>();
            witchUI.panel = witchPanel.gameObject; witchUI.heroText = witchHero; witchUI.recipeText = witchRecipe;
            UnityEventTools.AddPersistentListener(wPrevHero.onClick, witchUI.OnPrevHeroClicked);
            UnityEventTools.AddPersistentListener(wNextHero.onClick, witchUI.OnNextHeroClicked);
            UnityEventTools.AddPersistentListener(wPrevRecipe.onClick, witchUI.OnPrevRecipeClicked);
            UnityEventTools.AddPersistentListener(wNextRecipe.onClick, witchUI.OnNextRecipeClicked);
            UnityEventTools.AddPersistentListener(wTemp.onClick, witchUI.OnTempPotentialClicked);
            UnityEventTools.AddPersistentListener(wPerm.onClick, witchUI.OnPermPotentialClicked);
            UnityEventTools.AddPersistentListener(wCraft.onClick, witchUI.OnCraftClicked);

            var buildUI = canvasGO.AddComponent<TavernBuildUIManager>();
            buildUI.panel = buildPanel.gameObject; buildUI.facilitiesText = buildFacilities; buildUI.forgeText = buildForge; buildUI.summaryText = buildSummary;
            UnityEventTools.AddPersistentListener(bPrevFac.onClick, buildUI.OnPrevFacilityClicked);
            UnityEventTools.AddPersistentListener(bNextFac.onClick, buildUI.OnNextFacilityClicked);
            UnityEventTools.AddPersistentListener(bUpgrade.onClick, buildUI.OnUpgradeClicked);
            UnityEventTools.AddPersistentListener(bPrevRec.onClick, buildUI.OnPrevRecipeClicked);
            UnityEventTools.AddPersistentListener(bNextRec.onClick, buildUI.OnNextRecipeClicked);
            UnityEventTools.AddPersistentListener(bForgeBtn.onClick, buildUI.OnForgeClicked);

            var menu = canvasGO.AddComponent<ManagementMenuUIManager>();
            menu.panels = new GameObject[] { sheetPanel.gameObject, invPanel.gameObject, witchPanel.gameObject, buildPanel.gameObject };
            menu.heroSheet = sheetUI; menu.inventoryUI = invUI; menu.witchUI = witchUI; menu.buildUI = buildUI;
            UnityEventTools.AddPersistentListener(menuHeroes.onClick, menu.OnHeroesClicked);
            UnityEventTools.AddPersistentListener(menuInventory.onClick, menu.OnInventoryClicked);
            UnityEventTools.AddPersistentListener(menuWitch.onClick, menu.OnWitchClicked);
            UnityEventTools.AddPersistentListener(menuTavern.onClick, menu.OnTavernClicked);

            // ---- Mission report, save/load, tutorial
            var reportUI = canvasGO.AddComponent<MissionReportUIManager>();
            reportUI.panel = reportPanel.gameObject; reportUI.titleText = reportTitle; reportUI.summaryText = reportSummary; reportUI.lootText = reportLoot;
            reportUI.rowObjects = reportRows; reportUI.nameTexts = reportNames; reportUI.barFills = reportFills; reportUI.expTexts = reportExps;
            UnityEventTools.AddPersistentListener(reportContinue.onClick, reportUI.OnContinueClicked);

            var saveLoad = managers.GetComponent<SaveLoadManager>();
            UnityEventTools.AddPersistentListener(saveBtn.onClick, saveLoad.OnSaveClicked);
            UnityEventTools.AddPersistentListener(loadBtn.onClick, saveLoad.OnLoadClicked);

            managers.GetComponent<TutorialManager>().hintText = hint;

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = managers;

            EditorUtility.DisplayDialog("Hero Management",
                "Demo scene built!\n\n- Content: Assets/_Project/Data/Demo\n- Objects: 'Managers' and 'Canvas'\n\nSave the scene (Ctrl+S), then press Play.\n" +
                "Press SPACE to advance phases: the board appears in the 'Day' phase.", "OK");
        }

        // ---------------------------------------------------------------- content helpers

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        // Reuses the asset if it already exists, so running the tool twice never overwrites your edits
        private static T GetOrCreate<T>(string fileName, out bool created) where T : ScriptableObject
        {
            string path = DataFolder + "/" + fileName + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                created = false;
                return existing;
            }

            var obj = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(obj, path);
            created = true;
            return obj;
        }

        private static SkillData MakeSkill(string file, string name, string desc, SkillType type, int dmg, int mana, string trigger, int value)
        {
            var s = GetOrCreate<SkillData>(file, out bool created);
            if (created)
            {
                s.skillName = name; s.description = desc; s.type = type;
                s.baseDamageOrHeal = dmg; s.manaCost = mana; s.animTrigger = trigger; s.scrollMarketValue = value;
                EditorUtility.SetDirty(s);
            }
            return s;
        }

        private static ItemData MakeItem(string file, string name, string desc, ItemType type, ItemRarity rarity, int gold, int stack, SkillData skill, float weight, int powerBonus)
        {
            var it = GetOrCreate<ItemData>(file, out bool created);
            if (created)
            {
                it.itemName = name; it.description = desc; it.itemType = type; it.rarity = rarity;
                it.goldValue = gold; it.maxStack = stack; it.skillTaught = skill;
                it.weight = weight; it.powerBonus = powerBonus;
                EditorUtility.SetDirty(it);
            }
            return it;
        }

        private static HeroData MakeHero(string file, string name, HeroClass hClass, HeroRace race, PersonalityType personality,
            int age, int level, HeroStats stats, int upkeep, FameTier fame, string bio, MountType mount,
            ItemData weapon, ItemData armor, params SkillData[] skills)
        {
            var h = GetOrCreate<HeroData>(file, out bool created);
            if (created)
            {
                h.heroName = name; h.heroClass = hClass; h.heroRace = race; h.personality = personality;
                h.age = age; h.level = level; h.baseStats = stats; h.dailyUpkeepCost = upkeep;
                h.fameTier = fame; h.heroBio = bio; h.mount = mount;
                h.equippedWeapon = weapon; h.equippedArmor = armor;

                h.equippedSkills = new SkillData[HeroData.MaxSkillSlots];
                for (int i = 0; i < skills.Length && i < HeroData.MaxSkillSlots; i++) h.equippedSkills[i] = skills[i];

                EditorUtility.SetDirty(h);
            }
            return h;
        }

        private static ContractData MakeContract(string file, string client, string desc, string kingdom, Region region,
            TravelDifficulty difficulty, int days, int gold, int slots, RequiredSkill skill, bool hasPreferred, HeroClass preferred,
            int recommendedPower, MissionType missionType, ContractData.ItemReward[] rewards, System.Action<ContractData> extra)
        {
            var c = GetOrCreate<ContractData>(file, out bool created);
            if (created)
            {
                c.clientName = client; c.contractDescription = desc; c.kingdomName = kingdom; c.targetRegion = region;
                c.travelDifficulty = difficulty; c.estimatedDays = days; c.baseRewardGold = gold; c.maxAllowedSlots = slots;
                c.primarySkillRequired = skill; c.hasPreferredClass = hasPreferred; c.preferredClass = preferred;
                c.recommendedPower = recommendedPower; c.missionType = missionType;
                c.rewardItems = new List<ContractData.ItemReward>(rewards);
                if (extra != null) extra(c);
                EditorUtility.SetDirty(c);
            }
            return c;
        }

        // ---------------------------------------------------------------- UI helpers

        private static RectTransform FixedRect(string name, Transform parent, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        private static RadarChart NewChart(string name, Transform parent, Color color, bool outline, float fixedValue)
        {
            var rt = NewRect(name, parent, Vector2.zero, Vector2.one);
            var chart = rt.gameObject.AddComponent<RadarChart>();
            chart.color = color;
            chart.raycastTarget = false;
            chart.outlineOnly = outline;
            chart.maxValue = 1f;
            chart.values = new float[HeroStats.Count];
            for (int i = 0; i < chart.values.Length; i++) chart.values[i] = fixedValue;
            return chart;
        }

        private static void AddChartLabels(Transform area, float chartSize)
        {
            for (int i = 0; i < HeroStats.Count; i++)
            {
                float angle = Mathf.PI / 2f - i * 2f * Mathf.PI / HeroStats.Count;
                var rt = FixedRect("Label_" + HeroStats.ShortNames[i], area, new Vector2(0.5f, 0.5f), new Vector2(chartSize < 200f ? 50f : 70f, chartSize < 200f ? 20f : 26f));
                rt.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (chartSize * 0.4f + (chartSize < 200f ? 16f : 26f));

                var t = rt.gameObject.AddComponent<Text>();
                t.font = GetFont();
                t.text = HeroStats.ShortNames[i];
                t.fontSize = chartSize < 200f ? 13 : 18;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = Color.white;
                t.raycastTarget = false;
            }
        }

        // A radar chart with grid rings, a filled 'main' polygon and an optional outline overlay
        private static void BuildChartArea(Transform parent, string name, Vector2 anchor, Color fill, Color overlayColor,
            bool hasOverlay, out RadarChart main, out RadarChart overlay, float size = 260f)
        {
            var area = FixedRect(name, parent, anchor, new Vector2(size, size));

            NewChart("GridFull", area, new Color(1f, 1f, 1f, 0.08f), false, 1f);
            NewChart("GridHalf", area, new Color(1f, 1f, 1f, 0.08f), false, 0.5f);
            main = NewChart("Values", area, fill, false, 0f);
            overlay = hasOverlay ? NewChart("Recommended", area, overlayColor, true, 0f) : null;

            AddChartLabels(area, size);
        }

        private static Vector2 V(float x, float y) { return new Vector2(x, y); }
        private static Color DarkPanel() { return new Color(0.08f, 0.09f, 0.12f, 0.85f); }

        private static Font GetFont()
        {
#if UNITY_2022_2_OR_NEWER
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }

        private static RectTransform NewRect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static Image Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var rt = NewRect(name, parent, min, max);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static Text Label(string name, Transform parent, Vector2 min, Vector2 max, string text, int size, TextAnchor anchor)
        {
            var rt = NewRect(name, parent, min, max);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = GetFont();
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return t;
        }

        private static Button MakeButton(string name, Transform parent, Vector2 min, Vector2 max, string label)
        {
            var img = Panel(name, parent, min, max, new Color(0.25f, 0.35f, 0.6f, 1f));
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            Label("Text", img.transform, Vector2.zero, Vector2.one, label, 24, TextAnchor.MiddleCenter);
            return btn;
        }
    }
}

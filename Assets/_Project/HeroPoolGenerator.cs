using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// IMPORTANT: this file must live in a folder called "Editor" (e.g. Assets/Editor/HeroPoolGenerator.cs)
// Creates mercenaries as normal HeroData assets, so saving, loading and everything else keeps working.
namespace HeroManagement.EditorTools
{
    public static class HeroPoolGenerator
    {
        private const string Folder = "Assets/_Project/Data/Mercenaries";

        private static readonly Dictionary<HeroRace, string[]> Names = new Dictionary<HeroRace, string[]>
        {
            { HeroRace.Human, new[] { "Aldric", "Mira", "Tomas", "Elsa", "Garrick", "Lena", "Bran", "Sofia", "Hugo", "Clara" } },
            { HeroRace.Elf, new[] { "Aelindra", "Faelar", "Sylwen", "Elowen", "Caelith", "Thalia", "Ilyan", "Nerys" } },
            { HeroRace.Orc, new[] { "Gruk", "Mogra", "Thrag", "Urzul", "Brakka", "Zogar", "Ghaz", "Drogna" } },
            { HeroRace.Dwarf, new[] { "Brum", "Dagna", "Kholin", "Hilda", "Borrik", "Ragna", "Torvald", "Gerda" } },
            { HeroRace.DemiHuman, new[] { "Nyx", "Vesper", "Caius", "Isolde", "Rowan", "Sable", "Orsin", "Lysa" } },
            { HeroRace.Beastkin, new[] { "Kiba", "Luma", "Rukh", "Tamsin", "Fenn", "Yara", "Bastet", "Koro" } }
        };

        private static readonly string[] Epithets =
        {
            "the Bold", "Ironhand", "the Quiet", "Swiftfoot", "the Lucky", "Stonefist", "the Wanderer",
            "Brightblade", "the Grim", "Oakheart", "the Sly", "Redcloak", "the Elder", "Nightwatch"
        };

        // STR, INT, SPD, VIT, HEAL, CHA
        private static readonly Dictionary<HeroClass, int[]> ClassBase = new Dictionary<HeroClass, int[]>
        {
            { HeroClass.Warrior, new[] { 10, 3, 5, 9, 2, 4 } },
            { HeroClass.Ranger, new[] { 6, 5, 10, 6, 3, 6 } },
            { HeroClass.Mage, new[] { 3, 11, 6, 4, 6, 5 } },
            { HeroClass.Cleric, new[] { 5, 7, 4, 8, 11, 6 } },
            { HeroClass.Rogue, new[] { 5, 5, 11, 5, 2, 8 } }
        };

        // Same growth weights as HeroData.GetGrowthWeights()
        private static readonly Dictionary<HeroClass, int[]> ClassGrowth = new Dictionary<HeroClass, int[]>
        {
            { HeroClass.Warrior, new[] { 4, 0, 1, 3, 0, 1 } },
            { HeroClass.Ranger, new[] { 1, 1, 4, 2, 0, 1 } },
            { HeroClass.Mage, new[] { 0, 5, 1, 1, 1, 1 } },
            { HeroClass.Cleric, new[] { 0, 2, 0, 2, 4, 1 } },
            { HeroClass.Rogue, new[] { 1, 1, 5, 1, 0, 2 } }
        };

        private static readonly Dictionary<HeroRace, int[]> RaceMod = new Dictionary<HeroRace, int[]>
        {
            { HeroRace.Human, new[] { 0, 0, 0, 0, 0, 1 } },
            { HeroRace.Elf, new[] { 0, 2, 2, -2, 0, 0 } },
            { HeroRace.Orc, new[] { 3, -2, 0, 2, 0, -2 } },
            { HeroRace.Dwarf, new[] { 1, 0, -2, 3, 0, 0 } },
            { HeroRace.DemiHuman, new[] { 0, 1, 0, 0, 0, 1 } },
            { HeroRace.Beastkin, new[] { 1, -1, 3, 0, 0, 0 } }
        };

        // ---------------------------------------------------------------- menu

        [MenuItem("Hero Management/Generate 20 More Mercenaries")]
        public static void GenerateMoreFromMenu()
        {
            var created = Generate(20);

            var managers = GameObject.Find("Managers");
            var recruitment = managers != null ? managers.GetComponent<RecruitmentManager>() : null;
            int added = 0;

            if (recruitment != null)
            {
                foreach (var hero in created)
                {
                    if (!recruitment.availableRecruitsDatabase.Contains(hero))
                    {
                        recruitment.availableRecruitsDatabase.Add(hero);
                        added++;
                    }
                }
                EditorUtility.SetDirty(recruitment);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            EditorUtility.DisplayDialog("Hero Management",
                $"{created.Count} mercenaries created in {Folder}.\n" +
                (recruitment != null ? $"{added} added to the RecruitmentManager of this scene." : "No 'Managers' object found: add them to RecruitmentManager.availableRecruitsDatabase by hand."), "OK");
        }

        // ---------------------------------------------------------------- public API

        // Reuses the mercenaries already generated, so running the demo builder twice never piles them up
        public static List<HeroData> GetOrGenerate(int count)
        {
            var existing = LoadExisting();
            if (existing.Count >= count) return existing.GetRange(0, count);

            existing.AddRange(Generate(count - existing.Count));
            return existing;
        }

        public static List<HeroData> Generate(int count)
        {
            EnsureFolder();

            var items = LoadAll<ItemData>();
            var weapons = items.FindAll(i => i.itemType == ItemType.Weapon);
            var armors = items.FindAll(i => i.itemType == ItemType.Armor);
            weapons.Sort((a, b) => a.powerBonus.CompareTo(b.powerBonus));
            armors.Sort((a, b) => a.powerBonus.CompareTo(b.powerBonus));
            var skills = LoadAll<SkillData>();

            var usedNames = new HashSet<string>();
            foreach (var h in LoadAll<HeroData>()) usedNames.Add(h.heroName);

            int startIndex = LoadExisting().Count;
            var created = new List<HeroData>();

            for (int n = 0; n < count; n++)
            {
                var hero = ScriptableObject.CreateInstance<HeroData>();
                Fill(hero, usedNames, weapons, armors, skills);

                string safeName = hero.heroName.Replace(" ", "_");
                AssetDatabase.CreateAsset(hero, $"{Folder}/Merc_{startIndex + n + 1:000}_{safeName}.asset");
                created.Add(hero);
            }

            AssetDatabase.SaveAssets();
            return created;
        }

        // ---------------------------------------------------------------- hero creation

        private static void Fill(HeroData hero, HashSet<string> usedNames, List<ItemData> weapons, List<ItemData> armors, List<SkillData> skills)
        {
            HeroRace race = Pick(new[] { HeroRace.Human, HeroRace.Elf, HeroRace.Orc, HeroRace.Dwarf, HeroRace.DemiHuman, HeroRace.Beastkin },
                                 new[] { 30, 15, 15, 15, 10, 15 });
            HeroClass heroClass = PickClass(race);
            FameTier fame = Pick(new[] { FameTier.Unknown, FameTier.LocalHero, FameTier.Veteran, FameTier.Legend }, new[] { 60, 25, 12, 3 });
            PersonalityType personality = (PersonalityType)Random.Range(0, 5);

            int level;
            switch (fame)
            {
                case FameTier.LocalHero: level = Random.Range(3, 6); break;
                case FameTier.Veteran: level = Random.Range(6, 10); break;
                case FameTier.Legend: level = Random.Range(10, 15); break;
                default: level = Random.Range(1, 3); break;
            }

            // ---- stats: class base + race + a little luck + the growth of the levels already gained
            int[] stats = new int[6];
            for (int i = 0; i < 6; i++) stats[i] = Mathf.Max(1, ClassBase[heroClass][i] + RaceMod[race][i] + Random.Range(-1, 3));

            int[] growth = ClassGrowth[heroClass];
            int growthSum = 0;
            foreach (int w in growth) growthSum += w;
            for (int p = 0; p < (level - 1) * 3; p++)
            {
                int roll = Random.Range(0, growthSum);
                int index = 0;
                while (index < growth.Length - 1 && roll >= growth[index]) { roll -= growth[index]; index++; }
                stats[index]++;
            }

            // ---- identity
            string[] pool = Names[race];
            string heroName = pool[Random.Range(0, pool.Length)];
            if (usedNames.Contains(heroName)) heroName += " " + Epithets[Random.Range(0, Epithets.Length)];
            int suffix = 2;
            string baseName = heroName;
            while (usedNames.Contains(heroName)) heroName = baseName + " " + suffix++;
            usedNames.Add(heroName);

            hero.heroName = heroName;
            hero.heroClass = heroClass;
            hero.heroRace = race;
            hero.personality = personality;
            hero.fameTier = fame;
            hero.age = Random.Range(18, 61);
            hero.level = level;
            hero.baseStats = new HeroStats(stats[0], stats[1], stats[2], stats[3], stats[4], stats[5]);
            hero.dailyUpkeepCost = 5 + 2 * (level - 1) + Random.Range(0, 4);
            hero.heroBio = $"A {personality.ToString().ToLower()} {race} {heroClass} looking for work.";

            float roll01 = Random.value;
            hero.mount = roll01 < 0.02f ? MountType.Flying : (roll01 < 0.17f ? MountType.Horse : MountType.None);

            // ---- gear: better heroes arrive better equipped
            float quality = fame == FameTier.Unknown ? 0f : (fame == FameTier.LocalHero ? 0.5f : (fame == FameTier.Veteran ? 0.8f : 1f));
            if (weapons.Count > 0 && Random.value < 0.8f) hero.equippedWeapon = weapons[Mathf.RoundToInt(quality * (weapons.Count - 1))];
            if (armors.Count > 0 && Random.value < 0.7f) hero.equippedArmor = armors[Mathf.RoundToInt(quality * (armors.Count - 1))];

            // ---- skills: 0-2, of a type that fits the class
            hero.equippedSkills = new SkillData[HeroData.MaxSkillSlots];
            int skillCount = fame == FameTier.Unknown ? Random.Range(0, 2) : Random.Range(1, 3);
            var preferred = skills.FindAll(s => FitsClass(s.type, heroClass));
            if (preferred.Count == 0) preferred = skills;

            for (int s = 0; s < skillCount && preferred.Count > 0; s++)
            {
                var skill = preferred[Random.Range(0, preferred.Count)];
                if (System.Array.IndexOf(hero.equippedSkills, skill) < 0) hero.equippedSkills[s] = skill;
            }
        }

        private static bool FitsClass(SkillType type, HeroClass heroClass)
        {
            switch (heroClass)
            {
                case HeroClass.Warrior: return type == SkillType.Physical;
                case HeroClass.Ranger: return type == SkillType.Physical || type == SkillType.Utility;
                case HeroClass.Mage: return type == SkillType.Magical;
                case HeroClass.Cleric: return type == SkillType.Magical || type == SkillType.Utility;
                case HeroClass.Rogue: return type == SkillType.Utility || type == SkillType.Physical;
                default: return true;
            }
        }

        private static HeroClass PickClass(HeroRace race)
        {
            var classes = new[] { HeroClass.Warrior, HeroClass.Ranger, HeroClass.Mage, HeroClass.Cleric, HeroClass.Rogue };
            switch (race)
            {
                case HeroRace.Orc: return Pick(classes, new[] { 50, 15, 10, 10, 15 });
                case HeroRace.Elf: return Pick(classes, new[] { 5, 35, 35, 15, 10 });
                case HeroRace.Dwarf: return Pick(classes, new[] { 40, 10, 10, 30, 10 });
                case HeroRace.DemiHuman: return Pick(classes, new[] { 15, 10, 30, 20, 25 });
                case HeroRace.Beastkin: return Pick(classes, new[] { 25, 30, 5, 5, 35 });
                default: return Pick(classes, new[] { 20, 20, 20, 20, 20 });
            }
        }

        private static T Pick<T>(T[] values, int[] weights)
        {
            int total = 0;
            foreach (int w in weights) total += w;

            int roll = Random.Range(0, total);
            for (int i = 0; i < values.Length; i++)
            {
                roll -= weights[i];
                if (roll < 0) return values[i];
            }
            return values[values.Length - 1];
        }

        // ---------------------------------------------------------------- assets

        private static void EnsureFolder()
        {
            string[] parts = Folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static List<T> LoadAll<T>() where T : Object
        {
            var list = new List<T>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) list.Add(asset);
            }
            return list;
        }

        private static List<HeroData> LoadExisting()
        {
            var list = new List<HeroData>();
            if (!AssetDatabase.IsValidFolder(Folder)) return list;

            foreach (string guid in AssetDatabase.FindAssets("t:HeroData", new[] { Folder }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<HeroData>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) list.Add(asset);
            }
            return list;
        }
    }
}

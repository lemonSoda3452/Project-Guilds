using System.Collections.Generic;
using UnityEngine;

namespace HeroManagement
{
    // Keep the order of these enums: Unity saves them as integers. Add new values only at the END.
    public enum HeroClass { Warrior, Ranger, Mage, Cleric, Rogue }
    public enum HeroRace { Human, Elf, Orc, Dwarf, DemiHuman, Beastkin }
    public enum FameTier { Unknown, LocalHero, Veteran, Legend }
    public enum PersonalityType { Neutral, Greedy, Altruistic, GlorySeeker, Cautious }
    public enum EmploymentType { None, Permanent, MissionBased, TimeBased }
    public enum MountType { None, Horse, Flying }

    [CreateAssetMenu(fileName = "New Hero", menuName = "Hero Management/Hero Data")]
    public class HeroData : ScriptableObject
    {
        public const int MaxSkillSlots = 4;

        [Header("Identity & Progression")]
        public string heroName;
        [TextArea(2, 4)]
        public string heroBio;
        public HeroClass heroClass;
        public HeroRace heroRace;
        public PersonalityType personality;
        public FameTier fameTier = FameTier.Unknown;
        public int age = 25;
        public int level = 1;
        public int currentExp = 0;

        [Header("RPG Stats")]
        public HeroStats baseStats = new HeroStats(8, 8, 8, 8, 8, 8);
        [Tooltip("Gained from level-ups during a session (runtime)")]
        public HeroStats levelBonusStats;

        [Header("Progression Tracking (runtime)")]
        public int levelsGained = 0;
        public int bonusUpkeepGained = 0;

        [Header("Combat Skills (Max 4 Slots)")]
        public SkillData baseAttack;
        public SkillData baseDefense;
        [Tooltip("The 4 unique abilities learned or equipped via scrolls")]
        public SkillData[] equippedSkills = new SkillData[MaxSkillSlots];
        [HideInInspector] public bool hasSkillSnapshot = false;
        [HideInInspector] public SkillData[] skillSnapshot;

        [Header("Equipment & Travel")]
        public ItemData equippedWeapon;
        public ItemData equippedArmor;
        [Range(0, 100)] public int weaponDurability = 100;
        [Range(0, 100)] public int armorDurability = 100;
        public MountType mount = MountType.None;
        [HideInInspector] public bool hasGearSnapshot = false;
        [HideInInspector] public ItemData snapshotWeapon;
        [HideInInspector] public ItemData snapshotArmor;

        [Header("Economy")]
        public int dailyUpkeepCost;
        public int successCount = 0;
        public int failCount = 0;

        [Header("Relationships")]
        public List<HeroData> favoredCompanions = new List<HeroData>();
        public List<HeroData> hatedCompanions = new List<HeroData>();

        [Header("Employment (runtime)")]
        public EmploymentType currentContract = EmploymentType.None;
        public int contractDurationLeft = 0;
        public bool hasFreeEquipmentClause = false;
        [Tooltip("Percent of each mission reward this hero takes home (agreed at hire)")]
        [Range(0, 100)] public int agreedSharePercent = 20;
        [Range(0, 100)] public int loyalty = 100;

        [Header("Health & Rest (runtime)")]
        [Range(0, 100)] public int injury = 0;
        public int restDaysLeft = 0;
        public int idleDays = 0;
        public int missionRequests = 0;

        [Header("Witch's Unlocked Potential")]
        public bool hasTemporaryPotential = false;
        public bool hasPermanentPotential = false;
        [Tooltip("The Minecraft-style unique enchantment text")]
        public string permanentEnchantmentDesc;

        [Header("State")]
        public bool isBusy = false;
        public bool isDead = false;

        // ---------------------------------------------------------------- identity & stats

        public string GetFormattedName()
        {
            return $"{heroName} (Lv.{level} {heroRace} {heroClass})";
        }

        public HeroStats GetStats() { return baseStats + levelBonusStats; }
        public int GetStat(StatType type) { return GetStats().Get(type); }
        public int TotalStatPoints { get { return GetStats().Total; } }

        public int GetRacialSurvivalBonus()
        {
            switch (heroRace)
            {
                case HeroRace.Orc: return 15;
                case HeroRace.Elf: return 10;
                case HeroRace.Dwarf: return 12;
                case HeroRace.DemiHuman: return 5;
                default: return 0;
            }
        }

        // Level-up: distributes points among the stats the class cares about
        public void GrowStats(int points)
        {
            int[] weights = GetGrowthWeights();
            int sum = 0;
            foreach (int w in weights) sum += w;
            if (sum <= 0) return;

            for (int p = 0; p < points; p++)
            {
                int roll = Random.Range(0, sum);
                int index = 0;
                while (index < weights.Length - 1 && roll >= weights[index])
                {
                    roll -= weights[index];
                    index++;
                }
                levelBonusStats.Add((StatType)index, 1);
            }
        }

        // STR, INT, SPD, VIT, HEAL, CHA
        private int[] GetGrowthWeights()
        {
            switch (heroClass)
            {
                case HeroClass.Warrior: return new[] { 4, 0, 1, 3, 0, 1 };
                case HeroClass.Ranger: return new[] { 1, 1, 4, 2, 0, 1 };
                case HeroClass.Mage: return new[] { 0, 5, 1, 1, 1, 1 };
                case HeroClass.Cleric: return new[] { 0, 2, 0, 2, 4, 1 };
                case HeroClass.Rogue: return new[] { 1, 1, 5, 1, 0, 2 };
                default: return new[] { 1, 1, 1, 1, 1, 1 };
            }
        }

        // Overall strength, used by the mission success formula
        public int GetPower()
        {
            NormalizeSkillSlots();

            int power = (level * 3) + (TotalStatPoints / 6) + GetGearRating();
            if (baseAttack != null) power += 2;
            if (baseDefense != null) power += 2;

            foreach (var skill in equippedSkills)
            {
                if (skill != null) power += 2 + Mathf.Max(0, skill.baseDamageOrHeal) / 10;
            }

            if (hasPermanentPotential) power += 5;
            return power;
        }

        // Does this hero fit the skill a contract asks for? (class and race based)
        public bool HasAptitudeFor(RequiredSkill skill)
        {
            switch (skill)
            {
                case RequiredSkill.Combat:
                    return heroClass == HeroClass.Warrior || heroClass == HeroClass.Rogue || heroRace == HeroRace.Orc;
                case RequiredSkill.Mind:
                    return heroClass == HeroClass.Mage || heroClass == HeroClass.Cleric;
                case RequiredSkill.Tracking:
                    return heroClass == HeroClass.Ranger || heroClass == HeroClass.Rogue || heroRace == HeroRace.Elf;
                case RequiredSkill.Diplomacy:
                    return heroClass == HeroClass.Cleric || heroRace == HeroRace.Human || heroRace == HeroRace.DemiHuman;
                default:
                    return true;
            }
        }

        // ---------------------------------------------------------------- gear & weight

        public int GetWeaponRating()
        {
            return equippedWeapon != null ? Mathf.RoundToInt(equippedWeapon.powerBonus * weaponDurability / 100f) : 0;
        }

        public int GetArmorRating()
        {
            return equippedArmor != null ? Mathf.RoundToInt(equippedArmor.powerBonus * armorDurability / 100f) : 0;
        }

        public int GetGearRating() { return GetWeaponRating() + GetArmorRating(); }

        public float GetGearWeight()
        {
            float w = 0f;
            if (equippedWeapon != null) w += equippedWeapon.weight;
            if (equippedArmor != null) w += equippedArmor.weight;
            return w;
        }

        // How many kg the hero can carry in total (gear included)
        public float GetCarryCapacity()
        {
            return 20f + GetStat(StatType.Strength) * 2.5f + GetStat(StatType.Vitality);
        }

        // Equips an item, remembering the original gear so a session reset can restore it
        public void SetEquipment(ItemData item, bool isWeapon)
        {
            if (!hasGearSnapshot)
            {
                snapshotWeapon = equippedWeapon;
                snapshotArmor = equippedArmor;
                hasGearSnapshot = true;
            }

            if (isWeapon) { equippedWeapon = item; weaponDurability = 100; }
            else { equippedArmor = item; armorDurability = 100; }
        }

        // ---------------------------------------------------------------- travel, health, pay

        // 1.0 = average. Speed stat and mount (horse x1.5, flying x2) make trips shorter, both ways.
        public float GetTravelSpeedMultiplier()
        {
            float statFactor = 1f + (GetStat(StatType.Speed) - 8) * 0.03f;
            float mountFactor = mount == MountType.Horse ? 1.5f : (mount == MountType.Flying ? 2f : 1f);
            return Mathf.Max(0.5f, statFactor * mountFactor);
        }

        // Injury points healed per day of rest
        public float GetDailyRecovery()
        {
            return 4f + GetStat(StatType.Vitality) * 0.4f + GetStat(StatType.Healing) * 0.2f;
        }

        // Rest needed after a mission, from the injury level and the hero's stats
        public int GetRestDaysFor(int injuryValue)
        {
            if (injuryValue <= 0) return 0;
            return Mathf.CeilToInt(injuryValue / GetDailyRecovery());
        }

        // What the hero expects to be paid per successful mission (depends on level, fame, personality)
        public int GetExpectedPayPerMission()
        {
            float pay = 20f + level * 10f;

            switch (personality)
            {
                case PersonalityType.Greedy: pay *= 1.4f; break;
                case PersonalityType.Altruistic: pay *= 0.7f; break;
                case PersonalityType.GlorySeeker: pay *= 0.9f; break;
            }

            switch (fameTier)
            {
                case FameTier.LocalHero: pay *= 1.2f; break;
                case FameTier.Veteran: pay *= 1.5f; break;
                case FameTier.Legend: pay *= 2.2f; break;
            }

            return Mathf.RoundToInt(pay);
        }

        // ---------------------------------------------------------------- skills

        public void NormalizeSkillSlots()
        {
            if (equippedSkills == null)
            {
                equippedSkills = new SkillData[MaxSkillSlots];
            }
            else if (equippedSkills.Length != MaxSkillSlots)
            {
                var fixedArray = new SkillData[MaxSkillSlots];
                System.Array.Copy(equippedSkills, fixedArray, Mathf.Min(equippedSkills.Length, MaxSkillSlots));
                equippedSkills = fixedArray;
            }
        }

        public int GetFirstEmptySkillSlot()
        {
            NormalizeSkillSlots();
            for (int i = 0; i < MaxSkillSlots; i++)
            {
                if (equippedSkills[i] == null) return i;
            }
            return -1;
        }

        public bool KnowsSkill(SkillData skill)
        {
            if (skill == null) return false;
            NormalizeSkillSlots();
            for (int i = 0; i < MaxSkillSlots; i++)
            {
                if (equippedSkills[i] == skill) return true;
            }
            return false;
        }

        public bool LearnSkill(SkillData newSkill, int slotIndex)
        {
            if (newSkill == null) return false;
            if (slotIndex < 0 || slotIndex >= MaxSkillSlots) return false;

            NormalizeSkillSlots();

            if (!hasSkillSnapshot)
            {
                skillSnapshot = (SkillData[])equippedSkills.Clone();
                hasSkillSnapshot = true;
            }

            equippedSkills[slotIndex] = newSkill;
            Debug.Log($"[HERO] {heroName} learned {newSkill.skillName} in slot {slotIndex + 1}!");
            return true;
        }

        public void ResetTemporaryPotential()
        {
            hasTemporaryPotential = false;
        }

        // Clears everything that changes during a play session.
        // Needed because ScriptableObject changes persist in the Editor between Play sessions.
        public void ResetRuntimeState()
        {
            // Level-ups
            level = Mathf.Max(1, level - levelsGained);
            dailyUpkeepCost -= bonusUpkeepGained;
            levelsGained = 0;
            bonusUpkeepGained = 0;
            levelBonusStats = new HeroStats();
            currentExp = 0;

            // Skills
            if (hasSkillSnapshot && skillSnapshot != null) equippedSkills = (SkillData[])skillSnapshot.Clone();
            hasSkillSnapshot = false;
            skillSnapshot = null;

            // Gear
            if (hasGearSnapshot)
            {
                equippedWeapon = snapshotWeapon;
                equippedArmor = snapshotArmor;
            }
            hasGearSnapshot = false;
            snapshotWeapon = null;
            snapshotArmor = null;
            weaponDurability = 100;
            armorDurability = 100;

            // State
            isBusy = false;
            isDead = false;
            successCount = 0;
            failCount = 0;
            currentContract = EmploymentType.None;
            contractDurationLeft = 0;
            hasFreeEquipmentClause = false;
            agreedSharePercent = 20;
            loyalty = 100;
            injury = 0;
            restDaysLeft = 0;
            idleDays = 0;
            missionRequests = 0;
            hasTemporaryPotential = false;
            hasPermanentPotential = false;
            permanentEnchantmentDesc = "";
        }
    }
}

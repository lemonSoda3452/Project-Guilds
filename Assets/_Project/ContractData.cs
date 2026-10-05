using System.Collections.Generic;
using UnityEngine;

namespace HeroManagement
{
    public enum Region { Unknown, Millston, Ironcrest, NorthernWastes, Aethelgard }
    public enum TravelDifficulty { Easy, Moderate, Hard, Suicidal }
    public enum RequiredSkill { None, Combat, Mind, Tracking, Diplomacy }
    public enum MissionType { Combat, Escort, Delivery, Investigation, Gathering }

    // Where a contract comes from: ravens at dawn (kingdoms, orders, nobles) or the board during the day (townsfolk)
    public enum ContractSource { Raven, Board }
    public enum ClientType { Commoner, Merchant, Noble, Kingdom, Order }

    [CreateAssetMenu(fileName = "New Contract", menuName = "Hero Management/Contract Data")]
    public class ContractData : ScriptableObject
    {
        [System.Serializable]
        public class ItemReward
        {
            public ItemData item;
            [Min(1)] public int amount = 1;
            [Range(0, 100)] public int chancePercent = 100;
        }

        [Header("Contract Info")]
        public string clientName;
        [TextArea(3, 5)]
        public string contractDescription;
        public MissionType missionType;
        public ContractSource source = ContractSource.Board;
        public ClientType clientType = ClientType.Commoner;

        [Header("Availability on the Board")]
        [Tooltip("The contract can only show up from this day on (unlocks harder missions over time)")]
        [Min(1)] public int availableFromDay = 1;
        [Tooltip("Days it stays posted before it expires. Ravens: about 3, townsfolk: about 2")]
        [Min(1)] public int postedDurationDays = 2;
        [Tooltip("Higher = shows up more often")]
        [Min(1)] public int spawnWeight = 1;

        [Header("Location & Map Logistics")]
        [Tooltip("The kingdom or faction that issues the contract (Used by the Raven)")]
        public string kingdomName;
        public Region targetRegion;
        public TravelDifficulty travelDifficulty;
        [Tooltip("One-way trip for an average party. Mounts and fast heroes make it shorter.")]
        public int estimatedDays = 1;

        [Header("Rewards")]
        public int baseRewardGold;
        public int maxAllowedSlots = 3;
        [Tooltip("Items the party may bring back when the mission succeeds (they add weight to the way home)")]
        public List<ItemReward> rewardItems = new List<ItemReward>();

        [Header("MANDATORY Requirements (at least one hero in the party must match each)")]
        public bool requiresClass = false;
        public HeroClass requiredClass;
        public bool requiresRace = false;
        public HeroRace requiredRace;
        [Min(0)] public int requiredMinLevel = 0;

        [Header("Soft Preferences (the player is free to ignore them)")]
        [Tooltip("Specific skill that increases the success rate")]
        public RequiredSkill primarySkillRequired;
        public bool hasPreferredClass = false;
        public HeroClass preferredClass;
        [Tooltip("Total party power the mission expects. 0 = ignored.")]
        public int recommendedPower = 20;
        [Tooltip("Stats the PARTY (sum of all members) should reach. Shown as the yellow outline in the radar chart.")]
        public HeroStats recommendedPartyStats;

        [Header("Tower Raid")]
        public bool isTowerRaid = false;
        [Range(1, 100)] public int towerFloors = 10;
        [Tooltip("First floor of this attempt (1 = ground floor). The contract appears once the floors before it are cleared.")]
        [Min(1)] public int towerStartFloor = 1;
        [Tooltip("Minimum gear rating a hero should have. Heroes promised free equipment lose loyalty if the guild sends them below it.")]
        public int minGearRating = 0;

        [Header("Current State (runtime)")]
        public bool isAccepted = false;
        public bool isCompleted = false;
        public bool isPosted = false;
        public int postedUntilDay = 0;

        // Travel difficulty plus the Tower penalty (deeper floors = harder)
        public int GetDifficultyPenalty()
        {
            int penalty;
            switch (travelDifficulty)
            {
                case TravelDifficulty.Easy: penalty = 0; break;
                case TravelDifficulty.Moderate: penalty = -10; break;
                case TravelDifficulty.Hard: penalty = -25; break;
                case TravelDifficulty.Suicidal: penalty = -45; break;
                default: penalty = 0; break;
            }

            if (isTowerRaid) penalty -= GetTowerEndFloor() / 5; // the deeper, the harder
            return penalty;
        }

        public int GetTowerEndFloor() { return Mathf.Min(100, towerStartFloor + towerFloors - 1); }

        public bool HasMandatoryRequirements
        {
            get { return requiresClass || requiresRace || requiredMinLevel > 0; }
        }

        private static bool AnyHero(IList<HeroData> party, System.Func<HeroData, bool> test)
        {
            if (party == null) return false;
            foreach (var h in party)
            {
                if (h != null && test(h)) return true;
            }
            return false;
        }

        // True if the party satisfies every mandatory requirement. 'missing' explains what is lacking.
        public bool PartyMeetsRequirements(IList<HeroData> party, out string missing)
        {
            var list = new List<string>();

            if (requiresClass && !AnyHero(party, h => h.heroClass == requiredClass)) list.Add("a " + requiredClass);
            if (requiresRace && !AnyHero(party, h => h.heroRace == requiredRace)) list.Add("a " + requiredRace);
            if (requiredMinLevel > 0 && !AnyHero(party, h => h.level >= requiredMinLevel)) list.Add("a hero of level " + requiredMinLevel + "+");

            missing = list.Count > 0 ? "Requires " + string.Join(", ", list) : "";
            return list.Count == 0;
        }

        // Multi-line text for the UI: [OK] / [MISSING] for each mandatory requirement
        public string GetRequirementsStatus(IList<HeroData> party)
        {
            var lines = new List<string>();

            if (requiresClass)
                lines.Add((AnyHero(party, h => h.heroClass == requiredClass) ? "[OK] " : "[MISSING] ") + "Class: " + requiredClass);
            if (requiresRace)
                lines.Add((AnyHero(party, h => h.heroRace == requiredRace) ? "[OK] " : "[MISSING] ") + "Race: " + requiredRace);
            if (requiredMinLevel > 0)
                lines.Add((AnyHero(party, h => h.level >= requiredMinLevel) ? "[OK] " : "[MISSING] ") + "Level " + requiredMinLevel + "+");
            if (isTowerRaid)
                lines.Add("Tower: floors " + towerStartFloor + "-" + GetTowerEndFloor() + ", gear " + minGearRating + "+");

            if (lines.Count == 0) return "No mandatory requirements.\nAny party can go.";
            return string.Join("\n", lines);
        }

        public void ResetRuntimeState()
        {
            isAccepted = false;
            isCompleted = false;
            isPosted = false;
            postedUntilDay = 0;
        }
    }
}

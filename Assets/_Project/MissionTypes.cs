using System.Collections.Generic;
using UnityEngine;

namespace HeroManagement
{
    public enum ExpeditionPhase { Outbound, Returning }

    [System.Serializable]
    public class LootEntry
    {
        public ItemData item;
        public int amount;

        public float TotalWeight { get { return item != null ? item.weight * amount : 0f; } }
    }

    // How much of the mission reward a hero takes home
    [System.Serializable]
    public class HeroPay
    {
        public HeroData hero;
        public int amount;
    }

    // What happened to one hero during a mission (used by the mission report screen)
    [System.Serializable]
    public class HeroReportEntry
    {
        public HeroData hero;
        public int levelBefore;
        public int levelAfter;
        public int expBefore;
        public int expAfter;
        public int expGained;
        public int injury;
        public bool died;
        public bool gearBroke;
    }

    // Shown to the player when a party comes home (or is lost)
    public class MissionReport
    {
        public ContractData contract;
        public bool success;
        public int rewardGold;
        public int heroShares;
        public int daysAway;
        public bool partyWiped;
        public List<LootEntry> loot = new List<LootEntry>();
        public List<HeroReportEntry> heroes = new List<HeroReportEntry>();
    }

    // Result of a resolved mission (the party is still travelling home when this is created)
    public class MissionOutcome
    {
        public bool success;
        public int rewardGold;
        public List<LootEntry> loot = new List<LootEntry>();
        public List<HeroPay> pays = new List<HeroPay>();
        public List<HeroData> survivors = new List<HeroData>();
        public List<HeroData> casualties = new List<HeroData>();
        public List<HeroData> gearBroken = new List<HeroData>();
        public List<HeroReportEntry> report = new List<HeroReportEntry>();
    }
}

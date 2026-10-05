using UnityEngine;

namespace HeroManagement
{
    // Order matters: it is also the order of the axes in the radar chart
    public enum StatType { Strength, Intelligence, Speed, Vitality, Healing, Charisma }

    [System.Serializable]
    public struct HeroStats
    {
        public int strength;
        public int intelligence;
        public int speed;
        public int vitality;
        public int healing;
        public int charisma;

        public const int Count = 6;
        public static readonly string[] ShortNames = { "STR", "INT", "SPD", "VIT", "HEAL", "CHA" };

        public HeroStats(int strength, int intelligence, int speed, int vitality, int healing, int charisma)
        {
            this.strength = strength;
            this.intelligence = intelligence;
            this.speed = speed;
            this.vitality = vitality;
            this.healing = healing;
            this.charisma = charisma;
        }

        public int Total { get { return strength + intelligence + speed + vitality + healing + charisma; } }

        public int Get(StatType type)
        {
            switch (type)
            {
                case StatType.Strength: return strength;
                case StatType.Intelligence: return intelligence;
                case StatType.Speed: return speed;
                case StatType.Vitality: return vitality;
                case StatType.Healing: return healing;
                case StatType.Charisma: return charisma;
                default: return 0;
            }
        }

        public void Add(StatType type, int amount)
        {
            switch (type)
            {
                case StatType.Strength: strength += amount; break;
                case StatType.Intelligence: intelligence += amount; break;
                case StatType.Speed: speed += amount; break;
                case StatType.Vitality: vitality += amount; break;
                case StatType.Healing: healing += amount; break;
                case StatType.Charisma: charisma += amount; break;
            }
        }

        public float[] ToArray()
        {
            return new float[] { strength, intelligence, speed, vitality, healing, charisma };
        }

        public override string ToString()
        {
            return $"STR {strength}  INT {intelligence}  SPD {speed}  VIT {vitality}  HEAL {healing}  CHA {charisma}";
        }

        public static HeroStats operator +(HeroStats a, HeroStats b)
        {
            return new HeroStats(a.strength + b.strength, a.intelligence + b.intelligence, a.speed + b.speed,
                                 a.vitality + b.vitality, a.healing + b.healing, a.charisma + b.charisma);
        }
    }
}

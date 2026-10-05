using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HeroManagement
{
    public class HeroManager : MonoBehaviour
    {
        public static HeroManager Instance { get; private set; }

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        // Every living hero of the guild (used by the hero sheet, inventory and witch panels)
        public static List<HeroData> GetGuildHeroes()
        {
            if (GameManager.Instance == null || GameManager.Instance.allHeroesDatabase == null)
            {
                return new List<HeroData>();
            }

            return GameManager.Instance.allHeroesDatabase
                .Where(hero => hero != null && !hero.isDead)
                .ToList();
        }

        // Heroes who can leave on a mission right now: alive, at home and fully rested
        public List<HeroData> GetAvailableHeroes()
        {
            if (GameManager.Instance == null || GameManager.Instance.allHeroesDatabase == null)
            {
                return new List<HeroData>();
            }

            return GameManager.Instance.allHeroesDatabase
                .Where(hero => hero != null && !hero.isBusy && !hero.isDead && hero.restDaysLeft <= 0)
                .ToList();
        }

        // Heroes who are home but still recovering from their wounds
        public List<HeroData> GetRestingHeroes()
        {
            if (GameManager.Instance == null || GameManager.Instance.allHeroesDatabase == null)
            {
                return new List<HeroData>();
            }

            return GameManager.Instance.allHeroesDatabase
                .Where(hero => hero != null && !hero.isBusy && !hero.isDead && hero.restDaysLeft > 0)
                .ToList();
        }
    }
}

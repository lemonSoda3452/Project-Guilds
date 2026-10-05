using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace HeroManagement
{
    public class TavernUIManager : MonoBehaviour
    {
        [Header("Roster Display")]
        [Tooltip("Text that shows the heroes at the tavern")]
        public Text rosterText;

        void OnEnable()
        {
            GameManager.OnPhaseChanged += UpdateRoster;
            MissionDispatcher.OnExpeditionsChanged += RefreshRosterUI;
        }

        void OnDisable()
        {
            GameManager.OnPhaseChanged -= UpdateRoster;
            MissionDispatcher.OnExpeditionsChanged -= RefreshRosterUI;
        }

        void Start()
        {
            if (rosterText != null) rosterText.supportRichText = true;
            RefreshRosterUI();
        }

        public void RefreshRosterUI()
        {
            if (GameManager.Instance != null) UpdateRoster(GameManager.Instance.currentPhase);
        }

        // Contract length in the right unit: missions, days or permanent
        private string GetContractInfo(HeroData hero)
        {
            switch (hero.currentContract)
            {
                case EmploymentType.Permanent: return "Permanente";
                case EmploymentType.MissionBased: return $"{hero.contractDurationLeft} missioni";
                case EmploymentType.TimeBased: return $"{hero.contractDurationLeft} giorni";
                default: return "Nessuno";
            }
        }

        private string LoyaltyColor(int loyalty)
        {
            if (loyalty < 30) return "#FF6666";
            if (loyalty < 60) return "#FFAA44";
            return "#DDDDDD";
        }

        private void UpdateRoster(GameManager.DayPhase phase)
        {
            if (rosterText == null) return;

            if (HeroManager.Instance == null)
            {
                rosterText.text = "HeroManager non trovato.";
                return;
            }

            List<HeroData> available = HeroManager.Instance.GetAvailableHeroes();
            List<HeroData> resting = HeroManager.Instance.GetRestingHeroes();

            if (available.Count == 0 && resting.Count == 0)
            {
                rosterText.text = "La Taverna è vuota. Non hai eroi o sono tutti in missione!";
                return;
            }

            string text = "<b>EROI DISPONIBILI:</b>\n";
            foreach (var hero in available)
            {
                bool isRestless = EmploymentManager.Instance != null && hero.idleDays >= EmploymentManager.Instance.idleDaysBeforeRestless;
                string idleWarning = isRestless ? "<color=orange>[Irrequieto!]</color> " : "";

                text += $"- {idleWarning}<b>{hero.heroName}</b> (Lv.{hero.level} {hero.heroClass})\n";
                text += $"  <size=16><color={LoyaltyColor(hero.loyalty)}>Lealtà: {hero.loyalty}%</color> <color=#DDDDDD>| Power: {hero.GetPower()} | Contratto: {GetContractInfo(hero)}</color></size>\n";
            }

            if (resting.Count > 0)
            {
                text += "\n<b>IN INFERMERIA:</b>\n";
                foreach (var hero in resting)
                {
                    text += $"- <color=#FF6666>{hero.heroName}</color>: {hero.restDaysLeft} giorni di riposo (Ferite {hero.injury}%)\n";
                }
            }

            rosterText.text = text;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HeroManagement
{
    // Hero sheet: everything about one hero of the guild, with Prev/Next, contract renewal and dismissal
    public class HeroSheetUIManager : MonoBehaviour
    {
        [Header("UI")]
        public GameObject panel;
        public Text infoText;
        public Text equipText;
        public RadarChart statChart;
        public float chartMax = 20f;

        private int heroIndex = 0;

        void OnEnable()
        {
            GameManager.OnGoldChanged += HandleGold;
            GameManager.OnPhaseChanged += HandlePhase;
            MissionDispatcher.OnExpeditionsChanged += Refresh;
        }

        void OnDisable()
        {
            GameManager.OnGoldChanged -= HandleGold;
            GameManager.OnPhaseChanged -= HandlePhase;
            MissionDispatcher.OnExpeditionsChanged -= Refresh;
        }

        private void HandleGold(int gold) { Refresh(); }
        private void HandlePhase(GameManager.DayPhase phase) { Refresh(); }

        private void Log(string message)
        {
            if (SystemLogUIManager.Instance != null) SystemLogUIManager.Instance.LogMessage(message);
        }

        private HeroData Current()
        {
            var list = HeroManager.GetGuildHeroes();
            if (list.Count == 0) return null;

            heroIndex = Mathf.Clamp(heroIndex, 0, list.Count - 1);
            return list[heroIndex];
        }

        private string ContractText(HeroData hero)
        {
            switch (hero.currentContract)
            {
                case EmploymentType.Permanent: return "Permanente";
                case EmploymentType.MissionBased: return $"{hero.contractDurationLeft} missioni rimaste";
                case EmploymentType.TimeBased: return $"{hero.contractDurationLeft} giorni rimasti";
                default: return "Nessuno";
            }
        }

        private int RenewalDuration(HeroData hero)
        {
            return hero.currentContract == EmploymentType.TimeBased ? 30 : 3;
        }

        public void Refresh()
        {
            if (panel == null || !panel.activeInHierarchy) return;

            var hero = Current();

            if (hero == null)
            {
                if (infoText != null) infoText.text = "Nessun eroe in gilda.\nAssumine uno in taverna!";
                if (equipText != null) equipText.text = "";
                if (statChart != null) statChart.SetValues(new float[HeroStats.Count], chartMax);
                return;
            }

            if (statChart != null) statChart.SetValues(hero.GetStats().ToArray(), chartMax);

            if (infoText != null)
            {
                string status = hero.isBusy
                    ? "<color=#FFA500>In missione</color>"
                    : (hero.restDaysLeft > 0 ? $"<color=#FF6666>In infermeria ({hero.restDaysLeft} gg)</color>" : "<color=#88FF88>Disponibile</color>");

                int expNeeded = HeroProgressionManager.Instance != null ? HeroProgressionManager.Instance.GetExpNeededForLevel(hero.level) : 0;

                string text = $"<b>{hero.heroName}</b>  Lv.{hero.level} {hero.heroRace} {hero.heroClass}\n";
                text += $"<color=#CCCCCC>{hero.personality} | Fama {hero.fameTier} | Età {hero.age} | Cavalcatura {hero.mount}</color>\n";
                text += $"EXP {hero.currentExp}/{expNeeded}\n\n";
                text += $"Stato: {status}\n";
                text += $"Lealtà: {hero.loyalty}%   Ferite: {hero.injury}%\n";
                text += $"Missioni: {hero.successCount} vinte / {hero.failCount} perse\n\n";
                text += $"<b>CONTRATTO</b>\n";
                text += $"{hero.currentContract}: {ContractText(hero)}\n";
                text += $"Quota sul bottino: {hero.agreedSharePercent}% (si aspetta ~{hero.GetExpectedPayPerMission()} oro a missione)\n";
                text += $"Mantenimento: {hero.dailyUpkeepCost} oro/giorno\n";
                text += $"Equipaggiamento a spese della gilda: {(hero.hasFreeEquipmentClause ? "SI" : "NO")}\n";

                if ((hero.currentContract == EmploymentType.MissionBased || hero.currentContract == EmploymentType.TimeBased) && EmploymentManager.Instance != null)
                {
                    int cost = EmploymentManager.Instance.GetRenewalCost(hero, hero.currentContract, RenewalDuration(hero), hero.hasFreeEquipmentClause);
                    text += $"\nRinnovo ({RenewalDuration(hero)} {(hero.currentContract == EmploymentType.TimeBased ? "giorni" : "missioni")}): <color=#FFD700>{cost} oro</color>";
                }

                infoText.text = text;
            }

            if (equipText != null)
            {
                string e = "<b>EQUIPAGGIAMENTO</b>\n";
                e += hero.equippedWeapon != null
                    ? $"Arma: {hero.equippedWeapon.GetDisplayName()} ({hero.weaponDurability}%)\n"
                    : "Arma: -\n";
                e += hero.equippedArmor != null
                    ? $"Armatura: {hero.equippedArmor.GetDisplayName()} ({hero.armorDurability}%)\n"
                    : "Armatura: -\n";
                e += $"Gear rating {hero.GetGearRating()} | peso {hero.GetGearWeight():0.#}/{hero.GetCarryCapacity():0} kg\n\n";

                e += "<b>ABILITÀ</b>\n";
                hero.NormalizeSkillSlots();
                for (int i = 0; i < HeroData.MaxSkillSlots; i++)
                {
                    var skill = hero.equippedSkills[i];
                    e += $"{i + 1}. {(skill != null ? skill.skillName : "<color=#777777>vuoto</color>")}\n";
                }

                if (hero.hasPermanentPotential) e += $"\n<color=#CC88FF>{hero.permanentEnchantmentDesc}</color>";
                else if (hero.hasTemporaryPotential) e += "\n<color=#88CCFF>Potenziale temporaneo attivo</color>";

                equipText.text = e;
            }
        }

        // ---- Buttons
        public void OnPrevHeroClicked()
        {
            var list = HeroManager.GetGuildHeroes();
            if (list.Count == 0) return;

            heroIndex = (heroIndex - 1 + list.Count) % list.Count;
            Refresh();
        }

        public void OnNextHeroClicked()
        {
            var list = HeroManager.GetGuildHeroes();
            if (list.Count == 0) return;

            heroIndex = (heroIndex + 1) % list.Count;
            Refresh();
        }

        public void OnRenewClicked()
        {
            var hero = Current();
            if (hero == null || EmploymentManager.Instance == null) return;

            if (hero.currentContract == EmploymentType.None || hero.currentContract == EmploymentType.Permanent)
            {
                Log($"{hero.heroName} has nothing to renew.");
                return;
            }

            EmploymentManager.Instance.TryRenewContract(hero, hero.currentContract, RenewalDuration(hero), hero.hasFreeEquipmentClause);
            Refresh();
        }

        public void OnDismissClicked()
        {
            var hero = Current();
            if (hero == null || EmploymentManager.Instance == null) return;

            if (!EmploymentManager.Instance.TryRelease(hero, $"{hero.heroName} was dismissed and returns to the tavern."))
            {
                Log($"{hero.heroName} cannot be dismissed while on a mission.");
            }

            Refresh();
        }
    }
}

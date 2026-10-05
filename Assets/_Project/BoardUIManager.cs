using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HeroManagement
{
    // The board: browse the contracts posted by the ContractBoardManager, build a party, send it
    public class BoardUIManager : MonoBehaviour
    {
        [Header("Board UI Elements")]
        public GameObject bachecaPanel;
        public Text clientNameText;
        public Text descriptionText;
        public Text rewardText;

        [Header("Hero Selection UI Elements")]
        public Text selectedHeroText;
        public Text currentPartyText;

        [Header("Dispatch Info")]
        public Text requirementsText;
        public Text travelInfoText;
        public Button acceptButton;

        [Header("Stat Charts (radar)")]
        [Tooltip("Stats of the hero currently selected")]
        public RadarChart heroChart;
        [Tooltip("Sum of the stats of the whole party")]
        public RadarChart partyChart;
        [Tooltip("The stats the contract recommends (outline)")]
        public RadarChart requirementChart;
        public float heroChartMax = 20f;
        public float partyChartMax = 50f;

        [Header("3D scene")]
        [Tooltip("ON when a 3D controller shows/hides the board (the phase rules below are ignored)")]
        public bool externalVisibility = false;

        [Header("Board is open during")]
        public bool showInMorning = true;
        public bool showInDay = true;
        public bool showInAfternoon = true;
        public bool showInEvening = false;

        private static readonly List<ContractData> NoContracts = new List<ContractData>();

        private ContractData currentDisplayedContract;
        private int contractIndex = 0;

        private List<HeroData> availableHeroes = new List<HeroData>();
        private List<HeroData> partyToDispatch = new List<HeroData>();
        private int currentHeroIndex = 0;

        private List<ContractData> Posted
        {
            get { return ContractBoardManager.Instance != null ? ContractBoardManager.Instance.postedContracts : NoContracts; }
        }

        void OnEnable()
        {
            GameManager.OnPhaseChanged += HandlePhaseChange;
            MissionDispatcher.OnExpeditionsChanged += HandleExpeditionsChanged;
            ContractBoardManager.OnBoardChanged += HandleBoardChanged;
        }

        void OnDisable()
        {
            GameManager.OnPhaseChanged -= HandlePhaseChange;
            MissionDispatcher.OnExpeditionsChanged -= HandleExpeditionsChanged;
            ContractBoardManager.OnBoardChanged -= HandleBoardChanged;
        }

        void Start()
        {
            var phase = GameManager.Instance != null ? GameManager.Instance.currentPhase : GameManager.DayPhase.Morning;
            ApplyPhaseVisibility(phase);
        }

        private bool IsOpenIn(GameManager.DayPhase phase)
        {
            switch (phase)
            {
                case GameManager.DayPhase.Morning: return showInMorning;
                case GameManager.DayPhase.Day: return showInDay;
                case GameManager.DayPhase.Afternoon: return showInAfternoon;
                default: return showInEvening;
            }
        }

        private void ApplyPhaseVisibility(GameManager.DayPhase phase)
        {
            if (bachecaPanel == null) return;

            bool open = IsOpenIn(phase);
            if (!externalVisibility) bachecaPanel.SetActive(open);
            if (open || externalVisibility) RefreshBoard(false);
        }

        private void HandlePhaseChange(GameManager.DayPhase newPhase) { ApplyPhaseVisibility(newPhase); }

        private void HandleBoardChanged()
        {
            if (bachecaPanel != null && bachecaPanel.activeSelf) RefreshBoard(false);
        }

        // Heroes leave, come back or finish resting: keep the candidates fresh
        private void HandleExpeditionsChanged()
        {
            if (bachecaPanel != null && bachecaPanel.activeSelf && currentDisplayedContract != null) RefreshAvailableHeroes(false);
        }

        // Called by the 3D controller when the player walks up to the board
        public void RefreshFromOutside() { RefreshBoard(false); }

        // ------------------------------------------------------------ contracts

        private void RefreshBoard(bool forceReselect)
        {
            var posted = Posted;

            if (posted.Count == 0)
            {
                ShowEmpty();
                return;
            }

            int existing = currentDisplayedContract != null ? posted.IndexOf(currentDisplayedContract) : -1;

            if (existing >= 0 && !forceReselect)
            {
                contractIndex = existing;
                RefreshContractTexts();
                RefreshAvailableHeroes(false);
                return;
            }

            contractIndex = Mathf.Clamp(contractIndex, 0, posted.Count - 1);
            SelectContract(posted[contractIndex]);
        }

        private void SelectContract(ContractData contract)
        {
            currentDisplayedContract = contract;
            partyToDispatch.Clear();

            RefreshContractTexts();
            RefreshAvailableHeroes(true);
        }

        private void ShowEmpty()
        {
            currentDisplayedContract = null;
            contractIndex = 0;
            partyToDispatch.Clear();
            availableHeroes.Clear();

            if (clientNameText != null) clientNameText.text = "Nessun contratto sulla bacheca.";
            if (descriptionText != null) descriptionText.text = "I corvi arrivano la mattina, i popolani durante la giornata.";
            if (rewardText != null) rewardText.text = "";
            if (selectedHeroText != null) selectedHeroText.text = "";
            if (currentPartyText != null) currentPartyText.text = "";
            if (requirementsText != null) requirementsText.text = "";
            if (travelInfoText != null) travelInfoText.text = "";
            if (acceptButton != null) acceptButton.interactable = false;

            UpdateCharts();
        }

        private void RefreshContractTexts()
        {
            var c = currentDisplayedContract;
            if (c == null) return;

            string source = c.source == ContractSource.Raven
                ? "<color=#9AD0FF>[CORVO]</color>"
                : "<color=#FFD27F>[BACHECA]</color>";

            if (clientNameText != null)
                clientNameText.text = $"({contractIndex + 1}/{Posted.Count}) {source} {c.clientName}";

            if (descriptionText != null)
            {
                int daysLeft = ContractBoardManager.Instance != null ? ContractBoardManager.Instance.GetDaysLeft(c) : 0;
                string from = (c.source == ContractSource.Raven && !string.IsNullOrEmpty(c.kingdomName))
                    ? $"Mittente: {c.kingdomName} ({c.clientType})"
                    : $"Cliente: {c.clientType}";

                descriptionText.text = $"{c.contractDescription}\n<color=#AAAAAA>{from} | Scade tra {daysLeft} giorno/i</color>";
            }

            if (rewardText != null)
            {
                rewardText.text = $"Ricompensa: {c.baseRewardGold} Oro (arriva la mattina dopo la missione) | Max {c.maxAllowedSlots} Eroi";
            }
        }

        // ------------------------------------------------------------ heroes

        private void RefreshAvailableHeroes(bool resetIndex)
        {
            availableHeroes.Clear();

            if (HeroManager.Instance != null)
            {
                foreach (var hero in HeroManager.Instance.GetAvailableHeroes())
                {
                    if (hero != null && !partyToDispatch.Contains(hero)) availableHeroes.Add(hero);
                }
            }

            // Heroes already in the party who left (mercenary initiative) or got unavailable
            partyToDispatch.RemoveAll(h => h == null || h.isBusy || h.isDead || h.restDaysLeft > 0);

            if (resetIndex || availableHeroes.Count == 0) currentHeroIndex = 0;
            else if (currentHeroIndex >= availableHeroes.Count) currentHeroIndex = availableHeroes.Count - 1;

            UpdateAllDisplays();
        }

        // ------------------------------------------------------------ displays

        private void UpdateAllDisplays()
        {
            UpdateSelectedHeroDisplay();
            UpdatePartyDisplay();
            UpdateDispatchInfo();
            UpdateCharts();
        }

        private void UpdateSelectedHeroDisplay()
        {
            if (selectedHeroText == null) return;

            if (availableHeroes.Count == 0)
            {
                selectedHeroText.text = "Nessun eroe disponibile!";
                return;
            }

            var hero = availableHeroes[currentHeroIndex];
            selectedHeroText.text =
                $"In lizza: {hero.GetFormattedName()}\n{hero.GetStats()}\n" +
                $"Power {hero.GetPower()} | Carry {hero.GetCarryCapacity():0} kg | Gear {hero.GetGearRating()} | Mount {hero.mount} | Speed x{hero.GetTravelSpeedMultiplier():0.00}";
        }

        private void UpdatePartyDisplay()
        {
            if (currentPartyText == null) return;

            if (partyToDispatch.Count == 0)
            {
                currentPartyText.text = "Squadra: Vuota";
                return;
            }

            string text = "Squadra: ";
            int power = 0;
            foreach (var h in partyToDispatch)
            {
                if (h == null) continue;
                text += h.heroName + ", ";
                power += h.GetPower();
            }

            currentPartyText.text = text.TrimEnd(',', ' ') + $"\nPotenza totale: {power}" +
                                    (currentDisplayedContract != null && currentDisplayedContract.recommendedPower > 0
                                        ? $" / consigliata {currentDisplayedContract.recommendedPower}" : "");
        }

        private void UpdateDispatchInfo()
        {
            if (currentDisplayedContract == null) return;

            if (requirementsText != null)
                requirementsText.text = currentDisplayedContract.GetRequirementsStatus(partyToDispatch);

            if (travelInfoText != null)
            {
                if (partyToDispatch.Count == 0 || MissionDispatcher.Instance == null)
                {
                    travelInfoText.text = $"Viaggio base: {currentDisplayedContract.estimatedDays} giorno/i\nAggiungi eroi per vedere il tempo reale.";
                }
                else
                {
                    var d = MissionDispatcher.Instance;
                    int days = d.EstimateOutboundDays(currentDisplayedContract, partyToDispatch);
                    float speed = d.GetPartySpeed(partyToDispatch);
                    float free = d.GetPartyFreeCapacity(partyToDispatch);
                    travelInfoText.text = $"Andata: ~{days} giorno/i\nVelocita': x{speed:0.00}\nPeso trasportabile: {free:0} kg";
                }
            }

            if (acceptButton != null)
            {
                string missing;
                acceptButton.interactable = partyToDispatch.Count > 0 &&
                                            currentDisplayedContract.PartyMeetsRequirements(partyToDispatch, out missing);
            }
        }

        private void UpdateCharts()
        {
            float[] zeros = new float[HeroStats.Count];

            if (heroChart != null)
            {
                heroChart.SetValues(availableHeroes.Count > 0 ? availableHeroes[currentHeroIndex].GetStats().ToArray() : zeros, heroChartMax);
            }

            HeroStats partySum = new HeroStats();
            foreach (var h in partyToDispatch)
            {
                if (h != null) partySum = partySum + h.GetStats();
            }

            if (partyChart != null) partyChart.SetValues(partySum.ToArray(), partyChartMax);

            if (requirementChart != null)
            {
                requirementChart.SetValues(
                    currentDisplayedContract != null ? currentDisplayedContract.recommendedPartyStats.ToArray() : zeros,
                    partyChartMax);
            }
        }

        // ------------------------------------------------------------ buttons

        public void OnNextContractClicked()
        {
            var posted = Posted;
            if (posted.Count <= 1) return;

            contractIndex = (contractIndex + 1) % posted.Count;
            SelectContract(posted[contractIndex]);
        }

        public void OnPrevContractClicked()
        {
            var posted = Posted;
            if (posted.Count <= 1) return;

            contractIndex = (contractIndex - 1 + posted.Count) % posted.Count;
            SelectContract(posted[contractIndex]);
        }

        public void OnNextHeroClicked()
        {
            if (availableHeroes.Count <= 1) return;

            currentHeroIndex++;
            if (currentHeroIndex >= availableHeroes.Count) currentHeroIndex = 0;

            UpdateSelectedHeroDisplay();
            UpdateCharts();
        }

        public void OnAddHeroToPartyClicked()
        {
            if (availableHeroes.Count == 0 || currentDisplayedContract == null) return;

            if (partyToDispatch.Count >= currentDisplayedContract.maxAllowedSlots)
            {
                if (SystemLogUIManager.Instance != null)
                    SystemLogUIManager.Instance.LogMessage("Warning: Maximum hero slots reached for this contract!");
                return;
            }

            HeroData selected = availableHeroes[currentHeroIndex];
            if (selected != null && !partyToDispatch.Contains(selected))
            {
                partyToDispatch.Add(selected);
                RefreshAvailableHeroes(false);
            }
        }

        public void OnClearPartyClicked()
        {
            partyToDispatch.Clear();
            RefreshAvailableHeroes(true);
        }

        // After a departure the contract leaves the board by itself and the next one is shown
        public void OnAcceptContractClicked()
        {
            if (currentDisplayedContract == null || partyToDispatch.Count == 0 || MissionDispatcher.Instance == null) return;

            MissionDispatcher.Instance.DispatchPartyToContract(currentDisplayedContract, new List<HeroData>(partyToDispatch));
        }
    }
}

using UnityEngine;

namespace HeroManagement
{
    // TEMPORARY: lets you test the whole game loop with keyboard keys, before any UI exists.
    //   SPACE = advance phase      H = hire first tavern visitor (3 missions)
    //   A     = auto-send a party  G = +gold cheat
    //   R     = refresh tavern     I = print status in the log
    public class DebugInputManager : MonoBehaviour
    {
        public bool enableDebugKeys = true;
        public int goldCheatAmount = 500;

        void Update()
        {
            if (!enableDebugKeys) return;

            if (Pressed(KeyCode.Space)) AdvancePhase();
            if (Pressed(KeyCode.H)) HireFirstVisitor();
            if (Pressed(KeyCode.A)) AutoDispatch();
            if (Pressed(KeyCode.G)) GiveGold();
            if (Pressed(KeyCode.R)) RefreshTavern();
            if (Pressed(KeyCode.I)) PrintStatus();
        }

        private bool Pressed(KeyCode key)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key);
#elif ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return false;

            switch (key)
            {
                case KeyCode.Space: return kb.spaceKey.wasPressedThisFrame;
                case KeyCode.H: return kb.hKey.wasPressedThisFrame;
                case KeyCode.A: return kb.aKey.wasPressedThisFrame;
                case KeyCode.G: return kb.gKey.wasPressedThisFrame;
                case KeyCode.R: return kb.rKey.wasPressedThisFrame;
                case KeyCode.I: return kb.iKey.wasPressedThisFrame;
                default: return false;
            }
#else
            return false;
#endif
        }

        private void Log(string message)
        {
            Debug.Log("[DEBUG] " + message);
            if (SystemLogUIManager.Instance != null) SystemLogUIManager.Instance.LogMessage("[DEBUG] " + message);
        }

        private void AdvancePhase()
        {
            if (GameManager.Instance != null) GameManager.Instance.AdvancePhase();
        }

        private void HireFirstVisitor()
        {
            if (RecruitmentManager.Instance == null) return;

            var visitors = RecruitmentManager.Instance.currentTavernVisitors;
            if (visitors.Count == 0) { Log("No visitors in the tavern."); return; }

            RecruitmentManager.Instance.HireHero(visitors[0], EmploymentType.MissionBased, 3, false);
        }

        private int RequirementScore(ContractData contract, HeroData hero)
        {
            int score = 0;
            if (contract.requiresClass && hero.heroClass == contract.requiredClass) score += 2;
            if (contract.requiresRace && hero.heroRace == contract.requiredRace) score += 2;
            if (contract.requiredMinLevel > 0 && hero.level >= contract.requiredMinLevel) score += 1;
            return score;
        }

        // Tries every free contract and sends the first party that satisfies its mandatory requirements
        private void AutoDispatch()
        {
            if (GameManager.Instance == null || MissionDispatcher.Instance == null || HeroManager.Instance == null) return;

            var available = HeroManager.Instance.GetAvailableHeroes();
            if (available.Count == 0) { Log("No hero available (busy or resting)."); return; }

            var contracts = ContractBoardManager.Instance != null
                ? new System.Collections.Generic.List<ContractData>(ContractBoardManager.Instance.postedContracts)
                : GameManager.Instance.allContractsDatabase;
            if (contracts != null)
            {
                foreach (var contract in contracts)
                {
                    if (contract == null || contract.isAccepted || contract.isCompleted) continue;

                    var ordered = new System.Collections.Generic.List<HeroData>(available);
                    ordered.Sort((x, y) => RequirementScore(contract, y).CompareTo(RequirementScore(contract, x)));

                    var party = new System.Collections.Generic.List<HeroData>();
                    foreach (var hero in ordered)
                    {
                        if (party.Count >= contract.maxAllowedSlots) break;
                        party.Add(hero);
                    }

                    string missing;
                    if (!contract.PartyMeetsRequirements(party, out missing)) continue;

                    if (MissionDispatcher.Instance.DispatchPartyToContract(contract, party)) return;
                }
            }

            Log("No contract can be started with the heroes available right now.");
        }

        private void GiveGold()
        {
            if (GameManager.Instance != null) GameManager.Instance.AddGold(goldCheatAmount);
        }

        private void RefreshTavern()
        {
            if (RecruitmentManager.Instance != null)
                RecruitmentManager.Instance.RefreshTavernVisitors(RecruitmentManager.Instance.visitorsPerDay);
        }

        private void PrintStatus()
        {
            if (GameManager.Instance == null) return;

            var gm = GameManager.Instance;
            int living = 0, busy = 0;
            if (gm.allHeroesDatabase != null)
            {
                foreach (var h in gm.allHeroesDatabase)
                {
                    if (h == null || h.isDead) continue;
                    living++;
                    if (h.isBusy) busy++;
                }
            }

            int expeditions = MissionDispatcher.Instance != null ? MissionDispatcher.Instance.ongoingExpeditions.Count : 0;
            Log($"Day {gm.currentDay} ({gm.currentPhase}) | Gold {gm.playerGold} | Heroes {living} ({busy} busy) | Expeditions {expeditions}");
        }
    }
}

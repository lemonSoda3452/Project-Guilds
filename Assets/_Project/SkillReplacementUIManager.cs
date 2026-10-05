using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HeroManagement
{
    public class SkillReplacementUIManager : MonoBehaviour
    {
        public static SkillReplacementUIManager Instance { get; private set; }

        [Header("UI Panels")]
        public GameObject replacementPanel;

        [Header("Text References")]
        public Text promptText; // e.g. "Grog wants to learn Fireball, but already knows 4 moves!"
        public Text newSkillNameText;
        public Text newSkillDescText;

        [Header("Skill Slots (the 4 buttons to forget a skill)")]
        public Button[] slotButtons = new Button[HeroData.MaxSkillSlots];
        public Text[] slotNameTexts = new Text[HeroData.MaxSkillSlots];

        [Header("Options")]
        [Tooltip("Freezes Time.timeScale while the panel is open. Not needed for a button-driven game.")]
        public bool pauseGameWhileOpen = false;

        private struct LearnRequest
        {
            public HeroData hero;
            public SkillData skill;
            public System.Action<bool> onFinished;
        }

        private readonly Queue<LearnRequest> pendingRequests = new Queue<LearnRequest>();

        private HeroData currentHero;
        private SkillData pendingSkill;
        private System.Action<bool> currentCallback;
        private bool pausedByThisPanel = false;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            if (replacementPanel != null) replacementPanel.SetActive(false);
        }

        // Never leave the game frozen if this object is disabled or destroyed while the panel is open
        void OnDisable() { RestoreTimeScale(); }
        void OnDestroy() { RestoreTimeScale(); }

        private void Log(string message)
        {
            if (SystemLogUIManager.Instance != null) SystemLogUIManager.Instance.LogMessage(message);
        }

        // Main entry point. Returns true if the skill was learned or the panel was opened/queued.
        // onFinished (optional) is called with true if the skill was really learned, false otherwise.
        public bool TryLearnNewSkill(HeroData hero, SkillData newSkill, System.Action<bool> onFinished = null)
        {
            if (hero == null || newSkill == null || hero.isDead)
            {
                onFinished?.Invoke(false);
                return false;
            }

            // A request is already on screen: queue this one instead of overwriting it
            if (currentHero != null)
            {
                pendingRequests.Enqueue(new LearnRequest { hero = hero, skill = newSkill, onFinished = onFinished });
                return true;
            }

            return HandleRequest(hero, newSkill, onFinished);
        }

        private bool HandleRequest(HeroData hero, SkillData newSkill, System.Action<bool> onFinished)
        {
            // 0. Already known: nothing to learn
            if (hero.KnowsSkill(newSkill))
            {
                Log($"{hero.heroName} already knows {newSkill.skillName}.");
                onFinished?.Invoke(false);
                return false;
            }

            // 1. Free slot: learn automatically, no UI
            int freeSlot = hero.GetFirstEmptySkillSlot();
            if (freeSlot >= 0)
            {
                hero.LearnSkill(newSkill, freeSlot);
                Log($"{hero.heroName} ha imparato {newSkill.skillName}!");
                onFinished?.Invoke(true);
                return true;
            }

            // 2. All slots full: open the replacement UI
            if (replacementPanel == null)
            {
                Debug.LogWarning("[SKILL UI] replacementPanel is not assigned: cannot ask the player which skill to forget.");
                onFinished?.Invoke(false);
                return false;
            }

            currentHero = hero;
            pendingSkill = newSkill;
            currentCallback = onFinished;

            PopulateUI();
            replacementPanel.SetActive(true);

            if (pauseGameWhileOpen)
            {
                Time.timeScale = 0f;
                pausedByThisPanel = true;
            }

            return true;
        }

        private void PopulateUI()
        {
            if (promptText != null)
                promptText.text = $"{currentHero.heroName} vuole imparare {pendingSkill.skillName}, ma conosce già {HeroData.MaxSkillSlots} abilità. Quale vuoi dimenticare?";

            if (newSkillNameText != null) newSkillNameText.text = pendingSkill.skillName;
            if (newSkillDescText != null) newSkillDescText.text = pendingSkill.description;

            for (int i = 0; i < HeroData.MaxSkillSlots; i++)
            {
                // Arrays set in the Inspector might be shorter than 4
                if (slotButtons == null || i >= slotButtons.Length || slotButtons[i] == null) continue;

                SkillData oldSkill = currentHero.equippedSkills[i];

                if (slotNameTexts != null && i < slotNameTexts.Length && slotNameTexts[i] != null)
                {
                    slotNameTexts[i].text = oldSkill != null ? oldSkill.skillName : "Slot Vuoto";
                }

                int slotIndex = i; // capture for the closure
                slotButtons[i].onClick.RemoveAllListeners();
                slotButtons[i].onClick.AddListener(() => OnForgetSkillClicked(slotIndex));
            }
        }

        private void OnForgetSkillClicked(int slotIndex)
        {
            if (currentHero == null || pendingSkill == null) return;

            SkillData forgottenSkill = currentHero.equippedSkills[slotIndex];
            bool learned = currentHero.LearnSkill(pendingSkill, slotIndex);

            if (learned)
            {
                string forgottenName = forgottenSkill != null ? forgottenSkill.skillName : "una vecchia abilità";
                Log($"{currentHero.heroName} ha dimenticato {forgottenName} per imparare {pendingSkill.skillName}!");
            }

            ClosePanel(learned);
        }

        // Hooked to a "Don't learn / Cancel" button
        public void OnCancelLearningClicked()
        {
            if (currentHero == null || pendingSkill == null) return;

            Log($"{currentHero.heroName} ha rinunciato a imparare {pendingSkill.skillName}.");
            ClosePanel(false);
        }

        private void ClosePanel(bool learned)
        {
            var callback = currentCallback;

            currentHero = null;
            pendingSkill = null;
            currentCallback = null;

            if (replacementPanel != null) replacementPanel.SetActive(false);
            RestoreTimeScale();

            callback?.Invoke(learned);

            // Show the next queued request, skipping any that became invalid in the meantime
            while (pendingRequests.Count > 0 && currentHero == null)
            {
                var next = pendingRequests.Dequeue();
                if (next.hero != null && !next.hero.isDead)
                {
                    HandleRequest(next.hero, next.skill, next.onFinished);
                }
                else
                {
                    next.onFinished?.Invoke(false);
                }
            }
        }

        private void RestoreTimeScale()
        {
            if (!pausedByThisPanel) return;

            Time.timeScale = 1f;
            pausedByThisPanel = false;
        }
    }
}

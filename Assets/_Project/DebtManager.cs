using UnityEngine;

namespace HeroManagement
{
    // When the gold goes below zero at dawn the guild is in DEBT: you have a number of days to sell things and pay.
    // The debt grows with interest every day. If it is still there when the days run out, the tavern is lost.
    public class DebtManager : MonoBehaviour
    {
        public static DebtManager Instance { get; private set; }

        [Header("Rules")]
        public int graceDays = 10;
        [Tooltip("Interest per day on the money owed (0.05 = 5%)")]
        [Range(0f, 0.3f)] public float dailyInterest = 0.05f;

        public bool InDebt { get; private set; }
        public int DaysLeft { get; private set; }

        public static event System.Action OnDebtChanged;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void OnEnable()
        {
            GameManager.OnPhaseChanged += HandlePhase;
            GameManager.OnGoldChanged += HandleGold;
        }

        void OnDisable()
        {
            GameManager.OnPhaseChanged -= HandlePhase;
            GameManager.OnGoldChanged -= HandleGold;
        }

        private void Log(string message)
        {
            if (SystemLogUIManager.Instance != null) SystemLogUIManager.Instance.LogMessage(message);
        }

        // Paid back (by selling things or earning gold): the debt is over right away
        private void HandleGold(int gold)
        {
            if (InDebt && gold >= 0)
            {
                InDebt = false;
                DaysLeft = 0;
                Log("[DEBT] Your debt is paid. The creditors leave.");
                OnDebtChanged?.Invoke();
            }
        }

        // Checked at dawn, after upkeep, payments and income of the new day
        private void HandlePhase(GameManager.DayPhase phase)
        {
            if (phase != GameManager.DayPhase.Morning || GameManager.Instance == null) return;

            int gold = GameManager.Instance.playerGold;

            if (gold >= 0)
            {
                if (InDebt)
                {
                    InDebt = false;
                    DaysLeft = 0;
                    OnDebtChanged?.Invoke();
                }
                return;
            }

            if (!InDebt)
            {
                InDebt = true;
                DaysLeft = graceDays;
                Log($"[DEBT] The guild owes {-gold} gold! Sell what you can (Sell menu, Inventory) within {graceDays} days or the tavern is lost. Interest: {dailyInterest * 100f:0}% a day.");
                OnDebtChanged?.Invoke();
                return;
            }

            int interest = Mathf.CeilToInt(-gold * dailyInterest);
            if (EconomyManager.Instance != null) EconomyManager.Instance.AddGold(-interest);

            DaysLeft--;
            Log($"[DEBT] Interest: +{interest} gold owed. {Mathf.Max(0, DaysLeft)} day(s) left.");
            OnDebtChanged?.Invoke();

            if (DaysLeft <= 0 && GameManager.Instance.playerGold < 0)
            {
                InDebt = false;
                OnDebtChanged?.Invoke();
                if (GameFlowManager.Instance != null) GameFlowManager.Instance.HandleBankruptcy();
            }
        }

        public string GetStatusText()
        {
            return InDebt ? $"DEBT: {DaysLeft} days left, {dailyInterest * 100f:0}% a day" : "";
        }

        public void Restore(bool inDebt, int daysLeft)
        {
            InDebt = inDebt;
            DaysLeft = daysLeft;
            OnDebtChanged?.Invoke();
        }
    }
}

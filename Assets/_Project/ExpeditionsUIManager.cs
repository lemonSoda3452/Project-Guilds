using UnityEngine;
using UnityEngine.UI;

namespace HeroManagement
{
    public class ExpeditionsUIManager : MonoBehaviour
    {
        [Header("Expeditions Display")]
        public Text expeditionsText;

        void OnEnable()
        {
            GameManager.OnPhaseChanged += HandlePhaseChanged;
            MissionDispatcher.OnExpeditionsChanged += Refresh;
        }

        void OnDisable()
        {
            GameManager.OnPhaseChanged -= HandlePhaseChanged;
            MissionDispatcher.OnExpeditionsChanged -= Refresh;
        }

        void Start() { Refresh(); }

        private void HandlePhaseChanged(GameManager.DayPhase phase) { Refresh(); }

        public void Refresh()
        {
            if (expeditionsText == null) return;

            var dispatcher = MissionDispatcher.Instance;
            if (dispatcher == null)
            {
                expeditionsText.text = "Nessuna spedizione in corso.";
                return;
            }

            string text = "";

            if (dispatcher.ongoingExpeditions != null && dispatcher.ongoingExpeditions.Count > 0)
            {
                text += "Spedizioni Attive:\n";
                foreach (var e in dispatcher.ongoingExpeditions)
                {
                    if (e == null || e.contract == null) continue;

                    string phase = e.phase == ExpeditionPhase.Outbound ? "in viaggio verso la meta" : "sulla via del ritorno";
                    text += $"- {e.contract.clientName}: {phase}, {e.daysRemaining} giorno/i\n";
                }
            }

            if (dispatcher.pendingPayouts != null && dispatcher.pendingPayouts.Count > 0)
            {
                text += "\nOro in arrivo:\n";
                foreach (var p in dispatcher.pendingPayouts)
                {
                    if (p != null) text += $"- {p.grossGold} da {p.region} (giorno {p.payDay})\n";
                }
            }

            expeditionsText.text = string.IsNullOrEmpty(text) ? "Nessuna spedizione in corso." : text;
        }
    }
}

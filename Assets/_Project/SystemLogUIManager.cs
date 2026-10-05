using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace HeroManagement
{
    public class SystemLogUIManager : MonoBehaviour
    {
        public static SystemLogUIManager Instance { get; private set; }

        [Header("Log Settings")]
        public Text logText;
        [Tooltip("Numero massimo di messaggi da mantenere visibili nello storico")]
        public int maxLogLines = 5;

        private Queue<string> logQueue = new Queue<string>();

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void Start()
        {
            LogMessage("Welcome to the Guild. A new day has begun.");
        }

        // Metodo globale per inviare notifiche a schermo e console
        public void LogMessage(string message)
        {
            Debug.Log("[SYSTEM LOG] " + message);

            if (logText == null) return;

            // Aggiungiamo il nuovo messaggio alla coda
            logQueue.Enqueue(message);

            // Se superiamo il limite, rimuoviamo il messaggio più vecchio
            if (logQueue.Count > maxLogLines)
            {
                logQueue.Dequeue();
            }

            // Aggiorniamo il testo unendo tutti i messaggi salvati
            logText.text = string.Join("\n", logQueue.ToArray());
        }
    }
}
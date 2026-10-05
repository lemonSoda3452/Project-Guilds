using System.Collections.Generic;
using UnityEngine;

namespace HeroManagement
{
    // The little toy characters of the 3D tavern: every hero at home and every mercenary waiting at the counter.
    // Heroes on a mission disappear and come back when the party returns.
    public class HeroAvatarSpawner : MonoBehaviour
    {
        [Tooltip("Where guild heroes stand when they are at the tavern")]
        public Transform[] guildSlots;
        [Tooltip("Where the mercenaries looking for a contract wait")]
        public Transform[] visitorSlots;

        private readonly Dictionary<HeroData, GameObject> avatars = new Dictionary<HeroData, GameObject>();

        void OnEnable()
        {
            MissionDispatcher.OnExpeditionsChanged += Refresh;
            RecruitmentManager.OnVisitorsChanged += Refresh;
            GameManager.OnPhaseChanged += HandlePhase;
        }

        void OnDisable()
        {
            MissionDispatcher.OnExpeditionsChanged -= Refresh;
            RecruitmentManager.OnVisitorsChanged -= Refresh;
            GameManager.OnPhaseChanged -= HandlePhase;
        }

        void Start() { Refresh(); }

        private void HandlePhase(GameManager.DayPhase phase) { Refresh(); }

        public void Refresh()
        {
            var wanted = new Dictionary<HeroData, KeyValuePair<Transform, string>>();

            // Guild heroes who are home (resting ones too)
            if (guildSlots != null && GameManager.Instance != null && GameManager.Instance.allHeroesDatabase != null)
            {
                int slot = 0;
                foreach (var hero in GameManager.Instance.allHeroesDatabase)
                {
                    if (hero == null || hero.isDead || hero.isBusy || slot >= guildSlots.Length) continue;

                    string label = hero.restDaysLeft > 0 ? $"{hero.heroName} (riposo {hero.restDaysLeft}g)" : hero.heroName;
                    wanted[hero] = new KeyValuePair<Transform, string>(guildSlots[slot], label);
                    slot++;
                }
            }

            // Mercenaries at the counter
            if (visitorSlots != null && RecruitmentManager.Instance != null)
            {
                int slot = 0;
                foreach (var hero in RecruitmentManager.Instance.currentTavernVisitors)
                {
                    if (hero == null || slot >= visitorSlots.Length || wanted.ContainsKey(hero)) continue;

                    wanted[hero] = new KeyValuePair<Transform, string>(visitorSlots[slot], hero.heroName + " ($)");
                    slot++;
                }
            }

            // Remove avatars that should not be here anymore
            var toRemove = new List<HeroData>();
            foreach (var pair in avatars)
            {
                if (pair.Key == null || !wanted.ContainsKey(pair.Key)) toRemove.Add(pair.Key);
            }

            foreach (var hero in toRemove)
            {
                if (avatars[hero] != null) Destroy(avatars[hero]);
                avatars.Remove(hero);
            }

            // Create or move the others
            foreach (var pair in wanted)
            {
                GameObject avatar;
                if (!avatars.TryGetValue(pair.Key, out avatar) || avatar == null)
                {
                    avatar = ChibiAvatarFactory.Create(pair.Key, transform);
                    avatars[pair.Key] = avatar;
                }

                avatar.transform.position = pair.Value.Key.position;
                avatar.transform.rotation = pair.Value.Key.rotation;

                var text = avatar.GetComponentInChildren<TextMesh>();
                if (text != null) text.text = pair.Value.Value;
            }
        }
    }
}

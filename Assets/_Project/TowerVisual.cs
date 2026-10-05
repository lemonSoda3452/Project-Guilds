using UnityEngine;

namespace HeroManagement
{
    // A little model of the Spire on a table: one disc per 10 floors, lit up in gold as the guild climbs
    public class TowerVisual : MonoBehaviour
    {
        public Renderer[] segments;
        public Color lockedColor = new Color(0.58f, 0.58f, 0.66f);
        public Color clearedColor = new Color(1f, 0.85f, 0.4f);

        void OnEnable()
        {
            TowerManager.OnFloorReached += HandleFloor;
            TowerManager.OnProgressRestored += Refresh;
        }

        void OnDisable()
        {
            TowerManager.OnFloorReached -= HandleFloor;
            TowerManager.OnProgressRestored -= Refresh;
        }

        void Start() { Refresh(); }

        private void HandleFloor(int floor) { Refresh(); }

        public void Refresh()
        {
            if (segments == null) return;

            int cleared = TowerManager.HighestFloor / 10;

            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] != null) segments[i].material.color = i < cleared ? clearedColor : lockedColor;
            }
        }
    }
}

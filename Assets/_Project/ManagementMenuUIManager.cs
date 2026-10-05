using UnityEngine;

namespace HeroManagement
{
    // The four menu buttons of the top bar: Heroes, Inventory, Witch, Tavern. One panel open at a time.
    public class ManagementMenuUIManager : MonoBehaviour
    {
        [Tooltip("In this order: hero sheet, inventory, witch, tavern")]
        public GameObject[] panels = new GameObject[4];

        public HeroSheetUIManager heroSheet;
        public InventoryUIManager inventoryUI;
        public WitchUIManager witchUI;
        public TavernBuildUIManager buildUI;

        public void OnHeroesClicked() { Toggle(0); }
        public void OnInventoryClicked() { Toggle(1); }
        public void OnWitchClicked() { Toggle(2); }
        public void OnTavernClicked() { Toggle(3); }

        public void CloseAll()
        {
            if (panels == null) return;
            foreach (var p in panels)
            {
                if (p != null) p.SetActive(false);
            }
        }

        private void Toggle(int index)
        {
            if (panels == null || index < 0 || index >= panels.Length || panels[index] == null) return;

            bool open = !panels[index].activeSelf;

            CloseAll();
            panels[index].SetActive(open);

            if (!open) return;

            switch (index)
            {
                case 0: if (heroSheet != null) heroSheet.Refresh(); break;
                case 1: if (inventoryUI != null) inventoryUI.Refresh(); break;
                case 2: if (witchUI != null) witchUI.Refresh(); break;
                case 3: if (buildUI != null) buildUI.Refresh(); break;
            }
        }
    }
}
